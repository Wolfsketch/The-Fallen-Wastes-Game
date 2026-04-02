import { createApp } from 'vue'
import { createRouter, createWebHashHistory } from 'vue-router'
import DocsApp from './DocsApp.vue'
import './docs.css'

import DocsGettingStarted from './pages/DocsGettingStarted.vue'
import DocsBuildings      from './pages/DocsBuildings.vue'
import DocsResources      from './pages/DocsResources.vue'
import DocsPopulation     from './pages/DocsPopulation.vue'
import DocsUnits          from './pages/DocsUnits.vue'
import DocsCombat         from './pages/DocsCombat.vue'
import DocsOperations     from './pages/DocsOperations.vue'
import DocsResearch       from './pages/DocsResearch.vue'
import DocsSalvage        from './pages/DocsSalvage.vue'
import DocsWasteland      from './pages/DocsWasteland.vue'
import DocsAlliances      from './pages/DocsAlliances.vue'

const routes = [
  { path: '/',                redirect: '/introduction' },
  { path: '/introduction',   component: DocsGettingStarted },
  { path: '/buildings',      component: DocsBuildings },
  { path: '/resources',      component: DocsResources },
  { path: '/population',     component: DocsPopulation },
  { path: '/units',          component: DocsUnits },
  { path: '/combat',         component: DocsCombat },
  { path: '/operations',     component: DocsOperations },
  { path: '/research',       component: DocsResearch },
  { path: '/salvage',        component: DocsSalvage },
  { path: '/wasteland',      component: DocsWasteland },
  { path: '/alliances',      component: DocsAlliances },
]

const router = createRouter({
  history: createWebHashHistory(),
  routes,
  scrollBehavior(to, _from, savedPosition) {
    if (savedPosition) return savedPosition
    if (to.hash) return { el: to.hash, behavior: 'smooth', top: 70 }
    return { top: 0, behavior: 'smooth' }
  },
})

createApp(DocsApp).use(router).mount('#docs-app')
