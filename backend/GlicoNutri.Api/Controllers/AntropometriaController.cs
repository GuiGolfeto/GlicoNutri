using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GlicoNutri.Api.Controllers;

/// <summary>
/// UC008 — Registrar Dados Antropométricos.
/// Aberto a qualquer perfil autenticado, com a permissão decidida por paciente
/// (RN33): o Nutricionista responsável registra pela web, e o próprio paciente
/// registra pelo app, como pede o RF04.1.
/// </summary>
[ApiController]
[Route("api/pacientes/{pacienteId:long}/antropometria")]
[Authorize]
public class AntropometriaController(
    IAntropometriaService servico,
    IAcessoPacienteService acesso) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<RegistroAntropometricoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Registrar(
        long pacienteId, CriarRegistroAntropometricoRequest pedido, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var resultado = await servico.RegistrarAsync(pacienteId, pedido, ct);
        if (!resultado.Sucesso) return BadRequest(new { mensagem = resultado.Mensagem });

        return CreatedAtAction(nameof(Historico), new { pacienteId }, resultado.Dados);
    }

    /// <summary>RF04.2 — histórico cronológico das medidas.</summary>
    [HttpGet]
    public async Task<IActionResult> Historico(long pacienteId, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();
        return Ok(await servico.HistoricoAsync(pacienteId, ct));
    }

    /// <summary>RF08.2 — série de peso e IMC para o gráfico.</summary>
    [HttpGet("serie")]
    public async Task<IActionResult> Serie(
        long pacienteId, [FromQuery] int dias = 90, CancellationToken ct = default)
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
