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
              v-if="q.isActive && getQueueRemaining(q) <= 300"
              class="queue-item-free"
              @click.prevent="doInstantFinish(q)"
              title="Complete now for free!"
          >
            FREE
          </button>

          <button
              class="queue-item-cancel"
              @click.prevent="openCancelConfirm(q)"
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

    <!-- Cancel confirmation modal -->
    <div v-if="confirmCancel" class="cancel-overlay" @click.self="closeCancelConfirm">
      <div class="cancel-card">
        <div class="cancel-accent" />
        <div class="cancel-inner">

          <div class="cancel-header">
            <div class="cancel-header-text">
              <div class="cancel-title">CONFIRM CANCELLATION</div>
              <div class="cancel-subtitle">{{ confirmCancel.displayName }}</div>
            </div>
            <button class="cancel-close" @click="closeCancelConfirm">✕</button>
          </div>

          <div class="cancel-meta">
            <div class="cancel-meta-row">
              <span class="cancel-meta-label">TARGET LEVEL</span>
              <span class="cancel-meta-val">L{{ confirmCancel.toLevel ?? confirmCancel.targetLevel }}</span>
            </div>
            <div class="cancel-meta-row">
              <span class="cancel-meta-label">STATUS</span>
              <span class="cancel-meta-val" :class="confirmCancel.isActive ? 'status--active' : 'status--waiting'">
                {{ confirmCancel.isActive ? 'ACTIVE' : 'WAITING' }}
              </span>
            </div>
          </div>

          <div v-if="refundPreview(confirmCancel.cost)" class="cancel-refund-box">
            <div class="cancel-refund-title">REFUND PREVIEW — 75%</div>
            <div class="cancel-refund-grid">
              <div v-for="r in refundPreview(confirmCancel.cost)" :key="r.label" class="cancel-refund-item">
                <span class="refund-icon-label">{{ r.label }}</span>
                <span class="refund-amount">+{{ r.amount }}</span>
              </div>
            </div>
          </div>

          <div class="cancel-note" v-if="confirmCancel.isWaiting">
            Removes queued item in reverse order (highest level first). 75% of resources will be returned.
          </div>
          <div class="cancel-note" v-else-if="confirmCancel.isActive">
            Active construction will be stopped. 75% of resources will be returned.
          </div>

          <div class="cancel-actions">
            <button class="cancel-btn-confirm" @click="(async ()=>{ await cancelQueueItem(confirmCancel); closeCancelConfirm(); })()">
              CONFIRM CANCEL
            </button>
            <button class="cancel-btn-abort" @click="closeCancelConfirm">ABORT</button>
          </div>

        </div>
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
  instantFinishBuilding,
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
const queueFull = computed(() => queueItems.value.length >= queueLimit.value)

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

// Cancel confirmation state
const confirmCancel = ref(null) // will hold the queue item being confirmed

function openCancelConfirm(q) {
  confirmCancel.value = q
}

function closeCancelConfirm() {
  confirmCancel.value = null
}

const REFUND_KEYS = [
  { key: 'water',    label: '💧 Water' },
  { key: 'food',     label: '🌾 Food' },
  { key: 'scrap',    label: '⚙️ Scrap' },
  { key: 'fuel',     label: '⛽ Fuel' },
  { key: 'energy',   label: '⚡ Energy' },
  { key: 'rareTech', label: '🧬 RareTech' }
]

