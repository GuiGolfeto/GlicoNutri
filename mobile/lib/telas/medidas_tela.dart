import 'package:flutter/material.dart';

import '../api/modelos.dart';
import '../componentes/comuns.dart';
import '../componentes/grafico_linha.dart';
import '../escopo.dart';
import '../tema.dart';

/// UC008 — medidas do paciente, somente leitura. Peso e altura entram no cálculo
/// energético e no plano: quem registra é o nutricionista (decisão do Mateus de
/// 11/09/2026). RF08.2 — evolução do peso.
class MedidasTela extends StatelessWidget {
  const MedidasTela({super.key});

  @override
  Widget build(BuildContext context) {
    final escopo = Escopo.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('Minhas medidas')),
      body: Carga<List<RegistroAntropometrico>>(
        buscar: () => escopo.api.medidas(escopo.pacienteId),
        construir: (context, medidas, _) {
          if (medidas.isEmpty) {
            return ListView(children: const [
              EstadoVazio(
                icone: Icons.monitor_weight_outlined,
                titulo: 'Nenhuma medida registrada',
                texto: 'Suas medidas são registradas pelo nutricionista na consulta.',
              ),
            ]);
          }
          final atual = medidas.first;
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Cartao(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('Medição de ${dataCurta(atual.dataHora)}', style: Theme.of(context).textTheme.titleMedium),
                    const SizedBox(height: 12),
                    Wrap(
                      spacing: 24,
                      runSpacing: 12,
                      children: [
                        Indicador(rotulo: 'Peso', valor: numero(atual.peso), unidade: 'kg'),
                        Indicador(rotulo: 'Altura', valor: numero(atual.altura), unidade: 'cm'),
                        if (atual.imc != null) Indicador(rotulo: 'IMC', valor: numero(atual.imc!)),
                        if (atual.circunferenciaCintura != null) Indicador(rotulo: 'Cintura', valor: numero(atual.circunferenciaCintura!), unidade: 'cm'),
                        if (atual.circunferenciaQuadril != null) Indicador(rotulo: 'Quadril', valor: numero(atual.circunferenciaQuadril!), unidade: 'cm'),
                        if (atual.circunferenciaAbdominal != null) Indicador(rotulo: 'Abdômen', valor: numero(atual.circunferenciaAbdominal!), unidade: 'cm'),
                        if (atual.circunferenciaBraco != null) Indicador(rotulo: 'Braço', valor: numero(atual.circunferenciaBraco!), unidade: 'cm'),
                      ],
                    ),
                    if (atual.classificacaoImc != null) ...[
                      const SizedBox(height: 12),
                      Selo.neutro(atual.classificacaoImc!),
                    ],
                  ],
                ),
              ),
              if (medidas.length > 1) ...[
                const SizedBox(height: 12),
                Cartao(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Evolução do peso', style: Theme.of(context).textTheme.titleMedium),
                      const SizedBox(height: 2),
                      const Text('Em kg. Toque no gráfico para ver cada medição.'),
                      const SizedBox(height: 12),
                      GraficoLinha(
                        unidade: 'kg',
                        descricao: 'Gráfico com ${medidas.length} medições de peso. Os valores estão na lista abaixo.',
                        pontos: [for (final m in medidas) PontoGrafico(data: m.dataHora, valor: m.peso)],
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 16),
                Text('Histórico', style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 8),
                for (final m in medidas) ...[
                  Cartao(
                    child: Row(
                      children: [
                        Expanded(child: Text(dataCurta(m.dataHora), style: const TextStyle(fontFeatures: numerico))),
                        Text('${numero(m.peso)} kg', style: const TextStyle(fontWeight: FontWeight.w600, color: Cores.texto, fontFeatures: numerico)),
                        if (m.imc != null) ...[
                          const SizedBox(width: 16),
                          Text('IMC ${numero(m.imc!)}', style: const TextStyle(fontFeatures: numerico)),
                        ],
                      ],
                    ),
                  ),
                  const SizedBox(height: 8),
                ],
              ],
              const SizedBox(height: 12),
              Text('Suas medidas são registradas pelo nutricionista na consulta.', style: Theme.of(context).textTheme.bodySmall),
            ],
          );
        },
      ),
    );
  }
}
