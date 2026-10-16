<script setup>
// Mostra a primeira foto do serviço ou, sem foto, um fundo liso com o símbolo da marca.
import LogoSimbolo from './LogoSimbolo.vue'
defineProps({ servico: Object })
</script>

<template>
  <div class="relative overflow-hidden bg-areia">
    <img v-if="servico.fotos?.length" :src="servico.fotos[0]" :alt="servico.nome"
      class="absolute inset-0 h-full w-full object-cover" loading="lazy" />
    <LogoSimbolo v-else class="absolute left-1/2 top-1/2 h-2/5 w-auto -translate-x-1/2 -translate-y-1/2 text-marca-200" />
  </div>
</template>
