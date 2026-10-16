<script setup>
import { computed, inject, onMounted, ref, watch } from 'vue'
import { admin } from '../../api/admin'
import AcoesDoAgendamento from '../../components/AcoesDoAgendamento.vue'
import SeloStatus from '../../components/SeloStatus.vue'
import {
  formatarHora, formatarTelefone, hoje, linkDoWhatsApp, paraDataIso, somarDias,
} from '../../dominio/formatos'

const atualizarAlertas = inject('atualizarAlertas')

const dia = ref(hoje())
const status = ref('')
const profissionalId = ref('')
const profissionais = ref([])
const agendamentos = ref([])
const carregando = ref(false)

const titulo = computed(() =>
  dia.value.toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long' }))

async function carregar() {
  carregando.value = true
  try {
    agendamentos.value = await admin.agendamentos({
      de: paraDataIso(dia.value),
      ate: paraDataIso(somarDias(dia.value, 1)),
      status: status.value,
      profissionalId: profissionalId.value,
    })
  } finally {
    carregando.value = false
  }
}

function aposAlterar() {
  carregar()
  atualizarAlertas()
}

const mudarDia = (n) => (dia.value = somarDias(dia.value, n))
const irParaHoje = () => (dia.value = hoje())

onMounted(async () => {
  profissionais.value = await admin.profissionais()
  carregar()
})
watch([dia, status, profissionalId], carregar)

const mensagem = (a) =>
  `Olá, ${a.clienteNome.split(' ')[0]}! Aqui é da clínica, sobre seu agendamento de ${a.servicoNome} em ${a.inicio.slice(8, 10)}/${a.inicio.slice(5, 7)} às ${formatarHora(a.inicio)}.`
</script>

<template>
  <div class="flex flex-wrap items-end justify-between gap-4">
    <div>
      <h1 class="text-4xl font-semibold">Agenda</h1>
      <p class="mt-1 first-letter:uppercase text-stone-500">{{ titulo }}</p>
    </div>
    <div class="flex items-center gap-2">
      <button class="btn-secundario btn-pequeno" aria-label="Dia anterior" @click="mudarDia(-1)">‹</button>
      <button class="btn-secundario btn-pequeno" @click="irParaHoje">Hoje</button>
      <button class="btn-secundario btn-pequeno" aria-label="Próximo dia" @click="mudarDia(1)">›</button>
      <input type="date" class="campo w-auto py-1.5" :value="paraDataIso(dia)"
        @change="(e) => e.target.value && (dia = new Date(e.target.value + 'T00:00'))" />
    </div>
  </div>

  <div class="mt-6 flex flex-wrap gap-3">
    <select v-model="status" class="campo w-auto">
      <option value="">Todos os status</option>
      <option value="Pendente">Pendentes</option>
      <option value="Confirmado">Confirmados</option>
      <option value="Cancelado">Cancelados</option>
    </select>
    <select v-model="profissionalId" class="campo w-auto">
      <option value="">Todas as profissionais</option>
      <option v-for="p in profissionais" :key="p.id" :value="p.id">{{ p.nome }}</option>
    </select>
  </div>

  <p v-if="carregando && !agendamentos.length" class="mt-8 text-stone-500">Carregando…</p>
  <p v-else-if="!agendamentos.length" class="mt-8 rounded-2xl border border-dashed border-stone-300 px-6 py-10 text-center text-stone-500">
    Nenhum agendamento neste dia.
  </p>

  <ul class="mt-6 space-y-3">
    <li v-for="a in agendamentos" :key="a.id" class="cartao flex flex-col gap-4 md:flex-row md:items-center"
      :class="{ 'opacity-60': a.status === 'Cancelado' }">
      <div class="w-28 shrink-0">
        <p class="font-titulo text-3xl font-semibold leading-none">{{ formatarHora(a.inicio) }}</p>
        <p class="mt-1 text-xs text-stone-500">até {{ formatarHora(a.fimDoAtendimento) }}</p>
      </div>
      <div class="flex-1">
        <div class="flex flex-wrap items-center gap-2">
          <p class="font-medium">{{ a.clienteNome }}</p>
          <SeloStatus :status="a.status" />
        </div>
        <p class="text-sm text-stone-600">{{ a.servicoNome }} · {{ a.profissionalNome }}</p>
        <a :href="linkDoWhatsApp(a.clienteTelefone, mensagem(a))" target="_blank" rel="noopener"
          class="text-sm text-marca-600 hover:underline">{{ formatarTelefone(a.clienteTelefone) }}</a>
        <p v-if="a.observacao" class="mt-1 text-sm italic text-stone-500">“{{ a.observacao }}”</p>
      </div>
      <AcoesDoAgendamento :agendamento="a" @alterado="aposAlterar" />
    </li>
  </ul>
</template>
