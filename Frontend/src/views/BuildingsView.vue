<template>
  <div class="fade-in">
    <div class="page-header">
      <h2 class="page-title">BUILDINGS</h2>
      <span class="page-subtitle">FACILITY MANAGEMENT</span>
    </div>
    <div class="accent-line" style="margin-bottom: 16px;" />

    <div class="queue-panel" v-if="!loading">
      <div class="queue-header">
        <div class="queue-title-row">
          <span class="queue-title">CONSTRUCTION QUEUE</span>
          <span class="queue-slots" :class="{ 'queue-slots--full': queueFull }">
            {{ activeCount }} / {{ queueLimit }}
          </span>
        </div>

        <div class="queue-commander" v-if="!commanderActive" @click="showCommanderInfo = !showCommanderInfo">
          <span class="commander-icon">⭐</span>
          <span class="commander-text">COMMANDER: +5 SLOTS</span>
        </div>

        <div class="queue-commander queue-commander--active" v-else>
          <span class="commander-icon">⭐</span>
          <span class="commander-text">COMMANDER ACTIVE</span>
        </div>
      </div>

      <div class="commander-info" v-if="showCommanderInfo && !commanderActive">
        <div class="commander-info-text">
          Activate a <strong>Commander</strong> to expand your build queue from 2 to 7 simultaneous constructions.
        </div>
        <button class="commander-activate-btn" @click="activateCommander">
          ACTIVATE COMMANDER (14 DAYS)
        </button>
      </div>

      <div class="queue-items" v-if="queueItems.length > 0">
        <div
            v-for="(q, idx) in queueItems"
            :key="q.uniqueKey"
            class="queue-item"
        >
          <div class="queue-item-index">{{ idx + 1 }}</div>
          <span class="queue-item-icon">{{ getIcon(q.type) }}</span>

          <div class="queue-item-info">
            <div class="queue-item-name">{{ q.displayName }}</div>
            <div class="queue-item-level">→ L{{ q.toLevel ?? q.targetLevel }}</div>
          </div>

          <div class="queue-item-timer">
            <!-- Only the active/constructing item shows a countdown; waiting items show a WAITING label -->
            <div v-if="q.isActive" class="queue-item-time">{{ formatTime(getQueueRemaining(q)) }}</div>
            <div v-else class="queue-item-waiting">WAITING</div>

            <div class="queue-item-bar">
              <!-- Fill only for the active item -->
              <div v-if="q.isActive" class="queue-item-bar-fill" :style="{ width: getQueuePct(q) + '%' }" />
              <div v-else class="queue-item-bar-empty" />
            </div>
          </div>

          <button
              class="queue-item-cancel"
              @click="cancelQueueItem(q)"
              title="Cancel"
          >
            ✕
          </button>
        </div>
      </div>

      <div class="queue-empty" v-else>
        <span class="queue-empty-text">No active constructions — {{ queueLimit }} slots available</span>
      </div>

      <div class="queue-slot-dots">
        <div
            v-for="i in queueLimit"
            :key="i"
            class="queue-dot"
            :class="{
            'queue-dot--active': i <= activeCount,
            'queue-dot--commander': i > 2
          }"
        />
      </div>
    </div>

    <div class="filters">
      <button
          v-for="cat in categories"
          :key="cat.key"
          class="filter-btn"
          :class="{ 'filter-btn--active': activeFilter === cat.key }"
          @click="activeFilter = cat.key"
      >
        {{ cat.label }}
      </button>
    </div>

    <div v-if="loading" class="loading-msg">Loading facilities...</div>
    <div v-if="error" class="error-msg">{{ error }}</div>

    <div v-if="!loading" class="buildings-grid">
      <button
          v-for="b in filteredBuildings"
          :key="b.type"
          class="building-card"
          :class="{
          'building-card--upgrading': b.isConstructing,
          'building-card--unbuilt': b.level === 0 && !b.isConstructing,
          'building-card--locked': b.isLocked,
          'building-card--future': b.isFutureFeature
        }"
          @click="selectedBuilding = b"
      >
        <div class="building-accent" :class="{ 'building-accent--active': b.isConstructing }" />

        <div class="building-body">
          <div class="building-top">
            <div class="building-info">
              <span class="building-icon">{{ getIcon(b.type) }}</span>
              <div>
                <div class="building-name" :class="{ 'building-name--dim': b.level === 0 && !b.isConstructing }">
                  {{ b.displayName }}
                </div>
                <div class="building-cat">{{ formatCategoryLabel(b.category) }}</div>
              </div>
            </div>

            <div v-if="b.level > 0" class="building-level">L{{ b.level }}</div>
          </div>

          <div class="building-desc">{{ b.description }}</div>

          <div v-if="b.level > 0 && hasProduction(b)" class="building-prod">
            {{ getProductionText(b) }}
          </div>

          <div v-if="b.level > 0 && hasStatsRow(b)" class="building-stats">
            <span v-if="b.powerValue > 0">PWR {{ b.powerValue }}</span>
            <span v-if="b.populationUsage > 0">POP {{ b.populationUsage }}</span>
            <span v-if="b.storageBonus > 0">STORE {{ b.storageBonus }}</span>
            <span v-if="b.defenseValue > 0">DEF {{ b.defenseValue }}</span>
          </div>

          <div v-if="b.isConstructing" class="building-progress">
            <div class="building-progress-header">
              <span class="building-progress-label">UPGRADING → L{{ b.targetLevel }}</span>
              <span class="building-progress-time">{{ formatTime(getRemaining(b)) }}</span>
            </div>
            <div class="building-progress-bar">
              <div class="building-progress-fill" :style="{ width: getProgressPct(b) + '%' }" />
            </div>
          </div>

          <div v-if="b.isFutureFeature" class="building-future">
            [ FUTURE FEATURE ]
          </div>

          <div v-else-if="b.isLocked && b.level === 0" class="building-locked">
            [ LOCKED ]
          </div>

          <div v-else-if="b.queueFull && !b.isConstructing" class="building-queue-full">
            QUEUE FULL ({{ activeCount }}/{{ queueLimit }})
          </div>

          <div v-else-if="b.level === 0 && !b.isConstructing" class="building-blueprint">
            [ BLUEPRINT AVAILABLE ]
          </div>
        </div>
      </button>
    </div>

        <BuildingModal
        v-if="selectedBuilding"
        :building="selectedBuilding"
        :settlement="settlement"
        :live-resources="liveResources"
        :queue-active="activeCount"
        :queue-limit="queueLimit"
        :queue-items="queueItems"
        @close="selectedBuilding = null"
        @upgrade="onUpgrade"
    />
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import {
  getBuildings,
  cancelBuilding,
  getBuildQueue,
  activateCommanderApi
} from '../services/api.js'
import BuildingModal from '../components/BuildingModal.vue'

