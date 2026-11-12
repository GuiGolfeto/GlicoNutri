import 'package:flutter/material.dart';

import '../tema.dart';
import 'historico_tela.dart';
import 'inicio_tela.dart';
import 'mais_tela.dart';
import 'plano_tela.dart';
import 'registrar_glicemia_tela.dart';

/// Navegação principal do paciente, como no protótipo: quatro abas e o botão
/// flutuante de nova medição, a ação mais frequente do app (UC004).
class EstruturaTela extends StatefulWidget {
  const EstruturaTela({super.key});

  @override
  State<EstruturaTela> createState() => EstruturaTelaState();
}

class EstruturaTelaState extends State<EstruturaTela> {
  int _aba = 0;

  void irPara(int aba) => setState(() => _aba = aba);

  /// Passo 8 do UC004 — depois de salvar, o paciente cai no histórico.
  Future<void> novaMedicao() async {
    final salvou = await Navigator.of(context).push<bool>(
      MaterialPageRoute(builder: (_) => const RegistrarGlicemiaTela(), fullscreenDialog: true),
    );
    if (salvou == true && mounted) irPara(1);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      // IndexedStack preserva filtro e rolagem de cada aba ao alternar.
      body: IndexedStack(
        index: _aba,
        children: [
          InicioTela(aoAbrirAba: irPara, aoRegistrar: novaMedicao),
          const HistoricoTela(),
          const PlanoTela(),
          const MaisTela(),
        ],
      ),
      floatingActionButton: _aba < 2
          ? FloatingActionButton(
              tooltip: 'Registrar glicemia',
              onPressed: novaMedicao,
              child: const Icon(Icons.add),
            )
          : null,
      bottomNavigationBar: DecoratedBox(
        decoration: const BoxDecoration(border: Border(top: BorderSide(color: Cores.borda))),
        child: NavigationBar(
          selectedIndex: _aba,
          onDestinationSelected: irPara,
          destinations: const [
            NavigationDestination(icon: Icon(Icons.home_outlined), selectedIcon: Icon(Icons.home), label: 'Início'),
            NavigationDestination(icon: Icon(Icons.show_chart), label: 'Histórico'),
            NavigationDestination(icon: Icon(Icons.restaurant_menu_outlined), selectedIcon: Icon(Icons.restaurant_menu), label: 'Plano'),
            NavigationDestination(icon: Icon(Icons.menu), label: 'Mais'),
          ],
        ),
      ),
    );
  }
}
