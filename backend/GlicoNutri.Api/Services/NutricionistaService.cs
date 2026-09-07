using GlicoNutri.Api.Data;
using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Models;
using GlicoNutri.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GlicoNutri.Api.Services;

public record Resultado<T>(bool Sucesso, T? Dados = default, string? Mensagem = null)
{
    public static Resultado<T> Ok(T dados) => new(true, dados);
    public static Resultado<T> Erro(string mensagem) => new(false, Mensagem: mensagem);
}

public interface INutricionistaService
{
    Task<Resultado<NutricionistaResponse>> CriarAsync(CriarNutricionistaRequest pedido, CancellationToken ct = default);
    Task<IReadOnlyList<NutricionistaResponse>> ListarAsync(bool incluirInativos, CancellationToken ct = default);
    Task<NutricionistaResponse?> BuscarAsync(long id, CancellationToken ct = default);
    /// <summary>RF10.5 — o Administrador edita os dados do nutricionista.</summary>
    Task<Resultado<NutricionistaResponse>> AtualizarAsync(
        long id, AtualizarNutricionistaRequest pedido, CancellationToken ct = default);

    Task<Resultado<bool>> DesativarAsync(long id, CancellationToken ct = default);
    Task<Resultado<bool>> ReativarAsync(long id, CancellationToken ct = default);
}

