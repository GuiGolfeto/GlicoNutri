import 'package:flutter/material.dart';

/// Sistema visual "Clinical Precision", o mesmo do ambiente web: teal como
/// primária, escala slate nos neutros e separação por borda de 1px em vez de
/// sombra. O protótipo do app usa azul, mas a marca e a web ficaram no teal, e o
/// paciente transita entre os dois ambientes.
abstract final class Cores {
  static const primaria = Color(0xFF00685D);
  static const primariaClara = Color(0xFFE0F2F1);
  static const marca = Color(0xFF00897B);

  static const superficie = Color(0xFFFFFFFF);
  static const fundo = Color(0xFFF7F9FB);
  static const borda = Color(0xFFE2E8F0);

  static const texto = Color(0xFF0F172A);
  static const textoSecundario = Color(0xFF475569);
  // Mais escuro que o --text-muted da web: aqui ele aparece em texto corrido, e
  // o tom original não alcança contraste de leitura sobre branco.
  static const textoApoio = Color(0xFF64748B);

  static const sucesso = Color(0xFF15803D);
  static const sucessoFundo = Color(0xFFDCFCE7);
  static const atencao = Color(0xFFB45309);
  static const atencaoFundo = Color(0xFFFEF3C7);
  static const perigo = Color(0xFFDC2626);
  static const perigoFundo = Color(0xFFFEE2E2);
}

/// Valores clínicos alinham em coluna com algarismos tabulares.
const numerico = [FontFeature.tabularFigures()];

ThemeData criarTema() {
  final esquema = ColorScheme.fromSeed(
    seedColor: Cores.primaria,
    primary: Cores.primaria,
    surface: Cores.superficie,
    error: Cores.perigo,
  );

  final bordaCampo = OutlineInputBorder(
    borderRadius: BorderRadius.circular(8),
    borderSide: const BorderSide(color: Cores.borda),
  );

  return ThemeData(
    useMaterial3: true,
    colorScheme: esquema,
    fontFamily: 'Inter',
    scaffoldBackgroundColor: Cores.fundo,
    textTheme: const TextTheme(
      headlineMedium: TextStyle(fontSize: 26, fontWeight: FontWeight.w600, color: Cores.texto, letterSpacing: -0.2),
      titleLarge: TextStyle(fontSize: 20, fontWeight: FontWeight.w600, color: Cores.texto),
      titleMedium: TextStyle(fontSize: 16, fontWeight: FontWeight.w600, color: Cores.texto),
      bodyLarge: TextStyle(fontSize: 16, height: 1.45, color: Cores.texto),
      bodyMedium: TextStyle(fontSize: 14, height: 1.45, color: Cores.textoSecundario),
      bodySmall: TextStyle(fontSize: 13, height: 1.4, color: Cores.textoApoio),
    ),
    appBarTheme: const AppBarTheme(
      backgroundColor: Cores.superficie,
      foregroundColor: Cores.texto,
      surfaceTintColor: Colors.transparent,
      elevation: 0,
      scrolledUnderElevation: 0,
      centerTitle: false,
      shape: Border(bottom: BorderSide(color: Cores.borda)),
      titleTextStyle: TextStyle(fontFamily: 'Inter', fontSize: 18, fontWeight: FontWeight.w600, color: Cores.texto),
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: Cores.superficie,
      border: bordaCampo,
      enabledBorder: bordaCampo,
      focusedBorder: bordaCampo.copyWith(
        borderSide: const BorderSide(color: Cores.primaria, width: 1.5),
      ),
    ),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        minimumSize: const Size.fromHeight(50),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        textStyle: const TextStyle(fontFamily: 'Inter', fontSize: 16, fontWeight: FontWeight.w600),
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        minimumSize: const Size(48, 48),
        side: const BorderSide(color: Cores.borda),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
      ),
    ),
    chipTheme: ChipThemeData(
      backgroundColor: Cores.superficie,
      selectedColor: Cores.primariaClara,
      side: const BorderSide(color: Cores.borda),
      labelStyle: const TextStyle(fontFamily: 'Inter', fontSize: 14, color: Cores.texto),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(999)),
      showCheckmark: false,
    ),
    navigationBarTheme: const NavigationBarThemeData(
      backgroundColor: Cores.superficie,
      indicatorColor: Cores.primariaClara,
      surfaceTintColor: Colors.transparent,
      elevation: 0,
    ),
    floatingActionButtonTheme: const FloatingActionButtonThemeData(
      backgroundColor: Cores.primaria,
      foregroundColor: Colors.white,
    ),
    dividerTheme: const DividerThemeData(color: Cores.borda, thickness: 1, space: 1),
    snackBarTheme: const SnackBarThemeData(behavior: SnackBarBehavior.floating),
  );
}
