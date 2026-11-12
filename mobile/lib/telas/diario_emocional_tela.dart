import 'package:flutter/material.dart';

import '../api/modelos.dart';
import '../componentes/comuns.dart';
import '../escopo.dart';
import '../tema.dart';
import 'registrar_emocao_tela.dart';

/// UC012 — Diário Emocional. Visível também ao nutricionista (RN04 do UC012), o
/// que a tela deixa claro: o paciente precisa saber quem lê o que ele escreve.
class DiarioEmocionalTela extends StatelessWidget {
  const DiarioEmocionalTela({super.key});

  @override
  Widget build(BuildContext context) {
    final escopo = Escopo.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('Diário emocional')),
      floatingActionButton: FloatingActionButton(
        tooltip: 'Novo registro',
        onPressed: () => Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => const RegistrarEmocaoTela())),
        child: const Icon(Icons.add),
      ),
      body: Carga<List<RegistroEmocional>>(
        buscar: () => escopo.api.diarioEmocional(escopo.pacienteId),
        construir: (context, registros, _) => ListView(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
          children: [
            Text('Últimos 30 dias. Seu nutricionista também vê estes registros.', style: Theme.of(context).textTheme.bodySmall),
            const SizedBox(height: 12),
            if (registros.isEmpty)
              const EstadoVazio(
                icone: Icons.mood_outlined,
                titulo: 'Nenhum registro no período',
                texto: 'Toque em + para contar como você está se sentindo.',
              )
            else
              for (final r in registros) ...[
                Cartao(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Expanded(child: Text(r.estadoEmocional, style: Theme.of(context).textTheme.titleMedium)),
                          Text('Intensidade ${r.intensidade} de 5', style: const TextStyle(fontSize: 13, color: Cores.textoSecundario, fontFeatures: numerico)),
                        ],
                      ),
                      const SizedBox(height: 2),
                      Text(quando(r.dataHora), style: const TextStyle(fontFeatures: numerico)),
                      if (r.descricao != null) ...[
                        const SizedBox(height: 6),
                        Text(r.descricao!, style: const TextStyle(color: Cores.texto)),
                      ],
                    ],
                  ),
                ),
                const SizedBox(height: 8),
              ],
          ],
        ),
      ),
    );
  }
}
