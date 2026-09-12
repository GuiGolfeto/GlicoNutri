# -*- coding: utf-8 -*-
"""Texto do documento técnico do back-end."""

CAPA = {
    "sobretitulo": "Documentação técnica",
    "titulo": "Back-end",
    "subtitulo": "API REST, modelo de dados, regras de negócio e decisões de "
                 "implementação do GlicoNutri",
    "meta": [
        ("Preparado para", "Mateus Rossini Marques Pêgo"),
        ("Projeto", "GlicoNutri · TCC em parceria com a ADJ de Birigui"),
        ("Camada", "API em C# / ASP.NET sobre PostgreSQL"),
        ("Documento", "Referência técnica do back-end"),
    ],
}

SUMARIO = [
    ("Arquitetura", "Camadas, fluxo de uma requisição e correspondência com o C4"),
    ("Tecnologias e dependências", "O que foi escolhido e por quê"),
    ("Modelo de dados", "As tabelas, herança, exclusão lógica e constraints"),
    ("Segurança e autenticação", "Credenciais, sessão, bloqueio e controle de acesso"),
    ("Cálculos clínicos", "As fórmulas implementadas, termo a termo"),
    ("Regras de negócio", "Onde cada uma das 35 é aplicada"),
    ("Serviços", "Responsabilidade de cada um"),
    ("Superfície da API", "Os 81 endpoints, agrupados por controlador"),
    ("Importação da tabela TACO", "O que o arquivo exige do leitor"),
    ("Modelo de leitura do painel", "Por que o dashboard não consulta as tabelas de escrita"),
    ("Testes", "Cobertura e verificação por mutação"),
    ("Configuração e execução", "Variáveis, segredos e como subir"),
    ("Defeitos corrigidos", "Erros que só apareceram com o sistema em execução"),
    ("Dívida técnica", "O que ficou pendente e o custo de cada item"),
]

ARQUITETURA = {
    "intro": "A API concentra toda a regra de negócio. O sistema web e o aplicativo mobile são "
             "clientes: nenhum deles acessa o banco diretamente, e nenhum reimplementa validação "
             "clínica. Essa escolha mantém a regra num lugar só e faz o aplicativo herdar, pronto, "
             "tudo que já foi validado pela web.",
    "camadas": [
        ("Controllers", "Recebem a requisição, validam o formato e delegam. Não contêm regra de "
         "negócio: no máximo decidem o código de resposta e verificam a permissão de acesso ao "
         "paciente. São 15, um por área."),
        ("Services", "Onde vive a regra. Cada um responde por um caso de uso ou um agregado, e é "
         "onde as regras de negócio estão codificadas e comentadas com o número correspondente. "
         "São 24, somando cerca de 4.400 linhas."),
        ("Repositories e DbContext", "Acesso a dados via Entity Framework Core. O DbContext "
         "concentra o mapeamento, os filtros de exclusão lógica e a conversão de nomes para o "
         "padrão do banco."),
        ("Models", "As entidades do domínio, espelhando o diagrama de classes e o modelo "
         "entidade-relacionamento."),
    ],
    "fluxo": [
        ("1. Autenticação", "O middleware de JWT valida assinatura, emissor, audiência e validade. "
         "Um token inválido nunca chega ao controlador."),
        ("2. Senha provisória", "Um middleware próprio recusa qualquer rota que não seja a troca de "
         "senha enquanto a credencial ainda for a provisória gerada pelo sistema."),
        ("3. Autorização por perfil", "As políticas de Paciente, Nutricionista e Administrador são "
         "verificadas pelo ASP.NET. O administrador recebe também a função de nutricionista no "
         "token, o que resolve a herança de permissões sem regra condicional espalhada."),
        ("4. Permissão sobre o paciente", "Para rotas clínicas, um serviço dedicado decide se o "
         "usuário pode acessar aquele paciente: o próprio paciente, o nutricionista vinculado ou o "
         "administrador."),
        ("5. Regra de negócio", "O serviço aplica as validações e persiste. Regras que dependem do "
         "estado do banco — como a unicidade de plano ativo — são reforçadas por constraint."),
        ("6. Modelo de leitura", "Serviços que escrevem dado clínico atualizam a tabela de resumo "
         "usada pelo painel, mantendo o dashboard isolado das tabelas transacionais."),
    ],
    "c4": [
        ("JwtMiddleware", "Autenticação JWT do ASP.NET, configurada em Program.cs"),
        ("RbacMiddleware", "Políticas de autorização por perfil, mais o middleware de senha provisória"),
        ("AuthController e AuthService", "Implementados com o mesmo nome"),
        ("PacienteService e NutricionistaService", "Implementados com o mesmo nome"),
        ("GlicemiaService, AntropometriaService, EmocaoService", "Os dois primeiros com o mesmo nome; "
         "o terceiro chama-se RegistroEmocionalService"),
        ("CalculadoraNutricionalService", "Implementado como CalculadoraNutricional, classe estática "
         "sem estado — os cálculos são funções puras"),
        ("ImportadorTACOService", "Implementado como ImportadorTacoService, com a leitura do arquivo "
         "isolada em LeitorCsvTaco"),
        ("PlanoAlimentarService, AlertaService, DashboardService, RelatorioService", "Implementados "
         "com o mesmo nome"),
        ("ConteudoEducativoService", "Implementado como RepositorioEducativoService, cobrindo "
         "conteúdos e receitas"),
    ],
}

