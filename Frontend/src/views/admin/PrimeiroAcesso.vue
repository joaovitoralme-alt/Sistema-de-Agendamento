<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import LogoSimbolo from '../../components/LogoSimbolo.vue'
import Mensagem from '../../components/Mensagem.vue'
import { sessao } from '../../dominio/sessao'

const router = useRouter()
const codigo = ref('')
const codigoDeAcesso = ref('')
const senha = ref('')
const confirmacao = ref('')
const erro = ref('')
const enviando = ref(false)

async function criarSenha() {
  erro.value = ''
  if (senha.value !== confirmacao.value) {
    erro.value = 'As duas senhas não são iguais.'
    return
  }
  enviando.value = true
  try {
    await sessao.primeiroAcesso(codigo.value, codigoDeAcesso.value, senha.value)
    router.replace('/admin')
  } catch (e) {
    erro.value = e.message
  } finally {
    enviando.value = false
  }
}
</script>

<template>
  <div class="flex min-h-screen items-center justify-center bg-areia px-4 py-10">
    <form class="w-full max-w-sm rounded-3xl bg-white p-8 shadow-sm" @submit.prevent="criarSenha">
      <LogoSimbolo class="mb-5 h-12 w-auto text-marca-500" />
      <h1 class="text-3xl font-semibold">Primeiro acesso</h1>
      <p class="mt-1 text-sm text-stone-500">
        Use o identificador e o código de acesso que você recebeu do administrador para criar a sua senha.
      </p>

      <Mensagem v-if="erro" class="mt-5">{{ erro }}</Mensagem>

      <div class="mt-6 space-y-4">
        <div>
          <label class="rotulo" for="codigo">Identificador</label>
          <input id="codigo" v-model="codigo" class="campo" required placeholder="@a123456@" autocomplete="username" />
        </div>
        <div>
          <label class="rotulo" for="acesso">Código de acesso</label>
          <input id="acesso" v-model="codigoDeAcesso" class="campo uppercase tracking-widest" required maxlength="8"
            autocomplete="one-time-code" placeholder="ABCD2345" />
        </div>
        <div>
          <label class="rotulo" for="senha">Nova senha</label>
          <input id="senha" v-model="senha" type="password" class="campo" required minlength="8" autocomplete="new-password" />
          <p class="mt-1 text-xs text-stone-500">Mínimo de 8 caracteres.</p>
        </div>
        <div>
          <label class="rotulo" for="confirmacao">Repita a senha</label>
          <input id="confirmacao" v-model="confirmacao" type="password" class="campo" required minlength="8" autocomplete="new-password" />
        </div>
      </div>
      <button class="btn-primario mt-6 w-full" :disabled="enviando">{{ enviando ? 'Salvando…' : 'Criar senha e entrar' }}</button>
      <RouterLink :to="{ name: 'login' }" class="mt-4 block text-center text-sm text-stone-500 hover:text-marca-600">
        Já tenho senha
      </RouterLink>
    </form>
  </div>
</template>
