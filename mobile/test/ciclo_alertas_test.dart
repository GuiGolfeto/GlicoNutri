import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:gliconutri/alertas/agenda_lembretes.dart';
import 'package:gliconutri/alertas/ciclo_alertas.dart';
import 'package:gliconutri/alertas/notificador.dart';
import 'package:gliconutri/api/cliente_api.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:shared_preferences/shared_preferences.dart';

class _NotificadorFalso implements Notificador {
  bool permitido = true;
  List<NotificacaoAgendada> agendadas = [];
  int cancelamentos = 0;

  @override
  Future<bool> permissaoConcedida() async => permitido;
  @override
  Future<bool> pedirPermissao() async => permitido;
  @override
  Future<void> agendar(List<NotificacaoAgendada> notificacoes) async => agendadas = notificacoes;
  @override
  Future<void> cancelarTodas() async {
    cancelamentos++;
    agendadas = [];
  }
}

/// API em memória, só com o que o ciclo chama.
class _ApiFalsa {
  List<Map<String, dynamic>> alertas = [];
  List<Map<String, dynamic>> registros = [];
  final disparos = <Map<String, dynamic>>[];
  final ordem = <String>[];
  bool foraDoAr = false;

  Map<String, dynamic> alerta({int id = 10, int tipoId = 1, String h1 = '08:00:00', bool ativo = true}) => {
        'id': id, 'tipoId': tipoId, 'tipo': 'Glicemia', 'mensagem': 'Meça', 'horario1': h1,
        'horario2': null, 'diasSemana': '1111111', 'ativo': ativo,
      };

  void medir(DateTime quando) => registros.add({
        'id': registros.length + 1, 'valor': 110, 'contexto': 'Jejum', 'contextoId': 1,
        'dataHora': quando.toUtc().toIso8601String(), 'observacao': null, 'foraDoAlvo': false,
        'classificacao': 'NORMAL', 'minAlvoAplicado': 70, 'maxAlvoAplicado': 180,
      });

  http.Client get cliente => MockClient((pedido) async {
        if (foraDoAr) throw http.ClientException('sem rede');
        final rota = pedido.url.path.replaceFirst('/api', '');
        ordem.add('${pedido.method} $rota');
        http.Response json(Object dados) => http.Response.bytes(utf8.encode(jsonEncode(dados)), 200);

        if (rota == '/pacientes/1/glicemia') {
          return json({
            'resumo': {'totalRegistros': registros.length, 'minAlvoAplicado': 70, 'maxAlvoAplicado': 180},
            'registros': registros,
          });
        }
        if (rota == '/referencias/tipos-alerta') {
          return json([
            {'id': 1, 'codigo': 'GLICEMIA', 'descricao': 'Glicemia'},
            {'id': 3, 'codigo': 'REFEICAO', 'descricao': 'Refeição'},
          ]);
        }
        if (rota == '/pacientes/1/alertas') return json(alertas);
        if (rota == '/pacientes/1/alertas/disparos') {
          disparos.add(jsonDecode(pedido.body) as Map<String, dynamic>);
          return json({});
        }
        if (rota == '/pacientes/1/alertas/verificar-expirados') return json({'expirados': 0, 'pendentesRestantes': 0});
        return http.Response('', 404);
      });

  List<String> get status => disparos.map((d) => d['status'] as String).toList();
}

