import 'package:flutter/material.dart';
import 'package:intl/date_symbol_data_local.dart';

import 'alertas/ciclo_alertas.dart';
import 'alertas/notificador.dart';
import 'api/cliente_api.dart';
import 'app.dart';
import 'config.dart';
import 'sessao/sessao.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await initializeDateFormatting('pt_BR');

  final notificador = NotificadorLocal();
  await notificador.inicializar();

  final api = ClienteApi(endereco: enderecoDaApi());
  final ciclo = CicloAlertas(api: api, notificador: notificador);
  final sessao = Sessao(api: api, ciclo: ciclo);

  runApp(GlicoNutriApp(
    sessao: sessao,
    ciclo: ciclo,
    notificador: notificador,
    abriuPorLembrete: await notificador.payloadDaAbertura() ?? false,
  ));
}