/// <summary>
/// UC003 — Cadastrar Nutricionista. Ação exclusiva do Administrador (RN09).
/// </summary>
public class NutricionistaService(
    GlicoNutriDbContext db,
    IUsuarioRepository usuarios,
    ISenhaService senhas,
    IEmailService emails) : INutricionistaService
{
    public async Task<Resultado<NutricionistaResponse>> CriarAsync(
        CriarNutricionistaRequest pedido, CancellationToken ct = default)
    {
        var email = pedido.Email.Trim().ToLowerInvariant();
        var crn = pedido.Crn.Trim().ToUpperInvariant();

        // UC003 A2 — duplicidade de e-mail e de CRN.
        if (await usuarios.EmailEmUsoAsync(email, ct))
            return Resultado<NutricionistaResponse>.Erro("Já existe um usuário cadastrado com este e-mail.");

        if (await db.Nutricionistas.IgnoreQueryFilters().AnyAsync(n => n.Crn == crn, ct))
            return Resultado<NutricionistaResponse>.Erro("Já existe um nutricionista cadastrado com este CRN.");

        var perfil = await db.PerfisUsuario.FirstAsync(p => p.Codigo == Codigos.Perfil.Nutricionista, ct);

        // RN03 e UC003 passo 10 — senha provisória gerada pelo sistema, trocada
        // obrigatoriamente no primeiro acesso.
        var senhaProvisoria = senhas.GerarProvisoria();

        var nutricionista = new Nutricionista
        {
            Nome = pedido.Nome.Trim(),
            Email = email,
            SenhaHash = senhas.Hash(senhaProvisoria),
            PerfilId = perfil.Id,
            Crn = crn,
            Especialidade = pedido.Especialidade?.Trim(),
            Telefone = pedido.Telefone?.Trim(),
            Ativo = true,
            DataCadastro = DateTime.UtcNow,
            SenhaProvisoria = true,
        };

        db.Nutricionistas.Add(nutricionista);
        await db.SaveChangesAsync(ct);

        // UC003 A3 — e-mail de boas-vindas com o link de primeiro acesso.
        await emails.EnviarBoasVindasAsync(nutricionista.Email, nutricionista.Nome, senhaProvisoria, ct);

        return Resultado<NutricionistaResponse>.Ok(Mapear(nutricionista, 0));
    }

    public async Task<IReadOnlyList<NutricionistaResponse>> ListarAsync(
        bool incluirInativos, CancellationToken ct = default)
    {
        var consulta = incluirInativos
            ? db.Nutricionistas.IgnoreQueryFilters()
            : db.Nutricionistas;

        return await consulta
            .OrderBy(n => n.Nome)
            .Select(n => new NutricionistaResponse(
                n.Id, n.Nome, n.Email, n.Crn, n.Especialidade, n.Telefone,
                n.Ativo, n.SenhaProvisoria, n.DataCadastro,
                n.Vinculos.Count(v => v.Ativo)))
            .ToListAsync(ct);
    }

    public async Task<NutricionistaResponse?> BuscarAsync(long id, CancellationToken ct = default) =>
        await db.Nutricionistas
            .Where(n => n.Id == id)
            .Select(n => new NutricionistaResponse(
                n.Id, n.Nome, n.Email, n.Crn, n.Especialidade, n.Telefone,
                n.Ativo, n.SenhaProvisoria, n.DataCadastro,
                n.Vinculos.Count(v => v.Ativo)))
            .FirstOrDefaultAsync(ct);

    public async Task<Resultado<NutricionistaResponse>> AtualizarAsync(
        long id, AtualizarNutricionistaRequest pedido, CancellationToken ct = default)
    {
        var nutricionista = await db.Nutricionistas.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (nutricionista is null)
            return Resultado<NutricionistaResponse>.Erro("Nutricionista não encontrado.");

        var email = pedido.Email.Trim().ToLowerInvariant();
        var crn = pedido.Crn.Trim().ToUpperInvariant();

        // RN06 e RN09 — e-mail e CRN seguem únicos após a edição. A checagem
        // ignora o soft delete: conta desativada continua ocupando os dois.
        if (await db.Usuarios.IgnoreQueryFilters()
                .AnyAsync(u => u.Id != id && u.Email == email, ct))
            return Resultado<NutricionistaResponse>.Erro(
                "Já existe outro usuário cadastrado com este e-mail.");

        if (await db.Nutricionistas.IgnoreQueryFilters()
                .AnyAsync(n => n.Id != id && n.Crn == crn, ct))
            return Resultado<NutricionistaResponse>.Erro(
                "Já existe outro nutricionista cadastrado com este CRN.");

        nutricionista.Nome = pedido.Nome.Trim();
        nutricionista.Email = email;
        nutricionista.Crn = crn;
        nutricionista.Especialidade = pedido.Especialidade?.Trim();
        nutricionista.Telefone = pedido.Telefone?.Trim();

        await db.SaveChangesAsync(ct);
        return Resultado<NutricionistaResponse>.Ok((await BuscarAsync(id, ct))!);
    }

    /// <summary>
    /// RN05 — desativação é soft delete: nenhum dado clínico é apagado.
    /// A RN07 exige nutricionista responsável ativo, então um profissional com
    /// pacientes vinculados só sai depois da transferência.
    /// </summary>
    public async Task<Resultado<bool>> DesativarAsync(long id, CancellationToken ct = default)
    {
        var nutricionista = await db.Nutricionistas
            .Include(n => n.Vinculos)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (nutricionista is null)
            return Resultado<bool>.Erro("Nutricionista não encontrado.");

        var pacientesAtivos = nutricionista.Vinculos.Count(v => v.Ativo);
        if (pacientesAtivos > 0)
            return Resultado<bool>.Erro(
                $"Este nutricionista possui {pacientesAtivos} paciente(s) vinculado(s). " +
                "Transfira os pacientes para outro profissional antes de desativá-lo.");

        nutricionista.Ativo = false;
        await db.SaveChangesAsync(ct);
        return Resultado<bool>.Ok(true);
    }

    /// <summary>RN05 — a reativação restaura o acesso a todos os dados anteriores.</summary>
    public async Task<Resultado<bool>> ReativarAsync(long id, CancellationToken ct = default)
    {
        var nutricionista = await db.Nutricionistas
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (nutricionista is null)
            return Resultado<bool>.Erro("Nutricionista não encontrado.");

        nutricionista.Ativo = true;
        await db.SaveChangesAsync(ct);
        return Resultado<bool>.Ok(true);
    }

    private static NutricionistaResponse Mapear(Nutricionista n, int totalPacientes) =>
        new(n.Id, n.Nome, n.Email, n.Crn, n.Especialidade, n.Telefone,
            n.Ativo, n.SenhaProvisoria, n.DataCadastro, totalPacientes);
}
