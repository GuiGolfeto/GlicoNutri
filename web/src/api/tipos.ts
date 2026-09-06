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
