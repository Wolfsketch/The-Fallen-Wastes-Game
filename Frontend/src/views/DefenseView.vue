<template>
  <div class="defense-page">
    <div class="page-header">
      <div>
        <h1 class="page-title">DEFENSE GRID</h1>
        <p class="page-subtitle">Perimeter wall integrity & settlement protection overview</p>
      </div>

      <div class="page-actions">
        <button class="refresh-btn" @click="reloadDefenseData" :disabled="loading">
          {{ loading ? 'SYNCING...' : 'REFRESH' }}
        </button>
      </div>
    </div>

    <div v-if="loading" class="defense-empty">
      <div class="empty-core">◌</div>
      <div class="empty-title">LOADING DEFENSE SYSTEMS</div>
      <div class="empty-subtitle">Scanning perimeter systems and fortification status...</div>
    </div>

    <div v-else-if="error" class="defense-empty defense-empty--error">
      <div class="empty-core">!</div>
      <div class="empty-title">DEFENSE LINK FAILURE</div>
      <div class="empty-subtitle">{{ error }}</div>
    </div>

    <template v-else>
      <div class="defense-grid">
        <section class="panel wall-panel">
          <div class="panel-head">
            <div>
              <div class="panel-kicker">Primary Fortification</div>
              <h2 class="panel-title">Perimeter Wall</h2>
            </div>
            <div class="wall-badge" :class="wallBadgeClass">
              {{ wallStatusLabel }}
            </div>
          </div>

          <div class="wall-main">
            <div class="wall-core">
              <div class="wall-level-ring">
                <div class="wall-level-inner">
                  <div class="wall-level-value">{{ wallLevel }}</div>
                  <div class="wall-level-label">LEVEL</div>
                </div>
              </div>
            </div>

            <div class="wall-stats">
              <div class="metric-row">
                <span class="metric-label">Structural Strength</span>
                <span class="metric-value">{{ wallDefenseValue }}</span>
              </div>

              <div class="metric-row">
                <span class="metric-label">Integrity</span>
                <span class="metric-value">{{ wallIntegrity }}%</span>
              </div>

              <div class="metric-row">
                <span class="metric-label">Breach Pressure</span>
                <span class="metric-value" :class="breachClass">{{ breachPressureLabel }}</span>
              </div>

              <div class="metric-row">
                <span class="metric-label">Upgrade State</span>
                <span class="metric-value">{{ wallUpgradeState }}</span>
              </div>

              <div class="metric-row">
                <span class="metric-label">Free Population</span>
                <span class="metric-value">{{ freePopulation }}</span>
              </div>
            </div>
          </div>

          <div class="integrity-block">
            <div class="integrity-head">
              <span>Outer Barrier Integrity</span>
              <span>{{ wallIntegrity }}%</span>
            </div>
            <div class="integrity-bar">
              <div class="integrity-fill" :style="{ width: `${wallIntegrity}%` }" />
            </div>
          </div>
        </section>

        <section class="panel summary-panel">
          <div class="panel-head">
            <div>
              <div class="panel-kicker">Settlement Overview</div>
              <h2 class="panel-title">Defense Summary</h2>
            </div>
          </div>

          <div class="summary-cards">
            <div class="summary-card">
              <div class="summary-label">Base Defense</div>
              <div class="summary-value">{{ baseDefense }}</div>
              <div class="summary-sub">Core settlement resilience</div>
            </div>

            <div class="summary-card">
              <div class="summary-label">Wall Bonus</div>
              <div class="summary-value">+{{ wallDefenseValue }}</div>
              <div class="summary-sub">From perimeter fortification</div>
            </div>

            <div class="summary-card">
              <div class="summary-label">Total Defense</div>
              <div class="summary-value">{{ totalDefense }}</div>
              <div class="summary-sub">Estimated defensive output</div>
            </div>

            <div class="summary-card">
              <div class="summary-label">Morale</div>
              <div class="summary-value">{{ settlementData?.morale ?? props.settlement?.morale ?? 0 }}</div>
              <div class="summary-sub">Impacts defensive stability</div>
            </div>
          </div>

          <div class="summary-note">
            Defensive strength is currently centered on the perimeter wall. Additional defensive layers can be integrated later.
          </div>
        </section>

        <section class="panel relic-panel" v-if="relicVaultLevel > 0">
          <div class="panel-head">
            <div>
              <div class="panel-kicker">RareTech Protection</div>
              <h2 class="panel-title">Relic Vault</h2>
            </div>
            <div class="wall-badge wall-badge--online">L{{ relicVaultLevel }}</div>
          </div>

          <div class="relic-stats">
            <div class="metric-row">
              <span class="metric-label">Vault Capacity</span>
              <span class="metric-value">
                {{ relicVaultLevel >= 10 ? '∞' : relicVaultCapacity }}
              </span>
            </div>
            <div class="metric-row">
              <span class="metric-label">RareTech Stored</span>
              <span class="metric-value relic-stored">{{ relicStored }}</span>
            </div>
            <div class="metric-row">
              <span class="metric-label">Protection Status</span>
              <span class="metric-value" :class="relicStored > 0 ? 'breach-low' : 'breach-high'">
                {{ relicStored > 0 ? 'PROTECTED' : 'EMPTY — VULNERABLE' }}
              </span>
            </div>
          </div>

          <div class="relic-deposit">
            <div class="panel-kicker" style="margin-bottom:8px">DEPOSIT RARETECH</div>
            <div class="relic-deposit-row">
              <button class="qty-btn" @click="relicDepositAmount = Math.max(0, relicDepositAmount - 10)">−</button>
              <input class="relic-input" type="number" min="0"
                     :max="maxRelicDeposit"
                     v-model.number="relicDepositAmount" />
              <button class="qty-btn" @click="relicDepositAmount = Math.min(maxRelicDeposit, relicDepositAmount + 10)">+</button>
              <button class="relic-btn"
                      :disabled="relicDepositAmount <= 0 || depositing"
                      @click="depositToRelicVault">
                {{ depositing ? 'DEPOSITING...' : 'DEPOSIT' }}
              </button>
            </div>
            <div v-if="relicDepositError" class="relic-error">{{ relicDepositError }}</div>
            <div v-if="relicDepositSuccess" class="relic-success">{{ relicDepositSuccess }}</div>
            <div class="relic-hint">
              Available: {{ availableRareTech }} RT —
              {{ relicVaultLevel >= 10 ? 'Unlimited storage at level 10+' : 'Max deposit: ' + maxRelicDeposit + ' RT' }}
            </div>
          </div>
        </section>

        <div class="defense-empty" v-else-if="!loading">
          <div class="empty-core">🧬</div>
          <div class="empty-title">NO RELIC VAULT</div>
          <div class="empty-subtitle">
            Build a Relic Vault to protect your RareTech from scouts.
            Requires TechLab 3 and TechVault 5.
          </div>
        </div>
      </div>

      <section class="panel structures-panel">
        <div class="panel-head">
          <div>
            <div class="panel-kicker">Fortification Registry</div>
            <h2 class="panel-title">Defensive Structures</h2>
          </div>
          <div class="registry-count">{{ defensiveBuildings.length }} REGISTERED</div>
        </div>

        <div v-if="defensiveBuildings.length === 0" class="structures-empty">
          <div class="structures-empty-title">NO DEFENSIVE STRUCTURES DETECTED</div>
          <div class="structures-empty-sub">
            No active fortifications were found in the current settlement profile.
          </div>
        </div>

        <div v-else class="structures-table">
          <div class="structures-head">
            <span>Structure</span>
            <span>Level</span>
            <span>Status</span>
            <span>Defense</span>
          </div>

          <div
              v-for="building in defensiveBuildings"
              :key="building.id || `${building.type}-${building.level}`"
              class="structures-row"
          >
            <span class="structure-name">{{ building.name }}</span>
            <span class="structure-level">LVL {{ building.level }}</span>
            <span class="structure-status">
              {{ building.isUpgrading ? 'UPGRADING' : 'ACTIVE' }}
            </span>
            <span class="structure-defense">
              {{ estimateBuildingDefense(building) }}
            </span>
          </div>
        </div>
      </section>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { getSettlement } from '../services/api'

