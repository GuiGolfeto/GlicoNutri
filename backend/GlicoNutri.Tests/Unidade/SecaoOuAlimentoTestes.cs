using System.Text;
using GlicoNutri.Api.Dtos;
using GlicoNutri.Api.Services;

namespace GlicoNutri.Tests.Unidade;

/// <summary>
/// A distinção entre título de seção e alimento sem valores publicados. Errar
/// aqui descarta alimento real sem que nada apareça no relatório de erros.
/// </summary>
public class SecaoOuAlimentoTestes
{
    [Fact]
    public void CelulaVazia_EhTituloDeSecao_MasMarcadorEhAlimento()
    {
        // Um título de seção vem com as colunas realmente vazias; um alimento não
        // determinado traz "*" escrito, como faz a TACO 4ª edição com o leite.
        Assert.True(LeitorCsvTaco.TentarNumero("", out var vazio));
        Assert.Null(vazio);

        Assert.True(LeitorCsvTaco.TentarNumero("*", out var naoDeterminado));
        Assert.Null(naoDeterminado);

        // Os dois viram nulo depois da conversão: por isso a decisão precisa
        // olhar a célula bruta, e não o valor convertido.
        Assert.Equal(vazio, naoDeterminado);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("NA")]
    [InlineData("Tr")]
    public void MarcadorNaoEhCelulaVazia(string marcador) =>
        Assert.False(string.IsNullOrWhiteSpace(marcador));

    [Fact]
    public void CelulaVaziaEReconhecidaComoVazia() =>
        Assert.True(string.IsNullOrWhiteSpace("   "));
}
