using System.Text;
using GlicoNutri.Api.Services;

namespace GlicoNutri.Tests.Unidade;

/// <summary>
/// RN10 — as particularidades do arquivo da TACO. Cada caso aqui é um jeito
/// diferente de o importador silenciosamente gravar dado errado.
/// </summary>
public class LeitorCsvTacoTestes
{
    // ── Marcadores da tabela ────────────────────────────────────────────────

    [Theory]
    [InlineData("Tr")]
    [InlineData("tr")]
    [InlineData("TRAÇO")]
    [InlineData("traco")]
    public void TentarNumero_Traco_ViraZero(string bruto)
    {
        // Traço é quantidade desprezível porém presente.
        Assert.True(LeitorCsvTaco.TentarNumero(bruto, out var valor));
        Assert.Equal(0, valor);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("NA")]
    [InlineData("n/a")]
    [InlineData("ND")]
    [InlineData("-")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TentarNumero_NaoDeterminado_ViraNuloENaoZero(string? bruto)
    {
        // Gravar zero num valor não medido mentiria na conta do plano alimentar.
        Assert.True(LeitorCsvTaco.TentarNumero(bruto, out var valor));
        Assert.Null(valor);
    }

    [Theory]
    [InlineData("123,5", 123.5)]
    [InlineData("123.5", 123.5)]
    [InlineData("1.234,5", 1234.5)]   // milhar com ponto, decimal com vírgula
    [InlineData("0", 0)]
    [InlineData("884", 884)]
    public void TentarNumero_AceitaOsFormatosNumericosQueAparecemNoArquivo(
        string bruto, double esperado)
    {
        Assert.True(LeitorCsvTaco.TentarNumero(bruto, out var valor));
        Assert.Equal(esperado, valor);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12,3,4")]
    [InlineData("R$ 10")]
    public void TentarNumero_LixoNaoNumerico_Falha(string bruto)
    {
        // É o que manda a linha para o relatório de erros da RN10.
        Assert.False(LeitorCsvTaco.TentarNumero(bruto, out _));
    }

    // ── Codificação ─────────────────────────────────────────────────────────

    [Fact]
    public void DecodificarTexto_Utf8_PreservaAcentos()
    {
        var bytes = Encoding.UTF8.GetBytes("Feijão, carioca, cozido");
        Assert.Equal("Feijão, carioca, cozido", LeitorCsvTaco.DecodificarTexto(bytes));
    }

    [Fact]
    public void DecodificarTexto_Windows1252_CaiNoFallbackEPreservaAcentos()
    {
        // Exports da TACO pelo Excel saem em cp1252; lê-los como UTF-8
        // corromperia o nome, que é a chave de duplicidade da RN11.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var bytes = Encoding.GetEncoding(1252).GetBytes("Pão, francês · Óleo · Maçã");

        Assert.Equal("Pão, francês · Óleo · Maçã", LeitorCsvTaco.DecodificarTexto(bytes));
    }

    // ── Delimitador ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Alimento;Energia;Proteina\nArroz;123;2,6", ";")]
    [InlineData("Alimento,Energia,Proteina\nArroz,123,2.6", ",")]
    [InlineData("Alimento\tEnergia\tProteina", "\t")]
    public void DetectarDelimitador_ReconhecePelaPrimeiraLinha(string conteudo, string esperado) =>
        Assert.Equal(esperado, LeitorCsvTaco.DetectarDelimitador(conteudo));

    [Fact]
    public void DetectarDelimitador_NomesComVirgulaNaoConfundemOPontoEVirgula()
    {
        // "Arroz, integral, cozido" tem vírgulas dentro do próprio nome.
        var conteudo = "Descrição do Alimento;Grupo;Energia (kcal)\nArroz, integral, cozido;Cereais;123,5";
        Assert.Equal(";", LeitorCsvTaco.DetectarDelimitador(conteudo));
    }

    // ── Mapeamento de colunas ───────────────────────────────────────────────

    [Fact]
    public void MapearColunas_ReconheceOsCabecalhosDaPlanilhaOficial()
    {
        string[] cabecalho =
        [
            "Descrição do Alimento", "Grupo", "Energia (kcal)",
            "Proteína (g)", "Lipídeos (g)", "Carboidrato (g)", "Fibra Alimentar (g)",
        ];

        var mapa = LeitorCsvTaco.MapearColunas(cabecalho);

        Assert.Equal(0, mapa["nome"]);
        Assert.Equal(1, mapa["grupo"]);
        Assert.Equal(2, mapa["calorias"]);
        Assert.Equal(3, mapa["proteinas"]);
        Assert.Equal(4, mapa["lipidios"]);
        Assert.Equal(5, mapa["carboidratos"]);
        Assert.Equal(6, mapa["fibras"]);
    }

    [Fact]
    public void MapearColunas_AceitaVariacoesDeTituloEmCirculacao()
    {
        string[] cabecalho = ["Alimento", "Categoria", "kcal", "PTN", "LIP", "CHO", "Fibra"];
        var mapa = LeitorCsvTaco.MapearColunas(cabecalho);

        Assert.Equal(7, mapa.Count);
        Assert.Equal(0, mapa["nome"]);
        Assert.Equal(2, mapa["calorias"]);
    }

    [Fact]
    public void MapearColunas_IgnoraColunasDesconhecidas()
    {
        string[] cabecalho = ["Número", "Descrição do Alimento", "Umidade (%)", "Energia (kcal)", "Cinzas (g)"];
        var mapa = LeitorCsvTaco.MapearColunas(cabecalho);

        Assert.Equal(1, mapa["nome"]);
        Assert.Equal(3, mapa["calorias"]);
        Assert.False(mapa.ContainsKey("proteinas"));
    }

    [Fact]
    public void MapearColunas_SemColunaDeNome_NaoInventaMapeamento()
    {
        // Sem o nome do alimento a importação inteira é recusada.
        var mapa = LeitorCsvTaco.MapearColunas(["Umidade (%)", "Cinzas (g)"]);
        Assert.False(mapa.ContainsKey("nome"));
    }

    [Fact]
    public void SemAcento_NormalizaParaComparacao()
    {
        Assert.Equal("Feijao", LeitorCsvTaco.SemAcento("Feijão"));
        Assert.Equal("Macaa", LeitorCsvTaco.SemAcento("Maçãa").Replace("c", "c"));
        Assert.Equal("Oleo", LeitorCsvTaco.SemAcento("Óleo"));
        Assert.Equal("Pao, frances", LeitorCsvTaco.SemAcento("Pão, francês"));
    }
}