function refundPreview(cost) {
  if (!cost) return null
  const out = []
  for (const { key, label } of REFUND_KEYS) {
    // Accept both camelCase (water) and PascalCase (Water) from the backend
    const raw = cost[key] ?? cost[key[0].toUpperCase() + key.slice(1)] ?? 0
    const v = Number(raw) || 0
    if (v > 0) out.push({ label, amount: Math.ceil(v * 0.75) })
  }
  return out.length > 0 ? out : null
}

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

  // If the backend explicitly tagged the source (we tag Constructing/Waiting when merging),
  // prefer that mapping so a Constructing item never renders as WAITING. This acts as a
  // strong hint/override for ambiguous or inconsistent status fields coming from servers.
  let status = rawStatus
  if (raw.__queueSource) {
    if (raw.__queueSource === 'constructing') status = 'active'
    else if (raw.__queueSource === 'waiting') status = 'waiting'
  }

  // If status is still undefined, fall back to boolean hints and any textual status.
  if (!status) {
    if (hintActive) status = 'active'
    else if (hintWaiting) status = 'waiting'
    else if (hintCompleted) status = 'completed'
  }

  const isActive = status === 'active'
  const isWaiting = status === 'waiting'
  const isCompleted = status === 'completed'

  // Preserve any cost info the backend provided so we can show refund preview
  const cost = raw.cost ?? raw.Cost ?? raw.Costs ?? raw.upgradeCost ?? raw.UpgradeCost ?? null

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
    isCompleted,
    // raw cost data (may be null) and original source tag (if backend set it)
    cost,
    __queueSource: raw.__queueSource ?? null
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
      const constructingRaw = (queueData?.Constructing ?? queueData?.constructing ?? [])
        .map(i => ({ ...i, __queueSource: 'constructing' }))
      const waitingRaw = (queueData?.Waiting ?? queueData?.waiting ?? [])
        .map(i => ({ ...i, __queueSource: 'waiting' }))
      const fallback = (queueData?.Queue ?? queueData?.queue ?? [])

      // Combine giving priority to constructing then waiting, then fallback
      const combinedRaw = [].concat(constructingRaw, waitingRaw, fallback)

      // Normalize and de-duplicate by uniqueKey (preserve first-seen order)
      const normalized = combinedRaw.map(normalizeQueueItem)
      const seen = new Map()
      for (const it of normalized) {
        if (!it) continue
        if (seen.has(it.uniqueKey)) continue
        seen.set(it.uniqueKey, it)
      }

      // If backend tagged items as constructing/waiting, ensure the status reflects that
      queueItems.value = Array.from(seen.values()).map(q => {
        // If backend explicitly marked the source, prefer/force constructing -> active
        if (q.__queueSource === 'constructing') {
          q.status = 'active'
          q.isActive = true
          q.isWaiting = false
        } else if (q.__queueSource === 'waiting') {
          // Only set waiting if not already marked active
          if (!q.isActive) {
            q.status = 'waiting'
            q.isWaiting = true
          }
        }
        return q
      }).filter(q => !q.isCompleted)

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
      const targetLvl = typeof arg === 'object'
        ? (arg.toLevel ?? arg.targetLevel ?? null)
        : null
      await cancelBuilding(props.settlement.id, buildingType, targetLvl)
      await fetchBuildings()
    }

    if (props.refreshSettlement) {
      await props.refreshSettlement()
    }
  } catch (err) {
    const msg = err?.response?.data
    error.value = typeof msg === 'string' ? msg : (msg?.message ?? 'Cancel failed.')
  }
}

