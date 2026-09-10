using GlicoNutri.Api.Security;
using GlicoNutri.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GlicoNutri.Api.Controllers;

/// <summary>
/// Relatórios clínicos exportáveis em PDF — o RelatorioController do C4 e o
/// RelatorioService do Diagrama de Classes V5.0.
/// </summary>
[ApiController]
[Route("api/pacientes/{pacienteId:long}/relatorios")]
[Authorize(Policy = Politicas.Nutricionista)]
public class RelatorioController(
    IRelatorioService servico,
    IAcessoPacienteService acesso) : ControllerBase
{
    [HttpGet("glicemico")]
    [Produces("application/pdf")]
    public Task<IActionResult> Glicemico(
        long pacienteId, [FromQuery] int dias = 30, CancellationToken ct = default) =>
        Responder(pacienteId, ct, () => servico.GerarRelatorioGlicemicoAsync(pacienteId, dias, ct));

    [HttpGet("antropometrico")]
    [Produces("application/pdf")]
    public Task<IActionResult> Antropometrico(
        long pacienteId, [FromQuery] int dias = 90, CancellationToken ct = default) =>
        Responder(pacienteId, ct, () => servico.GerarRelatorioAntropometricoAsync(pacienteId, dias, ct));

    [HttpGet("completo")]
    [Produces("application/pdf")]
    public Task<IActionResult> Completo(
        long pacienteId, [FromQuery] int dias = 90, CancellationToken ct = default) =>
        Responder(pacienteId, ct, () => servico.GerarRelatorioCompletoAsync(pacienteId, dias, ct));

    private async Task<IActionResult> Responder(
        long pacienteId, CancellationToken ct, Func<Task<Resultado<Relatorio>>> gerar)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var resultado = await gerar();
        if (!resultado.Sucesso) return BadRequest(new { mensagem = resultado.Mensagem });

        var relatorio = resultado.Dados!;
        return File(relatorio.Conteudo, "application/pdf", relatorio.NomeArquivo);
    }
}
