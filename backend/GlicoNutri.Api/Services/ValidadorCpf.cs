namespace GlicoNutri.Api.Services;

/// <summary>
/// Validação do dígito verificador do CPF, exigida como fluxo &lt;&lt;include&gt;&gt;
/// pelo UC002 (A1) e pelo UC003 (A1).
/// </summary>
public static class ValidadorCpf
{
    public static bool EhValido(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return false;

        Span<int> digitos = stackalloc int[11];
        var total = 0;

        foreach (var c in cpf)
        {
            if (c is '.' or '-' or ' ') continue;
            if (!char.IsAsciiDigit(c)) return false;
            if (total == 11) return false;
            digitos[total++] = c - '0';
        }

        if (total != 11) return false;

        // Sequências repetidas (000.000.000-00, 111.111.111-11, ...) passam no
        // cálculo dos dígitos, mas não são CPFs válidos.
        var todosIguais = true;
        for (var i = 1; i < 11 && todosIguais; i++)
            todosIguais = digitos[i] == digitos[0];
        if (todosIguais) return false;

        return digitos[9] == CalcularDigito(digitos, 9)
            && digitos[10] == CalcularDigito(digitos, 10);
    }

    private static int CalcularDigito(ReadOnlySpan<int> digitos, int posicao)
    {
        var peso = posicao + 1;
        var soma = 0;

        for (var i = 0; i < posicao; i++)
            soma += digitos[i] * peso--;

        var resto = soma * 10 % 11;
        return resto == 10 ? 0 : resto;
    }

    /// <summary>Remove a máscara, para que o CPF seja sempre persistido só com dígitos.</summary>
    public static string Normalizar(string cpf) =>
        new(cpf.Where(char.IsAsciiDigit).ToArray());
}
