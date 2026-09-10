using GlicoNutri.Api.Services;

namespace GlicoNutri.Tests.Unidade;

/// <summary>UC002 A1 e UC003 A1 — validação do dígito verificador.</summary>
public class ValidadorCpfTestes
{
    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    [InlineData("390.533.447-05")]
    [InlineData(" 529 982 247 25 ")]
    public void EhValido_AceitaCpfCorretoComOuSemMascara(string cpf) =>
        Assert.True(ValidadorCpf.EhValido(cpf));

    [Theory]
    [InlineData("529.982.247-26")]   // último dígito trocado
    [InlineData("529.982.247-15")]   // penúltimo dígito trocado
    [InlineData("123.456.789-00")]
    public void EhValido_RecusaDigitoVerificadorErrado(string cpf) =>
        Assert.False(ValidadorCpf.EhValido(cpf));

    [Theory]
    [InlineData("000.000.000-00")]
    [InlineData("111.111.111-11")]
    [InlineData("999.999.999-99")]
    public void EhValido_RecusaSequenciasRepetidas(string cpf)
    {
        // Passam no cálculo dos dígitos, mas não são CPFs válidos.
        Assert.False(ValidadorCpf.EhValido(cpf));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("5299822472")]        // 10 dígitos
    [InlineData("529982247250")]      // 12 dígitos
    [InlineData("529.982.247-2X")]    // caractere inválido
    public void EhValido_RecusaEntradaMalFormada(string? cpf) =>
        Assert.False(ValidadorCpf.EhValido(cpf));

    [Fact]
    public void Normalizar_DeixaApenasDigitos() =>
        Assert.Equal("52998224725", ValidadorCpf.Normalizar("529.982.247-25"));
}

/// <summary>RN32 — armazenamento de credenciais.</summary>
public class SenhaServiceTestes
{
    private readonly SenhaService _servico = new();

    [Fact]
    public void Hash_NaoGuardaASenhaEmTextoPuro()
    {
        var hash = _servico.Hash("Senha@2026");

        Assert.DoesNotContain("Senha@2026", hash);
        Assert.StartsWith("$2", hash);   // prefixo do bcrypt
    }

    [Fact]
    public void Hash_DaResultadosDiferentesParaAMesmaSenha()
    {
        // O salt por hash é o que impede tabela arco-íris.
        Assert.NotEqual(_servico.Hash("Senha@2026"), _servico.Hash("Senha@2026"));
    }

    [Fact]
    public void Conferir_ReconheceASenhaCorretaERecusaAsDemais()
    {
        var hash = _servico.Hash("Senha@2026");

        Assert.True(_servico.Conferir("Senha@2026", hash));
        Assert.False(_servico.Conferir("senha@2026", hash));
        Assert.False(_servico.Conferir("Senha@2027", hash));
        Assert.False(_servico.Conferir("", hash));
    }

    [Fact]
    public void Conferir_HashCorrompido_TrataComoCredencialInvalida()
    {
        // Não pode derrubar a requisição de login.
        Assert.False(_servico.Conferir("qualquer", "isto-nao-e-um-hash-bcrypt"));
    }

    [Fact]
    public void GerarProvisoria_EvitaCaracteresAmbiguosEVariaACadaChamada()
    {
        var senhas = Enumerable.Range(0, 200).Select(_ => _servico.GerarProvisoria()).ToList();

        Assert.All(senhas, s => Assert.Equal(10, s.Length));
        // O, 0, I, l e 1 saem do alfabeto para a senha poder ser ditada.
        Assert.All(senhas, s => Assert.DoesNotContain(s, c => "O0Il1".Contains(c)));
        Assert.True(senhas.Distinct().Count() > 190);
    }

    [Fact]
    public void ImpressaoDigital_MudaQuandoOHashMuda()
    {
        // É o que faz o link de recuperação queimar após o uso.
        var antes = _servico.Hash("Antiga@2026");
        var depois = _servico.Hash("Nova@2026");

        Assert.NotEqual(_servico.ImpressaoDigital(antes), _servico.ImpressaoDigital(depois));
        Assert.Equal(_servico.ImpressaoDigital(antes), _servico.ImpressaoDigital(antes));
    }
}

/// <summary>RN02 do UC001 — política de senha.</summary>
public class SenhaForteTestes
{
    private static bool Aceita(string senha)
    {
        var atributo = new Api.Security.SenhaForteAttribute();
        var contexto = new System.ComponentModel.DataAnnotations.ValidationContext(new object());
        return atributo.GetValidationResult(senha, contexto) is null;
    }

    [Theory]
    [InlineData("Senha2026")]
    [InlineData("abcdefg1")]
    [InlineData("1abcdefg")]
    public void Aceita_ComOitoCaracteresLetrasENumeros(string senha) =>
        Assert.True(Aceita(senha));

    [Theory]
    [InlineData("12345678")]        // só números
    [InlineData("senhasemnumero")]  // só letras
    [InlineData("Senha1")]          // curta demais
    [InlineData("")]
    public void Recusa_QuandoFaltaTamanhoOuComposicao(string senha) =>
        Assert.False(Aceita(senha));
}
