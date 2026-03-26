<template>
  <div class="game-layout" v-if="player">
    <header class="topbar">
      <div class="topbar-scan" />

      <div class="topbar-left">
        <div class="topbar-radar">
          <div class="topbar-radar-ring" />
          <div class="topbar-radar-core">☢</div>
        </div>
        <div>
          <div class="topbar-title">THE FALLEN WASTES</div>
        </div>
      </div>

      <div class="topbar-right">
        <AdvisorBar
            :settlement="settlement"
            :player="player"
            @refresh="refreshGameData"
        />

        <span class="topbar-meta">{{ dateStr }}</span>
        <span class="topbar-clock">{{ timeStr }}</span>
        <span class="topbar-status">● CONNECTED</span>
      </div>
    </header>

    <div class="resource-bar">
      <div
          v-for="r in resources"
          :key="r.key"
          class="resource-item"
      >
        <div
            class="resource-fill"
            :style="{ width: Math.min(100, (r.val / r.max) * 100) + '%' }"
        />
        <span class="resource-icon">{{ r.icon }}</span>
        <div class="resource-info">
          <div class="resource-header">
            <span class="resource-label">{{ r.key }}</span>
            <span class="resource-rate">{{ r.rate > 0 ? `+${r.rate}/H` : '—' }}</span>
          </div>
          <div class="resource-values">
            <span class="resource-val">{{ r.val.toLocaleString() }}</span>
            <span class="resource-max">/ {{ r.max.toLocaleString() }}</span>
          </div>
          <div class="resource-bar-mini">
            <div
                class="resource-bar-fill"
                :style="{ width: Math.min(100, (r.val / r.max) * 100) + '%' }"
            />
          </div>
        </div>
      </div>
    </div>

    <div class="game-body">
      <nav class="sidebar">
        <div class="sidebar-header">
          <div class="sidebar-settlement-icon">🏚️</div>
          <div class="sidebar-settlement-info">
            <div class="sidebar-settlement">{{ settlement?.name ?? 'Loading...' }}</div>
            <div class="sidebar-coords">
              <span style="color: var(--muted)">Free population: </span>
              <span style="color: var(--bright)">{{ freePopulation }}</span>
            </div>
          </div>
        </div>

        <div class="sidebar-nav">
          <div class="nav-group-label">Command</div>
          <router-link
              v-for="item in navMain"
              :key="item.route"
              :to="item.route"
              class="nav-item"
              :class="{ 'nav-item--active': $route.name === item.name }"
          >
            <div class="nav-icon" v-html="item.svg" />
            <span class="nav-label">{{ item.label }}</span>
          </router-link>

          <div class="nav-group-label">Military</div>
          <router-link
              v-for="item in navMilitary"
              :key="item.route"
              :to="item.route"
              class="nav-item"
              :class="{ 'nav-item--active': $route.name === item.name }"
          >
            <div class="nav-icon" v-html="item.svg" />
            <span class="nav-label">{{ item.label }}</span>
          </router-link>

          <div class="nav-group-label">World</div>
          <router-link
              v-for="item in navWorld"
              :key="item.route"
              :to="item.route"
              class="nav-item"
              :class="{ 'nav-item--active': $route.name === item.name }"
          >
            <div class="nav-icon" v-html="item.svg" />
            <span class="nav-label">{{ item.label }}</span>

            <span
                v-if="item.name === 'messages' && unreadMessagesCount > 0"
                class="nav-badge"
            >
              {{ unreadMessagesCount }}
            </span>
          </router-link>
        </div>

        <div class="sidebar-footer">
          <div class="sidebar-player">
            <div class="sidebar-avatar">{{ player.username.charAt(0).toUpperCase() }}</div>
            <div>
              <div class="sidebar-username">{{ player.username }}</div>
              <div class="sidebar-score">Score: {{ player.score }}</div>
            </div>
          </div>
        </div>
      </nav>

      <main class="content">
        <router-view
            :player="player"
            :settlement="settlement"
            :refresh-settlement="refreshGameData"
            :live-resources="liveSettlementResources"
            :live-population="livePopulation"
        />
      </main>
    </div>
  </div>

  <div v-else class="loading-screen">
    <div class="topbar-radar" style="width: 48px; height: 48px;">
      <div class="topbar-radar-ring" />
      <div class="topbar-radar-core" style="inset: 8px; font-size: 16px;">☢</div>
    </div>
    <p style="color: var(--muted); margin-top: 16px; font-family: var(--ff-title); font-size: 10px; letter-spacing: 3px;">
      ESTABLISHING CONNECTION...
    </p>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { getPlayerById, getSettlement, getUnreadMessageCount } from '../services/api'
