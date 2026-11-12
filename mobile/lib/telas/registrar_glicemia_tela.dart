import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../api/cliente_api.dart';
import '../api/modelos.dart';
import '../componentes/comuns.dart';
import '../escopo.dart';
import '../tema.dart';
import 'seletor_emocoes.dart';

/// UC004 — Registrar Glicemia, com o estado emocional opcional do protótipo.
class RegistrarGlicemiaTela extends StatefulWidget {
  const RegistrarGlicemiaTela({super.key});

  @override
  State<RegistrarGlicemiaTela> createState() => _RegistrarGlicemiaTelaState();
}

class _RegistrarGlicemiaTelaState extends State<RegistrarGlicemiaTela> {
  final _valor = TextEditingController();
  final _observacao = TextEditingController();

  List<Referencia>? _contextos;
  List<Referencia> _emocoes = [];
  String? _erroCarga;

  int? _contextoId;
  final Set<int> _emocoesEscolhidas = {};
  int _intensidade = 3;

  // Enquanto o paciente não mexer, vale o momento em que tocou em salvar.
  DateTime? _dataEditada;

  String? _erroValor;
  String? _erroContexto;
  String? _erro;
  bool _enviando = false;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_contextos == null && _erroCarga == null) _carregar();
  }

  @override
  void dispose() {
    _valor.dispose();
    _observacao.dispose();
    super.dispose();
  }

  Future<void> _carregar() async {
    final api = Escopo.of(context).api;
    setState(() => _erroCarga = null);
    try {
      final resultados = await Future.wait([api.referencias('contextos-glicemia'), api.referencias('estados-emocionais')]);
      if (!mounted) return;
      setState(() {
        _contextos = resultados[0];
        _emocoes = resultados[1];
      });
    } on ErroApi catch (e) {
      if (mounted) setState(() => _erroCarga = e.mensagem);
    }
  }

  /// A1 do UC004. O zero também sai: a API o aceita, mas é sempre erro de digitação.
  String? _validarValor() {
    final valor = int.tryParse(_valor.text);
    if (valor == null) return 'Informe o valor da glicemia.';
    if (valor < 1 || valor > 600) return 'O valor deve estar entre 1 e 600 mg/dL.';
    return null;
  }

  Future<void> _salvar() async {
    setState(() {
      _erroValor = _validarValor();
      // RN18 — o contexto é obrigatório. Sem pré-seleção: um contexto errado
      // preenchido sozinho contaminaria o histórico sem ninguém notar.
      _erroContexto = _contextoId == null ? 'Selecione o momento da medição.' : null;
      _erro = null;
    });
    if (_erroValor != null || _erroContexto != null) return;

    final escopo = Escopo.of(context);
    final dataHora = _dataEditada ?? DateTime.now();

    // RN04 do UC004 — retroativo sim, futuro não.
    if (dataHora.isAfter(DateTime.now())) {
      setState(() => _erro = 'A data da medição não pode estar no futuro.');
      return;
    }

    setState(() => _enviando = true);

    final RegistroGlicemia registro;
    try {
      registro = await escopo.api.registrarGlicemia(
        escopo.pacienteId,
        valor: int.parse(_valor.text),
        contextoId: _contextoId!,
        dataHora: dataHora,
        observacao: _observacao.text.trim().isEmpty ? null : _observacao.text.trim(),
      );
    } on ErroApi catch (e) {
      if (mounted) {
        setState(() {
          _erro = e.mensagem;
          _enviando = false;
        });
      }
      return;
    }

    // RN22 — a emoção é voluntária e fica ligada à medição pelo horário. A medição
    // já está salva: se só esta parte falhar, reenviar o formulário a duplicaria.
    var emocaoFalhou = false;
    if (_emocoesEscolhidas.isNotEmpty) {
      try {
        await escopo.api.registrarEmocao(
          escopo.pacienteId,
          estados: _emocoesEscolhidas.toList(),
          intensidade: _intensidade,
          dataHora: dataHora,
        );
      } on ErroApi {
        emocaoFalhou = true;
      }
    }

    escopo.dadosMudaram();
    // RN26 — a medição pode encerrar o lembrete em aberto.
    escopo.ciclo.conciliar();

    if (!mounted) return;
    if (registro.classificacao != 'NORMAL') await _alertarClassificacao(registro);
    if (!mounted) return;

    mostrarMensagem(
      context,
      emocaoFalhou
          ? 'Glicemia salva. Não foi possível salvar como você se sente; registre em Mais › Diário emocional.'
          : 'Glicemia registrada.',
    );
    Navigator.of(context).pop(true);
  }

  /// A2 do UC004 — a classificação vem da API, que é quem aplica o limiar clínico.
  Future<void> _alertarClassificacao(RegistroGlicemia r) {
    final baixa = r.classificacao == 'HIPOGLICEMIA';
    return showDialog<void>(
      context: context,
      builder: (contexto) => AlertDialog(
        icon: const Icon(Icons.warning_amber_rounded, color: Cores.perigo, size: 36),
        title: Text(baixa ? 'Glicemia baixa' : 'Glicemia alta'),
        content: Text(
          '${numero(r.valor)} mg/dL indica ${baixa ? 'hipoglicemia (abaixo de 70)' : 'hiperglicemia (acima de 180)'}.\n\n'
          'Siga agora a orientação da sua equipe de saúde. Se tiver sintomas ou não melhorar, procure atendimento médico.',
        ),
        actions: [TextButton(onPressed: () => Navigator.pop(contexto), child: const Text('Entendi'))],
      ),
    );
  }

  Widget _titulo(String texto, {bool opcional = false}) => Padding(
        padding: const EdgeInsets.only(top: 24, bottom: 10),
        child: Text.rich(TextSpan(
          text: texto,
          style: Theme.of(context).textTheme.titleMedium,
          children: [
            if (opcional) const TextSpan(text: '  opcional', style: TextStyle(fontSize: 13, fontWeight: FontWeight.w400, color: Cores.textoApoio)),
          ],
        )),
      );

  @override
  Widget build(BuildContext context) {
    final contextos = _contextos;

    return Scaffold(
      backgroundColor: Cores.superficie,
      appBar: AppBar(title: const Text('Registrar glicemia')),
      body: contextos == null
          ? Center(
              child: _erroCarga == null
                  ? const CircularProgressIndicator()
                  : EstadoVazio(
                      icone: Icons.cloud_off_outlined,
                      titulo: 'Não foi possível abrir o formulário',
                      texto: _erroCarga,
                      acao: OutlinedButton(onPressed: _carregar, child: const Text('Tentar novamente')),
                    ),
            )
          : SafeArea(
              child: ListView(
                padding: const EdgeInsets.fromLTRB(20, 16, 20, 24),
                children: [
                  const Text('Valor da glicemia', textAlign: TextAlign.center),
                  TextField(
                    controller: _valor,
                    autofocus: true,
                    textAlign: TextAlign.center,
                    keyboardType: TextInputType.number,
                    inputFormatters: [FilteringTextInputFormatter.digitsOnly, LengthLimitingTextInputFormatter(3)],
                    style: const TextStyle(fontSize: 64, height: 1.1, fontWeight: FontWeight.w700, color: Cores.texto, fontFeatures: numerico),
                    onChanged: (_) {
                      if (_erroValor != null) setState(() => _erroValor = _validarValor());
                    },
                    decoration: InputDecoration(
                      hintText: '000',
                      hintStyle: const TextStyle(color: Color(0xFFCBD5E1)),
                      suffixText: 'mg/dL',
                      suffixStyle: const TextStyle(fontSize: 16, color: Cores.textoSecundario),
                      errorText: _erroValor,
                      filled: false,
                      border: const UnderlineInputBorder(borderSide: BorderSide(color: Cores.borda)),
                      enabledBorder: const UnderlineInputBorder(borderSide: BorderSide(color: Cores.borda)),
                      focusedBorder: const UnderlineInputBorder(borderSide: BorderSide(color: Cores.primaria, width: 2)),
                    ),
                  ),
                  _titulo('Contexto'),
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: [
                      for (final c in contextos)
                        ChoiceChip(
                          label: Text(c.descricao),
                          selected: _contextoId == c.id,
                          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                          onSelected: (_) => setState(() {
                            _contextoId = c.id;
                            _erroContexto = null;
                          }),
                        ),
                    ],
                  ),
                  if (_erroContexto != null) ...[
                    const SizedBox(height: 6),
                    Text(_erroContexto!, style: const TextStyle(fontSize: 12, color: Cores.perigo)),
                  ],
                  _titulo('Data e hora'),
                  CampoDataHora(
                    valor: _dataEditada ?? DateTime.now(),
                    aoMudar: (d) => setState(() => _dataEditada = d),
                  ),
                  _titulo('Como você está se sentindo?', opcional: true),
                  SeletorEmocoes(
                    emocoes: _emocoes,
                    escolhidas: _emocoesEscolhidas,
                    intensidade: _intensidade,
                    aoAlternar: (id) => setState(() {
                      if (!_emocoesEscolhidas.remove(id)) _emocoesEscolhidas.add(id);
                    }),
                    aoMudarIntensidade: (v) => setState(() => _intensidade = v),
                  ),
                  _titulo('Observações', opcional: true),
                  TextField(
                    controller: _observacao,
                    maxLength: 500,
                    minLines: 2,
                    maxLines: 4,
                    textCapitalization: TextCapitalization.sentences,
                    decoration: const InputDecoration(hintText: 'Ex.: senti tontura, após exercício físico'),
                  ),
                  if (_erro != null) ...[const SizedBox(height: 8), AvisoErro(_erro!)],
                  const SizedBox(height: 16),
                  FilledButton(
                    onPressed: _enviando ? null : _salvar,
                    child: Text(_enviando ? 'Salvando…' : 'Salvar registro'),
                  ),
                ],
              ),
            ),
    );
  }
}
