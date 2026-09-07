using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Security;
using GlicoNutri.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GlicoNutri.Api.Controllers;

/// <summary>
/// UC009 — Cadastrar Alimento. Nutricionista ou Administrador; o Administrador
/// já é contemplado pela política de Nutricionista por herança (RN04).
/// </summary>
[ApiController]
[Route("api/alimentos")]
[Authorize(Policy = Politicas.Nutricionista)]
public class AlimentoController(
    IAlimentoService servico,
    IImportadorTacoService importador) : ControllerBase
{
    /// <summary>Tamanho máximo do CSV aceito na importação.</summary>
    private const long TamanhoMaximoBytes = 10 * 1024 * 1024;

    /// <summary>RN12 — a busca não devolve alimentos inativados.</summary>
    [HttpGet]
    public async Task<IActionResult> Buscar(
        [FromQuery] string? termo = null,
        [FromQuery] string? grupo = null,
        [FromQuery] int limite = 50,
        CancellationToken ct = default) =>
        Ok(await servico.BuscarAsync(termo, grupo, limite, ct));

    [HttpGet("grupos")]
    public async Task<IActionResult> Grupos(CancellationToken ct) =>
        Ok(await servico.GruposAsync(ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obter(long id, CancellationToken ct)
    {
        var alimento = await servico.ObterAsync(id, ct);
        return alimento is null ? NotFound() : Ok(alimento);
    }

    /// <summary>RF01.1 — cadastro manual.</summary>
    [HttpPost]
    [ProducesResponseType<AlimentoResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(CriarAlimentoRequest pedido, CancellationToken ct)
    {
        var resultado = await servico.CriarAsync(pedido, ct);
        if (!resultado.Sucesso) return BadRequest(new { mensagem = resultado.Mensagem });

        return CreatedAtAction(nameof(Obter), new { id = resultado.Dados!.Id }, resultado.Dados);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Atualizar(long id, CriarAlimentoRequest pedido, CancellationToken ct)
    {
        var resultado = await servico.AtualizarAsync(id, pedido, ct);
        return resultado.Sucesso ? Ok(resultado.Dados) : BadRequest(new { mensagem = resultado.Mensagem });
    }

    /// <summary>RN12 — inativação lógica; o alimento continua resolvível pelos planos.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Inativar(long id, CancellationToken ct)
    {
        var resultado = await servico.InativarAsync(id, ct);
        return resultado.Sucesso ? NoContent() : BadRequest(new { mensagem = resultado.Mensagem });
    }

    /// <summary>
    /// RF01.2 / RN10 — importação em lote via CSV no padrão TACO/IBGE. Devolve o
    /// relatório de erros junto com a contagem do que entrou.
    /// </summary>
    [HttpPost("importar")]
    [ProducesResponseType<ResultadoImportacao>(StatusCodes.Status200OK)]
    [RequestSizeLimit(TamanhoMaximoBytes)]
    public async Task<IActionResult> Importar(IFormFile arquivo, CancellationToken ct)
    {
        if (arquivo is null || arquivo.Length == 0)
            return BadRequest(new { mensagem = "Envie um arquivo CSV." });

        if (arquivo.Length > TamanhoMaximoBytes)
            return BadRequest(new { mensagem = "Arquivo maior que 10 MB." });

        // RN10 — o formato aceito é exclusivamente CSV.
        var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        if (extensao is not (".csv" or ".txt"))
            return BadRequest(new
            {
                mensagem = "A importação aceita apenas arquivos CSV. " +
                           "Se a tabela estiver em XLS ou XLSX, exporte como CSV antes de enviar.",
            });

        await using var fluxo = arquivo.OpenReadStream();
        var resultado = await importador.ImportarAsync(fluxo, ct);

        return resultado.Sucesso ? Ok(resultado.Dados) : BadRequest(new { mensagem = resultado.Mensagem });
    }
}