import AdvisorBar from '../components/AdvisorBar.vue'

const router = useRouter()

const player = ref(null)
const settlement = ref(null)
const time = ref(new Date())
const unreadMessagesCount = ref(0)
let timerInterval = null
let refreshInFlight = null

const baseResources = ref(null)
const production = ref(null)
const fetchedAt = ref(Date.now())
const storageCaps = ref({
  water: 2000,
  food: 2000,
  scrap: 2000,
  fuel: 2000,
  energy: 2000,
  rareTech: 2000
})

const icons = {
  camp: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M3 12l9-8 9 8"/><path d="M5 10v10h14V10"/><path d="M9 20v-6h6v6"/></svg>`,
  buildings: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><rect x="3" y="8" width="7" height="12"/><rect x="14" y="4" width="7" height="16"/><path d="M5 12h3M5 15h3M16 8h3M16 11h3M16 14h3"/></svg>`,
  research: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><circle cx="10" cy="10" r="4"/><path d="M14 10h6M17 7v6"/><path d="M10 14v4M7 16h6"/></svg>`,
  units: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M12 2L2 7l10 5 10-5-10-5z"/><path d="M2 17l10 5 10-5"/><path d="M2 12l10 5 10-5"/></svg>`,
  defense: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M12 2l8 4v6c0 5.5-3.8 10.7-8 12-4.2-1.3-8-6.5-8-12V6l8-4z"/><path d="M9 12l2 2 4-4"/></svg>`,
  wasteland: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><circle cx="12" cy="12" r="10"/><path d="M2 12h20"/><ellipse cx="12" cy="12" rx="4" ry="10"/></svg>`,
  alliance: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><circle cx="8" cy="8" r="3"/><circle cx="16" cy="8" r="3"/><path d="M3 18c0-3 2.5-5 5-5s5 2 5 5"/><path d="M11 18c0-3 2.5-5 5-5s5 2 5 5"/></svg>`,
  messages: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><rect x="2" y="4" width="20" height="14" rx="2"/><path d="M2 4l10 8 10-8"/></svg>`,
  ranking: `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M6 9H2v12h4V9zM14 4h-4v17h4V4zM22 13h-4v8h4v-8z"/></svg>`,
}

const navMain = [
  { route: '/game', name: 'camp', label: 'Camp', svg: icons.camp },
  { route: '/game/buildings', name: 'buildings', label: 'Buildings', svg: icons.buildings },
  { route: '/game/research', name: 'research', label: 'Research', svg: icons.research },
]

const navMilitary = [
  { route: '/game/units', name: 'units', label: 'Units', svg: icons.units },
  { route: '/game/defense', name: 'defense', label: 'Defense', svg: icons.defense },
]

const navWorld = [
  { route: '/game/wasteland', name: 'wasteland', label: 'Wasteland', svg: icons.wasteland },
  { route: '/game/alliance', name: 'alliance', label: 'Alliance', svg: icons.alliance },
  { route: '/game/messages', name: 'messages', label: 'Messages', svg: icons.messages },
  { route: '/game/ranking', name: 'ranking', label: 'Ranking', svg: icons.ranking },
]

const timeStr = computed(() => time.value.toLocaleTimeString('en-GB'))
const dateStr = computed(() =>
    time.value.toLocaleDateString('en-GB', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    }).toUpperCase()
)

const freePopulation = computed(() => {
  const available = settlement.value?.availablePopulation
  if (available != null) return Math.max(0, available)

  const capacity = settlement.value?.populationCapacity ?? 0
  const used = settlement.value?.usedPopulation ?? 0
  return Math.max(0, capacity - used)
})

const livePopulation = computed(() => ({
  usedPopulation: settlement.value?.usedPopulation ?? 0,
  populationCapacity: settlement.value?.populationCapacity ?? 0,
  availablePopulation: freePopulation.value
}))

