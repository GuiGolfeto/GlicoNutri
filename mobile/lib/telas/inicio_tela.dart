import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../alertas/agenda_lembretes.dart';
import '../alertas/ciclo_alertas.dart';
import '../api/cliente_api.dart';
import '../api/modelos.dart';
import '../componentes/comuns.dart';
import '../escopo.dart';
import '../tema.dart';
import 'lembretes_tela.dart';
import 'medidas_tela.dart';
import 'registrar_emocao_tela.dart';
import 'repositorio_tela.dart';

class _DadosInicio {
  const _DadosInicio(this.ultima, this.medida);

  final RegistroGlicemia? ultima;
  final RegistroAntropometrico? medida;
}

class InicioTela extends StatefulWidget {
  const InicioTela({super.key, required this.aoAbrirAba, required this.aoRegistrar});

  final ValueChanged<int> aoAbrirAba;
  final VoidCallback aoRegistrar;

  @override
  State<InicioTela> createState() => _InicioTelaState();
}

class _InicioTelaState extends State<InicioTela> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _oferecerBiometria());
  }

  /// RN01 — habilitável pelo paciente depois do primeiro acesso bem-sucedido.
  Future<void> _oferecerBiometria() async {
    final sessao = Escopo.of(context).sessao;
    if (!await sessao.deveOferecerBiometria() || !mounted) return;
    await sessao.marcarBiometriaOferecida();
    if (!mounted) return;

    final aceitou = await showDialog<bool>(
      context: context,
      builder: (contexto) => AlertDialog(
        title: const Text('Entrar com biometria?'),
        content: const Text('Use a digital ou o reconhecimento facial para abrir o app sem digitar a senha. Você pode mudar isso depois em Mais › Entrar com biometria.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(contexto, false), child: const Text('Agora não')),
          TextButton(onPressed: () => Navigator.pop(contexto, true), child: const Text('Ativar')),
        ],
      ),
    );
    if (aceitou == true) await sessao.definirBiometria(true);
  }

  Future<_DadosInicio> _buscar() async {
    final e = Escopo.of(context);
    // A última glicemia pode ser antiga: 90 dias é o maior período da API.
    final historico = await e.api.historicoGlicemia(e.pacienteId, dias: 90);
    final medidas = await e.api.medidas(e.pacienteId);
    return _DadosInicio(
      historico.registros.isEmpty ? null : historico.registros.first,
      medidas.isEmpty ? null : medidas.first,
    );
  }

  void _abrir(Widget tela) => Navigator.of(context).push(MaterialPageRoute<void>(builder: (_) => tela));

  @override
  Widget build(BuildContext context) {
    final escopo = Escopo.of(context);
    final credencial = escopo.sessao.credencial!;
    final hoje = DateFormat("EEEE, dd 'de' MMMM", 'pt_BR').format(DateTime.now());

    return Scaffold(
      body: SafeArea(
        child: Carga<_DadosInicio>(
          buscar: _buscar,
          construir: (context, dados, _) => ListView(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
            children: [
              Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('Olá, ${credencial.primeiroNome}!', style: Theme.of(context).textTheme.headlineMedium),
                        const SizedBox(height: 2),
                        Text('${hoje[0].toUpperCase()}${hoje.substring(1)}'),
                      ],
                    ),
                  ),
                  _BotaoLembretes(ciclo: escopo.ciclo, aoTocar: () => _abrir(const LembretesTela())),
                ],
              ),
              const SizedBox(height: 16),
              _CartaoUltimaGlicemia(registro: dados.ultima, aoRegistrar: widget.aoRegistrar),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(child: _Atalho(icone: Icons.show_chart, rotulo: 'Histórico', aoTocar: () => widget.aoAbrirAba(1))),
                  const SizedBox(width: 12),
                  Expanded(child: _Atalho(icone: Icons.restaurant_menu_outlined, rotulo: 'Plano', aoTocar: () => widget.aoAbrirAba(2))),
                  const SizedBox(width: 12),
                  Expanded(child: _Atalho(icone: Icons.menu_book_outlined, rotulo: 'Conteúdos', aoTocar: () => _abrir(const RepositorioTela()))),
                ],
              ),
              const SizedBox(height: 12),
              ListenableBuilder(
                listenable: escopo.ciclo,
                builder: (context, _) => _CartaoLembrete(
                  ciclo: escopo.ciclo,
                  aoRegistrar: widget.aoRegistrar,
                  aoAbrirLembretes: () => _abrir(const LembretesTela()),
                ),
              ),
              const SizedBox(height: 12),
              if (dados.medida != null) ...[
                _CartaoMedida(medida: dados.medida!, aoTocar: () => _abrir(const MedidasTela())),
                const SizedBox(height: 12),
              ],
              Cartao(
                aoTocar: () => _abrir(const RegistrarEmocaoTela()),
                child: Row(
                  children: [
                    const Text('🙂', style: TextStyle(fontSize: 30)),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('Como você está se sentindo?', style: Theme.of(context).textTheme.titleMedium),
                          const Text('Registrar é opcional e ajuda seu nutricionista.'),
                        ],
                      ),
                    ),
                    const Icon(Icons.chevron_right, color: Cores.textoApoio),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _BotaoLembretes extends StatelessWidget {
  const _BotaoLembretes({required this.ciclo, required this.aoTocar});

  final CicloAlertas ciclo;
  final VoidCallback aoTocar;

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: ciclo,
      builder: (context, _) {
        final abertos = ciclo.emAberto.length;
        return IconButton(
          tooltip: abertos > 0 ? 'Lembretes: $abertos em aberto' : 'Lembretes',
          onPressed: aoTocar,
          icon: Badge(isLabelVisible: abertos > 0, child: const Icon(Icons.notifications_outlined)),
        );
      },
    );
  }
}