const props = defineProps({
  settlement: {
    type: Object,
    default: null
  },
  refreshSettlement: {
    type: Function,
    default: null
  },
  liveResources: {
    type: Object,
    default: () => ({})
  },
  livePopulation: {
    type: Object,
    default: () => ({})
  }
})

const loading = ref(true)
const error = ref('')
const settlementData = ref(null)
const buildings = ref([])

const freePopulation = computed(() => {
  if (props.livePopulation?.availablePopulation != null) {
    return props.livePopulation.availablePopulation
  }

  return settlementData.value?.availablePopulation
      ?? settlementData.value?.AvailablePopulation
      ?? props.settlement?.availablePopulation
      ?? props.settlement?.AvailablePopulation
      ?? Math.max(
          0,
          (props.settlement?.populationCapacity ?? 0) - (props.settlement?.usedPopulation ?? 0)
      )
})

const baseDefense = computed(() => {
  const morale = settlementData.value?.morale ?? props.settlement?.morale ?? 0
  return 10 + Math.floor(morale / 10)
})

const wallBuilding = computed(() => findWallBuilding(buildings.value))
const wallLevel = computed(() => wallBuilding.value?.level ?? 0)

const wallDefenseValue = computed(() => {
  if (!wallLevel.value) return 0
  return wallLevel.value * 18
})

