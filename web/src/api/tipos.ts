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
