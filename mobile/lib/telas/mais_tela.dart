import 'package:flutter/material.dart';

import '../componentes/comuns.dart';
import '../escopo.dart';
import '../sessao/sessao.dart';
import '../tema.dart';
import 'diario_emocional_tela.dart';
import 'favoritos_tela.dart';
import 'lembretes_tela.dart';
import 'medidas_tela.dart';
import 'repositorio_tela.dart';
import 'trocar_senha_tela.dart';

class MaisTela extends StatelessWidget {
  const MaisTela({super.key});

  @override
  Widget build(BuildContext context) {
    final sessao = Escopo.of(context).sessao;
    final credencial = sessao.credencial!;

    void abrir(Widget tela) => Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => tela));

    return Scaffold(
      appBar: AppBar(title: const Text('Mais')),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Cartao(
            child: Row(
              children: [
                CircleAvatar(
                  radius: 24,
                  backgroundColor: Cores.primariaClara,
                  child: Text(credencial.nome.substring(0, 1).toUpperCase(),
                      style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w600, color: Cores.primaria)),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(credencial.nome, style: Theme.of(context).textTheme.titleMedium),
                      Text(credencial.email, overflow: TextOverflow.ellipsis),
                    ],
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
          _Grupo(children: [
            _Item(icone: Icons.menu_book_outlined, titulo: 'Conteúdos e receitas', aoTocar: () => abrir(const RepositorioTela())),
            _Item(icone: Icons.favorite_border, titulo: 'Meus favoritos', aoTocar: () => abrir(const FavoritosTela())),
          ]),
          const SizedBox(height: 16),
          _Grupo(children: [
            _Item(icone: Icons.monitor_weight_outlined, titulo: 'Minhas medidas', aoTocar: () => abrir(const MedidasTela())),
            _Item(icone: Icons.mood_outlined, titulo: 'Diário emocional', aoTocar: () => abrir(const DiarioEmocionalTela())),
            _Item(icone: Icons.notifications_outlined, titulo: 'Lembretes', aoTocar: () => abrir(const LembretesTela())),
          ]),
          const SizedBox(height: 16),
          _Grupo(children: [
            _ItemBiometria(sessao: sessao),
            _Item(icone: Icons.lock_outline, titulo: 'Alterar senha', aoTocar: () => abrir(TrocarSenhaTela(sessao: sessao))),
          ]),
          const SizedBox(height: 16),
          _Grupo(children: [
            ListTile(
              leading: const Icon(Icons.logout, color: Cores.perigo),
              title: const Text('Sair', style: TextStyle(color: Cores.perigo)),
              onTap: () => _confirmarSaida(context, sessao),
            ),
          ]),
        ],
      ),
    );
  }

  Future<void> _confirmarSaida(BuildContext context, Sessao sessao) async {
    final sair = await showDialog<bool>(
      context: context,
      builder: (contexto) => AlertDialog(
        title: const Text('Sair do aplicativo?'),
        content: const Text('Os lembretes param de tocar neste aparelho até você entrar novamente.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(contexto, false), child: const Text('Cancelar')),
          TextButton(onPressed: () => Navigator.pop(contexto, true), child: const Text('Sair')),
        ],
      ),
    );
    if (sair == true) await sessao.encerrar();
  }
}

class _Grupo extends StatelessWidget {
  const _Grupo({required this.children});

  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Cores.superficie,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10), side: const BorderSide(color: Cores.borda)),
      clipBehavior: Clip.antiAlias,
      child: Column(
        children: [
          for (final (i, filho) in children.indexed) ...[if (i > 0) const Divider(), filho],
        ],
      ),
    );
  }
}

class _Item extends StatelessWidget {
  const _Item({required this.icone, required this.titulo, required this.aoTocar});

  final IconData icone;
  final String titulo;
  final VoidCallback aoTocar;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      leading: Icon(icone, color: Cores.primaria),
      title: Text(titulo, style: const TextStyle(fontSize: 16, color: Cores.texto)),
      trailing: const Icon(Icons.chevron_right, color: Cores.textoApoio),
      onTap: aoTocar,
    );
  }
}

/// RN01 — respeita as capacidades do aparelho: sem biometria, a opção não aparece.
class _ItemBiometria extends StatelessWidget {
  const _ItemBiometria({required this.sessao});

  final Sessao sessao;

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: sessao,
      builder: (context, _) => FutureBuilder<bool>(
        future: sessao.biometriaDisponivel(),
        builder: (context, disponivel) {
          if (disponivel.data != true) return const SizedBox.shrink();
          return SwitchListTile(
            secondary: const Icon(Icons.fingerprint, color: Cores.primaria),
            title: const Text('Entrar com biometria', style: TextStyle(fontSize: 16, color: Cores.texto)),
            value: sessao.biometriaAtiva,
            onChanged: (v) async {
              final mudou = await sessao.definirBiometria(v);
              if (!mudou && context.mounted) mostrarMensagem(context, 'Não foi possível confirmar a biometria.');
            },
          );
        },
      ),
    );
  }
}
