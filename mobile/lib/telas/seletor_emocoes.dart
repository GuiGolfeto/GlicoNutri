import 'package:flutter/material.dart';

import '../api/modelos.dart';
import '../componentes/comuns.dart';
import '../tema.dart';

/// UC012 — emoções do momento e intensidade de 1 a 5, usado tanto no registro de
/// glicemia quanto no diário emocional.
class SeletorEmocoes extends StatelessWidget {
  const SeletorEmocoes({
    super.key,
    required this.emocoes,
    required this.escolhidas,
    required this.intensidade,
    required this.aoAlternar,
    required this.aoMudarIntensidade,
  });

  final List<Referencia> emocoes;
  final Set<int> escolhidas;
  final int intensidade;
  final ValueChanged<int> aoAlternar;
  final ValueChanged<int> aoMudarIntensidade;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        LayoutBuilder(builder: (context, medidas) {
          final largura = (medidas.maxWidth - 16) / 3;
          return Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              for (final e in emocoes)
                SizedBox(
                  width: largura,
                  child: Semantics(
                    selected: escolhidas.contains(e.id),
                    button: true,
                    label: e.descricao,
                    excludeSemantics: true,
                    child: Material(
                      color: escolhidas.contains(e.id) ? Cores.primariaClara : Cores.fundo,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(8),
                        side: BorderSide(color: escolhidas.contains(e.id) ? Cores.primaria : Cores.borda),
                      ),
                      clipBehavior: Clip.antiAlias,
                      child: InkWell(
                        onTap: () => aoAlternar(e.id),
                        child: Padding(
                          padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 4),
                          child: Column(
                            children: [
                              Text(emojiDaEmocao[e.codigo] ?? '🙂', style: const TextStyle(fontSize: 26)),
                              const SizedBox(height: 4),
                              Text(e.descricao, maxLines: 1, overflow: TextOverflow.ellipsis,
                                  style: const TextStyle(fontSize: 13, color: Cores.texto)),
                            ],
                          ),
                        ),
                      ),
                    ),
                  ),
                ),
            ],
          );
        }),
        if (escolhidas.isNotEmpty) ...[
          const SizedBox(height: 16),
          Text('Intensidade: $intensidade de 5', style: Theme.of(context).textTheme.titleMedium),
          Slider(
            value: intensidade.toDouble(),
            min: 1,
            max: 5,
            divisions: 4,
            label: '$intensidade',
            onChanged: (v) => aoMudarIntensidade(v.round()),
          ),
          const Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [Text('Leve'), Text('Muito intenso')],
          ),
        ],
      ],
    );
  }
}
