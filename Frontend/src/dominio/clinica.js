import { ref } from 'vue'
import { publico } from '../api/publico'

// Dados da clínica (nome, WhatsApp, regras), carregados uma vez e reaproveitados.
const clinica = ref(null)
let carregando = null

export function useClinica() {
  if (!clinica.value && !carregando) {
    carregando = publico
      .clinica()
      .then((dados) => {
        clinica.value = dados
        document.title = dados.nomeDaClinica
      })
      .catch(() => {})
      .finally(() => (carregando = null))
  }
  return clinica
}

export function recarregarClinica() {
  clinica.value = null
  return useClinica()
}
