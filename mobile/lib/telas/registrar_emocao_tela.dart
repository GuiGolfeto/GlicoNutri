import 'package:flutter/material.dart';

import '../api/cliente_api.dart';
import '../api/modelos.dart';
import '../componentes/comuns.dart';
import '../escopo.dart';
import '../tema.dart';
import 'seletor_emocoes.dart';

/// UC012 — Registrar Emoção. Sempre opcional (RN22).
class RegistrarEmocaoTela extends StatefulWidget {
  const RegistrarEmocaoTela({super.key});

  @override
  State<RegistrarEmocaoTela> createState() => _RegistrarEmocaoTelaState();
}

class _RegistrarEmocaoTelaState extends State<RegistrarEmocaoTela> {
  final _descricao = TextEditingController();
  final Set<int> _escolhidas = {};
  int _intensidade = 3;
  DateTime? _dataEditada;
  bool _enviando = false;
  String? _erro;

  @override
  void dispose() {
    _descricao.dispose();
    super.dispose();
  }

  Future<void> _salvar() async {
    // A3 do UC012 — ao menos uma emoção.
    if (_escolhidas.isEmpty) {
      setState(() => _erro = 'Selecione ao menos uma emoção.');
      return;
    }
    final escopo = Escopo.of(context);
    setState(() {
      _enviando = true;
      _erro = null;
    });
    try {
      await escopo.api.registrarEmocao(
        escopo.pacienteId,
        estados: _escolhidas.toList(),
        intensidade: _intensidade,
        dataHora: _dataEditada ?? DateTime.now(),
        descricao: _descricao.text.trim().isEmpty ? null : _descricao.text.trim(),
      );
      escopo.dadosMudaram();
      if (!mounted) return;
      mostrarMensagem(context, 'Registro salvo.');
      Navigator.of(context).pop(true);
    } on ErroApi catch (e) {
      if (mounted) setState(() => _erro = e.mensagem);
    } finally {
      if (mounted) setState(() => _enviando = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final escopo = Escopo.of(context);
    return Scaffold(
      backgroundColor: Cores.superficie,
      appBar: AppBar(title: const Text('Como me sinto')),
      body: Carga<List<Referencia>>(
        buscar: () => escopo.api.referencias('estados-emocionais'),
        construir: (context, emocoes, _) => SafeArea(
          child: ListView(
            padding: const EdgeInsets.fromLTRB(20, 20, 20, 24),
            children: [
              const Text('Registrar como você se sente ajuda seu nutricionista a entender o que influencia sua glicemia.'),
              const SizedBox(height: 20),
              SeletorEmocoes(
                emocoes: emocoes,
                escolhidas: _escolhidas,
                intensidade: _intensidade,
                aoAlternar: (id) => setState(() {
                  if (!_escolhidas.remove(id)) _escolhidas.add(id);
                  // O aviso do A3 respondia à seleção vazia; com uma emoção escolhida, ele
                  // passaria a contradizer a tela.
                  if (_escolhidas.isNotEmpty) _erro = null;
                }),
                aoMudarIntensidade: (v) => setState(() => _intensidade = v),
              ),
              const SizedBox(height: 20),
              Text('Data e hora', style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: 10),
              CampoDataHora(valor: _dataEditada ?? DateTime.now(), aoMudar: (d) => setState(() => _dataEditada = d)),
              const SizedBox(height: 20),
              TextField(
                controller: _descricao,
                maxLength: 500,
                minLines: 2,
                maxLines: 4,
                textCapitalization: TextCapitalization.sentences,
                decoration: const InputDecoration(labelText: 'Quer contar o que aconteceu? (opcional)', alignLabelWithHint: true),
              ),
              if (_erro != null) ...[const SizedBox(height: 8), AvisoErro(_erro!)],
              const SizedBox(height: 16),
              FilledButton(onPressed: _enviando ? null : _salvar, child: Text(_enviando ? 'Salvando…' : 'Salvar')),
            ],
          ),
        ),
      ),
    );
  }
}
