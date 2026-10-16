<script setup>
import { inject, onMounted, ref } from 'vue'
import { admin } from '../../api/admin'
import Mensagem from '../../components/Mensagem.vue'
import Modal from '../../components/Modal.vue'
import { DIAS_DA_SEMANA } from '../../dominio/formatos'

const atualizarAlertas = inject('atualizarAlertas')
const profissionais = ref([])
const servicos = ref([])
const editando = ref(null)
const erro = ref('')
const salvando = ref(false)

async function carregar() {
  ;[profissionais.value, servicos.value] = await Promise.all([admin.profissionais(), admin.servicos()])
}
onMounted(carregar)

const nomeDoServico = (id) => servicos.value.find((s) => s.id === id)?.nome ?? '?'

function resumoDosHorarios(horarios) {
  return DIAS_DA_SEMANA.filter((d) => horarios.some((h) => h.dia === d.valor))
    .map((d) => d.curto)
    .join(', ') || 'Sem horários'
}

function novo() {
  erro.value = ''
  editando.value = { nome: '', ativa: true, servicoIds: [], horarios: [] }
}

function editar(p) {
  erro.value = ''
  editando.value = { ...p, servicoIds: [...p.servicoIds], horarios: p.horarios.map((h) => ({ ...h })) }
}

const horariosDoDia = (dia) => editando.value.horarios.filter((h) => h.dia === dia)

function adicionarIntervalo(dia) {
  const existentes = horariosDoDia(dia)
  const novo = existentes.length ? { dia, inicio: '13:00', fim: '18:00' } : { dia, inicio: '09:00', fim: '12:00' }
  editando.value.horarios.push(novo)
}

function removerIntervalo(intervalo) {
  editando.value.horarios = editando.value.horarios.filter((h) => h !== intervalo)
}

function copiarSegundaParaSemana() {
  const segunda = horariosDoDia('Monday')
  const outros = editando.value.horarios.filter((h) => !['Tuesday', 'Wednesday', 'Thursday', 'Friday'].includes(h.dia))
  const copias = ['Tuesday', 'Wednesday', 'Thursday', 'Friday'].flatMap((dia) => segunda.map((h) => ({ ...h, dia })))
  editando.value.horarios = [...outros, ...copias]
}

async function salvar() {
  salvando.value = true
  erro.value = ''
  const { id, nome, ativa, servicoIds, horarios } = editando.value
  try {
    if (id) await admin.atualizarProfissional(id, { nome, ativa, servicoIds, horarios })
    else await admin.criarProfissional({ nome, ativa, servicoIds, horarios })
    editando.value = null
    await carregar()
    atualizarAlertas()
  } catch (e) {
    erro.value = e.message
  } finally {
    salvando.value = false
  }
}
</script>

<template>
  <div class="flex flex-wrap items-end justify-between gap-4">
    <div>
      <h1 class="text-4xl font-semibold">Profissionais</h1>
      <p class="mt-1 text-stone-500">Quem atende, quais procedimentos faz e em quais horários.</p>
    </div>
    <button class="btn-primario" @click="novo">Nova profissional</button>
  </div>

  <ul class="mt-8 grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
    <li v-for="p in profissionais" :key="p.id" class="cartao cursor-pointer transition hover:shadow-md"
      :class="{ 'opacity-50': !p.ativa }" @click="editar(p)">
      <p class="font-titulo text-2xl font-semibold">{{ p.nome }}</p>
      <p class="mt-1 text-sm text-stone-500">{{ resumoDosHorarios(p.horarios) }}</p>
      <div class="mt-3 flex flex-wrap gap-1.5">
        <span v-for="id in p.servicoIds" :key="id" class="rounded-full bg-marca-50 px-2.5 py-0.5 text-xs text-marca-700">
          {{ nomeDoServico(id) }}
        </span>
      </div>
      <p v-if="!p.ativa" class="mt-2 text-xs font-medium text-stone-500">Inativa</p>
    </li>
  </ul>

  <Modal :titulo="editando?.id ? 'Editar profissional' : 'Nova profissional'" :aberto="!!editando" largo @fechar="editando = null">
    <form v-if="editando" class="space-y-6" @submit.prevent="salvar">
      <div class="grid gap-4 sm:grid-cols-[1fr_auto]">
        <div>
          <label class="rotulo">Nome</label>
          <input v-model="editando.nome" class="campo" required maxlength="100" />
        </div>
        <label class="flex items-center gap-3 self-end pb-2.5 text-sm">
          <input v-model="editando.ativa" type="checkbox" class="h-4 w-4 accent-marca-500" /> Ativa
        </label>
      </div>

      <fieldset>
        <legend class="rotulo">Procedimentos que realiza</legend>
        <div class="grid gap-2 sm:grid-cols-2">
          <label v-for="s in servicos" :key="s.id" class="flex items-center gap-3 rounded-xl border border-stone-200 px-3 py-2 text-sm">
            <input v-model="editando.servicoIds" type="checkbox" :value="s.id" class="h-4 w-4 accent-marca-500" />
            {{ s.nome }}
          </label>
        </div>
      </fieldset>

      <fieldset>
        <div class="mb-2 flex flex-wrap items-center justify-between gap-2">
          <legend class="rotulo mb-0">Horários de trabalho</legend>
          <button type="button" class="text-xs text-marca-600 hover:underline" @click="copiarSegundaParaSemana">
            Copiar segunda para terça a sexta
          </button>
        </div>
        <div class="divide-y divide-stone-100 rounded-xl border border-stone-200">
          <div v-for="dia in DIAS_DA_SEMANA" :key="dia.valor" class="flex flex-wrap items-center gap-3 px-3 py-2.5">
            <span class="w-20 text-sm font-medium">{{ dia.nome }}</span>
            <div class="flex flex-1 flex-wrap items-center gap-2">
              <span v-if="!horariosDoDia(dia.valor).length" class="text-sm text-stone-400">Folga</span>
              <span v-for="(h, i) in horariosDoDia(dia.valor)" :key="i" class="flex items-center gap-1">
                <input v-model="h.inicio" type="time" step="900" class="campo w-28 px-2 py-1.5" required />
                <span class="text-stone-400">–</span>
                <input v-model="h.fim" type="time" step="900" class="campo w-28 px-2 py-1.5" required />
                <button type="button" class="px-1.5 text-stone-400 hover:text-red-600" aria-label="Remover intervalo"
                  @click="removerIntervalo(h)">×</button>
              </span>
            </div>
            <button type="button" class="text-xs text-marca-600 hover:underline" @click="adicionarIntervalo(dia.valor)">+ intervalo</button>
          </div>
        </div>
        <p class="mt-1 text-xs text-stone-500">Use dois intervalos no mesmo dia para a pausa do almoço. Horários de 15 em 15 minutos.</p>
      </fieldset>

      <Mensagem v-if="erro">{{ erro }}</Mensagem>
      <button class="btn-primario w-full sm:w-auto" :disabled="salvando">{{ salvando ? 'Salvando…' : 'Salvar' }}</button>
    </form>
  </Modal>
</template>
