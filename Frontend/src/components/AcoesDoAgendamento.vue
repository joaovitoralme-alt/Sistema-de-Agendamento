<script setup>
// Botões de ação de um agendamento (confirmar, cancelar, remarcar, trocar profissional).
// Usado na Agenda e nos Alertas.
import { ref } from 'vue'
import { admin } from '../api/admin'
import { formatarHora } from '../dominio/formatos'
import Mensagem from './Mensagem.vue'
import Modal from './Modal.vue'

const props = defineProps({ agendamento: Object })
const emit = defineEmits(['alterado'])

const modal = ref(null) // 'remarcar' | 'trocar'
const erro = ref('')
const ocupado = ref(false)
const novaData = ref('')
const novaHora = ref('')
const opcoes = ref([])
const novaProfissional = ref('')

async function executar(acao) {
  ocupado.value = true
  erro.value = ''
  try {
    await acao()
    modal.value = null
    emit('alterado')
  } catch (e) {
    erro.value = e.message
  } finally {
    ocupado.value = false
  }
}

const confirmar = () => executar(() => admin.confirmar(props.agendamento.id))

function cancelar() {
  if (confirm(`Cancelar o agendamento de ${props.agendamento.clienteNome}?`))
    executar(() => admin.cancelar(props.agendamento.id))
}

function abrirRemarcar() {
  erro.value = ''
  novaData.value = props.agendamento.inicio.slice(0, 10)
  novaHora.value = formatarHora(props.agendamento.inicio)
  modal.value = 'remarcar'
}

const remarcar = () =>
  executar(() => admin.remarcar(props.agendamento.id, `${novaData.value}T${novaHora.value}:00`))

async function abrirTrocar() {
  erro.value = ''
  novaProfissional.value = ''
  modal.value = 'trocar'
  opcoes.value = await admin.profissionaisParaTroca(props.agendamento.id).catch(() => [])
}

const trocar = () => executar(() => admin.trocarProfissional(props.agendamento.id, novaProfissional.value))
</script>

<template>
  <div v-if="agendamento.status !== 'Cancelado'" class="flex flex-wrap gap-2">
    <button v-if="agendamento.status === 'Pendente'" class="btn-primario btn-pequeno" :disabled="ocupado" @click="confirmar">Confirmar</button>
    <button class="btn-secundario btn-pequeno" @click="abrirRemarcar">Remarcar</button>
    <button class="btn-secundario btn-pequeno" @click="abrirTrocar">Trocar profissional</button>
    <button class="btn-perigo btn-pequeno" :disabled="ocupado" @click="cancelar">Cancelar</button>
  </div>
  <Mensagem v-if="erro && !modal" class="mt-2">{{ erro }}</Mensagem>

  <Modal titulo="Remarcar" :aberto="modal === 'remarcar'" @fechar="modal = null">
    <form class="space-y-4" @submit.prevent="remarcar">
      <p class="text-sm text-stone-600">{{ agendamento.clienteNome }} · {{ agendamento.servicoNome }} com {{ agendamento.profissionalNome }}</p>
      <div class="grid grid-cols-2 gap-3">
        <div>
          <label class="rotulo">Data</label>
          <input v-model="novaData" type="date" class="campo" required />
        </div>
        <div>
          <label class="rotulo">Horário</label>
          <input v-model="novaHora" type="time" step="900" class="campo" required />
        </div>
      </div>
      <Mensagem v-if="erro">{{ erro }}</Mensagem>
      <button class="btn-primario w-full" :disabled="ocupado">Salvar novo horário</button>
    </form>
  </Modal>

  <Modal titulo="Trocar profissional" :aberto="modal === 'trocar'" @fechar="modal = null">
    <form class="space-y-4" @submit.prevent="trocar">
      <p class="text-sm text-stone-600">Profissionais livres que fazem {{ agendamento.servicoNome }} neste horário:</p>
      <p v-if="!opcoes.length" class="rounded-xl bg-areia px-4 py-3 text-sm text-stone-600">
        Nenhuma profissional disponível neste horário. Tente remarcar.
      </p>
      <select v-else v-model="novaProfissional" class="campo" required>
        <option value="" disabled>Escolha…</option>
        <option v-for="p in opcoes" :key="p.id" :value="p.id">{{ p.nome }}</option>
      </select>
      <Mensagem v-if="erro">{{ erro }}</Mensagem>
      <button class="btn-primario w-full" :disabled="ocupado || !opcoes.length">Trocar</button>
    </form>
  </Modal>
</template>