TECNOLOGIAS = {
    "intro": "As escolhas seguem o que o projeto de pesquisa definiu, com uma substituição no banco.",
    "itens": [
        ("Linguagem e framework", ".NET 10 · C#", "Definido no projeto de pesquisa. A versão 10 é a "
         "mais recente com suporte de longo prazo."),
        ("Acesso a dados", "Entity Framework Core 10 · Npgsql 10", "ORM oficial da plataforma, com o "
         "provedor PostgreSQL. As migrations versionam o schema."),
        ("Banco de dados", "PostgreSQL 17 no Supabase", "O projeto de pesquisa previa PostgreSQL. O "
         "Supabase entra como serviço gerenciado desse mesmo banco: a API continua acessando por "
         "conexão direta e Entity Framework, sem usar a API própria do Supabase, o que mantém o "
         "diagrama de containers verdadeiro."),
        ("Autenticação", "JWT Bearer · BCrypt.Net-Next", "Token próprio emitido pela API. O bcrypt "
         "atende à exigência de hash seguro."),
        ("Leitura de CSV", "CsvHelper", "Biblioteca citada no relatório de atividades da semana 20 "
         "como caminho para importar a tabela nutricional."),
        ("Geração de PDF", "QuestPDF", "Licença Community, gratuita para este uso. Gera os relatórios "
         "clínicos previstos no diagrama de classes."),
        ("Envio de e-mail", "MailKit", "Biblioteca recomendada para SMTP em .NET."),
        ("Login federado", "Google.Apis.Auth", "Valida o token de identidade emitido pelo Google, "
         "conforme a regra de negócio 01."),
        ("Containerização", "Docker · Docker Compose", "O Postgres de desenvolvimento sobe em "
         "container, o que dispensa instalar banco na máquina de quem for trabalhar no projeto. A "
         "API também é publicada como imagem: é a containerização que define quais hospedagens "
         "gratuitas conseguem receber o sistema."),
        ("Testes", "xUnit · Microsoft.AspNetCore.Mvc.Testing", "Testes unitários e de integração, "
         "estes últimos subindo a aplicação inteira contra um banco descartável."),
    ],
}

