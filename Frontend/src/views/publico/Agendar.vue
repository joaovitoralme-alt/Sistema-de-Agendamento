<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { publico } from '../../api/publico'
import FotoDoServico from '../../components/FotoDoServico.vue'
import Mensagem from '../../components/Mensagem.vue'
import { useClinica } from '../../dominio/clinica'
import {
  diaDaSemanaDe, formatarDataLonga, formatarDuracao, hoje, juntarDataEHora, linkDoWhatsApp, paraDataIso, somarDias,
} from '../../dominio/formatos'

const route = useRoute()
const clinica = useClinica()

// Passos: servico → horario → profissional (opcional) → dados → concluido
const passo = ref('servico')
const servicos = ref([])
const servico = ref(null)
const data = ref(null)
const horarios = ref([])
const hora = ref(null)
const profissionais = ref([])
const profissionalId = ref(null)
const cliente = ref({ nome: '', telefone: '', observacao: '' })
const resultado = ref(null)
const erro = ref('')
const carregando = ref(false)

const podeEscolherProfissional = computed(() => clinica.value?.permitirEscolherProfissional)
const inicio = computed(() => (data.value && hora.value ? juntarDataEHora(paraDataIso(data.value), hora.value) : null))

const dias = computed(() => {
  const total = Math.min(clinica.value?.diasMaximosParaAgendar ?? 30, 45)
  return Array.from({ length: total + 1 }, (_, i) => somarDias(hoje(), i)).map((d) => ({
    data: d,
    fechado: clinica.value?.diasFechados?.includes(diaDaSemanaDe(d)),
  }))
})

const passos = computed(() => [
  { id: 'servico', nome: 'Procedimento' },
  { id: 'horario', nome: 'Data e hora' },
  ...(podeEscolherProfissional.value ? [{ id: 'profissional', nome: 'Profissional' }] : []),
  { id: 'dados', nome: 'Seus dados' },
])
const indiceDoPasso = computed(() => passos.value.findIndex((p) => p.id === passo.value))

onMounted(async () => {
  servicos.value = await publico.listarServicos()
  const escolhido = servicos.value.find((s) => s.id === route.query.servico)
  if (escolhido) escolherServico(escolhido)
})

function escolherServico(s) {
  servico.value = s
  hora.value = null
  passo.value = 'horario'
  if (!data.value) irParaPrimeiroDiaComHorarios()
  else carregarHorarios()
}

// Abre direto no primeiro dia que tem horário livre (procura nas próximas 2 semanas).
async function irParaPrimeiroDiaComHorarios() {
  const abertos = dias.value.filter((d) => !d.fechado).slice(0, 14)
  if (!abertos.length) return
  for (const d of abertos) {
    data.value = d.data
    await carregarHorarios()
    if (horarios.value.length) return
  }
  data.value = abertos[0].data
  await carregarHorarios()
}

function escolherData(d) {
  data.value = d
  hora.value = null
  carregarHorarios()
}

async function carregarHorarios() {
  if (!servico.value || !data.value) return
  carregando.value = true
  erro.value = ''
  try {
    horarios.value = await publico.horarios(servico.value.id, paraDataIso(data.value))
  } catch (e) {
    erro.value = e.message
    horarios.value = []
  } finally {
    carregando.value = false
  }
}

// Os dias só são conhecidos depois que a configuração da clínica carrega.
watch(dias, () => {
  if (servico.value && !data.value) irParaPrimeiroDiaComHorarios()
})

async function escolherHora(h) {
  hora.value = h
  profissionalId.value = null
  if (!podeEscolherProfissional.value) {
    passo.value = 'dados'
    return
  }
  carregando.value = true
  try {
    profissionais.value = await publico.profissionais(servico.value.id, inicio.value)
    passo.value = 'profissional'
  } catch (e) {
    erro.value = e.message
  } finally {
    carregando.value = false
  }
}

async function confirmar() {
  carregando.value = true
  erro.value = ''
  try {
    resultado.value = await publico.agendar({
      servicoId: servico.value.id,
      inicio: inicio.value,
      profissionalId: profissionalId.value,
      clienteNome: cliente.value.nome,
      clienteTelefone: cliente.value.telefone,
      observacao: cliente.value.observacao || null,
    })
    passo.value = 'concluido'
  } catch (e) {
    erro.value = e.message
    if (e.message.includes('não está mais disponível')) {
      hora.value = null
      passo.value = 'horario'
      carregarHorarios()
    }
  } finally {
    carregando.value = false
  }
}

