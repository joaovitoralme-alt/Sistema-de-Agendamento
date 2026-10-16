<script setup>
import { onMounted, ref } from 'vue'
import { publico } from '../../api/publico'
import FotoDoServico from '../../components/FotoDoServico.vue'
import LogoSimbolo from '../../components/LogoSimbolo.vue'
import { useClinica } from '../../dominio/clinica'
import { formatarDuracao } from '../../dominio/formatos'

const clinica = useClinica()
const servicos = ref([])
const carregando = ref(true)
const aberto = ref(null) // serviço com a descrição expandida

onMounted(async () => {
  try {
    servicos.value = await publico.listarServicos()
  } finally {
    carregando.value = false
  }
})

const passos = [
  { titulo: 'Escolha o procedimento', texto: 'Veja como funciona cada tratamento e quanto tempo ele leva.' },
  { titulo: 'Escolha dia e horário', texto: 'Só aparecem horários realmente livres na agenda.' },
  { titulo: 'A clínica confirma', texto: 'Entramos em contato para confirmar o horário e os valores.' },
]
</script>

<template>
  <!-- Destaque: fundo vinho com o símbolo do logo -->
  <section class="relative overflow-hidden bg-marca-500 text-pessego">
    <LogoSimbolo class="pointer-events-none absolute -right-24 top-1/2 h-[140%] w-auto -translate-y-1/2 text-pessego/[0.06] md:hidden" />
    <div class="relative mx-auto grid max-w-6xl items-center gap-12 px-4 py-20 sm:px-6 md:grid-cols-[1.4fr_1fr] md:py-28">
      <div>
        <LogoSimbolo class="mb-8 h-16 w-auto md:hidden" />
        <p class="mb-6 text-xs font-medium uppercase tracking-[0.35em] text-pessego/70">Estética &amp; bem-estar</p>
        <h1 class="text-5xl font-semibold leading-[1.08] md:text-6xl lg:text-7xl">
          Sua pele merece um cuidado feito sob medida.
        </h1>
        <p class="mt-6 max-w-xl text-lg leading-relaxed text-pessego/80">
          Tratamentos faciais e corporais com profissionais dedicadas. Escolha o procedimento,
          veja os horários livres e agende em poucos cliques.
        </p>
        <div class="mt-10 flex flex-wrap gap-3">
          <RouterLink to="/agendar" class="btn bg-pessego px-7 py-3 text-base text-marca-600 hover:bg-white">Agendar horário</RouterLink>
          <a href="#procedimentos" class="btn border border-pessego/40 px-7 py-3 text-base text-pessego hover:bg-pessego/10">Ver procedimentos</a>
        </div>
      </div>

      <div class="hidden flex-col items-center md:flex">
        <LogoSimbolo :titulo="clinica?.nomeDaClinica" class="h-72 w-auto lg:h-80" />
        <div class="mt-8 flex w-full items-center gap-4">
          <span class="h-px flex-1 bg-pessego/50" />
          <span class="font-sans text-sm uppercase tracking-[0.4em] text-pessego/90">{{ clinica?.nomeDaClinica }}</span>
          <span class="h-px flex-1 bg-pessego/50" />
        </div>
      </div>
    </div>
  </section>

  <!-- Procedimentos -->
  <section id="procedimentos" class="scroll-mt-20 bg-white py-20">
    <div class="mx-auto max-w-6xl px-4 sm:px-6">
      <div class="mb-12 max-w-2xl">
        <h2 class="text-4xl font-semibold md:text-5xl">Procedimentos</h2>
        <p class="mt-3 text-stone-600">Conheça os tratamentos e agende o que combina com você.</p>
      </div>

      <p v-if="carregando" class="text-stone-500">Carregando…</p>
      <p v-else-if="!servicos.length" class="text-stone-500">Em breve, novos procedimentos por aqui.</p>

      <div class="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
        <article v-for="servico in servicos" :key="servico.id"
          class="group flex flex-col overflow-hidden rounded-3xl border border-stone-200 bg-creme transition hover:shadow-lg">
          <FotoDoServico :servico="servico" class="aspect-[4/3]" />
          <div class="flex flex-1 flex-col p-6">
            <div class="flex items-baseline justify-between gap-3">
              <h3 class="text-2xl font-semibold">{{ servico.nome }}</h3>
              <span class="shrink-0 text-xs font-medium text-stone-500">{{ formatarDuracao(servico.duracaoEmMinutos) }}</span>
            </div>
            <p class="mt-3 flex-1 text-sm leading-relaxed text-stone-600"
              :class="aberto === servico.id ? '' : 'line-clamp-3'">
              {{ servico.descricao }}
            </p>
            <button v-if="servico.descricao?.length > 140" class="mt-2 self-start text-xs font-medium text-marca-600 hover:underline"
              @click="aberto = aberto === servico.id ? null : servico.id">
              {{ aberto === servico.id ? 'Ver menos' : 'Como funciona' }}
            </button>
            <RouterLink :to="{ name: 'agendar', query: { servico: servico.id } }" class="btn-secundario mt-5 self-start">
              Agendar este
            </RouterLink>
          </div>
        </article>
      </div>
    </div>
  </section>

  <!-- Como funciona -->
  <section id="como-funciona" class="scroll-mt-20 py-20">
    <div class="mx-auto max-w-6xl px-4 sm:px-6">
      <h2 class="mb-12 text-4xl font-semibold md:text-5xl">Como funciona</h2>
      <ol class="grid gap-6 md:grid-cols-3">
        <li v-for="(passo, i) in passos" :key="passo.titulo" class="cartao">
          <span class="font-titulo text-5xl text-marca-300">{{ i + 1 }}</span>
          <h3 class="mt-2 text-2xl font-semibold">{{ passo.titulo }}</h3>
          <p class="mt-2 text-sm leading-relaxed text-stone-600">{{ passo.texto }}</p>
        </li>
      </ol>
    </div>
  </section>
</template>
