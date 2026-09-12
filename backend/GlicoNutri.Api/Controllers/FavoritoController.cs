using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GlicoNutri.Api.Controllers;

/// <summary>
/// RF09.3 — favoritos do paciente no repositório educativo.
///
/// O acesso é o mesmo das demais rotas por paciente (RN33): o próprio paciente
/// pelo app ou pela web, e o nutricionista responsável, que precisa enxergar o
/// que o paciente guardou para orientar a conduta.
/// </summary>
[ApiController]
[Route("api/pacientes/{pacienteId:long}/favoritos")]
[Authorize]
public class FavoritoController(
    IFavoritoService servico,
    IAcessoPacienteService acesso) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<FavoritosResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(long pacienteId, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();
        return Ok(await servico.ListarAsync(pacienteId, ct));
    }

    [HttpPut("conteudos/{conteudoId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarcarConteudo(long pacienteId, long conteudoId, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var resultado = await servico.MarcarConteudoAsync(pacienteId, conteudoId, ct);
        return resultado.Sucesso ? NoContent() : BadRequest(new { mensagem = resultado.Mensagem });
    }

    [HttpDelete("conteudos/{conteudoId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DesmarcarConteudo(long pacienteId, long conteudoId, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        await servico.DesmarcarConteudoAsync(pacienteId, conteudoId, ct);
        return NoContent();
    }

    [HttpPut("receitas/{receitaId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarcarReceita(long pacienteId, long receitaId, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var resultado = await servico.MarcarReceitaAsync(pacienteId, receitaId, ct);
        return resultado.Sucesso ? NoContent() : BadRequest(new { mensagem = resultado.Mensagem });
    }

    [HttpDelete("receitas/{receitaId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DesmarcarReceita(long pacienteId, long receitaId, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        await servico.DesmarcarReceitaAsync(pacienteId, receitaId, ct);
        return NoContent();
    }
}