const props = defineProps({
  player: Object,
  settlement: Object,
  refreshSettlement: Function,
  liveResources: Object
})

const selectedBuilding = ref(null)
const activeFilter = ref('all')
const buildings = ref([])
const loading = ref(true)
const error = ref('')
const now = ref(Date.now())
let tickInterval = null
let fetchBuildingsInFlight = null

// Full normalized queue from backend (includes constructing + waiting)
const queueItems = ref([])
const queueLimit = ref(2)
const commanderActiveState = ref(false)
const showCommanderInfo = ref(false)

const commanderActive = computed(() => commanderActiveState.value)
// Number of currently active/constructing slots (used to show active slots vs waiting)
const activeCount = computed(() => queueItems.value.filter(q => q.isActive).length)
const queueFull = computed(() => activeCount.value >= queueLimit.value)

const categories = [
  { key: 'all', label: 'ALL' },
  { key: 'center', label: 'CENTER' },
  { key: 'resource', label: 'RES' },
  { key: 'storage', label: 'STORE' },
  { key: 'special-storage', label: 'TECH' },
  { key: 'military', label: 'MIL' },
  { key: 'defense', label: 'DEF' },
  { key: 'research', label: 'R&D' }
]

const ICONS = {
  HeadQuarter: '🏛️',
  Shelter: '🏠',
  CouncilHall: '🏛',

  FarmDome: '🌾',
  FuelRefinery: '🛢️',
  ScrapForge: '🔩',
  WaterPurifier: '💧',
  SolarArray: '☀️',

  FoodSilo: '🥫',
  FuelDepot: '⛽',
  PowerBank: '🔋',
  ScrapVault: '⚙️',
  WaterTank: '💧',
  TechVault: '🧬',

  Barracks: '🎖️',
  Garage: '🚗',
  Workshop: '🛠️',
  CommandCenter: '📡',

  PerimeterWall: '🧱',
  WatchTower: '🗼',

  TechLab: '🧪',
  TechSalvager: '🔬'
}

