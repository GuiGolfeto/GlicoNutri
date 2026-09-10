using System.Globalization;
using System.Text;

namespace GlicoNutri.Api.Services;

/// <summary>Uma linha da tabela nutricional já traduzida para os campos do sistema.</summary>
public record LinhaAlimento(
    string Nome,
    string? Grupo,
    double? Calorias,
    double? Carboidratos,
    double? Proteinas,
    double? Lipidios,
    double? Fibras);

/// <summary>
/// Leitura do CSV da TACO/IBGE. Isolado do serviço de importação porque é aqui
/// que moram as particularidades do arquivo — e são muitas.
/// </summary>
public static class LeitorCsvTaco
{
    /// <summary>
    /// Cabeçalhos aceitos para cada campo, em minúsculas e sem acento. A TACO
    /// circula em versões com títulos diferentes (planilha oficial, exports de
    /// terceiros, traduções), então o mapeamento é por nome e tolerante.
    /// </summary>
    private static readonly Dictionary<string, string[]> Sinonimos = new()
    {
        ["nome"] = ["descricao do alimento", "descricao", "alimento", "nome", "food"],
        ["grupo"] = ["grupo", "grupo alimentar", "categoria"],
        ["calorias"] = ["energia kcal", "energia (kcal)", "energia", "kcal", "calorias", "valor energetico"],
        ["carboidratos"] = ["carboidrato g", "carboidrato (g)", "carboidrato", "carboidratos", "cho"],
        ["proteinas"] = ["proteina g", "proteina (g)", "proteina", "proteinas", "ptn"],
        ["lipidios"] = ["lipideos g", "lipideos (g)", "lipideos", "lipidios", "lipidio", "gordura", "gorduras totais", "lip"],
        ["fibras"] = ["fibra alimentar g", "fibra alimentar (g)", "fibra alimentar", "fibra", "fibras"],
    };

    /// <summary>
    /// A TACO usa marcadores no lugar de números:
    ///   Tr = traço, quantidade desprezível porém presente → 0
    ///   *  = não determinado  |  NA = não aplicável → nulo, e não zero.
    /// Gravar zero num valor não medido seria mentir na conta do plano alimentar.
    /// </summary>
    private static readonly string[] Traco = ["tr", "traco", "traço"];
    private static readonly string[] NaoDisponivel = ["*", "na", "n/a", "-", "nd"];

    /// <summary>
    /// Exports da TACO costumam vir em Windows-1252, não em UTF-8. Ler tudo como
    /// UTF-8 transformaria "Manteiga" em "Manteiga" — e o nome é a chave de
    /// duplicidade da RN11, então o encoding errado gera importação duplicada.
    /// </summary>
    public static string DecodificarTexto(byte[] bytes)
    {
        var estrito = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        try
        {
            return estrito.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1252).GetString(bytes);
        }
    }

    /// <summary>Detecta o separador pela primeira linha: CSV brasileiro usa ";".</summary>
    public static string DetectarDelimitador(string conteudo)
    {
        var primeira = conteudo.Split('\n').FirstOrDefault() ?? string.Empty;
        var pontoVirgula = primeira.Count(c => c == ';');
        var virgula = primeira.Count(c => c == ',');
        var tab = primeira.Count(c => c == '\t');

        if (tab > pontoVirgula && tab > virgula) return "\t";
        return pontoVirgula >= virgula ? ";" : ",";
    }

    /// <summary>
    /// Converte um valor numérico da tabela. Aceita vírgula ou ponto decimal e
    /// devolve <c>false</c> apenas quando o texto não é número nem marcador
    /// conhecido — é o que faz a linha entrar no relatório de erros da RN10.
    /// </summary>
    public static bool TentarNumero(string? bruto, out double? valor)
    {
        valor = null;
        if (string.IsNullOrWhiteSpace(bruto)) return true;

        var texto = bruto.Trim();
        var comparavel = SemAcento(texto).ToLowerInvariant();

        if (NaoDisponivel.Contains(comparavel)) return true;
        if (Traco.Contains(comparavel)) { valor = 0; return true; }

        // Milhar com ponto e decimal com vírgula ("1.234,5") aparece em exports
        // gerados pelo Excel em português.
        var normalizado = texto.Contains(',')
            ? texto.Replace(".", string.Empty).Replace(',', '.')
            : texto;

        if (double.TryParse(normalizado, NumberStyles.Float, CultureInfo.InvariantCulture, out var n))
        {
            valor = n;
            return true;
        }

        return false;
    }

    /// <summary>Casa um cabeçalho do arquivo com um campo do sistema.</summary>
    public static Dictionary<string, int> MapearColunas(string[] cabecalho)
    {
        var mapa = new Dictionary<string, int>();

        for (var i = 0; i < cabecalho.Length; i++)
        {
            var titulo = SemAcento(cabecalho[i] ?? string.Empty).Trim().ToLowerInvariant();
            if (titulo.Length == 0) continue;

            foreach (var (campo, aceitos) in Sinonimos)
            {
                if (mapa.ContainsKey(campo)) continue;
                if (aceitos.Contains(titulo)) mapa[campo] = i;
            }
        }

        return mapa;
    }

    public static string SemAcento(string texto)
    {
        var decomposto = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(texto.Length);

        foreach (var c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
