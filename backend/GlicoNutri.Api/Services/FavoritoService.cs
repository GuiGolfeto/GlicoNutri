using GlicoNutri.Api.Data;
using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GlicoNutri.Api.Services;

public interface IFavoritoService
{
    Task<FavoritosResponse> ListarAsync(long pacienteId, CancellationToken ct = default);

    Task<Resultado<bool>> MarcarConteudoAsync(long pacienteId, long conteudoId, CancellationToken ct = default);
    Task<Resultado<bool>> DesmarcarConteudoAsync(long pacienteId, long conteudoId, CancellationToken ct = default);

    Task<Resultado<bool>> MarcarReceitaAsync(long pacienteId, long receitaId, CancellationToken ct = default);
    Task<Resultado<bool>> DesmarcarReceitaAsync(long pacienteId, long receitaId, CancellationToken ct = default);
}

/// <summary>
/// RF09.3 — favoritos do paciente no repositório educativo.
///
/// Marcar duas vezes o mesmo material não é erro: a chave é o par paciente +
/// material, e a segunda marcação não tem efeito. Isso deixa a tela livre para
/// tratar o botão como um interruptor, sem precisar saber o estado anterior.
/// </summary>
public class FavoritoService(GlicoNutriDbContext db) : IFavoritoService
{
    public async Task<FavoritosResponse> ListarAsync(long pacienteId, CancellationToken ct = default)
    {
        // O filtro global já exclui material despublicado (RN29): o favorito
        // continua gravado, mas some da lista enquanto o conteúdo estiver fora.
        var conteudos = await db.FavoritosConteudo
            .Where(f => f.PacienteId == pacienteId)
            .OrderByDescending(f => f.DataFavoritado)
            .Select(f => new FavoritoConteudoResponse(
                f.ConteudoId, f.Conteudo.Titulo, f.Conteudo.Tipo.Descricao,
                f.Conteudo.Corpo, f.Conteudo.UrlMidia, f.Conteudo.Autor.Nome,
                f.Conteudo.DataPublicacao, f.DataFavoritado))
            .ToListAsync(ct);

        var receitas = await db.FavoritosReceita
            .Where(f => f.PacienteId == pacienteId)
            .OrderByDescending(f => f.DataFavoritado)
            .Select(f => new FavoritoReceitaResponse(
                f.ReceitaId, f.Receita.Nome, f.Receita.Descricao, f.Receita.TempoPreparo,
                f.Receita.Porcoes,
                // Mesma conta da tela de receitas: o total dividido pelas porções.
                f.Receita.Porcoes == null || f.Receita.Porcoes == 0
                    ? null
                    : f.Receita.Ingredientes
                        .Where(i => i.Alimento.CaloriasPor100g != null)
                        .Sum(i => i.Alimento.CaloriasPor100g!.Value * i.Quantidade / 100)
                      / f.Receita.Porcoes,
                f.DataFavoritado))
            .ToListAsync(ct);

        return new FavoritosResponse(conteudos, receitas);
    }

    public async Task<Resultado<bool>> MarcarConteudoAsync(
        long pacienteId, long conteudoId, CancellationToken ct = default)
    {
        if (!await db.ConteudosEducativos.AnyAsync(c => c.Id == conteudoId, ct))
            return Resultado<bool>.Erro("Conteúdo não encontrado.");

        if (await db.FavoritosConteudo.IgnoreQueryFilters()
                .AnyAsync(f => f.PacienteId == pacienteId && f.ConteudoId == conteudoId, ct))
            return Resultado<bool>.Ok(true);

        db.FavoritosConteudo.Add(new FavoritoConteudo
        {
            PacienteId = pacienteId,
            ConteudoId = conteudoId,
            DataFavoritado = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(ct);
        return Resultado<bool>.Ok(true);
    }

    public async Task<Resultado<bool>> DesmarcarConteudoAsync(
        long pacienteId, long conteudoId, CancellationToken ct = default)
    {
        var favorito = await db.FavoritosConteudo.IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.PacienteId == pacienteId && f.ConteudoId == conteudoId, ct);

        if (favorito is null) return Resultado<bool>.Ok(true);

        // Favorito é preferência do paciente, não histórico clínico: sai da tabela
        // de verdade, sem inativação lógica.
        db.FavoritosConteudo.Remove(favorito);
        await db.SaveChangesAsync(ct);
        return Resultado<bool>.Ok(true);
    }

    public async Task<Resultado<bool>> MarcarReceitaAsync(
        long pacienteId, long receitaId, CancellationToken ct = default)
    {
        if (!await db.Receitas.AnyAsync(r => r.Id == receitaId, ct))
            return Resultado<bool>.Erro("Receita não encontrada.");

        if (await db.FavoritosReceita.IgnoreQueryFilters()
                .AnyAsync(f => f.PacienteId == pacienteId && f.ReceitaId == receitaId, ct))
            return Resultado<bool>.Ok(true);

        db.FavoritosReceita.Add(new FavoritoReceita
        {
            PacienteId = pacienteId,
            ReceitaId = receitaId,
            DataFavoritado = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(ct);
        return Resultado<bool>.Ok(true);
    }

    public async Task<Resultado<bool>> DesmarcarReceitaAsync(
        long pacienteId, long receitaId, CancellationToken ct = default)
    {
        var favorito = await db.FavoritosReceita.IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.PacienteId == pacienteId && f.ReceitaId == receitaId, ct);

        if (favorito is null) return Resultado<bool>.Ok(true);

        db.FavoritosReceita.Remove(favorito);
        await db.SaveChangesAsync(ct);
        return Resultado<bool>.Ok(true);
    }
}
