<script setup>
import { STATUS } from '../dominio/formatos'
defineProps({ status: String })
</script>

<template>
  <span class="inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset" :class="STATUS[status]?.classe">
    {{ STATUS[status]?.nome ?? status }}
  </span>
</template>
