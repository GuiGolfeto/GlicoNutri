import 'package:flutter/material.dart';

import '../api/modelos.dart';
import '../componentes/comuns.dart';
import '../escopo.dart';
import '../tema.dart';

/// UC007 — plano vigente, somente leitura. A prescrição é do nutricionista.
class PlanoTela extends StatefulWidget {
  const PlanoTela({super.key});

  @override
  State<PlanoTela> createState() => _PlanoTelaState();
}

class _PlanoTelaState extends State<PlanoTela> {
  int _dia = 1;

  @override
  Widget build(BuildContext context) {
    final escopo = Escopo.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('Plano alimentar')),
      body: Carga<PlanoAlimentar?>(
        buscar: () => escopo.api.planoAtivo(escopo.pacienteId),
        construir: (context, plano, _) => plano == null
            ? ListView(children: const [
                EstadoVazio(
                  icone: Icons.restaurant_menu_outlined,
                  titulo: 'Você ainda não tem plano alimentar',
                  texto: 'Seu nutricionista elabora o plano na consulta. Quando estiver pronto, ele aparece aqui.',
                ),
              ])
            : _conteudo(context, plano),
      ),
    );
  }

  Widget _conteudo(BuildContext context, PlanoAlimentar plano) {
    final dias = {for (final i in plano.itens) i.dia}.toList()..sort();
    final dia = dias.contains(_dia) ? _dia : (dias.isEmpty ? 1 : dias.first);
    final itens = plano.itens.where((i) => i.dia == dia);

    // A API já entrega os itens em ordem de dia e horário; o agrupamento mantém.
    final refeicoes = <String, List<ItemPlano>>{};
    for (final item in itens) {
      refeicoes.putIfAbsent(item.refeicao ?? 'Refeição', () => []).add(item);
    }

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
      children: [
        Cartao(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (plano.objetivo != null) ...[
                Text(plano.objetivo!, style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 4),
              ],
              Text(plano.dataFim == null
                  ? 'Desde ${dataCurta(plano.dataInicio)}'
                  : '${dataCurta(plano.dataInicio)} a ${dataCurta(plano.dataFim!)}'),
              if (plano.calorias != null || plano.distribuicao != null) ...[
                const SizedBox(height: 12),
                Wrap(
                  spacing: 20,
                  runSpacing: 10,
                  children: [
                    if (plano.calorias != null) Indicador(rotulo: 'Por dia', valor: numero(plano.calorias!.round()), unidade: 'kcal'),
                    if (plano.distribuicao case final d?) ...[
                      Indicador(rotulo: 'Carboidratos', valor: numero(d.carboidratos.round()), unidade: 'g'),
                      Indicador(rotulo: 'Proteínas', valor: numero(d.proteinas.round()), unidade: 'g'),
                      Indicador(rotulo: 'Lipídios', valor: numero(d.lipidios.round()), unidade: 'g'),
                    ],
                  ],
                ),
              ],
            ],
          ),
        ),
        if (plano.observacoes != null) ...[
          const SizedBox(height: 12),
          Cartao(
            fundo: Cores.primariaClara,
            corBorda: Cores.primariaClara,
            child: Text('Orientação: ${plano.observacoes!}', style: const TextStyle(color: Cores.texto)),
          ),
        ],
        const SizedBox(height: 16),
        if (dias.isEmpty)
          const EstadoVazio(
            icone: Icons.schedule,
            titulo: 'As refeições ainda não foram montadas',
            texto: 'Seu nutricionista está preparando o detalhamento do plano.',
          )
        else ...[
          if (dias.length > 1) ...[
            SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Row(
                children: [
                  for (final d in dias)
                    Padding(
                      padding: const EdgeInsets.only(right: 8),
                      child: ChoiceChip(label: Text('Dia $d'), selected: d == dia, onSelected: (_) => setState(() => _dia = d)),
                    ),
                ],
              ),
            ),
            const SizedBox(height: 12),
          ],
          for (final refeicao in refeicoes.entries) ...[
            _CartaoRefeicao(nome: refeicao.key, itens: refeicao.value),
            const SizedBox(height: 12),
          ],
        ],
      ],
    );
  }
}

class _CartaoRefeicao extends StatelessWidget {
  const _CartaoRefeicao({required this.nome, required this.itens});

  final String nome;
  final List<ItemPlano> itens;

  @override
  Widget build(BuildContext context) {
    final horario = itens.first.horario;

    return Cartao(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(child: Text(nome, style: Theme.of(context).textTheme.titleMedium)),
              if (horario != null) Text(horario.substring(0, 5), style: const TextStyle(color: Cores.primaria, fontWeight: FontWeight.w600, fontFeatures: numerico)),
            ],
          ),
          const SizedBox(height: 8),
          for (final item in itens)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 5),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(item.alimentoNome, style: const TextStyle(fontSize: 15, color: Cores.texto)),
                        Text('${numero(item.quantidade)} ${item.unidade ?? 'g'}', style: Theme.of(context).textTheme.bodySmall),
                      ],
                    ),
                  ),
                  const SizedBox(width: 12),
                  // Valor não determinado na TACO aparece como tal, nunca como zero.
                  Text(item.calorias == null ? '— kcal' : '${numero(item.calorias!.round())} kcal',
                      style: const TextStyle(fontSize: 14, fontFeatures: numerico)),
                ],
              ),
            ),
        ],
      ),
    );
  }
}