class _CartaoUltimaGlicemia extends StatelessWidget {
  const _CartaoUltimaGlicemia({required this.registro, required this.aoRegistrar});

  final RegistroGlicemia? registro;
  final VoidCallback aoRegistrar;

  @override
  Widget build(BuildContext context) {
    final r = registro;
    if (r == null) {
      return Cartao(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Última glicemia registrada', style: Theme.of(context).textTheme.bodyMedium),
            const SizedBox(height: 8),
            Text('Nenhuma medição ainda', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 12),
            FilledButton(onPressed: aoRegistrar, child: const Text('Registrar primeira medição')),
          ],
        ),
      );
    }

    return Cartao(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Última glicemia registrada', style: Theme.of(context).textTheme.bodyMedium),
          const SizedBox(height: 6),
          Row(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Expanded(
                child: Text.rich(
                  TextSpan(children: [
                    TextSpan(
                      text: numero(r.valor),
                      style: TextStyle(
                        fontSize: 44,
                        height: 1.1,
                        fontWeight: FontWeight.w700,
                        fontFeatures: numerico,
                        color: r.foraDoAlvo ? Cores.perigo : Cores.primaria,
                      ),
                    ),
                    const TextSpan(text: ' mg/dL', style: TextStyle(fontSize: 15, color: Cores.textoSecundario)),
                  ]),
                ),
              ),
              seloDaGlicemia(r),
            ],
          ),
          const SizedBox(height: 12),
          const Divider(),
          const SizedBox(height: 10),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Flexible(child: Text(r.contexto)),
              const SizedBox(width: 8),
              Text(quando(r.dataHora), style: const TextStyle(fontFeatures: numerico)),
            ],
          ),
          const SizedBox(height: 2),
          Text('Sua faixa alvo: ${numero(r.minAlvo)}–${numero(r.maxAlvo)} mg/dL', style: Theme.of(context).textTheme.bodySmall),
        ],
      ),
    );
  }
}

/// RN19 e RN02 do UC004 — o selo diz o que o número significa para este paciente.
Widget seloDaGlicemia(RegistroGlicemia r) => switch (r.classificacao) {
      'HIPOGLICEMIA' => const Selo.perigo('Hipoglicemia'),
      'HIPERGLICEMIA' => const Selo.perigo('Hiperglicemia'),
      _ => r.foraDoAlvo ? const Selo.perigo('Fora do alvo') : const Selo.ok('No alvo'),
    };

class _Atalho extends StatelessWidget {
  const _Atalho({required this.icone, required this.rotulo, required this.aoTocar});

