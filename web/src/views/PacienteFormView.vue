<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { api, mensagemDeErro } from '../api/client'
import type { CriarPaciente, Nutricionista, Paciente, Referencia } from '../api/tipos'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()

const sexos = ref<Referencia[]>([])
const tiposDiabetes = ref<Referencia[]>([])
const nutricionistas = ref<Nutricionista[]>([])

const erro = ref('')
const enviando = ref(false)

const form = reactive<CriarPaciente>({
  nome: '', email: '', cpf: '', dataNascimento: '',
  sexoId: null, tipoDiabetesId: null,
  telefone: '', medicacaoEmUso: '', observacoesClinicas: '', nutricionistaId: null,
})

onMounted(async () => {
  const [s, t] = await Promise.all([
    api.get<Referencia[]>('/referencias/sexos-biologicos'),
    api.get<Referencia[]>('/referencias/tipos-diabetes'),
  ])
  sexos.value = s.data
  tiposDiabetes.value = t.data

  // RN07 — o Administrador não é nutricionista, então precisa escolher quem
  // será o responsável. O Nutricionista se vincula sozinho (UC002 RN03).
  if (auth.ehAdministrador) {
    const { data } = await api.get<Nutricionista[]>('/nutricionistas')
    nutricionistas.value = data
  }
})

/** UC002 A1 — o mesmo dígito verificador que a API valida, para avisar antes de enviar. */
function cpfValido(entrada: string): boolean {
  const d = entrada.replace(/\D/g, '')
  if (d.length !== 11 || /^(\d)\1{10}$/.test(d)) return false

  const digito = (ate: number) => {
    let soma = 0
    for (let i = 0; i < ate; i++) soma += Number(d[i]) * (ate + 1 - i)
    const resto = (soma * 10) % 11
    return resto === 10 ? 0 : resto
  }
  return digito(9) === Number(d[9]) && digito(10) === Number(d[10])
}

const cpfInvalido = computed(
  () => form.cpf.replace(/\D/g, '').length === 11 && !cpfValido(form.cpf),
)

const podeEnviar = computed(
  () => !!form.nome && !!form.email && cpfValido(form.cpf) && !!form.dataNascimento
    && !!form.sexoId && !!form.tipoDiabetesId
    && (!auth.ehAdministrador || !!form.nutricionistaId),
)

async function salvar() {
  erro.value = ''
  enviando.value = true
  try {
    const { data } = await api.post<Paciente>('/pacientes', form)
    router.push({ name: 'paciente-detalhe', params: { id: data.id } })
  } catch (e) {
    erro.value = mensagemDeErro(e)
  } finally {
    enviando.value = false
  }
}
</script>

<template>
  <h1>Novo Paciente</h1>
  <p class="sub">O paciente receberá por e-mail uma senha provisória para o primeiro acesso.</p>

  <form class="card formulario" @submit.prevent="salvar">
    <div v-if="erro" class="aviso aviso-erro">{{ erro }}</div>

    <h3>Dados pessoais</h3>
    <div class="campo">
      <label for="nome">Nome completo *</label>
      <input id="nome" v-model="form.nome" required />
    </div>

    <div class="grade-2">
      <div class="campo">
        <label for="cpf">CPF *</label>
        <input id="cpf" v-model="form.cpf" placeholder="000.000.000-00" required />
        <span v-if="cpfInvalido" class="erro-campo">CPF inválido — verifique os dígitos.</span>
      </div>
      <div class="campo">
        <label for="nascimento">Data de nascimento *</label>
        <input id="nascimento" v-model="form.dataNascimento" type="date" required />
      </div>
    </div>

    <div class="grade-2">
      <div class="campo">
        <label for="sexo">Sexo *</label>
        <select id="sexo" v-model="form.sexoId" required>
          <option :value="null" disabled>Selecione…</option>
          <option v-for="s in sexos" :key="s.id" :value="s.id">{{ s.descricao }}</option>
        </select>
      </div>
      <div class="campo">
        <label for="telefone">Telefone</label>
        <input id="telefone" v-model="form.telefone" />
      </div>
    </div>

    <div class="campo">
      <label for="email">E-mail *</label>
      <input id="email" v-model="form.email" type="email" required />
      <span class="ajuda">Será o login do paciente no aplicativo.</span>
    </div>

    <h3>Dados clínicos</h3>
    <div class="campo">
      <label for="tipo">Tipo de diabetes *</label>
      <select id="tipo" v-model="form.tipoDiabetesId" required>
        <option :value="null" disabled>Selecione…</option>
        <option v-for="t in tiposDiabetes" :key="t.id" :value="t.id">{{ t.descricao }}</option>
      </select>
    </div>

    <div class="campo">
      <label for="medicacao">Medicação em uso</label>
      <textarea id="medicacao" v-model="form.medicacaoEmUso" rows="2"></textarea>
    </div>

    <div class="campo">
      <label for="observacoes">Observações clínicas</label>
      <textarea id="observacoes" v-model="form.observacoesClinicas" rows="3"></textarea>
    </div>

    <template v-if="auth.ehAdministrador">
      <h3>Acompanhamento</h3>
      <div class="campo">
        <label for="responsavel">Nutricionista responsável *</label>
        <select id="responsavel" v-model="form.nutricionistaId" required>
          <option :value="null" disabled>Selecione…</option>
          <option v-for="n in nutricionistas" :key="n.id" :value="n.id">
            {{ n.nome }} — {{ n.crn }}
          </option>
        </select>
        <span class="ajuda">Todo paciente precisa de um nutricionista responsável.</span>
      </div>
    </template>
    <p v-else class="ajuda vinculo">
      O paciente será vinculado automaticamente a você como nutricionista responsável.
    </p>

    <div class="acoes">
      <button class="btn btn-primario" type="submit" :disabled="enviando || !podeEnviar">
        {{ enviando ? 'Salvando…' : 'Cadastrar paciente' }}
      </button>
      <RouterLink class="btn btn-secundario" :to="{ name: 'pacientes' }">Cancelar</RouterLink>
    </div>
  </form>
</template>

<style scoped>
.sub { margin: 4px 0 var(--lg); color: var(--text-secondary); font-size: 15px; }
.formulario { max-width: 720px; }
.formulario h3 { font-size: 15px; color: var(--text-secondary); margin: var(--lg) 0 var(--md); }
.formulario h3:first-of-type { margin-top: 0; }
.vinculo { display: block; margin: var(--md) 0 0; }
.acoes { display: flex; gap: var(--sm); margin-top: var(--lg); padding-top: var(--lg); border-top: 1px solid var(--border); }
</style>
