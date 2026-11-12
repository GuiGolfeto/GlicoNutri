import 'package:flutter_test/flutter_test.dart';
import 'package:gliconutri/api/modelos.dart';

void main() {
  test('data da API, em UTC, vira hora local', () {
    final r = RegistroEmocional.deJson({
      'id': 1, 'estadoEmocional': 'Feliz', 'intensidade': 2,
      'dataHora': '2026-11-04T16:06:19.155675Z', 'descricao': null,
    });
    expect(r.dataHora.isUtc, isFalse);
    expect(r.dataHora, DateTime.utc(2026, 11, 4, 16, 6, 19, 155, 675).toLocal());
  });

  test('data de calendário do plano não muda de dia por causa do fuso', () {
    final p = PlanoAlimentar.deJson({
      'objetivo': 'X', 'dataInicio': '2026-11-04', 'dataFim': null, 'observacoes': null,
      'totalCaloriasPrescritas': 1886, 'distribuicao': null, 'itens': [],
    });
    expect(p.dataInicio, DateTime(2026, 11, 4));
  });

  test('caloria não determinada na TACO continua nula, nunca zero', () {
    final i = ItemPlano.deJson({
      'alimentoNome': 'Leite', 'dia': 1, 'horario': null, 'refeicao': null,
      'quantidade': 200, 'unidade': 'ml', 'calorias': null,
    });
    expect(i.calorias, isNull);
  });

  test('favorito de conteúdo aceita o id com outro nome', () {
    final c = Conteudo.deJson({
      'conteudoId': 9, 'titulo': 'T', 'tipo': 'Artigo', 'corpo': null, 'urlMidia': null, 'autorNome': 'A',
    });
    expect(c.id, 9);
  });

  test('a credencial guardada volta igual', () {
    final original = Credencial(
      token: 't', expiraEm: DateTime(2026, 11, 5, 12), usuarioId: 3, nome: 'Maria Aparecida Souza',
      email: 'm@t.local', perfil: 'PACIENTE', senhaProvisoria: false,
    );
    final lida = Credencial.deJson(original.paraJson());
    expect(lida.expiraEm, original.expiraEm);
    expect(lida.primeiroNome, 'Maria');
  });
}
