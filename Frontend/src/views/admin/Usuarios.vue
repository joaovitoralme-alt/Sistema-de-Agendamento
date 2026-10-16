<script setup>
import { onMounted, ref } from 'vue'
import { admin } from '../../api/admin'
import Mensagem from '../../components/Mensagem.vue'
import Modal from '../../components/Modal.vue'
import { formatarData, formatarHora } from '../../dominio/formatos'
import { sessao } from '../../dominio/sessao'

const usuarios = ref([])
const editando = ref(null) // { id?, nome, email, ativo }
const acessoGerado = ref(null) // { usuario, codigoDeAcesso, expiraEm }
const erro = ref('')
const salvando = ref(false)
const copiado = ref(false)

const carregar = async () => (usuarios.value = await admin.usuarios())
onMounted(carregar)

function novo() {
  erro.value = ''
  editando.value = { nome: '', email: '', ativo: true }
}

function editar(u) {
  erro.value = ''
  editando.value = { id: u.id, nome: u.nome, email: u.email, ativo: u.ativo, codigo: u.codigo }
}

async function salvar() {
  salvando.value = true
  erro.value = ''
  const { id, nome, email, ativo } = editando.value
  try {
    if (id) {
      await admin.atualizarUsuario(id, { nome, email, ativo })
    } else {
      mostrarAcesso(await admin.criarUsuario({ nome, email, ativo }))
    }
    editando.value = null
    await carregar()
  } catch (e) {
    erro.value = e.message
  } finally {
    salvando.value = false
  }
}

async function gerarNovoAcesso(u) {
  if (!confirm(`Gerar um novo código de acesso para ${u.nome}? A senha atual dela deixa de funcionar.`)) return
  try {
    mostrarAcesso(await admin.novoAcesso(u.id))
    editando.value = null
    await carregar()
  } catch (e) {
    erro.value = e.message
  }
}

function mostrarAcesso(resposta) {
  copiado.value = false
  copiaManual.value = ''
  acessoGerado.value = resposta
}

const mensagemDeAcesso = (a) =>
  `Olá, ${a.usuario.nome.split(' ')[0]}! Seu acesso ao painel da clínica:\n` +
  `Endereço: ${location.origin}/admin/primeiro-acesso\n` +
  `Identificador: ${a.usuario.codigo}\n` +
  `Código de acesso: ${a.codigoDeAcesso}\n` +
  `(o código vale até ${formatarData(a.expiraEm)})`

const copiaManual = ref('')

async function copiar() {
  try {
    await navigator.clipboard.writeText(mensagemDeAcesso(acessoGerado.value))
    copiado.value = true
  } catch {
    // Fora de HTTPS o navegador bloqueia a cópia automática: mostra o texto para copiar à mão.
    copiaManual.value = mensagemDeAcesso(acessoGerado.value)
  }
}

const dataHora = (texto) => (texto ? `${formatarData(texto)} ${formatarHora(texto)}` : '—')
</script>

