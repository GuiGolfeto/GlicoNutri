import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart' as http;

import 'modelos.dart';

/// Falha devolvida pela API ou pela rede, já com a mensagem que a tela mostra.
class ErroApi implements Exception {
  const ErroApi(this.mensagem, {this.status, this.segundosBloqueado});

  final String mensagem;
  final int? status;

  /// RN02 — quanto falta para o desbloqueio automático da conta.
  final int? segundosBloqueado;

  @override
  String toString() => mensagem;
}

/// Acesso à API REST. Os clientes nunca falam com o banco direto: autenticação,
/// RBAC e regras de negócio são da API.
class ClienteApi {
  ClienteApi({required this.endereco, http.Client? cliente}) : _cliente = cliente ?? http.Client();

  final String endereco;
  final http.Client _cliente;

  String? token;

  /// Chamado quando um 401 revela token expirado ou inválido.
  void Function()? aoExpirarSessao;

  static const _limite = Duration(seconds: 25);

  Future<Object?> _enviar(
    String metodo,
    String rota, {
    Object? corpo,
    Map<String, String>? parametros,
  }) async {
    final url = Uri.parse('$endereco$rota').replace(queryParameters: parametros);
    final pedido = http.Request(metodo, url);
    pedido.headers['Accept'] = 'application/json';
    if (token != null) pedido.headers['Authorization'] = 'Bearer $token';
    if (corpo != null) {
      pedido.headers['Content-Type'] = 'application/json';
      pedido.body = jsonEncode(corpo);
    }

    final http.Response resposta;
    try {
      resposta = await http.Response.fromStream(await _cliente.send(pedido).timeout(_limite));
    } on TimeoutException {
      throw const ErroApi('O servidor demorou para responder. Tente novamente.');
    } on SocketException {
      throw const ErroApi('Sem conexão com o servidor. Verifique sua internet.');
    } on http.ClientException {
      throw const ErroApi('Sem conexão com o servidor. Verifique sua internet.');
    }

    final dados = resposta.body.isEmpty ? null : _decodificar(resposta.body);
    if (resposta.statusCode >= 200 && resposta.statusCode < 300) return dados;

    // O login devolve 401 para senha errada: isso não é sessão expirada.
    if (resposta.statusCode == 401 && !rota.startsWith('/auth/login')) {
      aoExpirarSessao?.call();
    }
    throw _erroDe(resposta.statusCode, dados);
  }

  static Object? _decodificar(String corpo) {
    try {
      return jsonDecode(corpo);
    } on FormatException {
      return null;
    }
  }

  static ErroApi _erroDe(int status, Object? dados) {
    var mensagem = status == 403
        ? 'Você não tem permissão para esta ação.'
        : 'Não foi possível concluir a operação.';
    int? segundos;

    if (dados is Map<String, dynamic>) {
      segundos = dados['segundosRestantes'] as int?;
      final erros = dados['errors'];
      if (dados['mensagem'] is String) {
        mensagem = dados['mensagem'] as String;
      } else if (erros is Map && erros.isNotEmpty) {
        final primeiro = erros.values.first;
        if (primeiro is List && primeiro.isNotEmpty) mensagem = primeiro.first as String;
      }
    }
    return ErroApi(mensagem, status: status, segundosBloqueado: segundos);
  }

  Future<List<T>> _lista<T>(String rota, T Function(Map<String, dynamic>) ler, {Map<String, String>? parametros}) async {
    final dados = await _enviar('GET', rota, parametros: parametros) as List;
    return dados.map((e) => ler(e as Map<String, dynamic>)).toList();
  }

  static Map<String, String> _soPreenchidos(Map<String, String?> mapa) => {
        for (final e in mapa.entries)
          if (e.value != null && e.value!.isNotEmpty) e.key: e.value!,
      };

  // ── UC001 — Autenticação ──────────────────────────────────────────────────

  Future<Credencial> entrar(String email, String senha) async => Credencial.deJson(
        await _enviar('POST', '/auth/login', corpo: {'email': email, 'senha': senha}) as Map<String, dynamic>,
      );

  /// RN03 do UC001 — renova o token enquanto ele ainda vale.
  Future<Credencial> renovar() async =>
      Credencial.deJson(await _enviar('POST', '/auth/renovar') as Map<String, dynamic>);

  Future<void> alterarSenha(String atual, String nova) =>
      _enviar('POST', '/auth/alterar-senha', corpo: {'senhaAtual': atual, 'novaSenha': nova});

  Future<void> recuperarSenha(String email) => _enviar('POST', '/auth/recuperar-senha', corpo: {'email': email});

  // ── Referências ───────────────────────────────────────────────────────────

  Future<List<Referencia>> referencias(String nome) => _lista('/referencias/$nome', Referencia.deJson);

  // ── UC004 / UC005 — Glicemia ──────────────────────────────────────────────

