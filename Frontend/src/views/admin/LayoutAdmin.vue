<script setup>
import { onMounted, provide, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { admin } from '../../api/admin'
import { sessao } from '../../dominio/sessao'

const route = useRoute()
const router = useRouter()
const totalDeAlertas = ref(0)

const itens = [
  { rota: 'agenda', nome: 'Agenda' },
  { rota: 'alertas', nome: 'Alertas' },
  { rota: 'servicos', nome: 'Procedimentos' },
  { rota: 'profissionais', nome: 'Profissionais' },
  { rota: 'ausencias', nome: 'Ausências' },
  { rota: 'configuracoes', nome: 'Configurações' },
  { rota: 'usuarios', nome: 'Usuários' },
  { rota: 'historico', nome: 'Histórico' },
  { rota: 'minha-conta', nome: 'Minha conta' },
]

async function atualizarAlertas() {
  totalDeAlertas.value = (await admin.alertas().catch(() => [])).length
}

// As telas filhas chamam isso quando mudam algo que pode criar ou resolver alertas.
provide('atualizarAlertas', atualizarAlertas)

onMounted(atualizarAlertas)
watch(() => route.name, atualizarAlertas)

function sair() {
  sessao.sair()
  router.push({ name: 'login' })
}
</script>

<template>
  <div class="min-h-screen bg-stone-50 md:flex">
    <aside class="border-b border-stone-200 bg-white md:fixed md:inset-y-0 md:w-60 md:border-b-0 md:border-r">
      <div class="flex items-center justify-between px-5 py-4 md:block md:py-6">
        <RouterLink to="/admin" class="font-titulo text-2xl font-semibold text-marca-700">Painel</RouterLink>
        <button class="text-sm text-stone-500 hover:text-marca-600 md:hidden" @click="sair">Sair</button>
      </div>
      <nav class="flex gap-1 overflow-x-auto px-3 pb-3 md:flex-col md:px-3">
        <RouterLink v-for="item in itens" :key="item.rota" :to="{ name: item.rota }"
          class="flex shrink-0 items-center justify-between gap-3 rounded-xl px-3 py-2 text-sm text-stone-600 hover:bg-stone-100"
          active-class="bg-marca-50 font-medium text-marca-700">
          {{ item.nome }}
          <span v-if="item.rota === 'alertas' && totalDeAlertas"
            class="rounded-full bg-red-500 px-2 py-0.5 text-xs font-semibold text-white">{{ totalDeAlertas }}</span>
        </RouterLink>
      </nav>
      <div class="hidden px-5 py-4 text-xs text-stone-400 md:absolute md:bottom-0 md:block">
        <p class="truncate font-medium text-stone-600">{{ sessao.usuario?.nome }}</p>
        <p class="font-mono">{{ sessao.usuario?.codigo }}</p>
        <div class="mt-2 flex gap-3">
          <RouterLink to="/" class="hover:text-marca-600">Ver site</RouterLink>
          <button class="hover:text-marca-600" @click="sair">Sair</button>
        </div>
      </div>
    </aside>

    <main class="flex-1 px-4 py-6 sm:px-8 md:ml-60 md:py-10">
      <RouterView />
    </main>
  </div>
</template>
