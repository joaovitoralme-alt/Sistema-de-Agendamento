<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { admin } from '../../api/admin'
import { formatarData, formatarHora, paraDataIso, somarDias } from '../../dominio/formatos'

const TAMANHO = 30

const usuarios = ref([])
const filtro = ref({ usuarioId: '', de: '', ate: '', busca: '' })
const pagina = ref(1)
const resultado = ref({ itens: [], total: 0 })
const carregando = ref(false)

const totalDePaginas = computed(() => Math.max(1, Math.ceil(resultado.value.total / TAMANHO)))

// Agrupa por área para colorir a etiqueta da ação (agendamento, usuário, login...).
const AREAS = {
  agendamento: { nome: 'Agendamento', classe: 'bg-marca-50 text-marca-700' },
  servico: { nome: 'Procedimento', classe: 'bg-amber-50 text-amber-800' },
  profissional: { nome: 'Profissional', classe: 'bg-sky-50 text-sky-800' },
  ausencia: { nome: 'Ausência', classe: 'bg-violet-50 text-violet-800' },
  configuracao: { nome: 'Configuração', classe: 'bg-stone-100 text-stone-700' },
  usuario: { nome: 'Usuário', classe: 'bg-emerald-50 text-emerald-800' },
  auth: { nome: 'Acesso', classe: 'bg-stone-100 text-stone-600' },
}
const area = (acao) => AREAS[acao.split('.')[0]] ?? { nome: acao, classe: 'bg-stone-100 text-stone-600' }

async function carregar() {
  carregando.value = true
  try {
    const f = filtro.value
    resultado.value = await admin.historico({
      usuarioId: f.usuarioId,
      de: f.de,
      // "até" inclui o dia inteiro escolhido.
      ate: f.ate ? paraDataIso(somarDias(new Date(`${f.ate}T00:00`), 1)) : '',
      busca: f.busca,
      pagina: pagina.value,
      tamanho: TAMANHO,
    })
  } finally {
    carregando.value = false
  }
}

onMounted(async () => {
  usuarios.value = await admin.usuarios()
  carregar()
})

let espera
watch(filtro, () => {
  clearTimeout(espera)
  espera = setTimeout(() => {
    pagina.value = 1
    carregar()
  }, 300)
}, { deep: true })

function irPara(p) {
  pagina.value = p
  carregar()
}
</script>

<template>
  <h1 class="text-4xl font-semibold">Histórico</h1>
  <p class="mt-1 max-w-2xl text-stone-500">Tudo o que foi feito no painel, por quem e quando. Os registros não podem ser alterados.</p>

  <div class="mt-6 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
    <select v-model="filtro.usuarioId" class="campo">
      <option value="">Todos os usuários</option>
      <option v-for="u in usuarios" :key="u.id" :value="u.id">{{ u.nome }} ({{ u.codigo }})</option>
    </select>
    <input v-model="filtro.busca" class="campo" placeholder="Buscar (ex.: Maria, cancelou…)" />
    <label class="flex items-center gap-2 text-sm text-stone-500">De <input v-model="filtro.de" type="date" class="campo" /></label>
    <label class="flex items-center gap-2 text-sm text-stone-500">Até <input v-model="filtro.ate" type="date" class="campo" /></label>
  </div>

  <p class="mt-4 text-sm text-stone-500">{{ resultado.total }} registro(s)</p>

  <ol class="mt-3 divide-y divide-stone-100 rounded-2xl border border-stone-200 bg-white" :class="{ 'opacity-60': carregando }">
    <li v-if="!resultado.itens.length" class="px-5 py-10 text-center text-stone-500">Nenhum registro encontrado.</li>
    <li v-for="r in resultado.itens" :key="r.id" class="flex flex-col gap-2 px-5 py-4 sm:flex-row sm:items-start sm:gap-5">
      <div class="w-36 shrink-0 text-sm text-stone-500">
        <p>{{ formatarData(r.dataHora) }}</p>
        <p class="font-medium text-tinta">{{ formatarHora(r.dataHora) }}</p>
      </div>
      <div class="flex-1">
        <div class="flex flex-wrap items-center gap-2 text-sm">
          <span class="font-medium">{{ r.usuarioNome }}</span>
          <span class="font-mono text-xs text-stone-400">{{ r.usuarioCodigo }}</span>
          <span class="rounded-full px-2 py-0.5 text-xs" :class="area(r.acao).classe">{{ area(r.acao).nome }}</span>
        </div>
        <p class="mt-1 text-sm leading-relaxed text-stone-700">{{ r.descricao }}</p>
      </div>
    </li>
  </ol>

  <div v-if="totalDePaginas > 1" class="mt-4 flex items-center justify-center gap-3 text-sm">
    <button class="btn-secundario btn-pequeno" :disabled="pagina <= 1" @click="irPara(pagina - 1)">Anterior</button>
    <span class="text-stone-500">Página {{ pagina }} de {{ totalDePaginas }}</span>
    <button class="btn-secundario btn-pequeno" :disabled="pagina >= totalDePaginas" @click="irPara(pagina + 1)">Próxima</button>
  </div>
</template>
