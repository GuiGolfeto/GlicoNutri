using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GlicoNutri.Api.Controllers;

/// <summary>
/// UC004 — Registrar Glicemia e UC005 — Visualizar Histórico Glicêmico.
/// O registro é ato do paciente pelo app; a consulta é dele e do nutricionista
/// responsável (RN01 e RN02 do UC005), decidido por paciente pela RN33.
/// </summary>
[ApiController]
[Route("api/pacientes/{pacienteId:long}/glicemia")]
[Authorize]
public class GlicemiaController(
    IGlicemiaService servico,
    IAcessoPacienteService acesso) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<RegistroGlicemiaResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Registrar(
        long pacienteId, CriarRegistroGlicemiaRequest pedido, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var resultado = await servico.RegistrarAsync(pacienteId, pedido, ct);
        if (!resultado.Sucesso) return BadRequest(new { mensagem = resultado.Mensagem });

        return CreatedAtAction(nameof(Historico), new { pacienteId }, resultado.Dados);
    }

    /// <summary>RN04 do UC005 — últimos 7 dias por padrão.</summary>
    [HttpGet]
    public async Task<IActionResult> Historico(
        long pacienteId, [FromQuery] int dias = 7, CancellationToken ct = default)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var resultado = await servico.HistoricoAsync(pacienteId, dias, ct);
        return resultado.Sucesso ? Ok(resultado.Dados) : BadRequest(new { mensagem = resultado.Mensagem });
    }

    /// <summary>RF08.1 — série do gráfico de evolução glicêmica.</summary>
    [HttpGet("serie")]
    public async Task<IActionResult> Serie(
        long pacienteId, [FromQuery] int dias = 30, CancellationToken ct = default)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var resultado = await servico.SerieAsync(pacienteId, dias, ct);
        return resultado.Sucesso ? Ok(resultado.Dados) : BadRequest(new { mensagem = resultado.Mensagem });
    }

    [HttpDelete("{registroId:long}")]
    public async Task<IActionResult> Remover(long pacienteId, long registroId, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var resultado = await servico.RemoverAsync(registroId, ct);
        return resultado.Sucesso ? NoContent() : BadRequest(new { mensagem = resultado.Mensagem });
    }
}