  Future<RegistroGlicemia> registrarGlicemia(
    int pacienteId, {
    required int valor,
    required int contextoId,
    required DateTime dataHora,
    String? observacao,
  }) async =>
      RegistroGlicemia.deJson(await _enviar('POST', '/pacientes/$pacienteId/glicemia', corpo: {
        'valor': valor,
        'contextoId': contextoId,
        'dataHora': dataHora.toUtc().toIso8601String(),
        'observacao': observacao,
      }) as Map<String, dynamic>);

  Future<HistoricoGlicemico> historicoGlicemia(int pacienteId, {int dias = 7}) async => HistoricoGlicemico.deJson(
        await _enviar('GET', '/pacientes/$pacienteId/glicemia', parametros: {'dias': '$dias'}) as Map<String, dynamic>,
      );

  // ── UC012 — Emoções ───────────────────────────────────────────────────────

  Future<void> registrarEmocao(
    int pacienteId, {
    required List<int> estados,
    required int intensidade,
    required DateTime dataHora,
    String? descricao,
  }) =>
      _enviar('POST', '/pacientes/$pacienteId/emocoes', corpo: {
        'estadosEmocionaisIds': estados,
        'intensidade': intensidade,
        'dataHora': dataHora.toUtc().toIso8601String(),
        'descricao': descricao,
      });

  Future<List<RegistroEmocional>> diarioEmocional(int pacienteId, {int dias = 30}) async {
    final dados = await _enviar('GET', '/pacientes/$pacienteId/emocoes', parametros: {'dias': '$dias'})
        as Map<String, dynamic>;
    return (dados['registros'] as List).map((e) => RegistroEmocional.deJson(e as Map<String, dynamic>)).toList();
  }

  // ── UC008 — Medidas ───────────────────────────────────────────────────────

  Future<List<RegistroAntropometrico>> medidas(int pacienteId) =>
      _lista('/pacientes/$pacienteId/antropometria', RegistroAntropometrico.deJson);

  // ── UC007 — Plano ─────────────────────────────────────────────────────────

  /// Sem plano ativo a API responde 204, e o retorno é nulo.
  Future<PlanoAlimentar?> planoAtivo(int pacienteId) async {
    final dados = await _enviar('GET', '/pacientes/$pacienteId/plano-alimentar');
    return dados == null ? null : PlanoAlimentar.deJson(dados as Map<String, dynamic>);
  }

  // ── UC010 — Alertas ───────────────────────────────────────────────────────

  Future<List<Alerta>> alertas(int pacienteId) => _lista('/pacientes/$pacienteId/alertas', Alerta.deJson);

  /// RN23 — a única ação do módulo permitida ao paciente.
  Future<void> desativarAlerta(int pacienteId, int alertaId) =>
      _enviar('DELETE', '/pacientes/$pacienteId/alertas/$alertaId');

  /// RN27 — o aplicativo informa cada disparo e seu desfecho.
  Future<void> registrarDisparo(
    int pacienteId, {
    required int alertaId,
    required DateTime dataHora,
    required String status,
    String? mensagemErro,
  }) =>
      _enviar('POST', '/pacientes/$pacienteId/alertas/disparos', corpo: {
        'alertaId': alertaId,
        'dataHoraDisparo': dataHora.toUtc().toIso8601String(),
        'status': status,
        'mensagemErro': mensagemErro,
      });

  /// RN35 — varredura de expiração ao iniciar a sessão.
  Future<void> verificarExpirados(int pacienteId) =>
      _enviar('POST', '/pacientes/$pacienteId/alertas/verificar-expirados');

  // ── RF09 — Repositório ────────────────────────────────────────────────────

  Future<List<Conteudo>> conteudos({String? termo}) =>
      _lista('/conteudos', Conteudo.deJson, parametros: _soPreenchidos({'termo': termo}));

  Future<Conteudo> conteudo(int id) async =>
      Conteudo.deJson(await _enviar('GET', '/conteudos/$id') as Map<String, dynamic>);

  Future<List<Receita>> receitas({String? termo, String? ingrediente}) => _lista('/receitas', Receita.deJson,
      parametros: _soPreenchidos({'termo': termo, 'ingrediente': ingrediente}));

  Future<Receita> receita(int id) async =>
      Receita.deJson(await _enviar('GET', '/receitas/$id') as Map<String, dynamic>);

  // ── RF09.3 — Favoritos ────────────────────────────────────────────────────

  Future<Favoritos> favoritos(int pacienteId) async =>
      Favoritos.deJson(await _enviar('GET', '/pacientes/$pacienteId/favoritos') as Map<String, dynamic>);

  /// [tipo] é `conteudos` ou `receitas`, como na rota.
  Future<void> marcarFavorito(int pacienteId, String tipo, int id, {required bool marcado}) =>
      _enviar(marcado ? 'PUT' : 'DELETE', '/pacientes/$pacienteId/favoritos/$tipo/$id');
}