<template>
  <div class="flex flex-wrap items-end justify-between gap-4">
    <div>
      <h1 class="text-4xl font-semibold">Usuários</h1>
      <p class="mt-1 text-stone-500">Quem pode entrar no painel. Tudo o que cada um faz fica no Histórico.</p>
    </div>
    <button class="btn-primario" @click="novo">Novo usuário</button>
  </div>

  <Mensagem v-if="erro && !editando" class="mt-6">{{ erro }}</Mensagem>

  <div class="mt-8 overflow-x-auto rounded-2xl border border-stone-200 bg-white">
    <table class="w-full min-w-[640px] text-left text-sm">
      <thead class="border-b border-stone-200 text-xs uppercase tracking-wider text-stone-500">
        <tr>
          <th class="px-4 py-3 font-medium">Nome</th>
          <th class="px-4 py-3 font-medium">Identificador</th>
          <th class="px-4 py-3 font-medium">E-mail</th>
          <th class="px-4 py-3 font-medium">Situação</th>
          <th class="px-4 py-3 font-medium">Último acesso</th>
          <th class="px-4 py-3 font-medium">Criado em</th>
        </tr>
      </thead>
      <tbody class="divide-y divide-stone-100">
        <tr v-for="u in usuarios" :key="u.id" class="cursor-pointer hover:bg-marca-50/50" @click="editar(u)">
          <td class="px-4 py-3 font-medium">
            {{ u.nome }}
            <span v-if="u.id === sessao.usuario?.id" class="ml-1 text-xs font-normal text-stone-400">(você)</span>
          </td>
          <td class="px-4 py-3 font-mono">{{ u.codigo }}</td>
          <td class="px-4 py-3 text-stone-600">{{ u.email }}</td>
          <td class="px-4 py-3">
            <span v-if="!u.ativo" class="rounded-full bg-stone-100 px-2.5 py-0.5 text-xs text-stone-500">Inativo</span>
            <span v-else-if="u.aguardandoPrimeiroAcesso" class="rounded-full bg-amber-50 px-2.5 py-0.5 text-xs text-amber-800">Aguardando 1º acesso</span>
            <span v-else class="rounded-full bg-emerald-50 px-2.5 py-0.5 text-xs text-emerald-800">Ativo</span>
          </td>
          <td class="px-4 py-3 text-stone-600">{{ dataHora(u.ultimoAcessoEm) }}</td>
          <td class="px-4 py-3 text-stone-600">{{ formatarData(u.criadoEm) }}</td>
        </tr>
      </tbody>
    </table>
  </div>

  <!-- Cadastro / edição -->
  <Modal :titulo="editando?.id ? 'Editar usuário' : 'Novo usuário'" :aberto="!!editando" @fechar="editando = null">
    <form v-if="editando" class="space-y-4" @submit.prevent="salvar">
      <p v-if="editando.codigo" class="text-sm text-stone-500">Identificador: <span class="font-mono">{{ editando.codigo }}</span></p>
      <div>
        <label class="rotulo">Nome</label>
        <input v-model="editando.nome" class="campo" required maxlength="120" />
      </div>
      <div>
        <label class="rotulo">E-mail</label>
        <input v-model="editando.email" type="email" class="campo" required maxlength="200" />
      </div>
      <label class="flex items-center gap-3 text-sm">
        <input v-model="editando.ativo" type="checkbox" class="h-4 w-4 accent-marca-500" /> Ativo (pode entrar no painel)
      </label>
      <p v-if="!editando.id" class="text-sm text-stone-500">
        O identificador é gerado automaticamente. Depois de salvar, você recebe um código para a pessoa criar a própria senha.
      </p>
      <Mensagem v-if="erro">{{ erro }}</Mensagem>
      <div class="flex flex-wrap gap-2">
        <button class="btn-primario" :disabled="salvando">{{ salvando ? 'Salvando…' : 'Salvar' }}</button>
        <button v-if="editando.id && editando.id !== sessao.usuario?.id" type="button" class="btn-secundario"
          @click="gerarNovoAcesso(editando)">Gerar novo acesso (esqueceu a senha)</button>
      </div>
    </form>
  </Modal>

  <!-- Código de acesso (aparece uma vez só) -->
  <Modal titulo="Acesso criado" :aberto="!!acessoGerado" @fechar="acessoGerado = null">
    <div v-if="acessoGerado" class="space-y-4">
      <p class="text-sm text-stone-600">
        Envie estes dados para <strong>{{ acessoGerado.usuario.nome }}</strong>. Por segurança, o código de acesso
        <strong>não aparece de novo</strong>. Se perder, gere um novo.
      </p>
      <dl class="space-y-3 rounded-2xl bg-areia p-5">
        <div>
          <dt class="text-xs uppercase tracking-wider text-stone-500">Identificador</dt>
          <dd class="font-mono text-lg font-semibold">{{ acessoGerado.usuario.codigo }}</dd>
        </div>
        <div>
          <dt class="text-xs uppercase tracking-wider text-stone-500">Código de acesso</dt>
          <dd class="font-mono text-2xl font-semibold tracking-[0.3em] text-marca-600">{{ acessoGerado.codigoDeAcesso }}</dd>
        </div>
        <div>
          <dt class="text-xs uppercase tracking-wider text-stone-500">Vale até</dt>
          <dd>{{ dataHora(acessoGerado.expiraEm) }}</dd>
        </div>
      </dl>
      <p class="text-sm text-stone-600">A pessoa entra em <span class="font-mono">/admin/primeiro-acesso</span> e cria a própria senha.</p>
      <button class="btn-primario w-full" @click="copiar">{{ copiado ? 'Copiado!' : 'Copiar mensagem para enviar' }}</button>
      <textarea v-if="copiaManual" class="campo font-mono text-xs" rows="5" readonly :value="copiaManual" @focus="(e) => e.target.select()" />
    </div>
  </Modal>
</template>
