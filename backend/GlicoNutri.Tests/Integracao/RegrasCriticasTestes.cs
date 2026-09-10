using System.Net;
using System.Net.Http.Json;
using GlicoNutri.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace GlicoNutri.Tests.Integracao;

/// <summary>
/// Regras cuja violação corrompe dado clínico. Rodam contra Postgres real
/// porque parte delas mora em constraint do banco, não no serviço.
/// </summary>
[Collection("api")]
public class RegrasCriticasTestes(ApiFixture api)
{
    private static string Cpf(int n) => GerarCpf(n);

    // ── RN02 — bloqueio progressivo ─────────────────────────────────────────

    [Fact]
    public async Task RN02_CincoTentativasInvalidas_BloqueiamAContaComTempoInformado()
    {
        var admin = await api.ClienteAdminAsync();
        var (_, _) = await api.CriarNutricionistaAsync(admin, "rn02@teste.local", "CRN-RN02");

        var cliente = api.CreateClient();
        HttpResponseMessage? ultima = null;

        for (var i = 0; i < 5; i++)
        {
            ultima = await cliente.PostAsJsonAsync("/api/auth/login",
                new { email = "rn02@teste.local", senha = "senha-errada" });
        }

        Assert.Equal(HttpStatusCode.Locked, ultima!.StatusCode);

        var falha = await ultima.Content.ReadFromJsonAsync<FalhaLogin>();
        Assert.True(falha!.Bloqueado);
        // 1º bloqueio: 5 minutos.
        Assert.InRange(falha.SegundosRestantes ?? 0, 250, 300);

        // A senha correta também é recusada enquanto durar o bloqueio.
        var comSenhaCerta = await cliente.PostAsJsonAsync("/api/auth/login",
            new { email = "rn02@teste.local", senha = "Teste@2026" });

        Assert.Equal(HttpStatusCode.Locked, comSenhaCerta.StatusCode);
    }

    [Fact]
    public async Task RN02_OsBloqueiosSeguintesSaoProgressivamenteMaisLongos()
    {
        var admin = await api.ClienteAdminAsync();
        await api.CriarNutricionistaAsync(admin, "rn02b@teste.local", "CRN-RN02B");

        var cliente = api.CreateClient();

        async Task<int> BloquearEMedirAsync()
        {
            for (var i = 0; i < 5; i++)
            {
                await cliente.PostAsJsonAsync("/api/auth/login",
                    new { email = "rn02b@teste.local", senha = "errada" });
            }

            await using var db = api.CriarContexto();
            var usuario = await db.Usuarios.FirstAsync(u => u.Email == "rn02b@teste.local");
            var minutos = (int)Math.Round((usuario.BloqueadoAte!.Value - DateTime.UtcNow).TotalMinutes);

            // Expira o bloqueio para exercitar o próximo nível.
            usuario.BloqueadoAte = DateTime.UtcNow.AddSeconds(-1);
            await db.SaveChangesAsync();

            return minutos;
        }

        Assert.InRange(await BloquearEMedirAsync(), 4, 5);        // 5 min
        Assert.InRange(await BloquearEMedirAsync(), 29, 30);      // 30 min
        Assert.InRange(await BloquearEMedirAsync(), 1439, 1440);  // 24 h
    }

    // ── RN03 — senha provisória ─────────────────────────────────────────────

    [Fact]
    public async Task RN03_ComSenhaProvisoria_SoATrocaDeSenhaEPermitida()
    {
        var admin = await api.ClienteAdminAsync();

        var criacao = await admin.PostAsJsonAsync("/api/nutricionistas", new
        {
            nome = "Provisorio", email = "rn03@teste.local", crn = "CRN-RN03",
        });
        criacao.EnsureSuccessStatusCode();

        var senha = "Provisoria1";
        await using (var db = api.CriarContexto())
        {
            var usuario = await db.Usuarios.FirstAsync(u => u.Email == "rn03@teste.local");
            usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(senha, 4);
            await db.SaveChangesAsync();
        }

        var cliente = api.CreateClient();
        var login = await cliente.PostAsJsonAsync("/api/auth/login",
            new { email = "rn03@teste.local", senha });

        var dados = await login.Content.ReadFromJsonAsync<ApiFixture.RespostaLogin>();
        Assert.True(dados!.SenhaProvisoria);

        cliente.DefaultRequestHeaders.Authorization = new("Bearer", dados.Token);

        var bloqueada = await cliente.GetAsync("/api/pacientes");
        Assert.Equal(HttpStatusCode.Forbidden, bloqueada.StatusCode);

        var troca = await cliente.PostAsJsonAsync("/api/auth/alterar-senha",
            new { senhaAtual = senha, novaSenha = "Definitiva2026" });
        Assert.Equal(HttpStatusCode.NoContent, troca.StatusCode);
    }

