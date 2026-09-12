using System.Net.Http.Headers;
using System.Net.Http.Json;
using GlicoNutri.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace GlicoNutri.Tests.Integracao;

/// <summary>
/// Sobe a API contra um banco Postgres descartável, criado e destruído a cada
/// execução. Regras como a RN16 vivem em índice do banco, e não sobrevivem a um
/// provedor em memória — testá-las exige o Postgres de verdade.
/// </summary>
public class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>Prefixo dos bancos descartáveis, usado também para varrer órfãos.</summary>
    private const string Prefixo = "gliconutri_teste_";

    private readonly string _banco = $"{Prefixo}{Guid.NewGuid():N}"[..28];

    /// <summary>
    /// A connection string vem do user-secrets compartilhado com a API — a mesma
    /// chave que a aplicação usa —, para a senha do banco não entrar no código
    /// nem no repositório.
    /// </summary>
    private static readonly string ConexaoBase =
        new ConfigurationBuilder()
            .AddUserSecrets<ApiFixture>()
            .AddEnvironmentVariables()
            .Build()
            .GetConnectionString("Supabase")
        ?? throw new InvalidOperationException(
            "ConnectionStrings:Supabase não configurada. Rode: dotnet user-secrets set " +
            "\"ConnectionStrings:Supabase\" \"...\" no projeto GlicoNutri.Api.");

    private static string TrocarBanco(string conexao, string banco) =>
        new NpgsqlConnectionStringBuilder(conexao) { Database = banco }.ConnectionString;

    private string ConexaoDoTeste => TrocarBanco(ConexaoBase, _banco);

    async Task IAsyncLifetime.InitializeAsync()
    {
        await using (var admin = new NpgsqlConnection(TrocarBanco(ConexaoBase, "postgres")))
        {
            await admin.OpenAsync();

            // Uma execução interrompida deixa banco para trás. Varrer antes evita
            // que os descartáveis se acumulem no projeto ao longo do tempo.
            await LimparOrfaosAsync(admin);

            await using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{_banco}\"", admin);
            await cmd.ExecuteNonQueryAsync();
        }

        // A migration roda num contexto próprio, e não pelo provedor da aplicação:
        // tocar em Services já constrói o host, e o seeder de desenvolvimento
        // consulta "usuarios" na inicialização — antes de a tabela existir.
        await using var db = CriarContexto();
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        NpgsqlConnection.ClearAllPools();

        await using var admin = new NpgsqlConnection(TrocarBanco(ConexaoBase, "postgres"));
        await admin.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS \"{_banco}\" WITH (FORCE)", admin);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Remove bancos de teste de execuções anteriores. Só apaga o que casa com o
    /// prefixo: o banco da aplicação nunca entra no filtro.
    /// </summary>
    private static async Task LimparOrfaosAsync(NpgsqlConnection admin)
    {
        var orfaos = new List<string>();

        await using (var busca = new NpgsqlCommand(
            "select datname from pg_database where datname like @prefixo", admin))
        {
            busca.Parameters.AddWithValue("prefixo", $"{Prefixo}%");
            await using var leitor = await busca.ExecuteReaderAsync();
            while (await leitor.ReadAsync()) orfaos.Add(leitor.GetString(0));
        }

        foreach (var orfao in orfaos)
        {
            await using var drop = new NpgsqlCommand(
                $"DROP DATABASE IF EXISTS \"{orfao}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Sobrescreve as DUAS chaves. A aplicação escolhe entre elas por
        // configuração, e deixar qualquer uma apontando para o banco real faria
        // a suíte escrever em produção.
        builder.UseSetting("ConnectionStrings:Supabase", ConexaoDoTeste);
        builder.UseSetting("ConnectionStrings:Default", ConexaoDoTeste);
        builder.UseSetting("USAR_LOCAL", "false");
        builder.UseSetting("Jwt:Issuer", "gliconutri-teste");
        builder.UseSetting("Jwt:Audience", "gliconutri-teste");
        builder.UseSetting("Jwt:SigningKey", "chave-de-teste-com-mais-de-32-bytes-para-o-hmac");
        builder.UseSetting("Jwt:ExpiresHours", "24");
        builder.UseSetting("Email:Habilitado", "false");
    }

    public GlicoNutriDbContext CriarContexto()
    {
        var opcoes = new DbContextOptionsBuilder<GlicoNutriDbContext>()
            .UseNpgsql(ConexaoDoTeste)
            .Options;

        return new GlicoNutriDbContext(opcoes);
    }

    // ── Atalhos usados pelos testes ─────────────────────────────────────────

    public async Task<HttpClient> ClienteAdminAsync()
    {
        var cliente = CreateClient();
        var login = await cliente.PostAsJsonAsync("/api/auth/login", new
        {
            email = SeedDesenvolvimento.EmailAdmin,
            senha = SeedDesenvolvimento.SenhaAdmin,
        });

        login.EnsureSuccessStatusCode();
        var dados = await login.Content.ReadFromJsonAsync<RespostaLogin>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", dados!.Token);
        return cliente;
    }

    public async Task<(HttpClient Cliente, long Id)> CriarNutricionistaAsync(
        HttpClient admin, string email, string crn)
    {
        var criacao = await admin.PostAsJsonAsync("/api/nutricionistas", new
        {
            nome = $"Nutricionista {crn}", email, crn,
        });
        criacao.EnsureSuccessStatusCode();
        var criado = await criacao.Content.ReadFromJsonAsync<RespostaId>();

        // A senha provisória é gerada pelo sistema; o teste a substitui direto no
        // banco, já que o e-mail não é enviado em ambiente de teste.
        var senha = "Teste@2026";
        await using (var db = CriarContexto())
        {
            var usuario = await db.Usuarios.FirstAsync(u => u.Email == email);
            usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(senha, 4);
            usuario.SenhaProvisoria = false;
            await db.SaveChangesAsync();
        }

        var cliente = CreateClient();
        var login = await cliente.PostAsJsonAsync("/api/auth/login", new { email, senha });
        login.EnsureSuccessStatusCode();
        var dados = await login.Content.ReadFromJsonAsync<RespostaLogin>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", dados!.Token);

        return (cliente, criado!.Id);
    }

    public async Task<long> CriarPacienteAsync(HttpClient nutricionista, string email, string cpf)
    {
        var resposta = await nutricionista.PostAsJsonAsync("/api/pacientes", new
        {
            nome = $"Paciente {cpf[..3]}",
            email,
            cpf,
            dataNascimento = "2000-01-15",
            sexoId = 2,
            tipoDiabetesId = 1,
        });

        resposta.EnsureSuccessStatusCode();
        var criado = await resposta.Content.ReadFromJsonAsync<RespostaId>();
        return criado!.Id;
    }

    /// <summary>
    /// Cliente autenticado como o próprio paciente. A senha provisória é trocada
    /// direto no banco, pelo mesmo motivo do cadastro de nutricionista.
    /// </summary>
    public async Task<HttpClient> ClientePacienteAsync(string email)
    {
        const string senha = "Paciente@2026";

        await using (var db = CriarContexto())
        {
            var usuario = await db.Usuarios.FirstAsync(u => u.Email == email);
            usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(senha, 4);
            usuario.SenhaProvisoria = false;
            await db.SaveChangesAsync();
        }

        var cliente = CreateClient();
        var login = await cliente.PostAsJsonAsync("/api/auth/login", new { email, senha });
        login.EnsureSuccessStatusCode();
        var dados = await login.Content.ReadFromJsonAsync<RespostaLogin>();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", dados!.Token);

        return cliente;
    }

    public record RespostaLogin(string Token, bool SenhaProvisoria);
    public record RespostaId(long Id);
}

[CollectionDefinition("api")]
public class ApiCollection : ICollectionFixture<ApiFixture>;
