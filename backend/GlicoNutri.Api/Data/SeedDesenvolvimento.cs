using GlicoNutri.Api.Models;
using GlicoNutri.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace GlicoNutri.Api.Data;

/// <summary>
/// Cria o Administrador inicial. O sistema não permite auto-cadastro (RN08 e RN09):
/// pacientes são cadastrados por nutricionistas e nutricionistas por administradores,
/// então é preciso um administrador de origem para a cadeia começar.
/// Só roda em ambiente de desenvolvimento.
/// </summary>
public static class SeedDesenvolvimento
{
    public const string EmailAdmin = "admin@gliconutri.local";
    public const string SenhaAdmin = "GlicoNutri@2026";

    public static async Task AplicarAsync(IServiceProvider servicos, CancellationToken ct = default)
    {
        using var escopo = servicos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<GlicoNutriDbContext>();
        var senhas = escopo.ServiceProvider.GetRequiredService<ISenhaService>();
        var log = escopo.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(SeedDesenvolvimento));

        if (await db.Usuarios.IgnoreQueryFilters().AnyAsync(ct))
            return;

        db.Administradores.Add(new Administrador
        {
            Nome = "Administrador do Sistema",
            Email = EmailAdmin,
            SenhaHash = senhas.Hash(SenhaAdmin),
            NivelAcesso = 1,
            Ativo = true,
            DataCadastro = DateTime.UtcNow,
            // Definitiva de propósito: sem ela o admin cairia no bloqueio da RN03
            // e não conseguiria cadastrar ninguém no primeiro acesso.
            SenhaProvisoria = false,
        });

        await db.SaveChangesAsync(ct);
        log.LogWarning("Administrador inicial criado: {Email} / {Senha}", EmailAdmin, SenhaAdmin);
    }
}
