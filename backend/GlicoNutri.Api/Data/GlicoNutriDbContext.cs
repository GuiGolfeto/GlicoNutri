using System.Linq.Expressions;
using System.Text;
using GlicoNutri.Api.Models;
using GlicoNutri.Api.Models.Referencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GlicoNutri.Api.Data;

public class GlicoNutriDbContext(DbContextOptions<GlicoNutriDbContext> options) : DbContext(options)
{
    // --- Identidade (herança TPT) ---
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Nutricionista> Nutricionistas => Set<Nutricionista>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Administrador> Administradores => Set<Administrador>();
    public DbSet<NutricionistaPaciente> NutricionistaPaciente => Set<NutricionistaPaciente>();

    // --- Plano alimentar e nutrição ---
    public DbSet<PlanoAlimentar> PlanosAlimentares => Set<PlanoAlimentar>();
    public DbSet<DistribuicaoMacronutrientes> DistribuicaoMacronutrientes => Set<DistribuicaoMacronutrientes>();
    public DbSet<ItemPlanoAlimentar> ItensPlanoAlimentar => Set<ItemPlanoAlimentar>();
    public DbSet<Alimento> Alimentos => Set<Alimento>();
    public DbSet<NecessidadeEnergetica> NecessidadesEnergeticas => Set<NecessidadeEnergetica>();

    // --- Registros clínicos ---
    public DbSet<RegistroGlicemia> RegistrosGlicemia => Set<RegistroGlicemia>();
    public DbSet<RegistroAntropometrico> RegistrosAntropometricos => Set<RegistroAntropometrico>();
    public DbSet<RegistroEmocional> RegistrosEmocionais => Set<RegistroEmocional>();

    // --- Alertas ---
    public DbSet<Alerta> Alertas => Set<Alerta>();
    public DbSet<HistoricoAlerta> HistoricoAlertas => Set<HistoricoAlerta>();

    // --- Conteúdo educativo ---
    public DbSet<ConteudoEducativo> ConteudosEducativos => Set<ConteudoEducativo>();
    public DbSet<Receita> Receitas => Set<Receita>();
    public DbSet<IngredienteReceita> IngredientesReceita => Set<IngredienteReceita>();

    // --- Read model do dashboard ---
    public DbSet<ResumoClinicoPaciente> ResumoClinicoPaciente => Set<ResumoClinicoPaciente>();

    // --- Tabelas de referência (ex-enums, Diagrama de Classes V5.0) ---
    public DbSet<SexoBiologico> SexosBiologicos => Set<SexoBiologico>();
    public DbSet<TipoDiabetes> TiposDiabetes => Set<TipoDiabetes>();
    public DbSet<PerfilUsuario> PerfisUsuario => Set<PerfilUsuario>();
    public DbSet<ContextoGlicemia> ContextosGlicemia => Set<ContextoGlicemia>();
    public DbSet<EstadoEmocional> EstadosEmocionais => Set<EstadoEmocional>();
    public DbSet<FormulaEnergetica> FormulasEnergeticas => Set<FormulaEnergetica>();
    public DbSet<NivelAtividade> NiveisAtividade => Set<NivelAtividade>();
    public DbSet<TipoConteudo> TiposConteudo => Set<TipoConteudo>();
    public DbSet<TipoAlerta> TiposAlerta => Set<TipoAlerta>();
    public DbSet<StatusEnvio> StatusEnvio => Set<StatusEnvio>();
    public DbSet<FonteAlimento> FontesAlimento => Set<FonteAlimento>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        ConfigurarIdentidade(b);
        ConfigurarNutricao(b);
        ConfigurarClinico(b);
        ConfigurarAlertas(b);
        ConfigurarConteudo(b);
        ConfigurarReadModel(b);
        ConfigurarReferencia(b);
        SeedReferencia.Aplicar(b);