DADOS = {
    "intro": "O modelo segue o diagrama entidade-relacionamento versão 2.0. Os nomes de tabela e "
             "coluna do banco reproduzem exatamente os do documento. São 30 tabelas de domínio, "
             "mais a tabela de controle das migrations, mantida pelo Entity Framework.",
    "grupos": [
        ("Identidade e autenticação", ["usuarios", "nutricionistas", "pacientes", "administradores",
                                        "nutricionista_paciente"],
         "Hierarquia com herança Table-Per-Hierarchy: uma única tabela usuarios guarda paciente, "
         "nutricionista e administrador, e a coluna de perfil diz o que cada linha é. As tabelas "
         "por subtipo do modelo entidade-relacionamento versão 2.0 deixaram de existir."),
        ("Plano alimentar", ["planos_alimentares", "distribuicao_macronutrientes",
                              "itens_plano_alimentar", "necessidades_energeticas"],
         "O plano referencia paciente e nutricionista, tem exatamente uma distribuição de "
         "macronutrientes e vários itens de refeição."),
        ("Banco de alimentos", ["alimentos", "receitas", "ingredientes_receita"],
         "Os ingredientes de uma receita apontam para alimentos cadastrados, o que permite calcular "
         "os valores nutricionais a partir da base."),
        ("Registros clínicos", ["registros_glicemia", "registros_antropometricos",
                                 "registros_emocionais"],
         "Séries temporais por paciente, com índice composto de paciente e data para as consultas "
         "por período."),
        ("Alertas", ["alertas", "historico_alertas"],
         "A configuração fica em alertas; cada disparo e seu desfecho ficam no histórico."),
        ("Conteúdo educativo", ["conteudos_educativos"],
         "Artigos, dicas, receitas e vídeos publicados pelos profissionais."),
        ("Modelo de leitura", ["resumo_clinico_paciente (view)"],
         "Uma linha por paciente, alimentada pelos serviços de escrita e consumida apenas pelo painel."),
        ("Tabelas de referência", ["perfis_usuario", "sexos_biologicos", "tipos_diabetes",
                                    "contextos_glicemia", "estados_emocionais", "formulas_energeticas",
                                    "niveis_atividade", "tipos_conteudo", "tipos_alerta",
                                    "status_envio", "fontes_alimento"],
         "Substituem as enumerações estáticas, conforme a mudança C1 do diagrama de classes versão "
         "5.0. Todas seguem o mesmo formato: identificador, código, descrição e situação."),
    ],
    "decisoes": [
        ("Herança Table-Per-Hierarchy",
         "O discriminador é a própria coluna de perfil, que já era chave estrangeira obrigatória: "
         "uma segunda coluna com o tipo diria o mesmo e poderia divergir dela. Em troca, o que era "
         "obrigatório apenas em um subtipo não pode mais ser NOT NULL, porque a coluna fica nula "
         "nas linhas dos outros perfis — a obrigatoriedade virou verificação condicionada ao perfil, "
         "declarada no banco."),
        ("Exclusão lógica com filtro global",
         "Doze entidades têm o campo de situação e um filtro global que as esconde quando inativas. "
         "Dependentes sem campo próprio — histórico de alertas, distribuição de macronutrientes, "
         "ingredientes e o resumo clínico — herdam o filtro do principal, senão sobreviveriam à "
         "exclusão de quem os governa."),
        ("Alimentos fora do filtro global",
         "A regra de negócio 12 é específica: alimentos inativados somem das buscas, mas seus dados "
         "são preservados para os planos que os referenciam. Um filtro global os esconderia também "
         "quando alcançados por um item de plano, quebrando justamente o histórico que a regra manda "
         "preservar. A exclusão é aplicada nas consultas de busca."),
        ("Conversão de nomes para o padrão do banco",
         "Um conversor transforma os nomes das classes em snake_case, reproduzindo o documento. Os "
         "dígitos exigiram regra própria: separador antes de um número precedido de letra, e nunca "
         "entre o número e a letra seguinte. Sem isso saíam calorias_por100g e media_glicemia7_dias, "
         "divergindo do modelo."),
        ("Regras críticas no próprio banco",
         "A unicidade de plano ativo por paciente é um índice único parcial, e as faixas de glicemia "
         "e de intensidade emocional são restrições de verificação. A regra vale mesmo que a "
         "aplicação erre."),
    ],
}

