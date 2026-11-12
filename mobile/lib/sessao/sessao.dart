import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:local_auth/local_auth.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../alertas/ciclo_alertas.dart';
import '../api/cliente_api.dart';
import '../api/modelos.dart';

enum EstadoSessao { carregando, desconectado, bloqueada, trocarSenha, ativa }

/// Credencial do paciente e estado de acesso ao aplicativo.
class Sessao extends ChangeNotifier {
  Sessao({required this.api, required this.ciclo, FlutterSecureStorage? cofre, LocalAuthentication? biometria})
      : _cofre = cofre ?? const FlutterSecureStorage(),
        _biometria = biometria ?? LocalAuthentication() {
    api.aoExpirarSessao = () => encerrar(aviso: 'Sua sessão expirou. Entre novamente.');
  }

  final ClienteApi api;
  final CicloAlertas ciclo;
  final FlutterSecureStorage _cofre;
  final LocalAuthentication _biometria;

  static const _chaveCredencial = 'credencial';
  static const _chaveBiometria = 'biometria_ativa';
  static const _chaveOferta = 'biometria_oferecida';
  static const _chaveInstalado = 'instalacao_iniciada';

  /// O token vale 24 horas e só pode ser renovado enquanto vale. Renovar a cada
  /// uso faz o prazo contar a partir da última abertura do app, e não do login:
  /// quem usa todo dia não precisa digitar a senha de novo.
  static const _renovarAposUso = Duration(hours: 1);

  /// Tempo em segundo plano depois do qual a biometria é exigida de novo.
  static const _tempoParaBloquear = Duration(minutes: 5);

  EstadoSessao estado = EstadoSessao.carregando;
  Credencial? credencial;
  String? aviso;

  bool biometriaAtiva = false;
  DateTime? _saiuEm;
  DateTime? _ultimaRenovacao;

  int get pacienteId => credencial!.usuarioId;

  void _mudar(EstadoSessao novo) {
    estado = novo;
    notifyListeners();
  }

  Future<void> iniciar() async {
    // O Keychain do iOS sobrevive à desinstalação, e as preferências, não. Sem esta
    // limpeza, quem reinstala herda a sessão de dados de saúde do dono anterior.
    final preferencias = await SharedPreferences.getInstance();
    if (preferencias.getBool(_chaveInstalado) != true) {
      await _cofre.deleteAll();
      await preferencias.setBool(_chaveInstalado, true);
    }

    final salva = await _cofre.read(key: _chaveCredencial);
    biometriaAtiva = await _cofre.read(key: _chaveBiometria) == 'true';

    if (salva == null) return _mudar(EstadoSessao.desconectado);

    final guardada = Credencial.deJson(jsonDecode(salva) as Map<String, dynamic>);
    if (guardada.expiraEm.isBefore(DateTime.now())) {
      await _cofre.delete(key: _chaveCredencial);
      aviso = 'Sua sessão expirou. Entre novamente.';
      return _mudar(EstadoSessao.desconectado);
    }

    credencial = guardada;
    api.token = guardada.token;

    // RN03 vem antes de tudo: nenhuma outra tela abre com a senha provisória.
    if (guardada.senhaProvisoria) return _mudar(EstadoSessao.trocarSenha);
    if (biometriaAtiva) return _mudar(EstadoSessao.bloqueada);
    await _liberar();
  }

  Future<void> _liberar() async {
    _mudar(EstadoSessao.ativa);
    await _renovarSeConvem();
    // RN35 — a verificação dos alertas acontece ao iniciar a sessão no app.
    await ciclo.iniciar(pacienteId);
  }

  Future<void> entrar(String email, String senha) async {
    final recebida = await api.entrar(email, senha);

    // A4 do UC001 — paciente vai ao app; nutricionista e administrador, à web.
    if (recebida.perfil != 'PACIENTE') {
      throw const ErroApi('Este aplicativo é para pacientes. Nutricionistas e administradores acessam pelo sistema web.');
    }

    await _guardar(recebida);
    aviso = null;
    if (recebida.senhaProvisoria) return _mudar(EstadoSessao.trocarSenha);
    await _liberar();
  }

  Future<void> _guardar(Credencial nova) async {
    credencial = nova;
    api.token = nova.token;
    _ultimaRenovacao = DateTime.now();
    await _cofre.write(key: _chaveCredencial, value: jsonEncode(nova.paraJson()));
  }

  /// RN03 — a API só devolve uma credencial sem a restrição depois de um novo
  /// login com a senha definitiva; o token em mãos ainda carrega a marca antiga.
  Future<void> alterarSenha(String atual, String nova) async {
    await api.alterarSenha(atual, nova);
    await entrar(credencial!.email, nova);
  }

  Future<void> _renovarSeConvem() async {
    final ultima = _ultimaRenovacao;
    if (ultima != null && DateTime.now().difference(ultima) < _renovarAposUso) return;
    try {
      await _guardar(await api.renovar());
    } on ErroApi {
      // Sem rede o token atual segue valendo; um 401 já derruba a sessão sozinho.
    }
  }

  void aoIrParaSegundoPlano() => _saiuEm = DateTime.now();

  Future<void> aoVoltar() async {
    if (estado != EstadoSessao.ativa) return;

    final saiu = _saiuEm;
    if (biometriaAtiva && saiu != null && DateTime.now().difference(saiu) >= _tempoParaBloquear) {
      return _mudar(EstadoSessao.bloqueada);
    }
    await _renovarSeConvem();
    await ciclo.conciliar();
  }

  // ── RN01 — biometria ──────────────────────────────────────────────────────

  Future<bool> biometriaDisponivel() async {
    try {
      return await _biometria.canCheckBiometrics && (await _biometria.getAvailableBiometrics()).isNotEmpty;
    } on Exception {
      return false;
    }
  }

  Future<bool> _confirmarBiometria(String motivo) async {
    try {
      return await _biometria.authenticate(localizedReason: motivo, biometricOnly: true);
    } on Exception {
      return false;
    }
  }

  /// RN01 — a oferta aparece uma vez, depois do primeiro acesso bem-sucedido.
  Future<bool> deveOferecerBiometria() async {
    if (biometriaAtiva || await _cofre.read(key: _chaveOferta) == 'true') return false;
    return biometriaDisponivel();
  }

  Future<void> marcarBiometriaOferecida() => _cofre.write(key: _chaveOferta, value: 'true');

  Future<bool> definirBiometria(bool ativar) async {
    if (ativar && !await _confirmarBiometria('Confirme para ativar o acesso por biometria')) return false;
    biometriaAtiva = ativar;
    await _cofre.write(key: _chaveBiometria, value: '$ativar');
    notifyListeners();
    return true;
  }

  Future<void> desbloquear() async {
    if (await _confirmarBiometria('Confirme para abrir o GlicoNutri')) await _liberar();
  }

  /// Abandona o desbloqueio: a biometria é alternativa de acesso, nunca a única
  /// porta, e volta a exigir e-mail e senha.
  Future<void> usarSenha() => encerrar();

  Future<void> encerrar({String? aviso}) async {
    // Sair não pode deixar credencial nem biometria para a próxima pessoa.
    await _cofre.delete(key: _chaveCredencial);
    await _cofre.delete(key: _chaveBiometria);
    biometriaAtiva = false;
    await ciclo.encerrar();
    api.token = null;
    credencial = null;
    this.aviso = aviso;
    _mudar(EstadoSessao.desconectado);
  }
}
