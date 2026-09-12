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

    // ── Glicemia (UC004, UC005) ─────────────────────────────────────────────

    /// <summary>
    /// RN20 — faixa aplicada enquanto o Nutricionista não personalizar os limites
    /// do paciente. São também os limites clínicos de hipo e hiperglicemia da
    /// RN02 do UC004.
    /// </summary>
    public const double GlicemiaMinPadrao = 70;
    public const double GlicemiaMaxPadrao = 180;

    /// <summary>Faixa aceita no registro (UC004 A1) — validada também por CHECK no banco.</summary>
    public const double GlicemiaMinimaAceita = 0;
    public const double GlicemiaMaximaAceita = 600;

    /// <summary>
    /// RN19 — classifica contra a faixa alvo individual do paciente. Sem
    /// personalização, cai no padrão da RN20.
    /// </summary>
    public static bool ForaDoAlvo(double valor, double? minAlvo, double? maxAlvo) =>
        valor < (minAlvo ?? GlicemiaMinPadrao) || valor > (maxAlvo ?? GlicemiaMaxPadrao);

    /// <summary>
    /// RN02 do UC004 — hipo e hiperglicemia são definidas por limiares clínicos
    /// absolutos (70 e 180), e não pela faixa alvo individual. As duas coisas
    /// convivem: um paciente com alvo estreito pode estar fora do alvo sem estar
    /// em hipoglicemia.
    /// </summary>
    public static string ClassificarGlicemia(double valor) => valor switch
    {
        < GlicemiaMinPadrao => "HIPOGLICEMIA",
        > GlicemiaMaxPadrao => "HIPERGLICEMIA",
        _ => "NORMAL",
    };

    // ── Macronutrientes (UC007) ─────────────────────────────────────────────

    /// <summary>Fatores de Atwater: quilocalorias por grama de cada macronutriente.</summary>
    public const double KcalPorGramaCarboidrato = 4;
    public const double KcalPorGramaProteina = 4;
    public const double KcalPorGramaLipidio = 9;

    /// <summary>
    /// UC007 A2 — distribuição automática, nas faixas da Diretriz da Sociedade
    /// Brasileira de Diabetes para diabetes tipo 2: carboidratos 45–60%,
    /// proteínas 15–20% e lipídios 25–35%. O padrão é o centro de cada faixa, e
    /// os três somam exatamente 100%, atendendo a RN15.
    ///
    /// As faixas do UC007 A2 são mais estreitas (50–60 e 25–30) e ficaram para
    /// trás: a referência clínica do sistema é a SBD, e o caso de uso reprovaria
    /// distribuições que a diretriz aceita. Decisão do Mateus em 11/09/2026.
    ///
    /// Valores de partida: o nutricionista ajusta pelo RF03.2.
    /// </summary>
    public const double PadraoCarboidratos = 50;
    public const double PadraoProteinas = 20;
    public const double PadraoLipidios = 30;

    /// <summary>Faixas aceitas pela SBD para cada macronutriente, em % do VET.</summary>
    public static readonly (double Min, double Max) FaixaCarboidratos = (45, 60);
    public static readonly (double Min, double Max) FaixaProteinas = (15, 20);
    public static readonly (double Min, double Max) FaixaLipidios = (25, 35);

    /// <summary>
    /// Percentual dentro da faixa da diretriz. Fora dela o plano não é recusado —
    /// a conduta é do nutricionista —, mas a tela avisa.
    /// </summary>
    public static bool DentroDaFaixa(double valor, (double Min, double Max) faixa) =>
        valor >= faixa.Min && valor <= faixa.Max;

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
