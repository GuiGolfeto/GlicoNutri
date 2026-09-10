using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Security;
using GlicoNutri.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GlicoNutri.Api.Controllers;

/// <summary>
/// UC006 — Calcular Necessidade Energética. Ator é o Nutricionista: a RN14 veda
/// valores calóricos arbitrários, e a prescrição é ato do profissional.
/// </summary>
[ApiController]
[Route("api/pacientes/{pacienteId:long}/necessidade-energetica")]
[Authorize(Policy = Politicas.Nutricionista)]
public class NecessidadeEnergeticaController(
    INecessidadeEnergeticaService servico,
    IAcessoPacienteService acesso) : ControllerBase
{
    /// <summary>
    /// UC006, passo 6 — calcula e devolve TMB, fator e VET sem persistir, para o
    /// nutricionista conferir antes de confirmar no passo 7.
    /// </summary>
    [HttpPost("simular")]
    [ProducesResponseType<NecessidadeEnergeticaResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Simular(
        long pacienteId, CalcularVetRequest pedido, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var resultado = await servico.SimularAsync(pacienteId, pedido, ct);
        return resultado.Sucesso ? Ok(resultado.Dados) : BadRequest(new { mensagem = resultado.Mensagem });
    }

    /// <summary>UC006, passo 7 — confirma e grava o cálculo no prontuário.</summary>
    [HttpPost]
    [ProducesResponseType<NecessidadeEnergeticaResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Calcular(
        long pacienteId, CalcularVetRequest pedido, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var resultado = await servico.CalcularESalvarAsync(pacienteId, pedido, ct);
        if (!resultado.Sucesso) return BadRequest(new { mensagem = resultado.Mensagem });

        return CreatedAtAction(nameof(Historico), new { pacienteId }, resultado.Dados);
    }

    /// <summary>RN04 do UC006 — histórico versionado, para rastreabilidade clínica.</summary>
    [HttpGet]
    public async Task<IActionResult> Historico(long pacienteId, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();
        return Ok(await servico.HistoricoAsync(pacienteId, ct));
    }
}
