import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gliconutri/alertas/agenda_lembretes.dart';
import 'package:gliconutri/alertas/ciclo_alertas.dart';
import 'package:gliconutri/alertas/notificador.dart';
import 'package:gliconutri/api/cliente_api.dart';
import 'package:gliconutri/api/modelos.dart';
import 'package:gliconutri/escopo.dart';
import 'package:gliconutri/sessao/sessao.dart';
import 'package:gliconutri/telas/registrar_glicemia_tela.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:shared_preferences/shared_preferences.dart';

class _NotificadorMudo implements Notificador {
  @override
  Future<bool> permissaoConcedida() async => true;
  @override
  Future<bool> pedirPermissao() async => true;
  @override
  Future<void> agendar(List<NotificacaoAgendada> notificacoes) async {}
  @override
  Future<void> cancelarTodas() async {}
}

void main() {
  late List<Map<String, dynamic>> glicemias;
  late List<Map<String, dynamic>> emocoes;
  late bool falharEmocao;
  late String classificacao;

  Widget montar() {
    final cliente = MockClient((pedido) async {
      http.Response json(Object d, [int status = 200]) => http.Response.bytes(utf8.encode(jsonEncode(d)), status);
      final rota = pedido.url.path.replaceFirst('/api', '');

      if (rota == '/referencias/contextos-glicemia') {
        return json([{'id': 1, 'codigo': 'JEJUM', 'descricao': 'Jejum'}]);
      }
      if (rota == '/referencias/estados-emocionais') {
        return json([{'id': 3, 'codigo': 'ANSIOSO', 'descricao': 'Ansioso'}]);
      }
      if (rota == '/pacientes/1/glicemia' && pedido.method == 'POST') {
        final corpo = jsonDecode(pedido.body) as Map<String, dynamic>;
        glicemias.add(corpo);
        return json({
          'id': 1, 'valor': corpo['valor'], 'contexto': 'Jejum', 'contextoId': 1,
          'dataHora': corpo['dataHora'], 'observacao': null, 'foraDoAlvo': classificacao != 'NORMAL',
          'classificacao': classificacao, 'minAlvoAplicado': 70, 'maxAlvoAplicado': 180,
        }, 201);
      }
      if (rota == '/pacientes/1/emocoes') {
        if (falharEmocao) return http.Response('', 500);
        emocoes.add(jsonDecode(pedido.body) as Map<String, dynamic>);
        return json([], 201);
      }
      return http.Response('', 404);
    });

    final api = ClienteApi(endereco: 'http://api/api', cliente: cliente);
    final ciclo = CicloAlertas(api: api, notificador: _NotificadorMudo());
    final sessao = Sessao(api: api, ciclo: ciclo)
      ..credencial = Credencial(
        token: 't', expiraEm: DateTime(2030), usuarioId: 1, nome: 'Maria Souza',
        email: 'm@t.local', perfil: 'PACIENTE', senhaProvisoria: false,
      );

    return Escopo(
      sessao: sessao,
      ciclo: ciclo,
      revisao: ValueNotifier(0),
      child: MaterialApp(
        home: Builder(
          builder: (context) => Scaffold(
            body: Center(
              child: TextButton(
                onPressed: () => Navigator.of(context).push(MaterialPageRoute<bool>(builder: (_) => const RegistrarGlicemiaTela())),
                child: const Text('abrir'),
              ),
            ),
          ),
        ),
      ),
    );
  }

  Future<void> abrir(WidgetTester t) async {
    t.view.physicalSize = const Size(800, 2400);
    t.view.devicePixelRatio = 1;
    addTearDown(t.view.reset);
    await t.pumpWidget(montar());
    await t.tap(find.text('abrir'));
    await t.pumpAndSettle();
  }

  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    FlutterSecureStorage.setMockInitialValues({});
    await initializeDateFormatting('pt_BR');
    glicemias = [];
    emocoes = [];
    falharEmocao = false;
    classificacao = 'NORMAL';
  });

  testWidgets('A1 do UC004: valor fora de 1 a 600 não é enviado', (t) async {
    await abrir(t);
    await t.tap(find.text('Jejum'));

    for (final invalido in ['', '0', '601']) {
      await t.enterText(find.byType(TextField).first, invalido);
      await t.tap(find.text('Salvar registro'));
      await t.pump();
      expect(find.textContaining('mg/dL.').evaluate().isNotEmpty || find.text('Informe o valor da glicemia.').evaluate().isNotEmpty, isTrue);
    }
    expect(glicemias, isEmpty);
  });

  testWidgets('RN18: contexto é obrigatório e não vem pré-selecionado', (t) async {
    await abrir(t);
    await t.enterText(find.byType(TextField).first, '110');
    await t.tap(find.text('Salvar registro'));
    await t.pump();

    expect(find.text('Selecione o momento da medição.'), findsOneWidget);
    expect(glicemias, isEmpty);
  });

  testWidgets('RN22: sem emoção, grava só a glicemia, com a data em UTC', (t) async {
    await abrir(t);
    await t.enterText(find.byType(TextField).first, '110');
    await t.tap(find.text('Jejum'));
    await t.tap(find.text('Salvar registro'));
    await t.pumpAndSettle();

    expect(glicemias.single['valor'], 110);
    expect(glicemias.single['dataHora'] as String, endsWith('Z'));
    expect(emocoes, isEmpty);
    expect(find.text('Glicemia registrada.'), findsOneWidget);
  });

  testWidgets('emoção escolhida vai com a mesma data e a intensidade', (t) async {
    await abrir(t);
    await t.enterText(find.byType(TextField).first, '110');
    await t.tap(find.text('Jejum'));
    await t.tap(find.text('Ansioso'));
    await t.pump();
    await t.tap(find.text('Salvar registro'));
    await t.pumpAndSettle();

    expect(emocoes.single['estadosEmocionaisIds'], [3]);
    expect(emocoes.single['intensidade'], 3);
    expect(emocoes.single['dataHora'], glicemias.single['dataHora']);
  });

  testWidgets('se só a emoção falhar, a medição não é reenviada e o paciente é avisado', (t) async {
    falharEmocao = true;
    await abrir(t);
    await t.enterText(find.byType(TextField).first, '110');
    await t.tap(find.text('Jejum'));
    await t.tap(find.text('Ansioso'));
    await t.pump();
    await t.tap(find.text('Salvar registro'));
    await t.pumpAndSettle();

    expect(glicemias, hasLength(1));
    expect(find.textContaining('Glicemia salva'), findsOneWidget);
    expect(find.text('Salvar registro'), findsNothing, reason: 'o formulário fecha para não duplicar');
  });

  testWidgets('A2 do UC004: a classificação da API dispara o alerta', (t) async {
    classificacao = 'HIPOGLICEMIA';
    await abrir(t);
    await t.enterText(find.byType(TextField).first, '62');
    await t.tap(find.text('Jejum'));
    await t.tap(find.text('Salvar registro'));
    await t.pumpAndSettle();

    expect(find.text('Glicemia baixa'), findsOneWidget);
    await t.tap(find.text('Entendi'));
    await t.pumpAndSettle();
    expect(find.text('Salvar registro'), findsNothing);
  });
}
