using System.Security.Cryptography;

namespace GlicoNutri.Api.Services;

public interface ISenhaService
{
    string Hash(string senha);
    bool Conferir(string senha, string hash);
    string GerarProvisoria();
    string ImpressaoDigital(string hash);
}

/// <summary>
/// RN32 — senhas só existem em hash bcrypt. Texto puro, MD5 e SHA-1 são vedados.
/// </summary>
public class SenhaService : ISenhaService
{
    /// <summary>Fator de custo do bcrypt. 12 ≈ 250ms por hash no hardware atual.</summary>
    private const int FatorCusto = 12;

    private const string Alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

    public string Hash(string senha) => BCrypt.Net.BCrypt.HashPassword(senha, FatorCusto);

    public bool Conferir(string senha, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(senha, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash corrompido ou em formato legado: trata como credencial inválida
            // em vez de derrubar a requisição.
            return false;
        }
    }

    /// <summary>
    /// Senha provisória gerada pelo sistema no cadastro feito por Administrador ou
    /// Nutricionista (RN03). Sem caracteres ambíguos, para ser ditada por telefone.
    /// </summary>
    public string GerarProvisoria()
    {
        Span<char> destino = stackalloc char[10];
        for (var i = 0; i < destino.Length; i++)
            destino[i] = Alfabeto[RandomNumberGenerator.GetInt32(Alfabeto.Length)];
        return new string(destino);
    }

    /// <summary>
    /// Impressão digital curta do hash da senha, embutida no token de recuperação.
    /// Como redefinir a senha troca o hash, o token deixa de validar sozinho — é o
    /// que garante o uso único sem precisar de tabela de tokens fora do DER V2.0.
    /// </summary>
    public string ImpressaoDigital(string hash)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(hash));
        return Convert.ToHexString(bytes)[..16];
    }
}
