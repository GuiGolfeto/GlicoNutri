import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../api/cliente_api.dart';
import '../api/modelos.dart';
import 'agenda_lembretes.dart';
import 'notificador.dart';

/// UC010 — o ciclo dos lembretes no aparelho (RN25, RN26, RN27 e RN35).
///
/// Não há push: as notificações são locais. Isso significa que o app não roda no
/// instante em que um aviso toca, e a API só fica sabendo do disparo quando o
/// paciente abre o app. Por isso existe a agenda persistida: ela guarda o que foi
/// de fato programado, e [conciliar] informa depois o que aconteceu. Reconstruir
/// os disparos a partir da configuração atual inventaria disparos de um alerta
/// que o nutricionista acabou de criar com horário já passado.
class CicloAlertas extends ChangeNotifier {
  CicloAlertas({
    required ClienteApi api,
    required Notificador notificador,
    DateTime Function()? relogio,
  })  : _api = api, // ignore: prefer_initializing_formals
        _notificador = notificador, // ignore: prefer_initializing_formals
        _agora = relogio ?? DateTime.now;

  final ClienteApi _api;
  final Notificador _notificador;
  final DateTime Function() _agora;

  int? _pacienteId;
  List<Disparo> _agenda = [];
  bool _permitido = true;

  bool _emAndamento = false;
  bool _repetir = false;

  /// RN27 — sem permissão, nenhum aviso chega ao paciente.
  bool get notificacoesPermitidas => _permitido;

  /// Disparos que já tocaram e ainda esperam uma medição.
  List<Disparo> get emAberto {
    final agora = _agora();
    return _agenda.where((d) => d.persistente && d.pendenteEnviado && d.emJanela(agora)).toList();
  }

  Disparo? get proximo {
    final agora = _agora();
    final futuros = _agenda.where((d) => d.ocorrencia.isAfter(agora)).toList()
      ..sort((a, b) => a.ocorrencia.compareTo(b.ocorrencia));
    return futuros.isEmpty ? null : futuros.first;
  }

  String get _chaveAgenda => 'agenda_lembretes_$_pacienteId';

  /// Abre o ciclo de um paciente. Chamado ao iniciar a sessão autenticada.
  Future<void> iniciar(int pacienteId) async {
    _pacienteId = pacienteId;
    final prefs = await SharedPreferences.getInstance();
    final salva = prefs.getString(_chaveAgenda);
    _agenda = salva == null
        ? []
        : (jsonDecode(salva) as List).map((e) => Disparo.deJson(e as Map<String, dynamic>)).toList();
    await conciliar();
  }

  /// Ao sair, nenhum aviso de saúde pode continuar tocando no aparelho, que pode
  /// passar para outra pessoa.
  Future<void> encerrar() async {
    await _notificador.cancelarTodas();
    if (_pacienteId != null) {
      final prefs = await SharedPreferences.getInstance();
      await prefs.remove(_chaveAgenda);
    }
    _pacienteId = null;
    _agenda = [];
    notifyListeners();
  }

  Future<void> pedirPermissao() async {
    _permitido = await _notificador.pedirPermissao();
    await conciliar();
  }

  /// Sincroniza aparelho e API. Roda ao abrir o app, ao voltar para ele e depois
  /// de cada registro; chamadas sobrepostas viram uma nova rodada ao fim da atual.
  Future<void> conciliar() async {
    if (_pacienteId == null) return;
    if (_emAndamento) {
      _repetir = true;
      return;
    }

    _emAndamento = true;
    try {
      do {
        _repetir = false;
        await _rodada(_pacienteId!);
      } while (_repetir && _pacienteId != null);
    } on ErroApi catch (e) {
      // Sem rede, os avisos já programados continuam tocando e a agenda guarda o
      // que falta informar. A próxima abertura retoma daí.
      debugPrint('Conciliação adiada: $e');
    } finally {
      _emAndamento = false;
      notifyListeners();
    }
  }

  Future<void> _rodada(int pacienteId) async {
    _permitido = await _notificador.permissaoConcedida();

    final agora = _agora();
    final registros = (await _api.historicoGlicemia(pacienteId)).registros;

    await _informarDisparos(pacienteId, registros, agora);

    // RN35 — só depois de informar: um PENDENTE antigo enviado agora já precisa
    // sair desta varredura como FALHOU.
    await _api.verificarExpirados(pacienteId);

    final tipos = await _api.referencias('tipos-alerta');
    final persistentes = {for (final t in tipos) if (t.codigo == 'GLICEMIA') t.id};
    final alertas = await _api.alertas(pacienteId);

    await _replanejar(alertas, persistentes, registros, agora);
  }