const totalDefense = computed(() => {
  const extraBuildingDefense = defensiveBuildings.value
      .filter(b => !isWallBuilding(b))
      .reduce((sum, b) => sum + estimateBuildingDefense(b), 0)

  return baseDefense.value + wallDefenseValue.value + extraBuildingDefense
})

const wallIntegrity = computed(() => {
  if (!wallLevel.value) return 0
  if (wallBuilding.value?.isDestroyed) return 0
  if (wallBuilding.value?.breachDamage != null) {
    return Math.max(0, 100 - wallBuilding.value.breachDamage)
  }
  if (wallBuilding.value?.wallDamage != null) {
    return Math.max(0, 100 - wallBuilding.value.wallDamage)
  }
  return Math.min(100, 35 + wallLevel.value * 13)
})

const breachPressureLabel = computed(() => {
  if (!wallLevel.value) return 'CRITICAL'
  if (wallIntegrity.value >= 85) return 'LOW'
  if (wallIntegrity.value >= 60) return 'MODERATE'
  if (wallIntegrity.value >= 35) return 'HIGH'
  return 'CRITICAL'
})

const breachClass = computed(() => {
  return {
    'metric-value--low': breachPressureLabel.value === 'LOW',
    'metric-value--mid': breachPressureLabel.value === 'MODERATE',
    'metric-value--high': breachPressureLabel.value === 'HIGH',
    'metric-value--critical': breachPressureLabel.value === 'CRITICAL'
  }
})

const wallStatusLabel = computed(() => {
  if (!wallLevel.value) return 'OFFLINE'
  if (wallBuilding.value?.isUpgrading) return 'UPGRADING'
  if (wallIntegrity.value >= 85) return 'FORTIFIED'
  if (wallIntegrity.value >= 50) return 'STABLE'
  if (wallIntegrity.value >= 25) return 'DAMAGED'
  return 'BREACHED'
})

const wallBadgeClass = computed(() => ({
  'wall-badge--offline': wallStatusLabel.value === 'OFFLINE',
  'wall-badge--good': ['FORTIFIED', 'STABLE'].includes(wallStatusLabel.value),
  'wall-badge--warn': ['DAMAGED', 'UPGRADING'].includes(wallStatusLabel.value),
  'wall-badge--danger': wallStatusLabel.value === 'BREACHED'
}))

const wallUpgradeState = computed(() => {
  if (!wallBuilding.value) return 'NOT BUILT'
  return wallBuilding.value.isUpgrading ? 'UPGRADE IN PROGRESS' : 'OPERATIONAL'
})

const defensiveBuildings = computed(() => {
  const filtered = buildings.value.filter(isRelevantDefensiveBuilding)

  const deduped = new Map()

  for (const building of filtered) {
    const key = String(building.type || building.name || '').toLowerCase()

    if (!deduped.has(key)) {
      deduped.set(key, building)
      continue
    }

    const existing = deduped.get(key)
    const existingScore = (existing.level ?? 0) + (existing.isUpgrading ? 0.5 : 0)
    const currentScore = (building.level ?? 0) + (building.isUpgrading ? 0.5 : 0)

    if (currentScore > existingScore) {
      deduped.set(key, building)
    }
  }

  return Array.from(deduped.values())
})

