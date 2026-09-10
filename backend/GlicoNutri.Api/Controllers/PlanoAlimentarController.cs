using GlicoNutri.Api.Data;
using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Security;
using GlicoNutri.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GlicoNutri.Api.Controllers;

/// <summary>
/// UC007 — Elaborar Plano Alimentar. A elaboração é ato do Nutricionista; a
/// leitura do plano vigente é liberada também ao paciente, que o consulta pelo
/// aplicativo.
/// </summary>
[ApiController]
[Route("api/pacientes/{pacienteId:long}/plano-alimentar")]
[Authorize]
public class PlanoAlimentarController(
    IPlanoAlimentarService servico,
    IAcessoPacienteService acesso) : ControllerBase
{
    private bool EhPaciente =>
        User.FindFirst(ClaimsGlicoNutri.Perfil)?.Value == Codigos.Perfil.Paciente;

    /// <summary>Plano vigente do paciente. Visível ao paciente e ao responsável (RN33).</summary>
    [HttpGet]
    public async Task<IActionResult> Ativo(long pacienteId, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var plano = await servico.ObterAtivoAsync(pacienteId, ct);
        return plano is null ? NoContent() : Ok(plano);
    }

    /// <summary>RN17 — histórico completo de prescrições, incluindo as inativadas.</summary>
    [HttpGet("historico")]
    public async Task<IActionResult> Historico(long pacienteId, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();
        return Ok(await servico.HistoricoAsync(pacienteId, ct));
    }

    /// <summary>
    /// UC007 A2 — prévia da distribuição de macronutrientes sobre o VET vigente,
    /// antes de o plano existir. Sem corpo, devolve a distribuição automática.
    /// </summary>
    [HttpPost("distribuicao")]
    [Authorize(Policy = Politicas.Nutricionista)]
    public async Task<IActionResult> Distribuicao(
        long pacienteId, [FromBody] DistribuicaoRequest? ajuste, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var resultado = await servico.SugerirDistribuicaoAsync(pacienteId, ajuste, ct);
        return resultado.Sucesso ? Ok(resultado.Dados) : BadRequest(new { mensagem = resultado.Mensagem });
    }

    [HttpPost]
    [Authorize(Policy = Politicas.Nutricionista)]
    [ProducesResponseType<PlanoAlimentarResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(long pacienteId, CriarPlanoRequest pedido, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();
        if (EhPaciente) return Forbid();

        var autor = User.ObterUsuarioId();
        if (autor is null) return Unauthorized();

        var resultado = await servico.CriarAsync(pacienteId, autor.Value, pedido, ct);
        if (!resultado.Sucesso) return BadRequest(new { mensagem = resultado.Mensagem });

        return CreatedAtAction(nameof(Ativo), new { pacienteId }, resultado.Dados);
    }

    /// <summary>UC007 A3 — duplica um plano anterior como base do novo.</summary>
    [HttpPost("{planoId:long}/duplicar")]
    [Authorize(Policy = Politicas.Nutricionista)]
    public async Task<IActionResult> Duplicar(
        long pacienteId, long planoId, [FromQuery] DateOnly? dataInicio, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var autor = User.ObterUsuarioId();
        if (autor is null) return Unauthorized();

        var resultado = await servico.DuplicarAsync(
            planoId, autor.Value, dataInicio ?? DateOnly.FromDateTime(DateTime.UtcNow), ct);

        return resultado.Sucesso ? Ok(resultado.Dados) : BadRequest(new { mensagem = resultado.Mensagem });
    }

    /// <summary>RN17 — retoma um plano do histórico, inativando o vigente.</summary>
    [HttpPost("{planoId:long}/reativar")]
    [Authorize(Policy = Politicas.Nutricionista)]
    public async Task<IActionResult> Reativar(long pacienteId, long planoId, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var resultado = await servico.ReativarAsync(planoId, ct);
        return resultado.Sucesso ? Ok(resultado.Dados) : BadRequest(new { mensagem = resultado.Mensagem });
    }

    [HttpDelete("{planoId:long}")]
    [Authorize(Policy = Politicas.Nutricionista)]
    public async Task<IActionResult> Inativar(long pacienteId, long planoId, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var resultado = await servico.InativarAsync(planoId, ct);
        return resultado.Sucesso ? NoContent() : BadRequest(new { mensagem = resultado.Mensagem });
    }
}
