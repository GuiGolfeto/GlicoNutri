import 'package:flutter/material.dart';

import '../componentes/logo.dart';
import '../sessao/sessao.dart';
import '../tema.dart';

/// RN01 — acesso por digital ou reconhecimento facial, habilitado pelo paciente.
class DesbloqueioTela extends StatefulWidget {
  const DesbloqueioTela({super.key, required this.sessao});

  final Sessao sessao;

  @override
  State<DesbloqueioTela> createState() => _DesbloqueioTelaState();
}

class _DesbloqueioTelaState extends State<DesbloqueioTela> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => widget.sessao.desbloquear());
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Cores.superficie,
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Center(child: LogoGlicoNutri(tamanho: 56)),
              const SizedBox(height: 12),
              Text('Olá, ${widget.sessao.credencial?.primeiroNome ?? ''}',
                  textAlign: TextAlign.center, style: Theme.of(context).textTheme.headlineMedium),
              const SizedBox(height: 4),
              const Text('Confirme sua identidade para continuar.', textAlign: TextAlign.center),
              const SizedBox(height: 32),
              FilledButton.icon(
                onPressed: widget.sessao.desbloquear,
                icon: const Icon(Icons.fingerprint),
                label: const Text('Usar biometria'),
              ),
              const SizedBox(height: 8),
              TextButton(onPressed: widget.sessao.usarSenha, child: const Text('Entrar com e-mail e senha')),
            ],
          ),
        ),
      ),
    );
  }
}