const resources = computed(() => {
  const caps = storageCaps.value

  if (!baseResources.value || !production.value) {
    if (!settlement.value) return []

    const s = settlement.value
    return [
      { key: 'Water', icon: '💧', val: s.water ?? 0, rate: 0, max: caps.water },
      { key: 'Food', icon: '🥫', val: s.food ?? 0, rate: 0, max: caps.food },
      { key: 'Scrap', icon: '⚙️', val: s.scrap ?? 0, rate: 0, max: caps.scrap },
      { key: 'Fuel', icon: '⛽', val: s.fuel ?? 0, rate: 0, max: caps.fuel },
      { key: 'Energy', icon: '⚡', val: s.energy ?? 0, rate: 0, max: caps.energy },
      { key: 'RareTech', icon: '🔬', val: s.rareTech ?? 0, rate: 0, max: caps.rareTech },
    ]
  }

  const elapsedHours = (time.value.getTime() - fetchedAt.value) / 3600000
  const b = baseResources.value
  const p = production.value

  return [
    {
      key: 'Water',
      icon: '💧',
      val: Math.min(caps.water, Math.floor(b.water + p.water * elapsedHours)),
      rate: p.water,
      max: caps.water
    },
    {
      key: 'Food',
      icon: '🥫',
      val: Math.min(caps.food, Math.floor(b.food + p.food * elapsedHours)),
      rate: p.food,
      max: caps.food
    },
    {
      key: 'Scrap',
      icon: '⚙️',
      val: Math.min(caps.scrap, Math.floor(b.scrap + p.scrap * elapsedHours)),
      rate: p.scrap,
      max: caps.scrap
    },
    {
      key: 'Fuel',
      icon: '⛽',
      val: Math.min(caps.fuel, Math.floor(b.fuel + p.fuel * elapsedHours)),
      rate: p.fuel,
      max: caps.fuel
    },
    {
      key: 'Energy',
      icon: '⚡',
      val: Math.min(caps.energy, Math.floor(b.energy + p.energy * elapsedHours)),
      rate: p.energy,
      max: caps.energy
    },
    {
      key: 'RareTech',
      icon: '🔬',
      val: Math.min(caps.rareTech, Math.floor(b.rareTech + p.rareTech * elapsedHours)),
      rate: p.rareTech,
      max: caps.rareTech
    },
  ]
})

const liveSettlementResources = computed(() => {
  const list = resources.value || []

  return {
    water: list.find(r => r.key === 'Water')?.val ?? 0,
    food: list.find(r => r.key === 'Food')?.val ?? 0,
    scrap: list.find(r => r.key === 'Scrap')?.val ?? 0,
    fuel: list.find(r => r.key === 'Fuel')?.val ?? 0,
    energy: list.find(r => r.key === 'Energy')?.val ?? 0,
    rareTech: list.find(r => r.key === 'RareTech')?.val ?? 0,
  }
})

async function loadUnreadMessagesCount() {
  const playerId = sessionStorage.getItem('playerId')
  if (!playerId) return

  try {
    const result = await getUnreadMessageCount(playerId)
    unreadMessagesCount.value = result?.count ?? 0
  } catch (err) {
    console.error('Failed to load unread message count', err)
  }
}

