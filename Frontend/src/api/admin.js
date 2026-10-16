import { del, get, post, put, query, requisicao } from './http'

export const auth = {
  login: (login, senha) => post('/api/auth/login', { login, senha }),
  primeiroAcesso: (codigo, codigoDeAcesso, novaSenha) => post('/api/auth/primeiro-acesso', { codigo, codigoDeAcesso, novaSenha }),
  eu: () => get('/api/auth/eu'),
  trocarSenha: (senhaAtual, novaSenha) => post('/api/auth/senha', { senhaAtual, novaSenha }),
}

const base = '/api/admin'

export const admin = {
  // Agendamentos
  agendamentos: (filtro) => get(`${base}/agendamentos?${query(filtro)}`),
  confirmar: (id) => post(`${base}/agendamentos/${id}/confirmar`),
  cancelar: (id) => post(`${base}/agendamentos/${id}/cancelar`),
  remarcar: (id, novoInicio) => post(`${base}/agendamentos/${id}/remarcar`, { novoInicio }),
  trocarProfissional: (id, profissionalId) => post(`${base}/agendamentos/${id}/trocar-profissional`, { profissionalId }),
  profissionaisParaTroca: (id) => get(`${base}/agendamentos/${id}/profissionais-disponiveis`),
  alertas: () => get(`${base}/alertas`),

  // Serviços
  servicos: () => get(`${base}/servicos`),
  criarServico: (dados) => post(`${base}/servicos`, dados),
  atualizarServico: (id, dados) => put(`${base}/servicos/${id}`, dados),
  enviarFoto: (id, arquivo) => {
    const formulario = new FormData()
    formulario.append('arquivo', arquivo)
    return requisicao('POST', `${base}/servicos/${id}/fotos`, formulario)
  },
  removerFoto: (id, caminho) => del(`${base}/servicos/${id}/fotos?${query({ caminho })}`),

  // Profissionais
  profissionais: () => get(`${base}/profissionais`),
  criarProfissional: (dados) => post(`${base}/profissionais`, dados),
  atualizarProfissional: (id, dados) => put(`${base}/profissionais/${id}`, dados),

  // Ausências
  ausencias: (filtro = {}) => get(`${base}/ausencias?${query(filtro)}`),
  criarAusencia: (dados) => post(`${base}/ausencias`, dados),
  removerAusencia: (id) => del(`${base}/ausencias/${id}`),

  // Usuários
  usuarios: () => get(`${base}/usuarios`),
  criarUsuario: (dados) => post(`${base}/usuarios`, dados),
  atualizarUsuario: (id, dados) => put(`${base}/usuarios/${id}`, dados),
  novoAcesso: (id) => post(`${base}/usuarios/${id}/novo-acesso`),

  // Histórico
  historico: (filtro) => get(`${base}/historico?${query(filtro)}`),

  // Configuração
  configuracao: () => get(`${base}/configuracao`),
  salvarConfiguracao: (dados) => put(`${base}/configuracao`, dados),
}
