import '../api/modelos.dart';

/// RN35 — depois desta janela o disparo pendente expira.
const janelaDoAlerta = Duration(hours: 2);

/// RN26 manda reenviar "periodicamente" sem dizer o intervalo. Trinta minutos
/// dão quatro avisos dentro da janela de 2 horas da RN35, sem insistir além dela.
const repeticoesDoAlerta = [
  Duration.zero,
  Duration(minutes: 30),
  Duration(minutes: 60),
  Duration(minutes: 90),
];

/// Quem mediu pouco antes do horário já atendeu ao lembrete: sem esta folga, a
/// medição das 7h55 não impediria quatro avisos a partir das 8h.
const antecedenciaAceita = Duration(minutes: 30);

/// Horizonte da agenda. As notificações são locais e cada abertura do app refaz a
/// agenda, então ela só precisa cobrir o tempo até a próxima abertura.
const diasAgendados = 7;

/// O iOS guarda no máximo 64 notificações pendentes e descarta as excedentes.
const limiteDeNotificacoes = 60;

/// Um horário de alerta: o disparo que o aparelho programou para o paciente.
class Disparo {
  Disparo({
    required this.alertaId,
    required this.ocorrencia,
    required this.persistente,
    required this.titulo,
    required this.mensagem,
    this.pendenteEnviado = false,
    this.avulso = false,
  });

  final int alertaId;
  final DateTime ocorrencia;

  /// RN26 vale para o alerta de glicemia: repete até haver registro. Os demais
  /// tipos não têm registro correspondente no sistema e avisam uma vez só.
  final bool persistente;
  final String titulo;

  /// RN25 — a mensagem definida pelo nutricionista.
  final String mensagem;

  /// RN27 — o PENDENTE deste disparo já chegou à API.
  bool pendenteEnviado;

  /// Criado por "lembrar em 15 minutos", e não pela configuração do alerta.
  final bool avulso;

  /// Mesmo alerta e mesmo horário são o mesmo disparo, seja qual for a origem.
  String get chave => '$alertaId@${ocorrencia.millisecondsSinceEpoch}';

  bool emJanela(DateTime agora) => !ocorrencia.isAfter(agora) && agora.difference(ocorrencia) < janelaDoAlerta;

  factory Disparo.deJson(Map<String, dynamic> j) => Disparo(
        alertaId: j['alertaId'] as int,
        ocorrencia: DateTime.parse(j['ocorrencia'] as String).toLocal(),
        persistente: j['persistente'] as bool,
        titulo: j['titulo'] as String,
        mensagem: j['mensagem'] as String,
        pendenteEnviado: j['pendenteEnviado'] as bool,
        avulso: j['avulso'] as bool,
      );

  Map<String, dynamic> paraJson() => {
        'alertaId': alertaId,
        'ocorrencia': ocorrencia.toUtc().toIso8601String(),
        'persistente': persistente,
        'titulo': titulo,
        'mensagem': mensagem,
        'pendenteEnviado': pendenteEnviado,
        'avulso': avulso,
      };
}

/// Notificação a entregar no aparelho.
class NotificacaoAgendada {
  const NotificacaoAgendada({required this.quando, required this.disparo});

  final DateTime quando;
  final Disparo disparo;
}

DateTime? _horarioNoDia(String? horario, DateTime dia) {
  if (horario == null) return null;
  final partes = horario.split(':');
  return DateTime(dia.year, dia.month, dia.day, int.parse(partes[0]), int.parse(partes[1]));
}

/// Dá para o alerta tocar neste dia? A máscara começa no domingo, e o
/// `weekday` do Dart vai de segunda (1) a domingo (7).
bool alertaValeNoDia(Alerta alerta, DateTime dia) {
  final mascara = alerta.diasSemana ?? '1111111';
  return mascara[dia.weekday % 7] == '1';
}

/// Disparos futuros previstos pela configuração dos alertas ativos.
List<Disparo> disparosPrevistos(
  List<Alerta> alertas,
  DateTime agora, {
  required Set<int> tiposPersistentes,
  int dias = diasAgendados,
}) {
  final hoje = DateTime(agora.year, agora.month, agora.day);
  final previstos = <Disparo>[];

  for (final alerta in alertas.where((a) => a.ativo)) {
    for (var i = 0; i <= dias; i++) {
      // O construtor normaliza o dia: somar Duration(days:) erraria na virada de
      // um horário de verão, se ele voltar a existir.
      final dia = DateTime(hoje.year, hoje.month, hoje.day + i);
      if (!alertaValeNoDia(alerta, dia)) continue;

      for (final horario in [alerta.horario1, alerta.horario2]) {
        final ocorrencia = _horarioNoDia(horario, dia);
        if (ocorrencia == null || !ocorrencia.isAfter(agora)) continue;
        previstos.add(Disparo(
          alertaId: alerta.id,
          ocorrencia: ocorrencia,
          persistente: tiposPersistentes.contains(alerta.tipoId),
          titulo: alerta.tipo,
          mensagem: alerta.mensagem?.trim().isNotEmpty == true ? alerta.mensagem! : 'Hora do seu lembrete: ${alerta.tipo}.',
        ));
      }
    }
  }
  return previstos;
}

/// RN26 — a medição que conta para o disparo, se houver. O que vale é a hora da
/// medição, não a do envio: o paciente pode registrar retroativamente (RN04 do
/// UC004) e pode ter registrado pela web.
RegistroGlicemia? registroQueAtende(Disparo disparo, List<RegistroGlicemia> registros) {
  final inicio = disparo.ocorrencia.subtract(antecedenciaAceita);
  final fim = disparo.ocorrencia.add(janelaDoAlerta);

  RegistroGlicemia? escolhido;
  for (final r in registros) {
    if (r.dataHora.isBefore(inicio) || !r.dataHora.isBefore(fim)) continue;
    if (escolhido == null || r.dataHora.isBefore(escolhido.dataHora)) escolhido = r;
  }
  return escolhido;
}

/// Notificações que devem estar programadas agora. Inclui o restante das
/// repetições de um disparo em aberto: RN26 — receber o aviso não encerra o ciclo.
List<NotificacaoAgendada> montarNotificacoes(
  List<Disparo> agenda,
  List<RegistroGlicemia> registros,
  DateTime agora,
) {
  final notificacoes = <NotificacaoAgendada>[];

  for (final disparo in agenda) {
    if (disparo.persistente && registroQueAtende(disparo, registros) != null) continue;

    final passos = disparo.persistente ? repeticoesDoAlerta : const [Duration.zero];
    for (final passo in passos) {
      final quando = disparo.ocorrencia.add(passo);
      if (quando.isAfter(agora)) {
        notificacoes.add(NotificacaoAgendada(quando: quando, disparo: disparo));
      }
    }
  }

  notificacoes.sort((a, b) => a.quando.compareTo(b.quando));
  return notificacoes.take(limiteDeNotificacoes).toList();
}