watch(
    () => props.settlement?.id,
    async newId => {
      if (newId) {
        await loadDefenseData()
      }
    }
)

onMounted(async () => {
  await loadDefenseData()
})

async function reloadDefenseData() {
  await loadDefenseData(true)
}

async function loadDefenseData(runParentRefresh = false) {
  if (!props.settlement?.id) {
    loading.value = false
    error.value = 'No active settlement found.'
    return
  }

  loading.value = true
  error.value = ''

  try {
    if (runParentRefresh && typeof props.refreshSettlement === 'function') {
      await props.refreshSettlement()
    }

    const data = await getSettlement(props.settlement.id)
    settlementData.value = data
    buildings.value = normalizeBuildings(data)
  } catch (err) {
    console.error(err)
    error.value = 'Unable to access settlement defense data.'
  } finally {
    loading.value = false
  }
}

function normalizeBuildings(data) {
  const sources = [
    data?.buildings,
    data?.Buildings,
    data?.settlementBuildings,
    data?.buildingQueue,
    data?.structures
  ].filter(Array.isArray)

  if (!sources.length) return []

  return sources.flatMap(list =>
      list.map(b => ({
        id: b.id ?? b.Id ?? b.buildingId ?? null,
        name: b.displayName ?? b.DisplayName ?? b.name ?? b.buildingName ?? b.type ?? b.Type ?? 'Unknown Structure',
        level: b.level ?? b.Level ?? b.currentLevel ?? 0,
        isUpgrading: b.isUpgrading ?? b.IsConstructing ?? b.upgrading ?? false,
        isDestroyed: b.isDestroyed ?? b.IsDestroyed ?? false,
        breachDamage: b.breachDamage ?? b.BreachDamage ?? null,
        wallDamage: b.wallDamage ?? b.WallDamage ?? null,
        category: b.category ?? b.Category ?? b.buildingCategory ?? '',
        type: b.type ?? b.Type ?? ''
      }))
  )
}

function findWallBuilding(list) {
  return list
      .filter(isWallBuilding)
      .sort((a, b) => (b.level ?? 0) - (a.level ?? 0))[0] ?? null
}

function isWallBuilding(building) {
  const name = `${building?.name ?? ''}`.toLowerCase()
  const type = `${building?.type ?? ''}`.toLowerCase()

  return (
      type === 'perimeterwall' ||
      type === 'perimeter wall' ||
      name === 'perimeter wall' ||
      name === 'perimeterwall'
  )
}

function isWatchTower(building) {
  const name = `${building?.name ?? ''}`.toLowerCase()
  const type = `${building?.type ?? ''}`.toLowerCase()

  return (
      type === 'watchtower' ||
      type === 'watch tower' ||
      name === 'watchtower' ||
      name === 'watch tower'
  )
}

function isRelevantDefensiveBuilding(building) {
  if (isWatchTower(building)) return false

  const name = `${building?.name ?? ''}`.toLowerCase()
  const category = `${building?.category ?? ''}`.toLowerCase()
  const type = `${building?.type ?? ''}`.toLowerCase()

  return (
      isWallBuilding(building) ||
      category.includes('def') ||
      type.includes('bunker') ||
      type.includes('barrier') ||
      type.includes('fort') ||
      name.includes('bunker') ||
      name.includes('barrier') ||
      name.includes('fort')
  )
}

function estimateBuildingDefense(building) {
  const level = building?.level ?? 0

  if (isWallBuilding(building)) {
    return level * 18
  }

  const name = `${building?.name ?? ''}`.toLowerCase()

  if (name.includes('turret')) return level * 14
  if (name.includes('bunker')) return level * 12
  if (name.includes('barrier')) return level * 10
  if (name.includes('fort')) return level * 16

  return level * 8
}

// ── Relic Vault ──────────────────────────────────────────────
const relicVaultBuilding = computed(() =>
  buildings.value.find(b =>
    b.type === 'RaidVault' || b.displayName === 'Relic Vault' || b.name === 'Relic Vault'
  ) ?? null
)
const relicVaultLevel = computed(() => relicVaultBuilding.value?.level ?? 0)
const relicVaultCapacity = computed(() =>
  relicVaultLevel.value >= 10 ? Infinity : relicVaultLevel.value * 100
)
const relicStored = computed(() =>
  props.liveResources?.rareTech ?? props.settlement?.rareTech ?? 0
)
const availableRareTech = computed(() =>
  props.liveResources?.rareTech ?? props.settlement?.rareTech ?? 0
)
const maxRelicDeposit = computed(() => {
  if (relicVaultLevel.value >= 10) return availableRareTech.value
  return Math.min(availableRareTech.value, relicVaultCapacity.value - relicStored.value)
})

