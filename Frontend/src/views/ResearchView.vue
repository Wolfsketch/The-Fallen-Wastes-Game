<template>
  <div class="research-page">
    <div class="page-header">
      <div>
        <h1 class="page-title">TECH LAB</h1>
        <p class="page-subtitle">Technology advancement, salvage recovery & strategic unlocks</p>
      </div>

      <div class="page-badges" v-if="!loading">
        <div class="page-badge">
          {{ completedResearch.length }} / {{ allResearch.length }} COMPLETE
        </div>
        <div class="page-badge page-badge--raretech">
          🧬 {{ currentRareTech.toLocaleString() }} RARE TECH
        </div>
      </div>
    </div>

    <div class="research-tabs" v-if="!loading && hasResearchLab">
      <button
          class="research-tab"
          :class="{ 'research-tab--active': activeTab === 'lab' }"
          @click="activeTab = 'lab'"
      >
        TECH LAB
      </button>

      <button
          class="research-tab"
          :class="{ 'research-tab--active': activeTab === 'salvager' }"
          @click="activeTab = 'salvager'"
      >
        TECH SALVAGER
      </button>
    </div>

    <div v-if="loading" class="research-empty">
      <div class="empty-core">◌</div>
      <div class="empty-title">LOADING RESEARCH DATA</div>
      <div class="empty-subtitle">Accessing classified tech archives...</div>
    </div>

    <div v-else-if="error" class="research-empty research-empty--error">
      <div class="empty-core">!</div>
      <div class="empty-title">DATA LINK FAILURE</div>
      <div class="empty-subtitle">{{ error }}</div>
    </div>

    <div v-else-if="!hasResearchLab" class="research-empty">
      <div class="empty-core">🔒</div>
      <div class="empty-title">TECH LAB REQUIRED</div>
      <div class="empty-subtitle">
        Build a <strong>Tech Lab</strong> to unlock advanced technologies and strategic progression.
      </div>
      <div class="empty-hint">
        Research unlocks stronger systems, logistics upgrades, military specialization and expansion tools.
      </div>
    </div>

    <template v-else>
      <!-- TECH LAB TAB -->
      <template v-if="activeTab === 'lab'">
        <div class="queue-panel">
          <div class="queue-header">
            <div class="queue-title-row">
              <span class="queue-title">RESEARCH QUEUE</span>
              <span class="queue-slots" :class="{ 'queue-slots--full': researchQueueFull }">
                {{ researchQueue.length }} / {{ researchQueueLimit }}
              </span>
            </div>

            <div
                class="queue-commander"
                v-if="!researchCommanderActive"
                @click="showResearchCommanderInfo = !showResearchCommanderInfo"
            >
              <span class="commander-icon">⭐</span>
              <span class="commander-text">TECH PRIEST: +2 SLOTS</span>
            </div>

            <div class="queue-commander queue-commander--active" v-else>
              <span class="commander-icon">⭐</span>
              <span class="commander-text">TECH PRIEST ACTIVE</span>
            </div>
          </div>

            <div class="commander-info" v-if="showResearchCommanderInfo && !researchCommanderActive">
              <div class="commander-info-text">
                Activate a <strong>Tech Priest</strong> to expand your research queue from 1 to 3 simultaneous projects.
              </div>
              <button class="commander-activate-btn" @click="activateResearchCommander">
                ACTIVATE TECH PRIEST (14 DAYS)
              </button>
            </div>

          <div class="queue-items" v-if="researchQueue.length > 0">
            <div
                v-for="(q, idx) in researchQueue"
                :key="q.key"
                class="queue-item"
            >
              <div class="queue-item-index">{{ idx + 1 }}</div>
              <span class="queue-item-icon">{{ getResearchIcon(q.key) }}</span>

              <div class="queue-item-info">
                <div class="queue-item-name">{{ q.name }}</div>
                <div class="queue-item-level">{{ q.branch }}</div>
              </div>

              <div class="queue-item-timer">
                <div class="queue-item-time">{{ formatTime(q.remainingSeconds || 0) }}</div>
                <div class="queue-item-bar">
                  <div class="queue-item-bar-fill" :style="{ width: getResearchPct(q) + '%' }" />
                </div>
              </div>

              <button class="queue-item-cancel" @click="cancelResearchItem(q.key)" title="Cancel">✕</button>
            </div>
          </div>

          <div class="queue-empty" v-else>
            <span class="queue-empty-text">No active research — {{ researchQueueLimit }} slot(s) available</span>
          </div>

          <div class="queue-slot-dots">
            <div
                v-for="i in researchQueueLimit"
                :key="i"
                class="queue-dot"
                :class="{
                'queue-dot--active': i <= researchQueue.length,
                'queue-dot--commander': i > 1
              }"
            />
          </div>
        </div>

        <div class="research-topbar">
          <div class="research-filters">
            <button
                v-for="cat in categories"
                :key="cat.key"
                class="filter-btn"
                :class="{ 'filter-btn--active': categoryFilter === cat.key }"
                @click="categoryFilter = cat.key"
            >
              {{ cat.label }}
            </button>
          </div>

          <div class="research-points-box">
            <span class="research-points-label">RESEARCH POINTS</span>
            <span class="research-points-value">{{ researchPoints }}</span>
          </div>
        </div>

        <div v-if="!filteredResearch.length" class="research-empty research-empty--small">
          <div class="empty-core">◌</div>
          <div class="empty-title">NO MATCHING RESEARCH</div>
          <div class="empty-subtitle">Adjust filters or upgrade your Tech Lab.</div>
        </div>

        <div v-else class="research-grid">
          <article
              v-for="research in filteredResearch"
              :key="research.key"
              class="research-card"
              :class="{
              'research-card--completed': research.isUnlocked,
              'research-card--locked': isResearchLocked(research),
              'research-card--researching': research.isResearching,
              'research-card--future': research.isFutureFeature
            }"
          >
            <div class="research-status-badge" v-if="research.isUnlocked">✓ COMPLETE</div>
            <div class="research-status-badge research-status-badge--active" v-else-if="research.isResearching">⚡ RESEARCHING</div>
            <div class="research-status-badge research-status-badge--future" v-else-if="research.isFutureFeature">⏳ FUTURE</div>
            <div class="research-status-badge research-status-badge--locked" v-else-if="isResearchLocked(research)">🔒 LOCKED</div>

            <div class="research-header">
              <span class="research-icon">{{ getResearchIcon(research.key) }}</span>
              <div class="research-title-wrap">
                <div class="research-name">{{ research.name }}</div>
                <div class="research-category">{{ research.branch }}</div>
              </div>
            </div>

            <p class="research-description">{{ research.description }}</p>

            <div class="research-requirements" v-if="hasRequirements(research)">
              <div class="requirements-title">REQUIRES:</div>
              <div class="requirements-list">
                <div
                    class="requirement-item"
                    :class="{ 'requirement-item--met': research.requirements?.techLabMet }"
                >
                  <span class="requirement-icon">{{ research.requirements?.techLabMet ? '✓' : '✕' }}</span>
                  <span class="requirement-text">
                    Tech Lab Level {{ research.requiredTechLabLevel }}
                    <span class="requirement-note">(current: {{ techLabLevel }})</span>
                  </span>
                </div>

                <div
                    v-for="reqKey in research.requiredResearchKeys || []"
                    :key="reqKey"
                    class="requirement-item"
                    :class="{ 'requirement-item--met': isResearchCompleted(reqKey) }"
                >
                  <span class="requirement-icon">{{ isResearchCompleted(reqKey) ? '✓' : '✕' }}</span>
                  <span class="requirement-text">{{ getResearchName(reqKey) }}</span>
                </div>

                <div
                    v-for="salvageKey in research.requiredSalvageItems || []"
                    :key="salvageKey"
                    class="requirement-item"
                    :class="{ 'requirement-item--met': hasSalvageRequirement(salvageKey) }"
                >
                  <span class="requirement-icon">{{ hasSalvageRequirement(salvageKey) ? '✓' : '○' }}</span>
                  <span class="requirement-text">{{ formatSalvageKey(salvageKey) }}</span>
                </div>
              </div>
            </div>

            <div class="research-cost">
              <div class="cost-title">RESEARCH COST</div>
              <div class="cost-grid">
                <div class="cost-item" :class="{ 'cost-item--insufficient': currentRareTech < (research.rareTechCost ?? 0) }">
                  <img :src="resRareTechIcon" class="cost-icon" alt="RareTech" />
                  <span>{{ research.rareTechCost ?? 0 }}</span>
                </div>
                <div class="cost-item">
                  <span class="cost-icon">📘</span>
                  <span>{{ research.researchPointCost ?? 0 }}</span>
                </div>
              </div>
            </div>

            <div class="research-time">
              <span class="time-icon">⏱️</span>
              <span class="time-value">{{ formatResearchTime(research.baseDurationSeconds) }}</span>
            </div>

            <div class="research-actions">
              <button
                  v-if="research.isUnlocked"
                  class="research-btn research-btn--completed"
                  disabled
              >
                COMPLETED
              </button>

              <button
                  v-else-if="research.isResearching"
                  class="research-btn research-btn--researching"
                  disabled
              >
                IN PROGRESS
              </button>

              <button
                  v-else-if="research.isFutureFeature"
                  class="research-btn research-btn--locked"
                  disabled
              >
                FUTURE FEATURE
              </button>

              <button
                  v-else-if="researchQueueFull && !research.canStart"
                  class="research-btn research-btn--disabled"
                  disabled
              >
                QUEUE FULL
              </button>

              <button
                  v-else
                  class="research-btn"
                  @click="startSelectedResearch(research)"
                  :disabled="!research.canStart"
              >
                START RESEARCH
              </button>
            </div>
          </article>
        </div>
      </template>

      <!-- TECH SALVAGER TAB -->
      <template v-else>
        <div class="salvager-layout">
          <section class="salvager-panel">
            <div class="salvager-head">
              <div>
                <div class="salvager-kicker">Recovery Matrix</div>
                <h2 class="salvager-title">Salvage Inventory</h2>
              </div>
              <div class="salvager-badge">{{ salvageItems.length }} ENTRIES</div>
            </div>

            <div class="salvager-summary">
              <div class="salvager-stat">
                <span class="salvager-stat-label">Total Items</span>
                <strong class="salvager-stat-value">{{ salvageTotalQuantity }}</strong>
              </div>
              <div class="salvager-stat">
                <span class="salvager-stat-label">Potential Yield</span>
                <strong class="salvager-stat-value">{{ salvageTotalYield }}</strong>
              </div>
              <div class="salvager-stat">
                <span class="salvager-stat-label">Rare Tech</span>
                <strong class="salvager-stat-value">{{ currentRareTech }}</strong>
              </div>
            </div>

            <div v-if="filteredSalvageItems.length === 0" class="research-empty research-empty--small">
              <div class="empty-core">◌</div>
              <div class="empty-title">NO SALVAGE STOCKPILES</div>
              <div class="empty-subtitle">Recover event loot and wasteland relics to populate this inventory.</div>
            </div>

            <div v-else class="salvage-list">
              <article
                  v-for="item in filteredSalvageItems"
                  :key="item.key"
                  class="salvage-item"
                  :class="{ 'salvage-item--selected': selectedSalvageKey === item.key }"
                  @click="selectedSalvageKey = item.key"
              >
                <div class="salvage-item-left">
                  <div class="salvage-item-icon">{{ item.icon }}</div>
                  <div class="salvage-item-main">
                    <div class="salvage-item-name">{{ item.name }}</div>
                    <div class="salvage-item-meta">
                      <span>{{ item.category }}</span>
                      <span>•</span>
                      <span>{{ item.rarity.toUpperCase() }}</span>
                    </div>
                  </div>
                </div>

                <div class="salvage-item-right">
                  <div class="salvage-item-qty">×{{ item.quantity }}</div>
                  <div class="salvage-item-yield">+{{ item.salvageYield * item.quantity }}</div>
                </div>
              </article>
            </div>
          </section>

          <section class="salvager-panel salvager-panel--detail">
            <div class="salvager-head">
              <div>
                <div class="salvager-kicker">Processing Chamber</div>
                <h2 class="salvager-title">Item Detail</h2>
              </div>
            </div>

            <template v-if="selectedSalvage">
              <div class="salvage-detail">
                <div class="salvage-detail-icon">{{ selectedSalvage.icon }}</div>
                <div class="salvage-detail-name">{{ selectedSalvage.name }}</div>
                <div class="salvage-detail-desc">{{ selectedSalvage.description }}</div>

                <div class="salvage-detail-grid">
                  <div class="salvage-detail-stat">
                    <span>Category</span>
                    <strong>{{ selectedSalvage.category }}</strong>
                  </div>
                  <div class="salvage-detail-stat">
                    <span>Rarity</span>
                    <strong>{{ selectedSalvage.rarity.toUpperCase() }}</strong>
                  </div>
                  <div class="salvage-detail-stat">
                    <span>Stored</span>
                    <strong>{{ selectedSalvage.quantity }}</strong>
                  </div>
                  <div class="salvage-detail-stat">
                    <span>Yield / Item</span>
                    <strong>{{ selectedSalvage.salvageYield }}</strong>
                  </div>
                </div>

                <div class="salvage-control">
                  <div class="cost-title">SALVAGE CONTROL</div>

                  <div class="train-qty-box">
                    <button class="qty-btn" @click="decreaseSalvageQuantity" :disabled="salvageQuantity <= 1" type="button">-</button>
                    <input class="qty-input" type="number" min="1" :max="selectedSalvage.quantity" :value="salvageQuantity" @input="setSalvageQuantity($event.target.value)" />
                    <button class="qty-btn" @click="increaseSalvageQuantity" :disabled="salvageQuantity >= selectedSalvage.quantity" type="button">+</button>
                    <button class="qty-btn qty-btn--max" @click="setMaxSalvageQuantity" type="button">MAX</button>
                  </div>

                  <div class="salvage-yield-preview">
                    <span class="salvage-yield-label">Projected Rare Tech Yield</span>
                    <strong class="salvage-yield-value">+{{ selectedSalvage.salvageYield * salvageQuantity }}</strong>
                  </div>

                  <button
                      class="research-btn"
                      :disabled="selectedSalvage.quantity <= 0"
                      @click="processSelectedSalvage"
                  >
                    PROCESS SALVAGE
                  </button>
                </div>
              </div>
            </template>

            <div v-else class="panel-empty">
              SELECT A SALVAGE ENTRY TO VIEW RECOVERY DETAILS
            </div>
          </section>
        </div>
      </template>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import {
  getBuildings,
  getResearchState,
  startResearch,
  cancelResearch,
  completeReadyResearch,
  activateAdvisor,
  getSalvageInventory,
  processSalvageItem
} from '../services/api'
import resRareTechIcon from '../images/Resources/RareTech.png'