async function loadSettlementData() {
  if (!settlement.value?.id) return

  try {
    const data = await getSettlement(settlement.value.id)

    baseResources.value = {
      water: data.resources?.water ?? 0,
      food: data.resources?.food ?? 0,
      scrap: data.resources?.scrap ?? 0,
      fuel: data.resources?.fuel ?? 0,
      energy: data.resources?.energy ?? 0,
      rareTech: data.resources?.rareTech ?? 0
    }

    production.value = {
      water: data.production?.water ?? 0,
      food: data.production?.food ?? 0,
      scrap: data.production?.scrap ?? 0,
      fuel: data.production?.fuel ?? 0,
      energy: data.production?.energy ?? 0,
      rareTech: data.production?.rareTech ?? 0
    }

    if (data.storage) {
      storageCaps.value = {
        water: data.storage.waterCapacity ?? 2000,
        food: data.storage.foodCapacity ?? 2000,
        scrap: data.storage.scrapCapacity ?? 2000,
        fuel: data.storage.fuelCapacity ?? 2000,
        energy: data.storage.energyCapacity ?? 2000,
        rareTech: data.storage.rareTechCapacity ?? 2000
      }
    }

    fetchedAt.value = Date.now()

    settlement.value = {
      ...settlement.value,
      usedPopulation: data.usedPopulation ?? settlement.value.usedPopulation ?? 0,
      populationCapacity: data.populationCapacity ?? settlement.value.populationCapacity ?? 0,
      availablePopulation: data.availablePopulation ?? settlement.value.availablePopulation ?? 0,
      morale: data.morale ?? settlement.value.morale ?? 0,
      water: data.resources?.water ?? 0,
      food: data.resources?.food ?? 0,
      scrap: data.resources?.scrap ?? 0,
      fuel: data.resources?.fuel ?? 0,
      energy: data.resources?.energy ?? 0,
      rareTech: data.resources?.rareTech ?? 0
    }
  } catch (err) {
    console.error('Failed to load settlement data', err)
  }
}

function onUnreadMessagesUpdated(event) {
  unreadMessagesCount.value = event.detail?.count ?? 0
}

function setPlayerData(data) {
  player.value = {
    id: data.id,
    username: data.username,
    email: data.email,
    createdAtUtc: data.createdAtUtc,
    isActive: data.isActive,
    score: data.score,
    advisors: {
      commander: data.advisors?.commander ?? { active: false, expiresUtc: null },
      quartermaster: data.advisors?.quartermaster ?? { active: false, expiresUtc: null },
      techPriest: data.advisors?.techPriest ?? { active: false, expiresUtc: null },
      warlord: data.advisors?.warlord ?? { active: false, expiresUtc: null },
      scoutMaster: data.advisors?.scoutMaster ?? { active: false, expiresUtc: null }
    }
  }

  const s = data.settlement || data.csharpSettlements?.[0]
  if (s) {
    settlement.value = {
      id: s.id,
      name: s.name,
      playerId: s.playerId,
      usedPopulation: s.usedPopulation ?? 0,
      populationCapacity: s.populationCapacity ?? 0,
      availablePopulation: s.availablePopulation ?? s.AvailablePopulation ?? 0,
      morale: s.morale ?? 0,
      water: s.water ?? s.resources?.water ?? 0,
      food: s.food ?? s.resources?.food ?? 0,
      scrap: s.scrap ?? s.resources?.scrap ?? 0,
      fuel: s.fuel ?? s.resources?.fuel ?? 0,
      energy: s.energy ?? s.resources?.energy ?? 0,
      rareTech: s.rareTech ?? s.resources?.rareTech ?? 0
    }
  }
}

async function refreshGameData() {
  const playerId = sessionStorage.getItem('playerId')
  if (!playerId) return

  // Coalesce concurrent refreshes: return the in-flight promise if one exists.
  if (refreshInFlight) return refreshInFlight

  refreshInFlight = (async () => {
    try {
      const data = await getPlayerById(playerId)
      sessionStorage.setItem('playerData', JSON.stringify(data))
      setPlayerData(data)
      await loadSettlementData()
      await loadUnreadMessagesCount()
    } catch (err) {
      console.error('Failed to refresh game data', err)
    } finally {
      refreshInFlight = null
    }
  })()

  return refreshInFlight
}

onMounted(async () => {
  timerInterval = setInterval(() => {
    time.value = new Date()
  }, 1000)

  window.addEventListener('messages-unread-updated', onUnreadMessagesUpdated)

  const playerId = sessionStorage.getItem('playerId')
  const cached = sessionStorage.getItem('playerData')

  if (!playerId) {
    router.push('/')
    return
  }

  if (cached) {
    const data = JSON.parse(cached)
    setPlayerData(data)
  }

  try {
    const data = await getPlayerById(playerId)
    sessionStorage.setItem('playerData', JSON.stringify(data))
    setPlayerData(data)
  } catch (err) {
    if (!cached) {
      sessionStorage.removeItem('playerId')
      router.push('/')
      return
    }
  }

  await Promise.all([
    loadSettlementData(),
    loadUnreadMessagesCount()
  ])
})

