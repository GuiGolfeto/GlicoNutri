import 'package:flutter/material.dart';

import '../api/cliente_api.dart';
import '../componentes/comuns.dart';
import '../sessao/sessao.dart';

/// RN03 — troca obrigatória no primeiro acesso, e também disponível a qualquer
/// momento pela área de segurança.
class TrocarSenhaTela extends StatefulWidget {
  const TrocarSenhaTela({super.key, required this.sessao, this.obrigatoria = false});

  final Sessao sessao;
  final bool obrigatoria;

  @override
  State<TrocarSenhaTela> createState() => _TrocarSenhaTelaState();
}

class _TrocarSenhaTelaState extends State<TrocarSenhaTela> {
  final _formulario = GlobalKey<FormState>();
  final _atual = TextEditingController();
  final _nova = TextEditingController();
  final _confirmacao = TextEditingController();
  bool _enviando = false;
  String? _erro;

  @override
  void dispose() {
    _atual.dispose();
    _nova.dispose();
    _confirmacao.dispose();
    super.dispose();
  }

  /// RN02 do UC001 — mesmo critério da API, para o erro aparecer antes do envio.
  String? _validarNova(String? v) {
    final senha = v ?? '';
    if (senha.length < 8) return 'Use no mínimo 8 caracteres.';
    if (!senha.contains(RegExp(r'[A-Za-zÀ-ÿ]'))) return 'Inclua ao menos uma letra.';
    if (!senha.contains(RegExp(r'\d'))) return 'Inclua ao menos um número.';
    return null;
  }

  Future<void> _salvar() async {
    if (!_formulario.currentState!.validate()) return;
    setState(() {
      _enviando = true;
      _erro = null;
    });
    try {
      await widget.sessao.alterarSenha(_atual.text, _nova.text);
      if (!mounted) return;
      if (!widget.obrigatoria) {
        mostrarMensagem(context, 'Senha alterada.');
        Navigator.of(context).pop();
      }
    } on ErroApi catch (e) {
      if (mounted) setState(() => _erro = e.mensagem);
    } finally {
      if (mounted) setState(() => _enviando = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Alterar senha'),
        automaticallyImplyLeading: !widget.obrigatoria,
        actions: [
          if (widget.obrigatoria) TextButton(onPressed: () => widget.sessao.encerrar(), child: const Text('Sair')),
        ],
      ),
      body: SafeArea(
        child: Form(
          key: _formulario,
          child: ListView(
            padding: const EdgeInsets.all(24),
            children: [
              if (widget.obrigatoria) ...[
                const Text('Sua senha atual é provisória. Crie uma senha só sua para continuar.'),
                const SizedBox(height: 20),
              ],
              if (_erro != null) ...[AvisoErro(_erro!), const SizedBox(height: 16)],
              TextFormField(
                controller: _atual,
                obscureText: true,
                decoration: InputDecoration(labelText: widget.obrigatoria ? 'Senha provisória' : 'Senha atual'),
                validator: (v) => (v ?? '').isEmpty ? 'Informe a senha atual.' : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _nova,
                obscureText: true,
                decoration: const InputDecoration(labelText: 'Nova senha', helperText: 'Mínimo de 8 caracteres, com letras e números.'),
                validator: _validarNova,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _confirmacao,
                obscureText: true,
                decoration: const InputDecoration(labelText: 'Confirme a nova senha'),
                validator: (v) => v == _nova.text ? null : 'As senhas não são iguais.',
              ),
              const SizedBox(height: 24),
              FilledButton(onPressed: _enviando ? null : _salvar, child: Text(_enviando ? 'Salvando…' : 'Salvar nova senha')),
            ],
          ),
        ),
      ),
    );
  }
}
