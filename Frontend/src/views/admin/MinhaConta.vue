<script setup>
import { ref } from 'vue'
import Mensagem from '../../components/Mensagem.vue'
import { sessao } from '../../dominio/sessao'

const atual = ref('')
const nova = ref('')
const confirmacao = ref('')
const aviso = ref(null)
const enviando = ref(false)

async function trocar() {
  aviso.value = null
  if (nova.value !== confirmacao.value) {
    aviso.value = { tipo: 'erro', texto: 'As duas senhas novas não são iguais.' }
    return
  }
  enviando.value = true
  try {
    await sessao.trocarSenha(atual.value, nova.value)
    atual.value = nova.value = confirmacao.value = ''
    aviso.value = { tipo: 'sucesso', texto: 'Senha alterada. Se você estava logado em outro aparelho, será preciso entrar de novo lá.' }
  } catch (e) {
    aviso.value = { tipo: 'erro', texto: e.message }
  } finally {
    enviando.value = false
  }
}
</script>

<template>
  <h1 class="text-4xl font-semibold">Minha conta</h1>

  <section v-if="sessao.usuario" class="cartao mt-8 max-w-2xl">
    <dl class="grid gap-4 sm:grid-cols-3">
      <div>
        <dt class="text-xs uppercase tracking-wider text-stone-500">Nome</dt>
        <dd class="mt-1 font-medium">{{ sessao.usuario.nome }}</dd>
      </div>
      <div>
        <dt class="text-xs uppercase tracking-wider text-stone-500">Identificador</dt>
        <dd class="mt-1 font-mono font-medium">{{ sessao.usuario.codigo }}</dd>
      </div>
      <div>
        <dt class="text-xs uppercase tracking-wider text-stone-500">E-mail</dt>
        <dd class="mt-1 font-medium">{{ sessao.usuario.email }}</dd>
      </div>
    </dl>
  </section>

  <form class="cartao mt-6 max-w-2xl space-y-4" @submit.prevent="trocar">
    <h2 class="text-2xl font-semibold">Trocar senha</h2>
    <div>
      <label class="rotulo" for="atual">Senha atual</label>
      <input id="atual" v-model="atual" type="password" class="campo sm:w-1/2" required autocomplete="current-password" />
    </div>
    <div class="grid gap-4 sm:grid-cols-2">
      <div>
        <label class="rotulo" for="nova">Nova senha</label>
        <input id="nova" v-model="nova" type="password" class="campo" required minlength="8" autocomplete="new-password" />
      </div>
      <div>
        <label class="rotulo" for="confirmacao">Repita a nova senha</label>
        <input id="confirmacao" v-model="confirmacao" type="password" class="campo" required minlength="8" autocomplete="new-password" />
      </div>
    </div>
    <Mensagem v-if="aviso" :tipo="aviso.tipo">{{ aviso.texto }}</Mensagem>
    <button class="btn-primario" :disabled="enviando">Trocar senha</button>
  </form>
</template>
