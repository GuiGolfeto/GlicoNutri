import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

import '../api/cliente_api.dart';
import '../api/modelos.dart';
import '../componentes/comuns.dart';
import '../escopo.dart';
import '../tema.dart';

/// RF09 — repositório educativo, somente leitura para o paciente (RN28).
class RepositorioTela extends StatelessWidget {
  const RepositorioTela({super.key});

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: 2,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Conteúdos e receitas'),
          bottom: const TabBar(tabs: [Tab(text: 'Conteúdos'), Tab(text: 'Receitas')]),
        ),
        body: const TabBarView(children: [_ListaConteudos(), _ListaReceitas()]),
      ),
    );
  }
}

class _Busca extends StatelessWidget {
  const _Busca({required this.dica, required this.aoBuscar});

  final String dica;
  final ValueChanged<String> aoBuscar;

  @override
  Widget build(BuildContext context) {
    return TextField(
      textInputAction: TextInputAction.search,
      onSubmitted: (v) => aoBuscar(v.trim()),
      decoration: InputDecoration(hintText: dica, prefixIcon: const Icon(Icons.search), isDense: true),
    );
  }
}

/// Marca e desmarca favorito (RF09.3). Muda na hora e desfaz se a chamada falhar:
/// esperar o servidor para acender um coração deixa o toque sem resposta.
class BotaoFavorito extends StatefulWidget {
  const BotaoFavorito({super.key, required this.tipo, required this.id, required this.marcado});

  final String tipo;
  final int id;
  final bool marcado;

  @override
  State<BotaoFavorito> createState() => _BotaoFavoritoState();
}

class _BotaoFavoritoState extends State<BotaoFavorito> {
  late bool _marcado = widget.marcado;

  @override
  void didUpdateWidget(BotaoFavorito anterior) {
    super.didUpdateWidget(anterior);
    if (anterior.marcado != widget.marcado) _marcado = widget.marcado;
  }

  Future<void> _alternar() async {
    final escopo = Escopo.of(context);
    final novo = !_marcado;
    setState(() => _marcado = novo);
    try {
      await escopo.api.marcarFavorito(escopo.pacienteId, widget.tipo, widget.id, marcado: novo);
      escopo.dadosMudaram();
    } on ErroApi catch (e) {
      if (!mounted) return;
      setState(() => _marcado = !novo);
      mostrarMensagem(context, e.mensagem);
    }
  }

  @override
  Widget build(BuildContext context) {
    return IconButton(
      tooltip: _marcado ? 'Remover dos favoritos' : 'Guardar nos favoritos',
      onPressed: _alternar,
      icon: Icon(_marcado ? Icons.favorite : Icons.favorite_border, color: _marcado ? Cores.perigo : Cores.textoApoio),
    );
  }
}

class _DadosConteudos {
  const _DadosConteudos(this.itens, this.favoritos);

  final List<Conteudo> itens;
  final Set<int> favoritos;
}

class _ListaConteudos extends StatefulWidget {
  const _ListaConteudos();

  @override
  State<_ListaConteudos> createState() => _ListaConteudosState();
}

class _ListaConteudosState extends State<_ListaConteudos> {
  String _termo = '';

  Future<_DadosConteudos> _buscar() async {
    final e = Escopo.of(context);
    final itens = await e.api.conteudos(termo: _termo);
    final favoritos = await e.api.favoritos(e.pacienteId);
    return _DadosConteudos(itens, {for (final c in favoritos.conteudos) c.id});
  }

  @override
  Widget build(BuildContext context) {
    return Carga<_DadosConteudos>(
      dependencia: _termo,
      buscar: _buscar,
      construir: (context, dados, _) => ListView(
        padding: const EdgeInsets.all(16),
        children: [
          _Busca(dica: 'Buscar conteúdo', aoBuscar: (t) => setState(() => _termo = t)),
          const SizedBox(height: 12),
          if (dados.itens.isEmpty)
            const EstadoVazio(icone: Icons.menu_book_outlined, titulo: 'Nenhum conteúdo encontrado')
          else
            for (final c in dados.itens) ...[
              CartaoConteudo(conteudo: c, marcado: dados.favoritos.contains(c.id)),
              const SizedBox(height: 8),
            ],
        ],
      ),
    );
  }
}

