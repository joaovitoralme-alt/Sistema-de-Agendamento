<script setup>
import { ref } from 'vue'
import LogoSimbolo from '../../components/LogoSimbolo.vue'
import { useClinica } from '../../dominio/clinica'
import { linkDoInstagram, linkDoWhatsApp } from '../../dominio/formatos'

const clinica = useClinica()
const menuAberto = ref(false)
</script>

<template>
  <div class="flex min-h-screen flex-col">
    <header class="sticky top-0 z-40 border-b border-marca-100 bg-creme">
      <div class="mx-auto flex max-w-6xl items-center justify-between px-4 py-4 sm:px-6">
        <RouterLink to="/" class="flex items-center gap-2.5 text-marca-500">
          <LogoSimbolo class="h-9 w-auto" />
          <span class="font-titulo text-xl font-semibold tracking-tight">{{ clinica?.nomeDaClinica ?? '' }}</span>
        </RouterLink>

        <nav class="hidden items-center gap-8 text-sm text-stone-600 md:flex">
          <RouterLink to="/#procedimentos" class="hover:text-marca-600">Procedimentos</RouterLink>
          <RouterLink to="/#como-funciona" class="hover:text-marca-600">Como funciona</RouterLink>
          <RouterLink to="/#contato" class="hover:text-marca-600">Contato</RouterLink>
          <RouterLink to="/agendar" class="btn-primario">Agendar horário</RouterLink>
        </nav>

        <button class="rounded-lg p-2 text-stone-700 md:hidden" aria-label="Abrir menu" @click="menuAberto = !menuAberto">
          <svg class="h-6 w-6" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M4 7h16M4 12h16M4 17h16"/></svg>
        </button>
      </div>

      <nav v-if="menuAberto" class="flex flex-col gap-1 border-t border-stone-200 px-4 py-3 text-stone-700 md:hidden"
        @click="menuAberto = false">
        <RouterLink to="/#procedimentos" class="rounded-lg px-3 py-2 hover:bg-areia">Procedimentos</RouterLink>
        <RouterLink to="/#como-funciona" class="rounded-lg px-3 py-2 hover:bg-areia">Como funciona</RouterLink>
        <RouterLink to="/#contato" class="rounded-lg px-3 py-2 hover:bg-areia">Contato</RouterLink>
        <RouterLink to="/agendar" class="btn-primario mt-2">Agendar horário</RouterLink>
      </nav>
    </header>

    <main class="flex-1">
      <RouterView />
    </main>

    <footer id="contato" class="bg-marca-600 text-pessego/80">
      <div class="mx-auto grid max-w-6xl gap-10 px-4 py-14 sm:px-6 md:grid-cols-3">
        <div>
          <LogoSimbolo class="mb-4 h-14 w-auto text-pessego" />
          <p class="font-titulo text-2xl text-pessego">{{ clinica?.nomeDaClinica }}</p>
          <p class="mt-3 text-sm leading-relaxed text-pessego/60">Cuidado, técnica e acolhimento em cada atendimento.</p>
        </div>
        <div class="space-y-2 text-sm">
          <p class="mb-3 font-medium uppercase tracking-[0.3em] text-pessego/50">Contato</p>
          <a v-if="clinica?.whatsApp" :href="linkDoWhatsApp(clinica.whatsApp)" target="_blank" rel="noopener"
            class="block hover:text-pessego">WhatsApp</a>
          <a v-if="clinica?.instagram" :href="linkDoInstagram(clinica.instagram)" target="_blank" rel="noopener"
            class="block hover:text-pessego">Instagram {{ clinica.instagram.startsWith('@') ? clinica.instagram : '' }}</a>
          <p v-if="clinica?.endereco">{{ clinica.endereco }}</p>
        </div>
        <div class="md:text-right">
          <RouterLink to="/agendar" class="btn bg-pessego text-marca-600 hover:bg-white">Agendar horário</RouterLink>
        </div>
      </div>
      <div class="border-t border-pessego/15 py-5 text-center text-xs text-pessego/50">
        © {{ new Date().getFullYear() }} {{ clinica?.nomeDaClinica }}
        <RouterLink to="/admin" class="ml-3 hover:text-pessego">Área da clínica</RouterLink>
      </div>
    </footer>
  </div>
</template>
