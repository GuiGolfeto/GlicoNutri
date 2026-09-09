# -*- coding: utf-8 -*-
"""Conteúdo do relatório. Separado do gerador para o texto ficar legível."""

CAPA = {
    "titulo": "GlicoNutri",
    "subtitulo": "Ecossistema digital de apoio ao controle nutricional do diabetes — "
                 "documentação do sistema web e da API",
    "meta": [
        ("Trabalho de Conclusão de Curso", "Mateus Rossini Marques Pêgo · RA 219468"),
        ("Instituição", "UniSalesiano Araçatuba · Tecnologia em Desenvolvimento de Sistemas"),
        ("Parceria", "ADJ — Associação de Diabetes Juvenil de Birigui"),
        ("Orientação", "Prof. Francisco Antônio de Sousa"),
    ],
}

SUMARIO = [
    ("Visão geral do sistema", "Arquitetura, tecnologias e números"),
    ("Autenticação e controle de acesso", "Login, perfis, senha provisória e recuperação"),
    ("Gestão de usuários", "Cadastro de nutricionistas e pacientes"),
    ("Banco de alimentos", "Cadastro manual e importação da tabela TACO"),
    ("Acompanhamento clínico", "Antropometria, necessidade energética, glicemia e emoções"),
    ("Plano alimentar", "Distribuição de macronutrientes e montagem de refeições"),
    ("Alertas e lembretes", "Configuração, disparos e expiração automática"),
    ("Painel do nutricionista", "Indicadores consolidados e pendências"),
    ("Repositório educativo", "Receitas e conteúdos para o paciente"),
    ("Relatórios clínicos", "Exportação em PDF"),
    ("Área do paciente", "Autocuidado pela web"),
    ("Validação automatizada", "Cobertura de testes e verificação por mutação"),
    ("Rastreabilidade", "Requisitos e regras de negócio atendidos"),
    ("Limites conhecidos", "O que não foi implementado e por quê"),
]

VISAO_GERAL = {
    "intro": "O GlicoNutri é composto por uma API REST central, um sistema web para "
             "nutricionistas e administradores, e um aplicativo mobile para os pacientes. "
             "Este documento cobre o que está implementado na API e no sistema web.",
    "indicadores": [
        ("81", "endpoints"),
        ("31", "tabelas"),
        ("134", "testes"),
        ("597", "alimentos"),
    ],
    "camadas": [
        ("API REST", "C# / ASP.NET (.NET 10)",
         "Concentra todas as regras de negócio: cálculos nutricionais, validações clínicas e "
         "controle de acesso por perfil. Organizada em Controllers, Services e Repositories, "
         "como descreve o diagrama de componentes."),
        ("Sistema web", "Vue 3 + TypeScript",
         "Interface de nutricionistas e administradores. Consome exclusivamente a API; "
         "não acessa o banco diretamente."),
        ("Banco de dados", "PostgreSQL (Supabase)",
         "As 31 tabelas do DER V2.0, com herança Table-Per-Type na hierarquia de usuários e "
         "exclusão lógica em toda entidade que guarda histórico clínico."),
        ("Aplicativo mobile", "Flutter",
         "Etapa posterior. Os endpoints que ele consome — registro de glicemia, antropometria, "
         "emoções e o ciclo de alertas — já estão implementados e testados."),
    ],
    "decisoes": [
        ("Regras críticas no banco, não só no serviço",
         "A unicidade de plano alimentar ativo por paciente é garantida por índice único parcial "
         "no PostgreSQL. Durante o desenvolvimento, essa constraint revelou um defeito de ordem de "
         "gravação que a camada de aplicação escondia: um paciente teria ficado com dois planos ativos."),
        ("Modelo de leitura isolado para o painel",
         "O dashboard lê exclusivamente a tabela resumo_clinico_paciente, alimentada pelos serviços "
         "de escrita após cada persistência. Nenhum indicador do painel é calculado sobre as tabelas "
         "transacionais."),
        ("Valor derivado acompanha a regra vigente",
         "A marcação de glicemia fora do alvo é recalculada em todo o histórico quando o nutricionista "
         "altera a faixa individual do paciente. Sem isso, o percentual no alvo misturaria dois critérios."),
    ],
}