class CartaoConteudo extends StatelessWidget {
  const CartaoConteudo({super.key, required this.conteudo, required this.marcado});

  final Conteudo conteudo;
  final bool marcado;

  @override
  Widget build(BuildContext context) {
    return Cartao(
      aoTocar: () => Navigator.of(context).push(
        MaterialPageRoute<void>(builder: (_) => ConteudoTela(conteudo: conteudo, marcado: marcado)),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(conteudo.tipo.toUpperCase(), style: const TextStyle(fontSize: 11, letterSpacing: 0.5, color: Cores.textoApoio)),
                const SizedBox(height: 2),
                Text(conteudo.titulo, style: Theme.of(context).textTheme.titleMedium),
                if (conteudo.corpo != null) ...[
                  const SizedBox(height: 4),
                  Text(conteudo.corpo!, maxLines: 2, overflow: TextOverflow.ellipsis),
                ],
              ],
            ),
          ),
          BotaoFavorito(tipo: 'conteudos', id: conteudo.id, marcado: marcado),
        ],
      ),
    );
  }
}

class ConteudoTela extends StatelessWidget {
  const ConteudoTela({super.key, required this.conteudo, required this.marcado});

  final Conteudo conteudo;
  final bool marcado;

  /// RF09.2 — mídia é link externo: vídeo e matéria abrem no app de origem, com o
  /// domínio à mostra para o paciente saber para onde está indo.
  Future<void> _abrirMidia(BuildContext context, String url) async {
    final abriu = await launchUrl(Uri.parse(url), mode: LaunchMode.externalApplication);
    if (!abriu && context.mounted) mostrarMensagem(context, 'Não foi possível abrir o material.');
  }

  @override
  Widget build(BuildContext context) {
    final url = conteudo.urlMidia ?? '';
    return Scaffold(
      backgroundColor: Cores.superficie,
      appBar: AppBar(
        title: Text(conteudo.tipo),
        actions: [BotaoFavorito(tipo: 'conteudos', id: conteudo.id, marcado: marcado)],
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            Text(conteudo.titulo, style: Theme.of(context).textTheme.headlineMedium),
            const SizedBox(height: 6),
            Text('Por ${conteudo.autorNome}'),
            const SizedBox(height: 16),
            if (conteudo.corpo != null) Text(conteudo.corpo!, style: Theme.of(context).textTheme.bodyLarge),
            if (linkSeguro(url)) ...[
              const SizedBox(height: 20),
              OutlinedButton.icon(
                onPressed: () => _abrirMidia(context, url),
                icon: const Icon(Icons.open_in_new, size: 18),
                label: Text('Abrir material em ${dominioDe(url)}'),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _DadosReceitas {
  const _DadosReceitas(this.itens, this.favoritos);

  final List<Receita> itens;
  final Set<int> favoritos;
}

class _ListaReceitas extends StatefulWidget {
  const _ListaReceitas();

  @override
  State<_ListaReceitas> createState() => _ListaReceitasState();
}

class _ListaReceitasState extends State<_ListaReceitas> {
  String _termo = '';
  bool _porIngrediente = false;

  Future<_DadosReceitas> _buscar() async {
    final e = Escopo.of(context);
    final itens = await e.api.receitas(
      termo: _porIngrediente ? null : _termo,
      ingrediente: _porIngrediente ? _termo : null,
    );
    final favoritos = await e.api.favoritos(e.pacienteId);
    return _DadosReceitas(itens, {for (final r in favoritos.receitas) r.receitaId});
  }

  @override
  Widget build(BuildContext context) {
    return Carga<_DadosReceitas>(
      dependencia: (_termo, _porIngrediente),
      buscar: _buscar,
      construir: (context, dados, _) => ListView(
        padding: const EdgeInsets.all(16),
        children: [
          _Busca(
            dica: _porIngrediente ? 'Buscar por ingrediente' : 'Buscar receita',
            aoBuscar: (t) => setState(() => _termo = t),
          ),
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            children: [
              ChoiceChip(label: const Text('Por nome'), selected: !_porIngrediente, onSelected: (_) => setState(() => _porIngrediente = false)),
              ChoiceChip(label: const Text('Por ingrediente'), selected: _porIngrediente, onSelected: (_) => setState(() => _porIngrediente = true)),
            ],
          ),
          const SizedBox(height: 12),
          if (dados.itens.isEmpty)
            const EstadoVazio(icone: Icons.restaurant_outlined, titulo: 'Nenhuma receita encontrada')
          else
            for (final r in dados.itens) ...[
              Cartao(
                aoTocar: () => Navigator.of(context).push(
                  MaterialPageRoute<void>(builder: (_) => ReceitaTela(id: r.id, marcado: dados.favoritos.contains(r.id))),
                ),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(r.nome, style: Theme.of(context).textTheme.titleMedium),
                          const SizedBox(height: 4),
                          Text(
                            [
                              if (r.caloriasPorPorcao != null) '${numero(r.caloriasPorPorcao!.round())} kcal por porção' else '${numero(r.calorias.round())} kcal no total',
                              if (r.tempoPreparo != null) '${r.tempoPreparo} min',
                            ].join(' · '),
                            style: const TextStyle(fontFeatures: numerico),
                          ),
                        ],
                      ),
                    ),
                    BotaoFavorito(tipo: 'receitas', id: r.id, marcado: dados.favoritos.contains(r.id)),
                  ],
                ),
              ),
              const SizedBox(height: 8),
            ],
        ],
      ),
    );
  }
}