function getIcon(type) {
  return ICONS[type] || '🏗️'
}

function normalizeBuilding(raw) {
  return {
    id: raw.id ?? raw.Id ?? null,
    type: raw.type ?? raw.Type ?? '',
    displayName: raw.displayName ?? raw.DisplayName ?? raw.type ?? raw.Type ?? '',
    description: raw.description ?? raw.Description ?? '',
    category: raw.category ?? raw.Category ?? 'other',

    startingLevel: raw.startingLevel ?? raw.StartingLevel ?? 0,
    level: raw.level ?? raw.Level ?? 0,
    effectiveLevel: raw.effectiveLevel ?? raw.EffectiveLevel ?? raw.level ?? raw.Level ?? 0,

    isConstructing: raw.isConstructing ?? raw.IsConstructing ?? false,
    targetLevel: raw.targetLevel ?? raw.TargetLevel ?? 0,
    remainingSeconds: raw.remainingSeconds ?? raw.RemainingSeconds ?? 0,
    constructionEndUtc: raw.constructionEndUtc ?? raw.ConstructionEndUtc ?? null,

    isFutureFeature: raw.isFutureFeature ?? raw.IsFutureFeature ?? false,
    isBuildable: raw.isBuildable ?? raw.IsBuildable ?? true,
    isLocked: raw.isLocked ?? raw.IsLocked ?? false,
    queueFull: raw.queueFull ?? raw.QueueFull ?? false,

    prerequisites: raw.prerequisites ?? raw.Prerequisites ?? [],

    canUpgrade: raw.canUpgrade ?? raw.CanUpgrade ?? false,
    upgradeCost: raw.upgradeCost ?? raw.UpgradeCost ?? null,
    buildTimeSeconds: raw.buildTimeSeconds ?? raw.BuildTimeSeconds ?? 0,

    hourlyProduction: raw.hourlyProduction ?? raw.HourlyProduction ?? {},
    populationUsage: raw.populationUsage ?? raw.PopulationUsage ?? 0,
    powerValue: raw.powerValue ?? raw.PowerValue ?? 0,
    storageBonus: raw.storageBonus ?? raw.StorageBonus ?? 0,
    defenseValue: raw.defenseValue ?? raw.DefenseValue ?? 0
  }
}

