import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../tema.dart';
import 'comuns.dart';

class PontoGrafico {
  const PontoGrafico({required this.data, required this.valor, this.destaque = false, this.rotulo});

  final DateTime data;
  final double valor;

  /// RF08.1 — valor fora da faixa alvo. Muda a forma do marcador além da cor.
  final bool destaque;
  final String? rotulo;
}

/// RF08 — evolução no tempo: uma série só, com a faixa alvo ao fundo.
/// Tocar ou arrastar mostra o valor do ponto; a lista abaixo do gráfico, nas telas
/// que o usam, traz os mesmos dados para quem prefere ler.
class GraficoLinha extends StatefulWidget {
  const GraficoLinha({super.key, required this.pontos, required this.unidade, this.minAlvo, this.maxAlvo, this.descricao});

  final List<PontoGrafico> pontos;
  final String unidade;
  final double? minAlvo;
  final double? maxAlvo;
  final String? descricao;

  @override
  State<GraficoLinha> createState() => _GraficoLinhaState();
}

class _GraficoLinhaState extends State<GraficoLinha> {
  int? _selecionado;

  @override
  void didUpdateWidget(GraficoLinha anterior) {
    super.didUpdateWidget(anterior);
    if (anterior.pontos != widget.pontos) _selecionado = null;
  }

  void _selecionar(Offset posicao, Size tamanho) {
    final escala = _Escala(widget.pontos, tamanho, widget.minAlvo, widget.maxAlvo);
    var melhor = 0;
    var menor = double.infinity;
    for (final (i, p) in widget.pontos.indexed) {
      final d = (escala.x(p.data) - posicao.dx).abs();
      if (d < menor) {
        menor = d;
        melhor = i;
      }
    }
    setState(() => _selecionado = melhor);
  }

  @override
  Widget build(BuildContext context) {
    return Semantics(
      label: widget.descricao,
      child: LayoutBuilder(builder: (context, medidas) {
        final tamanho = Size(medidas.maxWidth, 200);
        final temAlvo = widget.minAlvo != null && widget.maxAlvo != null;
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            GestureDetector(
              behavior: HitTestBehavior.opaque,
              onTapDown: (d) => _selecionar(d.localPosition, tamanho),
              onHorizontalDragUpdate: (d) => _selecionar(d.localPosition, tamanho),
              child: CustomPaint(
                size: tamanho,
                painter: _PintorGrafico(widget.pontos, widget.unidade, widget.minAlvo, widget.maxAlvo, _selecionado),
              ),
            ),
            if (temAlvo) ...[
              const SizedBox(height: 8),
              Wrap(
                spacing: 16,
                runSpacing: 4,
                children: [
                  _ItemLegenda(
                    marca: Container(width: 14, height: 10, color: Cores.primaria.withValues(alpha: 0.18)),
                    texto: 'Faixa alvo: ${numero(widget.minAlvo!)}–${numero(widget.maxAlvo!)} ${widget.unidade}',
                  ),
                  _ItemLegenda(
                    marca: Transform.rotate(
                      angle: math.pi / 4,
                      child: Container(width: 8, height: 8, color: Cores.perigo),
                    ),
                    texto: 'Fora do alvo',
                  ),
                ],
              ),
            ],
          ],
        );
      }),
    );
  }
}

class _ItemLegenda extends StatelessWidget {
  const _ItemLegenda({required this.marca, required this.texto});

  final Widget marca;
  final String texto;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        marca,
        const SizedBox(width: 6),
        Text(texto, style: Theme.of(context).textTheme.bodySmall),
      ],
    );
  }
}

/// Converte dado em posição. Fica fora do pintor porque o toque usa a mesma conta.
class _Escala {
  _Escala(this.pontos, this.tamanho, double? minAlvo, double? maxAlvo) {
    final valores = [...pontos.map((p) => p.valor), ?minAlvo, ?maxAlvo];
    final menor = valores.reduce(math.min);
    final maior = valores.reduce(math.max);
    final folga = math.max((maior - menor) * 0.12, 1.0);

    passo = _passoLimpo((maior - menor + 2 * folga) / 4);
    minY = ((menor - folga) / passo).floor() * passo;
    maxY = ((maior + folga) / passo).ceil() * passo;

    inicio = pontos.map((p) => p.data).reduce((a, b) => a.isBefore(b) ? a : b);
    fim = pontos.map((p) => p.data).reduce((a, b) => a.isAfter(b) ? a : b);
  }