class ReceitaTela extends StatelessWidget {
  const ReceitaTela({super.key, required this.id, required this.marcado});

  final int id;
  final bool marcado;

  @override
  Widget build(BuildContext context) {
    final escopo = Escopo.of(context);
    return Scaffold(
      backgroundColor: Cores.superficie,
      appBar: AppBar(
        title: const Text('Receita'),
        actions: [BotaoFavorito(tipo: 'receitas', id: id, marcado: marcado)],
      ),
      body: Carga<Receita>(
        buscar: () => escopo.api.receita(id),
        construir: (context, r, _) => ListView(
          padding: const EdgeInsets.all(20),
          children: [
            Text(r.nome, style: Theme.of(context).textTheme.headlineMedium),
            if (r.descricao != null) ...[const SizedBox(height: 6), Text(r.descricao!)],
            const SizedBox(height: 6),
            Text(
              [
                if (r.tempoPreparo != null) '${r.tempoPreparo} min',
                if (r.porcoes != null) '${r.porcoes} porções',
                'Por ${r.nutricionistaNome}',
              ].join(' · '),
              style: Theme.of(context).textTheme.bodySmall,
            ),
            const SizedBox(height: 16),
            Cartao(
              child: Wrap(
                spacing: 20,
                runSpacing: 10,
                children: [
                  if (r.caloriasPorPorcao != null)
                    Indicador(rotulo: 'Por porção', valor: numero(r.caloriasPorPorcao!.round()), unidade: 'kcal'),
                  Indicador(rotulo: 'Total', valor: numero(r.calorias.round()), unidade: 'kcal'),
                  Indicador(rotulo: 'Carboidratos', valor: numero(r.carboidratos.round()), unidade: 'g'),
                  Indicador(rotulo: 'Proteínas', valor: numero(r.proteinas.round()), unidade: 'g'),
                  Indicador(rotulo: 'Lipídios', valor: numero(r.lipidios.round()), unidade: 'g'),
                ],
              ),
            ),
            if (!r.nutricionalCompleta) ...[
              const SizedBox(height: 8),
              Text('Valores aproximados: algum ingrediente não tem informação completa na tabela de alimentos.',
                  style: Theme.of(context).textTheme.bodySmall),
            ],
            const SizedBox(height: 20),
            Text('Ingredientes', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            for (final i in r.ingredientes)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 4),
                child: Text('${numero(i.quantidade)} ${i.unidade ?? 'g'} · ${i.alimentoNome}', style: Theme.of(context).textTheme.bodyLarge),
              ),
            if (r.instrucoes != null) ...[
              const SizedBox(height: 20),
              Text('Modo de preparo', style: Theme.of(context).textTheme.titleMedium),
              const SizedBox(height: 8),
              Text(r.instrucoes!, style: Theme.of(context).textTheme.bodyLarge),
            ],
          ],
        ),
      ),
    );
  }
}