function normalizeQueueItem(raw) {
  // Derive a stable status from commonly-used backend fields. Backends differ so we
  // look for several possible names and normalize to 'active' | 'waiting' | 'completed' | undefined.
  let rawStatus = raw.status ?? raw.Status ?? raw.state ?? raw.State

  if (typeof rawStatus === 'string') {
    const s = rawStatus.toLowerCase()
    if (s.includes('wait') || s.includes('queue') || s.includes('queued')) rawStatus = 'waiting'
    else if (s.includes('complete') || s.includes('done')) rawStatus = 'completed'
    else if (s.includes('active') || s.includes('inprogress') || s.includes('construct')) rawStatus = 'active'
  }

  // boolean hints from other fields
  const hintActive = raw.isActive ?? raw.IsActive ?? raw.active ?? raw.Active ?? raw.isConstructing ?? raw.IsConstructing
  const hintWaiting = raw.isWaiting ?? raw.IsWaiting ?? raw.queuePosition ?? raw.QueuePosition ?? raw.queueIndex ?? raw.QueueIndex
  const hintCompleted = raw.isCompleted ?? raw.IsCompleted ?? raw.completed ?? raw.Completed

  let status = rawStatus
  if (!status) {
    if (hintActive) status = 'active'
    else if (hintWaiting) status = 'waiting'
    else if (hintCompleted) status = 'completed'
  }

  const isActive = status === 'active'
  const isWaiting = status === 'waiting'
  const isCompleted = status === 'completed'

  // Some backends include explicit from/to levels or a queue position; normalize those
  const fromLevel = raw.fromLevel ?? raw.FromLevel ?? raw.level ?? raw.Level ?? null
  const toLevel = raw.toLevel ?? raw.ToLevel ?? raw.targetLevel ?? raw.TargetLevel ?? null
  const position = raw.position ?? raw.Position ?? raw.queuePosition ?? raw.QueuePosition ?? raw.queueIndex ?? raw.QueueIndex ?? null

  // Build a stable unique key for list rendering. Prefer server id; otherwise use a composite key
  const type = raw.type ?? raw.Type ?? ''
  const serverId = raw.id ?? raw.Id ?? null
  const uniqueKey = serverId ?? `${type}-${position ?? toLevel ?? fromLevel ?? ''}-${raw.requestId ?? raw.request_id ?? ''}`

  return {
    id: serverId,
    uniqueKey,
    type,
    displayName: raw.displayName ?? raw.DisplayName ?? type,
    fromLevel,
    toLevel,
    level: raw.level ?? raw.Level ?? 0,
    targetLevel: raw.targetLevel ?? raw.TargetLevel ?? toLevel ?? 0,
    constructionEndUtc: raw.constructionEndUtc ?? raw.ConstructionEndUtc ?? null,
    remainingSeconds: raw.remainingSeconds ?? raw.RemainingSeconds ?? 0,
    buildTimeSeconds: raw.buildTimeSeconds ?? raw.BuildTimeSeconds ?? 0,
    position,
    // Normalized status flags (used by the UI to decide how to render items)
    status,
    isActive,
    isWaiting,
    isCompleted
  }
}

function hasProduction(b) {
  const p = b.hourlyProduction || {}
  return p.water || p.food || p.scrap || p.fuel || p.energy || p.rareTech
}

function hasStatsRow(b) {
  return (b.powerValue > 0 || b.populationUsage > 0 || b.storageBonus > 0 || b.defenseValue > 0) && b.level > 0
}

function getProductionText(b) {
  const p = b.hourlyProduction || {}
  const parts = []
  if (p.water) parts.push(`+${p.water} 💧`)
  if (p.food) parts.push(`+${p.food} 🌾`)
  if (p.scrap) parts.push(`+${p.scrap} ⚙️`)
  if (p.fuel) parts.push(`+${p.fuel} ⛽`)
  if (p.energy) parts.push(`+${p.energy} ⚡`)
  if (p.rareTech) parts.push(`+${p.rareTech} 🧬`)
  return parts.join('  ') + ' /h'
}

function getRemaining(b) {
  if (!b.constructionEndUtc) return b.remainingSeconds || 0
  const endStr = b.constructionEndUtc.endsWith('Z') ? b.constructionEndUtc : `${b.constructionEndUtc}Z`
  const end = new Date(endStr).getTime()
  return Math.max(0, Math.ceil((end - now.value) / 1000))
}

function getProgressPct(b) {
  if (!b.constructionEndUtc || !b.buildTimeSeconds) return 0
  const remaining = getRemaining(b)
  return Math.min(100, Math.max(0, ((b.buildTimeSeconds - remaining) / b.buildTimeSeconds) * 100))
}

function getQueueRemaining(q) {
  if (!q.constructionEndUtc) return q.remainingSeconds || 0
  const endStr = q.constructionEndUtc.endsWith('Z') ? q.constructionEndUtc : `${q.constructionEndUtc}Z`
  const end = new Date(endStr).getTime()
  return Math.max(0, Math.ceil((end - now.value) / 1000))
}

function getQueuePct(q) {
  const total = q.buildTimeSeconds || 1
  const remaining = getQueueRemaining(q)
  return Math.min(100, Math.max(0, ((total - remaining) / total) * 100))
}

function formatTime(sec) {
  if (sec <= 0) return 'Done!'
  const h = Math.floor(sec / 3600)
  const m = Math.floor((sec % 3600) / 60)
  const s = sec % 60
  return `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`
}

function formatCategoryLabel(category) {
  return String(category || 'other').replace(/-/g, ' ').toUpperCase()
}