    // ── RN06 — unicidade de e-mail ──────────────────────────────────────────

    [Fact]
    public async Task RN06_EmailDesativadoContinuaOcupado()
    {
        var admin = await api.ClienteAdminAsync();
        var (_, id) = await api.CriarNutricionistaAsync(admin, "rn06@teste.local", "CRN-RN06");

        var desativa = await admin.DeleteAsync($"/api/nutricionistas/{id}");
        Assert.Equal(HttpStatusCode.NoContent, desativa.StatusCode);

        // A RN05 garante reativação com todo o histórico: o e-mail segue reservado.
        var duplicata = await admin.PostAsJsonAsync("/api/nutricionistas", new
        {
            nome = "Outro", email = "rn06@teste.local", crn = "CRN-RN06-B",
        });

        Assert.Equal(HttpStatusCode.BadRequest, duplicata.StatusCode);
    }

    // ── RN13, RN15 e RN16 — plano alimentar ─────────────────────────────────

    [Fact]
    public async Task RN13_PlanoExigeCalculoEnergeticoPrevio()
    {
        var admin = await api.ClienteAdminAsync();
        var (nutri, _) = await api.CriarNutricionistaAsync(admin, "rn13@teste.local", "CRN-RN13");
        var paciente = await api.CriarPacienteAsync(nutri, "p.rn13@teste.local", Cpf(13));

        var semVet = await nutri.PostAsJsonAsync($"/api/pacientes/{paciente}/plano-alimentar",
            new { objetivo = "Sem VET", dataInicio = "2026-09-07" });

        Assert.Equal(HttpStatusCode.BadRequest, semVet.StatusCode);

        await DarVetAsync(nutri, paciente);

        var comVet = await nutri.PostAsJsonAsync($"/api/pacientes/{paciente}/plano-alimentar",
            new { objetivo = "Com VET", dataInicio = "2026-09-07" });

        Assert.Equal(HttpStatusCode.Created, comVet.StatusCode);
    }

