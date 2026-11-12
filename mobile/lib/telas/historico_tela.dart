import 'package:flutter/material.dart';

import '../api/modelos.dart';
import '../componentes/comuns.dart';
import '../componentes/grafico_linha.dart';
import '../escopo.dart';
import '../tema.dart';
import 'inicio_tela.dart' show seloDaGlicemia;

/// UC005 — Visualizar Histórico Glicêmico.
class HistoricoTela extends StatefulWidget {
  const HistoricoTela({super.key});

  @override
  State<HistoricoTela> createState() => _HistoricoTelaState();
}

class _HistoricoTelaState extends State<HistoricoTela> {
  // RN04 do UC005 — 7 dias por padrão. Os demais são os períodos aceitos pela API.
  static const _periodos = [7, 15, 30, 60, 90];

  int _dias = 7;
  String? _contexto;

  @override
  Widget build(BuildContext context) {
    final escopo = Escopo.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('Histórico glicêmico')),
      body: Carga<HistoricoGlicemico>(
        dependencia: _dias,
        buscar: () => escopo.api.historicoGlicemia(escopo.pacienteId, dias: _dias),
        construir: (context, historico, _) => _conteudo(context, historico),
      ),
    );
  }

  Widget _conteudo(BuildContext context, HistoricoGlicemico historico) {
    final contextos = {for (final r in historico.registros) r.contexto}.toList()..sort();
    final registros = _contexto == null
        ? historico.registros
        : historico.registros.where((r) => r.contexto == _contexto).toList();

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 96),
      children: [
        // A1 do UC005.
        _Filtros(
          opcoes: {for (final d in _periodos) '$d dias': d},
          selecionado: _dias,
          aoEscolher: (d) => setState(() => _dias = d),
        ),
        // A2 do UC005 — só aparece quando há o que separar.
        if (contextos.length > 1) ...[
          const SizedBox(height: 8),
          _Filtros<String?>(
            opcoes: {'Todos': null, for (final c in contextos) c: c},
            selecionado: _contexto,
            aoEscolher: (c) => setState(() => _contexto = c),
          ),
        ],
        const SizedBox(height: 12),
        if (registros.isEmpty)
          const EstadoVazio(
            icone: Icons.water_drop_outlined,
            titulo: 'Nenhuma medição no período',
            texto: 'Toque em + para registrar sua glicemia.',
          )
        else ...[
          _PainelResumo(registros: registros, resumo: historico.resumo, filtrado: _contexto != null),
          const SizedBox(height: 12),
          Cartao(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Evolução', style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 2),
                const Text('Em mg/dL. Toque no gráfico para ver cada medição.'),
                const SizedBox(height: 12),
                GraficoLinha(
                  unidade: 'mg/dL',
                  minAlvo: historico.resumo.minAlvo,
                  maxAlvo: historico.resumo.maxAlvo,
                  descricao: 'Gráfico com ${registros.length} medições de glicemia. Os valores estão na lista abaixo.',
                  pontos: [
                    for (final r in registros)
                      PontoGrafico(data: r.dataHora, valor: r.valor, destaque: r.foraDoAlvo, rotulo: r.contexto),
                  ],
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
          Text('Medições', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 8),
          for (final r in registros) ...[_LinhaRegistro(r), const SizedBox(height: 8)],
        ],
      ],
    );
  }
}

class _Filtros<T> extends StatelessWidget {
  const _Filtros({required this.opcoes, required this.selecionado, required this.aoEscolher});

  final Map<String, T> opcoes;
  final T selecionado;
  final ValueChanged<T> aoEscolher;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        children: [
          for (final o in opcoes.entries)
            Padding(
              padding: const EdgeInsets.only(right: 8),
              child: ChoiceChip(
                label: Text(o.key),
                selected: selecionado == o.value,
                onSelected: (_) => aoEscolher(o.value),
              ),
            ),
        ],
      ),
    );
  }
}

class _PainelResumo extends StatelessWidget {
  const _PainelResumo({required this.registros, required this.resumo, required this.filtrado});

  final List<RegistroGlicemia> registros;
  final ResumoGlicemico resumo;
  final bool filtrado;

  @override
  Widget build(BuildContext context) {
    // A4 do UC005. O resumo da API cobre o período inteiro; filtrado por contexto,
    // ele não corresponde à lista, e o painel é refeito sobre o que está na tela.
    final valores = registros.map((r) => r.valor);
    final noAlvo = registros.where((r) => !r.foraDoAlvo).length;
    final media = filtrado ? valores.reduce((a, b) => a + b) / registros.length : resumo.media;
    final minimo = filtrado ? valores.reduce((a, b) => a < b ? a : b) : resumo.minimo;
    final maximo = filtrado ? valores.reduce((a, b) => a > b ? a : b) : resumo.maximo;
    final percentual = filtrado ? 100 * noAlvo / registros.length : resumo.percentualNoAlvo;

    return Cartao(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(child: Indicador(rotulo: 'Média', valor: media == null ? '—' : numero(media.round()))),
          Expanded(child: Indicador(rotulo: 'Mínimo', valor: minimo == null ? '—' : numero(minimo))),
          Expanded(child: Indicador(rotulo: 'Máximo', valor: maximo == null ? '—' : numero(maximo))),
          Expanded(child: Indicador(rotulo: 'No alvo', valor: percentual == null ? '—' : '${percentual.round()}%')),
        ],
      ),
    );
  }
}

class _LinhaRegistro extends StatelessWidget {
  const _LinhaRegistro(this.r);

  final RegistroGlicemia r;

  @override
  Widget build(BuildContext context) {
    return Cartao(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text.rich(TextSpan(children: [
                  TextSpan(text: numero(r.valor), style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w600, color: Cores.texto, fontFeatures: numerico)),
                  const TextSpan(text: ' mg/dL', style: TextStyle(fontSize: 13, color: Cores.textoApoio)),
                ])),
              ),
              seloDaGlicemia(r),
            ],
          ),
          const SizedBox(height: 4),
          Text('${r.contexto} · ${quando(r.dataHora)}', style: const TextStyle(fontFeatures: numerico)),
          if (r.observacao != null) ...[
            const SizedBox(height: 4),
            Text(r.observacao!, style: Theme.of(context).textTheme.bodySmall),
          ],
        ],
      ),
    );
  }
}