const relicDepositAmount = ref(0)
const depositing = ref(false)
const relicDepositError = ref('')
const relicDepositSuccess = ref('')

async function depositToRelicVault() {
  if (relicDepositAmount.value <= 0) return
  depositing.value = true
  relicDepositError.value = ''
  relicDepositSuccess.value = ''
  try {
    // No actual API call needed: RareTech is already in settlement resources.
    // The RaidVault level determines how much is protected during a scout.
    await new Promise(r => setTimeout(r, 400))
    relicDepositSuccess.value =
      `${relicDepositAmount.value} RT is now protected by the vault (up to vault capacity).`
    relicDepositAmount.value = 0
  } catch (e) {
    relicDepositError.value = 'Deposit failed.'
  } finally {
    depositing.value = false
  }
}
</script>

<style scoped>
.defense-page {
  display: flex;
  flex-direction: column;
  gap: 18px;
  color: var(--text);
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 16px;
}

.page-title {
  margin: 0;
  color: var(--bright);
  font-family: var(--ff-title), sans-serif;
  letter-spacing: 3px;
  font-size: 28px;
}

.page-subtitle {
  margin: 6px 0 0;
  color: var(--muted);
  font-size: 12px;
  letter-spacing: 1px;
  text-transform: uppercase;
}

.page-actions {
  display: flex;
  align-items: center;
  gap: 10px;
}

.refresh-btn {
  border: 1px solid var(--border-bright);
  background: linear-gradient(180deg, rgba(0, 212, 255, 0.12), rgba(0, 212, 255, 0.04));
  color: var(--cyan);
  padding: 10px 14px;
  font-family: var(--ff-title), sans-serif;
  font-size: 11px;
  letter-spacing: 2px;
  cursor: pointer;
  transition: 0.2s ease;
}

.refresh-btn:hover:not(:disabled) {
  box-shadow: 0 0 12px rgba(0, 212, 255, 0.18);
  border-color: var(--cyan);
}

.refresh-btn:disabled {
  opacity: 0.6;
  cursor: default;
}

.defense-grid {
  display: grid;
  grid-template-columns: 1.2fr 1fr;
  gap: 18px;
}

.panel {
  background: linear-gradient(180deg, rgba(10, 16, 24, 0.96), rgba(6, 10, 16, 0.96));
  border: 1px solid var(--border);
  padding: 18px;
  position: relative;
  overflow: hidden;
}

.panel::before {
  content: '';
  position: absolute;
  inset: 0;
  background: linear-gradient(135deg, rgba(0,212,255,0.03), transparent 45%, transparent);
  pointer-events: none;
}

.panel-head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 12px;
  position: relative;
  z-index: 1;
}

.panel-kicker {
  color: var(--cyan-dim);
  font-size: 10px;
  text-transform: uppercase;
  letter-spacing: 2px;
  font-family: var(--ff-title), sans-serif;
}

.panel-title {
  margin: 4px 0 0;
  color: var(--bright);
  font-size: 20px;
  letter-spacing: 1.5px;
  font-family: var(--ff-title), sans-serif;
}

.wall-badge {
  padding: 7px 10px;
  font-size: 10px;
  letter-spacing: 1.6px;
  border: 1px solid var(--border-bright);
  font-family: var(--ff-title), sans-serif;
}

.wall-badge--offline {
  color: #9aa8b3;
  border-color: rgba(154, 168, 179, 0.35);
  background: rgba(154, 168, 179, 0.08);
}

.wall-badge--good {
  color: var(--green);
  border-color: rgba(48, 255, 128, 0.35);
  background: rgba(48, 255, 128, 0.08);
}

.wall-badge--warn {
  color: var(--amber);
  border-color: rgba(255, 170, 32, 0.35);
  background: rgba(255, 170, 32, 0.08);
}

.wall-badge--danger {
  color: var(--red);
  border-color: rgba(255, 48, 64, 0.35);
  background: rgba(255, 48, 64, 0.08);
}

