import 'package:flutter_test/flutter_test.dart';
import 'package:gliconutri/alertas/agenda_lembretes.dart';
import 'package:gliconutri/api/modelos.dart';

Alerta _alerta({int id = 1, int tipoId = 1, String h1 = '08:00:00', String? h2, String? dias, bool ativo = true, String? mensagem = 'Meça sua glicemia'}) =>
    Alerta(id: id, tipoId: tipoId, tipo: 'Glicemia', mensagem: mensagem, horario1: h1, horario2: h2, diasSemana: dias, ativo: ativo);

RegistroGlicemia _registro(DateTime quando) => RegistroGlicemia(
      id: 1, valor: 100, contexto: 'Jejum', contextoId: 1, dataHora: quando, observacao: null,
      foraDoAlvo: false, classificacao: 'NORMAL', minAlvo: 70, maxAlvo: 180,
    );

Disparo _disparo(DateTime ocorrencia, {bool persistente = true}) => Disparo(
      alertaId: 1, ocorrencia: ocorrencia, persistente: persistente, titulo: 'Glicemia', mensagem: 'Meça',
    );

void main() {
  // Quarta-feira.
  final agora = DateTime(2026, 11, 4, 9);

  group('dias da semana', () {
    test('a máscara começa no domingo', () {
      final soDomingo = _alerta(dias: '1000000');
      expect(alertaValeNoDia(soDomingo, DateTime(2026, 11, 8)), isTrue, reason: 'domingo');
      expect(alertaValeNoDia(soDomingo, DateTime(2026, 11, 9)), isFalse, reason: 'segunda');

      final soSabado = _alerta(dias: '0000001');
      expect(alertaValeNoDia(soSabado, DateTime(2026, 11, 7)), isTrue, reason: 'sábado');
    });

    test('sem máscara vale todo dia', () {
      expect(alertaValeNoDia(_alerta(), DateTime(2026, 11, 8)), isTrue);
    });
  });

  group('disparos previstos', () {
    test('não inclui horário que já passou hoje', () {
      final d = disparosPrevistos([_alerta(h1: '08:00:00', h2: '20:00:00')], agora, tiposPersistentes: {1}, dias: 0);
      expect(d.map((e) => e.ocorrencia), [DateTime(2026, 11, 4, 20)]);
    });

    test('alerta inativo não agenda nada', () {
      expect(disparosPrevistos([_alerta(ativo: false)], agora, tiposPersistentes: {1}), isEmpty);
    });

    test('RN25: usa a mensagem do nutricionista e só cai no padrão sem ela', () {
      expect(disparosPrevistos([_alerta()], agora, tiposPersistentes: {1}).first.mensagem, 'Meça sua glicemia');
      expect(disparosPrevistos([_alerta(mensagem: null)], agora, tiposPersistentes: {1}).first.mensagem, contains('Glicemia'));
    });

    test('RN26: só o tipo de glicemia é persistente', () {
      final d = disparosPrevistos([_alerta(tipoId: 3)], agora, tiposPersistentes: {1});
      expect(d.first.persistente, isFalse);
    });
  });

  group('registro que atende', () {
    final ocorrencia = DateTime(2026, 11, 4, 8);

    test('vale de 30 minutos antes até o fim da janela de 2 horas', () {
      final d = _disparo(ocorrencia);
      expect(registroQueAtende(d, [_registro(DateTime(2026, 11, 4, 7, 30))]), isNotNull);
      expect(registroQueAtende(d, [_registro(DateTime(2026, 11, 4, 9, 59))]), isNotNull);
    });

    test('fora da janela não atende', () {
      final d = _disparo(ocorrencia);
      expect(registroQueAtende(d, [_registro(DateTime(2026, 11, 4, 7, 29))]), isNull);
      expect(registroQueAtende(d, [_registro(DateTime(2026, 11, 4, 10))]), isNull);
    });

    test('havendo mais de uma, conta a primeira medição', () {
      final primeira = DateTime(2026, 11, 4, 8, 10);
      final r = registroQueAtende(_disparo(ocorrencia), [_registro(DateTime(2026, 11, 4, 9)), _registro(primeira)]);
      expect(r!.dataHora, primeira);
    });
  });

  group('montagem das notificações', () {
    test('RN26: alerta de glicemia toca quatro vezes dentro da janela da RN35', () {
      final n = montarNotificacoes([_disparo(DateTime(2026, 11, 4, 20))], [], agora);
      expect(n.map((e) => e.quando), [
        DateTime(2026, 11, 4, 20),
        DateTime(2026, 11, 4, 20, 30),
        DateTime(2026, 11, 4, 21),
        DateTime(2026, 11, 4, 21, 30),
      ]);
      expect(n.last.quando.difference(n.first.quando), lessThan(janelaDoAlerta));
    });

    test('RN26: disparo em aberto mantém só as repetições que faltam', () {
      final n = montarNotificacoes([_disparo(DateTime(2026, 11, 4, 8))], [], agora);
      expect(n.map((e) => e.quando), [DateTime(2026, 11, 4, 9, 30)]);
    });

    test('RN26: medição que atende cancela todas as repetições', () {
      final n = montarNotificacoes([_disparo(DateTime(2026, 11, 4, 8))], [_registro(DateTime(2026, 11, 4, 8, 50))], agora);
      expect(n, isEmpty);
    });

    test('medir pouco antes do horário evita o aviso', () {
      final n = montarNotificacoes([_disparo(DateTime(2026, 11, 4, 9, 10))], [_registro(DateTime(2026, 11, 4, 8, 55))], agora);
      expect(n, isEmpty);
    });

    test('lembrete que não é de glicemia avisa uma vez e ignora medição', () {
      final n = montarNotificacoes(
        [_disparo(DateTime(2026, 11, 4, 12), persistente: false)],
        [_registro(DateTime(2026, 11, 4, 11, 50))],
        agora,
      );
      expect(n, hasLength(1));
    });

    test('respeita o limite de notificações pendentes do iOS, ficando com as mais próximas', () {
      final alertas = [for (var i = 0; i < 10; i++) _alerta(id: i + 1, h1: '${(i + 10).toString().padLeft(2, '0')}:00:00')];
      final n = montarNotificacoes(disparosPrevistos(alertas, agora, tiposPersistentes: {1}), [], agora);
      expect(n, hasLength(limiteDeNotificacoes));
      for (var i = 1; i < n.length; i++) {
        expect(n[i].quando.isBefore(n[i - 1].quando), isFalse);
      }
    });
  });
}
