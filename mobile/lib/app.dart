import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';

import 'alertas/ciclo_alertas.dart';
import 'alertas/notificador.dart';
import 'escopo.dart';
import 'sessao/sessao.dart';
import 'tema.dart';
import 'telas/desbloqueio_tela.dart';
import 'telas/estrutura_tela.dart';
import 'telas/login_tela.dart';
import 'telas/registrar_glicemia_tela.dart';
import 'telas/trocar_senha_tela.dart';

class GlicoNutriApp extends StatefulWidget {
  const GlicoNutriApp({super.key, required this.sessao, required this.ciclo, this.notificador, this.abriuPorLembrete = false});

  final Sessao sessao;
  final CicloAlertas ciclo;
  final NotificadorLocal? notificador;

  /// O app foi aberto, estando fechado, pelo toque em um lembrete de glicemia.
  final bool abriuPorLembrete;

  @override
  State<GlicoNutriApp> createState() => _GlicoNutriAppState();
}

class _GlicoNutriAppState extends State<GlicoNutriApp> with WidgetsBindingObserver {
  final _navegador = GlobalKey<NavigatorState>();
  final _revisao = ValueNotifier<int>(0);

  late bool _abrirRegistro = widget.abriuPorLembrete;
  EstadoSessao? _estadoAnterior;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    widget.sessao.addListener(_aoMudarSessao);
    widget.notificador?.aoTocar = (persistente) {
      if (!persistente) return;
      _abrirRegistro = true;
      _abrirRegistroSePuder();
    };
    widget.sessao.iniciar();
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    widget.sessao.removeListener(_aoMudarSessao);
    _revisao.dispose();
    super.dispose();
  }

  void _aoMudarSessao() {
    final estado = widget.sessao.estado;
    if (estado == _estadoAnterior) return;
    _estadoAnterior = estado;

    // Nenhuma tela aberta pode ficar por cima do bloqueio ou do login, com dados
    // de saúde à mostra.
    if (estado != EstadoSessao.ativa) {
      _navegador.currentState?.popUntil((rota) => rota.isFirst);
    } else {
      WidgetsBinding.instance.addPostFrameCallback((_) => _abrirRegistroSePuder());
    }
  }

  /// O toque no lembrete pode chegar antes do desbloqueio; a tela espera por ele.
  void _abrirRegistroSePuder() {
    if (!_abrirRegistro || widget.sessao.estado != EstadoSessao.ativa) return;
    _abrirRegistro = false;
    _navegador.currentState?.push(
      MaterialPageRoute<void>(builder: (_) => const RegistrarGlicemiaTela(), fullscreenDialog: true),
    );
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState estado) {
    if (estado == AppLifecycleState.paused) widget.sessao.aoIrParaSegundoPlano();
    if (estado == AppLifecycleState.resumed) {
      widget.sessao.aoVoltar();
      _revisao.value++;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Escopo(
      sessao: widget.sessao,
      ciclo: widget.ciclo,
      revisao: _revisao,
      child: MaterialApp(
        title: 'GlicoNutri',
        navigatorKey: _navegador,
        debugShowCheckedModeBanner: false,
        theme: criarTema(),
        locale: const Locale('pt', 'BR'),
        supportedLocales: const [Locale('pt', 'BR')],
        localizationsDelegates: GlobalMaterialLocalizations.delegates,
        home: ListenableBuilder(
          listenable: widget.sessao,
          builder: (context, _) => switch (widget.sessao.estado) {
            EstadoSessao.carregando => const Scaffold(body: Center(child: CircularProgressIndicator())),
            EstadoSessao.desconectado => LoginTela(sessao: widget.sessao),
            EstadoSessao.bloqueada => DesbloqueioTela(sessao: widget.sessao),
            EstadoSessao.trocarSenha => TrocarSenhaTela(sessao: widget.sessao, obrigatoria: true),
            EstadoSessao.ativa => const EstruturaTela(),
          },
        ),
      ),
    );
  }
}
