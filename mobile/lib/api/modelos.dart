/// Respostas da API. A API grava e devolve em UTC; toda data é convertida para o
/// fuso do aparelho aqui, para nenhuma tela precisar lembrar disso.
library;

DateTime _data(Object? valor) => DateTime.parse(valor as String).toLocal();
double? _real(Object? valor) => (valor as num?)?.toDouble();

class Credencial {
  const Credencial({
    required this.token,
    required this.expiraEm,
    required this.usuarioId,
    required this.nome,
    required this.email,
    required this.perfil,
    required this.senhaProvisoria,
  });

  final String token;
  final DateTime expiraEm;
  final int usuarioId;
  final String nome;
  final String email;
  final String perfil;

  /// RN03 — enquanto verdadeiro, a única tela permitida é a troca de senha.
  final bool senhaProvisoria;

  String get primeiroNome => nome.split(' ').first;

  factory Credencial.deJson(Map<String, dynamic> j) => Credencial(
        token: j['token'] as String,
        expiraEm: _data(j['expiraEm']),
        usuarioId: j['usuarioId'] as int,
        nome: j['nome'] as String,
        email: j['email'] as String,
        perfil: j['perfil'] as String,
        senhaProvisoria: j['senhaProvisoria'] as bool,
      );

  Map<String, dynamic> paraJson() => {
        'token': token,
        'expiraEm': expiraEm.toUtc().toIso8601String(),
        'usuarioId': usuarioId,
        'nome': nome,
        'email': email,
        'perfil': perfil,
        'senhaProvisoria': senhaProvisoria,
      };
}

class Referencia {
  const Referencia({required this.id, required this.codigo, required this.descricao});

  final int id;
  final String codigo;
  final String descricao;

  factory Referencia.deJson(Map<String, dynamic> j) =>
      Referencia(id: j['id'] as int, codigo: j['codigo'] as String, descricao: j['descricao'] as String);
}

// ── UC004 / UC005 — Glicemia ────────────────────────────────────────────────

class RegistroGlicemia {
  const RegistroGlicemia({
    required this.id,
    required this.valor,
    required this.contexto,
    required this.contextoId,
    required this.dataHora,
    required this.observacao,
    required this.foraDoAlvo,
    required this.classificacao,
    required this.minAlvo,
    required this.maxAlvo,
  });

  final int id;
  final double valor;
  final String contexto;
  final int contextoId;
  final DateTime dataHora;
  final String? observacao;
  final bool foraDoAlvo;

  /// RN02 do UC004 — NORMAL, HIPOGLICEMIA ou HIPERGLICEMIA, decidido pela API.
  final String classificacao;
  final double minAlvo;
  final double maxAlvo;

  factory RegistroGlicemia.deJson(Map<String, dynamic> j) => RegistroGlicemia(
        id: j['id'] as int,
        valor: _real(j['valor'])!,
        contexto: j['contexto'] as String,
        contextoId: j['contextoId'] as int,
        dataHora: _data(j['dataHora']),
        observacao: j['observacao'] as String?,
        foraDoAlvo: j['foraDoAlvo'] as bool,
        classificacao: j['classificacao'] as String,
        minAlvo: _real(j['minAlvoAplicado'])!,
        maxAlvo: _real(j['maxAlvoAplicado'])!,
      );
}

class ResumoGlicemico {
  const ResumoGlicemico({
    required this.total,
    required this.media,
    required this.minimo,
    required this.maximo,
    required this.percentualNoAlvo,
    required this.minAlvo,
    required this.maxAlvo,
  });

  final int total;
  final double? media;
  final double? minimo;
  final double? maximo;
  final double? percentualNoAlvo;
  final double minAlvo;
  final double maxAlvo;

  factory ResumoGlicemico.deJson(Map<String, dynamic> j) => ResumoGlicemico(
        total: j['totalRegistros'] as int,
        media: _real(j['media']),
        minimo: _real(j['minimo']),
        maximo: _real(j['maximo']),
        percentualNoAlvo: _real(j['percentualNoAlvo']),
        minAlvo: _real(j['minAlvoAplicado'])!,
        maxAlvo: _real(j['maxAlvoAplicado'])!,
      );
}

class HistoricoGlicemico {
  const HistoricoGlicemico({required this.resumo, required this.registros});

  final ResumoGlicemico resumo;

  /// RN04 do UC005 — do mais recente para o mais antigo.
  final List<RegistroGlicemia> registros;

  factory HistoricoGlicemico.deJson(Map<String, dynamic> j) => HistoricoGlicemico(
        resumo: ResumoGlicemico.deJson(j['resumo'] as Map<String, dynamic>),
        registros: (j['registros'] as List).map((e) => RegistroGlicemia.deJson(e as Map<String, dynamic>)).toList(),
      );
}

// ── UC012 — Emoções ─────────────────────────────────────────────────────────

class RegistroEmocional {
  const RegistroEmocional({
    required this.id,
    required this.estadoEmocional,
    required this.intensidade,
    required this.dataHora,
    required this.descricao,
  });

