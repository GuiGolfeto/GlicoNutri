import 'package:flutter/material.dart';

import '../api/cliente_api.dart';
import '../api/modelos.dart';
import '../componentes/comuns.dart';
import '../escopo.dart';
import '../tema.dart';

/// UC010 — lembretes vistos pelo paciente. RN23: quem cria e edita é o
/// nutricionista; ao paciente cabe apenas desativar.
class LembretesTela extends StatelessWidget {
  const LembretesTela({super.key});

  static const _dias = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'];

  static String _textoDias(String? mascara) {
    final m = mascara ?? '1111111';
    if (m == '1111111') return 'Todos os dias';
    return [for (var i = 0; i < 7; i++) if (m[i] == '1') _dias[i]].join(', ');
  }

  Future<void> _desativar(BuildContext context, Alerta alerta) async {
    final escopo = Escopo.of(context);
    final confirmou = await showDialog<bool>(
      context: context,
      builder: (contexto) => AlertDialog(
        title: const Text('Desativar este lembrete?'),
        // RN23 — o paciente não reativa: precisa saber disso antes, e não depois.
        content: const Text('Ele para de tocar neste aparelho. Só o seu nutricionista pode ativá-lo de novo.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(contexto, false), child: const Text('Cancelar')),
          TextButton(onPressed: () => Navigator.pop(contexto, true), child: const Text('Desativar')),
        ],
      ),
    );
    if (confirmou != true) return;

    try {
      await escopo.api.desativarAlerta(escopo.pacienteId, alerta.id);
      escopo.dadosMudaram();
      // RN26 — desativar interrompe os reenvios já programados.
      await escopo.ciclo.conciliar();
    } on ErroApi catch (e) {
      if (context.mounted) mostrarMensagem(context, e.mensagem);
    }
  }

  @override
  Widget build(BuildContext context) {
    final escopo = Escopo.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('Lembretes')),
      body: Carga<List<Alerta>>(
        buscar: () => escopo.api.alertas(escopo.pacienteId),
        construir: (context, alertas, _) => ListView(
          padding: const EdgeInsets.all(16),
          children: [
            ListenableBuilder(
              listenable: escopo.ciclo,
              builder: (context, _) => escopo.ciclo.notificacoesPermitidas
                  ? const SizedBox.shrink()
                  : Padding(
                      padding: const EdgeInsets.only(bottom: 12),
                      child: Cartao(
                        fundo: Cores.atencaoFundo,
                        corBorda: const Color(0xFFFCD34D),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            const Text(
                              'Os avisos estão desligados, e seu nutricionista verá os lembretes como não entregues. '
                              'Permita as notificações para recebê-los. Se o botão não abrir nada, ative em Ajustes do aparelho › GlicoNutri.',
                              style: TextStyle(color: Cores.texto),
                            ),
                            const SizedBox(height: 10),
                            FilledButton(onPressed: escopo.ciclo.pedirPermissao, child: const Text('Permitir notificações')),
                          ],
                        ),
                      ),
                    ),
            ),
            if (alertas.isEmpty)
              const EstadoVazio(
                icone: Icons.notifications_none,
                titulo: 'Nenhum lembrete ativo',
                texto: 'Os lembretes são configurados pelo seu nutricionista.',
              )
            else ...[
              for (final a in alertas) ...[
                Cartao(
                  child: Row(
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              [a.horario1, a.horario2].nonNulls.map((h) => h.substring(0, 5)).join(' e '),
                              style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w600, color: Cores.primaria, fontFeatures: numerico),
                            ),
                            const SizedBox(height: 2),
                            Text(a.mensagem ?? a.tipo, style: const TextStyle(fontSize: 15, color: Cores.texto)),
                            const SizedBox(height: 2),
                            Text('${a.tipo} · ${_textoDias(a.diasSemana)}', style: Theme.of(context).textTheme.bodySmall),
                          ],
                        ),
                      ),
                      const SizedBox(width: 8),
                      TextButton(onPressed: () => _desativar(context, a), child: const Text('Desativar')),
                    ],
                  ),
                ),
                const SizedBox(height: 8),
              ],
              const SizedBox(height: 4),
              Text('Os lembretes de glicemia se repetem por até 2 horas, até você registrar a medição.',
                  style: Theme.of(context).textTheme.bodySmall),
            ],
          ],
        ),
      ),
    );
  }
}
