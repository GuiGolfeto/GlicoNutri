using System.ComponentModel.DataAnnotations;

namespace GlicoNutri.Api.Dtos;

// ── UC012 — Registrar Emoção ────────────────────────────────────────────────

public record CriarRegistroEmocionalRequest(
    /// <summary>
    /// O passo 3 do UC012 fala em "a(s) emoção(ões)", mas o DER V2.0 guarda um
    /// estado emocional por registro. A lista é resolvida gravando um registro
    /// por emoção, compartilhando data, intensidade e descrição — a RN03 permite
    /// múltiplos registros por dia. Ao menos uma é obrigatória (RN01 do UC012).
    /// </summary>
    [Required, MinLength(1)] IReadOnlyList<long> EstadosEmocionaisIds,
    /// <summary>Escala de 1 (leve) a 5 (muito intenso), obrigatória conforme o UC012.</summary>
    [Range(1, 5)] int Intensidade,
    DateTime? DataHora,
    [StringLength(500)] string? Descricao);

public record RegistroEmocionalResponse(
    long Id,
    long PacienteId,
    long EstadoEmocionalId,
    string EstadoEmocional,
    int Intensidade,
    DateTime DataHora,
    string? Descricao);

public record DiarioEmocionalResponse(
    int Dias,
    DateTime InicioPeriodo,
    int TotalRegistros,
    IReadOnlyList<RegistroEmocionalResponse> Registros);

// ── RF06.2 — Correlação emocional × glicemia ────────────────────────────────

public record CorrelacaoEmocao(
    string EstadoEmocional,
    int RegistrosEmocionais,
    /// <summary>Medições de glicemia próximas no tempo a esses registros.</summary>
    int GlicemiasAssociadas,
    double? MediaGlicemia,
    double? MinimoGlicemia,
    double? MaximoGlicemia,
    double? PercentualForaDoAlvo,
    double IntensidadeMedia);

public record CorrelacaoEmocionalResponse(
    int Dias,
    /// <summary>Janela em horas usada para associar emoção e medição.</summary>
    int JanelaHoras,
    int RegistrosEmocionaisNoPeriodo,
    int GlicemiasNoPeriodo,
    double? MediaGlicemiaGeral,
    IReadOnlyList<CorrelacaoEmocao> PorEmocao);
