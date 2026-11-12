import 'package:flutter/widgets.dart';

import 'alertas/ciclo_alertas.dart';
import 'api/cliente_api.dart';
import 'sessao/sessao.dart';

/// Serviços do aplicativo, disponíveis para qualquer tela.
class Escopo extends InheritedWidget {
  const Escopo({
    super.key,
    required this.sessao,
    required this.ciclo,
    required this.revisao,
    required super.child,
  });

  final Sessao sessao;
  final CicloAlertas ciclo;

  /// Incrementada quando um dado muda: as telas abertas recarregam sozinhas, e o
  /// paciente não volta do registro para um número antigo.
  final ValueNotifier<int> revisao;

  ClienteApi get api => sessao.api;
  int get pacienteId => sessao.pacienteId;

  void dadosMudaram() => revisao.value++;

  static Escopo of(BuildContext context) => context.getInheritedWidgetOfExactType<Escopo>()!;

  @override
  bool updateShouldNotify(Escopo oldWidget) => false;
}
