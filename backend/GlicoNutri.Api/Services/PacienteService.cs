using GlicoNutri.Api.Data;
using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Models;
using GlicoNutri.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GlicoNutri.Api.Services;

public interface IPacienteService
{
    Task<Resultado<PacienteResponse>> CriarAsync(
        CriarPacienteRequest pedido, long autorId, bool autorEhAdministrador, CancellationToken ct = default);

    Task<IReadOnlyList<PacienteResponse>> ListarAsync(
        long? nutricionistaId, bool incluirInativos, CancellationToken ct = default);

    Task<PacienteResponse?> BuscarAsync(long id, CancellationToken ct = default);

    Task<Resultado<PacienteResponse>> DefinirMetasGlicemicasAsync(
        long id, DefinirMetasGlicemicasRequest pedido, CancellationToken ct = default);

    Task<Resultado<bool>> DesativarAsync(long id, CancellationToken ct = default);
    Task<Resultado<bool>> ReativarAsync(long id, CancellationToken ct = default);
}

/// <summary>
/// UC002 — Cadastrar Paciente. Ação restrita a Nutricionista e Administrador
/// (RN08): o paciente nunca cria a própria conta.
/// </summary>
public class PacienteService(
    GlicoNutriDbContext db,
    IUsuarioRepository usuarios,
    ISenhaService senhas,
    IEmailService emails,
    IGlicemiaService glicemias) : IPacienteService
{
    /// <summary>RN24 — horários padrão pré-configurados no cadastro do paciente.</summary>
    private static readonly TimeOnly HorarioAlertaManha = new(8, 0);
    private static readonly TimeOnly HorarioAlertaNoite = new(20, 0);

    /// <summary>
    /// Máscara de dias da semana: 7 posições, começando no domingo, 1 = dispara.
    /// O DER V2.0 limita dias_semana a VARCHAR(20), o que não comporta a lista de
    /// siglas por extenso.
    /// </summary>
    private const string TodosOsDias = "1111111";

    private const string MensagemAlertaPadrao =
        "Hora de registrar sua glicemia. Toque para abrir o GlicoNutri.";

    public async Task<Resultado<PacienteResponse>> CriarAsync(
        CriarPacienteRequest pedido, long autorId, bool autorEhAdministrador, CancellationToken ct = default)
    {
        var email = pedido.Email.Trim().ToLowerInvariant();

        // UC002 A1 — dígito verificador do CPF.
        if (!ValidadorCpf.EhValido(pedido.Cpf))
            return Resultado<PacienteResponse>.Erro("CPF inválido.");

        var cpf = ValidadorCpf.Normalizar(pedido.Cpf);

        // UC002 A2 — duplicidade de e-mail (RN06) e de CPF.
        if (await usuarios.EmailEmUsoAsync(email, ct))
            return Resultado<PacienteResponse>.Erro("Já existe um usuário cadastrado com este e-mail.");

        if (await db.Pacientes.IgnoreQueryFilters().AnyAsync(p => p.Cpf == cpf, ct))
            return Resultado<PacienteResponse>.Erro("Já existe um paciente cadastrado com este CPF.");

        // RN07 — o vínculo com um nutricionista responsável ativo é obrigatório.
        // Quando quem cadastra é o próprio Nutricionista, ele se vincula (UC002
        // RN03); quando é o Administrador, que não é nutricionista, o responsável
        // precisa ser escolhido explicitamente.
        long nutricionistaId;
        if (autorEhAdministrador)
        {
            if (pedido.NutricionistaId is null)
                return Resultado<PacienteResponse>.Erro(
                    "Informe o nutricionista responsável pelo paciente.");

            nutricionistaId = pedido.NutricionistaId.Value;
        }
        else
        {
            nutricionistaId = autorId;
        }

        if (!await db.Nutricionistas.AnyAsync(n => n.Id == nutricionistaId, ct))
            return Resultado<PacienteResponse>.Erro("Nutricionista responsável não encontrado ou inativo.");

        if (!await db.SexosBiologicos.AnyAsync(s => s.Id == pedido.SexoId && s.Ativo, ct))
            return Resultado<PacienteResponse>.Erro("Sexo biológico inválido.");

        if (!await db.TiposDiabetes.AnyAsync(t => t.Id == pedido.TipoDiabetesId && t.Ativo, ct))
            return Resultado<PacienteResponse>.Erro("Tipo de diabetes inválido.");

        var perfil = await db.PerfisUsuario.FirstAsync(p => p.Codigo == Codigos.Perfil.Paciente, ct);
        var tipoAlertaGlicemia = await db.TiposAlerta.FirstAsync(t => t.Codigo == Codigos.Alerta.Glicemia, ct);

        var senhaProvisoria = senhas.GerarProvisoria();
        var agora = DateTime.UtcNow;

        var paciente = new Paciente
        {
            Nome = pedido.Nome.Trim(),
            Email = email,
            SenhaHash = senhas.Hash(senhaProvisoria),
            PerfilId = perfil.Id,
            Cpf = cpf,
            SexoId = pedido.SexoId,
            TipoDiabetesId = pedido.TipoDiabetesId,
            DataNascimento = pedido.DataNascimento,
            Telefone = pedido.Telefone?.Trim(),
            MedicacaoEmUso = pedido.MedicacaoEmUso?.Trim(),
            ObservacoesClinicas = pedido.ObservacoesClinicas?.Trim(),
            // RN20 — a faixa alvo nasce vazia de propósito: enquanto o Nutricionista
            // não personalizar, o sistema aplica 70–180 mg/dL e sinaliza a pendência.
            GlicemiaMinAlvo = null,
            GlicemiaMaxAlvo = null,
            Ativo = true,
            DataCadastro = agora,
            SenhaProvisoria = true,
        };

        paciente.Vinculos.Add(new NutricionistaPaciente
        {
            NutricionistaId = nutricionistaId,
            DataVinculo = agora,
            Ativo = true,
        });

        // RN24 — dois alertas de glicemia pré-configurados às 8h e 20h. Um único
        // registro cobre os dois horários, como modelado no DER (horario_1/horario_2).
        paciente.Alertas.Add(new Alerta
        {
            TipoId = tipoAlertaGlicemia.Id,
            Mensagem = MensagemAlertaPadrao,
            Horario1 = HorarioAlertaManha,
            Horario2 = HorarioAlertaNoite,
            DiasSemana = TodosOsDias,
            Ativo = true,
        });

        // O read model do dashboard nasce junto do paciente, conforme a nota do DER.
        paciente.Resumo = new ResumoClinicoPaciente
        {
            PlanoAtivo = false,
            AlertasPendentesCount = 0,
            DataAtualizacao = agora,
        };

        db.Pacientes.Add(paciente);
        await db.SaveChangesAsync(ct);

        // RF10.2 — o paciente recebe e-mail com as credenciais de acesso.
        await emails.EnviarBoasVindasAsync(paciente.Email, paciente.Nome, senhaProvisoria, ct);

        return Resultado<PacienteResponse>.Ok((await BuscarAsync(paciente.Id, ct))!);
    }

    public async Task<IReadOnlyList<PacienteResponse>> ListarAsync(
        long? nutricionistaId, bool incluirInativos, CancellationToken ct = default)
    {
        var consulta = incluirInativos ? db.Pacientes.IgnoreQueryFilters() : db.Pacientes;

        // RN33 — o Nutricionista enxerga apenas os pacientes sob sua responsabilidade.
        if (nutricionistaId is { } id)
            consulta = consulta.Where(p => p.Vinculos.Any(v => v.NutricionistaId == id && v.Ativo));

        return await consulta.OrderBy(p => p.Nome).Select(Projecao()).ToListAsync(ct);
    }

    public Task<PacienteResponse?> BuscarAsync(long id, CancellationToken ct = default) =>
        db.Pacientes.Where(p => p.Id == id).Select(Projecao()).FirstOrDefaultAsync(ct);

    /// <summary>
    /// RN20 — define a faixa glicêmica alvo individual do paciente. É o que faz o
    /// sistema deixar de usar o padrão clínico de 70–180 mg/dL para esse paciente.
    /// </summary>
    public async Task<Resultado<PacienteResponse>> DefinirMetasGlicemicasAsync(
        long id, DefinirMetasGlicemicasRequest pedido, CancellationToken ct = default)
    {
        var paciente = await db.Pacientes.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (paciente is null)
            return Resultado<PacienteResponse>.Erro("Paciente não encontrado.");

        if (pedido.GlicemiaMinAlvo is { } min && pedido.GlicemiaMaxAlvo is { } max && min >= max)
            return Resultado<PacienteResponse>.Erro(
                "O limite mínimo de glicemia deve ser menor que o máximo.");

        paciente.GlicemiaMinAlvo = pedido.GlicemiaMinAlvo;
        paciente.GlicemiaMaxAlvo = pedido.GlicemiaMaxAlvo;
        await db.SaveChangesAsync(ct);

        // RN19 — a nova faixa vale para todo o histórico, não só para os próximos
        // registros; o percentual no alvo tem de refletir um critério único.
        await glicemias.ReclassificarAsync(id, ct);

        return Resultado<PacienteResponse>.Ok((await BuscarAsync(id, ct))!);
    }

    /// <summary>RN05 — soft delete preservando todo o histórico clínico.</summary>
    public async Task<Resultado<bool>> DesativarAsync(long id, CancellationToken ct = default)
    {
        var paciente = await db.Pacientes.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (paciente is null) return Resultado<bool>.Erro("Paciente não encontrado.");

        paciente.Ativo = false;
        await db.SaveChangesAsync(ct);
        return Resultado<bool>.Ok(true);
    }

    public async Task<Resultado<bool>> ReativarAsync(long id, CancellationToken ct = default)
    {
        var paciente = await db.Pacientes.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (paciente is null) return Resultado<bool>.Erro("Paciente não encontrado.");

        paciente.Ativo = true;
        await db.SaveChangesAsync(ct);
        return Resultado<bool>.Ok(true);
    }

    private static System.Linq.Expressions.Expression<Func<Paciente, PacienteResponse>> Projecao() =>
        p => new PacienteResponse(
            p.Id,
            p.Nome,
            p.Email,
            p.Cpf,
            p.DataNascimento,
            // Idade em anos completos. A conta vai inline porque uma chamada de
            // método estático não seria traduzida para SQL pelo provedor.
            DateTime.UtcNow.Year - p.DataNascimento.Year
                - (DateTime.UtcNow.Month < p.DataNascimento.Month
                   || (DateTime.UtcNow.Month == p.DataNascimento.Month
                       && DateTime.UtcNow.Day < p.DataNascimento.Day) ? 1 : 0),
            p.Sexo.Descricao,
            p.TipoDiabetes.Descricao,
            p.Telefone,
            p.MedicacaoEmUso,
            p.ObservacoesClinicas,
            p.GlicemiaMinAlvo,
            p.GlicemiaMaxAlvo,
            p.GlicemiaMinAlvo != null && p.GlicemiaMaxAlvo != null,
            p.Vinculos.Where(v => v.Ativo).Select(v => (long?)v.NutricionistaId).FirstOrDefault(),
            p.Vinculos.Where(v => v.Ativo).Select(v => v.Nutricionista.Nome).FirstOrDefault(),
            p.Ativo,
            p.DataCadastro);
}
