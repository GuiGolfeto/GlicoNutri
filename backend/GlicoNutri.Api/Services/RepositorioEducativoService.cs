using GlicoNutri.Api.Data;
using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GlicoNutri.Api.Services;

public interface IRepositorioEducativoService
{
    // RF09.2 — conteúdo educativo
    Task<Resultado<ConteudoEducativoResponse>> CriarConteudoAsync(
        long autorId, CriarConteudoRequest pedido, CancellationToken ct = default);

    Task<IReadOnlyList<ConteudoEducativoResponse>> ListarConteudosAsync(
        string? termo, long? tipoId, bool incluirInativos, CancellationToken ct = default);

    Task<ConteudoEducativoResponse?> ObterConteudoAsync(long id, CancellationToken ct = default);

    Task<Resultado<ConteudoEducativoResponse>> AtualizarConteudoAsync(
        long id, CriarConteudoRequest pedido, CancellationToken ct = default);

    Task<Resultado<bool>> DespublicarConteudoAsync(long id, CancellationToken ct = default);
    Task<Resultado<bool>> RepublicarConteudoAsync(long id, CancellationToken ct = default);

    // RF09.1 — receitas
    Task<Resultado<ReceitaResponse>> CriarReceitaAsync(
        long nutricionistaId, CriarReceitaRequest pedido, CancellationToken ct = default);

    Task<IReadOnlyList<ReceitaResponse>> ListarReceitasAsync(
        string? termo, string? ingrediente, bool incluirInativas, CancellationToken ct = default);

    Task<ReceitaResponse?> ObterReceitaAsync(long id, CancellationToken ct = default);

    Task<Resultado<ReceitaResponse>> AtualizarReceitaAsync(
        long id, CriarReceitaRequest pedido, CancellationToken ct = default);

    Task<Resultado<bool>> DespublicarReceitaAsync(long id, CancellationToken ct = default);
    Task<Resultado<bool>> RepublicarReceitaAsync(long id, CancellationToken ct = default);
}

/// <summary>
/// RF09 — Repositório Educativo. Publicação restrita a Nutricionista e
/// Administrador (RN28); despublicação exclusiva do Administrador, sempre por
/// inativação lógica (RN29). Receita carrega ingredientes vinculados a alimentos
/// cadastrados, para que os valores nutricionais sejam calculados (RN30).
/// </summary>
public class RepositorioEducativoService(GlicoNutriDbContext db) : IRepositorioEducativoService
{
    // ── Conteúdo educativo ──────────────────────────────────────────────────

    public async Task<Resultado<ConteudoEducativoResponse>> CriarConteudoAsync(
        long autorId, CriarConteudoRequest pedido, CancellationToken ct = default)
    {
        var tipo = await db.TiposConteudo.FirstOrDefaultAsync(t => t.Id == pedido.TipoId && t.Ativo, ct);
        if (tipo is null) return Resultado<ConteudoEducativoResponse>.Erro("Tipo de conteúdo inválido.");

        var conteudo = new ConteudoEducativo
        {
            AutorId = autorId,
            Titulo = pedido.Titulo.Trim(),
            TipoId = tipo.Id,
            Corpo = pedido.Corpo.Trim(),
            DataPublicacao = DateTime.UtcNow,
            Ativo = true,
        };

        db.ConteudosEducativos.Add(conteudo);
        await db.SaveChangesAsync(ct);

        return Resultado<ConteudoEducativoResponse>.Ok((await ObterConteudoAsync(conteudo.Id, ct))!);
    }

    public async Task<IReadOnlyList<ConteudoEducativoResponse>> ListarConteudosAsync(
        string? termo, long? tipoId, bool incluirInativos, CancellationToken ct = default)
    {
        var consulta = incluirInativos
            ? db.ConteudosEducativos.IgnoreQueryFilters()
            : db.ConteudosEducativos;

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = termo.Trim().ToLower();
            consulta = consulta.Where(c =>
                EF.Functions.Unaccent(c.Titulo.ToLower()).Contains(EF.Functions.Unaccent(t)));
        }