onUnmounted(() => {
  clearInterval(timerInterval)
  window.removeEventListener('messages-unread-updated', onUnreadMessagesUpdated)
})
</script>

<style scoped>
:root {
  --cyan: #00d4ff;
  --cyan-dark: #0088aa;
  --cyan-dim: #004466;
  --cyan-glow: rgba(0, 212, 255, 0.08);
  --red: #ff3040;
  --green: #30ff80;
  --amber: #ffaa20;
  --bg: #060a10;
  --bg2: #0a1018;
  --bg3: #0e1620;
  --border: #142030;
  --border-bright: #1a3048;
  --muted: #3a5a70;
  --text: #8ab4cc;
  --bright: #c0e8ff;
  --ff: 'Rajdhani', sans-serif;
  --ff-title: 'Orbitron', sans-serif;
}

.game-layout {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  animation: fadeInUp 0.4s ease-out;
}

.game-body {
  display: flex;
  flex: 1;
  min-height: 0;
}

.content {
  flex: 1;
  padding: 22px;
  overflow: auto;
}

.topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
  height: 48px;
  background: linear-gradient(180deg, #0c1420, var(--bg));
  border-bottom: 1px solid var(--border);
  position: relative;
  overflow: hidden;
}

.topbar-scan {
  position: absolute;
  top: 0;
  width: 20%;
  height: 100%;
  background: linear-gradient(90deg, transparent, var(--cyan-glow), transparent);
  animation: horzScan 6s linear infinite;
  pointer-events: none;
}

.topbar-left,
.topbar-right {
  display: flex;
  align-items: center;
  gap: 14px;
  z-index: 1;
}

.topbar-radar {
  position: relative;
  width: 30px;
  height: 30px;
}

.topbar-radar-ring {
  position: absolute;
  inset: 0;
  border: 2px solid var(--cyan-dim);
  border-radius: 50%;
  border-top-color: var(--cyan);
  animation: radarSweep 3s linear infinite;
}

.topbar-radar-core {
  position: absolute;
  inset: 4px;
  background: radial-gradient(circle, var(--cyan-dim), transparent);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 10px;
  color: var(--cyan);
}

.topbar-title {
  font-family: var(--ff-title), sans-serif;
  font-size: 14px;
  color: var(--cyan);
  letter-spacing: 4px;
  font-weight: 700;
  text-shadow: 0 0 16px rgba(0, 212, 255, 0.3);
}

.topbar-meta {
  color: var(--muted);
  letter-spacing: 2px;
  font-family: var(--ff-title), sans-serif;
  font-size: 9px;
}

.topbar-clock {
  color: var(--cyan);
  font-family: var(--ff-title), sans-serif;
  font-size: 12px;
  letter-spacing: 3px;
  text-shadow: 0 0 8px rgba(0, 212, 255, 0.3);
  animation: dataStream 2s infinite;
}

.topbar-status {
  color: var(--green);
  font-size: 9px;
}

.resource-bar {
  display: flex;
  gap: 1px;
  padding: 6px 10px;
  background: var(--bg2);
  border-bottom: 1px solid var(--border);
  flex-wrap: wrap;
}

.resource-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 6px 14px;
  background: var(--bg3);
  border: 1px solid var(--border);
  flex: 1 1 140px;
  min-width: 140px;
  position: relative;
  overflow: hidden;
}

.resource-fill {
  position: absolute;
  left: 0;
  top: 0;
  bottom: 0;
  background: linear-gradient(90deg, transparent, rgba(0, 212, 255, 0.04));
  transition: width 1s;
}

.resource-icon {
  font-size: 13px;
  z-index: 1;
}

.resource-info {
  z-index: 1;
  flex: 1;
}

.resource-header {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
}

.resource-label {
  font-size: 8px;
  color: var(--muted);
  text-transform: uppercase;
  letter-spacing: 2px;
  font-weight: 700;
}

.resource-rate {
  font-size: 8px;
  color: var(--cyan-dim);
}

.resource-values {
  display: flex;
  align-items: baseline;
  gap: 4px;
  margin-top: 1px;
}

.resource-val {
  font-size: 16px;
  color: var(--cyan);
  font-family: var(--ff-title), sans-serif;
  font-weight: 700;
  text-shadow: 0 0 8px rgba(0, 212, 255, 0.2);
}