const mensagemWhatsApp = computed(() =>
  resultado.value
    ? `Olá! Acabei de solicitar ${resultado.value.servicoNome} para ${formatarDataLonga(resultado.value.inicio)} às ${resultado.value.inicio.slice(11, 16)}. Meu nome é ${cliente.value.nome}.`
    : '',
)

function voltarPara(id) {
  if (passos.value.findIndex((p) => p.id === id) < indiceDoPasso.value) passo.value = id
}
</script>

<template>
  <section class="mx-auto max-w-3xl px-4 py-12 sm:px-6 md:py-16">
    <h1 class="text-4xl font-semibold md:text-5xl">Agendar horário</h1>

    <!-- Indicador de passos -->
    <ol v-if="passo !== 'concluido'" class="mt-8 flex flex-wrap gap-2 text-xs">
      <li v-for="(p, i) in passos" :key="p.id">
        <button class="rounded-full px-3 py-1.5 font-medium transition"
          :class="i === indiceDoPasso ? 'bg-marca-500 text-white' : i < indiceDoPasso ? 'bg-marca-100 text-marca-700 hover:bg-marca-200' : 'bg-stone-100 text-stone-400'"
          :disabled="i >= indiceDoPasso" @click="voltarPara(p.id)">
          {{ i + 1 }}. {{ p.nome }}
        </button>
      </li>
    </ol>

    <Mensagem v-if="erro" class="mt-6">{{ erro }}</Mensagem>

    <!-- 1. Procedimento -->
    <div v-if="passo === 'servico'" class="mt-8 grid gap-4 sm:grid-cols-2">
      <button v-for="s in servicos" :key="s.id"
        class="flex items-center gap-4 rounded-2xl border border-stone-200 bg-white p-3 text-left transition hover:border-marca-300 hover:shadow-md"
        @click="escolherServico(s)">
        <FotoDoServico :servico="s" class="h-20 w-20 shrink-0 rounded-xl" />
        <span>
          <span class="block font-titulo text-xl font-semibold">{{ s.nome }}</span>
          <span class="text-sm text-stone-500">{{ formatarDuracao(s.duracaoEmMinutos) }}</span>
        </span>
      </button>
    </div>

    <!-- 2. Data e hora -->
    <div v-if="passo === 'horario'" class="mt-8">
      <p class="text-stone-600">
        <strong class="text-tinta">{{ servico.nome }}</strong> · {{ formatarDuracao(servico.duracaoEmMinutos) }}
        <button class="ml-2 text-sm text-marca-600 hover:underline" @click="passo = 'servico'">trocar</button>
      </p>

      <div class="-mx-4 mt-6 flex gap-2 overflow-x-auto px-4 pb-2">
        <button v-for="d in dias" :key="d.data.getTime()" :disabled="d.fechado"
          class="flex w-16 shrink-0 flex-col items-center rounded-2xl border py-3 text-sm transition disabled:opacity-35"
          :class="data?.getTime() === d.data.getTime() ? 'border-marca-500 bg-marca-500 text-white' : 'border-stone-200 bg-white hover:border-marca-300'"
          @click="escolherData(d.data)">
          <span class="text-xs uppercase">{{ d.data.toLocaleDateString('pt-BR', { weekday: 'short' }).replace('.', '') }}</span>
          <span class="font-titulo text-2xl font-semibold leading-tight">{{ d.data.getDate() }}</span>
          <span class="text-[11px] opacity-70">{{ d.data.toLocaleDateString('pt-BR', { month: 'short' }).replace('.', '') }}</span>
        </button>
      </div>

      <div class="mt-6">
        <p v-if="carregando" class="text-stone-500">Buscando horários…</p>
        <p v-else-if="data && !horarios.length" class="rounded-2xl bg-areia px-5 py-6 text-center text-stone-600">
          Não há horários livres neste dia. Tente outra data.
        </p>
        <div v-else class="grid grid-cols-3 gap-2 sm:grid-cols-5">
          <button v-for="h in horarios" :key="h"
            class="rounded-xl border border-stone-200 bg-white py-2.5 text-sm font-medium transition hover:border-marca-400 hover:bg-marca-50"
            @click="escolherHora(h)">
            {{ h }}
          </button>
        </div>
      </div>
    </div>

    <!-- 3. Profissional -->
    <div v-if="passo === 'profissional'" class="mt-8">
      <p class="text-stone-600">{{ servico.nome }} · {{ formatarDataLonga(inicio) }} às {{ hora }}</p>
      <div class="mt-6 grid gap-3 sm:grid-cols-2">
        <button class="rounded-2xl border bg-white p-5 text-left transition hover:border-marca-300"
          :class="profissionalId === null ? 'border-marca-500 ring-2 ring-marca-100' : 'border-stone-200'"
          @click="profissionalId = null; passo = 'dados'">
          <span class="block font-titulo text-xl font-semibold">Sem preferência</span>
          <span class="text-sm text-stone-500">A clínica escolhe quem vai te atender</span>
        </button>
        <button v-for="p in profissionais" :key="p.id"
          class="rounded-2xl border bg-white p-5 text-left transition hover:border-marca-300"
          :class="profissionalId === p.id ? 'border-marca-500 ring-2 ring-marca-100' : 'border-stone-200'"
          @click="profissionalId = p.id; passo = 'dados'">
          <span class="block font-titulo text-xl font-semibold">{{ p.nome }}</span>
        </button>
      </div>
    </div>

    <!-- 4. Dados -->
    <form v-if="passo === 'dados'" class="mt-8 space-y-5" @submit.prevent="confirmar">
      <div class="rounded-2xl bg-areia p-5 text-sm leading-relaxed">
        <p class="font-titulo text-2xl font-semibold">{{ servico.nome }}</p>
        <p class="first-letter:uppercase">{{ formatarDataLonga(inicio) }} às {{ hora }}</p>
        <p v-if="profissionalId" class="text-stone-600">com {{ profissionais.find((p) => p.id === profissionalId)?.nome }}</p>
      </div>
      <div>
        <label class="rotulo" for="nome">Seu nome</label>
        <input id="nome" v-model="cliente.nome" class="campo" required maxlength="120" autocomplete="name" />
      </div>
      <div>
        <label class="rotulo" for="telefone">Telefone / WhatsApp (com DDD)</label>
        <input id="telefone" v-model="cliente.telefone" class="campo" required type="tel" inputmode="tel"
          placeholder="(11) 98888-7777" autocomplete="tel" />
      </div>
      <div>
        <label class="rotulo" for="obs">Observação <span class="font-normal text-stone-400">(opcional)</span></label>
        <textarea id="obs" v-model="cliente.observacao" class="campo" rows="3" maxlength="500" />
      </div>
      <p class="text-sm text-stone-500">Depois do pedido, a clínica entra em contato para confirmar o horário e os valores.</p>
      <button class="btn-primario w-full py-3 text-base sm:w-auto" :disabled="carregando">
        {{ carregando ? 'Enviando…' : 'Solicitar agendamento' }}
      </button>
    </form>

    <!-- Concluído -->
    <div v-if="passo === 'concluido'" class="mt-10 rounded-3xl bg-white p-8 text-center shadow-sm ring-1 ring-stone-200">
      <div class="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-emerald-50 text-emerald-600">
        <svg class="h-7 w-7" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="m5 13 4 4L19 7"/></svg>
      </div>
      <h2 class="mt-5 text-3xl font-semibold">Pedido recebido!</h2>
      <p class="mt-3 text-stone-600">
        {{ resultado.servicoNome }} · <span class="first-letter:uppercase">{{ formatarDataLonga(resultado.inicio) }}</span> às {{ resultado.inicio.slice(11, 16) }}
        <template v-if="resultado.profissionalNome"> com {{ resultado.profissionalNome }}</template>
      </p>
      <p class="mt-4 text-sm text-stone-500">Em breve a clínica entra em contato para confirmar o horário e os valores.</p>
      <div class="mt-7 flex flex-wrap justify-center gap-3">
        <a v-if="clinica?.whatsApp" :href="linkDoWhatsApp(clinica.whatsApp, mensagemWhatsApp)" target="_blank" rel="noopener"
          class="btn-primario">Falar no WhatsApp</a>
        <RouterLink to="/" class="btn-secundario">Voltar ao início</RouterLink>
      </div>
    </div>
  </section>
</template>