.wall-main {
  display: grid;
  grid-template-columns: 220px 1fr;
  gap: 24px;
  align-items: center;
  margin-top: 18px;
  position: relative;
  z-index: 1;
}

.wall-core {
  display: flex;
  justify-content: center;
  align-items: center;
}

.wall-level-ring {
  width: 170px;
  height: 170px;
  border-radius: 50%;
  border: 1px solid var(--border-bright);
  background:
      radial-gradient(circle at center, rgba(0,212,255,0.08), transparent 62%),
      linear-gradient(180deg, rgba(0, 212, 255, 0.08), rgba(0, 212, 255, 0.02));
  display: flex;
  justify-content: center;
  align-items: center;
  box-shadow:
      inset 0 0 20px rgba(0,212,255,0.08),
      0 0 20px rgba(0,212,255,0.05);
}

.wall-level-inner {
  width: 112px;
  height: 112px;
  border-radius: 50%;
  border: 1px solid rgba(0, 212, 255, 0.24);
  background: rgba(6, 10, 16, 0.9);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}

.wall-level-value {
  color: var(--cyan);
  font-size: 38px;
  line-height: 1;
  font-family: var(--ff-title), sans-serif;
  text-shadow: 0 0 12px rgba(0, 212, 255, 0.22);
}

.wall-level-label {
  margin-top: 6px;
  color: var(--muted);
  font-size: 10px;
  letter-spacing: 2px;
  font-family: var(--ff-title), sans-serif;
}

.wall-stats {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.metric-row {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  padding: 10px 12px;
  border: 1px solid rgba(20, 32, 48, 0.9);
  background: rgba(14, 22, 32, 0.72);
}

.metric-label {
  color: var(--muted);
  font-size: 11px;
  letter-spacing: 1px;
  text-transform: uppercase;
}

.metric-value {
  color: var(--bright);
  font-family: var(--ff-title), sans-serif;
  letter-spacing: 1px;
}

.metric-value--low {
  color: var(--green);
}

.metric-value--mid {
  color: var(--amber);
}

.metric-value--high,
.metric-value--critical {
  color: var(--red);
}

.integrity-block {
  margin-top: 18px;
  position: relative;
  z-index: 1;
}

.integrity-head {
  display: flex;
  justify-content: space-between;
  margin-bottom: 8px;
  color: var(--muted);
  font-size: 11px;
  letter-spacing: 1px;
  text-transform: uppercase;
}

.integrity-bar {
  height: 12px;
  background: rgba(14, 22, 32, 0.9);
  border: 1px solid var(--border);
  overflow: hidden;
}

.integrity-fill {
  height: 100%;
  background: linear-gradient(90deg, var(--red), var(--amber), var(--green));
  box-shadow: 0 0 12px rgba(0, 212, 255, 0.16);
}

.summary-cards {
  margin-top: 18px;
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
  position: relative;
  z-index: 1;
}

.summary-card {
  padding: 14px;
  border: 1px solid rgba(20, 32, 48, 0.9);
  background: rgba(14, 22, 32, 0.72);
}

.summary-label {
  color: var(--muted);
  font-size: 10px;
  text-transform: uppercase;
  letter-spacing: 1.5px;
}

.summary-value {
  margin-top: 8px;
  color: var(--cyan);
  font-size: 28px;
  line-height: 1;
  font-family: var(--ff-title), sans-serif;
}

.summary-sub {
  margin-top: 8px;
  color: var(--text);
  font-size: 11px;
  line-height: 1.4;
}

.summary-note {
  margin-top: 14px;
  padding: 12px 14px;
  border: 1px solid rgba(0, 212, 255, 0.16);
  background: rgba(0, 212, 255, 0.05);
  color: var(--text);
  font-size: 12px;
  line-height: 1.5;
  position: relative;
  z-index: 1;
}

.registry-count {
  color: var(--cyan);
  font-family: var(--ff-title), sans-serif;
  font-size: 11px;
  letter-spacing: 2px;
}

.structures-panel {
  position: relative;
}

.structures-empty {
  padding: 24px 10px 10px;
  text-align: center;
  position: relative;
  z-index: 1;
}

.structures-empty-title {
  color: var(--bright);
  font-family: var(--ff-title), sans-serif;
  letter-spacing: 2px;
  font-size: 13px;
}

.structures-empty-sub {
  margin-top: 8px;
  color: var(--muted);
  font-size: 12px;
}

.structures-table {
  margin-top: 18px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  position: relative;
  z-index: 1;
}

.structures-head,
.structures-row {
  display: grid;
  grid-template-columns: 2fr 110px 140px 110px;
  gap: 12px;
  align-items: center;
}

.structures-head {
  color: var(--cyan-dim);
  font-size: 10px;
  text-transform: uppercase;
  letter-spacing: 2px;
  padding: 0 12px 8px;
  border-bottom: 1px solid var(--border);
}

.structures-row {
  padding: 12px;
  border: 1px solid rgba(20, 32, 48, 0.9);
  background: rgba(14, 22, 32, 0.72);
}

.structure-name {
  color: var(--bright);
  font-weight: 600;
}

.structure-level,
.structure-status,
.structure-defense {
  color: var(--text);
  font-size: 12px;
}

.defense-empty {
  min-height: 360px;
  border: 1px solid var(--border);
  background: linear-gradient(180deg, rgba(10, 16, 24, 0.96), rgba(6, 10, 16, 0.96));
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
}

.defense-empty--error {
  border-color: rgba(255, 48, 64, 0.24);
}

.empty-core {
  width: 64px;
  height: 64px;
  border-radius: 50%;
  border: 1px solid var(--border-bright);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--cyan);
  font-family: var(--ff-title), sans-serif;
  font-size: 22px;
  box-shadow: inset 0 0 16px rgba(0,212,255,0.06);
}