        if (tipoId is { } id) consulta = consulta.Where(c => c.TipoId == id);

        return await consulta
            .OrderByDescending(c => c.DataPublicacao)
            .Select(ProjecaoConteudo())
            .ToListAsync(ct);
    }

    public Task<ConteudoEducativoResponse?> ObterConteudoAsync(long id, CancellationToken ct = default) =>
        db.ConteudosEducativos.IgnoreQueryFilters()
            .Where(c => c.Id == id)
            .Select(ProjecaoConteudo())
            .FirstOrDefaultAsync(ct);

    public async Task<Resultado<ConteudoEducativoResponse>> AtualizarConteudoAsync(
        long id, CriarConteudoRequest pedido, CancellationToken ct = default)
    {
        var conteudo = await db.ConteudosEducativos.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (conteudo is null) return Resultado<ConteudoEducativoResponse>.Erro("Conteúdo não encontrado.");

        var tipo = await db.TiposConteudo.FirstOrDefaultAsync(t => t.Id == pedido.TipoId && t.Ativo, ct);
        if (tipo is null) return Resultado<ConteudoEducativoResponse>.Erro("Tipo de conteúdo inválido.");

        conteudo.Titulo = pedido.Titulo.Trim();
        conteudo.TipoId = tipo.Id;
        conteudo.Corpo = pedido.Corpo.Trim();

        await db.SaveChangesAsync(ct);
        return Resultado<ConteudoEducativoResponse>.Ok((await ObterConteudoAsync(id, ct))!);
    }

    /// <summary>RN29 — despublicar é inativar, preservando o conteúdo para reativação.</summary>
    public async Task<Resultado<bool>> DespublicarConteudoAsync(long id, CancellationToken ct = default)
    {
        var conteudo = await db.ConteudosEducativos.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (conteudo is null) return Resultado<bool>.Erro("Conteúdo não encontrado.");

        conteudo.Ativo = false;
        await db.SaveChangesAsync(ct);
        return Resultado<bool>.Ok(true);
    }

    public async Task<Resultado<bool>> RepublicarConteudoAsync(long id, CancellationToken ct = default)
    {
        var conteudo = await db.ConteudosEducativos.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (conteudo is null) return Resultado<bool>.Erro("Conteúdo não encontrado.");

        conteudo.Ativo = true;
        await db.SaveChangesAsync(ct);
        return Resultado<bool>.Ok(true);
    }

    // ── Receitas ────────────────────────────────────────────────────────────

    public async Task<Resultado<ReceitaResponse>> CriarReceitaAsync(
        long nutricionistaId, CriarReceitaRequest pedido, CancellationToken ct = default)
    {
        var erro = await ValidarIngredientesAsync(pedido.Ingredientes, ct);
        if (erro is not null) return Resultado<ReceitaResponse>.Erro(erro);

        var receita = new Receita
        {
            NutricionistaId = nutricionistaId,
            Nome = pedido.Nome.Trim(),
            Descricao = pedido.Descricao?.Trim(),
            TempoPreparo = pedido.TempoPreparo,
            Porcoes = pedido.Porcoes,
            Instrucoes = pedido.Instrucoes.Trim(),
            DataPublicacao = DateTime.UtcNow,
            Ativo = true,
        };

        foreach (var i in pedido.Ingredientes)
        {
            receita.Ingredientes.Add(new IngredienteReceita
            {
                AlimentoId = i.AlimentoId,
                Quantidade = i.Quantidade,
                Unidade = string.IsNullOrWhiteSpace(i.Unidade) ? "g" : i.Unidade.Trim(),
            });
        }

        db.Receitas.Add(receita);
        await db.SaveChangesAsync(ct);

        return Resultado<ReceitaResponse>.Ok((await ObterReceitaAsync(receita.Id, ct))!);
    }

    /// <summary>RF09.3 — o paciente busca receita por nome ou por ingrediente.</summary>
    public async Task<IReadOnlyList<ReceitaResponse>> ListarReceitasAsync(
        string? termo, string? ingrediente, bool incluirInativas, CancellationToken ct = default)
    {
        var consulta = incluirInativas ? db.Receitas.IgnoreQueryFilters() : db.Receitas;

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = termo.Trim().ToLower();
            consulta = consulta.Where(r =>
                EF.Functions.Unaccent(r.Nome.ToLower()).Contains(EF.Functions.Unaccent(t)));
        }

        if (!string.IsNullOrWhiteSpace(ingrediente))
        {
            var i = ingrediente.Trim().ToLower();
            consulta = consulta.Where(r => r.Ingredientes.Any(x =>
                EF.Functions.Unaccent(x.Alimento.Nome.ToLower()).Contains(EF.Functions.Unaccent(i))));
        }

        var ids = await consulta
            .OrderByDescending(r => r.DataPublicacao)
            .Select(r => r.Id)
            .ToListAsync(ct);

        var receitas = new List<ReceitaResponse>(ids.Count);
        foreach (var id in ids)
        {
            if (await ObterReceitaAsync(id, ct) is { } receita) receitas.Add(receita);
        }

        return receitas;
    }

    public async Task<ReceitaResponse?> ObterReceitaAsync(long id, CancellationToken ct = default)
    {
        var receita = await db.Receitas
            .IgnoreQueryFilters()
            .Include(r => r.Nutricionista)
            .Include(r => r.Ingredientes).ThenInclude(i => i.Alimento)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (receita is null) return null;

        var ingredientes = receita.Ingredientes.Select(i => new IngredienteResponse(
            i.Id, i.AlimentoId, i.Alimento.Nome, i.Quantidade, i.Unidade,
            CalculadoraNutricional.PorPorcao(i.Alimento.CaloriasPor100g, i.Quantidade),
            CalculadoraNutricional.PorPorcao(i.Alimento.CarboidratosPor100g, i.Quantidade),
            CalculadoraNutricional.PorPorcao(i.Alimento.ProteinasPor100g, i.Quantidade),
            CalculadoraNutricional.PorPorcao(i.Alimento.LipidiosPor100g, i.Quantidade)))
            .OrderBy(i => i.AlimentoNome)
            .ToList();

        // RN30 — os valores da receita saem da soma dos ingredientes. Um valor
        // ausente na base entra como zero na soma e o total sai subestimado, sem
        // que nada na tela denuncie. A flag cobre todos os campos que a resposta
        // apresenta, fibra inclusive: como a TACO traz "*" (não determinado) em
        // vários alimentos, é comum que ela seja falsa — e é justamente esse o
        // aviso que o nutricionista precisa ver antes de confiar no número.
        var completa = receita.Ingredientes.All(i =>
            i.Alimento.CaloriasPor100g is not null
            && i.Alimento.CarboidratosPor100g is not null
            && i.Alimento.ProteinasPor100g is not null
            && i.Alimento.LipidiosPor100g is not null
            && i.Alimento.FibrasPor100g is not null);

        double Somar(Func<Alimento, double?> campo) => Math.Round(
            receita.Ingredientes.Sum(i =>
                CalculadoraNutricional.PorPorcao(campo(i.Alimento), i.Quantidade) ?? 0), 2);

        var calorias = Somar(a => a.CaloriasPor100g);
        var carboidratos = Somar(a => a.CarboidratosPor100g);
        var porcoes = receita.Porcoes is > 0 ? receita.Porcoes.Value : (int?)null;

        var nutricional = new InformacaoNutricional(
            calorias, carboidratos,
            Somar(a => a.ProteinasPor100g),
            Somar(a => a.LipidiosPor100g),
            Somar(a => a.FibrasPor100g),
            porcoes is { } p ? Math.Round(calorias / p, 2) : null,
            porcoes is { } p2 ? Math.Round(carboidratos / p2, 2) : null,
            completa);

        return new ReceitaResponse(
            receita.Id, receita.Nome, receita.Descricao, receita.TempoPreparo, receita.Porcoes,
            receita.Instrucoes, receita.NutricionistaId, receita.Nutricionista.Nome,
            receita.DataPublicacao, receita.Ativo, ingredientes, nutricional);
    }

    public async Task<Resultado<ReceitaResponse>> AtualizarReceitaAsync(
        long id, CriarReceitaRequest pedido, CancellationToken ct = default)
    {
        var receita = await db.Receitas
            .Include(r => r.Ingredientes)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (receita is null) return Resultado<ReceitaResponse>.Erro("Receita não encontrada.");

        var erro = await ValidarIngredientesAsync(pedido.Ingredientes, ct);
        if (erro is not null) return Resultado<ReceitaResponse>.Erro(erro);

        receita.Nome = pedido.Nome.Trim();
        receita.Descricao = pedido.Descricao?.Trim();
        receita.TempoPreparo = pedido.TempoPreparo;
        receita.Porcoes = pedido.Porcoes;
        receita.Instrucoes = pedido.Instrucoes.Trim();

        // Ingredientes não têm soft delete no DER; a lista é substituída.
        db.IngredientesReceita.RemoveRange(receita.Ingredientes);
        receita.Ingredientes.Clear();

        foreach (var i in pedido.Ingredientes)
        {
            receita.Ingredientes.Add(new IngredienteReceita
            {
                AlimentoId = i.AlimentoId,
                Quantidade = i.Quantidade,
                Unidade = string.IsNullOrWhiteSpace(i.Unidade) ? "g" : i.Unidade.Trim(),
            });
        }

        await db.SaveChangesAsync(ct);
        return Resultado<ReceitaResponse>.Ok((await ObterReceitaAsync(id, ct))!);
    }

    public async Task<Resultado<bool>> DespublicarReceitaAsync(long id, CancellationToken ct = default)
    {
        var receita = await db.Receitas.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (receita is null) return Resultado<bool>.Erro("Receita não encontrada.");

        receita.Ativo = false;
        await db.SaveChangesAsync(ct);
        return Resultado<bool>.Ok(true);
    }

    public async Task<Resultado<bool>> RepublicarReceitaAsync(long id, CancellationToken ct = default)
    {
        var receita = await db.Receitas.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id, ct);
        if (receita is null) return Resultado<bool>.Erro("Receita não encontrada.");

        receita.Ativo = true;
        await db.SaveChangesAsync(ct);
        return Resultado<bool>.Ok(true);
    }

    /// <summary>
    /// RN30 — os ingredientes precisam apontar para alimentos existentes. Aceita
    /// alimento inativado: a RN12 manda preservar a referência histórica, e uma
    /// receita publicada não deve quebrar porque o alimento saiu da busca.
    /// </summary>
    private async Task<string?> ValidarIngredientesAsync(
        IReadOnlyList<IngredienteRequest> ingredientes, CancellationToken ct)
    {
        if (ingredientes.Count == 0)
            return "A receita precisa de ao menos um ingrediente.";

        var ids = ingredientes.Select(i => i.AlimentoId).Distinct().ToList();
        var encontrados = await db.Alimentos.Where(a => ids.Contains(a.Id)).Select(a => a.Id).ToListAsync(ct);
        var faltando = ids.Except(encontrados).ToList();

        return faltando.Count > 0
            ? $"Alimento não encontrado na base: {string.Join(", ", faltando)}."
            : null;
    }

    private static System.Linq.Expressions.Expression<Func<ConteudoEducativo, ConteudoEducativoResponse>>
        ProjecaoConteudo() =>
        c => new ConteudoEducativoResponse(
            c.Id, c.Titulo, c.TipoId, c.Tipo.Descricao, c.Corpo,
            c.AutorId, c.Autor.Nome, c.DataPublicacao, c.Ativo);
}
