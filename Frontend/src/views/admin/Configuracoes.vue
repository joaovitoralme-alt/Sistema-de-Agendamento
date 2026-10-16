<script setup>
import { inject, onMounted, ref } from 'vue'
import { admin } from '../../api/admin'
import Mensagem from '../../components/Mensagem.vue'
import { recarregarClinica } from '../../dominio/clinica'
import { DIAS_DA_SEMANA } from '../../dominio/formatos'

const atualizarAlertas = inject('atualizarAlertas')
const config = ref(null)
const aviso = ref(null) // { tipo, texto }

onMounted(async () => (config.value = await admin.configuracao()))

async function salvar() {
  aviso.value = null
  try {
    config.value = await admin.salvarConfiguracao(config.value)
    aviso.value = { tipo: 'sucesso', texto: 'Configurações salvas.' }
    recarregarClinica()
    atualizarAlertas()
  } catch (e) {
    aviso.value = { tipo: 'erro', texto: e.message }
  }
}

</script>

<template>
  <h1 class="text-4xl font-semibold">Configurações</h1>

  <form v-if="config" class="mt-8 max-w-2xl space-y-8" @submit.prevent="salvar">
    <section class="cartao space-y-4">
      <h2 class="text-2xl font-semibold">Dados da clínica</h2>
      <div><label class="rotulo">Nome</label><input v-model="config.nomeDaClinica" class="campo" required maxlength="100" /></div>
      <div class="grid gap-4 sm:grid-cols-2">
        <div><label class="rotulo">WhatsApp (com DDD)</label><input v-model="config.whatsApp" class="campo" placeholder="11999999999" /></div>
        <div><label class="rotulo">Instagram</label><input v-model="config.instagram" class="campo" placeholder="@suaclinica" /></div>
      </div>
      <div><label class="rotulo">Endereço</label><input v-model="config.endereco" class="campo" maxlength="200" /></div>
    </section>

    <section class="cartao space-y-5">
      <h2 class="text-2xl font-semibold">Regras da agenda</h2>
      <label class="flex items-start gap-3 text-sm">
        <input v-model="config.permitirEscolherProfissional" type="checkbox" class="mt-0.5 h-4 w-4 accent-marca-500" />
        <span>
          <span class="font-medium">Cliente pode escolher a profissional</span>
          <span class="block text-stone-500">Desligado: o sistema escolhe a que tem menos atendimentos no dia.</span>
        </span>
      </label>
      <fieldset>
        <legend class="rotulo">Dias em que a clínica não abre</legend>
        <div class="flex flex-wrap gap-2">
          <label v-for="d in DIAS_DA_SEMANA" :key="d.valor"
            class="flex cursor-pointer items-center gap-2 rounded-full border px-3 py-1.5 text-sm"
            :class="config.diasFechados.includes(d.valor) ? 'border-marca-400 bg-marca-50 text-marca-700' : 'border-stone-200'">
            <input v-model="config.diasFechados" type="checkbox" :value="d.valor" class="sr-only" />
            {{ d.nome }}
          </label>
        </div>
      </fieldset>
      <div class="grid gap-4 sm:grid-cols-2">
        <div>
          <label class="rotulo">Antecedência mínima (minutos)</label>
          <input v-model.number="config.antecedenciaMinimaEmMinutos" type="number" min="0" step="15" class="campo" />
          <p class="mt-1 text-xs text-stone-500">Ex.: 120 = o cliente só agenda com 2 horas de antecedência.</p>
        </div>
        <div>
          <label class="rotulo">Agenda aberta por (dias)</label>
          <input v-model.number="config.diasMaximosParaAgendar" type="number" min="1" max="365" class="campo" />
        </div>
      </div>
    </section>

    <Mensagem v-if="aviso" :tipo="aviso.tipo">{{ aviso.texto }}</Mensagem>
    <button class="btn-primario">Salvar configurações</button>
  </form>

</template>