const props = defineProps({
  player: Object,
  settlement: Object,
  refreshSettlement: Function,
  liveResources: {
    type: Object,
    default: () => ({})
  }
})

const loading = ref(true)
const error = ref('')
const buildings = ref([])
const researchState = ref(null)
const categoryFilter = ref('all')
const now = ref(Date.now())
let tickInterval = null

const activeTab = ref('lab')
const showResearchCommanderInfo = ref(false)

const salvageItems = ref([])

function mapSalvageItem(i) {
  const key = (i.key ?? i.Key ?? '').toLowerCase()
  const name = i.name ?? i.Name ?? key

  // Derive category from key
  let category = 'Component'
  if (key.includes('datacore')) category = 'Datacore'
  else if (key.includes('schematic')) category = 'Schematic'
  else if (key.includes('fragment')) category = 'Fragment'
  else if (key.includes('artifact')) category = 'Artifact'

  // Derive icon from key
  const iconMap = {
    encrypted_datacore: '💾',
    ancient_data_core: '💽',
    prototype_schematic: '📐',
    reactor_fragment: '⚛️',
    vault_artifact: '✦',
    cracked_circuit_board: '📟',
    burned_power_cell: '🔋',
    scrap_bundle: '🔩',
    fractured_optics_module: '🔭',
    damaged_servo_bundle: '⚙️',
    broken_drone_core: '🤖',
    pre_war_guidance_chip: '🧭',
  }
  const icon = iconMap[key] ?? '🔬'

  return {
    key,
    name,
    description: i.description ?? i.Description ?? '',
    category,
    rarity: (i.rarity ?? i.Rarity ?? 'common').toLowerCase(),
    quantity: i.quantity ?? i.Quantity ?? 0,
    salvageYield: i.rareTechYield ?? i.RareTechYield ?? 0,
    baseSalvageTimeSeconds: i.baseSalvageTimeSeconds ?? i.BaseSalvageTimeSeconds ?? 120,
    icon
  }
}

