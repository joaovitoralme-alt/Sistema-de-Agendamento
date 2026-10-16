<script setup>
defineProps({ titulo: String, aberto: Boolean, largo: Boolean })
const emit = defineEmits(['fechar'])
</script>

<template>
  <Teleport to="body">
    <div v-if="aberto" class="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-0 sm:items-center sm:p-4"
      @click.self="emit('fechar')">
      <div class="max-h-[92vh] w-full overflow-y-auto rounded-t-3xl bg-white p-6 shadow-xl sm:rounded-3xl"
        :class="largo ? 'sm:max-w-2xl' : 'sm:max-w-md'" role="dialog" aria-modal="true">
        <div class="mb-5 flex items-start justify-between gap-4">
          <h2 class="text-2xl font-semibold">{{ titulo }}</h2>
          <button class="rounded-full p-1 text-stone-400 hover:bg-stone-100 hover:text-stone-700" aria-label="Fechar"
            @click="emit('fechar')">
            <svg class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor"><path d="M6.3 5 5 6.3 8.7 10 5 13.7 6.3 15 10 11.3l3.7 3.7 1.3-1.3L11.3 10 15 6.3 13.7 5 10 8.7z"/></svg>
          </button>
        </div>
        <slot />
      </div>
    </div>
  </Teleport>
</template>
