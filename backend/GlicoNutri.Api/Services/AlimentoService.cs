using GlicoNutri.Api.Data;
using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GlicoNutri.Api.Services;

public interface IAlimentoService
{
    Task<Resultado<AlimentoResponse>> CriarAsync(CriarAlimentoRequest pedido, CancellationToken ct = default);
    Task<IReadOnlyList<AlimentoResponse>> BuscarAsync(string? termo, string? grupo, int limite, CancellationToken ct = default);
    Task<AlimentoResponse?> ObterAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GruposAsync(CancellationToken ct = default);
    Task<Resultado<AlimentoResponse>> AtualizarAsync(long id, CriarAlimentoRequest pedido, CancellationToken ct = default);
    Task<Resultado<bool>> InativarAsync(long id, CancellationToken ct = default);
}

/// <summary>
/// UC009 — Cadastrar Alimento. Cadastro manual (RF01.1), busca e inativação
/// lógica (RN12).
/// </summary>
public class AlimentoService(GlicoNutriDbContext db) : IAlimentoService
{
    public async Task<Resultado<AlimentoResponse>> CriarAsync(
        CriarAlimentoRequest pedido, CancellationToken ct = default)
    {
        var nome = pedido.Nome.Trim();

        // RN11 — nome duplicado é bloqueado. A checagem ignora o soft delete: um
        // alimento inativado continua ocupando o nome, senão a reativação
        // colidiria com o novo registro.
        if (await db.Alimentos.AnyAsync(a => a.Nome.ToLower() == nome.ToLower(), ct))
            return Resultado<AlimentoResponse>.Erro("Já existe um alimento cadastrado com este nome.");

        var manual = await db.FontesAlimento.FirstAsync(f => f.Codigo == Codigos.Fonte.Manual, ct);

        var alimento = new Alimento
        {
            Nome = nome,
            GrupoAlimentar = pedido.GrupoAlimentar?.Trim(),
            CaloriasPor100g = pedido.CaloriasPor100g,
            CarboidratosPor100g = pedido.CarboidratosPor100g,
            ProteinasPor100g = pedido.ProteinasPor100g,
            LipidiosPor100g = pedido.LipidiosPor100g,
            FibrasPor100g = pedido.FibrasPor100g,
            IndiceGlicemico = pedido.IndiceGlicemico,
            FonteIndiceGlicemicoId = pedido.IndiceGlicemico is null ? null : Codigos.IdFonteIg.Profissional,
            FonteId = manual.Id,
            Ativo = true,
        };

        db.Alimentos.Add(alimento);
        await db.SaveChangesAsync(ct);

        return Resultado<AlimentoResponse>.Ok((await ObterAsync(alimento.Id, ct))!);
    }

    /// <summary>
    /// RN12 — alimentos inativados não aparecem nas buscas, mas continuam
    /// legíveis pelos planos que os referenciam. Por isso o filtro de "ativo"
    /// mora aqui, e não como filtro global do contexto.
    /// </summary>
    public async Task<IReadOnlyList<AlimentoResponse>> BuscarAsync(
        string? termo, string? grupo, int limite, CancellationToken ct = default)
    {
        var consulta = db.Alimentos.Where(a => a.Ativo);

        if (!string.IsNullOrWhiteSpace(termo))
        {
            // unaccent dos dois lados: o termo digitado e o nome armazenado. É o
            // que faz "feijao" encontrar "Feijão" e "acucar" encontrar "Açúcar".
            var t = termo.Trim().ToLower();
            consulta = consulta.Where(a =>
                EF.Functions.Unaccent(a.Nome.ToLower()).Contains(EF.Functions.Unaccent(t)));
        }

        if (!string.IsNullOrWhiteSpace(grupo))
            consulta = consulta.Where(a => a.GrupoAlimentar == grupo);

        return await consulta
            .OrderBy(a => a.Nome)
            .Take(Math.Clamp(limite, 1, 200))
            .Select(Projecao())
            .ToListAsync(ct);
    }

    /// <summary>Busca por id sem filtrar inativos: planos antigos precisam resolver o alimento (RN12).</summary>
    public Task<AlimentoResponse?> ObterAsync(long id, CancellationToken ct = default) =>
        db.Alimentos.Where(a => a.Id == id).Select(Projecao()).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<string>> GruposAsync(CancellationToken ct = default) =>
        await db.Alimentos
            .Where(a => a.Ativo && a.GrupoAlimentar != null)
            .Select(a => a.GrupoAlimentar!)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync(ct);

    public async Task<Resultado<AlimentoResponse>> AtualizarAsync(
        long id, CriarAlimentoRequest pedido, CancellationToken ct = default)
    {
        var alimento = await db.Alimentos.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (alimento is null) return Resultado<AlimentoResponse>.Erro("Alimento não encontrado.");

        var nome = pedido.Nome.Trim();
        if (await db.Alimentos.AnyAsync(a => a.Id != id && a.Nome.ToLower() == nome.ToLower(), ct))
            return Resultado<AlimentoResponse>.Erro("Já existe outro alimento com este nome.");

        alimento.Nome = nome;
        alimento.GrupoAlimentar = pedido.GrupoAlimentar?.Trim();
        alimento.CaloriasPor100g = pedido.CaloriasPor100g;
        alimento.CarboidratosPor100g = pedido.CarboidratosPor100g;
        alimento.ProteinasPor100g = pedido.ProteinasPor100g;
        alimento.LipidiosPor100g = pedido.LipidiosPor100g;
        alimento.FibrasPor100g = pedido.FibrasPor100g;
        // Quem sobrescreve o índice glicêmico assume a autoria do número: o valor
        // deixa de constar como vindo da tabela internacional.
        if (alimento.IndiceGlicemico != pedido.IndiceGlicemico)
        {
            alimento.IndiceGlicemico = pedido.IndiceGlicemico;
            alimento.FonteIndiceGlicemicoId =
                pedido.IndiceGlicemico is null ? null : Codigos.IdFonteIg.Profissional;
        }

        await db.SaveChangesAsync(ct);
        return Resultado<AlimentoResponse>.Ok((await ObterAsync(id, ct))!);
    }

    /// <summary>RN12 — remoção é sempre inativação lógica, nunca exclusão física.</summary>
    public async Task<Resultado<bool>> InativarAsync(long id, CancellationToken ct = default)
    {
        var alimento = await db.Alimentos.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (alimento is null) return Resultado<bool>.Erro("Alimento não encontrado.");

        alimento.Ativo = false;
        await db.SaveChangesAsync(ct);
        return Resultado<bool>.Ok(true);
    }

    private static System.Linq.Expressions.Expression<Func<Alimento, AlimentoResponse>> Projecao() =>
        a => new AlimentoResponse(
            a.Id, a.Nome, a.GrupoAlimentar,
            a.CaloriasPor100g, a.CarboidratosPor100g, a.ProteinasPor100g,
            a.LipidiosPor100g, a.FibrasPor100g, a.IndiceGlicemico,
            a.FonteIndiceGlicemico == null ? null : a.FonteIndiceGlicemico.Descricao,
            a.Fonte.Codigo, a.Ativo);
}
