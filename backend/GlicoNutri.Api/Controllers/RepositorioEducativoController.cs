using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Security;
using GlicoNutri.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GlicoNutri.Api.Controllers;

/// <summary>
/// RF09.2 — Conteúdo educativo.
/// RN28 — criar e editar são de Nutricionista ou Administrador; o paciente tem
/// permissão exclusiva de leitura. RN29 — despublicar é só do Administrador.
/// </summary>
[ApiController]
[Route("api/conteudos")]
[Authorize]
public class ConteudoEducativoController(IRepositorioEducativoService servico) : ControllerBase
{
    /// <summary>RF09.3 — leitura liberada a todos os perfis autenticados.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? termo = null,
        [FromQuery] long? tipoId = null,
        [FromQuery] bool incluirInativos = false,
        CancellationToken ct = default)
    {
        // Só quem publica enxerga o que foi despublicado.
        var ehPaciente = User.FindFirst(ClaimsGlicoNutri.Perfil)?.Value == Data.Codigos.Perfil.Paciente;
        return Ok(await servico.ListarConteudosAsync(termo, tipoId, incluirInativos && !ehPaciente, ct));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obter(long id, CancellationToken ct)
    {
        var conteudo = await servico.ObterConteudoAsync(id, ct);
        if (conteudo is null) return NotFound();

        var ehPaciente = User.FindFirst(ClaimsGlicoNutri.Perfil)?.Value == Data.Codigos.Perfil.Paciente;
        if (!conteudo.Ativo && ehPaciente) return NotFound();

        return Ok(conteudo);
    }

    [HttpPost]
    [Authorize(Policy = Politicas.Nutricionista)]
    public async Task<IActionResult> Criar(CriarConteudoRequest pedido, CancellationToken ct)
    {
        var autor = User.ObterUsuarioId();
        if (autor is null) return Unauthorized();

        var resultado = await servico.CriarConteudoAsync(autor.Value, pedido, ct);
        if (!resultado.Sucesso) return BadRequest(new { mensagem = resultado.Mensagem });

        return CreatedAtAction(nameof(Obter), new { id = resultado.Dados!.Id }, resultado.Dados);
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = Politicas.Nutricionista)]
    public async Task<IActionResult> Atualizar(long id, CriarConteudoRequest pedido, CancellationToken ct)
    {
        var resultado = await servico.AtualizarConteudoAsync(id, pedido, ct);
        return resultado.Sucesso ? Ok(resultado.Dados) : BadRequest(new { mensagem = resultado.Mensagem });
    }

    /// <summary>RN29 — despublicação é competência exclusiva do Administrador.</summary>
    [HttpDelete("{id:long}")]
    [Authorize(Policy = Politicas.Administrador)]
    public async Task<IActionResult> Despublicar(long id, CancellationToken ct)
    {
        var resultado = await servico.DespublicarConteudoAsync(id, ct);
        return resultado.Sucesso ? NoContent() : BadRequest(new { mensagem = resultado.Mensagem });
    }

    [HttpPost("{id:long}/republicar")]
    [Authorize(Policy = Politicas.Administrador)]
    public async Task<IActionResult> Republicar(long id, CancellationToken ct)
    {
        var resultado = await servico.RepublicarConteudoAsync(id, ct);
        return resultado.Sucesso ? NoContent() : BadRequest(new { mensagem = resultado.Mensagem });
    }
}

/// <summary>
/// RF09.1 — Receitas. Cadastro pelo Nutricionista (RN28); despublicação
/// exclusiva do Administrador (RN29).
/// </summary>
[ApiController]
[Route("api/receitas")]
[Authorize]
public class ReceitaController(IRepositorioEducativoService servico) : ControllerBase
{
    /// <summary>RF09.3 — o paciente busca por nome ou por ingrediente.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? termo = null,
        [FromQuery] string? ingrediente = null,
        [FromQuery] bool incluirInativas = false,
        CancellationToken ct = default)
    {
        var ehPaciente = User.FindFirst(ClaimsGlicoNutri.Perfil)?.Value == Data.Codigos.Perfil.Paciente;
        return Ok(await servico.ListarReceitasAsync(termo, ingrediente, incluirInativas && !ehPaciente, ct));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obter(long id, CancellationToken ct)
    {
        var receita = await servico.ObterReceitaAsync(id, ct);
        if (receita is null) return NotFound();

        var ehPaciente = User.FindFirst(ClaimsGlicoNutri.Perfil)?.Value == Data.Codigos.Perfil.Paciente;
        if (!receita.Ativo && ehPaciente) return NotFound();

        return Ok(receita);
    }

    [HttpPost]
    [Authorize(Policy = Politicas.Nutricionista)]
    public async Task<IActionResult> Criar(CriarReceitaRequest pedido, CancellationToken ct)
    {
        var autor = User.ObterUsuarioId();
        if (autor is null) return Unauthorized();

        var resultado = await servico.CriarReceitaAsync(autor.Value, pedido, ct);
        if (!resultado.Sucesso) return BadRequest(new { mensagem = resultado.Mensagem });

        return CreatedAtAction(nameof(Obter), new { id = resultado.Dados!.Id }, resultado.Dados);
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = Politicas.Nutricionista)]
    public async Task<IActionResult> Atualizar(long id, CriarReceitaRequest pedido, CancellationToken ct)
    {
        var resultado = await servico.AtualizarReceitaAsync(id, pedido, ct);
        return resultado.Sucesso ? Ok(resultado.Dados) : BadRequest(new { mensagem = resultado.Mensagem });
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = Politicas.Administrador)]
    public async Task<IActionResult> Despublicar(long id, CancellationToken ct)
    {
        var resultado = await servico.DespublicarReceitaAsync(id, ct);
        return resultado.Sucesso ? NoContent() : BadRequest(new { mensagem = resultado.Mensagem });
    }

    [HttpPost("{id:long}/republicar")]
    [Authorize(Policy = Politicas.Administrador)]
    public async Task<IActionResult> Republicar(long id, CancellationToken ct)
    {
        var resultado = await servico.RepublicarReceitaAsync(id, ct);
        return resultado.Sucesso ? NoContent() : BadRequest(new { mensagem = resultado.Mensagem });
    }
}
