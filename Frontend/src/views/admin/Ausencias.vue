<script setup>
import { inject, onMounted, ref } from 'vue'
import { admin } from '../../api/admin'
import Mensagem from '../../components/Mensagem.vue'
import Modal from '../../components/Modal.vue'
import { MOTIVOS_DE_AUSENCIA, formatarData, formatarHora, paraDataIso, somarDias, hoje } from '../../dominio/formatos'

const atualizarAlertas = inject('atualizarAlertas')
const ausencias = ref([])
const profissionais = ref([])
const nova = ref(null)
const erro = ref('')
const salvando = ref(false)
const conflitos = ref(null)

async function carregar() {
  ;[ausencias.value, profissionais.value] = await Promise.all([admin.ausencias(), admin.profissionais()])
}
onMounted(carregar)

const nomeDoMotivo = (valor) => MOTIVOS_DE_AUSENCIA.find((m) => m.valor === valor)?.nome ?? valor

function abrir() {
  erro.value = ''
  const amanha = paraDataIso(somarDias(hoje(), 1))
  nova.value = { profissionalId: '', diaInteiro: true, dataInicio: amanha, dataFim: amanha, horaInicio: '09:00', horaFim: '12:00', motivo: 'Ferias', descricao: '' }
}

async function salvar() {
  const n = nova.value
  // Dia inteiro: do início do primeiro dia até o início do dia seguinte ao último.
  const inicio = n.diaInteiro ? `${n.dataInicio}T00:00:00` : `${n.dataInicio}T${n.horaInicio}:00`
  const fim = n.diaInteiro
    ? `${paraDataIso(somarDias(new Date(`${n.dataFim}T00:00`), 1))}T00:00:00`
    : `${n.dataInicio}T${n.horaFim}:00`

  salvando.value = true
  erro.value = ''
  try {
    const resposta = await admin.criarAusencia({
      profissionalId: n.profissionalId || null, inicio, fim, motivo: n.motivo, descricao: n.descricao || null,
    })
    nova.value = null
    conflitos.value = resposta.conflitos
    await carregar()
    atualizarAlertas()
  } catch (e) {
    erro.value = e.message
  } finally {
    salvando.value = false
  }
}

async function remover(a) {
  if (!confirm('Remover esta ausência?')) return
  await admin.removerAusencia(a.id)
  await carregar()
  atualizarAlertas()
}

// Mostra "10/10 a 25/10" para dias inteiros, ou "10/10 das 15:00 às 17:00".
function periodo(a) {
  const ehDiaInteiro = formatarHora(a.inicio) === '00:00' && formatarHora(a.fim) === '00:00'
  if (ehDiaInteiro) {
    const ultimo = paraDataIso(somarDias(new Date(`${a.fim.slice(0, 10)}T00:00`), -1))
    const de = formatarData(a.inicio)
    const ate = formatarData(`${ultimo}T00:00:00`)
    return de === ate ? `${de} (dia inteiro)` : `${de} até ${ate}`
  }
  return `${formatarData(a.inicio)} das ${formatarHora(a.inicio)} às ${formatarHora(a.fim)}`
}
</script>

<template>
  <div class="flex flex-wrap items-end justify-between gap-4">
    <div>
      <h1 class="text-4xl font-semibold">Ausências</h1>
      <p class="mt-1 text-stone-500">Férias, feriados e saídas. Nesses períodos a agenda não oferece horários.</p>
    </div>
    <button class="btn-primario" @click="abrir">Nova ausência</button>
  </div>

  <Mensagem v-if="conflitos?.length" tipo="aviso" class="mt-6">
    Ausência salva. {{ conflitos.length }} agendamento(s) ficaram em conflito:
    <span v-for="(c, i) in conflitos" :key="c.agendamento.id">
      {{ i ? ', ' : '' }}{{ c.agendamento.clienteNome }} ({{ formatarData(c.agendamento.inicio) }} {{ formatarHora(c.agendamento.inicio) }})
    </span>.
    <RouterLink :to="{ name: 'alertas' }" class="font-medium underline">Resolver nos alertas</RouterLink>
  </Mensagem>
  <Mensagem v-else-if="conflitos" tipo="sucesso" class="mt-6">Ausência salva. Nenhum agendamento foi afetado.</Mensagem>

  <p v-if="!ausencias.length" class="mt-8 rounded-2xl border border-dashed border-stone-300 px-6 py-10 text-center text-stone-500">
    Nenhuma ausência futura cadastrada.
  </p>
  <ul class="mt-6 space-y-3">
    <li v-for="a in ausencias" :key="a.id" class="cartao flex flex-wrap items-center justify-between gap-3">
      <div>
        <p class="font-medium">{{ a.profissionalNome }} · {{ nomeDoMotivo(a.motivo) }}</p>
        <p class="text-sm first-letter:uppercase text-stone-600">{{ periodo(a) }}</p>
        <p v-if="a.descricao" class="text-sm text-stone-500">{{ a.descricao }}</p>
      </div>
      <button class="btn-perigo btn-pequeno" @click="remover(a)">Remover</button>
    </li>
  </ul>

  <Modal titulo="Nova ausência" :aberto="!!nova" @fechar="nova = null">
    <form v-if="nova" class="space-y-4" @submit.prevent="salvar">
      <div>
        <label class="rotulo">Quem</label>
        <select v-model="nova.profissionalId" class="campo">
          <option value="">Clínica toda (ex.: feriado)</option>
          <option v-for="p in profissionais" :key="p.id" :value="p.id">{{ p.nome }}</option>
        </select>
      </div>
      <div class="grid grid-cols-2 gap-3">
        <div>
          <label class="rotulo">Motivo</label>
          <select v-model="nova.motivo" class="campo">
            <option v-for="m in MOTIVOS_DE_AUSENCIA" :key="m.valor" :value="m.valor">{{ m.nome }}</option>
          </select>
        </div>
        <label class="flex items-center gap-2 self-end pb-2.5 text-sm">
          <input v-model="nova.diaInteiro" type="checkbox" class="h-4 w-4 accent-marca-500" /> Dia(s) inteiro(s)
        </label>
      </div>
      <div v-if="nova.diaInteiro" class="grid grid-cols-2 gap-3">
        <div><label class="rotulo">De</label><input v-model="nova.dataInicio" type="date" class="campo" required /></div>
        <div><label class="rotulo">Até</label><input v-model="nova.dataFim" type="date" class="campo" :min="nova.dataInicio" required /></div>
      </div>
      <div v-else class="grid grid-cols-3 gap-3">
        <div class="col-span-3"><label class="rotulo">Dia</label><input v-model="nova.dataInicio" type="date" class="campo" required /></div>
        <div><label class="rotulo">Das</label><input v-model="nova.horaInicio" type="time" step="900" class="campo" required /></div>
        <div><label class="rotulo">Às</label><input v-model="nova.horaFim" type="time" step="900" class="campo" required /></div>
      </div>
      <div>
        <label class="rotulo">Descrição <span class="font-normal text-stone-400">(opcional)</span></label>
        <input v-model="nova.descricao" class="campo" maxlength="200" placeholder="Ex.: Natal, consulta médica…" />
      </div>
      <Mensagem v-if="erro">{{ erro }}</Mensagem>
      <button class="btn-primario w-full" :disabled="salvando">Salvar</button>
    </form>
  </Modal>
</template>