void main() {
  late _ApiFalsa api;
  late _NotificadorFalso notificador;
  late DateTime agora;
  late CicloAlertas ciclo;

  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    await initializeDateFormatting('pt_BR');
    api = _ApiFalsa();
    notificador = _NotificadorFalso();
    agora = DateTime(2026, 11, 4, 7, 50);
    final cliente = ClienteApi(endereco: 'http://api/api', cliente: api.cliente);
    ciclo = CicloAlertas(api: cliente, notificador: notificador, relogio: () => agora);
  });

  test('primeira abertura agenda, mas não informa disparo nenhum', () async {
    agora = DateTime(2026, 11, 4, 11);
    api.alertas = [api.alerta()];
    await ciclo.iniciar(1);

    // O alerta das 8h nunca foi programado neste aparelho: informá-lo como
    // pendente acusaria o paciente de algo que não aconteceu.
    expect(api.disparos, isEmpty);
    expect(notificador.agendadas, isNotEmpty);
    expect(ciclo.proximo!.ocorrencia, DateTime(2026, 11, 5, 8));
  });

  test('RN27: horário que passou sem medição vira PENDENTE e fica em aberto', () async {
    api.alertas = [api.alerta()];
    await ciclo.iniciar(1);

    agora = DateTime(2026, 11, 4, 8, 40);
    await ciclo.conciliar();

    expect(api.status, ['PENDENTE']);
    expect(ciclo.emAberto, hasLength(1));
    // RN26: as repetições que faltam continuam agendadas.
    expect(notificador.agendadas.first.quando, DateTime(2026, 11, 4, 9));
  });

  test('o PENDENTE não é informado duas vezes', () async {
    api.alertas = [api.alerta()];
    await ciclo.iniciar(1);

    agora = DateTime(2026, 11, 4, 8, 40);
    await ciclo.conciliar();
    agora = DateTime(2026, 11, 4, 8, 50);
    await ciclo.conciliar();

    expect(api.status, ['PENDENTE']);
  });

  test('RN26: medição depois do aviso fecha o pendente com a hora real', () async {
    api.alertas = [api.alerta()];
    await ciclo.iniciar(1);

    agora = DateTime(2026, 11, 4, 8, 40);
    await ciclo.conciliar();

    final medida = DateTime(2026, 11, 4, 8, 45);
    api.medir(medida);
    agora = DateTime(2026, 11, 4, 8, 46);
    await ciclo.conciliar();

    expect(api.status, ['PENDENTE', 'ATENDIDO']);
    expect(DateTime.parse(api.disparos.last['dataHoraDisparo'] as String).toLocal(), medida);
    expect(ciclo.emAberto, isEmpty);
    // Os demais dias seguem agendados; as repetições de hoje, não.
    expect(notificador.agendadas.first.quando, DateTime(2026, 11, 5, 8));
  });

  test('medição feita pela web, com o app fechado, também atende', () async {
    api.alertas = [api.alerta()];
    await ciclo.iniciar(1);

    api.medir(DateTime(2026, 11, 4, 8, 10));
    agora = DateTime(2026, 11, 4, 12);
    await ciclo.conciliar();

    // O PENDENTE vem primeiro porque é ele que a API fecha com o ATENDIDO.
    expect(api.status, ['PENDENTE', 'ATENDIDO']);
  });

  test('medição antes do horário atende sem PENDENTE', () async {
    api.alertas = [api.alerta()];
    await ciclo.iniciar(1);

    api.medir(DateTime(2026, 11, 4, 7, 55));
    agora = DateTime(2026, 11, 4, 7, 56);
    await ciclo.conciliar();
    expect(notificador.agendadas.first.quando, DateTime(2026, 11, 5, 8), reason: 'as 8h de hoje já foram atendidas');

    agora = DateTime(2026, 11, 4, 8, 1);
    await ciclo.conciliar();
    expect(api.status, ['ATENDIDO']);
  });

  test('RN35: janela encerrada informa PENDENTE antes da varredura e sai da agenda', () async {
    api.alertas = [api.alerta()];
    await ciclo.iniciar(1);

    agora = DateTime(2026, 11, 4, 11);
    api.ordem.clear();
    await ciclo.conciliar();

    expect(api.status, ['PENDENTE']);
    expect(ciclo.emAberto, isEmpty);
    final disparo = api.ordem.indexOf('POST /pacientes/1/alertas/disparos');
    final varredura = api.ordem.indexOf('POST /pacientes/1/alertas/verificar-expirados');
    expect(disparo, lessThan(varredura), reason: 'o PENDENTE antigo precisa expirar nesta mesma varredura');

    await ciclo.conciliar();
    expect(api.status, ['PENDENTE'], reason: 'não reabre nem repete');
  });

  test('RN27: sem permissão, registra FALHOU com a causa e não agenda', () async {
    api.alertas = [api.alerta()];
    await ciclo.iniciar(1);

    notificador.permitido = false;
    agora = DateTime(2026, 11, 4, 8, 40);
    await ciclo.conciliar();

    expect(api.status, ['FALHOU']);
    expect(api.disparos.single['mensagemErro'], contains('Notificações desativadas'));
    expect(notificador.agendadas, isEmpty);
    expect(ciclo.notificacoesPermitidas, isFalse);
  });

  test('sem permissão, mas com medição, o desfecho é ATENDIDO', () async {
    api.alertas = [api.alerta()];
    await ciclo.iniciar(1);

    notificador.permitido = false;
    api.medir(DateTime(2026, 11, 4, 8, 5));
    agora = DateTime(2026, 11, 4, 8, 40);
    await ciclo.conciliar();

    expect(api.status, ['PENDENTE', 'ATENDIDO']);
  });

  test('lembrete de refeição não expira como falha do paciente', () async {
    api.alertas = [api.alerta(tipoId: 3)];
    await ciclo.iniciar(1);

    agora = DateTime(2026, 11, 4, 8, 40);
    await ciclo.conciliar();

    expect(api.status, ['ATENDIDO']);
    expect(ciclo.emAberto, isEmpty);
  });

  test('RN26: desativar o alerta interrompe as repetições do que está em aberto', () async {
    api.alertas = [api.alerta()];
    await ciclo.iniciar(1);

    agora = DateTime(2026, 11, 4, 8, 40);
    await ciclo.conciliar();
    expect(ciclo.emAberto, hasLength(1));

    api.alertas = [];
    await ciclo.conciliar();

    expect(notificador.agendadas, isEmpty);
    expect(ciclo.emAberto, isEmpty);
  });

  test('lembrar em 15 minutos fecha como REAGENDADO e recomeça o ciclo', () async {
    api.alertas = [api.alerta()];
    await ciclo.iniciar(1);

    agora = DateTime(2026, 11, 4, 8, 40);
    await ciclo.conciliar();
    await ciclo.lembrarDepois(ciclo.emAberto.single);

    expect(api.status, ['PENDENTE', 'REAGENDADO']);
    expect(ciclo.emAberto, isEmpty);
    expect(notificador.agendadas.first.quando, DateTime(2026, 11, 4, 8, 55));
    expect(notificador.agendadas[1].quando, DateTime(2026, 11, 4, 9, 25), reason: 'RN26 continua valendo');
  });

  test('sem rede, nada se perde: a agenda informa na próxima abertura', () async {
    api.alertas = [api.alerta()];
    await ciclo.iniciar(1);
    final agendadas = notificador.agendadas.length;

    agora = DateTime(2026, 11, 4, 8, 40);
    api.foraDoAr = true;
    await ciclo.conciliar();
    expect(api.disparos, isEmpty);
    expect(notificador.agendadas, hasLength(agendadas), reason: 'os avisos continuam programados');

    api.foraDoAr = false;
    await ciclo.conciliar();
    expect(api.status, ['PENDENTE']);
  });

  test('a agenda sobrevive ao fechamento do app', () async {
    api.alertas = [api.alerta()];
    await ciclo.iniciar(1);

    agora = DateTime(2026, 11, 4, 8, 40);
    final reaberto = CicloAlertas(
      api: ClienteApi(endereco: 'http://api/api', cliente: api.cliente),
      notificador: notificador,
      relogio: () => agora,
    );
    await reaberto.iniciar(1);

    expect(api.status, ['PENDENTE']);
    expect(reaberto.emAberto, hasLength(1));
  });

  test('sair cancela os avisos e limpa a agenda', () async {
    api.alertas = [api.alerta()];
    await ciclo.iniciar(1);
    await ciclo.encerrar();

    expect(notificador.agendadas, isEmpty);
    final prefs = await SharedPreferences.getInstance();
    expect(prefs.getString('agenda_lembretes_1'), isNull);
  });
}
