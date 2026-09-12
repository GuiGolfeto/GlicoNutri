using System.Linq.Expressions;
using System.Text;
using GlicoNutri.Api.Models;
using GlicoNutri.Api.Models.Referencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GlicoNutri.Api.Data;

public class GlicoNutriDbContext(DbContextOptions<GlicoNutriDbContext> options) : DbContext(options)
{
    // --- Identidade (herança TPH) ---
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
    public DbSet<FavoritoConteudo> FavoritosConteudo => Set<FavoritoConteudo>();
    public DbSet<FavoritoReceita> FavoritosReceita => Set<FavoritoReceita>();

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
    public DbSet<FonteIndiceGlicemico> FontesIndiceGlicemico => Set<FonteIndiceGlicemico>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Busca de alimento sem sensibilidade a acento: um nutricionista digita
        // "feijao" e precisa encontrar "Feijão". Sem isto, metade da tabela TACO
        // fica inalcançável pela busca.
        b.HasPostgresExtension("unaccent");

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
    }

    // ════════════════════════════════════════════════════════════════════
    //  IDENTIDADE
    // ════════════════════════════════════════════════════════════════════
    private static void ConfigurarIdentidade(ModelBuilder b)
    {
        b.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");

            // Questão 1 da orientação — a hierarquia passou de TPT para TPH: uma
            // tabela só, com o perfil dizendo de que tipo é cada linha. No TPT do
            // DER V2.0 qualquer leitura que misturasse dados de usuário e de
            // subtipo custava um JOIN.
            e.UseTphMappingStrategy();

            // O discriminador é o próprio perfil_id, que já era FK obrigatória:
            // uma segunda coluna com o tipo diria o mesmo e poderia divergir dela.
            e.HasDiscriminator(x => x.PerfilId)
             .HasValue<Paciente>(Codigos.IdPerfil.Paciente)
             .HasValue<Nutricionista>(Codigos.IdPerfil.Nutricionista)
             .HasValue<Administrador>(Codigos.IdPerfil.Administrador);

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

        // Sob TPH as colunas dos subtipos são nulas nas linhas dos outros perfis,
        // então a obrigatoriedade não pode mais ser NOT NULL: ela vira CHECK
        // condicionado ao perfil, aplicado na migration.
        b.Entity<Nutricionista>(e =>
        {
            e.Property(x => x.Crn).HasMaxLength(20);
            e.Property(x => x.Especialidade).HasMaxLength(100);

            // Nutricionista e Paciente dividem a coluna telefone. Sem dizer isso, o
            // EF criaria telefone e telefone1 para o mesmo dado.
            e.Property(x => x.Telefone).HasMaxLength(20).HasColumnName("telefone");

            // Nulos não colidem entre si no Postgres, então o índice único continua
            // valendo só para as linhas que têm CRN — as de nutricionista.
            e.HasIndex(x => x.Crn).IsUnique();
        });

        b.Entity<Paciente>(e =>
        {
            e.Property(x => x.Cpf).HasMaxLength(14);
            e.Property(x => x.Telefone).HasMaxLength(20).HasColumnName("telefone");
            e.HasIndex(x => x.Cpf).IsUnique();

            e.HasOne(x => x.Sexo).WithMany()
             .HasForeignKey(x => x.SexoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TipoDiabetes).WithMany()
             .HasForeignKey(x => x.TipoDiabetesId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Administrador>(e =>
        {
            // Sem valor padrão no banco: sob TPH ele preencheria nivel_acesso também
            // nas linhas de paciente e de nutricionista, onde a coluna não significa
            // nada. O padrão 1 vive na propriedade, e só o administrador o grava.
            e.Property(x => x.NivelAcesso);
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

            e.HasOne(x => x.FonteIndiceGlicemico).WithMany()
             .HasForeignKey(x => x.FonteIndiceGlicemicoId).OnDelete(DeleteBehavior.Restrict);
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

        // RF09.3 — favoritos do paciente. A chave é o par, então o mesmo material
        // não entra duas vezes na lista de quem favoritou.
        b.Entity<FavoritoConteudo>(e =>
        {
            e.ToTable("favoritos_conteudo");
            e.HasKey(x => new { x.PacienteId, x.ConteudoId });
            e.Property(x => x.DataFavoritado).HasDefaultValueSql("now()");

            e.HasOne(x => x.Paciente).WithMany()
             .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Conteudo).WithMany()
             .HasForeignKey(x => x.ConteudoId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<FavoritoReceita>(e =>
        {
            e.ToTable("favoritos_receita");
            e.HasKey(x => new { x.PacienteId, x.ReceitaId });
            e.Property(x => x.DataFavoritado).HasDefaultValueSql("now()");

            e.HasOne(x => x.Paciente).WithMany()
             .HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Receita).WithMany()
             .HasForeignKey(x => x.ReceitaId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    // ════════════════════════════════════════════════════════════════════
    //  READ MODEL
    // ════════════════════════════════════════════════════════════════════
    private static void ConfigurarReadModel(ModelBuilder b)
    {
        b.Entity<ResumoClinicoPaciente>(e =>
        {
            // Questão 2 da orientação — deixou de ser tabela e virou VIEW. Enquanto
            // era tabela, guardava dado calculado a partir de outras tabelas, que é
            // a violação de 3FN apontada: media_glicemia_7_dias e ultimo_imc podiam
            // envelhecer sem que nada no banco os corrigisse. A view recalcula na
            // leitura, então não existe versão desatualizada para corrigir.
            e.ToView("resumo_clinico_paciente");

            // ToView sozinho não desfaz o mapeamento de tabela: sem isto o EF
            // continuaria gerando migrations para uma tabela que não existe mais.
            e.ToTable((string?)null);
            e.HasKey(x => x.PacienteId);
            e.Property(x => x.PacienteId).ValueGeneratedNever();
            e.Property(x => x.UltimaClassificacaoImc).HasMaxLength(50);

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
        ConfigurarUma<FonteIndiceGlicemico>(b, "fontes_indice_glicemico");
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
    /// Numa hierarquia TPH o filtro só pode ser declarado na raiz (Usuario).
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
        b.Entity<FavoritoConteudo>().HasQueryFilter(x => x.Conteudo.Ativo && x.Paciente.Ativo);
        b.Entity<FavoritoReceita>().HasQueryFilter(x => x.Receita.Ativo && x.Paciente.Ativo);
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

            // O resumo clínico é view, não tabela: para ele o identificador de
            // armazenamento tem de ser o da view, senão GetTableName volta nulo.
            var objeto = entidade.GetTableName() is { } nomeTabela
                ? StoreObjectIdentifier.Table(nomeTabela, entidade.GetSchema())
                : StoreObjectIdentifier.View(entidade.GetViewName()!, entidade.GetViewSchema());

            foreach (var propriedade in entidade.GetProperties())
            {
                // Só renomeia o que ainda usa o nome convencional: colunas já
                // configuradas à mão (usuario_id, por exemplo) são preservadas.
                var atual = propriedade.GetColumnName(objeto);
                if (atual is not null && atual == propriedade.Name)
                    propriedade.SetColumnName(ParaSnakeCase(propriedade.Name));
            }

            // As chaves primárias ficam com o nome padrão do EF: o DER V2.0 nomeia
            // tabelas e colunas, não constraints.
            // Quem é view não tem constraint nem índice físico, e aí os nomes vêm
            // nulos — não há o que renomear.
            foreach (var fk in entidade.GetForeignKeys())
                if (fk.GetConstraintName() is { } nomeFk)
                    fk.SetConstraintName(ParaSnakeCase(nomeFk));

            foreach (var indice in entidade.GetIndexes())
                if (indice.GetDatabaseName() is { } nomeIndice)
                    indice.SetDatabaseName(ParaSnakeCase(nomeIndice));
        }
    }

    /// <summary>
    /// Converte PascalCase para snake_case reproduzindo os nomes do DER V2.0,
    /// inclusive onde há dígitos:
    ///   CaloriasPor100g       → calorias_por_100g
    ///   MediaGlicemia7Dias    → media_glicemia_7dias
    ///   Horario1              → horario_1
    /// As duas regras que os dígitos impõem são separar a letra do número que a
    /// segue, e nunca separar o número da letra que vem depois dele.
    /// </summary>
    private static string ParaSnakeCase(string nome)
    {
        var sb = new StringBuilder(nome.Length + 8);

        for (var i = 0; i < nome.Length; i++)
        {
            var c = nome[i];
            var anterior = i > 0 ? nome[i - 1] : '\0';
            var jaSeparado = i == 0 || anterior == '_';

            if (char.IsAsciiDigit(c))
            {
                // "Por100" → "por_100", mas o "0" seguinte não separa de novo.
                if (!jaSeparado && char.IsLetter(anterior)) sb.Append('_');
                sb.Append(c);
                continue;
            }

            if (char.IsUpper(c))
            {
                // "7Dias" continua "7dias": o dígito já é a fronteira da palavra.
                var depoisDeDigito = char.IsAsciiDigit(anterior);

                var fimDeSigla = char.IsUpper(anterior)
                                 && i + 1 < nome.Length
                                 && char.IsLower(nome[i + 1]);

                if (!jaSeparado && !depoisDeDigito && (!char.IsUpper(anterior) || fimDeSigla))
                    sb.Append('_');

                sb.Append(char.ToLowerInvariant(c));
                continue;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }
}
