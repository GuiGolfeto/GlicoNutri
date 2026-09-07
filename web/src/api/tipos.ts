export interface LoginResponse {
  token: string
  expiraEm: string
  usuarioId: number
  nome: string
  email: string
  perfil: Perfil
  senhaProvisoria: boolean
}

export interface LoginFalha {
  mensagem: string
  bloqueado: boolean
  bloqueadoAte: string | null
  segundosRestantes: number | null
}

export type Perfil = 'PACIENTE' | 'NUTRICIONISTA' | 'ADMINISTRADOR'

export interface Referencia {
  id: number
  codigo: string
  descricao: string
}

export interface Nutricionista {
  id: number
  nome: string
  email: string
  crn: string
  especialidade: string | null
  telefone: string | null
  ativo: boolean
  senhaProvisoria: boolean
  dataCadastro: string
  totalPacientes: number
}

export interface CriarNutricionista {
  nome: string
  email: string
  crn: string
  especialidade?: string | null
  telefone?: string | null
}

export interface Paciente {
  id: number
  nome: string
  email: string
  cpf: string
  dataNascimento: string
  idade: number
  sexo: string
  tipoDiabetes: string
  telefone: string | null
  medicacaoEmUso: string | null
  observacoesClinicas: string | null
  glicemiaMinAlvo: number | null
  glicemiaMaxAlvo: number | null
  /** RN20 — falso enquanto o nutricionista não personalizar a faixa alvo. */
  faixaGlicemicaPersonalizada: boolean
  nutricionistaId: number | null
  nutricionistaNome: string | null
  ativo: boolean
  dataCadastro: string
}

export interface CriarPaciente {
  nome: string
  email: string
  cpf: string
  dataNascimento: string
  sexoId: number | null
  tipoDiabetesId: number | null
  telefone?: string | null
  medicacaoEmUso?: string | null
  observacoesClinicas?: string | null
  nutricionistaId?: number | null
}

// ── UC008 — Dados antropométricos ───────────────────────────────────────────

export interface ComparativoMedida {
  anterior: number | null
  atual: number
  delta: number | null
  deltaPercentual: number | null
}

export interface RegistroAntropometrico {
  id: number
  pacienteId: number
  peso: number
  /** Em centímetros, como o UC008 coleta. */
  altura: number
  circunferenciaAbdominal: number | null
  circunferenciaCintura: number | null
  circunferenciaQuadril: number | null
  circunferenciaBraco: number | null
  rcq: number | null
  imc: number | null
  classificacaoImc: string | null
  dataHora: string
  comparativoPeso: ComparativoMedida | null
  comparativoImc: ComparativoMedida | null
}

export interface CriarRegistroAntropometrico {
  peso: number | null
  altura: number | null
  circunferenciaAbdominal?: number | null
  circunferenciaCintura?: number | null
  circunferenciaQuadril?: number | null
  circunferenciaBraco?: number | null
  dataHora?: string | null
}

// ── UC006 — Necessidade energética ──────────────────────────────────────────

export interface NecessidadeEnergetica {
  id: number | null
  pacienteId: number
  formula: string
  nivelAtividade: string
  fatorAtividade: number
  /** Nulo no histórico: o DER persiste apenas o VET. */
  tmb: number | null
  valorKcal: number
  objetivo: string | null
  dataCalculo: string
  pesoUtilizado: number | null
  alturaUtilizada: number | null
  idadeUtilizada: number | null
  sexoUtilizado: string | null
  dataMedicaoUtilizada: string | null
  vetAnterior: number | null
  variacaoPercentual: number | null
}

export interface CalcularVet {
  formulaId: number | null
  nivelAtividadeId: number | null
  objetivo?: string | null
}

// ── UC009 — Alimentos ───────────────────────────────────────────────────────

export interface Alimento {
  id: number
  nome: string
  grupoAlimentar: string | null
  caloriasPor100g: number | null
  carboidratosPor100g: number | null
  proteinasPor100g: number | null
  lipidiosPor100g: number | null
  fibrasPor100g: number | null
  indiceGlicemico: number | null
  fonte: string
  ativo: boolean
}