const selectedSalvageKey = ref('')
const salvageQuantity = ref(1)

const researchCommanderActive = computed(() =>
    // Prefer current "techPriest" advisor key; fall back to legacy "scientist" key for compatibility
    props.player?.advisors?.techPriest?.active ?? props.player?.advisors?.scientist?.active ?? false
)

const researchQueueLimit = computed(() => {
    // Advisor-derived expected limit
    const advisorLimit = researchCommanderActive.value ? 3 : 1

    // Prefer backend-provided value when it increases the capacity,
    // but if backend returns a smaller value (e.g. 1) and the player has an active Tech Priest,
    // show the higher advisor-derived limit so the UI matches player-level effects.
    const backendLimit = researchState.value?.maxConcurrentResearches ?? researchState.value?.MaxConcurrentResearches
    if (Number.isInteger(backendLimit) && backendLimit > 0) {
      return Math.max(backendLimit, advisorLimit)
    }

    // No backend limit provided: rely on advisor-derived value
    return advisorLimit
})

const researchQueue = computed(() => {
  const researches = researchState.value?.researches || []
  return researches.filter(r => r.isResearching)
})

const researchQueueFull = computed(() =>
    researchQueue.value.length >= researchQueueLimit.value
)

const techLabLevel = computed(() =>
    researchState.value?.techLabLevel ?? 0
)