    [Theory]
    [InlineData(50, 20, 25, false)]
    [InlineData(60, 25, 30, false)]
    [InlineData(55, 20, 25, true)]
    public async Task RN15_SomaDosPercentuaisPrecisaFecharEmCem(
        double cho, double ptn, double lip, bool aceito)
    {
        var admin = await api.ClienteAdminAsync();
        var sufixo = $"{cho}-{ptn}-{lip}".Replace(".", string.Empty);
        var (nutri, _) = await api.CriarNutricionistaAsync(
            admin, $"rn15-{sufixo}@teste.local", $"CRN-RN15-{sufixo}");

        var paciente = await api.CriarPacienteAsync(nutri, $"p.rn15-{sufixo}@teste.local", Cpf((int)(cho + ptn)));
        await DarVetAsync(nutri, paciente);

        var resposta = await nutri.PostAsJsonAsync($"/api/pacientes/{paciente}/plano-alimentar", new
        {
            objetivo = "Distribuição",
            dataInicio = "2026-09-07",
            distribuicao = new
            {
                carboidratosPercentual = cho,
                proteinasPercentual = ptn,
                lipidiosPercentual = lip,
            },
        });

        Assert.Equal(aceito ? HttpStatusCode.Created : HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task RN16_AlternarEntrePlanosMantemExatamenteUmAtivo()
    {
        var admin = await api.ClienteAdminAsync();
        var (nutri, _) = await api.CriarNutricionistaAsync(admin, "rn16@teste.local", "CRN-RN16");
        var paciente = await api.CriarPacienteAsync(nutri, "p.rn16@teste.local", Cpf(16));
        await DarVetAsync(nutri, paciente);

        var ids = new List<long>();
        for (var i = 1; i <= 3; i++)
        {
            var criado = await nutri.PostAsJsonAsync($"/api/pacientes/{paciente}/plano-alimentar",
                new { objetivo = $"Plano {i}", dataInicio = "2026-09-07" });

            criado.EnsureSuccessStatusCode();
            ids.Add((await criado.Content.ReadFromJsonAsync<ApiFixture.RespostaId>())!.Id);
        }

        await AssertUmUnicoPlanoAtivoAsync(paciente, ids[2]);

        // Reativar um plano antigo precisa liberar o slot antes de ocupá-lo,
        // senão o índice único parcial da RN16 recusa o lote.
        foreach (var id in new[] { ids[0], ids[1], ids[0] })
        {
            var reativa = await nutri.PostAsJsonAsync(
                $"/api/pacientes/{paciente}/plano-alimentar/{id}/reativar", new { });

            Assert.Equal(HttpStatusCode.OK, reativa.StatusCode);
            await AssertUmUnicoPlanoAtivoAsync(paciente, id);
        }

        // RN17 — nenhum plano some do histórico.
        await using var db = api.CriarContexto();
        var total = await db.PlanosAlimentares.IgnoreQueryFilters()
            .CountAsync(p => p.PacienteId == paciente);

        Assert.Equal(3, total);
    }

    [Fact]
    public async Task RN16_OBancoRecusaDoisPlanosAtivosMesmoPorForaDaAplicacao()
    {
        var admin = await api.ClienteAdminAsync();
        var (nutri, _) = await api.CriarNutricionistaAsync(admin, "rn16b@teste.local", "CRN-RN16B");
        var paciente = await api.CriarPacienteAsync(nutri, "p.rn16b@teste.local", Cpf(17));
        await DarVetAsync(nutri, paciente);

        for (var i = 1; i <= 2; i++)
        {
            var criado = await nutri.PostAsJsonAsync($"/api/pacientes/{paciente}/plano-alimentar",
                new { objetivo = $"Plano {i}", dataInicio = "2026-09-07" });
            criado.EnsureSuccessStatusCode();
        }

        await using var db = api.CriarContexto();

        // A regra não depende da aplicação estar correta.
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            var planos = await db.PlanosAlimentares.IgnoreQueryFilters()
                .Where(p => p.PacienteId == paciente).ToListAsync();

            foreach (var plano in planos) plano.Ativo = true;
            await db.SaveChangesAsync();
        });
    }

    // ── RN19 e RN20 — classificação glicêmica ───────────────────────────────

    [Fact]
    public async Task RN19_MudarAFaixaAlvoReclassificaOHistoricoInteiro()
    {
        var admin = await api.ClienteAdminAsync();
        var (nutri, _) = await api.CriarNutricionistaAsync(admin, "rn19@teste.local", "CRN-RN19");
        var paciente = await api.CriarPacienteAsync(nutri, "p.rn19@teste.local", Cpf(19));

        foreach (var valor in new[] { 95, 168, 62, 210 })
        {
            var r = await nutri.PostAsJsonAsync($"/api/pacientes/{paciente}/glicemia",
                new { valor, contextoId = 1 });
            r.EnsureSuccessStatusCode();
        }

        // Sem personalização vale o padrão 70–180: 62 e 210 ficam fora.
        Assert.Equal(50, await PercentualNoAlvoAsync(nutri, paciente));

        var aperta = await nutri.PutAsJsonAsync($"/api/pacientes/{paciente}/metas-glicemicas",
            new { glicemiaMinAlvo = 80, glicemiaMaxAlvo = 160 });
        aperta.EnsureSuccessStatusCode();

        // 168 passa a estar fora: o histórico acompanha a faixa vigente.
        Assert.Equal(25, await PercentualNoAlvoAsync(nutri, paciente));

        var afrouxa = await nutri.PutAsJsonAsync($"/api/pacientes/{paciente}/metas-glicemicas",
            new { glicemiaMinAlvo = 60, glicemiaMaxAlvo = 220 });
        afrouxa.EnsureSuccessStatusCode();

        Assert.Equal(100, await PercentualNoAlvoAsync(nutri, paciente));
    }

