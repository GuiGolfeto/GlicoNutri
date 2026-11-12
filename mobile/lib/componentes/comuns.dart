import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../api/cliente_api.dart';
import '../escopo.dart';
import '../tema.dart';

// ── Formatação ──────────────────────────────────────────────────────────────

String dataCurta(DateTime d) => DateFormat('dd/MM/yyyy', 'pt_BR').format(d);
String hora(DateTime d) => DateFormat('HH:mm', 'pt_BR').format(d);

/// "hoje às 12:30", "ontem às 08:00" ou "03/04/2026 às 08:00".
String quando(DateTime d) {
  final agora = DateTime.now();
  final dias = DateTime(agora.year, agora.month, agora.day).difference(DateTime(d.year, d.month, d.day)).inDays;
  final dia = switch (dias) {
    0 => 'hoje',
    1 => 'ontem',
    -1 => 'amanhã',
    _ => dataCurta(d),
  };
  return '$dia às ${hora(d)}';
}

/// Inteiro sem casa decimal; o restante com vírgula.
String numero(num valor, {int casas = 1}) {
  if (valor == valor.roundToDouble()) return valor.round().toString();
  return NumberFormat.decimalPatternDigits(locale: 'pt_BR', decimalDigits: casas).format(valor);
}

/// RF09.2 — só http e https saem do sistema; o back-end aplica a mesma regra.
bool linkSeguro(String? url) => url != null && (url.startsWith('https://') || url.startsWith('http://'));

String dominioDe(String url) => (Uri.tryParse(url)?.host ?? url).replaceFirst(RegExp(r'^www\.'), '');

const emojiDaEmocao = {
  'FELIZ': '😊',
  'TRANQUILO': '😌',
  'ANSIOSO': '😰',
  'TRISTE': '😢',
  'IRRITADO': '😠',
  'ESTRESSADO': '😫',
};

// ── Peças de tela ───────────────────────────────────────────────────────────

class Cartao extends StatelessWidget {
  const Cartao({super.key, required this.child, this.aoTocar, this.fundo, this.corBorda});

  final Widget child;
  final VoidCallback? aoTocar;
  final Color? fundo;
  final Color? corBorda;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: fundo ?? Cores.superficie,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(10),
        side: BorderSide(color: corBorda ?? Cores.borda),
      ),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: aoTocar,
        child: Padding(padding: const EdgeInsets.all(16), child: child),
      ),
    );
  }
}

/// Estado clínico sempre tem texto: a cor sozinha não informa quem não a distingue.
class Selo extends StatelessWidget {
  const Selo(this.texto, {super.key, required this.cor, required this.fundo});

  const Selo.ok(this.texto, {super.key}) : cor = Cores.sucesso, fundo = Cores.sucessoFundo;
  const Selo.perigo(this.texto, {super.key}) : cor = const Color(0xFFB91C1C), fundo = Cores.perigoFundo;
  const Selo.neutro(this.texto, {super.key}) : cor = Cores.primaria, fundo = Cores.primariaClara;

  final String texto;
  final Color cor;
  final Color fundo;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(color: fundo, borderRadius: BorderRadius.circular(999)),
      child: Text(texto, style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: cor)),
    );
  }
}

class Indicador extends StatelessWidget {
  const Indicador({super.key, required this.rotulo, required this.valor, this.unidade});

  final String rotulo;
  final String valor;
  final String? unidade;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(rotulo, style: Theme.of(context).textTheme.bodySmall),
        const SizedBox(height: 2),
        Text.rich(
          TextSpan(children: [
            TextSpan(
              text: valor,
              style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w600, color: Cores.texto, fontFeatures: numerico),
            ),
            if (unidade != null)
              TextSpan(text: ' $unidade', style: const TextStyle(fontSize: 13, color: Cores.textoApoio)),
          ]),
        ),
      ],
    );
  }
}

class EstadoVazio extends StatelessWidget {
  const EstadoVazio({super.key, required this.icone, required this.titulo, this.texto, this.acao});

  final IconData icone;
  final String titulo;
  final String? texto;
  final Widget? acao;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 40),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icone, size: 40, color: Cores.textoApoio),
          const SizedBox(height: 12),
          Text(titulo, textAlign: TextAlign.center, style: Theme.of(context).textTheme.titleMedium),
          if (texto != null) ...[
            const SizedBox(height: 6),
            Text(texto!, textAlign: TextAlign.center),
          ],
          if (acao != null) ...[const SizedBox(height: 16), acao!],
        ],
      ),
    );
  }
}

class AvisoErro extends StatelessWidget {
  const AvisoErro(this.mensagem, {super.key});

