import { get, post, query } from './http'

export const publico = {
  clinica: () => get('/api/clinica'),
  listarServicos: () => get('/api/servicos'),
  obterServico: (id) => get(`/api/servicos/${id}`),
  horarios: (servicoId, data) => get(`/api/disponibilidade/horarios?${query({ servicoId, data })}`),
  profissionais: (servicoId, inicio) => get(`/api/disponibilidade/profissionais?${query({ servicoId, inicio })}`),
  agendar: (dados) => post('/api/agendamentos', dados),
}