SECOES = [
    {
        "titulo": "Autenticação e controle de acesso",
        "intro": "Todo acesso ao sistema exige sessão autenticada. O controle é por perfil, "
                 "e o Administrador herda as permissões do Nutricionista.",
        "requisitos": ["RF10.1", "RF10.4"],
        "regras": ["RN01", "RN02", "RN03", "RN04", "RN31", "RN32"],
        "casos": ["UC001"],
        "telas": [
            ("01-login", "Tela de login. O bloqueio progressivo informa o tempo restante e "
                         "desabilita o botão até a liberação."),
            ("02-recuperar-senha", "Recuperação de senha. A resposta é idêntica exista ou não a "
                                   "conta, para não revelar quais e-mails estão cadastrados."),
        ],
        "funcoes": [
            ("Autenticação por e-mail e senha", "Senhas armazenadas em hash bcrypt com fator de custo 12. "
             "Token JWT com validade de 24 horas, renovado automaticamente enquanto o sistema está em uso."),
            ("Bloqueio progressivo por tentativas inválidas", "Cinco tentativas consecutivas bloqueiam a "
             "conta. A espera cresce a cada bloqueio: 5 minutos, 30 minutos e, do terceiro em diante, "
             "24 horas. A tela mostra a contagem regressiva, e o usuário recebe aviso por e-mail."),
            ("Troca obrigatória da senha provisória", "Quem foi cadastrado por um administrador ou "
             "nutricionista recebe senha gerada pelo sistema e só consegue executar a própria troca de "
             "senha até defini-la."),
            ("Política de senha", "Mínimo de oito caracteres, com letras e números."),
            ("Recuperação de senha", "Link válido por 30 minutos e de uso único: ele carrega uma "
             "impressão digital do hash atual, que deixa de conferir assim que a senha muda. "
             "Permanece disponível mesmo com a conta bloqueada, e concluí-la libera o acesso."),
            ("Login federado com Google", "Implementado. O acesso só é concedido se o e-mail da conta "
             "Google corresponder a um usuário previamente cadastrado e ativo; o bloqueio por tentativas "
             "vale igualmente por essa via. Depende de credencial no Google Cloud Console para ser ativado."),
            ("Controle de acesso por perfil", "Paciente, Nutricionista e Administrador. A verificação "
             "acontece na API, e a interface espelha as mesmas regras para não oferecer o que seria recusado."),
        ],
    },
    {
        "titulo": "Gestão de usuários",
        "intro": "Nenhum usuário se autocadastra. Pacientes são cadastrados por nutricionistas ou "
                 "administradores; nutricionistas, apenas por administradores.",
        "requisitos": ["RF10", "RF10.2", "RF10.3", "RF10.5"],
        "regras": ["RN05", "RN06", "RN07", "RN08", "RN09", "RN24", "RN33"],
        "casos": ["UC002", "UC003"],
        "telas": [
            ("04-pacientes", "Listagem de pacientes com busca por nome ou CPF. A coluna de faixa alvo "
                             "sinaliza quando os limites ainda são o padrão clínico."),
            ("05-paciente-novo", "Cadastro de paciente. O CPF é validado pelo dígito verificador antes "
                                 "do envio, e o vínculo com o nutricionista responsável é obrigatório."),
            ("09-nutricionistas", "Nutricionistas cadastrados, com indicação de quem ainda não fez o "
                                  "primeiro acesso e a contagem de pacientes acompanhados."),
        ],
        "funcoes": [
            ("Cadastro de paciente", "Dados pessoais, tipo de diabetes e vínculo com o nutricionista "
             "responsável. O CPF passa por validação de dígito verificador; e-mail e CPF são únicos em "
             "todo o sistema, inclusive contra contas desativadas."),
            ("Vínculo obrigatório com nutricionista", "Quando quem cadastra é o próprio nutricionista, o "
             "vínculo é automático. Quando é o administrador — que não é nutricionista — o responsável "
             "precisa ser escolhido explicitamente."),
            ("Alertas pré-configurados", "O cadastro de um paciente já cria o alerta de glicemia das "
             "8h e 20h, com mensagem padrão, e o registro de resumo clínico usado pelo painel."),
            ("Cadastro de nutricionista", "CRN único, senha provisória gerada pelo sistema e e-mail de "
             "boas-vindas com as credenciais de primeiro acesso."),
            ("Edição de cadastros", "Dados de pacientes e nutricionistas podem ser corrigidos, com "
             "revalidação da unicidade de e-mail, CPF e CRN."),
            ("Desativação sem perda de histórico", "Desativar é exclusão lógica: nenhum dado clínico é "
             "apagado, e a reativação restaura o acesso a tudo. Um nutricionista com pacientes vinculados "
             "não pode ser desativado antes da transferência."),
            ("Isolamento entre profissionais", "O nutricionista enxerga apenas os pacientes sob sua "
             "responsabilidade — na listagem e no acesso direto por identificador. O administrador vê todos."),
        ],
    },
    {
        "titulo": "Banco de alimentos",
        "intro": "A base nutricional que sustenta a montagem dos planos alimentares. Foi populada com "
                 "os 597 alimentos da Tabela TACO, 4ª edição, publicada pelo NEPA/UNICAMP.",
        "requisitos": ["RF01", "RF01.1", "RF01.2"],
        "regras": ["RN10", "RN11", "RN12"],
        "casos": ["UC009"],
        "telas": [
            ("06-alimentos", "Banco de alimentos com a tabela TACO importada, busca que ignora acentos "
                             "e filtro por grupo alimentar."),
        ],
        "funcoes": [
            ("Importação em lote", "Aceita exclusivamente arquivos CSV compatíveis com a tabela "
             "TACO/IBGE. Cada linha é validada antes da persistência, e as inválidas viram um relatório "
             "de erros sem interromper a importação das demais."),
            ("Tratamento dos marcadores da tabela", "A TACO usa símbolos no lugar de números. "
             "Traço, que indica quantidade desprezível, é convertido em zero. Asterisco e NA, que "
             "indicam valor não determinado, viram ausência de dado — nunca zero, porque zerar um "
             "valor não medido falsearia o cálculo do plano."),
            ("Detecção de codificação e formato", "Exports da tabela costumam sair em Windows-1252. "
             "O sistema detecta a codificação, o separador e o formato decimal, e reconhece os "
             "cabeçalhos em suas várias grafias em circulação."),
            ("Prevenção de duplicatas", "Alimento cujo nome já existe não é importado; a ocorrência "
             "entra no relatório para o administrador decidir pela atualização manual."),
            ("Cadastro manual", "Nome, grupo alimentar e valores por 100 gramas, para itens que não "
             "constam da tabela oficial."),
            ("Busca sem sensibilidade a acento", "Feita no banco pela extensão unaccent. Sem ela, uma "
             "busca por \"feijao\" não encontraria \"Feijão\", e boa parte da tabela ficaria inalcançável."),
            ("Inativação lógica", "Alimento removido deixa de aparecer nas buscas, mas continua "
             "resolvível pelos planos e receitas que o referenciam, preservando a integridade histórica."),
        ],
    },
    {
        "titulo": "Acompanhamento clínico",
        "intro": "Os registros que descrevem a evolução do paciente entre as consultas, e o cálculo "
                 "que fundamenta a prescrição.",
        "requisitos": ["RF02", "RF02.1", "RF02.2", "RF04", "RF04.1", "RF04.2",
                       "RF05", "RF05.1", "RF05.2", "RF06", "RF06.1", "RF06.2", "RF08.1", "RF08.2"],
        "regras": ["RN14", "RN18", "RN19", "RN20", "RN21", "RN22"],
        "casos": ["UC004", "UC005", "UC006", "UC008", "UC012"],
        "telas": [
            ("11-paciente-acompanhamento", "Aba de acompanhamento: monitoramento glicêmico com gráfico "
             "e faixa alvo, medidas antropométricas, cálculo energético e diário emocional."),
        ],
        "funcoes": [
            ("Registro antropométrico", "Peso, altura e circunferências. O índice de massa corporal é "
             "sempre calculado pelo sistema, nunca informado, e classificado segundo as faixas da "
             "Organização Mundial da Saúde. A relação cintura-quadril é derivada quando as duas medidas existem."),
            ("Comparativo entre medições", "Cada registro mostra a variação de peso e IMC em relação à "
             "medição imediatamente anterior no tempo, em valor absoluto e percentual."),
            ("Gráfico de evolução de peso e IMC", "Série cronológica com alternância entre as duas "
             "métricas, que não convivem numa escala única."),
            ("Cálculo da necessidade energética", "Harris-Benedict, na revisão de Roza e Shizgal, ou "
             "Mifflin-St Jeor, aplicadas sobre a medição mais recente. O fator de atividade multiplica a "
             "taxa metabólica basal e produz o valor energético total. A fórmula usada fica registrada em "
             "cada cálculo, e valores calóricos arbitrários não são aceitos."),
            ("Simulação antes de gravar", "O resultado é exibido como taxa metabólica basal, fator "
             "aplicado e valor final, para conferência do nutricionista antes de ir ao prontuário. "
             "Cada cálculo é versionado, e o histórico completo é preservado."),
            ("Registro de glicemia", "Valor em mg/dL, contexto da medição e data e hora, com registro "
             "retroativo permitido e futuro recusado. O sistema classifica automaticamente se o valor "
             "está fora da faixa alvo individual do paciente."),
            ("Faixa alvo individual e limiar clínico", "São informações distintas e convivem. A faixa "
             "alvo é definida pelo nutricionista para cada paciente; sem personalização, o sistema aplica "
             "70 a 180 mg/dL e sinaliza a pendência. Já hipoglicemia e hiperglicemia seguem limiares "
             "clínicos absolutos. Uma medição de 168 mg/dL num paciente com alvo estreito está fora do "
             "alvo sem ser hiperglicemia."),
            ("Histórico e evolução glicêmica", "Painel com total de medições, média, mínima, máxima e "
             "percentual dentro da faixa alvo, filtrável por período. O gráfico desenha a faixa alvo como "
             "banda de fundo e destaca os pontos fora dela."),
            ("Registro emocional", "Opcional por natureza. O paciente seleciona uma ou mais emoções e "
             "gradua a intensidade de um a cinco. A ausência do registro nunca impede o lançamento de "
             "glicemia ou refeição."),
            ("Correlação entre emoção e glicemia", "Para cada estado emocional, o painel do nutricionista "
             "agrega as medições de glicemia próximas no tempo, com média, extremos, percentual fora do "
             "alvo e o desvio em relação à média geral do período."),
            ("Correção de lançamento", "Registros de glicemia, antropometria e emoção podem ser "
             "removidos, com o resumo e as médias recalculando em seguida."),
        ],
    },
    {
        "titulo": "Plano alimentar",
        "intro": "A prescrição nutricional propriamente dita, fundamentada no cálculo energético e "
                 "montada sobre o banco de alimentos.",
        "requisitos": ["RF03", "RF03.1", "RF03.2", "RF03.3"],
        "regras": ["RN13", "RN15", "RN16", "RN17"],
        "casos": ["UC007"],
        "telas": [
            ("12-paciente-plano", "Plano vigente com a distribuição de macronutrientes, os subtotais por "
                                  "refeição e o desvio em relação ao valor energético prescrito."),
        ],
        "funcoes": [
            ("Cálculo energético como pré-requisito", "Não é possível criar plano para paciente sem "
             "necessidade energética calculada. A prescrição precisa de embasamento clínico."),
            ("Distribuição automática de macronutrientes", "Carboidratos, proteínas e lipídios "
             "distribuídos sobre o valor energético total, expressos em percentual e em gramas por dia, "
             "pelos fatores de Atwater."),
            ("Ajuste manual", "O nutricionista pode sobrepor os percentuais. O sistema recalcula as "
             "gramas e impede a confirmação enquanto a soma não fechar exatamente em 100%."),
            ("Montagem das refeições", "Alimentos do banco são adicionados por refeição, com busca "
             "incremental. O sistema apresenta os subtotais de calorias e macronutrientes por refeição."),
            ("Aderência ao valor prescrito", "O total montado é comparado ao valor energético e o desvio "
             "percentual é sinalizado. A margem de 5% é informada, não bloqueada: o profissional pode "
             "prescrever fora dela com intenção clínica."),
            ("Plano ativo único", "Cada paciente tem no máximo um plano vigente. Criar um novo desativa "
             "o anterior automaticamente. A regra é garantida por índice único no banco, e não apenas "
             "pela aplicação."),
            ("Histórico e retomada", "Planos desativados permanecem íntegros, com todos os itens e "
             "cálculos, e podem ser consultados, duplicados como base de um novo ou reativados."),
        ],
    },
    {
        "titulo": "Alertas e lembretes",
        "intro": "Os lembretes que o paciente recebe no aplicativo. A configuração é do profissional; "
                 "ao paciente cabe apenas desativá-los.",
        "requisitos": ["RF07", "RF07.1", "RF07.2"],
        "regras": ["RN23", "RN24", "RN25", "RN26", "RN27", "RN35"],
        "casos": ["UC010"],
        "telas": [
            ("13-paciente-alertas", "Alertas configurados, com horários, dias da semana e o histórico "
                                    "de disparos."),
        ],
        "funcoes": [
            ("Configuração pelo profissional", "Tipo, mensagem personalizada, até dois horários e os "
             "dias da semana. Criar, editar e reativar são ações exclusivas de nutricionista ou "
             "administrador; o paciente pode listar e desativar."),
            ("Limite e conflito", "Máximo de dez alertas ativos por paciente, e dois alertas ativos não "
             "podem ocupar o mesmo horário."),
            ("Registro de disparos", "Toda tentativa de notificação é registrada com data, status, "
             "número de tentativas e causa da falha. Um desfecho encerra o disparo pendente daquele "
             "ciclo em vez de abrir outro registro."),
            ("Expiração automática", "Ao abrir sessão no aplicativo, disparos pendentes há mais de duas "
             "horas são marcados como falhos, com a causa preenchida. Impede o acúmulo de pendências sem "
             "resolução e mantém o contador do painel coerente."),
            ("Notificação local", "O envio é responsabilidade do aplicativo, que agenda as notificações "
             "no próprio dispositivo. A API guarda a configuração e recebe o desfecho, sem depender de "
             "serviço externo de push."),
        ],
    },
    {
        "titulo": "Painel do nutricionista",
        "intro": "A visão consolidada de todos os pacientes acompanhados, com as pendências ordenadas "
                 "por criticidade.",
        "requisitos": ["RF08", "RF08.3"],
        "regras": ["RN01 do UC011", "RN03 do UC011"],
        "casos": ["UC011"],
        "telas": [
            ("03-dashboard", "Painel com indicadores consolidados, pendências por criticidade e a "
                             "situação de cada paciente."),
        ],
        "funcoes": [
            ("Indicadores consolidados", "Total de pacientes, quantos registraram glicemia hoje, quantos "
             "têm pendência, planos ativos, média glicêmica geral e média do percentual no alvo."),
            ("Pendências por criticidade", "Hipoglicemia recente vem antes de hiperglicemia, porque o "
             "risco agudo é maior. Em seguida vêm ausência de registro por mais de três dias, ausência de "
             "plano ativo, alertas aguardando resposta e faixa glicêmica ainda não personalizada."),
            ("Distribuições", "Controle glicêmico por faixa de aderência e classificação de índice de "
             "massa corporal entre os pacientes acompanhados."),
            ("Isolamento por responsável", "O nutricionista vê apenas seus pacientes. O administrador vê "
             "todos e pode filtrar por profissional."),
            ("Origem exclusiva no modelo de leitura", "Todos os indicadores clínicos vêm da tabela de "
             "resumo, alimentada pelos serviços de escrita. Nenhuma agregação é calculada sobre as "
             "tabelas transacionais, como determina a modelagem."),
        ],
    },
    {
        "titulo": "Repositório educativo",
        "intro": "Receitas e conteúdos publicados pelos profissionais e acessíveis ao paciente.",
        "requisitos": ["RF09", "RF09.1", "RF09.2", "RF09.3"],
        "regras": ["RN28", "RN29", "RN30"],
        "casos": [],
        "telas": [
            ("08-repositorio", "Receitas publicadas, com valores nutricionais calculados a partir dos "
                               "ingredientes."),
        ],
        "funcoes": [
            ("Receitas com valor nutricional calculado", "Os ingredientes são vinculados a alimentos "
             "cadastrados, e as calorias e macronutrientes são somados a partir da base — totais e por "
             "porção. Quando algum ingrediente não tem valor publicado, o sistema avisa que o total está "
             "subestimado em vez de apresentar um número silenciosamente incompleto."),
            ("Conteúdos educativos", "Artigos, dicas e vídeos, com texto formatado."),
            ("Busca pelo paciente", "Por nome da receita ou por ingrediente, ignorando acentos."),
            ("Publicação restrita", "Criar e editar são ações de nutricionista ou administrador. O "
             "paciente tem permissão exclusiva de leitura."),
            ("Despublicação reversível", "Retirar um item do ar é competência exclusiva do "
             "administrador e é feito por inativação lógica: o conteúdo some para o paciente, continua "
             "visível para quem publica e pode ser republicado."),
        ],
    },
    {
        "titulo": "Relatórios clínicos",
        "intro": "Documentos em PDF para o prontuário ou para entregar ao paciente.",
        "requisitos": ["RF08"],
        "regras": ["RN20", "RN33"],
        "casos": ["UC011"],
        "telas": [],
        "funcoes": [
            ("Três relatórios", "Glicêmico, antropométrico e completo. O completo reúne os registros do "
             "período, a evolução das medidas e a prescrição vigente."),
            ("Identificação e período", "Cabeçalho com o paciente, o nutricionista responsável e o "
             "intervalo analisado, repetido em todas as páginas."),
            ("Faixa alvo explícita", "O documento impresso indica quando os limites em uso são o padrão "
             "clínico e não os do paciente, para não sugerir uma personalização que não houve."),
            ("Sinalização clínica", "Cada medição é marcada como normal, fora do alvo, hipoglicemia ou "
             "hiperglicemia, preservando a distinção entre limiar absoluto e faixa individual. O "
             "percentual no alvo é destacado quando fica abaixo de 70%."),
            ("Ressalva de uso", "O rodapé registra que o documento é apoio clínico e não substitui a "
             "avaliação do profissional de saúde."),
        ],
    },
    {
        "titulo": "Área do paciente",
        "intro": "O aplicativo mobile é etapa posterior. Enquanto isso, o paciente acessa pela web uma "
                 "área própria, com o que a API já lhe permite ler e registrar.",
        "requisitos": ["RF04.1", "RF05.1", "RF06.1", "RF09.3"],
        "regras": ["RN22", "RN23", "RN33", "RN35"],
        "casos": ["UC004", "UC012"],
        "telas": [
            ("15-minha-area", "Área do paciente: última medição comparada à faixa alvo, resumo da "
                              "semana, evolução, plano vigente, lembretes e receitas."),
            ("16-registro-emocional", "Registro emocional pelo paciente, com seleção de múltiplas "
                                      "emoções e escala de intensidade."),
        ],
        "funcoes": [
            ("Registro da própria glicemia", "Com contexto da medição e observação opcional. O valor é "
             "classificado imediatamente contra a faixa alvo do paciente."),
            ("Registro emocional", "Seleção de uma ou mais emoções e graduação da intensidade. A tela "
             "deixa explícito que o registro é voluntário."),
            ("Acompanhamento", "Última medição em destaque, resumo dos últimos sete dias, gráfico de "
             "evolução, plano alimentar vigente e lembretes configurados pelo nutricionista."),
            ("Repositório", "Acesso às receitas publicadas, com o valor calórico por porção."),
            ("Expiração de alertas", "A varredura de pendências acontece ao abrir a sessão, como "
             "descreve a regra de expiração automática."),
        ],
    },
]

