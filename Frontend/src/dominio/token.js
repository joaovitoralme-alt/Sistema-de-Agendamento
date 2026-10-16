// Guarda o token JWT do painel no navegador.
// localStorage mantém o login ao fechar a aba; o token vence sozinho (padrão: 10 horas).
const CHAVE = 'clinica_token'

export function lerToken() {
  try {
    const salvo = JSON.parse(localStorage.getItem(CHAVE))
    if (!salvo || new Date(salvo.expiraEmUtc) <= new Date()) return null
    return salvo.token
  } catch {
    return null
  }
}

export function salvarToken(token, expiraEmUtc) {
  // A API manda a data em UTC sem o "Z"; acrescentamos para o navegador entender certo.
  const expira = expiraEmUtc.endsWith('Z') ? expiraEmUtc : `${expiraEmUtc}Z`
  localStorage.setItem(CHAVE, JSON.stringify({ token, expiraEmUtc: expira }))
}

export function apagarToken() {
  localStorage.removeItem(CHAVE)
}