SEGURANCA = {
    "intro": "A API é a única fronteira de segurança. Nenhum cliente acessa o banco, e a verificação "
             "de permissão acontece no servidor mesmo quando a interface já a espelha.",
    "itens": [
        ("Armazenamento de credenciais",
         "Senhas apenas em hash bcrypt, com fator de custo 12 — cerca de 250 milissegundos por "
         "verificação no hardware atual, o que torna a força bruta cara. O salt por hash impede "
         "tabelas pré-computadas. Um hash corrompido é tratado como credencial inválida em vez de "
         "derrubar a requisição."),
        ("Sessão",
         "Token JWT assinado com HMAC-SHA256, válido por 24 horas, contendo identificador, e-mail, "
         "perfil e o indicador de senha provisória. É renovado enquanto o sistema está em uso, para "
         "a sessão não cair no meio de um atendimento."),
        ("Bloqueio progressivo",
         "Cinco tentativas inválidas consecutivas bloqueiam a conta. A espera cresce a cada "
         "bloqueio: cinco minutos, trinta minutos e, do terceiro em diante, vinte e quatro horas. O "
         "nível é preservado após o desbloqueio, para que o próximo seja mais longo. O acerto da "
         "senha zera o ciclo. O tempo restante volta na resposta, para a tela informar."),
        ("Recuperação de senha",
         "O link vale trinta minutos e é de uso único sem tabela de tokens: ele carrega uma "
         "impressão digital do hash atual da senha, que deixa de conferir assim que a senha muda. "
         "Continua disponível com a conta bloqueada, e concluí-lo libera o acesso."),
        ("Não revelação de contas",
         "Login com e-mail inexistente, com conta desativada ou com senha errada devolvem a mesma "
         "mensagem. A recuperação responde igual exista ou não a conta."),
        ("Controle de acesso por perfil",
         "Três políticas, verificadas pelo ASP.NET. O administrador recebe também a função de "
         "nutricionista no token: um endpoint marcado para nutricionista passa a aceitá-lo sem "
         "condicional espalhada pelo código."),
        ("Permissão sobre o paciente",
         "Rotas clínicas passam por um serviço que decide caso a caso: o próprio paciente, o "
         "nutricionista com vínculo ativo, ou o administrador. É o que implementa a proteção de "
         "dados de saúde exigida pela regra 33."),
        ("Login federado",
         "O cliente obtém o token de identidade no Google e o envia à API, que valida assinatura e "
         "audiência. Não cria conta: e-mail sem usuário ativo recebe mensagem informativa e acesso "
         "negado. O bloqueio por tentativas vale igualmente por essa via, para que trocar de forma "
         "de entrada não seja atalho."),
        ("Segredos fora do repositório",
         "Connection string, chave de assinatura, credenciais de e-mail e identificador do Google "
         "ficam no armazenamento de segredos do .NET. O repositório traz apenas o arquivo de exemplo."),
    ],
}