  /// RN27 — cada horário que passou vira um registro no histórico de alertas.
  Future<void> _informarDisparos(int pacienteId, List<RegistroGlicemia> registros, DateTime agora) async {
    for (final disparo in List.of(_agenda)) {
      if (disparo.ocorrencia.isAfter(agora)) continue;

      if (!disparo.persistente) {
        // Lembrete de refeição ou medicamento não tem registro correspondente:
        // ficar PENDENTE o faria expirar como falha do paciente na RN35, e o
        // nutricionista veria um descumprimento que não existe.
        await _api.registrarDisparo(pacienteId, alertaId: disparo.alertaId, dataHora: disparo.ocorrencia, status: 'ATENDIDO');
        await _remover(disparo);
        continue;
      }

      final registro = registroQueAtende(disparo, registros);
      if (registro != null) {
        await _informarAtendido(pacienteId, disparo, registro);
        await _remover(disparo);
        continue;
      }

      if (!disparo.pendenteEnviado && !_permitido) {
        await _api.registrarDisparo(
          pacienteId,
          alertaId: disparo.alertaId,
          dataHora: disparo.ocorrencia,
          status: 'FALHOU',
          mensagemErro: 'Notificações desativadas no aparelho do paciente.',
        );
        await _remover(disparo);
        continue;
      }

      if (!disparo.pendenteEnviado) {
        await _api.registrarDisparo(pacienteId, alertaId: disparo.alertaId, dataHora: disparo.ocorrencia, status: 'PENDENTE');
        disparo.pendenteEnviado = true;
        await _salvar();
      }

      // Passada a janela, a API marca como FALHOU pela RN35; aqui acabou.
      if (!disparo.emJanela(agora)) await _remover(disparo);
    }
  }

  Future<void> _informarAtendido(int pacienteId, Disparo disparo, RegistroGlicemia registro) async {
    final mediuDepois = registro.dataHora.isAfter(disparo.ocorrencia);

    // A API usa a data enviada como hora do atendimento e fecha o PENDENTE
    // anterior a ela. Mandar os dois passos preserva a hora real de cada um.
    if (mediuDepois && !disparo.pendenteEnviado) {
      await _api.registrarDisparo(pacienteId, alertaId: disparo.alertaId, dataHora: disparo.ocorrencia, status: 'PENDENTE');
      disparo.pendenteEnviado = true;
      await _salvar();
    }
    await _api.registrarDisparo(
      pacienteId,
      alertaId: disparo.alertaId,
      dataHora: mediuDepois ? registro.dataHora : disparo.ocorrencia,
      status: 'ATENDIDO',
    );
  }

  Future<void> _replanejar(
    List<Alerta> alertas,
    Set<int> persistentes,
    List<RegistroGlicemia> registros,
    DateTime agora,
  ) async {
    final ativos = {for (final a in alertas.where((a) => a.ativo)) a.id};

    // RN26 — desativar interrompe os reenvios do que estava em aberto também.
    final mantidos = _agenda.where((d) {
      final passado = !d.ocorrencia.isAfter(agora);
      return ativos.contains(d.alertaId) && (passado || d.avulso);
    }).toList();
    final chaves = {for (final d in mantidos) d.chave};

    _agenda = [
      ...mantidos,
      ...disparosPrevistos(alertas, agora, tiposPersistentes: persistentes).where((d) => !chaves.contains(d.chave)),
    ];
    await _salvar();

    await _notificador.cancelarTodas();
    if (_permitido) await _notificador.agendar(montarNotificacoes(_agenda, registros, agora));
  }

  /// "Lembrar em 15 minutos": encerra o disparo como REAGENDADO, e o novo horário
  /// recomeça o ciclo de repetição da RN26.
  Future<void> lembrarDepois(Disparo disparo) async {
    final pacienteId = _pacienteId;
    if (pacienteId == null) return;

    final agora = _agora();
    await _api.registrarDisparo(pacienteId, alertaId: disparo.alertaId, dataHora: agora, status: 'REAGENDADO');

    _agenda
      ..removeWhere((d) => d.chave == disparo.chave)
      ..add(Disparo(
        alertaId: disparo.alertaId,
        ocorrencia: agora.add(const Duration(minutes: 15)),
        persistente: disparo.persistente,
        titulo: disparo.titulo,
        mensagem: disparo.mensagem,
        avulso: true,
      ));
    await _salvar();
    await conciliar();
  }

  Future<void> _remover(Disparo disparo) async {
    _agenda.removeWhere((d) => d.chave == disparo.chave);
    await _salvar();
  }

  Future<void> _salvar() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_chaveAgenda, jsonEncode(_agenda.map((d) => d.paraJson()).toList()));
  }
}
