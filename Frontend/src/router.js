import { createRouter, createWebHistory } from 'vue-router'
import { sessao } from './dominio/sessao'

// As telas do painel só são baixadas quando alguém entra nelas (import dinâmico).
export const router = createRouter({
  history: createWebHistory(),
  scrollBehavior: (to) => (to.hash ? { el: to.hash, behavior: 'smooth' } : { top: 0 }),
  routes: [
    {
      path: '/',
      component: () => import('./views/publico/LayoutPublico.vue'),
      children: [
        { path: '', name: 'inicio', component: () => import('./views/publico/Inicio.vue') },
        { path: 'agendar', name: 'agendar', component: () => import('./views/publico/Agendar.vue') },
      ],
    },
    { path: '/admin/login', name: 'login', component: () => import('./views/admin/Login.vue') },
    { path: '/admin/primeiro-acesso', name: 'primeiro-acesso', component: () => import('./views/admin/PrimeiroAcesso.vue') },
    {
      path: '/admin',
      component: () => import('./views/admin/LayoutAdmin.vue'),
      meta: { requerLogin: true },
      children: [
        { path: '', redirect: { name: 'agenda' } },
        { path: 'agenda', name: 'agenda', component: () => import('./views/admin/Agenda.vue') },
        { path: 'alertas', name: 'alertas', component: () => import('./views/admin/Alertas.vue') },
        { path: 'servicos', name: 'servicos', component: () => import('./views/admin/Servicos.vue') },
        { path: 'profissionais', name: 'profissionais', component: () => import('./views/admin/Profissionais.vue') },
        { path: 'ausencias', name: 'ausencias', component: () => import('./views/admin/Ausencias.vue') },
        { path: 'configuracoes', name: 'configuracoes', component: () => import('./views/admin/Configuracoes.vue') },
        { path: 'usuarios', name: 'usuarios', component: () => import('./views/admin/Usuarios.vue') },
        { path: 'historico', name: 'historico', component: () => import('./views/admin/Historico.vue') },
        { path: 'minha-conta', name: 'minha-conta', component: () => import('./views/admin/MinhaConta.vue') },
      ],
    },
    { path: '/:caminho(.*)*', redirect: '/' },
  ],
})

router.beforeEach(async (to) => {
  if (to.matched.some((r) => r.meta.requerLogin) && !(await sessao.estaLogado())) {
    return { name: 'login', query: { voltar: to.fullPath } }
  }
})