  final IconData icone;
  final String rotulo;
  final VoidCallback aoTocar;

  @override
  Widget build(BuildContext context) {
    return Cartao(
      aoTocar: aoTocar,
      child: Column(
        children: [
          CircleAvatar(
            radius: 22,
            backgroundColor: Cores.primariaClara,
            child: Icon(icone, color: Cores.primaria),
          ),
          const SizedBox(height: 8),
          Text(rotulo, textAlign: TextAlign.center, maxLines: 1, overflow: TextOverflow.ellipsis,
              style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w500, color: Cores.texto)),
        ],
      ),
    );
  }
}

class _CartaoLembrete extends StatefulWidget {
  const _CartaoLembrete({required this.ciclo, required this.aoRegistrar, required this.aoAbrirLembretes});

  final CicloAlertas ciclo;
  final VoidCallback aoRegistrar;
  final VoidCallback aoAbrirLembretes;

  @override
  State<_CartaoLembrete> createState() => _CartaoLembreteState();
}

class _CartaoLembreteState extends State<_CartaoLembrete> {
  bool _enviando = false;

  Future<void> _lembrarDepois(Disparo disparo) async {
    setState(() => _enviando = true);
    try {
      await widget.ciclo.lembrarDepois(disparo);
      if (mounted) mostrarMensagem(context, 'Vamos lembrar você em 15 minutos.');
    } on ErroApi catch (e) {
      if (mounted) mostrarMensagem(context, e.mensagem);
    } finally {
      if (mounted) setState(() => _enviando = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final ciclo = widget.ciclo;

    if (!ciclo.notificacoesPermitidas) {
      return Cartao(
        fundo: Cores.atencaoFundo,
        corBorda: const Color(0xFFFCD34D),
        aoTocar: widget.aoAbrirLembretes,
        child: const Row(
          children: [
            Icon(Icons.notifications_off_outlined, color: Cores.atencao),
            SizedBox(width: 12),
            Expanded(child: Text('Lembretes desligados. Toque para permitir os avisos do seu nutricionista.', style: TextStyle(color: Cores.texto))),
          ],
        ),
      );
    }

    final abertos = ciclo.emAberto;
    if (abertos.isNotEmpty) {
      final disparo = abertos.first;
      return Cartao(
        fundo: Cores.atencaoFundo,
        corBorda: const Color(0xFFFCD34D),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Lembrete das ${hora(disparo.ocorrencia)}', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 2),
            Text(disparo.mensagem),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(child: FilledButton(onPressed: widget.aoRegistrar, child: const Text('Registrar'))),
                const SizedBox(width: 8),
                Expanded(
                  child: OutlinedButton(
                    style: OutlinedButton.styleFrom(minimumSize: const Size.fromHeight(50), side: const BorderSide(color: Cores.atencao)),
                    onPressed: _enviando ? null : () => _lembrarDepois(disparo),
                    child: const Text('Em 15 min'),
                  ),
                ),
              ],
            ),
          ],
        ),
      );
    }

    final proximo = ciclo.proximo;
    if (proximo == null) return const SizedBox.shrink();

    return Cartao(
      aoTocar: widget.aoAbrirLembretes,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const CircleAvatar(
            radius: 18,
            backgroundColor: Cores.primariaClara,
            child: Icon(Icons.notifications_none, color: Cores.primaria, size: 20),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Próximo lembrete: ${quando(proximo.ocorrencia)}', style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 2),
                Text(proximo.mensagem),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _CartaoMedida extends StatelessWidget {
  const _CartaoMedida({required this.medida, required this.aoTocar});

  final RegistroAntropometrico medida;
  final VoidCallback aoTocar;

  @override
  Widget build(BuildContext context) {
    return Cartao(
      aoTocar: aoTocar,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Última medição', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 10),
          Row(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Expanded(child: Indicador(rotulo: 'Peso', valor: numero(medida.peso), unidade: 'kg')),
              if (medida.imc != null) Expanded(child: Indicador(rotulo: 'IMC', valor: numero(medida.imc!))),
              if (medida.classificacaoImc != null) Flexible(child: Selo.neutro(medida.classificacaoImc!)),
            ],
          ),
        ],
      ),
    );
  }
}