  final List<PontoGrafico> pontos;
  final Size tamanho;
  late final double passo;
  late final double minY;
  late final double maxY;
  late final DateTime inicio;
  late final DateTime fim;

  static const esquerda = 40.0;
  static const direita = 12.0;
  static const topo = 12.0;
  static const base = 26.0;

  /// Marcas do eixo em números redondos: 1, 2, 5 ou 10 vezes a potência de dez.
  static double _passoLimpo(double bruto) {
    final potencia = math.pow(10, (math.log(bruto) / math.ln10).floor()).toDouble();
    final normalizado = bruto / potencia;
    final fator = normalizado <= 1 ? 1 : normalizado <= 2 ? 2 : normalizado <= 5 ? 5 : 10;
    return fator * potencia;
  }

  double x(DateTime d) {
    final largura = tamanho.width - esquerda - direita;
    final total = fim.difference(inicio).inSeconds;
    // Um único instante não tem eixo de tempo: o ponto fica no meio.
    if (total == 0) return esquerda + largura / 2;
    return esquerda + largura * d.difference(inicio).inSeconds / total;
  }

  double y(double v) {
    final altura = tamanho.height - topo - base;
    return topo + altura * (1 - (v - minY) / (maxY - minY));
  }
}

class _PintorGrafico extends CustomPainter {
  _PintorGrafico(this.pontos, this.unidade, this.minAlvo, this.maxAlvo, this.selecionado);

  final List<PontoGrafico> pontos;
  final String unidade;
  final double? minAlvo;
  final double? maxAlvo;
  final int? selecionado;

  static const _estiloEixo = TextStyle(fontFamily: 'Inter', fontSize: 11, color: Cores.textoApoio, fontFeatures: numerico);

  void _texto(Canvas canvas, String texto, Offset posicao, {TextStyle estilo = _estiloEixo, TextAlign alinhamento = TextAlign.left}) {
    final tp = TextPainter(text: TextSpan(text: texto, style: estilo), textDirection: TextDirection.ltr)..layout();
    final dx = switch (alinhamento) {
      TextAlign.right => posicao.dx - tp.width,
      TextAlign.center => posicao.dx - tp.width / 2,
      _ => posicao.dx,
    };
    tp.paint(canvas, Offset(dx, posicao.dy - tp.height / 2));
  }

  @override
  void paint(Canvas canvas, Size size) {
    if (pontos.isEmpty) return;
    final e = _Escala(pontos, size, minAlvo, maxAlvo);
    final esquerda = _Escala.esquerda;
    final direita = size.width - _Escala.direita;

    // Grade recessiva e eixo Y.
    final grade = Paint()..color = Cores.borda..strokeWidth = 1;
    for (var v = e.minY; v <= e.maxY + 0.001; v += e.passo) {
      canvas.drawLine(Offset(esquerda, e.y(v)), Offset(direita, e.y(v)), grade);
      _texto(canvas, numero(v), Offset(esquerda - 6, e.y(v)), alinhamento: TextAlign.right);
    }

    // Faixa alvo do paciente (RN19/RN20). O nome fica na legenda: dentro do gráfico
    // ele acabaria embaixo da própria linha.
    if (minAlvo != null && maxAlvo != null) {
      canvas.drawRect(
        Rect.fromLTRB(esquerda, e.y(maxAlvo!), direita, e.y(minAlvo!)),
        Paint()..color = Cores.primaria.withValues(alpha: 0.10),
      );
    }

    // Eixo X: só as pontas. A data de cada ponto vem no toque e na lista.
    final baseY = size.height - 10;
    _texto(canvas, dataCurtaSemAno(e.inicio), Offset(esquerda, baseY));
    if (e.inicio != e.fim) {
      _texto(canvas, dataCurtaSemAno(e.fim), Offset(direita, baseY), alinhamento: TextAlign.right);
    }

    // Linha.
    final ordenados = [...pontos]..sort((a, b) => a.data.compareTo(b.data));
    final linha = Path();
    for (final (i, p) in ordenados.indexed) {
      final o = Offset(e.x(p.data), e.y(p.valor));
      i == 0 ? linha.moveTo(o.dx, o.dy) : linha.lineTo(o.dx, o.dy);
    }
    canvas.drawPath(
      linha,
      Paint()
        ..color = Cores.primaria
        ..style = PaintingStyle.stroke
        ..strokeWidth = 2
        ..strokeJoin = StrokeJoin.round
        ..strokeCap = StrokeCap.round,
    );

    // Marcadores com anel na cor da superfície. Com muitos pontos, só os fora do
    // alvo e o último continuam: um colar de 90 bolinhas esconderia a própria linha.
    final poucos = ordenados.length <= 30;
    for (final (i, p) in ordenados.indexed) {
      if (poucos || p.destaque || i == ordenados.length - 1) _marcador(canvas, Offset(e.x(p.data), e.y(p.valor)), p.destaque);
    }

    final indice = selecionado;
    if (indice != null && indice < pontos.length) _dica(canvas, size, e, pontos[indice]);
  }