const filteredBuildings = computed(() => {
  if (activeFilter.value === 'all') return buildings.value
  return buildings.value.filter(b => b.category === activeFilter.value)
})

async function fetchBuildings() {
  if (!props.settlement?.id) return

  if (fetchBuildingsInFlight) return fetchBuildingsInFlight

  fetchBuildingsInFlight = (async () => {
    try {
      const [buildingsData, queueData] = await Promise.all([
        getBuildings(props.settlement.id),
        getBuildQueue(props.settlement.id)
      ])

      buildings.value = (buildingsData || []).map(normalizeBuilding)

      // Backend may provide separate arrays for Constructing and Waiting; support those
      const constructing = queueData?.Constructing ?? queueData?.constructing ?? []
      const waiting = queueData?.Waiting ?? queueData?.waiting ?? []
      const fallback = queueData?.Queue ?? queueData?.queue ?? []

      const combinedRaw = [].concat(constructing, waiting, fallback)

      // Normalize and de-duplicate by uniqueKey (preserve first-seen order)
      const normalized = combinedRaw.map(normalizeQueueItem)
      const seen = new Map()
      for (const it of normalized) {
        if (!it) continue
        if (seen.has(it.uniqueKey)) continue
        seen.set(it.uniqueKey, it)
      }

      queueItems.value = Array.from(seen.values()).filter(q => !q.isCompleted)

        // If the buildings list shows a building currently constructing but no queue item
        // was marked active by the backend, try to reconcile them client-side so the
        // UI remains consistent (building card shows upgrading while queue shows WAITING).
        if (!queueItems.value.some(q => q.isActive)) {
          const constructingBuilding = buildings.value.find(b => b.isConstructing)
          if (constructingBuilding) {
            // Prefer an exact match by toLevel/fromLevel/position when the backend provides it,
            // otherwise fall back to matching by type/displayName. Also prefer waiting items
            // so we don't accidentally pick a completed slot.
            let match = null

            // Try exact matches first
            match = queueItems.value.find(q => {
              if (!q) return false
              if ((q.toLevel != null) && (constructingBuilding.targetLevel != null) && (q.toLevel === constructingBuilding.targetLevel)) return true
              if ((q.fromLevel != null) && (constructingBuilding.level != null) && (q.fromLevel === constructingBuilding.level)) return true
              if ((q.position != null) && (constructingBuilding.queuePosition != null) && (q.position === constructingBuilding.queuePosition)) return true
              return false
            })

            // Next prefer same type + waiting
            if (!match) {
              match = queueItems.value.find(q => q.type === constructingBuilding.type && q.isWaiting)
            }

            // Fallback to any same-type item
            if (!match) {
              match = queueItems.value.find(q => q.type === constructingBuilding.type || q.displayName === constructingBuilding.displayName)
            }

            if (match) {
              match.isActive = true
              match.status = match.status || 'active'

              // If backend provided timing on the building but not the queue item, copy it
              if (!match.constructionEndUtc && constructingBuilding.constructionEndUtc) {
                match.constructionEndUtc = constructingBuilding.constructionEndUtc
              }
              if (!match.buildTimeSeconds && constructingBuilding.buildTimeSeconds) {
                match.buildTimeSeconds = constructingBuilding.buildTimeSeconds
              }
            }
          }
        }

      // Prefer backend-provided numeric limit when present; otherwise fall back to player-level advisor
      const backendLimit = queueData?.Limit ?? queueData?.limit
      if (Number.isInteger(backendLimit) && backendLimit > 0) {
        queueLimit.value = backendLimit
      } else {
        queueLimit.value = props.player?.advisors?.commander?.active ? 7 : 2
      }

      // Commander active state should be derived from player-level advisor first.
      commanderActiveState.value = props.player?.advisors?.commander?.active ?? (queueData?.CommanderActive ?? queueData?.commanderActive ?? false)

      error.value = ''
    } catch (err) {
      error.value = err?.response?.data || 'Failed to load buildings.'
    } finally {
      loading.value = false
      fetchBuildingsInFlight = null
    }
  })()

  return fetchBuildingsInFlight
}