    // ── RN33 — isolamento entre nutricionistas ──────────────────────────────

    [Fact]
    public async Task RN33_NutricionistaNaoAlcancaPacienteDeOutro()
    {
        var admin = await api.ClienteAdminAsync();
        var (dona, _) = await api.CriarNutricionistaAsync(admin, "rn33a@teste.local", "CRN-RN33A");
        var (outra, _) = await api.CriarNutricionistaAsync(admin, "rn33b@teste.local", "CRN-RN33B");

        var paciente = await api.CriarPacienteAsync(dona, "p.rn33@teste.local", Cpf(33));

        Assert.Equal(HttpStatusCode.OK, (await dona.GetAsync($"/api/pacientes/{paciente}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outra.GetAsync($"/api/pacientes/{paciente}")).StatusCode);

        foreach (var rota in new[] { "glicemia", "antropometria", "emocoes", "plano-alimentar", "alertas" })
        {
            var resposta = await outra.GetAsync($"/api/pacientes/{paciente}/{rota}");
            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        // E o paciente não aparece na listagem da outra profissional.
        var lista = await outra.GetFromJsonAsync<List<ApiFixture.RespostaId>>("/api/pacientes");
        Assert.DoesNotContain(lista!, p => p.Id == paciente);
    }

    // ── Apoio ───────────────────────────────────────────────────────────────

    private static async Task DarVetAsync(HttpClient nutri, long paciente)
    {
        var medida = await nutri.PostAsJsonAsync($"/api/pacientes/{paciente}/antropometria",
            new { peso = 68.0, altura = 170.0 });
        medida.EnsureSuccessStatusCode();

        var vet = await nutri.PostAsJsonAsync($"/api/pacientes/{paciente}/necessidade-energetica",
            new { formulaId = 1, nivelAtividadeId = 2 });
        vet.EnsureSuccessStatusCode();
    }

    private async Task AssertUmUnicoPlanoAtivoAsync(long paciente, long esperado)
    {
        await using var db = api.CriarContexto();
        var ativos = await db.PlanosAlimentares.IgnoreQueryFilters()
            .Where(p => p.PacienteId == paciente && p.Ativo)
            .Select(p => p.Id)
            .ToListAsync();

        Assert.Single(ativos);
        Assert.Equal(esperado, ativos[0]);
    }

    private static async Task<double?> PercentualNoAlvoAsync(HttpClient cliente, long paciente)
    {
        var historico = await cliente.GetFromJsonAsync<HistoricoGlicemico>(
            $"/api/pacientes/{paciente}/glicemia?dias=7");

        return historico!.Resumo.PercentualNoAlvo;
    }

    /// <summary>Gera um CPF com dígitos verificadores válidos a partir de uma semente.</summary>
    private static string GerarCpf(int semente)
    {
        var digitos = new int[11];
        var rnd = new Random(semente);
        for (var i = 0; i < 9; i++) digitos[i] = rnd.Next(0, 10);

        // Evita a sequência repetida, que é recusada mesmo com dígitos corretos.
        if (digitos.Take(9).Distinct().Count() == 1) digitos[0] = (digitos[0] + 1) % 10;

        for (var d = 9; d <= 10; d++)
        {
            var peso = d + 1;
            var soma = 0;
            for (var i = 0; i < d; i++) soma += digitos[i] * peso--;
            var resto = soma * 10 % 11;
            digitos[d] = resto == 10 ? 0 : resto;
        }

        return string.Concat(digitos);
    }

    private record FalhaLogin(string Mensagem, bool Bloqueado, int? SegundosRestantes);
    private record ResumoGlicemico(double? PercentualNoAlvo);
    private record HistoricoGlicemico(ResumoGlicemico Resumo);
}
