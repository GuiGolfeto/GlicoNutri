using GlicoNutri.Api.Models.Referencia;
using Microsoft.EntityFrameworkCore;

namespace GlicoNutri.Api.Data;

/// <summary>
/// Códigos das tabelas de referência, para uso nos Services sem string mágica.
/// Os valores vêm dos enums documentados no cabeçalho do DER V2.0.
/// </summary>
public static class Codigos
{
    public static class Perfil
    {
        public const string Paciente = "PACIENTE";
        public const string Nutricionista = "NUTRICIONISTA";
        public const string Administrador = "ADMINISTRADOR";
    }

    public static class Formula
    {
        public const string HarrisBenedict = "HARRIS_BENEDICT";
        public const string MifflinStJeor = "MIFFLIN_ST_JEOR";
    }

    public static class Alerta
    {
        public const string Glicemia = "GLICEMIA";
        public const string Medicamento = "MEDICAMENTO";
        public const string Refeicao = "REFEICAO";
        public const string Personalizado = "PERSONALIZADO";
    }

    public static class Status
    {
        public const string Pendente = "PENDENTE";
        public const string Atendido = "ATENDIDO";
        public const string Reagendado = "REAGENDADO";
        public const string Falhou = "FALHOU";
    }

    public static class Fonte
    {
        public const string Taco = "TACO";
        public const string Manual = "MANUAL";
    }

    public static class Sexo
    {
        public const string Masculino = "MASCULINO";
        public const string Feminino = "FEMININO";
    }
}

