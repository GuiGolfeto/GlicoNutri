import 'package:flutter/foundation.dart';

/// Endereço da API, definido na compilação:
/// `flutter run --dart-define=API_URL=https://...`
///
/// RN31 exige HTTPS. Os padrões em HTTP só existem em desenvolvimento, e por
/// isso o pacote de produção recusa subir sem um endereço explícito.
String enderecoDaApi() {
  const definido = String.fromEnvironment('API_URL');
  if (definido.isNotEmpty) return definido;

  if (kReleaseMode) {
    throw StateError('Compile o pacote de produção com --dart-define=API_URL=https://...');
  }

  // O emulador Android enxerga o computador hospedeiro neste endereço; para ele,
  // "localhost" é o próprio emulador.
  return defaultTargetPlatform == TargetPlatform.android
      ? 'http://10.0.2.2:5080/api'
      : 'http://localhost:5080/api';
}