async function doInstantFinish() {
  if (!props.settlement?.id) return

  try {
    await instantFinishBuilding(props.settlement.id)
    await fetchBuildings()
    if (props.refreshSettlement) await props.refreshSettlement()
  } catch (err) {
    const msg = err?.response?.data
    error.value = typeof msg === 'string' ? msg : (msg?.message ?? 'Instant finish failed.')
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

.queue-item-free {
  flex-shrink: 0;
  padding: 3px 7px;
  border: 1px solid #3dff9c;
  background: rgba(61,255,156,.12);
  color: #3dff9c;
  font-family: var(--ff-title);
  font-size: 9px;
  letter-spacing: 1.5px;
  cursor: pointer;
  transition: all .15s;
  font-weight: 700;
}
.queue-item-free:hover { background: rgba(61,255,156,.25); box-shadow: 0 0 8px rgba(61,255,156,.3) }

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

/* Cancel confirmation modal */
.cancel-overlay {
  position: fixed; inset: 0;
  display: flex; align-items: center; justify-content: center;
  background: rgba(0,0,0,.85); z-index: 1200;
}
.cancel-card {
  background: var(--bg2);
  border: 1px solid var(--border-bright);
  width: 460px; max-width: 94vw;
  overflow: hidden;
  box-shadow: 0 0 60px rgba(255,96,64,.08);
}
.cancel-accent {
  height: 2px;
  background: linear-gradient(90deg, transparent, #ff6040, transparent);
}
.cancel-inner { padding: 28px 32px; }
.cancel-header {
  display: flex; align-items: flex-start;
  justify-content: space-between; gap: 12px;
  margin-bottom: 24px;
}
.cancel-title {
  font-size: 8px; color: #ff6040;
  font-family: var(--ff-title); letter-spacing: 3px; font-weight: 700;
  margin-bottom: 6px;
}
.cancel-subtitle {
  font-size: 18px; color: var(--text);
  font-family: var(--ff-title); font-weight: 700; letter-spacing: 1px;
}
.cancel-close {
  background: var(--bg3); border: 1px solid var(--border);
  color: var(--muted); cursor: pointer;
  padding: 4px 12px; font-size: 11px; font-family: var(--ff);
  flex-shrink: 0;
}
.cancel-close:hover { border-color: #ff6040; color: #ff6040; }
.cancel-meta {
  display: flex; gap: 24px; margin-bottom: 20px;
  padding-bottom: 20px; border-bottom: 1px solid var(--border);
}
.cancel-meta-row { display: flex; flex-direction: column; gap: 4px; }
.cancel-meta-label {
  font-size: 8px; color: var(--muted);
  font-family: var(--ff-title); letter-spacing: 2px;
}
.cancel-meta-val {
  font-size: 13px; color: var(--text);
  font-family: var(--ff-title); font-weight: 700;
}
.status--active { color: #ff6040; }
.status--waiting { color: var(--cyan); }
.cancel-refund-box {
  background: var(--bg3); border: 1px solid var(--border);
  padding: 14px 16px; margin-bottom: 16px;
}
.cancel-refund-title {
  font-size: 8px; color: var(--cyan);
  font-family: var(--ff-title); letter-spacing: 2px; font-weight: 700;
  margin-bottom: 12px;
}
.cancel-refund-grid { display: flex; gap: 12px; flex-wrap: wrap; }
.cancel-refund-item {
  display: flex; flex-direction: column; gap: 2px;
  padding: 8px 12px; background: var(--bg2); border: 1px solid var(--border);
  min-width: 80px;
}
.refund-icon-label { font-size: 10px; color: var(--muted); }
.refund-amount { font-size: 14px; color: var(--green); font-family: var(--ff-title); font-weight: 700; }
.cancel-note {
  font-size: 11px; color: var(--muted); line-height: 1.6;
  margin-bottom: 20px;
}
.cancel-actions { display: flex; gap: 10px; }
.cancel-btn-confirm {
  flex: 1; padding: 12px;
  background: rgba(255,96,64,.1); border: 1px solid #ff6040;
  color: #ff6040; font-family: var(--ff-title); font-size: 11px;
  font-weight: 700; letter-spacing: 2px; cursor: pointer;
  transition: all .2s;
}
.cancel-btn-confirm:hover { background: rgba(255,96,64,.2); }
.cancel-btn-abort {
  flex: 1; padding: 12px;
  background: var(--bg3); border: 1px solid var(--border);
  color: var(--text); font-family: var(--ff-title); font-size: 11px;
  font-weight: 700; letter-spacing: 2px; cursor: pointer;
  transition: all .2s;
}
.cancel-btn-abort:hover { border-color: var(--cyan-dim); color: var(--cyan); }
</style>