  final String mensagem;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(color: Cores.perigoFundo, borderRadius: BorderRadius.circular(8)),
      child: Semantics(
        liveRegion: true,
        child: Text(mensagem, style: const TextStyle(fontSize: 14, color: Color(0xFF991B1B))),
      ),
    );
  }
}

/// Busca um dado e cuida dos estados de carregando, erro e nova tentativa.
///
/// Numa recarga, o dado anterior continua na tela até o novo chegar: a tela não
/// pisca e o paciente não perde a posição da rolagem.
class Carga<T> extends StatefulWidget {
  const Carga({super.key, required this.buscar, required this.construir, this.dependencia});

  final Future<T> Function() buscar;
  final Widget Function(BuildContext context, T dados, Future<void> Function() recarregar) construir;

  /// Quando muda (um filtro de período, por exemplo), busca de novo.
  final Object? dependencia;

  @override
  State<Carga<T>> createState() => _CargaState<T>();
}

class _CargaState<T> extends State<Carga<T>> {
  T? _dados;
  bool _temDados = false;
  String? _erro;
  ValueNotifier<int>? _revisao;
  int _pedido = 0;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_revisao == null) {
      _revisao = Escopo.of(context).revisao..addListener(_recarregar);
      _recarregar();
    }
  }

  @override
  void didUpdateWidget(Carga<T> anterior) {
    super.didUpdateWidget(anterior);
    if (anterior.dependencia != widget.dependencia) _recarregar();
  }

  @override
  void dispose() {
    _revisao?.removeListener(_recarregar);
    super.dispose();
  }

  Future<void> _recarregar() async {
    // Se o filtro mudou no meio, só a resposta do último pedido entra na tela.
    final pedido = ++_pedido;
    try {
      final dados = await widget.buscar();
      if (!mounted || pedido != _pedido) return;
      setState(() {
        _dados = dados;
        _temDados = true;
        _erro = null;
      });
    } on ErroApi catch (e) {
      if (!mounted || pedido != _pedido) return;
      setState(() => _erro = e.mensagem);
    } catch (e, pilha) {
      // Resposta fora do formato esperado: sem isto a tela giraria para sempre.
      debugPrint('Falha ao carregar: $e\n$pilha');
      if (!mounted || pedido != _pedido) return;
      setState(() => _erro = 'Ocorreu um erro inesperado.');
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_temDados) {
      return RefreshIndicator(
        onRefresh: _recarregar,
        child: widget.construir(context, _dados as T, _recarregar),
      );
    }
    if (_erro != null) {
      return Center(
        child: EstadoVazio(
          icone: Icons.cloud_off_outlined,
          titulo: 'Não foi possível carregar',
          texto: _erro,
          acao: OutlinedButton(onPressed: _recarregar, child: const Text('Tentar novamente')),
        ),
      );
    }
    return const Center(child: CircularProgressIndicator());
  }
}

void mostrarMensagem(BuildContext context, String texto) {
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(content: Text(texto)));
}

/// Data e hora da medição: preenchidas com o momento atual e ajustáveis para
/// registro retroativo, nunca futuro (RN04 do UC004).
class CampoDataHora extends StatelessWidget {
  const CampoDataHora({super.key, required this.valor, required this.aoMudar});

  final DateTime valor;
  final ValueChanged<DateTime> aoMudar;

  Future<void> _escolher(BuildContext context) async {
    final agora = DateTime.now();
    final dia = await showDatePicker(
      context: context,
      initialDate: valor,
      firstDate: agora.subtract(const Duration(days: 90)),
      lastDate: agora,
      helpText: 'Data da medição',
    );
    if (dia == null || !context.mounted) return;

    final horario = await showTimePicker(
      context: context,
      initialTime: TimeOfDay.fromDateTime(valor),
      helpText: 'Hora da medição',
    );
    if (horario == null) return;

    aoMudar(DateTime(dia.year, dia.month, dia.day, horario.hour, horario.minute));
  }

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      label: 'Data e hora: ${dataCurta(valor)} às ${hora(valor)}. Toque para alterar.',
      excludeSemantics: true,
      child: InkWell(
        borderRadius: BorderRadius.circular(8),
        onTap: () => _escolher(context),
        child: InputDecorator(
          decoration: const InputDecoration(suffixIcon: Icon(Icons.calendar_today_outlined, size: 20)),
          child: Text('${dataCurta(valor)} · ${hora(valor)}',
              style: const TextStyle(fontSize: 16, color: Cores.texto, fontFeatures: numerico)),
        ),
      ),
    );
  }
}
