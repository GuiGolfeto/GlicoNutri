import 'dart:async';

import 'package:flutter/material.dart';

import '../api/cliente_api.dart';
import '../componentes/comuns.dart';
import '../componentes/logo.dart';
import '../sessao/sessao.dart';
import '../tema.dart';

/// UC001 — acesso do paciente. Não há autocadastro (RN08 e RN09): a conta é criada
/// pelo nutricionista, e o login federado com Google é exclusivo da web (RN01).
class LoginTela extends StatefulWidget {
  const LoginTela({super.key, required this.sessao});

  final Sessao sessao;

  @override
  State<LoginTela> createState() => _LoginTelaState();
}

class _LoginTelaState extends State<LoginTela> {
  final _formulario = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _senha = TextEditingController();

  bool _enviando = false;
  bool _senhaVisivel = false;
  String? _erro;

  int _segundosBloqueado = 0;
  Timer? _contagem;

  @override
  void initState() {
    super.initState();
    _erro = widget.sessao.aviso;
  }

  @override
  void dispose() {
    _contagem?.cancel();
    _email.dispose();
    _senha.dispose();
    super.dispose();
  }

  /// RN02 — informa quanto falta para o desbloqueio automático.
  void _iniciarContagem(int segundos) {
    _contagem?.cancel();
    setState(() => _segundosBloqueado = segundos);
    _contagem = Timer.periodic(const Duration(seconds: 1), (t) {
      if (_segundosBloqueado <= 1) {
        t.cancel();
        setState(() {
          _segundosBloqueado = 0;
          _erro = null;
        });
      } else {
        setState(() => _segundosBloqueado--);
      }
    });
  }

  Future<void> _entrar() async {
    if (!_formulario.currentState!.validate()) return;
    setState(() {
      _enviando = true;
      _erro = null;
    });
    try {
      await widget.sessao.entrar(_email.text.trim(), _senha.text);
    } on ErroApi catch (e) {
      if (!mounted) return;
      setState(() => _erro = e.mensagem);
      if (e.segundosBloqueado != null && e.status == 423) _iniciarContagem(e.segundosBloqueado!);
    } finally {
      if (mounted) setState(() => _enviando = false);
    }
  }

  String get _textoBloqueio {
    final min = _segundosBloqueado ~/ 60;
    final seg = (_segundosBloqueado % 60).toString().padLeft(2, '0');
    return 'Tente novamente em $min:$seg';
  }

  @override
  Widget build(BuildContext context) {
    final bloqueado = _segundosBloqueado > 0;

    return Scaffold(
      backgroundColor: Cores.superficie,
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: AutofillGroup(
                child: Form(
                  key: _formulario,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      const Center(child: LogoGlicoNutri(tamanho: 56)),
                      const SizedBox(height: 12),
                      Text('GlicoNutri', textAlign: TextAlign.center, style: Theme.of(context).textTheme.headlineMedium),
                      const SizedBox(height: 4),
                      const Text('Seu acompanhamento nutricional', textAlign: TextAlign.center),
                      const SizedBox(height: 32),
                      if (_erro != null) ...[AvisoErro(_erro!), const SizedBox(height: 16)],
                      TextFormField(
                        controller: _email,
                        keyboardType: TextInputType.emailAddress,
                        textInputAction: TextInputAction.next,
                        autocorrect: false,
                        autofillHints: const [AutofillHints.username, AutofillHints.email],
                        decoration: const InputDecoration(labelText: 'E-mail'),
                        // A3 do UC001.
                        validator: (v) => RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(v?.trim() ?? '')
                            ? null
                            : 'Informe um e-mail válido.',
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _senha,
                        obscureText: !_senhaVisivel,
                        textInputAction: TextInputAction.done,
                        autofillHints: const [AutofillHints.password],
                        onFieldSubmitted: (_) => bloqueado ? null : _entrar(),
                        decoration: InputDecoration(
                          labelText: 'Senha',
                          suffixIcon: IconButton(
                            tooltip: _senhaVisivel ? 'Ocultar senha' : 'Mostrar senha',
                            icon: Icon(_senhaVisivel ? Icons.visibility_off_outlined : Icons.visibility_outlined),
                            onPressed: () => setState(() => _senhaVisivel = !_senhaVisivel),
                          ),
                        ),
                        validator: (v) => (v ?? '').isEmpty ? 'Informe a senha.' : null,
                      ),
                      const SizedBox(height: 20),
                      FilledButton(
                        onPressed: _enviando || bloqueado ? null : _entrar,
                        child: Text(bloqueado ? _textoBloqueio : _enviando ? 'Entrando…' : 'Entrar'),
                      ),
                      const SizedBox(height: 8),
                      TextButton(
                        onPressed: () => Navigator.of(context).push(
                          MaterialPageRoute<void>(builder: (_) => RecuperarSenhaTela(sessao: widget.sessao)),
                        ),
                        child: const Text('Esqueci minha senha'),
                      ),
                      const SizedBox(height: 16),
                      const Text(
                        'Sua conta é criada pelo seu nutricionista. Se ainda não recebeu o acesso, fale com seu nutricionista.',
                        textAlign: TextAlign.center,
                        style: TextStyle(fontSize: 13, color: Cores.textoApoio),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// A1 do UC001 — o link de redefinição chega por e-mail e abre a tela da web.
class RecuperarSenhaTela extends StatefulWidget {
  const RecuperarSenhaTela({super.key, required this.sessao});

  final Sessao sessao;

  @override
  State<RecuperarSenhaTela> createState() => _RecuperarSenhaTelaState();
}

class _RecuperarSenhaTelaState extends State<RecuperarSenhaTela> {
  final _email = TextEditingController();
  bool _enviando = false;
  bool _enviado = false;
  String? _erro;

  @override
  void dispose() {
    _email.dispose();
    super.dispose();
  }

  Future<void> _enviar() async {
    if (!RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(_email.text.trim())) {
      setState(() => _erro = 'Informe um e-mail válido.');
      return;
    }
    setState(() {
      _enviando = true;
      _erro = null;
    });
    try {
      await widget.sessao.api.recuperarSenha(_email.text.trim());
      if (mounted) setState(() => _enviado = true);
    } on ErroApi catch (e) {
      if (mounted) setState(() => _erro = e.mensagem);
    } finally {
      if (mounted) setState(() => _enviando = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Recuperar senha')),
      body: ListView(
        padding: const EdgeInsets.all(24),
        children: [
          if (_enviado)
            // Mesma resposta exista ou não a conta, para não revelar quem é cadastrado.
            const Text('Se houver conta ativa para este e-mail, o link de redefinição foi enviado. Ele vale por 30 minutos.')
          else ...[
            const Text('Informe o e-mail da sua conta. Enviaremos um link para você criar uma nova senha.'),
            const SizedBox(height: 20),
            if (_erro != null) ...[AvisoErro(_erro!), const SizedBox(height: 16)],
            TextField(
              controller: _email,
              keyboardType: TextInputType.emailAddress,
              autocorrect: false,
              decoration: const InputDecoration(labelText: 'E-mail'),
            ),
            const SizedBox(height: 20),
            FilledButton(onPressed: _enviando ? null : _enviar, child: Text(_enviando ? 'Enviando…' : 'Enviar link')),
          ],
        ],
      ),
    );
  }
}
