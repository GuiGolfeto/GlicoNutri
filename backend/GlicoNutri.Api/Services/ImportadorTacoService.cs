using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using GlicoNutri.Api.Data;
using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GlicoNutri.Api.Services;

public interface IImportadorTacoService
{
    Task<Resultado<ResultadoImportacao>> ImportarAsync(
        Stream arquivo, CancellationToken ct = default);
}

/// <summary>
/// RF01.2 / RN10 — importação em lote da tabela TACO/IBGE. Valida linha a linha
/// antes de persistir e devolve relatório de erros sem interromper as linhas
/// válidas.
/// </summary>
public class ImportadorTacoService(GlicoNutriDbContext db, ILogger<ImportadorTacoService> log)
    : IImportadorTacoService
{
    /// <summary>Teto por arquivo, para uma planilha errada não travar a API.</summary>
    private const int LimiteLinhas = 5000;

    public async Task<Resultado<ResultadoImportacao>> ImportarAsync(
        Stream arquivo, CancellationToken ct = default)
    {
        using var memoria = new MemoryStream();
        await arquivo.CopyToAsync(memoria, ct);

        var conteudo = LeitorCsvTaco.DecodificarTexto(memoria.ToArray());
        if (string.IsNullOrWhiteSpace(conteudo))
            return Resultado<ResultadoImportacao>.Erro("Arquivo vazio.");

        var delimitador = LeitorCsvTaco.DetectarDelimitador(conteudo);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimitador,
            HasHeaderRecord = true,
            // A TACO tem linhas de seção e rodapés com contagem de colunas
            // diferente; abortar nelas jogaria fora o arquivo inteiro.
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true,
        };

        using var leitor = new StringReader(conteudo);
        using var csv = new CsvReader(leitor, config);

        if (!await csv.ReadAsync() || !csv.ReadHeader())
            return Resultado<ResultadoImportacao>.Erro("Não foi possível ler o cabeçalho do arquivo.");

        var mapa = LeitorCsvTaco.MapearColunas(csv.HeaderRecord ?? []);

        if (!mapa.ContainsKey("nome"))
            return Resultado<ResultadoImportacao>.Erro(
                "O arquivo não tem uma coluna reconhecível com o nome do alimento. " +
                "Esperado um cabeçalho como \"Descrição do Alimento\".");

        // Os nomes já existentes entram em memória de uma vez: consultar o banco
        // por linha custaria centenas de idas e vindas (RN11).
        var existentes = (await db.Alimentos
                .Select(a => a.Nome)
                .ToListAsync(ct))
            .Select(n => n.ToLowerInvariant())
            .ToHashSet();

        var taco = await db.FontesAlimento.FirstAsync(f => f.Codigo == Codigos.Fonte.Taco, ct);

        var erros = new List<LinhaRejeitada>();
        var novos = new List<Alimento>();
        var lidas = 0;
        var duplicados = 0;
        var numeroLinha = 1;

        // Preenchido pelas linhas de título de seção, para os arquivos que não
        // trazem uma coluna de grupo própria.
        string? grupoCorrente = null;

        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            numeroLinha++;

            if (lidas >= LimiteLinhas)
            {
                erros.Add(new LinhaRejeitada(numeroLinha, string.Empty,
                    $"Importação interrompida no limite de {LimiteLinhas} linhas por arquivo."));
                break;
            }

            var bruto = csv.Parser.RawRecord.Trim();
            if (bruto.Length == 0) continue;

            lidas++;

            var nome = Campo(csv, mapa, "nome")?.Trim();

            if (string.IsNullOrWhiteSpace(nome))
            {
                lidas--;
                continue;
            }

            if (!TentarCampoNumerico(csv, mapa, "calorias", out var calorias, out var erro) ||
                !TentarCampoNumerico(csv, mapa, "carboidratos", out var carboidratos, out erro) ||
                !TentarCampoNumerico(csv, mapa, "proteinas", out var proteinas, out erro) ||
                !TentarCampoNumerico(csv, mapa, "lipidios", out var lipidios, out erro) ||
                !TentarCampoNumerico(csv, mapa, "fibras", out var fibras, out erro))
            {
                erros.Add(new LinhaRejeitada(numeroLinha, Resumir(bruto), erro!));
                continue;
            }

            // A planilha oficial da TACO separa os alimentos por seções, e o
            // título da seção ocupa a própria coluna de descrição — não uma linha
            // em branco. Uma linha com nome mas sem nenhum valor nutricional é
            // esse título: não é alimento nem erro, é a estrutura do arquivo.
            // Guardado como grupo corrente, cobre os arquivos sem coluna de grupo.
            var semNenhumValor = calorias is null && carboidratos is null
                              && proteinas is null && lipidios is null;

            if (semNenhumValor)
            {
                grupoCorrente = nome;
                lidas--;
                continue;
            }

            var grupoDaLinha = Campo(csv, mapa, "grupo")?.Trim();
            if (string.IsNullOrWhiteSpace(grupoDaLinha)) grupoDaLinha = grupoCorrente;

            // RN11 — duplicata não é importada; vai para o relatório, e cabe ao
            // Administrador decidir por atualizar o registro existente.
            var chave = nome.ToLowerInvariant();
            if (!existentes.Add(chave))
            {
                duplicados++;
                erros.Add(new LinhaRejeitada(numeroLinha, Resumir(bruto),
                    "Alimento já cadastrado; linha ignorada para não duplicar o registro."));
                continue;
            }

            if (nome.Length > 200)
            {
                erros.Add(new LinhaRejeitada(numeroLinha, Resumir(bruto),
                    "Nome do alimento excede 200 caracteres."));
                continue;
            }

            novos.Add(new Alimento
            {
                Nome = nome,
                GrupoAlimentar = Truncar(grupoDaLinha, 100),
                CaloriasPor100g = calorias,
                CarboidratosPor100g = carboidratos,
                ProteinasPor100g = proteinas,
                LipidiosPor100g = lipidios,
                FibrasPor100g = fibras,
                // A TACO não publica índice glicêmico. Fica nulo de propósito, em
                // vez de receber um valor arbitrário — num sistema de diabetes,
                // um IG inventado é pior do que a ausência dele.
                IndiceGlicemico = null,
                FonteId = taco.Id,
                Ativo = true,
            });
        }

        if (novos.Count > 0)
        {
            db.Alimentos.AddRange(novos);
            await db.SaveChangesAsync(ct);
        }

        log.LogInformation(
            "Importação TACO: {Lidas} linhas, {Importados} importados, {Duplicados} duplicados, {Rejeitados} rejeitados.",
            lidas, novos.Count, duplicados, erros.Count - duplicados);

        return Resultado<ResultadoImportacao>.Ok(new ResultadoImportacao(
            lidas, novos.Count, duplicados, erros.Count - duplicados, erros));
    }

    private static string? Campo(CsvReader csv, Dictionary<string, int> mapa, string campo) =>
        mapa.TryGetValue(campo, out var i) && csv.TryGetField<string>(i, out var v) ? v : null;

    private static bool TentarCampoNumerico(
        CsvReader csv, Dictionary<string, int> mapa, string campo, out double? valor, out string? erro)
    {
        erro = null;
        var bruto = Campo(csv, mapa, campo);

        if (LeitorCsvTaco.TentarNumero(bruto, out valor)) return true;

        erro = $"Valor inválido na coluna \"{campo}\": \"{bruto}\".";
        return false;
    }

    private static string Resumir(string linha) =>
        linha.Length <= 120 ? linha : linha[..120] + "…";

    private static string? Truncar(string? texto, int max) =>
        texto is null ? null : texto.Length <= max ? texto : texto[..max];
}
