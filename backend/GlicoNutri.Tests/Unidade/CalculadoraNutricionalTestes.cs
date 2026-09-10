using GlicoNutri.Api.Services;

namespace GlicoNutri.Tests.Unidade;

/// <summary>
/// Os cálculos que sustentam a prescrição. Um erro aqui não aparece na tela:
/// sai um número plausível e errado, que vira plano alimentar.
/// </summary>
public class CalculadoraNutricionalTestes
{
    // ── RN21 — IMC ──────────────────────────────────────────────────────────

    [Theory]
    // peso, altura(cm), IMC esperado
    [InlineData(62, 168, 21.97)]
    [InlineData(59.5, 168, 21.08)]
    [InlineData(100, 180, 30.86)]
    [InlineData(45, 170, 15.57)]
    public void CalcularImc_UsaAlturaEmCentimetrosConvertidaParaMetros(
        double peso, double alturaCm, double esperado)
    {
        var resultado = CalculadoraNutricional.CalcularImc(peso, alturaCm);
        Assert.Equal(esperado, resultado.Imc, precision: 2);
    }

    [Theory]
    // Faixas do UC008 A2.
    [InlineData(17.0, "Abaixo do peso")]
    [InlineData(18.4, "Abaixo do peso")]
    [InlineData(18.5, "Peso normal")]
    [InlineData(24.9, "Peso normal")]
    [InlineData(25.0, "Sobrepeso")]
    [InlineData(29.9, "Sobrepeso")]
    [InlineData(30.0, "Obesidade")]
    [InlineData(42.0, "Obesidade")]
    public void ClassificarImc_SegueAsFaixasDoCasoDeUso(double imc, string esperado) =>
        Assert.Equal(esperado, CalculadoraNutricional.ClassificarImc(imc));

    [Fact]
    public void CalcularRcq_DivideCinturaPorQuadril() =>
        Assert.Equal(0.76, CalculadoraNutricional.CalcularRcq(74, 98));

    [Theory]
    [InlineData(null, 98d)]
    [InlineData(74d, null)]
    [InlineData(null, null)]
    [InlineData(74d, 0d)]
    public void CalcularRcq_SemAsDuasMedidas_NaoInventaValor(double? cintura, double? quadril) =>
        Assert.Null(CalculadoraNutricional.CalcularRcq(cintura, quadril));

    // ── RN14 — fórmulas energéticas ─────────────────────────────────────────

    [Fact]
    public void HarrisBenedict_Feminino_UsaOsCoeficientesDaRevisaoDe1984()
    {
        // 447,593 + 550,1965 + 520,464 − 90,93
        var tmb = CalculadoraNutricional.HarrisBenedict(59.5, 168, 21, masculino: false);
        Assert.Equal(1427.3235, tmb, precision: 4);
    }

    [Fact]
    public void HarrisBenedict_Masculino_UsaOsCoeficientesDaRevisaoDe1984()
    {
        // 88,362 + 1071,76 + 854,222 − 198,695
        var tmb = CalculadoraNutricional.HarrisBenedict(80, 178, 35, masculino: true);
        Assert.Equal(1815.649, tmb, precision: 3);
    }

    [Fact]
    public void MifflinStJeor_DiferenciaOsSexosPeloTermoConstante()
    {
        var homem = CalculadoraNutricional.MifflinStJeor(80, 178, 35, masculino: true);
        var mulher = CalculadoraNutricional.MifflinStJeor(80, 178, 35, masculino: false);

        // 800 + 1112,5 − 175 + 5 = 1742,5  |  o feminino subtrai 161 em vez de somar 5
        Assert.Equal(1742.5, homem, precision: 1);
        Assert.Equal(1576.5, mulher, precision: 1);
        Assert.Equal(166, homem - mulher, precision: 1);
    }

