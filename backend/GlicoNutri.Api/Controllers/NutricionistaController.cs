using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Security;
using GlicoNutri.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GlicoNutri.Api.Controllers;

/// <summary>
/// UC003 — Cadastrar Nutricionista. Todo o controller exige perfil de
/// Administrador: a RN09 veda que um Nutricionista crie contas de outros.
/// </summary>
[ApiController]
[Route("api/nutricionistas")]
[Authorize(Policy = Politicas.Administrador)]
public class NutricionistaController(INutricionistaService servico) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<NutricionistaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar(CriarNutricionistaRequest pedido, CancellationToken ct)
    {
        var resultado = await servico.CriarAsync(pedido, ct);
        if (!resultado.Sucesso)
            return BadRequest(new { mensagem = resultado.Mensagem });

        return CreatedAtAction(nameof(Buscar), new { id = resultado.Dados!.Id }, resultado.Dados);
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] bool incluirInativos = false, CancellationToken ct = default) =>
        Ok(await servico.ListarAsync(incluirInativos, ct));

    [HttpGet("{id:long}")]
    [ProducesResponseType<NutricionistaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Buscar(long id, CancellationToken ct)
    {
        var nutricionista = await servico.BuscarAsync(id, ct);
        return nutricionista is null ? NotFound() : Ok(nutricionista);
    }

    /// <summary>RF10.5 — desativa sem excluir o histórico (RN05).</summary>
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
