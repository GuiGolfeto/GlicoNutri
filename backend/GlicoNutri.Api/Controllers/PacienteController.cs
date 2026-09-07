using GlicoNutri.Api.Data;
using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Security;
using GlicoNutri.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GlicoNutri.Api.Controllers;

/// <summary>
/// UC002 — Cadastrar Paciente. Perfil de Nutricionista ou Administrador (RN08).
/// Como o Administrador também carrega a role de Nutricionista no token (RN04),
/// a política de Nutricionista já o contempla.
/// </summary>
[ApiController]
[Route("api/pacientes")]
[Authorize(Policy = Politicas.Nutricionista)]
public class PacienteController(IPacienteService servico) : ControllerBase
{
    private bool EhAdministrador =>
        User.FindFirst(ClaimsGlicoNutri.Perfil)?.Value == Codigos.Perfil.Administrador;

    [HttpPost]
    [ProducesResponseType<PacienteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar(CriarPacienteRequest pedido, CancellationToken ct)
    {
        var autorId = User.ObterUsuarioId();
        if (autorId is null) return Unauthorized();

        var resultado = await servico.CriarAsync(pedido, autorId.Value, EhAdministrador, ct);
        if (!resultado.Sucesso)
            return BadRequest(new { mensagem = resultado.Mensagem });

        return CreatedAtAction(nameof(Buscar), new { id = resultado.Dados!.Id }, resultado.Dados);
    }

    /// <summary>
    /// RN33 — o Nutricionista lista somente os próprios pacientes. O Administrador
    /// enxerga todos, e pode filtrar por profissional responsável.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] long? nutricionistaId = null,
        [FromQuery] bool incluirInativos = false,
        CancellationToken ct = default)
    {
        var autorId = User.ObterUsuarioId();
        if (autorId is null) return Unauthorized();

        var filtro = EhAdministrador ? nutricionistaId : autorId;
        return Ok(await servico.ListarAsync(filtro, incluirInativos, ct));
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType<PacienteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Buscar(long id, CancellationToken ct)
    {
        var paciente = await servico.BuscarAsync(id, ct);
        if (paciente is null) return NotFound();

        // RN33 — dados clínicos só para o nutricionista responsável e o administrador.
        if (!EhAdministrador && paciente.NutricionistaId != User.ObterUsuarioId())
            return Forbid();

        return Ok(paciente);
    }

    /// <summary>RF10.5 — edição dos dados cadastrais do paciente.</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Atualizar(
        long id, AtualizarPacienteRequest pedido, CancellationToken ct)
    {
        var paciente = await servico.BuscarAsync(id, ct);
        if (paciente is null) return NotFound();

        // RN33 — só o responsável e o administrador editam o paciente.
        if (!EhAdministrador && paciente.NutricionistaId != User.ObterUsuarioId()) return Forbid();

        var resultado = await servico.AtualizarAsync(id, pedido, ct);
        return resultado.Sucesso ? Ok(resultado.Dados) : BadRequest(new { mensagem = resultado.Mensagem });
    }

    /// <summary>RN20 — define a faixa glicêmica alvo individual do paciente.</summary>
    [HttpPut("{id:long}/metas-glicemicas")]
    public async Task<IActionResult> DefinirMetasGlicemicas(
        long id, DefinirMetasGlicemicasRequest pedido, CancellationToken ct)
    {
        var resultado = await servico.DefinirMetasGlicemicasAsync(id, pedido, ct);
        return resultado.Sucesso ? Ok(resultado.Dados) : BadRequest(new { mensagem = resultado.Mensagem });
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Desativar(long id, CancellationToken ct)
    {
        var resultado = await servico.DesativarAsync(id, ct);
        return resultado.Sucesso ? NoContent() : BadRequest(new { mensagem = resultado.Mensagem });
    }

    [HttpPost("{id:long}/reativar")]
    public async Task<IActionResult> Reativar(long id, CancellationToken ct)
    {
        var resultado = await servico.ReativarAsync(id, ct);
        return resultado.Sucesso ? NoContent() : BadRequest(new { mensagem = resultado.Mensagem });
    }
}