.resource-max {
  font-size: 9px;
  color: var(--cyan-dim);
}

.resource-bar-mini {
  height: 2px;
  background: var(--border);
  margin-top: 4px;
  border-radius: 1px;
}

.resource-bar-fill {
  height: 100%;
  background: linear-gradient(90deg, var(--cyan-dark), var(--cyan));
  border-radius: 1px;
  box-shadow: 0 0 4px rgba(0, 212, 255, 0.4);
}

.sidebar {
  width: 210px;
  background: linear-gradient(180deg, var(--bg2), var(--bg));
  border-right: 1px solid var(--border);
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
  overflow-y: auto;
}

.sidebar-header {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 14px;
  border-bottom: 1px solid var(--border);
  background: linear-gradient(135deg, rgba(0,212,255,0.03), transparent);
}

.sidebar-settlement-icon {
  width: 38px;
  height: 38px;
  border-radius: 6px;
  background: linear-gradient(135deg, var(--bg3), var(--bg));
  border: 1px solid var(--border-bright);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 18px;
  flex-shrink: 0;
}

.sidebar-settlement-info {
  min-width: 0;
}

.sidebar-settlement {
  font-size: 13px;
  color: var(--cyan);
  font-weight: 700;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.sidebar-coords {
  font-size: 9px;
  color: var(--muted);
  margin-top: 2px;
  font-weight: 600;
}

.sidebar-nav {
  flex: 1;
  padding: 4px 0;
}

.nav-group-label {
  font-size: 8px;
  color: var(--cyan-dim);
  text-transform: uppercase;
  letter-spacing: 3px;
  font-weight: 700;
  font-family: var(--ff-title), sans-serif;
  padding: 12px 14px 6px;
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 9px 14px;
  text-decoration: none;
  color: var(--muted);
  transition: all 0.15s;
  border-left: 3px solid transparent;
  position: relative;
}

.nav-item:hover {
  color: var(--bright);
  background: rgba(0, 212, 255, 0.03);
}

.nav-item:hover .nav-icon {
  color: var(--cyan);
}

.nav-item--active {
  color: var(--cyan);
  background: linear-gradient(90deg, rgba(0,212,255,0.08), transparent);
  border-left-color: var(--cyan);
}

.nav-item--active .nav-icon {
  color: var(--cyan);
  filter: drop-shadow(0 0 4px rgba(0,212,255,0.4));
}

.nav-icon {
  width: 22px;
  height: 22px;
  color: var(--muted);
  flex-shrink: 0;
  transition: all 0.15s;
}

.nav-icon svg {
  width: 100%;
  height: 100%;
}

.nav-label {
  font-size: 13px;
  font-weight: 600;
  letter-spacing: 0.5px;
}

.sidebar-footer {
  padding: 12px 14px;
  border-top: 1px solid var(--border);
  background: linear-gradient(180deg, transparent, rgba(0,0,0,0.2));
}

.sidebar-player {
  display: flex;
  align-items: center;
  gap: 8px;
}

.sidebar-avatar {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  border: 1px solid var(--border-bright);
  background: linear-gradient(135deg, var(--cyan-dim), var(--bg));
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
  color: var(--cyan);
  font-family: var(--ff-title), sans-serif;
  font-weight: 700;
  flex-shrink: 0;
}

.sidebar-username {
  font-size: 11px;
  color: var(--bright);
  font-weight: 600;
}

.sidebar-score {
  font-size: 9px;
  color: var(--muted);
}

.loading-screen {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background: var(--bg);
}

.loading-text {
  color: var(--muted);
  margin-top: 16px;
  font-family: var(--ff-title), sans-serif;
  font-size: 10px;
  letter-spacing: 3px;
}

.nav-badge {
  margin-left: auto;
  min-width: 18px;
  height: 18px;
  padding: 0 6px;
  border-radius: 999px;
  background: rgba(0, 212, 255, 0.12);
  border: 1px solid rgba(0, 212, 255, 0.35);
  color: var(--cyan);
  font-family: var(--ff-title), sans-serif;
  font-size: 9px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 0 8px rgba(0,212,255,.12);
}
</style>