public static class SeedReferencia
{
    public static void Aplicar(ModelBuilder b)
    {
        b.Entity<PerfilUsuario>().HasData(
            new PerfilUsuario { Id = 1, Codigo = Codigos.Perfil.Paciente, Descricao = "Paciente", Ativo = true },
            new PerfilUsuario { Id = 2, Codigo = Codigos.Perfil.Nutricionista, Descricao = "Nutricionista", Ativo = true },
            new PerfilUsuario { Id = 3, Codigo = Codigos.Perfil.Administrador, Descricao = "Administrador", Ativo = true });

        b.Entity<SexoBiologico>().HasData(
            new SexoBiologico { Id = 1, Codigo = Codigos.Sexo.Masculino, Descricao = "Masculino", Ativo = true },
            new SexoBiologico { Id = 2, Codigo = Codigos.Sexo.Feminino, Descricao = "Feminino", Ativo = true });

        b.Entity<TipoDiabetes>().HasData(
            new TipoDiabetes { Id = 1, Codigo = "TIPO_1", Descricao = "Diabetes tipo 1", Ativo = true },
            new TipoDiabetes { Id = 2, Codigo = "TIPO_2", Descricao = "Diabetes tipo 2", Ativo = true },
            new TipoDiabetes { Id = 3, Codigo = "GESTACIONAL", Descricao = "Diabetes gestacional", Ativo = true },
            new TipoDiabetes { Id = 4, Codigo = "MODY", Descricao = "MODY (Maturity Onset Diabetes of the Young)", Ativo = true });

        // Contextos do DER V2.0. Por serem linhas de tabela, e não valores de um
        // enum compilado, o nutricionista pode desdobrar POS_REFEICAO em 1h e 2h
        // (como pede o UC004) sem migration nem recompilação.
        b.Entity<ContextoGlicemia>().HasData(
            new ContextoGlicemia { Id = 1, Codigo = "JEJUM", Descricao = "Jejum", Ativo = true },
            new ContextoGlicemia { Id = 2, Codigo = "PRE_REFEICAO", Descricao = "Pré-refeição", Ativo = true },
            new ContextoGlicemia { Id = 3, Codigo = "POS_REFEICAO", Descricao = "Pós-refeição", Ativo = true },
            new ContextoGlicemia { Id = 4, Codigo = "ANTES_DE_DORMIR", Descricao = "Ao deitar", Ativo = true },
            new ContextoGlicemia { Id = 5, Codigo = "OUTRO", Descricao = "Outro", Ativo = true });

        b.Entity<EstadoEmocional>().HasData(
            new EstadoEmocional { Id = 1, Codigo = "FELIZ", Descricao = "Feliz", Ativo = true },
            new EstadoEmocional { Id = 2, Codigo = "TRANQUILO", Descricao = "Tranquilo", Ativo = true },
            new EstadoEmocional { Id = 3, Codigo = "ANSIOSO", Descricao = "Ansioso", Ativo = true },
            new EstadoEmocional { Id = 4, Codigo = "TRISTE", Descricao = "Triste", Ativo = true },
            new EstadoEmocional { Id = 5, Codigo = "IRRITADO", Descricao = "Irritado", Ativo = true },
            new EstadoEmocional { Id = 6, Codigo = "ESTRESSADO", Descricao = "Estressado", Ativo = true });

        // RN14 — apenas fórmulas cientificamente reconhecidas e implementadas.
        b.Entity<FormulaEnergetica>().HasData(
            new FormulaEnergetica { Id = 1, Codigo = Codigos.Formula.HarrisBenedict, Descricao = "Harris-Benedict", Ativo = true },
            new FormulaEnergetica { Id = 2, Codigo = Codigos.Formula.MifflinStJeor, Descricao = "Mifflin-St Jeor", Ativo = true });

        // Fatores de atividade aplicados sobre a TMB para obter o VET (UC006).
        b.Entity<NivelAtividade>().HasData(
            new NivelAtividade { Id = 1, Codigo = "SEDENTARIO", Descricao = "Sedentário", Fator = 1.2, Ativo = true },
            new NivelAtividade { Id = 2, Codigo = "LEVEMENTE_ATIVO", Descricao = "Levemente ativo", Fator = 1.375, Ativo = true },
            new NivelAtividade { Id = 3, Codigo = "MODERADAMENTE_ATIVO", Descricao = "Moderadamente ativo", Fator = 1.55, Ativo = true },
            new NivelAtividade { Id = 4, Codigo = "MUITO_ATIVO", Descricao = "Muito ativo", Fator = 1.725, Ativo = true },
            new NivelAtividade { Id = 5, Codigo = "EXTREMAMENTE_ATIVO", Descricao = "Extremamente ativo", Fator = 1.9, Ativo = true });

        b.Entity<TipoConteudo>().HasData(
            new TipoConteudo { Id = 1, Codigo = "ARTIGO", Descricao = "Artigo", Ativo = true },
            new TipoConteudo { Id = 2, Codigo = "DICA", Descricao = "Dica", Ativo = true },
            new TipoConteudo { Id = 3, Codigo = "RECEITA", Descricao = "Receita", Ativo = true },
            new TipoConteudo { Id = 4, Codigo = "VIDEO", Descricao = "Vídeo", Ativo = true });

        b.Entity<TipoAlerta>().HasData(
            new TipoAlerta { Id = 1, Codigo = Codigos.Alerta.Glicemia, Descricao = "Glicemia", Ativo = true },
            new TipoAlerta { Id = 2, Codigo = Codigos.Alerta.Medicamento, Descricao = "Medicamento", Ativo = true },
            new TipoAlerta { Id = 3, Codigo = Codigos.Alerta.Refeicao, Descricao = "Refeição", Ativo = true },
            new TipoAlerta { Id = 4, Codigo = Codigos.Alerta.Personalizado, Descricao = "Personalizado", Ativo = true });

        b.Entity<StatusEnvio>().HasData(
            new StatusEnvio { Id = 1, Codigo = Codigos.Status.Pendente, Descricao = "Notificado; aguardando ação do paciente", Ativo = true },
            new StatusEnvio { Id = 2, Codigo = Codigos.Status.Atendido, Descricao = "Paciente realizou o registro correspondente", Ativo = true },
            new StatusEnvio { Id = 3, Codigo = Codigos.Status.Reagendado, Descricao = "Paciente pediu novo lembrete em 15 minutos", Ativo = true },
            new StatusEnvio { Id = 4, Codigo = Codigos.Status.Falhou, Descricao = "Não entregue ou expirado pela janela de 2 horas (RN35)", Ativo = true });

        b.Entity<FonteAlimento>().HasData(
            new FonteAlimento { Id = 1, Codigo = Codigos.Fonte.Taco, Descricao = "Tabela TACO / IBGE", Ativo = true },
            new FonteAlimento { Id = 2, Codigo = Codigos.Fonte.Manual, Descricao = "Cadastro manual", Ativo = true });
    }
}
