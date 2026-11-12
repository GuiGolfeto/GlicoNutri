import 'package:flutter/material.dart';

import '../tema.dart';

/// Marca do GlicoNutri: gota com check. Mesmo desenho do LogoGlicoNutri.vue.
class LogoGlicoNutri extends StatelessWidget {
  const LogoGlicoNutri({super.key, this.tamanho = 40});

  final double tamanho;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      label: 'GlicoNutri',
      image: true,
      child: CustomPaint(size: Size.square(tamanho), painter: _PintorLogo()),
    );
  }
}

class _PintorLogo extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    canvas.scale(size.width / 40);

    final gota = Path()
      ..moveTo(20, 38)
      ..cubicTo(27.732, 38, 34, 31.732, 34, 24)
      ..cubicTo(34, 16.268, 20, 2, 20, 2)
      ..cubicTo(20, 2, 6, 16.268, 6, 24)
      ..cubicTo(6, 31.732, 12.268, 38, 20, 38)
      ..close();
    canvas.drawPath(gota, Paint()..color = Cores.marca);

    final check = Path()
      ..moveTo(15, 24)
      ..lineTo(18.5, 27.5)
      ..lineTo(25.5, 20.5);
    canvas.drawPath(
      check,
      Paint()
        ..color = Colors.white
        ..style = PaintingStyle.stroke
        ..strokeWidth = 3
        ..strokeCap = StrokeCap.round
        ..strokeJoin = StrokeJoin.round,
    );
  }

  @override
  bool shouldRepaint(_PintorLogo oldDelegate) => false;
}
