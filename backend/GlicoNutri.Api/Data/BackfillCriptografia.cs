using GlicoNutri.Api.Security;
using GlicoNutri.Api.Services;
using Npgsql;

namespace GlicoNutri.Api.Data;

/// <summary>
/// RNF01 — cifra os dados sensíveis que já existiam no banco antes desta
/// versão. Roda no início da aplicação, com SQL puro (sem o DbContext, que já
/// espera a cifra): reproduzir o mesmo AES-GCM direto no banco seria
/// arriscado, e é só código que já existe e já é testado.
///
/// Idempotente: paciente com cpf_hash preenchido já foi migrado, e não é
/// tocado de novo. Telefone de nutricionista não tem uma coluna "já migrou" —
/// a checagem é tentar decifrar; falhou, ainda está em claro.
/// </summary>
public static class BackfillCriptografia
{
    public static async Task AplicarAsync(string conexao, CriptografiaService cripto, ILogger log)
    {
        await using var db = new NpgsqlConnection(conexao);
        await db.OpenAsync();

        // As linhas são lidas inteiras ANTES de qualquer UPDATE: Npgsql não aceita
        // um segundo comando na mesma conexão enquanto o leitor do SELECT segue aberto.
        var pacientesLidos = new List<(long Id, string Cpf, string? Telefone, string? Obs, string? Medicacao)>();
        await using (var selecionar = new NpgsqlCommand(
            """
            SELECT id, cpf, telefone, observacoes_clinicas, medicacao_em_uso
            FROM usuarios WHERE perfil_id = 1 AND cpf_hash IS NULL
            """, db))
        await using (var leitor = await selecionar.ExecuteReaderAsync())
        {
            while (await leitor.ReadAsync())
                pacientesLidos.Add((
                    leitor.GetInt64(0), leitor.GetString(1),
                    leitor.IsDBNull(2) ? null : leitor.GetString(2),
                    leitor.IsDBNull(3) ? null : leitor.GetString(3),
                    leitor.IsDBNull(4) ? null : leitor.GetString(4)));
        }

        var pacientes = 0;
        foreach (var p in pacientesLidos)
        {
            var cpfNormalizado = ValidadorCpf.Normalizar(p.Cpf);
            await using var atualizar = new NpgsqlCommand(
                """
                UPDATE usuarios SET cpf = @cpf, cpf_hash = @hash,
                       telefone = @telefone, observacoes_clinicas = @obs, medicacao_em_uso = @medicacao
                WHERE id = @id
                """, db);
            atualizar.Parameters.AddWithValue("cpf", cripto.Criptografar(p.Cpf)!);
            atualizar.Parameters.AddWithValue("hash", cripto.HashBusca(cpfNormalizado));
            atualizar.Parameters.AddWithValue("telefone", (object?)CifrarOuNulo(cripto, p.Telefone) ?? DBNull.Value);
            atualizar.Parameters.AddWithValue("obs", (object?)CifrarOuNulo(cripto, p.Obs) ?? DBNull.Value);
            atualizar.Parameters.AddWithValue("medicacao", (object?)CifrarOuNulo(cripto, p.Medicacao) ?? DBNull.Value);
            atualizar.Parameters.AddWithValue("id", p.Id);
            await atualizar.ExecuteNonQueryAsync();
            pacientes++;
        }

        var nutricionistasLidos = new List<(long Id, string Telefone)>();
        await using (var selecionar = new NpgsqlCommand(
            "SELECT id, telefone FROM usuarios WHERE perfil_id = 2 AND telefone IS NOT NULL", db))
        await using (var leitor = await selecionar.ExecuteReaderAsync())
        {
            while (await leitor.ReadAsync())
                nutricionistasLidos.Add((leitor.GetInt64(0), leitor.GetString(1)));
        }

        var nutricionistas = 0;
        foreach (var n in nutricionistasLidos)
        {
            if (!cripto.EhTextoPuro(n.Telefone)) continue;

            await using var atualizar = new NpgsqlCommand(
                "UPDATE usuarios SET telefone = @telefone WHERE id = @id", db);
            atualizar.Parameters.AddWithValue("telefone", cripto.Criptografar(n.Telefone)!);
            atualizar.Parameters.AddWithValue("id", n.Id);
            await atualizar.ExecuteNonQueryAsync();
            nutricionistas++;
        }

        if (pacientes > 0 || nutricionistas > 0)
            log.LogInformation(
                "RNF01 — backfill de criptografia: {Pacientes} paciente(s), {Nutricionistas} nutricionista(s).",
                pacientes, nutricionistas);
    }

    private static string? CifrarOuNulo(CriptografiaService cripto, string? valor) =>
        valor is null ? null : cripto.Criptografar(valor);
}
