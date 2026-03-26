import { createRouter, createWebHistory } from 'vue-router'
import LoginView from '../views/LoginView.vue'
import WorldSelectView from '../views/WorldSelectView.vue'
import GameLayout from '../views/GameLayout.vue'
import CampView from '../views/CampView.vue'
import BuildingsView from '../views/BuildingsView.vue'
import WastelandView from '../views/WastelandView.vue'
import PlaceholderView from '../views/PlaceholderView.vue'
import RankingView from '../views/RankingView.vue'
import UnitsView from "../views/UnitsView.vue";
import MessagesView from '../views/MessagesView.vue'
import ResearchView from "../views/ResearchView.vue";
import DefenseView from "../views/DefenseView.vue";
import RareTechInventoryView from "../views/RareTechInventoryView.vue";

const routes = [
    {
        path: '/',
        name: 'login',
        component: LoginView
    },
    {
        path: '/worlds',
        name: 'worlds',
        component: WorldSelectView
    },
    {
        path: '/game',
        component: GameLayout,
        children: [
            { path: '',          name: 'camp',      component: CampView },
            { path: 'buildings', name: 'buildings',  component: BuildingsView },
            { path: 'research',  name: 'research',   component: ResearchView },
            { path: 'units',     name: 'units',      component: UnitsView },
            { path: 'defense', name: 'defense', component: DefenseView },
            { path: 'raretech', name: 'raretech', component: RareTechInventoryView },
            { path: 'wasteland', name: 'wasteland',  component: WastelandView },
            { path: 'alliance',  name: 'alliance',   component: PlaceholderView, props: { title: 'ALLIANCE', icon: '◈' } },
            { path: 'messages', name: 'messages', component: MessagesView },
            { path: 'ranking',   name: 'ranking',    component: RankingView, props: { title: 'RANKING', icon: '◈' } },
        ]
    }
]

const router = createRouter({
    history: createWebHistory(),
    routes
})

// Navigation guards
router.beforeEach((to) => {
    const playerId = sessionStorage.getItem('playerId')
    const worldId = sessionStorage.getItem('worldId')

    // Not logged in -> go to login
    if (to.path !== '/' && !playerId) {
        return { name: 'login' }
    }

    // Logged in but no world selected -> go to world select
    if (to.path.startsWith('/game') && !worldId) {
        return { name: 'worlds' }
    }
})

export default router