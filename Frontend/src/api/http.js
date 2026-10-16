// Camada de dados: o único lugar que sabe falar HTTP com o back-end.
// As telas chamam funções como `publico.listarServicos()` e não sabem que existe fetch.
import { apagarToken, lerToken } from '../dominio/token'

export class ErroDaApi extends Error {
  constructor(mensagem, status) {
    super(mensagem)
    this.status = status
  }
}

export async function requisicao(metodo, url, corpo) {
  const opcoes = { method: metodo, headers: {} }

  // Se há alguém logado, todo pedido leva o token: "Authorization: Bearer <token>".
  const token = lerToken()
  if (token) opcoes.headers.Authorization = `Bearer ${token}`

  if (corpo instanceof FormData) {
    opcoes.body = corpo
  } else if (corpo !== undefined) {
    opcoes.headers['Content-Type'] = 'application/json'
    opcoes.body = JSON.stringify(corpo)
  }

  const resposta = await fetch(url, opcoes)

  if (resposta.status === 204) return null

  const dados = await resposta.json().catch(() => null)

  // Token vencido ou sessão encerrada (senha trocada, usuário desativado): volta para o login.
  if (resposta.status === 401 && token) {
    apagarToken()
    if (!location.pathname.startsWith('/admin/login')) location.href = '/admin/login?expirou=1'
  }

  if (!resposta.ok) {
    throw new ErroDaApi(mensagemDeErro(dados, resposta.status), resposta.status)
  }

  return dados
}

function mensagemDeErro(dados, status) {
  if (status === 429) return 'Muitas tentativas seguidas. Aguarde um minuto e tente de novo.'
  if (dados?.errors) return Object.values(dados.errors).flat().join(' ')
  return dados?.title ?? 'Não foi possível concluir. Tente novamente.'
}

export const get = (url) => requisicao('GET', url)
export const post = (url, corpo) => requisicao('POST', url, corpo)
export const put = (url, corpo) => requisicao('PUT', url, corpo)
export const del = (url) => requisicao('DELETE', url)

export function query(parametros) {
  const busca = new URLSearchParams()
  for (const [chave, valor] of Object.entries(parametros)) {
    if (valor !== undefined && valor !== null && valor !== '') busca.set(chave, valor)
  }
  return busca.toString()
}
