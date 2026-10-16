<script setup>
import { onMounted, ref } from 'vue'
import { admin } from '../../api/admin'
import FotoDoServico from '../../components/FotoDoServico.vue'
import Mensagem from '../../components/Mensagem.vue'
import Modal from '../../components/Modal.vue'
import { formatarDuracao } from '../../dominio/formatos'

const servicos = ref([])
const editando = ref(null) // { id?, nome, descricao, duracaoEmMinutos, ativo, fotos }
const erro = ref('')
const salvando = ref(false)

// Durações possíveis: 15 min até 4 h, de 15 em 15.
const duracoes = Array.from({ length: 16 }, (_, i) => (i + 1) * 15)

const carregar = async () => (servicos.value = await admin.servicos())
onMounted(carregar)

function novo() {
  erro.value = ''
  editando.value = { nome: '', descricao: '', duracaoEmMinutos: 60, ativo: true, fotos: [] }
}

function editar(s) {
  erro.value = ''
  editando.value = { ...s, fotos: [...s.fotos] }
}

async function salvar() {
  salvando.value = true
  erro.value = ''
  const { id, nome, descricao, duracaoEmMinutos, ativo } = editando.value
  try {
    const salvo = id
      ? await admin.atualizarServico(id, { nome, descricao, duracaoEmMinutos, ativo })
      : await admin.criarServico({ nome, descricao, duracaoEmMinutos, ativo })
    editando.value = { ...salvo }
    await carregar()
  } catch (e) {
    erro.value = e.message
  } finally {
    salvando.value = false
  }
}

async function enviarFotos(evento) {
  erro.value = ''
  for (const arquivo of evento.target.files) {
    try {
      editando.value = { ...editando.value, fotos: (await admin.enviarFoto(editando.value.id, arquivo)).fotos }
    } catch (e) {
      erro.value = e.message
    }
  }
  evento.target.value = ''
  carregar()
}

async function removerFoto(caminho) {
  editando.value = { ...editando.value, fotos: (await admin.removerFoto(editando.value.id, caminho)).fotos }
  carregar()
}
</script>

<template>
  <div class="flex flex-wrap items-end justify-between gap-4">
    <div>
      <h1 class="text-4xl font-semibold">Procedimentos</h1>
      <p class="mt-1 text-stone-500">O que aparece no site. Na agenda, cada um bloqueia a duração + 15 min de folga.</p>
    </div>
    <button class="btn-primario" @click="novo">Novo procedimento</button>
  </div>

  <ul class="mt-8 grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
    <li v-for="s in servicos" :key="s.id" class="cartao flex cursor-pointer gap-4 p-3 transition hover:shadow-md"
      :class="{ 'opacity-50': !s.ativo }" @click="editar(s)">
      <FotoDoServico :servico="s" class="h-20 w-20 shrink-0 rounded-xl" />
      <div class="min-w-0">
        <p class="truncate font-titulo text-xl font-semibold">{{ s.nome }}</p>
        <p class="text-sm text-stone-500">{{ formatarDuracao(s.duracaoEmMinutos) }} · {{ s.fotos.length }} foto(s)</p>
        <p v-if="!s.ativo" class="text-xs font-medium text-stone-500">Oculto no site</p>
      </div>
    </li>
  </ul>

  <Modal :titulo="editando?.id ? 'Editar procedimento' : 'Novo procedimento'" :aberto="!!editando" largo @fechar="editando = null">
    <form v-if="editando" class="space-y-4" @submit.prevent="salvar">
      <div>
        <label class="rotulo">Nome</label>
        <input v-model="editando.nome" class="campo" required maxlength="100" />
      </div>
      <div>
        <label class="rotulo">Como funciona</label>
        <textarea v-model="editando.descricao" class="campo" rows="5" maxlength="4000" />
      </div>
      <div class="grid gap-4 sm:grid-cols-2">
        <div>
          <label class="rotulo">Duração do procedimento</label>
          <select v-model.number="editando.duracaoEmMinutos" class="campo">
            <option v-for="d in duracoes" :key="d" :value="d">{{ formatarDuracao(d) }}</option>
          </select>
          <p class="mt-1 text-xs text-stone-500">Ocupa {{ formatarDuracao(editando.duracaoEmMinutos + 15) }} na agenda.</p>
        </div>
        <label class="flex items-center gap-3 self-center text-sm">
          <input v-model="editando.ativo" type="checkbox" class="h-4 w-4 accent-marca-500" />
          Mostrar no site
        </label>
      </div>

      <div v-if="editando.id">
        <p class="rotulo">Fotos</p>
        <div class="flex flex-wrap gap-3">
          <div v-for="foto in editando.fotos" :key="foto" class="relative">
            <img :src="foto" class="h-24 w-24 rounded-xl object-cover" alt="" />
            <button type="button" class="absolute -right-2 -top-2 rounded-full bg-white px-2 text-sm shadow ring-1 ring-stone-200 hover:text-red-600"
              aria-label="Remover foto" @click="removerFoto(foto)">×</button>
          </div>
          <label class="flex h-24 w-24 cursor-pointer items-center justify-center rounded-xl border-2 border-dashed border-stone-300 text-center text-xs text-stone-500 hover:border-marca-300">
            + Foto
            <input type="file" accept="image/jpeg,image/png,image/webp" multiple class="hidden" @change="enviarFotos" />
          </label>
        </div>
        <p class="mt-1 text-xs text-stone-500">JPG, PNG ou WEBP de até 5 MB. A primeira foto é a capa.</p>
      </div>
      <p v-else class="text-sm text-stone-500">Salve o procedimento para adicionar fotos.</p>

      <Mensagem v-if="erro">{{ erro }}</Mensagem>
      <button class="btn-primario w-full sm:w-auto" :disabled="salvando">{{ salvando ? 'Salvando…' : 'Salvar' }}</button>
    </form>
  </Modal>
</template>