const researchPoints = computed(() =>
    researchState.value?.researchPoints ?? 0
)

const currentRareTech = computed(() =>
    props.liveResources?.rareTech
    ?? props.settlement?.rareTech
    ?? props.settlement?.resources?.rareTech
    ?? 0
)

const categories = computed(() => {
  const base = [{ key: 'all', label: 'ALL' }]
  const branches = Array.from(new Set((allResearch.value || []).map(r => r.branch).filter(Boolean)))
  return [...base, ...branches.map(b => ({ key: b, label: String(b).toUpperCase() }))]
})

const allResearch = computed(() =>
    researchState.value?.researches || []
)

const completedResearch = computed(() =>
    allResearch.value.filter(r => r.isUnlocked)
)

const hasResearchLab = computed(() => {
  const lab = buildings.value.find(b => b.type === 'TechLab')
  return !!(lab && lab.level > 0)
})

const filteredResearch = computed(() => {
  let research = allResearch.value

  if (categoryFilter.value !== 'all') {
    research = research.filter(r => String(r.branch).toLowerCase() === String(categoryFilter.value).toLowerCase())
  }

  return research
})

const filteredSalvageItems = computed(() => salvageItems.value.filter(item => item.quantity > 0))

const selectedSalvage = computed(() => {
  return salvageItems.value.find(item => item.key === selectedSalvageKey.value) || null
})

const salvageTotalQuantity = computed(() =>
    salvageItems.value.reduce((sum, item) => sum + item.quantity, 0)
)

const salvageTotalYield = computed(() =>
    salvageItems.value.reduce((sum, item) => sum + (item.quantity * item.salvageYield), 0)
)

function normalizeBuilding(raw) {
  return {
    type: raw.type ?? raw.Type ?? '',
    level: raw.level ?? raw.Level ?? 0
  }
}

function normalizeResearchState(raw) {
  const researches = (raw?.researches || raw?.Researches || []).map(r => ({
    key: r.key ?? r.Key ?? '',
    name: r.name ?? r.Name ?? '',
    description: r.description ?? r.Description ?? '',
    branch: r.branch ?? r.Branch ?? '',
    requiredTechLabLevel: r.requiredTechLabLevel ?? r.RequiredTechLabLevel ?? 0,
    rareTechCost: r.rareTechCost ?? r.RareTechCost ?? 0,
    researchPointCost: r.researchPointCost ?? r.ResearchPointCost ?? 0,
    baseDurationSeconds: r.baseDurationSeconds ?? r.BaseDurationSeconds ?? 0,
    requiredResearchKeys: r.requiredResearchKeys ?? r.RequiredResearchKeys ?? [],
    requiredSalvageItems: r.requiredSalvageItems ?? r.RequiredSalvageItems ?? [],
    isFutureFeature: r.isFutureFeature ?? r.IsFutureFeature ?? false,
    isUnlocked: r.isUnlocked ?? r.IsUnlocked ?? false,
    isResearching: r.isResearching ?? r.IsResearching ?? false,
    remainingSeconds: r.remainingSeconds ?? r.RemainingSeconds ?? 0,
    requirements: r.requirements ?? r.Requirements ?? {},
    canStart: r.canStart ?? r.CanStart ?? false
  }))

  return {
    settlementId: raw?.settlementId ?? raw?.SettlementId ?? null,
    techLabLevel: raw?.techLabLevel ?? raw?.TechLabLevel ?? 0,
    researchPoints: raw?.researchPoints ?? raw?.ResearchPoints ?? 0,
    maxConcurrentResearches: raw?.maxConcurrentResearches ?? raw?.MaxConcurrentResearches ?? 1,
    availableResearchSlots: raw?.availableResearchSlots ?? raw?.AvailableResearchSlots ?? 0,
    researches
  }
}

function hasRequirements(research) {
  return (
      research.requiredTechLabLevel > 0 ||
      (research.requiredResearchKeys && research.requiredResearchKeys.length > 0) ||
      (research.requiredSalvageItems && research.requiredSalvageItems.length > 0)
  )
}

function isResearchCompleted(key) {
  return allResearch.value.some(r => r.key === key && r.isUnlocked)
}

function getResearchName(key) {
  return allResearch.value.find(r => r.key === key)?.name || key
}

function hasSalvageRequirement(key) {
  return salvageItems.value.some(item => item.key === key && item.quantity > 0)
}

function formatSalvageKey(key) {
  return salvageItems.value.find(item => item.key === key)?.name || key
}

function isResearchLocked(research) {
  if (research.isUnlocked) return false
  if (research.isResearching) return false
  if (research.isFutureFeature) return true
  return !research.canStart
}

function getResearchIcon(key) {
  const map = {
    rationing_protocols: '🥫',
    water_recycling: '💧',
    scrap_sorting: '⚙️',
    improved_ballistics: '🔫',
    impact_plating: '🛡️',
    energy_focusing: '⚡',
    fortified_barriers: '🧱',
    emergency_response_drills: '🚨',
    convoy_protocols: '🚚',
    field_logistics: '📦',
    salvage_optimization: '🔬',
    datacore_reconstruction: '💾',
    settlement_coordination: '🏛️'
  }
  return map[key] || '🔬'
}

function formatResearchTime(seconds) {
  if (!seconds) return '--'
  const mins = Math.floor(seconds / 60)
  const hours = Math.floor(mins / 60)
  const days = Math.floor(hours / 24)

  if (days > 0) return `${days}d ${hours % 24}h`
  if (hours > 0) return `${hours}h ${mins % 60}m`
  return `${mins}m`
}

function formatTime(sec) {
  if (sec <= 0) return 'Done!'
  const h = Math.floor(sec / 3600)
  const m = Math.floor((sec % 3600) / 60)
  const s = sec % 60
  return `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`
}

function getResearchPct(q) {
  const total = q.baseDurationSeconds || 1
  const remaining = q.remainingSeconds || 0
  return Math.min(100, Math.max(0, ((total - remaining) / total) * 100))
}

