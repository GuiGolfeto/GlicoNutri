import 'package:flutter/material.dart';

import '../api/modelos.dart';
import '../componentes/comuns.dart';
import '../escopo.dart';
import '../tema.dart';
import 'repositorio_tela.dart';

/// RF09.3 — o que o paciente guardou. Material despublicado pelo administrador
/// (RN29) some daqui sem apagar a preferência e volta se for republicado.
class FavoritosTela extends StatelessWidget {
  const FavoritosTela({super.key});

  @override
  Widget build(BuildContext context) {
    final escopo = Escopo.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('Meus favoritos')),
      body: Carga<Favoritos>(
        buscar: () => escopo.api.favoritos(escopo.pacienteId),
        construir: (context, f, _) {
          if (f.receitas.isEmpty && f.conteudos.isEmpty) {
            return ListView(children: const [
              EstadoVazio(
                icone: Icons.favorite_border,
                titulo: 'Você ainda não guardou nada',
                texto: 'Em Conteúdos e receitas, toque no coração para o material aparecer aqui.',
              ),
            ]);
          }
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              if (f.receitas.isNotEmpty) ...[
                Text('Receitas', style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 8),
                for (final r in f.receitas) ...[
                  Cartao(
                    aoTocar: () => Navigator.of(context).push(
                      MaterialPageRoute<void>(builder: (_) => ReceitaTela(id: r.receitaId, marcado: true)),
                    ),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(r.nome, style: Theme.of(context).textTheme.titleMedium),
                              if (r.descricao != null) Text(r.descricao!, maxLines: 2, overflow: TextOverflow.ellipsis),
                              Text(
                                [
                                  if (r.tempoPreparo != null) '${r.tempoPreparo} min',
                                  if (r.caloriasPorPorcao != null) '${numero(r.caloriasPorPorcao!.round())} kcal por porção',
                                ].join(' · '),
                                style: const TextStyle(fontSize: 13, color: Cores.textoApoio, fontFeatures: numerico),
                              ),
                            ],
                          ),
                        ),
                        BotaoFavorito(tipo: 'receitas', id: r.receitaId, marcado: true),
                      ],
                    ),
                  ),
                  const SizedBox(height: 8),
                ],
                const SizedBox(height: 8),
              ],
              if (f.conteudos.isNotEmpty) ...[
                Text('Conteúdos', style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 8),
                for (final c in f.conteudos) ...[
                  CartaoConteudo(conteudo: c, marcado: true),
                  const SizedBox(height: 8),
                ],
              ],
            ],
          );
        },
      ),
    );
  }
}
