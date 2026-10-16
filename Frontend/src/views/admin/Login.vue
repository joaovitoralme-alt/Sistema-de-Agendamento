<script setup>
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import LogoSimbolo from '../../components/LogoSimbolo.vue'
import Mensagem from '../../components/Mensagem.vue'
import { sessao } from '../../dominio/sessao'

const route = useRoute()
const router = useRouter()
const login = ref('')
const senha = ref('')
const erro = ref('')
const enviando = ref(false)

async function entrar() {
  enviando.value = true
  erro.value = ''
  try {
    await sessao.entrar(login.value, senha.value)
    router.replace(route.query.voltar || '/admin')
  } catch (e) {
    erro.value = e.message
  } finally {
    enviando.value = false
  }
}
</script>

<template>
  <div class="flex min-h-screen items-center justify-center bg-areia px-4">
    <form class="w-full max-w-sm rounded-3xl bg-white p-8 shadow-sm" @submit.prevent="entrar">
      <LogoSimbolo class="mb-5 h-12 w-auto text-marca-500" />
      <h1 class="text-3xl font-semibold">Área da clínica</h1>
      <p class="mt-1 text-sm text-stone-500">Entre para gerenciar a agenda.</p>

      <Mensagem v-if="route.query.expirou && !erro" tipo="aviso" class="mt-5">Sua sessão terminou. Entre novamente.</Mensagem>
      <Mensagem v-if="erro" class="mt-5">{{ erro }}</Mensagem>

      <div class="mt-6 space-y-4">
        <div>
          <label class="rotulo" for="login">Identificador ou e-mail</label>
          <input id="login" v-model="login" class="campo" required autocomplete="username" placeholder="@a123456@" />
        </div>
        <div>
          <label class="rotulo" for="senha">Senha</label>
          <input id="senha" v-model="senha" type="password" class="campo" required autocomplete="current-password" />
        </div>
      </div>
      <button class="btn-primario mt-6 w-full" :disabled="enviando">{{ enviando ? 'Entrando…' : 'Entrar' }}</button>

      <div class="mt-5 space-y-2 text-center text-sm">
        <RouterLink :to="{ name: 'primeiro-acesso' }" class="block font-medium text-marca-600 hover:underline">
          Primeiro acesso? Crie sua senha
        </RouterLink>
        <RouterLink to="/" class="block text-stone-500 hover:text-marca-600">Voltar ao site</RouterLink>
      </div>
    </form>
  </div>
</template>
