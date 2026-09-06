using System.Security.Claims;
using GlicoNutri.Api.Data;
using GlicoNutri.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace GlicoNutri.Api.Services;

public interface IAcessoPacienteService
{
    Task<bool> PodeAcessarAsync(ClaimsPrincipal usuario, long pacienteId, CancellationToken ct = default);
}

/// <summary>
/// RN33 — dados clínicos são acessíveis apenas pelo próprio paciente, pelo
/// Nutricionista responsável vinculado e pelo Administrador.
///
/// Concentrar a regra aqui também concilia os documentos: o UC008 coloca o
/// Nutricionista como ator do registro antropométrico, o RF04.1 diz que o
/// paciente registra pelo app e o C4 marca o AntropometriaController como perfil
/// Paciente. Os três valem — muda apenas quem é o dono do dado.
/// </summary>
public class AcessoPacienteService(GlicoNutriDbContext db) : IAcessoPacienteService
{
    public async Task<bool> PodeAcessarAsync(
        ClaimsPrincipal usuario, long pacienteId, CancellationToken ct = default)
    {
        var perfil = usuario.FindFirst(ClaimsGlicoNutri.Perfil)?.Value;
        var usuarioId = usuario.ObterUsuarioId();
        if (usuarioId is null) return false;

        return perfil switch
        {
            Codigos.Perfil.Administrador => true,
            Codigos.Perfil.Paciente => usuarioId == pacienteId,
            Codigos.Perfil.Nutricionista => await db.NutricionistaPaciente
                .AnyAsync(v => v.PacienteId == pacienteId
                            && v.NutricionistaId == usuarioId
                            && v.Ativo, ct),
            _ => false,
        };
    }
}
