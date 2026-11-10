using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace GlicoNutri.Api.Security;

/// <summary>
/// RNF01 — criptografia por coluna. AES-256-GCM com nonce aleatório a cada
/// chamada: duas cifras do mesmo texto saem diferentes, o que é o
/// recomendado, mas por isso nunca dá para comparar ciphertext no banco (nem
/// checar duplicidade, nem buscar). Para o CPF, que precisa das duas coisas,
/// ver <see cref="HashBusca"/>.
/// </summary>
public class CriptografiaService
{
    private const int TamanhoNonce = 12;
    private const int TamanhoTag = 16;

    private readonly byte[] _chave;
    private readonly byte[] _chaveHash;

    public CriptografiaService(IOptions<CriptografiaOptions> opcoes)
    {
        _chave = Convert.FromBase64String(opcoes.Value.Chave);
        _chaveHash = Convert.FromBase64String(opcoes.Value.ChaveHash);
    }

    public string? Criptografar(string? textoPuro)
    {
        if (textoPuro is null) return null;

        var dados = Encoding.UTF8.GetBytes(textoPuro);
        var nonce = RandomNumberGenerator.GetBytes(TamanhoNonce);
        var cifrado = new byte[dados.Length];
        var tag = new byte[TamanhoTag];

        using (var aes = new AesGcm(_chave, TamanhoTag))
            aes.Encrypt(nonce, dados, cifrado, tag);

        // nonce + cifrado + tag, tudo em base64 — um único campo de texto no banco.
        var saida = new byte[TamanhoNonce + cifrado.Length + TamanhoTag];
        Buffer.BlockCopy(nonce, 0, saida, 0, TamanhoNonce);
        Buffer.BlockCopy(cifrado, 0, saida, TamanhoNonce, cifrado.Length);
        Buffer.BlockCopy(tag, 0, saida, TamanhoNonce + cifrado.Length, TamanhoTag);
        return Convert.ToBase64String(saida);
    }

    public string? Descriptografar(string? valorCifrado)
    {
        if (valorCifrado is null) return null;

        var bytes = Convert.FromBase64String(valorCifrado);
        var tamanhoCifrado = bytes.Length - TamanhoNonce - TamanhoTag;

        var nonce = bytes.AsSpan(0, TamanhoNonce);
        var cifrado = bytes.AsSpan(TamanhoNonce, tamanhoCifrado);
        var tag = bytes.AsSpan(TamanhoNonce + tamanhoCifrado, TamanhoTag);
        var dados = new byte[tamanhoCifrado];

        using (var aes = new AesGcm(_chave, TamanhoTag))
            aes.Decrypt(nonce, cifrado, tag, dados);

        return Encoding.UTF8.GetString(dados);
    }

    /// <summary>
    /// Hash determinístico (HMAC-SHA256) do CPF normalizado: mesmo CPF sempre
    /// gera o mesmo hash, por isso serve pra buscar e checar duplicidade — mas
    /// não é reversível, então não expõe o CPF por si só.
    /// </summary>
    public string HashBusca(string textoPuro) =>
        Convert.ToHexString(HMACSHA256.HashData(_chaveHash, Encoding.UTF8.GetBytes(textoPuro)));

    /// <summary>
    /// Alguns valores legados ainda estão em texto puro (RNF01, carga
    /// retroativa). Tenta decifrar; se não for um ciphertext válido, assume que
    /// já está em claro.
    /// </summary>
    public bool EhTextoPuro(string valor)
    {
        try
        {
            Descriptografar(valor);
            return false;
        }
        catch (Exception e) when (e is FormatException or CryptographicException or ArgumentException)
        {
            return true;
        }
    }
}
