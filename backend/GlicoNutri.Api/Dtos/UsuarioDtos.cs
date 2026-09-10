using System.ComponentModel.DataAnnotations;

namespace GlicoNutri.Api.Dtos;

// ── UC003 — Cadastrar Nutricionista ─────────────────────────────────────────

public record CriarNutricionistaRequest(
    [Required, StringLength(150, MinimumLength = 3)] string Nome,
    [Required, EmailAddress, StringLength(150)] string Email,
    [Required, StringLength(20)] string Crn,
    [StringLength(100)] string? Especialidade,
    [StringLength(20)] string? Telefone);

public record NutricionistaResponse(
    long Id,
    string Nome,
    string Email,
    string Crn,
    string? Especialidade,
    string? Telefone,
    bool Ativo,
    bool SenhaProvisoria,
    DateTime DataCadastro,
    int TotalPacientes);

// ── UC002 — Cadastrar Paciente ──────────────────────────────────────────────

public record CriarPacienteRequest(
    [Required, StringLength(150, MinimumLength = 3)] string Nome,
    [Required, EmailAddress, StringLength(150)] string Email,
    [Required, StringLength(14)] string Cpf,
    [Required] DateOnly DataNascimento,
    [Required] long SexoId,
    [Required] long TipoDiabetesId,
    [StringLength(20)] string? Telefone,
    string? MedicacaoEmUso,
    string? ObservacoesClinicas,
    /// <summary>
    /// Nutricionista responsável (RN07). Obrigatório quando quem cadastra é o
    /// Administrador; ignorado quando é o próprio Nutricionista, que se vincula
    /// automaticamente conforme a RN03 do UC002.
    /// </summary>
    long? NutricionistaId);

public record PacienteResponse(
    long Id,
    string Nome,
    string Email,
    string Cpf,
    DateOnly DataNascimento,
    int Idade,
    string Sexo,
    string TipoDiabetes,
    string? Telefone,
    string? MedicacaoEmUso,
    string? ObservacoesClinicas,
    double? GlicemiaMinAlvo,
    double? GlicemiaMaxAlvo,
    /// <summary>RN20 — falso enquanto o Nutricionista não personalizar a faixa alvo.</summary>
    bool FaixaGlicemicaPersonalizada,
    long? NutricionistaId,
    string? NutricionistaNome,
    bool Ativo,
    DateTime DataCadastro);

public record DefinirMetasGlicemicasRequest(
    [Range(20, 600)] double? GlicemiaMinAlvo,
    [Range(20, 600)] double? GlicemiaMaxAlvo);

// ── RF10.5 — Edição de cadastros ────────────────────────────────────────────

public record AtualizarNutricionistaRequest(
    [Required, StringLength(150, MinimumLength = 3)] string Nome,
    [Required, EmailAddress, StringLength(150)] string Email,
    [Required, StringLength(20)] string Crn,
    [StringLength(100)] string? Especialidade,
    [StringLength(20)] string? Telefone);

public record AtualizarPacienteRequest(
    [Required, StringLength(150, MinimumLength = 3)] string Nome,
    [Required, EmailAddress, StringLength(150)] string Email,
    [Required, StringLength(14)] string Cpf,
    [Required] DateOnly DataNascimento,
    [Required] long SexoId,
    [Required] long TipoDiabetesId,
    [StringLength(20)] string? Telefone,
    string? MedicacaoEmUso,
    string? ObservacoesClinicas);

// ── Referências para preencher os selects do formulário ─────────────────────

public record ReferenciaResponse(long Id, string Codigo, string Descricao);