async function onUpgrade() {
  selectedBuilding.value = null
  // Ensure we perform a fresh fetch after an upgrade rather than re-using
  // any in-flight fetch that may have started before the server processed
  // the new queue item. Clearing the in-flight token forces fetchBuildings
  // to issue new API requests so the UI will reflect the queued upgrade.
  fetchBuildingsInFlight = null
  await fetchBuildings()

  if (props.refreshSettlement) {
    await props.refreshSettlement()
  }
}

async function cancelQueueItem(arg) {
  if (!props.settlement?.id) return

  // Backwards compatible: caller may pass a building type string or a queue item object
  const buildingType = typeof arg === 'string' ? arg : (arg?.type || arg?.buildingType || null)

  try {
    if (!buildingType) {
      // If we don't have a building type but the server provided a queue item id, try to use that
      const queueId = typeof arg === 'object' ? arg.id ?? arg.Id ?? null : null
      if (queueId) {
        // The existing API expects a buildingType; if backend later supports cancel by id,
        // this is the place to call it. For now fall back to refreshing.
        await fetchBuildings()
      } else {
        throw new Error('Unable to determine queue item to cancel')
      }
    } else {
      await cancelBuilding(props.settlement.id, buildingType)
      await fetchBuildings()
    }

    if (props.refreshSettlement) {
      await props.refreshSettlement()
    }
  } catch (err) {
    error.value = err?.response?.data || 'Cancel failed.'
  }
}

async function activateCommander() {
  if (!props.settlement?.id) return

  try {
    await activateCommanderApi(props.settlement.id, 14)
    showCommanderInfo.value = false
    await fetchBuildings()

    if (props.refreshSettlement) {
      await props.refreshSettlement()
    }
  } catch (err) {
    error.value = err?.response?.data || 'Commander activation failed.'
  }
}

onMounted(() => {
  fetchBuildings()

  tickInterval = setInterval(async () => {
    now.value = Date.now()

    // If any active queue item finished, refresh the data
    if (queueItems.value.some(q => q.isActive && getQueueRemaining(q) <= 0)) {
      await fetchBuildings()

      if (props.refreshSettlement) {
        await props.refreshSettlement()
      }
    }
  }, 1000)
})

onUnmounted(() => {
  if (tickInterval) clearInterval(tickInterval)
})
</script>

<style scoped>
.page-header { display: flex; align-items: baseline; gap: 12px; margin-bottom: 4px }
.page-title { font-family: var(--ff-title); font-size: 16px; color: var(--cyan); letter-spacing: 3px; font-weight: 700 }
.page-subtitle { font-size: 8px; color: var(--cyan-dim); letter-spacing: 2px; font-family: var(--ff-title) }
.loading-msg { color: var(--cyan); font-family: var(--ff-title); font-size: 11px; letter-spacing: 2px; text-align: center; padding: 40px; animation: pulse 1.5s infinite }
.error-msg { color: var(--red); font-size: 11px; padding: 10px 14px; background: rgba(255,48,64,.06); border: 1px solid rgba(255,48,64,.2); margin-bottom: 16px }
@keyframes pulse { 0%,100%{opacity:1}50%{opacity:.3} }