  final int id;
  final String estadoEmocional;
  final int intensidade;
  final DateTime dataHora;
  final String? descricao;

  factory RegistroEmocional.deJson(Map<String, dynamic> j) => RegistroEmocional(
        id: j['id'] as int,
        estadoEmocional: j['estadoEmocional'] as String,
        intensidade: j['intensidade'] as int,
        dataHora: _data(j['dataHora']),
        descricao: j['descricao'] as String?,
      );
}

// ── UC008 — Medidas (somente leitura para o paciente) ───────────────────────

class RegistroAntropometrico {
  const RegistroAntropometrico({
    required this.id,
    required this.peso,
    required this.altura,
    required this.circunferenciaAbdominal,
    required this.circunferenciaCintura,
    required this.circunferenciaQuadril,
    required this.circunferenciaBraco,
    required this.imc,
    required this.classificacaoImc,
    required this.dataHora,
  });

  final int id;
  final double peso;

  /// Em centímetros, como o UC008 coleta.
  final double altura;
  final double? circunferenciaAbdominal;
  final double? circunferenciaCintura;
  final double? circunferenciaQuadril;
  final double? circunferenciaBraco;
  final double? imc;
  final String? classificacaoImc;
  final DateTime dataHora;

  factory RegistroAntropometrico.deJson(Map<String, dynamic> j) => RegistroAntropometrico(
        id: j['id'] as int,
        peso: _real(j['peso'])!,
        altura: _real(j['altura'])!,
        circunferenciaAbdominal: _real(j['circunferenciaAbdominal']),
        circunferenciaCintura: _real(j['circunferenciaCintura']),
        circunferenciaQuadril: _real(j['circunferenciaQuadril']),
        circunferenciaBraco: _real(j['circunferenciaBraco']),
        imc: _real(j['imc']),
        classificacaoImc: j['classificacaoImc'] as String?,
        dataHora: _data(j['dataHora']),
      );
}

// ── UC007 — Plano alimentar ─────────────────────────────────────────────────

class ItemPlano {
  const ItemPlano({
    required this.alimentoNome,
    required this.dia,
    required this.horario,
    required this.refeicao,
    required this.quantidade,
    required this.unidade,
    required this.calorias,
  });

  final String alimentoNome;
  final int dia;
  final String? horario;
  final String? refeicao;
  final double quantidade;
  final String? unidade;

  /// Nulo quando a TACO não determina o valor. Nunca é tratado como zero.
  final double? calorias;

  factory ItemPlano.deJson(Map<String, dynamic> j) => ItemPlano(
        alimentoNome: j['alimentoNome'] as String,
        dia: j['dia'] as int,
        horario: j['horario'] as String?,
        refeicao: j['refeicao'] as String?,
        quantidade: _real(j['quantidade'])!,
        unidade: j['unidade'] as String?,
        calorias: _real(j['calorias']),
      );
}

class Distribuicao {
  const Distribuicao({required this.carboidratos, required this.proteinas, required this.lipidios});

  final double carboidratos;
  final double proteinas;
  final double lipidios;

  factory Distribuicao.deJson(Map<String, dynamic> j) => Distribuicao(
        carboidratos: _real(j['carboidratosGramas'])!,
        proteinas: _real(j['proteinasGramas'])!,
        lipidios: _real(j['lipidiosGramas'])!,
      );
}

class PlanoAlimentar {
  const PlanoAlimentar({
    required this.objetivo,
    required this.dataInicio,
    required this.dataFim,
    required this.observacoes,
    required this.calorias,
    required this.distribuicao,
    required this.itens,
  });

  final String? objetivo;
  final DateTime dataInicio;
  final DateTime? dataFim;
  final String? observacoes;
  final double? calorias;
  final Distribuicao? distribuicao;
  final List<ItemPlano> itens;

  factory PlanoAlimentar.deJson(Map<String, dynamic> j) => PlanoAlimentar(
        objetivo: j['objetivo'] as String?,
        // Data de calendário: o fuso não pode empurrá-la para o dia anterior.
        dataInicio: DateTime.parse((j['dataInicio'] as String).substring(0, 10)),
        dataFim: j['dataFim'] == null ? null : DateTime.parse((j['dataFim'] as String).substring(0, 10)),
        observacoes: j['observacoes'] as String?,
        calorias: _real(j['totalCaloriasPrescritas']),
        distribuicao: j['distribuicao'] == null ? null : Distribuicao.deJson(j['distribuicao'] as Map<String, dynamic>),
        itens: (j['itens'] as List).map((e) => ItemPlano.deJson(e as Map<String, dynamic>)).toList(),
      );
}

// ── UC010 — Alertas ─────────────────────────────────────────────────────────

class Alerta {
  const Alerta({
    required this.id,
    required this.tipoId,
    required this.tipo,
    required this.mensagem,
    required this.horario1,
    required this.horario2,
    required this.diasSemana,
    required this.ativo,
  });

  final int id;
  final int tipoId;
  final String tipo;
  final String? mensagem;

