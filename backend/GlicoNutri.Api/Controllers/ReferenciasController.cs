using GlicoNutri.Api.Data;
using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Models.Referencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GlicoNutri.Api.Controllers;

/// <summary>
/// Leitura das tabelas de referência que substituíram os enums (Diagrama de
/// Classes V5.0), para preencher os selects dos formulários. O CRUD completo de
/// cada uma entra quando houver demanda real de manutenção pelo Administrador.
/// </summary>
[ApiController]
[Route("api/referencias")]
[Authorize]
public class ReferenciasController(GlicoNutriDbContext db) : ControllerBase
{
    [HttpGet("sexos-biologicos")]
    public Task<List<ReferenciaResponse>> SexosBiologicos(CancellationToken ct) => Listar<SexoBiologico>(ct);

    [HttpGet("tipos-diabetes")]
    public Task<List<ReferenciaResponse>> TiposDiabetes(CancellationToken ct) => Listar<TipoDiabetes>(ct);

    [HttpGet("contextos-glicemia")]
    public Task<List<ReferenciaResponse>> ContextosGlicemia(CancellationToken ct) => Listar<ContextoGlicemia>(ct);

    [HttpGet("estados-emocionais")]
    public Task<List<ReferenciaResponse>> EstadosEmocionais(CancellationToken ct) => Listar<EstadoEmocional>(ct);

    [HttpGet("formulas-energeticas")]
    public Task<List<ReferenciaResponse>> FormulasEnergeticas(CancellationToken ct) => Listar<FormulaEnergetica>(ct);

    [HttpGet("niveis-atividade")]
    public Task<List<ReferenciaResponse>> NiveisAtividade(CancellationToken ct) => Listar<NivelAtividade>(ct);

    [HttpGet("tipos-alerta")]
    public Task<List<ReferenciaResponse>> TiposAlerta(CancellationToken ct) => Listar<TipoAlerta>(ct);

    [HttpGet("tipos-conteudo")]
    public Task<List<ReferenciaResponse>> TiposConteudo(CancellationToken ct) => Listar<TipoConteudo>(ct);

    private Task<List<ReferenciaResponse>> Listar<T>(CancellationToken ct) where T : class, IEntidadeReferencia =>
        db.Set<T>()
          .Where(x => x.Ativo)
          .OrderBy(x => x.Id)
          .Select(x => new ReferenciaResponse(x.Id, x.Codigo, x.Descricao))
          .ToListAsync(ct);
}