.empty-title {
  margin-top: 16px;
  color: var(--bright);
  font-family: var(--ff-title), sans-serif;
  letter-spacing: 2px;
  font-size: 13px;
}

.empty-subtitle {
  margin-top: 6px;
  color: var(--muted);
  font-size: 12px;
}

@media (max-width: 1100px) {
  .defense-grid {
    grid-template-columns: 1fr;
  }

  .wall-main {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 720px) {
  .page-header {
    flex-direction: column;
    align-items: stretch;
  }

  .summary-cards {
    grid-template-columns: 1fr;
  }

  .structures-head,
  .structures-row {
    grid-template-columns: 1.6fr 90px 110px 90px;
    font-size: 11px;
  }
}

/* ── Relic Vault ─────────────────────────────────────────── */
.relic-panel { grid-column: 1 / -1; }
.relic-stats { display: flex; flex-direction: column; gap: 8px; margin-bottom: 20px; margin-top: 18px; position: relative; z-index: 1; }
.relic-stored { color: var(--cyan); font-weight: 700; }
.relic-deposit { padding: 16px; background: rgba(0,212,255,.03); border: 1px solid var(--border); position: relative; z-index: 1; }
.relic-deposit-row { display: flex; gap: 8px; align-items: center; margin-bottom: 8px; }
.relic-input {
  width: 80px; background: var(--bg3); border: 1px solid var(--border);
  color: var(--text); padding: 6px 10px; font-family: var(--ff-title), sans-serif;
  font-size: 12px; text-align: center;
}
.relic-btn {
  padding: 8px 16px; background: linear-gradient(180deg, var(--cyan-dark), var(--cyan-dim));
  border: 1px solid var(--cyan); color: #fff; font-family: var(--ff-title), sans-serif;
  font-size: 11px; font-weight: 700; cursor: pointer; letter-spacing: 2px;
}
.relic-btn:disabled { background: var(--bg3); border-color: var(--border); color: var(--muted); cursor: not-allowed; }
.qty-btn {
  width: 28px; height: 28px; background: var(--bg3); border: 1px solid var(--border);
  color: var(--text); cursor: pointer; font-size: 14px;
}
.relic-hint { font-size: 10px; color: var(--muted); margin-top: 6px; }
.relic-error { color: var(--red); font-size: 11px; margin-top: 6px; }
.relic-success { color: var(--green); font-size: 11px; margin-top: 6px; }
.wall-badge--online {
  color: var(--cyan);
  border-color: rgba(0, 212, 255, 0.35);
  background: rgba(0, 212, 255, 0.08);
}
.breach-low { color: var(--green); }
.breach-high { color: var(--red); }
</style>