  void _marcador(Canvas canvas, Offset c, bool destaque) {
    final anel = Paint()..color = Cores.superficie;
    if (destaque) {
      // Losango: fora do alvo não pode depender só do vermelho.
      Path losango(double r) => Path()
        ..moveTo(c.dx, c.dy - r)
        ..lineTo(c.dx + r, c.dy)
        ..lineTo(c.dx, c.dy + r)
        ..lineTo(c.dx - r, c.dy)
        ..close();
      canvas.drawPath(losango(8), anel);
      canvas.drawPath(losango(6), Paint()..color = Cores.perigo);
    } else {
      canvas.drawCircle(c, 6, anel);
      canvas.drawCircle(c, 4, Paint()..color = Cores.primaria);
    }
  }

  void _dica(Canvas canvas, Size size, _Escala e, PontoGrafico p) {
    final c = Offset(e.x(p.data), e.y(p.valor));
    canvas.drawLine(
      Offset(c.dx, _Escala.topo),
      Offset(c.dx, size.height - _Escala.base),
      Paint()..color = Cores.textoApoio..strokeWidth = 1,
    );
    _marcador(canvas, c, p.destaque);

    final linhas = TextSpan(children: [
      TextSpan(
        text: '${numero(p.valor)} $unidade\n',
        style: const TextStyle(fontFamily: 'Inter', fontSize: 13, fontWeight: FontWeight.w600, color: Colors.white, fontFeatures: numerico),
      ),
      TextSpan(
        text: [dataHoraCurta(p.data), ?p.rotulo].join(' · '),
        style: const TextStyle(fontFamily: 'Inter', fontSize: 11, color: Color(0xFFCBD5E1), fontFeatures: numerico),
      ),
    ]);
    final tp = TextPainter(text: linhas, textDirection: TextDirection.ltr)..layout(maxWidth: size.width - 16);

    final largura = tp.width + 16;
    final altura = tp.height + 12;
    final dx = (c.dx - largura / 2).clamp(0.0, size.width - largura);
    // Sobe o balão; sem espaço acima, ele desce para não cobrir o ponto.
    final dy = c.dy - altura - 12 >= 0 ? c.dy - altura - 12 : c.dy + 12;
    canvas.drawRRect(
      RRect.fromRectAndRadius(Rect.fromLTWH(dx, dy, largura, altura), const Radius.circular(6)),
      Paint()..color = Cores.texto,
    );
    tp.paint(canvas, Offset(dx + 8, dy + 6));
  }

  @override
  bool shouldRepaint(_PintorGrafico antigo) =>
      antigo.pontos != pontos || antigo.selecionado != selecionado || antigo.minAlvo != minAlvo || antigo.maxAlvo != maxAlvo;
}

String dataCurtaSemAno(DateTime d) => dataCurta(d).substring(0, 5);
String dataHoraCurta(DateTime d) => '${dataCurtaSemAno(d)} ${hora(d)}';