CALCULOS = {
    "intro": "Os cálculos que sustentam a prescrição, com os coeficientes exatos. Todos são funções "
             "puras, sem estado, e é sobre eles que a maior parte dos testes incide.",
    "formulas": [
        ("Índice de massa corporal",
         "IMC = peso (kg) ÷ altura (m)²",
         "A altura é persistida em centímetros, como o caso de uso 008 a coleta, e convertida no "
         "cálculo. O valor aceito vai de 50 a 250 centímetros: a faixa recusa altura digitada em "
         "metros, um erro que passaria despercebido e falsearia todo o resultado."),
        ("Classificação do índice",
         "abaixo de 18,5 · de 18,5 a 24,9 · de 25 a 29,9 · 30 ou mais",
         "Abaixo do peso, peso normal, sobrepeso e obesidade, conforme o fluxo A2 do caso de uso 008. "
         "A Organização Mundial da Saúde ainda subdivide a obesidade em graus; estender as faixas é "
         "uma linha, se a equipe de Nutrição preferir."),
        ("Relação cintura-quadril",
         "RCQ = circunferência da cintura ÷ circunferência do quadril",
         "Calculada apenas quando as duas medidas existem. Faltando uma, o campo fica vazio em vez "
         "de receber um valor derivado de dado ausente."),
        ("Harris-Benedict — masculino",
         "TMB = 88,362 + (13,397 × peso) + (4,799 × altura) − (5,677 × idade)",
         "Revisão de Roza e Shizgal, de 1984."),
        ("Harris-Benedict — feminino",
         "TMB = 447,593 + (9,247 × peso) + (3,098 × altura) − (4,330 × idade)",
         "A equação original, de 1919, usa coeficientes diferentes — 66,473, 13,7516, 5,0033 e 6,755 "
         "para homens; 655,0955, 9,5634, 1,8496 e 4,6756 para mulheres — e ainda é ensinada em parte "
         "dos cursos. Qual das duas vale é decisão da equipe de Nutrição."),
        ("Mifflin-St Jeor",
         "TMB = (10 × peso) + (6,25 × altura) − (5 × idade) + 5 para homens, ou − 161 para mulheres",
         "Segunda fórmula prevista pela regra 14. A diferença entre os sexos é apenas o termo constante."),
        ("Valor energético total",
         "VET = TMB × fator de atividade",
         "Os fatores acompanham a tabela de referência de níveis de atividade: 1,2 para sedentário; "
         "1,375 levemente ativo; 1,55 moderadamente ativo; 1,725 muito ativo; 1,9 extremamente ativo. "
         "Guardar o fator como coluna da tabela de referência foi consequência de converter as "
         "enumerações em entidades — ele é dado de domínio, e não caberia num tipo do banco."),
        ("Distribuição de macronutrientes",
         "gramas = VET × percentual ÷ fator calórico",
         "Fatores de Atwater: 4 quilocalorias por grama de carboidrato e de proteína, 9 por grama de "
         "lipídio. A distribuição padrão é 55, 20 e 25 por cento, que fica dentro das faixas do caso "
         "de uso 007 e soma exatamente cem."),
        ("Contribuição de um item do plano",
         "valor = valor por 100 g × quantidade ÷ 100",
         "A quantidade é registrada em gramas. Valor ausente na base não vira zero: a soma o ignora "
         "e o sistema sinaliza que o total está subestimado."),
        ("Classificação glicêmica",
         "abaixo de 70 · de 70 a 180 · acima de 180",
         "Hipoglicemia, normal e hiperglicemia, por limiar clínico absoluto, conforme a regra 02 do "
         "caso de uso 004. É informação distinta da faixa alvo individual, e as duas convivem."),
        ("Marcação de fora do alvo",
         "fora = valor < mínimo do paciente, ou valor > máximo do paciente",
         "Usa a faixa individual definida pelo nutricionista. Sem personalização, aplica-se o padrão "
         "clínico de 70 a 180 e o sistema sinaliza a pendência. Um valor de 168 num paciente com alvo "
         "estreito está fora do alvo sem ser hiperglicemia."),
        ("Idade em anos completos",
         "diferença de anos, subtraindo um se o aniversário ainda não ocorreu",
         "Para quem nasceu em 29 de fevereiro, o sistema segue o comportamento padrão da plataforma, "
         "que ajusta a data para 28 de fevereiro em anos não bissextos. Não há convenção única; a "
         "escolhida está fixada por teste, e a diferença é de um dia a cada quatro anos."),
    ],
}