LIMITES = {
    "intro": "O que não está implementado, e a razão de cada caso. A lista existe para que nenhuma "
             "ausência seja descoberta durante a avaliação.",
    "itens": [
        ("Aplicativo mobile em Flutter", "Etapa posterior do cronograma.",
         "Os endpoints que o aplicativo consome já existem e estão testados: registro de glicemia, "
         "antropometria e emoções pelo paciente, leitura do plano vigente, ciclo de disparo de alertas "
         "e a varredura de expiração."),
        ("Índice glicêmico dos alimentos", "A fonte oficial não publica o dado.",
         "A tabela TACO não traz índice glicêmico em nenhum dos 597 alimentos. Num sistema de apoio ao "
         "diabetes essa é a informação mais relevante clinicamente, e ela precisa vir de outra fonte ou "
         "do preenchimento caso a caso. O campo existe na modelagem e aceita valor no cadastro manual."),
        ("Seis alimentos sem macronutrientes", "Limitação da 4ª edição da TACO.",
         "O leite de vaca líquido, o sal grosso, o sal dietético, o coco verde e o iogurte de abacaxi "
         "trazem valor não determinado em energia, proteína, lipídeos e carboidrato. Um plano que os use "
         "não soma calorias por eles, e o sistema sinaliza a lacuna em vez de assumir zero."),
        ("Favoritos do paciente", "Sem lugar na modelagem de dados.",
         "O requisito prevê marcar conteúdos como favoritos, mas o diagrama entidade-relacionamento não "
         "define tabela para isso. Implementar exigiria alterar a modelagem documentada."),
        ("Imagens no conteúdo educativo", "Depende de decisão sobre armazenamento de arquivos.",
         "O corpo do conteúdo aceita texto formatado. Imagens exigiriam um serviço de armazenamento que "
         "não está previsto na arquitetura documentada."),
        ("Envio real de e-mail", "Depende de conta de envio.",
         "O envio por SMTP está implementado e é ativado ao configurar as credenciais. Enquanto isso, as "
         "mensagens — incluindo as senhas provisórias — são registradas no log da aplicação."),
        ("Login com Google ativo", "Depende de credencial no Google Cloud Console.",
         "O fluxo está implementado na API e na interface. Sem o identificador de cliente configurado, o "
         "botão não é exibido e o login por senha segue como caminho único."),
        ("Publicação em ambiente de produção", "Decisão de infraestrutura.",
         "O sistema roda em ambiente de desenvolvimento. A regra de criptografia em trânsito exige HTTPS, "
         "o que depende de definir onde a aplicação será hospedada."),
        ("Campos previstos nos casos de uso sem lugar no modelo de dados", "Divergência entre documentos.",
         "Os casos de uso mencionam CPF, data de nascimento e sexo do nutricionista, endereço do paciente "
         "e observações no registro antropométrico. O diagrama entidade-relacionamento não define colunas "
         "para esses dados, e o sistema seguiu a modelagem."),
    ],
}

