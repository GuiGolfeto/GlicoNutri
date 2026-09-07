using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Security;
using GlicoNutri.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GlicoNutri.Api.Controllers;

/// <summary>
/// UC012 — Registrar Emoção. O registro é do paciente; o diário e a análise de
/// correlação ficam visíveis ao nutricionista responsável (RN04 do UC012).
/// </summary>
[ApiController]
[Route("api/pacientes/{pacienteId:long}/emocoes")]
[Authorize]
public class RegistroEmocionalController(
    IRegistroEmocionalService servico,
    IAcessoPacienteService acesso) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<IReadOnlyList<RegistroEmocionalResponse>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Registrar(
        long pacienteId, CriarRegistroEmocionalRequest pedido, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var resultado = await servico.RegistrarAsync(pacienteId, pedido, ct);
        if (!resultado.Sucesso) return BadRequest(new { mensagem = resultado.Mensagem });

        return CreatedAtAction(nameof(Diario), new { pacienteId }, resultado.Dados);
    }

    [HttpGet]
    public async Task<IActionResult> Diario(
        long pacienteId, [FromQuery] int dias = 30, CancellationToken ct = default)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var resultado = await servico.DiarioAsync(pacienteId, dias, ct);
        return resultado.Sucesso ? Ok(resultado.Dados) : BadRequest(new { mensagem = resultado.Mensagem });
    }

    /// <summary>
    /// RF06.2 — análise da correlação entre emoções e variações glicêmicas.
    /// Exclusiva do painel do nutricionista.
    /// </summary>
    [HttpGet("correlacao")]
    [Authorize(Policy = Politicas.Nutricionista)]
    public async Task<IActionResult> Correlacao(
        long pacienteId, [FromQuery] int dias = 30, CancellationToken ct = default)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var resultado = await servico.CorrelacaoAsync(pacienteId, dias, ct);
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