TACO = {
    "intro": "A tabela TACO é publicada em PDF e em planilha, e chega ao sistema como CSV. O arquivo "
             "real impõe um conjunto de particularidades que um leitor ingênuo não sobrevive.",
    "desafios": [
        ("Codificação", "Exports gerados pelo Excel saem em Windows-1252, não em UTF-8. Ler como "
         "UTF-8 corromperia os nomes acentuados — que são a chave de duplicidade da regra 11, e "
         "portanto o que impede o mesmo alimento de entrar duas vezes. O leitor tenta UTF-8 estrito "
         "e cai para Windows-1252 quando os bytes não formam UTF-8 válido."),
        ("Separador e decimal", "CSV brasileiro usa ponto e vírgula como separador e vírgula como "
         "decimal, e nomes como \"Arroz, integral, cozido\" trazem vírgulas dentro do próprio campo. "
         "O separador é detectado pela primeira linha, e o milhar com ponto é tratado."),
        ("Marcadores no lugar de números", "Traço indica quantidade desprezível porém presente, e "
         "vira zero. Asterisco e NA indicam valor não determinado, e viram ausência de dado. Zerar "
         "um valor não medido produziria um plano alimentar com contas erradas e nada na tela "
         "denunciando."),
        ("Títulos de seção", "A planilha separa os alimentos por grupo, e o título ocupa a própria "
         "coluna de descrição. O que distingue um título de um alimento não é o valor ausente, e sim "
         "a célula vazia: a quarta edição publica asterisco em todos os macronutrientes de seis itens "
         "reais, entre eles o leite de vaca líquido e o sal. Decidir pelo valor nulo descartaria esses "
         "alimentos em silêncio."),
        ("Cabeçalhos variados", "A tabela circula em versões com títulos diferentes. O mapeamento é "
         "por nome, com sinônimos aceitos para cada campo, e não por posição — arquivos com colunas a "
         "mais ou em outra ordem continuam funcionando."),
        ("Relatório de erros", "Linhas inválidas não interrompem a importação: entram num relatório "
         "com o número da linha, o conteúdo e o motivo, exibido na tela ao final."),
    ],
    "limites": [
        ("Índice glicêmico", "A tabela não publica o dado em nenhum dos 597 alimentos. O campo existe "
         "na modelagem e fica vazio nos importados."),
        ("Seis alimentos sem macronutrientes", "Leite de vaca integral e desnatado líquidos, sal "
         "grosso, sal dietético, coco verde e iogurte de abacaxi trazem valor não determinado em "
         "energia, proteína, lipídeos e carboidrato."),
    ],
}

LEITURA = {
    "intro": "O painel do nutricionista lê de uma projeção própria, separada das tabelas de escrita. "
             "Essa separação foi orientada na modelagem; a forma como ela é construída mudou depois "
             "da revisão do professor orientador.",
    "como": [
        ("Uma view, não uma tabela", "O resumo clínico era uma tabela física que guardava indicadores "
         "já calculados. Guardar dado derivado de outras tabelas viola a terceira forma normal, e foi "
         "o que a orientação apontou. Hoje é uma view: cada coluna é calculada na hora da consulta, a "
         "partir das tabelas transacionais."),
        ("Sem atualização a cargo dos serviços", "Antes, todo serviço que gravava dado clínico "
         "precisava lembrar de atualizar o resumo depois. Esquecer era deixar o painel mostrando "
         "número velho. Com a view não existe versão desatualizada para corrigir, e a camada de "
         "serviço perdeu cerca de cento e cinquenta linhas dedicadas a essa sincronização."),
        ("O que a view calcula", "Última glicemia com contexto e data, média e percentual no alvo dos "
         "últimos sete dias, último índice de massa corporal com a classificação, situação do plano, "
         "contagem de disparos pendentes e dias sem registro. Medições e materiais removidos por "
         "exclusão lógica ficam de fora, como nas telas."),
        ("O relatório faz o contrário", "O serviço de relatórios lê das tabelas de escrita, porque "
         "precisa do histórico completo e não de indicadores consolidados. A separação entre os dois "
         "está explícita no diagrama de componentes."),
    ],
    "efeito": "O cartão de registros do dia conta pacientes que registraram hoje, e não a quantidade "
              "de registros. O protótipo rotula o cartão de outra forma, e a diferença está "
              "documentada.",
}