.queue-panel {
  background: linear-gradient(180deg, rgba(0,212,255,.03), rgba(0,212,255,.01));
  border: 1px solid var(--border-bright);
  margin-bottom: 18px;
  overflow: hidden;
}
.queue-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 14px;
  background: linear-gradient(90deg, rgba(0,212,255,.06), transparent);
  border-bottom: 1px solid var(--border);
}
.queue-title-row { display: flex; align-items: center; gap: 10px }
.queue-title {
  font-family: var(--ff-title);
  font-size: 10px;
  color: var(--cyan);
  letter-spacing: 2px;
  font-weight: 700;
}
.queue-slots {
  font-family: var(--ff-title);
  font-size: 11px;
  color: var(--cyan);
  letter-spacing: 1px;
  padding: 2px 8px;
  border: 1px solid var(--border-bright);
  background: rgba(0,212,255,.06);
}
.queue-slots--full {
  color: #ff6040;
  border-color: rgba(255,96,64,.3);
  background: rgba(255,96,64,.06);
}
.queue-commander {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  border: 1px dashed var(--border-bright);
  cursor: pointer;
  transition: all .15s;
}
.queue-commander:hover { border-color: var(--cyan); background: rgba(0,212,255,.04) }
.queue-commander--active {
  border-style: solid;
  border-color: #ffc830;
  background: rgba(255,200,48,.06);
  cursor: default;
}
.commander-icon { font-size: 10px }
.commander-text { font-size: 8px; color: var(--cyan-dim); letter-spacing: 1.5px; font-family: var(--ff-title) }
.queue-commander--active .commander-text { color: #ffc830 }

.commander-info {
  padding: 12px 14px;
  background: rgba(255,200,48,.03);
  border-bottom: 1px solid var(--border);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 14px;
}
.commander-info-text { font-size: 10px; color: var(--muted); line-height: 1.5 }
.commander-info-text strong { color: #ffc830 }
.commander-activate-btn {
  white-space: nowrap;
  padding: 6px 14px;
  border: 1px solid #ffc830;
  background: rgba(255,200,48,.08);
  color: #ffc830;
  font-family: var(--ff-title);
  font-size: 9px;
  letter-spacing: 1.5px;
  cursor: pointer;
  transition: all .15s;
}
.commander-activate-btn:hover { background: rgba(255,200,48,.15) }

.queue-items { padding: 6px 10px }
.queue-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 6px;
  border-bottom: 1px solid var(--border);
}
.queue-item:last-child { border-bottom: none }
.queue-item-index {
  width: 20px;
  height: 20px;
  border: 1px solid var(--border-bright);
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: var(--ff-title);
  font-size: 9px;
  color: var(--cyan);
  flex-shrink: 0;
}
.queue-item-icon { font-size: 16px; flex-shrink: 0 }
.queue-item-info { min-width: 0 }
.queue-item-name { font-size: 11px; color: var(--bright); font-weight: 700 }
.queue-item-level { font-size: 9px; color: var(--cyan-dim); font-family: var(--ff-title); letter-spacing: 1px }
.queue-item-timer { flex: 1; min-width: 100px; text-align: right }
.queue-item-time {
  font-family: var(--ff-title);
  font-size: 11px;
  color: var(--cyan);
  letter-spacing: 1px;
}
.queue-item-bar { height: 2px; background: var(--border); margin-top: 4px; border-radius: 1px }
.queue-item-bar-fill {
  height: 100%;
  background: linear-gradient(90deg, var(--cyan-dark), var(--cyan));
  border-radius: 1px;
  box-shadow: 0 0 4px rgba(0,212,255,.4);
  transition: width 1s linear;
}
.queue-item-bar-empty { height: 100%; background: transparent; border-radius: 1px }
.queue-item-waiting { font-size: 9px; color: var(--muted); font-family: var(--ff-title); letter-spacing: 1px }
.queue-item-cancel {
  width: 22px;
  height: 22px;
  border: 1px solid var(--border);
  background: transparent;
  color: var(--muted);
  font-size: 10px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: all .15s;
}
.queue-item-cancel:hover { border-color: #ff4030; color: #ff4030; background: rgba(255,64,48,.06) }

.queue-empty { padding: 14px; text-align: center }
.queue-empty-text { font-size: 10px; color: var(--muted); font-family: var(--ff-title); letter-spacing: 1px }

.queue-slot-dots {
  display: flex;
  gap: 4px;
  padding: 8px 14px;
  border-top: 1px solid var(--border);
}
.queue-dot {
  width: 8px;
  height: 4px;
  background: var(--border);
  transition: all .2s;
}
.queue-dot--active {
  background: var(--cyan);
  box-shadow: 0 0 6px rgba(0,212,255,.4);
}
.queue-dot--commander {
  border: 1px solid rgba(255,200,48,.3);
  background: rgba(255,200,48,.05);
}
.queue-dot--commander.queue-dot--active {
  background: #ffc830;
  box-shadow: 0 0 6px rgba(255,200,48,.4);
  border-color: #ffc830;
}

.filters { display: flex; gap: 6px; margin-bottom: 16px; flex-wrap: wrap }
.filter-btn {
  padding: 4px 12px;
  background: transparent;
  border: 1px solid var(--border);
  color: var(--muted);
  font-family: var(--ff);
  font-size: 9px;
  font-weight: 700;
  letter-spacing: 1.5px;
  cursor: pointer;
  text-transform: uppercase;
  transition: all .15s;
}
.filter-btn:hover { border-color: var(--cyan); color: var(--cyan) }
.filter-btn--active { border-color: var(--cyan); color: var(--cyan); background: var(--cyan-glow) }

.buildings-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(230px, 1fr)); gap: 10px }
.building-card {
  background: var(--bg2);
  border: 1px solid var(--border);
  padding: 0;
  cursor: pointer;
  text-align: left;
  font-family: var(--ff);
  display: flex;
  flex-direction: column;
  min-height: 170px;
  position: relative;
  overflow: hidden;
  transition: all .2s;
}
.building-card--unbuilt { border-style: dashed }
.building-card--upgrading { border-color: var(--cyan-dark) }
.building-card--locked { opacity: .92 }
.building-card--future {
  border-color: rgba(255,200,48,.25);
  background: linear-gradient(180deg, rgba(255,200,48,.03), rgba(0,0,0,.1));
}
.building-card:hover { border-color: var(--cyan); box-shadow: 0 0 20px var(--cyan-glow) }
.building-accent { height: 1px; background: linear-gradient(90deg, var(--border), transparent) }
.building-accent--active { background: linear-gradient(90deg, var(--cyan-dark), var(--cyan), var(--cyan-dark)) }
.building-body { padding: 14px; flex: 1; display: flex; flex-direction: column; gap: 6px }
.building-top { display: flex; justify-content: space-between; align-items: flex-start }
.building-info { display: flex; align-items: center; gap: 10px }
.building-icon { font-size: 22px }
.building-name { font-size: 12px; color: var(--bright); font-weight: 700 }
.building-name--dim { color: var(--muted) }
.building-cat { font-size: 7px; color: var(--cyan-dim); font-family: var(--ff-title); letter-spacing: 2px }
.building-level {
  font-family: var(--ff-title);
  font-size: 10px;
  color: var(--cyan);
  font-weight: 700;
  background: var(--cyan-glow);
  padding: 2px 10px;
  border: 1px solid var(--cyan-dim);
  letter-spacing: 1px;
}
.building-desc { font-size: 9px; color: var(--muted); line-height: 1.5 }
.building-prod { font-size: 9px; color: var(--green); font-family: var(--ff-title); letter-spacing: .5px }
.building-stats {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 2px;
}
.building-stats span {
  font-size: 8px;
  color: var(--cyan-dim);
  font-family: var(--ff-title);
  letter-spacing: 1px;
  border: 1px solid var(--border);
  padding: 2px 6px;
  background: rgba(0,212,255,.03);
}
.building-progress { margin-top: auto }
.building-progress-header { display: flex; justify-content: space-between; font-size: 9px; margin-bottom: 4px }
.building-progress-label { color: var(--cyan); font-weight: 700 }
.building-progress-time { color: var(--cyan); font-family: var(--ff-title); letter-spacing: 1px }
.building-progress-bar { height: 3px; background: var(--border); border-radius: 1px }
.building-progress-fill {
  height: 100%;
  background: linear-gradient(90deg, var(--cyan-dark), var(--cyan));
  border-radius: 1px;
  box-shadow: 0 0 6px rgba(0,212,255,.4);
  transition: width 1s linear;
}
.building-blueprint,
.building-locked,
.building-future {
  margin-top: auto;
  font-size: 9px;
  font-family: var(--ff-title);
  letter-spacing: 1px;
}
.building-blueprint { color: var(--cyan-dim) }
.building-locked {
  color: #ff9a40;
  background: rgba(255,154,64,.04);
  border: 1px solid rgba(255,154,64,.15);
  padding: 4px 8px;
}
.building-future {
  color: #ffc830;
  background: rgba(255,200,48,.05);
  border: 1px solid rgba(255,200,48,.18);
  padding: 4px 8px;
}
.building-queue-full {
  margin-top: auto;
  font-size: 8px;
  color: #ff6040;
  font-family: var(--ff-title);
  letter-spacing: 1px;
  padding: 4px 8px;
  background: rgba(255,96,64,.04);
  border: 1px solid rgba(255,96,64,.15);
}
</style>