export interface CriarAlimento {
  nome: string
  grupoAlimentar?: string | null
  caloriasPor100g: number | null
  carboidratosPor100g: number | null
  proteinasPor100g: number | null
  lipidiosPor100g: number | null
  fibrasPor100g: number | null
  indiceGlicemico: number | null
}

export interface LinhaRejeitada { linha: number; conteudo: string; motivo: string }

export interface ResultadoImportacao {
  linhasLidas: number
  importados: number
  ignoradosPorDuplicidade: number
  rejeitados: number
  erros: LinhaRejeitada[]
}

// ── UC007 — Plano alimentar ─────────────────────────────────────────────────

export interface Distribuicao {
  carboidratosPercentual: number
  proteinasPercentual: number
  lipidiosPercentual: number
  carboidratosGramas: number
  proteinasGramas: number
  lipidiosGramas: number
  totalCaloriasPrescritas: number
}

export interface ItemPlano {
  id: number
  alimentoId: number
  alimentoNome: string
  dia: number
  horario: string | null
  refeicao: string | null
  quantidade: number
  unidade: string | null
  calorias: number | null
  carboidratos: number | null
  proteinas: number | null
  lipidios: number | null
}

export interface TotaisRefeicao {
  refeicao: string
  calorias: number
  carboidratos: number
  proteinas: number
  lipidios: number
}

export interface PlanoAlimentar {
  id: number
  pacienteId: number
  pacienteNome: string
  nutricionistaId: number
  objetivo: string | null
  dataInicio: string
  dataFim: string | null
  observacoes: string | null
  totalCaloriasPrescritas: number | null
  ativo: boolean
  distribuicao: Distribuicao | null
  itens: ItemPlano[]
  totaisPorRefeicao: TotaisRefeicao[]
  caloriasMontadas: number
  desvioPercentualDoVet: number | null
  dentroDaMargem: boolean
}

export interface ItemPlanoNovo {
  alimentoId: number
  dia: number
  horario?: string | null
  refeicao?: string | null
  quantidade: number
  unidade?: string | null
}

// ── UC004 / UC005 — Glicemia ────────────────────────────────────────────────

export interface RegistroGlicemia {
  id: number
  pacienteId: number
  valor: number
  contexto: string
  contextoId: number
  dataHora: string
  observacao: string | null
  foraDoAlvo: boolean
  classificacao: 'NORMAL' | 'HIPOGLICEMIA' | 'HIPERGLICEMIA'
  minAlvoAplicado: number
  maxAlvoAplicado: number
  faixaPersonalizada: boolean
}

export interface ResumoGlicemico {
  totalRegistros: number
  media: number | null
  minimo: number | null
  maximo: number | null
  percentualNoAlvo: number | null
  registrosForaDoAlvo: number
  hipoglicemias: number
  hiperglicemias: number
  minAlvoAplicado: number
  maxAlvoAplicado: number
  faixaPersonalizada: boolean
}

export interface HistoricoGlicemico {
  dias: number
  inicioPeriodo: string
  resumo: ResumoGlicemico
  registros: RegistroGlicemia[]
}

export interface PontoSerieGlicemia {
  dataHora: string
  valor: number
  contexto: string
  foraDoAlvo: boolean
}

export interface SerieGlicemica {
  dias: number
  minAlvo: number
  maxAlvo: number
  pontos: PontoSerieGlicemia[]
}

export interface PontoSerieAntropometrica {
  dataHora: string
  peso: number
  imc: number | null
  classificacaoImc: string | null
}

export interface SerieAntropometrica { dias: number; pontos: PontoSerieAntropometrica[] }

// ── UC012 — Emoções ─────────────────────────────────────────────────────────

export interface RegistroEmocional {
  id: number
  pacienteId: number
  estadoEmocionalId: number
  estadoEmocional: string
  intensidade: number
  dataHora: string
  descricao: string | null
}

