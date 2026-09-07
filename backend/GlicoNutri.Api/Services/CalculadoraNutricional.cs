namespace GlicoNutri.Api.Services;

public record ResultadoImc(double Imc, string Classificacao);

/// <summary>
/// Cálculos clínicos do sistema — o <c>CalculadoraNutricional</c> do Diagrama de
/// Classes V5.0 e o <c>CalculadoraNutricionalService</c> do C4.
///
/// ATENÇÃO CLÍNICA: os coeficientes daqui definem a prescrição nutricional e
/// precisam de validação pela equipe de Nutrição, como prevê o Projeto de
/// Pesquisa V2.0 ("as decisões relativas aos parâmetros nutricionais dependem
/// da validação dos alunos de Nutrição").
/// </summary>
public static class CalculadoraNutricional
{
    /// <summary>
    /// RN21 — IMC = peso(kg) / altura(m)². A altura é persistida em centímetros,
    /// como o UC008 a coleta, e convertida aqui.
    /// </summary>
    public static ResultadoImc CalcularImc(double pesoKg, double alturaCm)
    {
        var alturaM = alturaCm / 100.0;
        var imc = pesoKg / (alturaM * alturaM);
        return new ResultadoImc(Math.Round(imc, 2), ClassificarImc(imc));
    }

    /// <summary>
    /// Faixas conforme o UC008 A2: &lt;18,5 Abaixo; 18,5–24,9 Normal;
    /// 25–29,9 Sobrepeso; ≥30 Obesidade. A OMS ainda subdivide a obesidade em
    /// graus I, II e III — se a equipe de Nutrição quiser esse detalhe, é só
    /// estender as faixas abaixo.
    /// </summary>
    public static string ClassificarImc(double imc) => imc switch
    {
        < 18.5 => "Abaixo do peso",
        < 25.0 => "Peso normal",
        < 30.0 => "Sobrepeso",
        _ => "Obesidade",
    };

    /// <summary>Relação cintura-quadril, calculada quando ambas as medidas existirem.</summary>
    public static double? CalcularRcq(double? cinturaCm, double? quadrilCm) =>
        cinturaCm is > 0 && quadrilCm is > 0
            ? Math.Round(cinturaCm.Value / quadrilCm.Value, 2)
            : null;

    /// <summary>
    /// Taxa metabólica basal por Harris-Benedict, na revisão de Roza e Shizgal
    /// (1984), que é a mais precisa e a de uso corrente.
    ///
    /// A equação original de 1919 usa outros coeficientes
    /// (homens 66,473 / 13,7516 / 5,0033 / 6,755; mulheres 655,0955 / 9,5634 /
    /// 1,8496 / 4,6756) e ainda é o que se ensina em parte dos cursos. Qual das
    /// duas vale é decisão da equipe de Nutrição — trocar são estes quatro números.
    /// </summary>
    public static double HarrisBenedict(double pesoKg, double alturaCm, int idade, bool masculino) =>
        masculino
            ? 88.362 + (13.397 * pesoKg) + (4.799 * alturaCm) - (5.677 * idade)
            : 447.593 + (9.247 * pesoKg) + (3.098 * alturaCm) - (4.330 * idade);

    /// <summary>Taxa metabólica basal por Mifflin-St Jeor (1990).</summary>
    public static double MifflinStJeor(double pesoKg, double alturaCm, int idade, bool masculino) =>
        (10 * pesoKg) + (6.25 * alturaCm) - (5 * idade) + (masculino ? 5 : -161);

    /// <summary>
    /// RN14 — só fórmulas reconhecidas e implementadas. Um código desconhecido
    /// falha alto em vez de silenciosamente cair numa fórmula qualquer.
    /// </summary>
    public static double CalcularTmb(
        string codigoFormula, double pesoKg, double alturaCm, int idade, bool masculino) =>
        codigoFormula switch
        {
            Data.Codigos.Formula.HarrisBenedict => HarrisBenedict(pesoKg, alturaCm, idade, masculino),
            Data.Codigos.Formula.MifflinStJeor => MifflinStJeor(pesoKg, alturaCm, idade, masculino),
            _ => throw new ArgumentOutOfRangeException(
                nameof(codigoFormula), codigoFormula, "Fórmula de cálculo energético não implementada."),
        };

    /// <summary>UC006, passo 5 — VET = TMB × fator de atividade.</summary>
    public static double CalcularVet(double tmb, double fatorAtividade) =>
        Math.Round(tmb * fatorAtividade, 0);

    /// <summary>Idade em anos completos na data de referência.</summary>
    public static int CalcularIdade(DateOnly nascimento, DateOnly referencia)
    {
        var idade = referencia.Year - nascimento.Year;
        if (referencia < nascimento.AddYears(idade)) idade--;
        return idade;
    }

    // ── Macronutrientes (UC007) ─────────────────────────────────────────────

    /// <summary>Fatores de Atwater: quilocalorias por grama de cada macronutriente.</summary>
    public const double KcalPorGramaCarboidrato = 4;
    public const double KcalPorGramaProteina = 4;
    public const double KcalPorGramaLipidio = 9;

    /// <summary>
    /// UC007 A2 — distribuição automática. As faixas do caso de uso são
    /// carboidratos 50–60%, proteínas 15–20% e lipídios 25–30%; 55/20/25 fica
    /// dentro das três e soma exatamente 100%, atendendo a RN15.
    /// Valores de partida: o nutricionista ajusta pelo RF03.2.
    /// </summary>
    public const double PadraoCarboidratos = 55;
    public const double PadraoProteinas = 20;
    public const double PadraoLipidios = 25;

    /// <summary>Tolerância na soma dos percentuais, para não brigar com ponto flutuante.</summary>
    private const double ToleranciaSoma = 0.01;

    /// <summary>RN15 — a soma dos percentuais deve ser exatamente 100%.</summary>
    public static bool SomaCemPorCento(double carboidratos, double proteinas, double lipidios) =>
        Math.Abs(carboidratos + proteinas + lipidios - 100) <= ToleranciaSoma;

    /// <summary>Converte os percentuais em gramas/dia sobre o valor calórico total.</summary>
    public static (double Carboidratos, double Proteinas, double Lipidios) MacrosEmGramas(
        double vetKcal, double percentualCarboidratos, double percentualProteinas, double percentualLipidios) =>
    (
        Math.Round(vetKcal * percentualCarboidratos / 100 / KcalPorGramaCarboidrato, 1),
        Math.Round(vetKcal * percentualProteinas / 100 / KcalPorGramaProteina, 1),
        Math.Round(vetKcal * percentualLipidios / 100 / KcalPorGramaLipidio, 1)
    );

    /// <summary>
    /// Contribuição de um item do plano. Os valores da base são por 100 g, e a
    /// quantidade é registrada em gramas.
    /// </summary>
    public static double? PorPorcao(double? valorPor100g, double quantidadeGramas) =>
        valorPor100g is { } v ? Math.Round(v * quantidadeGramas / 100, 2) : null;

    /// <summary>Variação percentual entre duas medidas, para os comparativos das telas.</summary>
    public static double? VariacaoPercentual(double? anterior, double atual) =>
        anterior is > 0 ? Math.Round((atual - anterior.Value) / anterior.Value * 100, 1) : null;
}
