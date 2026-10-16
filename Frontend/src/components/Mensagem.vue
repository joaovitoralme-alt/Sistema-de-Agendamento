<script setup>
defineProps({ tipo: { type: String, default: 'erro' } })
</script>

<template>
  <div class="rounded-xl px-4 py-3 text-sm"
    :class="{
      'bg-red-50 text-red-800': tipo === 'erro',
      'bg-emerald-50 text-emerald-800': tipo === 'sucesso',
      'bg-amber-50 text-amber-900': tipo === 'aviso',
    }" role="status">
    <slot />
  </div>
</template>
