import { reactive } from 'vue'
import { auth } from '../api/admin'
import { apagarToken, lerToken, salvarToken } from './token'

// Quem está logado no painel, compartilhado entre as telas.
export const sessao = reactive({
  usuario: null, // { id, codigo, nome, email }
  verificado: false,

  async estaLogado() {
    if (!lerToken()) {
      this.usuario = null
      return false
    }
    if (!this.verificado) {
      try {
        this.usuario = await auth.eu()
      } catch {
        this.usuario = null
      }
      this.verificado = true
    }
    return !!this.usuario
  },

  /** Recebe a resposta de login/primeiro acesso/troca de senha: { token, expiraEmUtc, usuario }. */
  iniciar(resposta) {
    salvarToken(resposta.token, resposta.expiraEmUtc)
    this.usuario = resposta.usuario
    this.verificado = true
  },

  async entrar(login, senha) {
    this.iniciar(await auth.login(login, senha))
  },

  async primeiroAcesso(codigo, codigoDeAcesso, novaSenha) {
    this.iniciar(await auth.primeiroAcesso(codigo, codigoDeAcesso, novaSenha))
  },

  async trocarSenha(senhaAtual, novaSenha) {
    // A API devolve um token novo: o antigo deixa de valer depois da troca.
    this.iniciar(await auth.trocarSenha(senhaAtual, novaSenha))
  },

  sair() {
    apagarToken()
    this.usuario = null
    this.verificado = false
  },
})
