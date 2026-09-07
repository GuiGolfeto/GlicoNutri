using GlicoNutri.Api.Data;
using GlicoNutri.Api.Security;
using GlicoNutri.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GlicoNutri.Api.Controllers;

/// <summary>
/// UC011 — Dashboard do Nutricionista. RF08.3.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = Politicas.Nutricionista)]
public class DashboardController(IDashboardService servico) : ControllerBase
{
    /// <summary>
    /// RN01 do UC011 — o Nutricionista vê apenas os pacientes vinculados a ele.
    /// O Administrador, que herda o perfil pela RN04, enxerga todos e pode
    /// filtrar por profissional.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Obter(
        [FromQuery] long? nutricionistaId = null, CancellationToken ct = default)
    {
        var ehAdministrador =
            User.FindFirst(ClaimsGlicoNutri.Perfil)?.Value == Codigos.Perfil.Administrador;

        var filtro = ehAdministrador ? nutricionistaId : User.ObterUsuarioId();

        return Ok(await servico.ObterAsync(filtro, ct));
    }
}
