<script setup>
import { inject, onMounted, ref } from 'vue'
import { admin } from '../../api/admin'
import AcoesDoAgendamento from '../../components/AcoesDoAgendamento.vue'
import SeloStatus from '../../components/SeloStatus.vue'
import { formatarData, formatarHora, formatarTelefone, linkDoWhatsApp } from '../../dominio/formatos'

const atualizarAlertas = inject('atualizarAlertas')
const alertas = ref([])
const carregando = ref(true)

async function carregar() {
  carregando.value = true
  try {
    alertas.value = await admin.alertas()
  } finally {
    carregando.value = false
  }
  atualizarAlertas()
}

onMounted(carregar)
</script>

<template>
  <h1 class="text-4xl font-semibold">Alertas</h1>
  <p class="mt-1 max-w-2xl text-stone-500">
    Agendamentos que ficaram em conflito (férias, feriado, mudança de horário). Fale com o cliente e
    remarque, troque a profissional ou cancele. O alerta some sozinho quando o conflito é resolvido.
  </p>

  <p v-if="carregando" class="mt-8 text-stone-500">Carregando…</p>
  <p v-else-if="!alertas.length" class="mt-8 rounded-2xl bg-emerald-50 px-6 py-10 text-center text-emerald-800">
    Tudo certo! Nenhum agendamento em conflito.
  </p>

  <ul class="mt-6 space-y-3">
    <li v-for="{ agendamento: a, motivo } in alertas" :key="a.id" class="cartao border-l-4 border-l-red-400">
      <p class="text-sm font-medium text-red-700">{{ motivo }}</p>
      <div class="mt-3 flex flex-col gap-4 md:flex-row md:items-center">
        <div class="flex-1">
          <div class="flex flex-wrap items-center gap-2">
            <p class="font-medium">{{ a.clienteNome }}</p>
            <SeloStatus :status="a.status" />
          </div>
          <p class="text-sm first-letter:uppercase text-stone-600">{{ formatarData(a.inicio) }} às {{ formatarHora(a.inicio) }}</p>
          <p class="text-sm text-stone-600">{{ a.servicoNome }} · {{ a.profissionalNome }}</p>
          <a :href="linkDoWhatsApp(a.clienteTelefone)" target="_blank" rel="noopener" class="text-sm text-marca-600 hover:underline">
            {{ formatarTelefone(a.clienteTelefone) }}
          </a>
        </div>
        <AcoesDoAgendamento :agendamento="a" @alterado="carregar" />
      </div>
    </li>
  </ul>
</template>