VALIDACAO = {
    "intro": "A suíte automatizada cobre os cálculos que sustentam a prescrição e as regras cuja "
             "violação corromperia dado clínico.",
    "grupos": [
        ("Cálculos clínicos", "Índice de massa corporal e sua classificação, relação cintura-quadril, "
         "Harris-Benedict e Mifflin-St Jeor conferidas termo a termo, valor energético total, "
         "distribuição de macronutrientes pelos fatores de Atwater, classificação glicêmica e cálculo de idade."),
        ("Leitura da tabela nutricional", "Marcadores de traço e de valor não determinado, codificação "
         "Windows-1252, detecção de separador e reconhecimento dos cabeçalhos em suas várias grafias."),
        ("Validações de entrada", "Dígito verificador do CPF, política de senha, hash e senha provisória."),
        ("Regras de negócio, contra banco real", "Bloqueio progressivo em seus três níveis, senha "
         "provisória, unicidade de e-mail, pré-requisito do cálculo energético, soma dos macronutrientes, "
         "unicidade de plano ativo — incluindo a recusa do próprio banco a dois planos simultâneos — "
         "reclassificação do histórico glicêmico e isolamento entre profissionais."),
    ],
    "mutacao": "A suíte foi verificada por mutação: alterar a divisão do índice de massa corporal, um "
               "coeficiente de Harris-Benedict, o escalonamento do bloqueio, a reclassificação glicêmica, "
               "a ordem de gravação do plano ativo ou o tratamento do marcador de traço faz os testes "
               "falharem em todos os casos.",
}