export interface DiarioEmocional {
  dias: number
  totalRegistros: number
  registros: RegistroEmocional[]
}

export interface CorrelacaoEmocao {
  estadoEmocional: string
  registrosEmocionais: number
  glicemiasAssociadas: number
  mediaGlicemia: number | null
  minimoGlicemia: number | null
  maximoGlicemia: number | null
  percentualForaDoAlvo: number | null
  intensidadeMedia: number
}

export interface CorrelacaoEmocional {
  dias: number
  janelaHoras: number
  registrosEmocionaisNoPeriodo: number
  glicemiasNoPeriodo: number
  mediaGlicemiaGeral: number | null
  porEmocao: CorrelacaoEmocao[]
}

// ── UC010 — Alertas ─────────────────────────────────────────────────────────

export interface Alerta {
  id: number
  pacienteId: number
  tipoId: number
  tipo: string
  mensagem: string | null
  horario1: string | null
  horario2: string | null
  diasSemana: string | null
  ativo: boolean
  disparosPendentes: number
}

export interface CriarAlerta {
  tipoId: number | null
  mensagem?: string | null
  horario1: string
  horario2?: string | null
  diasSemana?: string | null
}

export interface HistoricoAlerta {
  id: number
  alertaId: number
  tipoAlerta: string
  dataHoraDisparo: string
  dataHoraAtendimento: string | null
  status: string
  tentativas: number
  mensagemErro: string | null
}

// ── UC011 — Dashboard ───────────────────────────────────────────────────────

export interface IndicadoresDashboard {
  totalPacientes: number
  pacientesComRegistroHoje: number
  pacientesComAlerta: number
  planosAtivos: number
  mediaGlicemicaGeral: number | null
  mediaPercentualNoAlvo: number | null
}

export type Severidade = 'CRITICO' | 'ATENCAO' | 'INFORMATIVO'

export interface AlertaDashboard {
  pacienteId: number
  pacienteNome: string
  tipo: string
  severidade: Severidade
  descricao: string
  ocorrencia: string | null
}

export interface PacienteDashboard {
  pacienteId: number
  nome: string
  ultimaGlicemia: number | null
  ultimaGlicemiaContexto: string | null
  ultimaGlicemiaData: string | null
  mediaGlicemia7Dias: number | null
  percentualNoAlvo7Dias: number | null
  ultimoImc: number | null
  classificacaoImc: string | null
  ultimaDataAntropometria: string | null
  planoAtivo: boolean
  alertasPendentes: number
  diasSemRegistroGlicemia: number | null
  severidade: Severidade | null
}

export interface FatiaDistribuicao { rotulo: string; quantidade: number }

export interface Dashboard {
  atualizadoEm: string
  indicadores: IndicadoresDashboard
  alertas: AlertaDashboard[]
  pacientes: PacienteDashboard[]
  controleGlicemico: FatiaDistribuicao[]
  classificacaoImc: FatiaDistribuicao[]
}

// ── RF09 — Repositório educativo ────────────────────────────────────────────

export interface ConteudoEducativo {
  id: number
  titulo: string
  tipoId: number
  tipo: string
  corpo: string | null
  autorId: number
  autorNome: string
  dataPublicacao: string | null
  ativo: boolean
}

export interface IngredienteReceita {
  id: number
  alimentoId: number
  alimentoNome: string
  quantidade: number
  unidade: string | null
  calorias: number | null
  carboidratos: number | null
  proteinas: number | null
  lipidios: number | null
}

export interface InformacaoNutricional {
  calorias: number
  carboidratos: number
  proteinas: number
  lipidios: number
  fibras: number
  caloriasPorPorcao: number | null
  carboidratosPorPorcao: number | null
  completa: boolean
}

export interface Receita {
  id: number
  nome: string
  descricao: string | null
  tempoPreparo: number | null
  porcoes: number | null
  instrucoes: string | null
  nutricionistaId: number
  nutricionistaNome: string
  dataPublicacao: string | null
  ativo: boolean
  ingredientes: IngredienteReceita[]
  nutricional: InformacaoNutricional
}
