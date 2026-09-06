using GlicoNutri.Api.Data;
using GlicoNutri.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GlicoNutri.Api.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken ct = default);
    Task<Usuario?> BuscarPorIdAsync(long id, CancellationToken ct = default);
    Task<bool> EmailEmUsoAsync(string email, CancellationToken ct = default);
    Task SalvarAsync(CancellationToken ct = default);
}

/// <summary>
/// Acesso a usuários, perfis e ao contador de tentativas de login (C4, módulo
/// de Autenticação).
/// </summary>
public class UsuarioRepository(GlicoNutriDbContext db) : IUsuarioRepository
{
    public Task<Usuario?> BuscarPorEmailAsync(string email, CancellationToken ct = default) =>
        db.Usuarios
          .Include(u => u.Perfil)
          .FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<Usuario?> BuscarPorIdAsync(long id, CancellationToken ct = default) =>
        db.Usuarios
          .Include(u => u.Perfil)
          .FirstOrDefaultAsync(u => u.Id == id, ct);

    /// <summary>
    /// RN06 — e-mail é único em todo o sistema, independentemente do perfil.
    /// Ignora o filtro de soft delete: um usuário desativado ainda ocupa o e-mail,
    /// já que a RN05 garante que ele pode ser reativado com todo o histórico.
    /// </summary>
    public Task<bool> EmailEmUsoAsync(string email, CancellationToken ct = default) =>
        db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Email == email, ct);

    public Task SalvarAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