        AplicarSoftDelete(b);
        AplicarNomesSnakeCase(b);
        RenomearPkDosSubtiposTpt(b);
    }

    // ════════════════════════════════════════════════════════════════════
    //  IDENTIDADE
    // ════════════════════════════════════════════════════════════════════
    private static void ConfigurarIdentidade(ModelBuilder b)
    {
        b.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");
            e.UseTptMappingStrategy();

            e.Property(x => x.Nome).HasMaxLength(150).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.Property(x => x.SenhaHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.DataCadastro).HasDefaultValueSql("now()");

            // RN06 — e-mail é o identificador único de autenticação, em qualquer perfil.
            e.HasIndex(x => x.Email).IsUnique();

            e.HasOne(x => x.Perfil)
             .WithMany()
             .HasForeignKey(x => x.PerfilId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Nos subtipos a PK compartilhada chama-se usuario_id, como no DER V2.0.
        b.Entity<Nutricionista>(e =>
        {
            e.ToTable("nutricionistas");
            e.Property(x => x.Crn).HasMaxLength(20).IsRequired();
            e.Property(x => x.Especialidade).HasMaxLength(100);
            e.Property(x => x.Telefone).HasMaxLength(20);
            e.HasIndex(x => x.Crn).IsUnique();
        });

        b.Entity<Paciente>(e =>
        {
            e.ToTable("pacientes");
            e.Property(x => x.Cpf).HasMaxLength(14).IsRequired();
            e.Property(x => x.Telefone).HasMaxLength(20);
            e.HasIndex(x => x.Cpf).IsUnique();

            e.HasOne(x => x.Sexo).WithMany()
             .HasForeignKey(x => x.SexoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TipoDiabetes).WithMany()
             .HasForeignKey(x => x.TipoDiabetesId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Administrador>(e =>
        {
            e.ToTable("administradores");
            e.Property(x => x.NivelAcesso).HasDefaultValue(1);
        });

        b.Entity<NutricionistaPaciente>(e =>
        {
            e.ToTable("nutricionista_paciente");
            e.HasKey(x => new { x.NutricionistaId, x.PacienteId });
            e.Property(x => x.DataVinculo).HasDefaultValueSql("now()");

            e.HasOne(x => x.Nutricionista).WithMany(x => x.Vinculos)
             .HasForeignKey(x => x.NutricionistaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Paciente).WithMany(x => x.Vinculos)
             .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    // ════════════════════════════════════════════════════════════════════
    //  NUTRIÇÃO
    // ════════════════════════════════════════════════════════════════════
    private static void ConfigurarNutricao(ModelBuilder b)
    {
        b.Entity<Alimento>(e =>
        {
            e.ToTable("alimentos");
            e.Property(x => x.Nome).HasMaxLength(200).IsRequired();
            e.Property(x => x.GrupoAlimentar).HasMaxLength(100);

            // RN11 — a importação não pode duplicar alimento já existente pelo nome.
            e.HasIndex(x => x.Nome).IsUnique();

            e.HasOne(x => x.Fonte).WithMany()
             .HasForeignKey(x => x.FonteId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<PlanoAlimentar>(e =>
        {
            e.ToTable("planos_alimentares");
            e.Property(x => x.Objetivo).HasMaxLength(255);

            e.HasOne(x => x.Paciente).WithMany(x => x.Planos)
             .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Nutricionista).WithMany(x => x.PlanosElaborados)
             .HasForeignKey(x => x.NutricionistaId).OnDelete(DeleteBehavior.Restrict);

            // RN16 — no máximo um plano ativo por paciente, garantido no banco por
            // índice único parcial, e não apenas pela camada de serviço.
            e.HasIndex(x => x.PacienteId)
             .HasFilter("ativo = true")
             .IsUnique()
             .HasDatabaseName("ix_planos_alimentares_paciente_ativo_unico");
        });

        b.Entity<DistribuicaoMacronutrientes>(e =>
        {
            e.ToTable("distribuicao_macronutrientes");
            e.HasOne(x => x.PlanoAlimentar).WithOne(x => x.Distribuicao)
             .HasForeignKey<DistribuicaoMacronutrientes>(x => x.PlanoAlimentarId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.PlanoAlimentarId).IsUnique();
        });

        b.Entity<ItemPlanoAlimentar>(e =>
        {
            e.ToTable("itens_plano_alimentar");
            e.Property(x => x.Refeicao).HasMaxLength(50);
            e.Property(x => x.Unidade).HasMaxLength(20);

            e.HasOne(x => x.PlanoAlimentar).WithMany(x => x.Itens)
             .HasForeignKey(x => x.PlanoAlimentarId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Alimento).WithMany(x => x.ItensPlano)
             .HasForeignKey(x => x.AlimentoId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<NecessidadeEnergetica>(e =>
        {
            e.ToTable("necessidades_energeticas");
            e.Property(x => x.Objetivo).HasMaxLength(100);

            e.HasOne(x => x.Paciente).WithMany(x => x.NecessidadesEnergeticas)
             .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Formula).WithMany()
             .HasForeignKey(x => x.FormulaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NivelAtividade).WithMany()
             .HasForeignKey(x => x.NivelAtividadeId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    // ════════════════════════════════════════════════════════════════════
    //  REGISTROS CLÍNICOS
    // ════════════════════════════════════════════════════════════════════
    private static void ConfigurarClinico(ModelBuilder b)
    {
        b.Entity<RegistroGlicemia>(e =>
        {
            e.ToTable("registros_glicemia", t => t.HasCheckConstraint(
                "ck_registros_glicemia_valor", "valor >= 0 AND valor <= 600"));

            e.HasOne(x => x.Paciente).WithMany(x => x.RegistrosGlicemia)
             .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Contexto).WithMany()
             .HasForeignKey(x => x.ContextoId).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.PacienteId, x.DataHora });
        });

        b.Entity<RegistroAntropometrico>(e =>
        {
            e.ToTable("registros_antropometricos");
            e.Property(x => x.ClassificacaoImc).HasMaxLength(50);

            e.HasOne(x => x.Paciente).WithMany(x => x.RegistrosAntropometricos)
             .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.PacienteId, x.DataHora });
        });

        b.Entity<RegistroEmocional>(e =>
        {
            e.ToTable("registros_emocionais", t => t.HasCheckConstraint(
                "ck_registros_emocionais_intensidade", "intensidade BETWEEN 1 AND 5"));

            e.HasOne(x => x.Paciente).WithMany(x => x.RegistrosEmocionais)
             .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.EstadoEmocional).WithMany()
             .HasForeignKey(x => x.EstadoEmocionalId).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.PacienteId, x.DataHora });
        });
    }

    // ════════════════════════════════════════════════════════════════════
    //  ALERTAS
    // ════════════════════════════════════════════════════════════════════
    private static void ConfigurarAlertas(ModelBuilder b)
    {
        b.Entity<Alerta>(e =>
        {
            e.ToTable("alertas");
            e.Property(x => x.DiasSemana).HasMaxLength(20);

            e.HasOne(x => x.Paciente).WithMany(x => x.Alertas)
             .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Tipo).WithMany()
             .HasForeignKey(x => x.TipoId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<HistoricoAlerta>(e =>
        {
            e.ToTable("historico_alertas");

            e.HasOne(x => x.Alerta).WithMany(x => x.Historico)
             .HasForeignKey(x => x.AlertaId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.StatusEnvio).WithMany()
             .HasForeignKey(x => x.StatusEnvioId).OnDelete(DeleteBehavior.Restrict);

            // RN35 — a varredura de expiração busca PENDENTE por data de disparo.
            e.HasIndex(x => new { x.StatusEnvioId, x.DataHoraDisparo });
        });
    }

    // ════════════════════════════════════════════════════════════════════
    //  CONTEÚDO EDUCATIVO
    // ════════════════════════════════════════════════════════════════════
    private static void ConfigurarConteudo(ModelBuilder b)
    {
        b.Entity<ConteudoEducativo>(e =>
        {
            e.ToTable("conteudos_educativos");
            e.Property(x => x.Titulo).HasMaxLength(200).IsRequired();

            e.HasOne(x => x.Autor).WithMany(x => x.ConteudosPublicados)
             .HasForeignKey(x => x.AutorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Tipo).WithMany()
             .HasForeignKey(x => x.TipoId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Receita>(e =>
        {
            e.ToTable("receitas");
            e.Property(x => x.Nome).HasMaxLength(200).IsRequired();

            e.HasOne(x => x.Nutricionista).WithMany(x => x.Receitas)
             .HasForeignKey(x => x.NutricionistaId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<IngredienteReceita>(e =>
        {
            e.ToTable("ingredientes_receita");
            e.Property(x => x.Unidade).HasMaxLength(20);

            e.HasOne(x => x.Receita).WithMany(x => x.Ingredientes)
             .HasForeignKey(x => x.ReceitaId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Alimento).WithMany(x => x.Ingredientes)
             .HasForeignKey(x => x.AlimentoId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    // ════════════════════════════════════════════════════════════════════
    //  READ MODEL
    // ════════════════════════════════════════════════════════════════════
    private static void ConfigurarReadModel(ModelBuilder b)
    {
        b.Entity<ResumoClinicoPaciente>(e =>
        {
            e.ToTable("resumo_clinico_paciente");
            e.HasKey(x => x.PacienteId);
            e.Property(x => x.PacienteId).ValueGeneratedNever();
            e.Property(x => x.UltimaClassificacaoImc).HasMaxLength(50);
            e.Property(x => x.DataAtualizacao).HasDefaultValueSql("now()");

            e.HasOne(x => x.Paciente).WithOne(x => x.Resumo)
             .HasForeignKey<ResumoClinicoPaciente>(x => x.PacienteId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.UltimaGlicemiaContexto).WithMany()
             .HasForeignKey(x => x.UltimaGlicemiaContextoId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    // ════════════════════════════════════════════════════════════════════
    //  TABELAS DE REFERÊNCIA
    // ════════════════════════════════════════════════════════════════════
    private static void ConfigurarReferencia(ModelBuilder b)
    {
        ConfigurarUma<SexoBiologico>(b, "sexos_biologicos");
        ConfigurarUma<TipoDiabetes>(b, "tipos_diabetes");
        ConfigurarUma<PerfilUsuario>(b, "perfis_usuario");
        ConfigurarUma<ContextoGlicemia>(b, "contextos_glicemia");
        ConfigurarUma<EstadoEmocional>(b, "estados_emocionais");
        ConfigurarUma<FormulaEnergetica>(b, "formulas_energeticas");
        ConfigurarUma<NivelAtividade>(b, "niveis_atividade");
        ConfigurarUma<TipoConteudo>(b, "tipos_conteudo");
        ConfigurarUma<TipoAlerta>(b, "tipos_alerta");
        ConfigurarUma<StatusEnvio>(b, "status_envio");
        ConfigurarUma<FonteAlimento>(b, "fontes_alimento");
    }

    private static void ConfigurarUma<T>(ModelBuilder b, string tabela) where T : class, IEntidadeReferencia
    {
        b.Entity<T>(e =>
        {
            e.ToTable(tabela);
            e.HasKey(x => x.Id);
            e.Property(x => x.Codigo).HasMaxLength(50).IsRequired();
            e.Property(x => x.Descricao).HasMaxLength(150).IsRequired();
            e.Property(x => x.Ativo).HasDefaultValue(true);
            e.HasIndex(x => x.Codigo).IsUnique();
        });
    }

    // ════════════════════════════════════════════════════════════════════
    //  CONVENÇÕES GLOBAIS
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Filtro global de soft delete. Aplicado exatamente às entidades listadas na
    /// legenda do DER V2.0 — historico_alertas, distribuicao_macronutrientes,
    /// ingredientes_receita e o read model ficam de fora, por não terem "ativo".
    /// Numa hierarquia TPT o filtro só pode ser declarado na raiz (Usuario).
    /// </summary>
    private static void AplicarSoftDelete(ModelBuilder b)
    {
        b.Entity<Usuario>().HasQueryFilter(x => x.Ativo);
        b.Entity<NutricionistaPaciente>().HasQueryFilter(x => x.Ativo);
        b.Entity<PlanoAlimentar>().HasQueryFilter(x => x.Ativo);
        b.Entity<ItemPlanoAlimentar>().HasQueryFilter(x => x.Ativo);
        b.Entity<Receita>().HasQueryFilter(x => x.Ativo);
        b.Entity<RegistroGlicemia>().HasQueryFilter(x => x.Ativo);
        b.Entity<RegistroAntropometrico>().HasQueryFilter(x => x.Ativo);
        b.Entity<RegistroEmocional>().HasQueryFilter(x => x.Ativo);
        b.Entity<NecessidadeEnergetica>().HasQueryFilter(x => x.Ativo);
        b.Entity<Alerta>().HasQueryFilter(x => x.Ativo);
        b.Entity<ConteudoEducativo>().HasQueryFilter(x => x.Ativo);

        // Alimento fica deliberadamente FORA do filtro global. A RN12 é específica:
        // alimentos inativados "não devem aparecer nas buscas do sistema, mas seus
        // dados devem ser preservados para garantir a integridade histórica dos
        // planos alimentares que os referenciam". Um filtro global esconderia o
        // alimento também quando alcançado por um item de plano ou ingrediente de
        // receita, quebrando justamente o histórico que a regra manda preservar.
        // A exclusão é aplicada nas consultas de busca e listagem do AlimentoService.

        // Dependentes sem coluna "ativo" própria no DER V2.0 herdam o filtro do
        // principal, senão sobreviveriam ao soft delete de quem os governa.
        b.Entity<DistribuicaoMacronutrientes>().HasQueryFilter(x => x.PlanoAlimentar.Ativo);
        b.Entity<HistoricoAlerta>().HasQueryFilter(x => x.Alerta.Ativo);
        b.Entity<IngredienteReceita>().HasQueryFilter(x => x.Receita.Ativo);
        b.Entity<ResumoClinicoPaciente>().HasQueryFilter(x => x.Paciente.Ativo);
    }

    /// <summary>
    /// Converte tabelas, colunas, índices e chaves para snake_case, para que o
    /// schema físico saia idêntico ao nomeado no DER V2.0.
    /// </summary>
    private static void AplicarNomesSnakeCase(ModelBuilder b)
    {
        foreach (var entidade in b.Model.GetEntityTypes())
        {
            var tabela = entidade.GetTableName();
            if (tabela is not null && tabela != ParaSnakeCase(tabela))
                entidade.SetTableName(ParaSnakeCase(tabela));

            var objeto = StoreObjectIdentifier.Table(
                entidade.GetTableName()!, entidade.GetSchema());

            foreach (var propriedade in entidade.GetProperties())
            {
                // Só renomeia o que ainda usa o nome convencional: colunas já
                // configuradas à mão (usuario_id, por exemplo) são preservadas.
                var atual = propriedade.GetColumnName(objeto);
                if (atual is not null && atual == propriedade.Name)
                    propriedade.SetColumnName(ParaSnakeCase(propriedade.Name));
            }

            // As chaves primárias ficam com o nome padrão do EF: numa hierarquia TPT
            // os quatro subtipos compartilham o mesmo objeto de chave, e renomeá-lo
            // daria a todas as tabelas o mesmo nome de constraint. O DER V2.0 nomeia
            // tabelas e colunas, não constraints.
            foreach (var fk in entidade.GetForeignKeys())
                fk.SetConstraintName(ParaSnakeCase(fk.GetConstraintName()!));

            foreach (var indice in entidade.GetIndexes())
                indice.SetDatabaseName(ParaSnakeCase(indice.GetDatabaseName()!));
        }
    }

    /// <summary>
    /// No DER V2.0 a PK da tabela base chama-se "id", e a dos subtipos "usuario_id"
    /// (PK e FK ao mesmo tempo). Como em TPT os quatro tipos compartilham a mesma
    /// propriedade Id, o nome precisa ser definido por tabela — HasColumnName no
    /// subtipo renomearia a coluna da base junto.
    /// </summary>
    private static void RenomearPkDosSubtiposTpt(ModelBuilder b)
    {
        (Type Tipo, string Tabela)[] subtipos =
        [
            (typeof(Nutricionista), "nutricionistas"),
            (typeof(Paciente), "pacientes"),
            (typeof(Administrador), "administradores"),
        ];

        foreach (var (tipo, tabela) in subtipos)
        {
            var entidade = b.Model.FindEntityType(tipo)!;
            var pk = entidade.FindPrimaryKey()!.Properties[0];
            pk.SetColumnName("usuario_id", StoreObjectIdentifier.Table(tabela));
        }
    }

    private static string ParaSnakeCase(string nome)
    {
        var sb = new StringBuilder(nome.Length + 8);
        for (var i = 0; i < nome.Length; i++)
        {
            var c = nome[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && nome[i - 1] != '_' &&
                    (!char.IsUpper(nome[i - 1]) || (i + 1 < nome.Length && char.IsLower(nome[i + 1]))))
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