function ensureSelectedSalvage() {
  if (!selectedSalvageKey.value || !salvageItems.value.some(x => x.key === selectedSalvageKey.value && x.quantity > 0)) {
    selectedSalvageKey.value = filteredSalvageItems.value[0]?.key ?? ''
    salvageQuantity.value = 1
  }
}

function setSalvageQuantity(value) {
  const max = selectedSalvage.value?.quantity ?? 1
  salvageQuantity.value = Math.max(1, Math.min(max, parseInt(value, 10) || 1))
}

function increaseSalvageQuantity() {
  const max = selectedSalvage.value?.quantity ?? 1
  salvageQuantity.value = Math.min(max, salvageQuantity.value + 1)
}

function decreaseSalvageQuantity() {
  salvageQuantity.value = Math.max(1, salvageQuantity.value - 1)
}

function setMaxSalvageQuantity() {
  salvageQuantity.value = selectedSalvage.value?.quantity ?? 1
}

async function fetchResearchData() {
  if (!props.settlement?.id) return

  try {
    const [buildingsData, researchData, salvageData] = await Promise.all([
      getBuildings(props.settlement.id),
      getResearchState(props.settlement.id),
      getSalvageInventory(props.settlement.id).catch(() => null)
    ])

    buildings.value = (buildingsData || []).map(normalizeBuilding)
    researchState.value = normalizeResearchState(researchData)

    if (salvageData?.items) {
      salvageItems.value = salvageData.items.map(mapSalvageItem).filter(i => i.quantity > 0)
    }

    ensureSelectedSalvage()
    error.value = ''
  } catch (err) {
    error.value = err?.response?.data || 'Unable to load research data.'
    console.error(err)
  } finally {
    loading.value = false
  }
}

async function startSelectedResearch(research) {
  if (!props.settlement?.id || !research?.key) return

  try {
    await startResearch(props.settlement.id, research.key)
    await fetchResearchData()

    if (props.refreshSettlement) {
      await props.refreshSettlement()
    }
  } catch (err) {
    error.value = err?.response?.data || 'Research failed to start.'
  }
}

async function cancelResearchItem(researchKey) {
  if (!props.settlement?.id) return

  try {
    await cancelResearch(props.settlement.id, researchKey)
    await fetchResearchData()

    if (props.refreshSettlement) {
      await props.refreshSettlement()
    }
  } catch (err) {
    error.value = err?.response?.data || 'Cancel failed.'
  }
}

async function activateResearchCommander() {
  if (!props.player?.id) return

  try {
    await activateAdvisor(props.player.id, 'techpriest', 14)
    showResearchCommanderInfo.value = false

    if (props.refreshSettlement) {
      await props.refreshSettlement()
    }
  } catch (err) {
    error.value = err?.response?.data || 'Tech Priest activation failed.'
  }
}

async function processSelectedSalvage() {
  if (!selectedSalvage.value || !props.settlement?.id) return

  const item = selectedSalvage.value
  const amount = Math.min(salvageQuantity.value, item.quantity)
  if (amount <= 0) return

  try {
    await processSalvageItem(props.settlement.id, item.key, amount)
    salvageQuantity.value = 1
    await fetchResearchData()
    if (props.refreshSettlement) await props.refreshSettlement()
  } catch (err) {
    error.value = err?.response?.data || 'Failed to process salvage item.'
  }
}

onMounted(async () => {
  if (!props.settlement?.id) {
    loading.value = false
    error.value = 'No settlement loaded'

    const stop = watch(
        () => props.settlement?.id,
        async newId => {
          if (newId) {
            stop()
            loading.value = true
            error.value = ''
            await fetchResearchData()
          }
        }
    )
    return
  }

  await fetchResearchData()

  tickInterval = setInterval(async () => {
    now.value = Date.now()

    if (researchQueue.value.some(q => (q.remainingSeconds || 0) <= 0)) {
      try {
        await completeReadyResearch(props.settlement.id)
      } catch (_) {
        // ignore transient completion errors, next refresh will reconcile
      }

      await fetchResearchData()

      if (props.refreshSettlement) {
        await props.refreshSettlement()
      }
    } else if (researchQueue.value.length > 0) {
      await fetchResearchData()
    }
  }, 1000)
})

onUnmounted(() => {
  if (tickInterval) clearInterval(tickInterval)
})
</script>

<style scoped>
.research-page{min-height:100%;color:var(--bright)}
.page-header{display:flex;justify-content:space-between;align-items:flex-start;gap:16px;margin-bottom:18px;padding-bottom:14px;border-bottom:1px solid var(--border)}
.page-title{margin:0;font-family:var(--ff-title);font-size:34px;letter-spacing:4px;color:var(--cyan);text-shadow:0 0 14px rgba(0,212,255,.2)}
.page-subtitle{margin:6px 0 0;font-size:11px;letter-spacing:2px;text-transform:uppercase;color:var(--muted)}
.page-badge{padding:8px 12px;border:1px solid var(--border-bright);background:linear-gradient(180deg,rgba(0,212,255,.08),rgba(0,212,255,.02));color:var(--cyan);font-family:var(--ff-title);font-size:11px;letter-spacing:2px;white-space:nowrap}

