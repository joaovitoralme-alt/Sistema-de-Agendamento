// Regras de apresentação usadas em várias telas: nomes dos dias, status, datas.

export const DIAS_DA_SEMANA = [
  { valor: 'Monday', nome: 'Segunda', curto: 'Seg' },
  { valor: 'Tuesday', nome: 'Terça', curto: 'Ter' },
  { valor: 'Wednesday', nome: 'Quarta', curto: 'Qua' },
  { valor: 'Thursday', nome: 'Quinta', curto: 'Qui' },
  { valor: 'Friday', nome: 'Sexta', curto: 'Sex' },
  { valor: 'Saturday', nome: 'Sábado', curto: 'Sáb' },
  { valor: 'Sunday', nome: 'Domingo', curto: 'Dom' },
]

// Date.getDay(): 0 = domingo ... 6 = sábado
const NOME_POR_GETDAY = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']
export const diaDaSemanaDe = (data) => NOME_POR_GETDAY[data.getDay()]

export const STATUS = {
  Pendente: { nome: 'Pendente', classe: 'bg-amber-50 text-amber-800 ring-amber-200' },
  Confirmado: { nome: 'Confirmado', classe: 'bg-emerald-50 text-emerald-800 ring-emerald-200' },
  Cancelado: { nome: 'Cancelado', classe: 'bg-stone-100 text-stone-500 ring-stone-200' },
}

export const MOTIVOS_DE_AUSENCIA = [
  { valor: 'Ferias', nome: 'Férias' },
  { valor: 'Feriado', nome: 'Feriado' },
  { valor: 'Outro', nome: 'Outro' },
]

const doisDigitos = (n) => String(n).padStart(2, '0')

/** Data local no formato da API: 2026-10-05 */
export const paraDataIso = (data) =>
  `${data.getFullYear()}-${doisDigitos(data.getMonth() + 1)}-${doisDigitos(data.getDate())}`

/** Junta "2026-10-05" + "14:00" no formato que a API espera (horário da clínica, sem fuso). */
export const juntarDataEHora = (dataIso, hora) => `${dataIso}T${hora}:00`

/** Lê "2026-10-05T14:00:00" como horário local (sem converter fuso). */
export function lerDataHora(texto) {
  const [data, hora = '00:00'] = texto.split('T')
  const [a, m, d] = data.split('-').map(Number)
  const [h, min] = hora.split(':').map(Number)
  return new Date(a, m - 1, d, h, min)
}

export const formatarHora = (texto) => texto.slice(11, 16)

const maiuscula = (texto) => texto.charAt(0).toUpperCase() + texto.slice(1)

export const formatarData = (texto, opcoes = { weekday: 'short', day: '2-digit', month: 'short' }) =>
  maiuscula(lerDataHora(texto).toLocaleDateString('pt-BR', opcoes))

export const formatarDataLonga = (texto) =>
  formatarData(texto, { weekday: 'long', day: 'numeric', month: 'long' })

export const formatarDuracao = (minutos) => {
  const h = Math.floor(minutos / 60)
  const m = minutos % 60
  if (!h) return `${m} min`
  return m ? `${h}h${doisDigitos(m)}` : `${h}h`
}

export const somarDias = (data, dias) => {
  const nova = new Date(data)
  nova.setDate(nova.getDate() + dias)
  return nova
}

export const hoje = () => {
  const agora = new Date()
  return new Date(agora.getFullYear(), agora.getMonth(), agora.getDate())
}

export function formatarTelefone(digitos = '') {
  if (digitos.length === 11) return `(${digitos.slice(0, 2)}) ${digitos.slice(2, 7)}-${digitos.slice(7)}`
  if (digitos.length === 10) return `(${digitos.slice(0, 2)}) ${digitos.slice(2, 6)}-${digitos.slice(6)}`
  return digitos
}

export function linkDoWhatsApp(numero, mensagem = '') {
  if (!numero) return null
  let digitos = numero.replace(/\D/g, '')
  if (digitos.length <= 11) digitos = `55${digitos}`
  return `https://wa.me/${digitos}${mensagem ? `?text=${encodeURIComponent(mensagem)}` : ''}`
}

export function linkDoInstagram(usuario) {
  if (!usuario) return null
  if (usuario.startsWith('http')) return usuario
  return `https://instagram.com/${usuario.replace('@', '')}`
}