    [Fact]
    public void CalcularTmb_FormulaDesconhecida_FalhaEmVezDeEscolherOutra()
    {
        // RN14 — não é permitido produzir valor calórico sem respaldo.
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CalculadoraNutricional.CalcularTmb("ATWATER_INVENTADA", 70, 170, 30, true));
    }

    [Theory]
    [InlineData("HARRIS_BENEDICT")]
    [InlineData("MIFFLIN_ST_JEOR")]
    public void CalcularTmb_FormulasValidadas_Respondem(string codigo) =>
        Assert.True(CalculadoraNutricional.CalcularTmb(codigo, 70, 170, 30, true) > 0);

    [Fact]
    public void CalcularVet_MultiplicaTmbPeloFatorDeAtividade() =>
        Assert.Equal(1963, CalculadoraNutricional.CalcularVet(1427.4, 1.375));

    // ── RN15 e distribuição de macronutrientes ──────────────────────────────

    [Theory]
    [InlineData(55, 20, 25, true)]
    [InlineData(50, 20, 30, true)]
    [InlineData(50, 20, 25, false)]
    [InlineData(60, 25, 30, false)]
    public void SomaCemPorCento_ValidaARegraDosCemPorCento(
        double cho, double ptn, double lip, bool esperado) =>
        Assert.Equal(esperado, CalculadoraNutricional.SomaCemPorCento(cho, ptn, lip));

    [Fact]
    public void SomaCemPorCento_ToleraRuidoDePontoFlutuante()
    {
        // 33.33 × 3 = 99.99: dentro da tolerância, não deve reprovar o plano.
        Assert.True(CalculadoraNutricional.SomaCemPorCento(33.34, 33.33, 33.33));
    }

    [Fact]
    public void DistribuicaoPadrao_FicaDentroDasFaixasDoUC007ESomaCem()
    {
        var cho = CalculadoraNutricional.PadraoCarboidratos;
        var ptn = CalculadoraNutricional.PadraoProteinas;
        var lip = CalculadoraNutricional.PadraoLipidios;

        Assert.InRange(cho, 50, 60);
        Assert.InRange(ptn, 15, 20);
        Assert.InRange(lip, 25, 30);
        Assert.True(CalculadoraNutricional.SomaCemPorCento(cho, ptn, lip));
    }

    [Fact]
    public void MacrosEmGramas_AplicaOsFatoresDeAtwater()
    {
        var (cho, ptn, lip) = CalculadoraNutricional.MacrosEmGramas(1963, 55, 20, 25);

        Assert.Equal(269.9, cho, precision: 1);   // 1963 × 0,55 / 4
        Assert.Equal(98.2, ptn, precision: 1);    // 1963 × 0,20 / 4
        Assert.Equal(54.5, lip, precision: 1);    // 1963 × 0,25 / 9
    }

    [Fact]
    public void MacrosEmGramas_AsCaloriasDeVoltaFecham()
    {
        var (cho, ptn, lip) = CalculadoraNutricional.MacrosEmGramas(2000, 55, 20, 25);

        var kcal = cho * CalculadoraNutricional.KcalPorGramaCarboidrato
                 + ptn * CalculadoraNutricional.KcalPorGramaProteina
                 + lip * CalculadoraNutricional.KcalPorGramaLipidio;

        Assert.Equal(2000, kcal, precision: 0);
    }

    [Fact]
    public void PorPorcao_ConverteDeCemGramasParaAQuantidade() =>
        Assert.Equal(149.85, CalculadoraNutricional.PorPorcao(299.7, 50));

    [Fact]
    public void PorPorcao_ValorAusenteNaoViraZero()
    {
        // Zerar um valor não medido mentiria na conta do plano.
        Assert.Null(CalculadoraNutricional.PorPorcao(null, 100));
    }

    // ── RN19 e RN20 — glicemia ──────────────────────────────────────────────

    [Theory]
    // valor, min, max, esperado fora do alvo
    [InlineData(95, null, null, false)]      // padrão 70–180
    [InlineData(69, null, null, true)]
    [InlineData(181, null, null, true)]
    [InlineData(168, 80.0, 160.0, true)]   // dentro do padrão, fora do alvo individual
    [InlineData(168, null, null, false)]
    [InlineData(75, 80.0, 160.0, true)]
    [InlineData(80, 80.0, 160.0, false)]   // limites são inclusivos
    [InlineData(160, 80.0, 160.0, false)]
    public void ForaDoAlvo_UsaFaixaIndividualComFallbackNoPadrao(
        double valor, double? min, double? max, bool esperado) =>
        Assert.Equal(esperado, CalculadoraNutricional.ForaDoAlvo(valor, min, max));

    [Theory]
    [InlineData(55, "HIPOGLICEMIA")]
    [InlineData(69.9, "HIPOGLICEMIA")]
    [InlineData(70, "NORMAL")]
    [InlineData(180, "NORMAL")]
    [InlineData(210, "HIPERGLICEMIA")]
    public void ClassificarGlicemia_UsaLimiaresClinicosAbsolutos(double valor, string esperado) =>
        Assert.Equal(esperado, CalculadoraNutricional.ClassificarGlicemia(valor));

    [Fact]
    public void ClassificacaoClinicaEForaDoAlvo_SaoIndependentes()
    {
        // Um paciente com alvo estreito pode estar fora do alvo sem estar em
        // hipoglicemia — a distinção que as RN02 do UC004 e RN19 exigem manter.
        Assert.True(CalculadoraNutricional.ForaDoAlvo(168, 80, 160));
        Assert.Equal("NORMAL", CalculadoraNutricional.ClassificarGlicemia(168));
    }

    // ── Idade e variação ────────────────────────────────────────────────────

    [Theory]
    [InlineData("2005-03-14", "2026-03-13", 20)]   // véspera do aniversário
    [InlineData("2005-03-14", "2026-03-14", 21)]   // no dia
    [InlineData("2005-03-14", "2026-03-15", 21)]
    [InlineData("2026-01-01", "2026-06-01", 0)]    // menos de um ano de vida
    public void CalcularIdade_ContaAnosCompletos(string nascimento, string referencia, int esperado)
    {
        var idade = CalculadoraNutricional.CalcularIdade(
            DateOnly.Parse(nascimento), DateOnly.Parse(referencia));

        Assert.Equal(esperado, idade);
    }

    [Fact]
    public void CalcularIdade_NascidoEm29DeFevereiro_AniversariaEm28EmAnoNaoBissexto()
    {
        // Não há convenção única: parte da doutrina considera 1º de março, parte
        // considera 28 de fevereiro. O sistema segue o comportamento de
        // DateOnly.AddYears, que ajusta 29/02 para 28/02 — a diferença é de um
        // único dia, a cada quatro anos, e move a TMB em poucas kcal.
        // O teste existe para fixar a convenção, não para escondê-la.
        var nascimento = new DateOnly(2004, 2, 29);

        Assert.Equal(21, CalculadoraNutricional.CalcularIdade(nascimento, new DateOnly(2026, 2, 27)));
        Assert.Equal(22, CalculadoraNutricional.CalcularIdade(nascimento, new DateOnly(2026, 2, 28)));
        Assert.Equal(22, CalculadoraNutricional.CalcularIdade(nascimento, new DateOnly(2026, 3, 1)));
        // Em ano bissexto o aniversário cai no próprio 29: no dia 28 ainda não fez.
        Assert.Equal(19, CalculadoraNutricional.CalcularIdade(nascimento, new DateOnly(2024, 2, 28)));
        Assert.Equal(20, CalculadoraNutricional.CalcularIdade(nascimento, new DateOnly(2024, 2, 29)));
    }

    [Fact]
    public void VariacaoPercentual_CalculaPerdaEGanho()
    {
        Assert.Equal(-4.0, CalculadoraNutricional.VariacaoPercentual(62, 59.5));
        Assert.Equal(25.4, CalculadoraNutricional.VariacaoPercentual(1963, 2462));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0d)]
    public void VariacaoPercentual_SemBaseValida_NaoDivideNemInventa(double? anterior) =>
        Assert.Null(CalculadoraNutricional.VariacaoPercentual(anterior, 100));
}