DEFEITOS = {
    "intro": "Erros que compilavam sem aviso e só apareceram com o sistema em execução ou contra o "
             "banco real. Estão aqui porque cada um mostra um limite de confiar apenas na compilação.",
    "itens": [
        ("Validação ignorada em silêncio",
         "Atributos de validação em parâmetros de construtor primário de record eram ignorados pelo "
         "ASP.NET, que lançava exceção ao validar o modelo. O login respondia erro interno. Os "
         "atributos precisam ficar no parâmetro, não na propriedade."),
        ("Claims remapeadas",
         "O manipulador de JWT converte, por padrão, os nomes padronizados de identificador e e-mail "
         "para URIs de outro vocabulário. A leitura pelo nome original devolvia vazio, o que quebrou "
         "primeiro a rota de perfil e depois toda a recuperação de senha."),
        ("Ordem de gravação contra constraint",
         "Ao trocar o plano ativo, o Entity Framework emitia a atualização que ativa antes da que "
         "desativa, e o índice único parcial recusava o lote. Se a regra estivesse apenas no serviço, "
         "o defeito teria passado e o paciente ficaria com dois planos ativos. A liberação do lugar "
         "agora é gravada antes da ocupação, dentro de uma transação."),
        ("Valor derivado congelado",
         "Alterar a faixa glicêmica alvo não reclassificava os registros anteriores. O painel exibia "
         "a faixa nova com a contagem antiga — um número errado na frente de quem decide. A marcação "
         "de fora do alvo é valor derivado da faixa vigente, não um fato histórico."),
        ("Nomes de coluna divergentes do modelo",
         "O conversor de nomes separava dígitos no lugar errado, produzindo nove colunas diferentes "
         "do documentado. Foi corrigido no conversor, e não coluna a coluna."),
        ("Alimento descartado como título de seção",
         "A heurística que reconhece títulos de grupo na planilha decidia pelo valor nulo, e "
         "descartava seis alimentos reais que a tabela publica sem macronutrientes. Eles sumiam sem "
         "aparecer no relatório de erros."),
        ("Informação nutricional incompleta sem aviso",
         "A marcação de completude das receitas checava apenas os macronutrientes, e dava por completa "
         "uma receita com óleo, cuja fibra a tabela não determina. O total de fibra saía subestimado "
         "em silêncio."),
        ("Administrador impedido de criar plano",
         "A tabela de planos exige um nutricionista, e o administrador não é um — embora a regra 04 "
         "diga que ele herda as permissões. O plano criado por ele agora é atribuído ao profissional "
         "responsável pelo paciente."),
        ("Comparativo retroativo inconsistente",
         "Um registro antropométrico lançado com data anterior comparava-se à medição mais recente, e "
         "não à imediatamente anterior no tempo. O mesmo registro mostrava uma variação no cadastro e "
         "outra no histórico."),
    ],
}

DIVIDA = {
    "intro": "O que ficou pendente, com o custo de cada item. Nada aqui impede o sistema de funcionar.",
    "itens": [
        ("Campos previstos nos casos de uso", "Exige colunas novas",
         "CPF, data de nascimento e sexo do nutricionista; endereço do paciente; observações no "
         "registro antropométrico; nome do plano alimentar, hoje gravado no campo de objetivo."),
        ("Criptografia por coluna", "Depende de interpretação do requisito",
         "O critério do requisito não funcional 01 pede dados de saúde criptografados no banco. O "
         "Supabase criptografa em repouso no nível de disco. Criptografia por coluna quebraria busca, "
         "ordenação e as agregações do painel."),
        ("Publicação com HTTPS", "Depende de decisão de hospedagem",
         "A regra 31 exige comunicação criptografada. O redirecionamento e o cabeçalho de segurança já "
         "são aplicados fora do ambiente de desenvolvimento; falta definir onde publicar. Como a API "
         "roda em container, a hospedagem precisa aceitar imagem Docker — critério que descarta "
         "plataformas restritas a aplicações estáticas ou a funções em JavaScript."),
        ("Índice glicêmico sem valores", "Depende da tabela de referência",
         "O alimento já guarda o índice e a origem do número, e o nutricionista pode informar o valor. "
         "A carga a partir das tabelas internacionais de 2021 ainda não foi feita, então os alimentos "
         "importados da tabela nutricional brasileira seguem sem o dado."),
        ("Conta de e-mail sem senha de aplicativo", "Depende de credencial",
         "A conta de envio existe e o serviço de SMTP está implementado. O Google recusa a senha comum "
         "da conta em conexões SMTP: é preciso gerar uma senha de aplicativo, com a verificação em "
         "duas etapas ativa."),
        ("Conexão em modo transação", "Ajuste na string de conexão",
         "O acesso ao Supabase usa o modo sessão, exigido pelas migrations. Para produção, o modo "
         "transação é mais eficiente, mas precisa desabilitar prepared statements na string."),
        ("Cobertura de testes de serviço", "Ampliação da suíte",
         "Os testes cobrem os cálculos clínicos e as regras críticas. Serviços como o do repositório "
         "educativo e o de relatórios não têm teste automatizado próprio."),
    ],
}
