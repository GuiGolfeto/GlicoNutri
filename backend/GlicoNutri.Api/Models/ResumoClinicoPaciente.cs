using GlicoNutri.Api.Models.Referencia;

namespace GlicoNutri.Api.Models;

/// <summary>
/// Read Model exclusivo dos dashboards (padrao orientado pelo professor).
/// NUNCA e fonte de escrita nem de verdade para regra de negocio: os Services de
/// escrita (Glicemia, Antropometria, PlanoAlimentar, Alerta) atualizam esta
/// tabela apos cada persistencia, e o DashboardService le SOMENTE dela.
/// Relacao 1:1 com paciente, criada junto do cadastro.
/// </summary>
public class ResumoClinicoPaciente
{
    /// <summary>PK e FK para pacientes.usuario_id.</summary>
    public long PacienteId { get; set; }
    public Paciente Paciente { get; set; } = null!;

    public double? UltimaGlicemiaValor { get; set; }

    public long? UltimaGlicemiaContextoId { get; set; }
    public ContextoGlicemia? UltimaGlicemiaContexto { get; set; }

    public DateTime? UltimaGlicemiaData { get; set; }
    public double? MediaGlicemia7Dias { get; set; }
    public double? PercentualNoAlvo7Dias { get; set; }

    public double? UltimoImc { get; set; }
    public string? UltimaClassificacaoImc { get; set; }
    public DateTime? UltimaDataAntropometria { get; set; }

    public bool PlanoAtivo { get; set; }

    /// <summary>Recalculado pelo AlertaService a cada mudanca de status (RN35).</summary>
    public int AlertasPendentesCount { get; set; }

    public int? DiasSemRegistroGlicemia { get; set; }
    public DateTime DataAtualizacao { get; set; }
}