  /// "HH:mm:ss", na hora local do paciente.
  final String? horario1;
  final String? horario2;

  /// Máscara de 7 posições começando no domingo; "1111111" é todo dia.
  final String? diasSemana;
  final bool ativo;

  factory Alerta.deJson(Map<String, dynamic> j) => Alerta(
        id: j['id'] as int,
        tipoId: j['tipoId'] as int,
        tipo: j['tipo'] as String,
        mensagem: j['mensagem'] as String?,
        horario1: j['horario1'] as String?,
        horario2: j['horario2'] as String?,
        diasSemana: j['diasSemana'] as String?,
        ativo: j['ativo'] as bool,
      );
}

// ── RF09 — Repositório educativo ────────────────────────────────────────────

class Conteudo {
  const Conteudo({
    required this.id,
    required this.titulo,
    required this.tipo,
    required this.corpo,
    required this.urlMidia,
    required this.autorNome,
  });

  final int id;
  final String titulo;
  final String tipo;
  final String? corpo;
  final String? urlMidia;
  final String autorNome;

  factory Conteudo.deJson(Map<String, dynamic> j) => Conteudo(
        // A listagem de favoritos chama o id de conteudoId.
        id: (j['id'] ?? j['conteudoId']) as int,
        titulo: j['titulo'] as String,
        tipo: j['tipo'] as String,
        corpo: j['corpo'] as String?,
        urlMidia: j['urlMidia'] as String?,
        autorNome: j['autorNome'] as String,
      );
}

class Ingrediente {
  const Ingrediente({required this.alimentoNome, required this.quantidade, required this.unidade});

  final String alimentoNome;
  final double quantidade;
  final String? unidade;

  factory Ingrediente.deJson(Map<String, dynamic> j) => Ingrediente(
        alimentoNome: j['alimentoNome'] as String,
        quantidade: _real(j['quantidade'])!,
        unidade: j['unidade'] as String?,
      );
}

class Receita {
  const Receita({
    required this.id,
    required this.nome,
    required this.descricao,
    required this.tempoPreparo,
    required this.porcoes,
    required this.instrucoes,
    required this.nutricionistaNome,
    required this.ingredientes,
    required this.calorias,
    required this.caloriasPorPorcao,
    required this.carboidratos,
    required this.proteinas,
    required this.lipidios,
    required this.nutricionalCompleta,
  });

  final int id;
  final String nome;
  final String? descricao;
  final int? tempoPreparo;
  final int? porcoes;
  final String? instrucoes;
  final String nutricionistaNome;
  final List<Ingrediente> ingredientes;
  final double calorias;
  final double? caloriasPorPorcao;
  final double carboidratos;
  final double proteinas;
  final double lipidios;

  /// Falso quando algum ingrediente não tem valor determinado na TACO: o total
  /// é um piso, e o paciente precisa saber disso.
  final bool nutricionalCompleta;

  factory Receita.deJson(Map<String, dynamic> j) {
    final n = j['nutricional'] as Map<String, dynamic>;
    return Receita(
      id: j['id'] as int,
      nome: j['nome'] as String,
      descricao: j['descricao'] as String?,
      tempoPreparo: j['tempoPreparo'] as int?,
      porcoes: j['porcoes'] as int?,
      instrucoes: j['instrucoes'] as String?,
      nutricionistaNome: j['nutricionistaNome'] as String,
      ingredientes: (j['ingredientes'] as List).map((e) => Ingrediente.deJson(e as Map<String, dynamic>)).toList(),
      calorias: _real(n['calorias'])!,
      caloriasPorPorcao: _real(n['caloriasPorPorcao']),
      carboidratos: _real(n['carboidratos'])!,
      proteinas: _real(n['proteinas'])!,
      lipidios: _real(n['lipidios'])!,
      nutricionalCompleta: n['completa'] as bool,
    );
  }
}

class FavoritoReceita {
  const FavoritoReceita({
    required this.receitaId,
    required this.nome,
    required this.descricao,
    required this.tempoPreparo,
    required this.caloriasPorPorcao,
  });

  final int receitaId;
  final String nome;
  final String? descricao;
  final int? tempoPreparo;
  final double? caloriasPorPorcao;

  factory FavoritoReceita.deJson(Map<String, dynamic> j) => FavoritoReceita(
        receitaId: j['receitaId'] as int,
        nome: j['nome'] as String,
        descricao: j['descricao'] as String?,
        tempoPreparo: j['tempoPreparo'] as int?,
        caloriasPorPorcao: _real(j['caloriasPorPorcao']),
      );
}

class Favoritos {
  const Favoritos({required this.conteudos, required this.receitas});

  final List<Conteudo> conteudos;
  final List<FavoritoReceita> receitas;

  factory Favoritos.deJson(Map<String, dynamic> j) => Favoritos(
        conteudos: (j['conteudos'] as List).map((e) => Conteudo.deJson(e as Map<String, dynamic>)).toList(),
        receitas: (j['receitas'] as List).map((e) => FavoritoReceita.deJson(e as Map<String, dynamic>)).toList(),
      );
}