.queue-panel{background:linear-gradient(180deg,rgba(0,212,255,.03),rgba(0,212,255,.01));border:1px solid var(--border-bright);margin-bottom:18px;overflow:hidden}
.queue-header{display:flex;justify-content:space-between;align-items:center;padding:10px 14px;background:linear-gradient(90deg,rgba(0,212,255,.06),transparent);border-bottom:1px solid var(--border)}
.queue-title-row{display:flex;align-items:center;gap:10px}
.queue-title{font-family:var(--ff-title);font-size:10px;color:var(--cyan);letter-spacing:2px;font-weight:700}
.queue-slots{font-family:var(--ff-title);font-size:11px;color:var(--cyan);letter-spacing:1px;padding:2px 8px;border:1px solid var(--border-bright);background:rgba(0,212,255,.06)}
.queue-slots--full{color:#ff6040;border-color:rgba(255,96,64,.3);background:rgba(255,96,64,.06)}
.queue-commander{display:flex;align-items:center;gap:6px;padding:4px 10px;border:1px dashed var(--border-bright);cursor:pointer;transition:all .15s}
.queue-commander:hover{border-color:var(--cyan);background:rgba(0,212,255,.04)}
.queue-commander--active{border-style:solid;border-color:#ffc830;background:rgba(255,200,48,.06);cursor:default}
.commander-icon{font-size:10px}
.commander-text{font-size:8px;color:var(--cyan-dim);letter-spacing:1.5px;font-family:var(--ff-title)}
.queue-commander--active .commander-text{color:#ffc830}
.commander-info{padding:12px 14px;background:rgba(255,200,48,.03);border-bottom:1px solid var(--border);display:flex;align-items:center;justify-content:space-between;gap:14px}
.commander-info-text{font-size:10px;color:var(--muted);line-height:1.5}
.commander-info-text strong{color:#ffc830}
.commander-activate-btn{white-space:nowrap;padding:6px 14px;border:1px solid #ffc830;background:rgba(255,200,48,.08);color:#ffc830;font-family:var(--ff-title);font-size:9px;letter-spacing:1.5px;cursor:pointer;transition:all .15s}
.commander-activate-btn:hover{background:rgba(255,200,48,.15)}
.queue-items{padding:6px 10px}
.queue-item{display:flex;align-items:center;gap:10px;padding:8px 6px;border-bottom:1px solid var(--border)}
.queue-item:last-child{border-bottom:none}
.queue-item-index{width:20px;height:20px;border:1px solid var(--border-bright);display:flex;align-items:center;justify-content:center;font-family:var(--ff-title);font-size:9px;color:var(--cyan);flex-shrink:0}
.queue-item-icon{font-size:16px;flex-shrink:0}
.queue-item-info{min-width:0}
.queue-item-name{font-size:11px;color:var(--bright);font-weight:700}
.queue-item-level{font-size:9px;color:var(--cyan-dim);font-family:var(--ff-title);letter-spacing:1px}
.queue-item-timer{flex:1;min-width:100px;text-align:right}
.queue-item-time{font-family:var(--ff-title);font-size:11px;color:var(--cyan);letter-spacing:1px}
.queue-item-bar{height:2px;background:var(--border);margin-top:4px;border-radius:1px}
.queue-item-bar-fill{height:100%;background:linear-gradient(90deg,var(--cyan-dark),var(--cyan));border-radius:1px;box-shadow:0 0 4px rgba(0,212,255,.4);transition:width 1s linear}
.queue-item-cancel{width:22px;height:22px;border:1px solid var(--border);background:transparent;color:var(--muted);font-size:10px;cursor:pointer;display:flex;align-items:center;justify-content:center;flex-shrink:0;transition:all .15s}
.queue-item-cancel:hover{border-color:#ff4030;color:#ff4030;background:rgba(255,64,48,.06)}
.queue-empty{padding:14px;text-align:center}
.queue-empty-text{font-size:10px;color:var(--muted);font-family:var(--ff-title);letter-spacing:1px}
.queue-slot-dots{display:flex;gap:4px;padding:8px 14px;border-top:1px solid var(--border)}
.queue-dot{width:8px;height:4px;background:var(--border);transition:all .2s}
.queue-dot--active{background:var(--cyan);box-shadow:0 0 6px rgba(0,212,255,.4)}
.queue-dot--commander{border:1px solid rgba(255,200,48,.3);background:rgba(255,200,48,.05)}
.queue-dot--commander.queue-dot--active{background:#ffc830;box-shadow:0 0 6px rgba(255,200,48,.4);border-color:#ffc830}

.research-filters{display:flex;flex-wrap:wrap;gap:8px;margin-bottom:18px}
.filter-btn{height:34px;padding:0 14px;border:1px solid var(--border);background:rgba(255,255,255,.02);color:var(--muted);font-size:11px;letter-spacing:1px;cursor:pointer;transition:all .15s}
.filter-btn:hover{color:var(--bright);border-color:var(--border-bright);background:rgba(0,212,255,.04)}
.filter-btn--active{color:var(--cyan);border-color:var(--border-bright);background:rgba(0,212,255,.08);box-shadow:inset 0 0 12px rgba(0,212,255,.04)}

.research-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(340px,1fr));gap:14px}
.research-card{padding:16px;border:1px solid var(--border);background:linear-gradient(180deg,rgba(7,16,28,.95),rgba(5,12,20,.98));box-shadow:inset 0 1px 0 rgba(255,255,255,.02);display:flex;flex-direction:column;gap:12px;position:relative}
.research-card--completed{border-color:rgba(61,255,156,.3);background:linear-gradient(180deg,rgba(61,255,156,.02),rgba(5,12,20,.98))}
.research-card--locked{opacity:.7;border-style:dashed}
.research-card--researching{border-color:var(--cyan);background:linear-gradient(180deg,rgba(0,212,255,.04),rgba(5,12,20,.98))}
.research-card--future{border-color:rgba(255,200,48,.25);background:linear-gradient(180deg,rgba(255,200,48,.04),rgba(5,12,20,.98))}

.research-status-badge{position:absolute;top:8px;right:8px;padding:4px 8px;font-size:8px;letter-spacing:1.5px;font-family:var(--ff-title);background:rgba(61,255,156,.1);color:#3dff9c;border:1px solid rgba(61,255,156,.3)}
.research-status-badge--active{background:rgba(0,212,255,.1);color:var(--cyan);border-color:var(--cyan)}
.research-status-badge--locked{background:rgba(255,255,255,.02);color:var(--muted);border-color:var(--border)}
.research-status-badge--future{background:rgba(255,200,48,.08);color:#ffc830;border-color:rgba(255,200,48,.35)}

.research-header{display:flex;align-items:center;gap:12px}
.research-icon{font-size:32px;flex-shrink:0}
.research-title-wrap{min-width:0}
.research-name{font-family:var(--ff-title);font-size:16px;letter-spacing:1.5px;color:var(--bright);line-height:1.3}
.research-category{font-size:9px;letter-spacing:2px;color:var(--cyan-dim);font-family:var(--ff-title);margin-top:4px;text-transform:uppercase}

.research-description{margin:0;font-size:12px;line-height:1.5;color:var(--muted)}

.research-requirements{padding:10px;border:1px solid var(--border);background:rgba(255,255,255,.01)}
.requirements-title{font-size:9px;letter-spacing:2px;color:var(--muted);font-family:var(--ff-title);margin-bottom:6px}
.requirements-list{display:flex;flex-direction:column;gap:4px}
.requirement-item{display:flex;align-items:center;gap:8px;font-size:10px;color:var(--muted)}
.requirement-item--met{color:var(--bright)}
.requirement-item--met .requirement-icon{color:#3dff9c}
.requirement-icon{font-size:12px;width:16px;text-align:center}
.requirement-text{font-family:var(--ff-title);letter-spacing:.5px}
.requirement-note{color:var(--cyan-dim);margin-left:4px}

.research-cost{display:flex;flex-direction:column;gap:8px}
.cost-title{font-size:10px;letter-spacing:2px;color:var(--muted);text-transform:uppercase}
.cost-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:6px}
.cost-item{border:1px solid var(--border);background:rgba(255,255,255,.02);padding:8px 6px;display:flex;align-items:center;gap:6px;justify-content:center;font-size:12px;color:#f0f7ff;transition:all .15s}
.cost-icon{width:44px;height:44px;object-fit:contain;flex-shrink:0}

.research-time{display:flex;align-items:center;gap:8px;padding:8px;border:1px solid var(--border);background:rgba(255,255,255,.01)}
.time-icon{font-size:16px}
.time-value{font-family:var(--ff-title);font-size:12px;letter-spacing:1px;color:var(--cyan)}

.research-actions{margin-top:auto}
.research-btn{width:100%;height:38px;border:1px solid var(--border);background:rgba(0,212,255,.08);color:var(--cyan);font-family:var(--ff-title);font-size:11px;letter-spacing:2px;cursor:pointer;transition:all .15s}
.research-btn:hover:not(:disabled){border-color:var(--border-bright);background:rgba(0,212,255,.14);box-shadow:0 0 14px rgba(0,212,255,.08)}
.research-btn:disabled{opacity:.45;cursor:not-allowed}
.research-btn--completed{background:rgba(61,255,156,.08);color:#3dff9c;border-color:rgba(61,255,156,.3)}
.research-btn--researching{background:rgba(0,212,255,.12);border-color:var(--cyan)}
.research-btn--locked{background:rgba(255,255,255,.02);color:var(--muted);border-style:dashed}
.research-btn--disabled{background:rgba(255,96,64,.04);color:#ff6040;border-color:rgba(255,96,64,.3)}

.research-empty{min-height:420px;display:flex;flex-direction:column;align-items:center;justify-content:center;border:1px solid var(--border);background:linear-gradient(180deg,rgba(7,16,28,.92),rgba(4,10,18,.98));text-align:center;padding:40px 20px}
.research-empty--small{min-height:200px}
.research-empty--error .empty-core,.research-empty--error .empty-title{color:#ff7b7b}
.empty-core{font-size:34px;color:var(--cyan);margin-bottom:8px}
.empty-title{font-family:var(--ff-title);font-size:18px;letter-spacing:3px;color:var(--bright)}
.empty-subtitle{font-size:12px;color:var(--muted);max-width:420px;line-height:1.6;margin-bottom:16px}
.empty-hint{font-size:11px;color:var(--muted);max-width:480px;line-height:1.6;padding:12px;border:1px solid var(--border);background:rgba(255,255,255,.01)}

@media(max-width:1200px){
  .research-grid{grid-template-columns:repeat(auto-fill,minmax(280px,1fr))}
}
@media(max-width:700px){
  .page-header{flex-direction:column;align-items:flex-start}
  .research-grid{grid-template-columns:1fr}
}

.page-badges {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
}

.page-badge--raretech {
  color: #9dffcf;
  border-color: rgba(157, 255, 207, 0.25);
  background: linear-gradient(180deg, rgba(61,255,156,.08), rgba(61,255,156,.02));
}

.research-tabs {
  display: flex;
  gap: 10px;
  margin-bottom: 18px;
  flex-wrap: wrap;
}

.research-tab {
  height: 36px;
  padding: 0 16px;
  border: 1px solid var(--border);
  background: rgba(255,255,255,.02);
  color: var(--muted);
  font-family: var(--ff-title);
  font-size: 10px;
  letter-spacing: 2px;
  cursor: pointer;
  transition: all .15s ease;
}

.research-tab:hover {
  color: var(--bright);
  border-color: var(--border-bright);
  background: rgba(0,212,255,.04);
}

.research-tab--active {
  color: var(--cyan);
  border-color: var(--border-bright);
  background: rgba(0,212,255,.08);
  box-shadow: inset 0 0 12px rgba(0,212,255,.04);
}

.research-topbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 14px;
  margin-bottom: 18px;
  flex-wrap: wrap;
}

.research-points-box {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 12px;
  border: 1px solid var(--border-bright);
  background: linear-gradient(180deg, rgba(0,212,255,.06), rgba(0,212,255,.02));
}

.research-points-label {
  font-size: 9px;
  letter-spacing: 2px;
  color: var(--muted);
  font-family: var(--ff-title);
}

.research-points-value {
  color: var(--cyan);
  font-family: var(--ff-title);
  font-size: 14px;
  letter-spacing: 1px;
}

.cost-item--insufficient {
  color: #ff6040;
  border-color: rgba(255,96,64,.35);
  background: rgba(255,96,64,.05);
}

.salvager-layout {
  display: grid;
  grid-template-columns: 1.2fr .95fr;
  gap: 16px;
}

.salvager-panel {
  border: 1px solid var(--border);
  background: linear-gradient(180deg, rgba(7,16,28,.95), rgba(5,12,20,.98));
  box-shadow: inset 0 1px 0 rgba(255,255,255,.02);
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-width: 0;
}

.salvager-panel--detail {
  min-height: 100%;
}

.salvager-head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 12px;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--border);
}

.salvager-kicker {
  font-size: 9px;
  letter-spacing: 2px;
  color: var(--cyan-dim);
  font-family: var(--ff-title);
  text-transform: uppercase;
}

.salvager-title {
  margin: 4px 0 0;
  font-family: var(--ff-title);
  font-size: 18px;
  letter-spacing: 2px;
  color: var(--bright);
}

.salvager-badge {
  padding: 6px 10px;
  border: 1px solid var(--border-bright);
  background: rgba(0,212,255,.06);
  color: var(--cyan);
  font-family: var(--ff-title);
  font-size: 10px;
  letter-spacing: 1.5px;
  white-space: nowrap;
}

.salvager-summary {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 10px;
}

.salvager-stat {
  padding: 12px;
  border: 1px solid var(--border);
  background: rgba(255,255,255,.02);
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.salvager-stat-label {
  font-size: 9px;
  letter-spacing: 1.6px;
  color: var(--muted);
  font-family: var(--ff-title);
  text-transform: uppercase;
}

.salvager-stat-value {
  color: var(--cyan);
  font-family: var(--ff-title);
  font-size: 20px;
  letter-spacing: 1px;
}

.salvage-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.salvage-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  padding: 12px;
  border: 1px solid var(--border);
  background: rgba(255,255,255,.02);
  cursor: pointer;
  transition: all .15s ease;
}

.salvage-item:hover {
  border-color: var(--border-bright);
  background: rgba(0,212,255,.04);
}

.salvage-item--selected {
  border-color: var(--cyan);
  background: rgba(0,212,255,.08);
  box-shadow: inset 0 0 12px rgba(0,212,255,.04);
}

.salvage-item-left {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;
}

.salvage-item-icon {
  width: 42px;
  height: 42px;
  border: 1px solid var(--border-bright);
  background: linear-gradient(180deg, rgba(0,212,255,.08), rgba(255,255,255,.02));
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  flex-shrink: 0;
}

.salvage-item-main {
  min-width: 0;
}

.salvage-item-name {
  font-size: 13px;
  color: var(--bright);
  font-weight: 700;
  line-height: 1.3;
}

.salvage-item-meta {
  display: flex;
  gap: 6px;
  margin-top: 4px;
  font-size: 9px;
  color: var(--cyan-dim);
  letter-spacing: 1.2px;
  font-family: var(--ff-title);
  text-transform: uppercase;
  flex-wrap: wrap;
}

.salvage-item-right {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 4px;
  flex-shrink: 0;
}

.salvage-item-qty {
  font-family: var(--ff-title);
  color: var(--bright);
  font-size: 14px;
  letter-spacing: 1px;
}

.salvage-item-yield {
  font-family: var(--ff-title);
  color: #3dff9c;
  font-size: 11px;
  letter-spacing: 1px;
}

.salvage-detail {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.salvage-detail-icon {
  width: 72px;
  height: 72px;
  border: 1px solid var(--border-bright);
  background: linear-gradient(180deg, rgba(0,212,255,.08), rgba(255,255,255,.02));
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 34px;
}

.salvage-detail-name {
  font-family: var(--ff-title);
  font-size: 20px;
  letter-spacing: 2px;
  color: var(--bright);
}

.salvage-detail-desc {
  font-size: 12px;
  line-height: 1.6;
  color: var(--muted);
}

.salvage-detail-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 8px;
}

.salvage-detail-stat {
  border: 1px solid var(--border);
  background: rgba(255,255,255,.02);
  padding: 10px 12px;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.salvage-detail-stat span {
  font-size: 9px;
  letter-spacing: 1.5px;
  color: var(--muted);
  font-family: var(--ff-title);
  text-transform: uppercase;
}

.salvage-detail-stat strong {
  font-size: 14px;
  color: var(--bright);
  font-family: var(--ff-title);
  letter-spacing: 1px;
}

.salvage-control {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.salvage-yield-preview {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  border: 1px solid rgba(61,255,156,.25);
  background: linear-gradient(180deg, rgba(61,255,156,.08), rgba(61,255,156,.02));
}

.salvage-yield-label {
  font-size: 10px;
  color: var(--muted);
  letter-spacing: 1px;
  text-transform: uppercase;
}

.salvage-yield-value {
  color: #3dff9c;
  font-family: var(--ff-title);
  font-size: 18px;
  letter-spacing: 1px;
}

@media (max-width: 1100px) {
  .salvager-layout {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 760px) {
  .page-badges,
  .research-topbar,
  .research-tabs {
    width: 100%;
  }

  .salvager-summary,
  .salvage-detail-grid {
    grid-template-columns: 1fr;
  }
}

.train-qty-box {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
}

.qty-btn,
.qty-input {
  height: 38px;
  border: 1px solid var(--border);
  background: rgba(0,212,255,.08);
  color: var(--cyan);
  font-family: var(--ff-title);
  transition: all .15s ease;
  box-sizing: border-box;
}

.qty-btn {
  min-width: 38px;
  padding: 0 10px;
  font-size: 11px;
  letter-spacing: 2px;
  cursor: pointer;
}

.qty-btn:hover:not(:disabled),
.qty-input:focus {
  border-color: var(--border-bright);
  background: rgba(0,212,255,.14);
  box-shadow: 0 0 14px rgba(0,212,255,.08);
}

.qty-btn:disabled {
  opacity: .45;
  cursor: not-allowed;
  box-shadow: none;
}

.qty-btn--max {
  min-width: 54px;
  font-size: 10px;
}

.qty-input {
  width: 64px;
  padding: 0 8px;
  text-align: center;
  font-size: 13px;
  outline: none;
  letter-spacing: 1px;
  -moz-appearance: textfield;
  appearance: textfield;
}

.qty-input::-webkit-outer-spin-button,
.qty-input::-webkit-inner-spin-button {
  -webkit-appearance: none;
  margin: 0;
}
</style>