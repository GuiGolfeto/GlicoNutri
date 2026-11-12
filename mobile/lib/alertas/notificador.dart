import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:flutter_timezone/flutter_timezone.dart';
import 'package:timezone/data/latest.dart' as tz_dados;
import 'package:timezone/timezone.dart' as tz;

import 'agenda_lembretes.dart';

/// O que o ciclo de alertas precisa do aparelho. Separado para os testes não
/// dependerem de plugin nativo.
abstract class Notificador {
  Future<bool> permissaoConcedida();
  Future<bool> pedirPermissao();
  Future<void> agendar(List<NotificacaoAgendada> notificacoes);
  Future<void> cancelarTodas();
}

class NotificadorLocal implements Notificador {
  final _plugin = FlutterLocalNotificationsPlugin();

  /// Toque em um aviso de lembrete: leva direto ao registro de glicemia.
  void Function(bool persistente)? aoTocar;

  static const _detalhes = NotificationDetails(
    android: AndroidNotificationDetails(
      'lembretes',
      'Lembretes',
      channelDescription: 'Lembretes configurados pelo seu nutricionista',
      importance: Importance.high,
      priority: Priority.high,
    ),
    iOS: DarwinNotificationDetails(),
  );

  Future<void> inicializar() async {
    tz_dados.initializeTimeZones();
    final fuso = await FlutterTimezone.getLocalTimezone();
    tz.setLocalLocation(tz.getLocation(fuso.identifier));

    await _plugin.initialize(
      settings: const InitializationSettings(
        android: AndroidInitializationSettings('ic_notificacao'),
        // A permissão é pedida no momento certo, com explicação na tela.
        iOS: DarwinInitializationSettings(
          requestAlertPermission: false,
          requestBadgePermission: false,
          requestSoundPermission: false,
        ),
      ),
      onDidReceiveNotificationResponse: (resposta) => aoTocar?.call(resposta.payload == 'persistente'),
    );
  }

  /// Aviso que abriu o app a partir de ele fechado.
  Future<bool?> payloadDaAbertura() async {
    final detalhes = await _plugin.getNotificationAppLaunchDetails();
    if (detalhes?.didNotificationLaunchApp != true) return null;
    return detalhes!.notificationResponse?.payload == 'persistente';
  }

  @override
  Future<bool> permissaoConcedida() async {
    if (defaultTargetPlatform == TargetPlatform.android) {
      final android = _plugin.resolvePlatformSpecificImplementation<AndroidFlutterLocalNotificationsPlugin>();
      return await android?.areNotificationsEnabled() ?? false;
    }
    final ios = _plugin.resolvePlatformSpecificImplementation<IOSFlutterLocalNotificationsPlugin>();
    final estado = await ios?.checkPermissions();
    // Na autorização provisória o iOS entrega os avisos em silêncio na central de
    // notificações. Chamar isso de "sem permissão" registraria falha de entrega (RN27).
    return (estado?.isEnabled ?? false) || (estado?.isProvisionalEnabled ?? false);
  }

  @override
  Future<bool> pedirPermissao() async {
    if (defaultTargetPlatform == TargetPlatform.android) {
      final android = _plugin.resolvePlatformSpecificImplementation<AndroidFlutterLocalNotificationsPlugin>();
      return await android?.requestNotificationsPermission() ?? false;
    }
    final ios = _plugin.resolvePlatformSpecificImplementation<IOSFlutterLocalNotificationsPlugin>();
    return await ios?.requestPermissions(alert: true, sound: true, badge: false) ?? false;
  }

  @override
  Future<void> agendar(List<NotificacaoAgendada> notificacoes) async {
    // A permissão de alarme exato vem no manifesto, mas o usuário pode revogá-la nos
    // ajustes; nesse caso o aviso atrasado ainda é melhor que nenhum aviso.
    final android = _plugin.resolvePlatformSpecificImplementation<AndroidFlutterLocalNotificationsPlugin>();
    final exato = await android?.canScheduleExactNotifications() ?? true;
    final modo = exato ? AndroidScheduleMode.exactAllowWhileIdle : AndroidScheduleMode.inexactAllowWhileIdle;

    for (final (i, n) in notificacoes.indexed) {
      await _plugin.zonedSchedule(
        id: i,
        title: n.disparo.titulo,
        body: n.disparo.mensagem,
        scheduledDate: tz.TZDateTime.from(n.quando, tz.local),
        notificationDetails: _detalhes,
        androidScheduleMode: modo,
        payload: n.disparo.persistente ? 'persistente' : 'avulso',
      );
    }
  }

  @override
  Future<void> cancelarTodas() => _plugin.cancelAll();
}
