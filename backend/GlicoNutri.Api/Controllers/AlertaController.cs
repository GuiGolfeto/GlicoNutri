using GlicoNutri.Api.Data;
using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Security;
using GlicoNutri.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GlicoNutri.Api.Controllers;

/// <summary>
/// UC010 — Configurar Alertas e Lembretes.
/// RN23 — criar, editar e excluir são exclusivos do Nutricionista ou do
/// Administrador; ao paciente cabe apenas desativar pelo aplicativo.
/// </summary>
[ApiController]
[Route("api/pacientes/{pacienteId:long}/alertas")]
[Authorize]
public class AlertaController(
    IAlertaService servico,
    IAcessoPacienteService acesso) : ControllerBase
{
    private bool EhPaciente =>
        User.FindFirst(ClaimsGlicoNutri.Perfil)?.Value == Codigos.Perfil.Paciente;

    [HttpGet]
    public async Task<IActionResult> Listar(
        long pacienteId, [FromQuery] bool incluirInativos = false, CancellationToken ct = default)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();
        return Ok(await servico.ListarAsync(pacienteId, incluirInativos, ct));
    }

    [HttpPost]
    [Authorize(Policy = Politicas.Nutricionista)]
    public async Task<IActionResult> Criar(
        long pacienteId, CriarAlertaRequest pedido, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var resultado = await servico.CriarAsync(pacienteId, pedido, ct);
        if (!resultado.Sucesso) return BadRequest(new { mensagem = resultado.Mensagem });

        return CreatedAtAction(nameof(Listar), new { pacienteId }, resultado.Dados);
    }

    [HttpPut("{alertaId:long}")]
    [Authorize(Policy = Politicas.Nutricionista)]
    public async Task<IActionResult> Atualizar(
        long pacienteId, long alertaId, CriarAlertaRequest pedido, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var resultado = await servico.AtualizarAsync(alertaId, pedido, ct);
        return resultado.Sucesso ? Ok(resultado.Dados) : BadRequest(new { mensagem = resultado.Mensagem });
    }

    /// <summary>RN23 — desativar é a única ação do módulo permitida ao paciente.</summary>
    [HttpDelete("{alertaId:long}")]
    public async Task<IActionResult> Desativar(long pacienteId, long alertaId, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var resultado = await servico.DesativarAsync(alertaId, ct);
        return resultado.Sucesso ? NoContent() : BadRequest(new { mensagem = resultado.Mensagem });
    }

    [HttpPost("{alertaId:long}/reativar")]
    [Authorize(Policy = Politicas.Nutricionista)]
    public async Task<IActionResult> Reativar(long pacienteId, long alertaId, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var resultado = await servico.ReativarAsync(alertaId, ct);
        return resultado.Sucesso ? NoContent() : BadRequest(new { mensagem = resultado.Mensagem });
    }

    /// <summary>
    /// RN27 — o aplicativo informa cada disparo e seu desfecho. As notificações
    /// são locais, agendadas no próprio dispositivo, sem serviço externo.
    /// </summary>
    [HttpPost("disparos")]
    public async Task<IActionResult> RegistrarDisparo(
        long pacienteId, RegistrarDisparoRequest pedido, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();

        var resultado = await servico.RegistrarDisparoAsync(pacienteId, pedido, ct);
        return resultado.Sucesso ? Ok(resultado.Dados) : BadRequest(new { mensagem = resultado.Mensagem });
    }

    [HttpGet("disparos")]
    public async Task<IActionResult> Historico(
        long pacienteId, [FromQuery] int dias = 30, CancellationToken ct = default)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();
        return Ok(await servico.HistoricoAsync(pacienteId, dias, ct));
    }

    /// <summary>
    /// RN35 — varredura de expiração, chamada pelo aplicativo ao iniciar sessão.
    /// </summary>
    [HttpPost("verificar-expirados")]
    public async Task<IActionResult> VerificarExpirados(long pacienteId, CancellationToken ct)
    {
        if (!await acesso.PodeAcessarAsync(User, pacienteId, ct)) return Forbid();
        return Ok(await servico.VerificarAlertasExpiradosAsync(pacienteId, ct));
    }
}