RASTREABILIDADE = {
    "intro": "Onde cada requisito funcional e cada regra de negócio foi implementado. "
             "As três exceções estão detalhadas na seção de limites conhecidos.",
    "requisitos": [
        ("RF01", "Importação de dados nutricionais", "Banco de alimentos", "Completo"),
        ("RF01.1", "Cadastro manual de alimento", "Banco de alimentos", "Completo"),
        ("RF01.2", "Importação via arquivo CSV/TACO", "Banco de alimentos", "Completo"),
        ("RF02", "Cálculo da necessidade energética", "Acompanhamento clínico", "Completo"),
        ("RF02.1", "Seleção de fórmula de cálculo", "Acompanhamento clínico", "Completo"),
        ("RF02.2", "Exibição do resultado por paciente", "Acompanhamento clínico", "Completo"),
        ("RF03", "Cálculo de macronutrientes", "Plano alimentar", "Completo"),
        ("RF03.1", "Distribuição automática", "Plano alimentar", "Completo"),
        ("RF03.2", "Ajuste manual pelo nutricionista", "Plano alimentar", "Completo"),
        ("RF03.3", "Unicidade e exclusão lógica do plano", "Plano alimentar", "Completo"),
        ("RF04", "Registro antropométrico", "Acompanhamento clínico", "Completo"),
        ("RF04.1", "Peso, altura, IMC e circunferências", "Acompanhamento clínico", "Completo"),
        ("RF04.2", "Histórico de medidas", "Acompanhamento clínico", "Completo"),
        ("RF05", "Monitoramento glicêmico", "Acompanhamento clínico", "Completo"),
        ("RF05.1", "Registro por horário e contexto", "Acompanhamento clínico", "Completo"),
        ("RF05.2", "Histórico e evolução glicêmica", "Acompanhamento clínico", "Completo"),
        ("RF06", "Registro emocional", "Acompanhamento clínico e área do paciente", "Completo"),
        ("RF06.1", "Emoção associada à refeição", "Área do paciente", "Completo"),
        ("RF06.2", "Correlação emocional e glicemia", "Acompanhamento clínico", "Completo"),
        ("RF07", "Sistema de alertas e lembretes", "Alertas e lembretes", "Completo"),
        ("RF07.1", "Configuração pelo profissional", "Alertas e lembretes", "Completo"),
        ("RF07.2", "Envio de notificação", "Alertas e lembretes", "API pronta; envio no aplicativo"),
        ("RF08", "Visualização gráfica de dados", "Acompanhamento clínico e painel", "Completo"),
        ("RF08.1", "Gráfico de evolução glicêmica", "Acompanhamento clínico", "Completo"),
        ("RF08.2", "Gráfico de peso e IMC", "Acompanhamento clínico", "Completo"),
        ("RF08.3", "Painel do nutricionista", "Painel do nutricionista", "Completo"),
        ("RF09", "Repositório educativo", "Repositório educativo", "Completo"),
        ("RF09.1", "Cadastro de receitas", "Repositório educativo", "Completo"),
        ("RF09.2", "Publicação de conteúdo educativo", "Repositório educativo", "Texto; imagens pendentes"),
        ("RF09.3", "Acesso pelo paciente", "Área do paciente", "Busca sim; favoritos pendentes"),
        ("RF10", "Gestão de usuários", "Gestão de usuários", "Completo"),
        ("RF10.1", "Login e autenticação", "Autenticação e controle de acesso", "Completo"),
        ("RF10.2", "Cadastro de paciente", "Gestão de usuários", "Completo"),
        ("RF10.3", "Cadastro de nutricionista", "Gestão de usuários", "Completo"),
        ("RF10.4", "Controle de acesso por perfil", "Autenticação e controle de acesso", "Completo"),
        ("RF10.5", "Gestão de usuários pelo administrador", "Gestão de usuários", "Completo"),
    ],
    "regras": [
        ("RN01", "Autenticação obrigatória e login federado", "Autenticação"),
        ("RN02", "Bloqueio progressivo por tentativas inválidas", "Autenticação"),
        ("RN03", "Troca obrigatória da senha provisória", "Autenticação"),
        ("RN04", "Controle de acesso por perfil", "Autenticação"),
        ("RN05", "Integridade do histórico na desativação", "Gestão de usuários"),
        ("RN06", "Unicidade de e-mail", "Gestão de usuários"),
        ("RN07", "Vínculo obrigatório com nutricionista", "Gestão de usuários"),
        ("RN08", "Cadastro de paciente restrito", "Gestão de usuários"),
        ("RN09", "Cadastro de nutricionista restrito", "Gestão de usuários"),
        ("RN10", "Formato obrigatório da importação", "Banco de alimentos"),
        ("RN11", "Prevenção de duplicatas", "Banco de alimentos"),
        ("RN12", "Inativação lógica de alimentos", "Banco de alimentos"),
        ("RN13", "Cálculo energético precede o plano", "Plano alimentar"),
        ("RN14", "Fórmulas validadas", "Acompanhamento clínico"),
        ("RN15", "Macronutrientes somam 100%", "Plano alimentar"),
        ("RN16", "Plano ativo único por paciente", "Plano alimentar"),
        ("RN17", "Preservação do histórico de planos", "Plano alimentar"),
        ("RN18", "Campos obrigatórios da glicemia", "Acompanhamento clínico"),
        ("RN19", "Classificação automática fora do alvo", "Acompanhamento clínico"),
        ("RN20", "Limites glicêmicos pelo nutricionista", "Acompanhamento clínico"),
        ("RN21", "Cálculo automático do IMC", "Acompanhamento clínico"),
        ("RN22", "Voluntariedade do registro emocional", "Área do paciente"),
        ("RN23", "Configuração de alertas restrita", "Alertas e lembretes"),
        ("RN24", "Alertas padrão no cadastro", "Gestão de usuários"),
        ("RN25", "Mensagem definida no cadastro", "Alertas e lembretes"),
        ("RN26", "Persistência até registro ou desativação", "Alertas e lembretes"),
        ("RN27", "Registro de tentativas de envio", "Alertas e lembretes"),
        ("RN28", "Publicação restrita a profissionais", "Repositório educativo"),
        ("RN29", "Despublicação restrita ao administrador", "Repositório educativo"),
        ("RN30", "Receita com ingredientes vinculados", "Repositório educativo"),
        ("RN31", "Criptografia em trânsito", "Infraestrutura; exige publicação com HTTPS"),
        ("RN32", "Armazenamento seguro de credenciais", "Autenticação"),
        ("RN33", "Proteção de dados de saúde", "Todos os módulos clínicos"),
        ("RN34", "Disponibilidade mínima do sistema", "Requisito operacional, não de código"),
        ("RN35", "Expiração automática de alertas", "Alertas e lembretes"),
    ],
}
