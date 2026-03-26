<template>
  <div class="modal-overlay" @click="$emit('close')">
    <div class="modal-card" @click.stop>
      <div class="modal-accent" />

      <div class="modal-body">
        <div class="modal-header">
          <span class="modal-icon">{{ icon }}</span>

          <div class="modal-header-main">
            <div class="modal-name">{{ building.displayName }}</div>
            <div class="modal-level">
              LEVEL {{ building.level }}<span v-if="canShowNextLevel"> → LEVEL {{ building.level + 1 }}</span>
            </div>
          </div>

          <button class="modal-close" @click="$emit('close')">✕</button>
        </div>

        <div class="modal-desc">{{ building.description }}</div>

        <div v-if="building.isFutureFeature" class="modal-future-box">
          <div class="modal-section-title">STATUS</div>
          <div class="modal-future-text">
            This building exists in the long-term design but is currently not available in gameplay.
          </div>
        </div>

        <div v-if="building.level > 0 && hasProduction" class="modal-current-prod">
          <span class="modal-section-title">CURRENT OUTPUT</span>
          <span class="modal-prod-val">{{ currentProdText }}</span>
        </div>

        <div v-if="hasCurrentEffects" class="modal-effects-box">
          <div class="modal-section-title">CURRENT EFFECTS</div>
          <div class="modal-effects-grid">
            <div v-if="building.powerValue > 0" class="modal-effect-item">
              <span class="modal-effect-label">POWER</span>
              <span class="modal-effect-value">{{ building.powerValue }}</span>
            </div>
            <div v-if="building.populationUsage > 0" class="modal-effect-item">
              <span class="modal-effect-label">POP USAGE</span>
              <span class="modal-effect-value">{{ building.populationUsage }}</span>
            </div>
            <div v-if="building.storageBonus > 0" class="modal-effect-item">
              <span class="modal-effect-label">STORAGE</span>
              <span class="modal-effect-value">{{ building.storageBonus }}</span>
            </div>
            <div v-if="building.defenseValue > 0" class="modal-effect-item">
              <span class="modal-effect-label">DEFENSE</span>
              <span class="modal-effect-value">{{ building.defenseValue }}</span>
            </div>
          </div>
        </div>

        <div v-if="missingPrerequisites.length > 0" class="modal-prereq-box">
          <div class="modal-section-title">UNLOCK REQUIREMENTS</div>
          <div
              v-for="req in missingPrerequisites"
              :key="req.type"
              class="modal-prereq-item"
          >
            {{ req.displayName }} Level {{ req.requiredLevel }}
            <span>(current: {{ req.currentLevel }})</span>
          </div>
        </div>

        <div v-if="building.upgradeCost" class="modal-costs-wrap">
          <div class="modal-section-title">REQUIRED RESOURCES</div>
          <div class="costs-grid">
            <div v-for="cost in costItems" :key="cost.label" class="cost-item">
              <span class="cost-label">{{ cost.label }}</span>
              <span class="cost-value" :class="{ 'cost-value--insufficient': !cost.enough }">
                {{ cost.amount }}
              </span>
            </div>
          </div>
        </div>

        <div class="modal-meta">
          <span>⏱ ETA: {{ formatTime(building.buildTimeSeconds) }}</span>
          <span v-if="building.category">{{ building.category.toUpperCase() }}</span>
        </div>

        <div class="modal-queue-info" :class="{ 'modal-queue-info--full': isQueueFull }">
          <span class="queue-info-label">BUILD QUEUE</span>
          <span class="queue-info-value">{{ queueActive }} / {{ queueLimit }}</span>

          <span v-if="isQueueFull" class="queue-info-msg">
            — All active slots occupied; new upgrades will be queued.
          </span>

          <span v-else class="queue-info-msg queue-info-msg--ok">
            — {{ queueLimit - queueActive }} slot{{ queueLimit - queueActive !== 1 ? 's' : '' }} available
          </span>
        </div>

        <!-- Per-building queue breakdown -->
        <div class="modal-building-queue" v-if="buildingQueue.length > 0">
          <div class="modal-section-title">THIS BUILDING QUEUE</div>

          <div class="modal-queue-row" v-for="q in buildingActiveQueue.concat(buildingWaitingQueue)" :key="q.uniqueKey">
            <div class="modal-queue-level">L{{ q.fromLevel ?? (building.level) }} → L{{ q.toLevel ?? q.targetLevel }}</div>
            <div class="modal-queue-status">
              <span v-if="q.isActive" class="modal-queue-active">ACTIVE</span>
              <span v-else class="modal-queue-waiting">WAITING</span>
            </div>
          </div>

          <div class="modal-queue-note">
            Cancel returns 75% resources and removes queued items in reverse order (highest queued level first).
          </div>
        </div>

        <div v-if="upgradeError" class="modal-error">{{ upgradeError }}</div>

        <button
            class="modal-btn"
            :class="{ 'modal-btn--disabled': !canUpgradeNow || upgrading }"
            :disabled="!canUpgradeNow || upgrading"
            @click="doUpgrade"
        >
          {{ buttonLabel }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { upgradeBuilding } from '../services/api.js'

const props = defineProps({
  building: { type: Object, required: true },
  settlement: { type: Object, required: true },
  liveResources: { type: Object, default: null },
  queueActive: { type: Number, default: 0 },
  queueLimit: { type: Number, default: 2 },
  // Future: accept the full queue list so modal can display per-building queued entries
  queueItems: { type: Array, default: () => [] }
})

const emit = defineEmits(['close', 'upgrade'])

const upgrading = ref(false)
const upgradeError = ref('')

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

const icon = computed(() => ICONS[props.building.type] || '🏗️')
const isQueueFull = computed(() => props.queueActive >= props.queueLimit)
const canShowNextLevel = computed(() => !props.building.isFutureFeature && props.building.level < 30)

const hasProduction = computed(() => {
  const p = props.building.hourlyProduction || {}
  return p.water || p.food || p.scrap || p.fuel || p.energy || p.rareTech
})

const hasCurrentEffects = computed(() => {
  return (
      props.building.powerValue > 0 ||
      props.building.populationUsage > 0 ||
      props.building.storageBonus > 0 ||
      props.building.defenseValue > 0
  )
})

const currentProdText = computed(() => {
  const p = props.building.hourlyProduction || {}
  const parts = []
  if (p.water) parts.push(`+${p.water} 💧`)
  if (p.food) parts.push(`+${p.food} 🌾`)
  if (p.scrap) parts.push(`+${p.scrap} ⚙️`)
  if (p.fuel) parts.push(`+${p.fuel} ⛽`)
  if (p.energy) parts.push(`+${p.energy} ⚡`)
  if (p.rareTech) parts.push(`+${p.rareTech} 🧬`)
  return parts.join('  ') + ' /h'
})

const availableResources = computed(() => {
  const live = props.liveResources || {}
  const settlement = props.settlement || {}
  const resources = settlement.resources || settlement.Resources || {}

  return {
    water: live.water ?? settlement.water ?? settlement.Water ?? resources.water ?? resources.Water ?? 0,
    food: live.food ?? settlement.food ?? settlement.Food ?? resources.food ?? resources.Food ?? 0,
    scrap: live.scrap ?? settlement.scrap ?? settlement.Scrap ?? resources.scrap ?? resources.Scrap ?? 0,
    fuel: live.fuel ?? settlement.fuel ?? settlement.Fuel ?? resources.fuel ?? resources.Fuel ?? 0,
    energy: live.energy ?? settlement.energy ?? settlement.Energy ?? resources.energy ?? resources.Energy ?? 0,
    rareTech: live.rareTech ?? settlement.rareTech ?? settlement.RareTech ?? resources.rareTech ?? resources.RareTech ?? 0
  }
})

const costItems = computed(() => {
  const c = props.building.upgradeCost
  const a = availableResources.value
  if (!c) return []

  const items = []
  if (c.water) items.push({ label: '💧 Water', amount: c.water, enough: a.water >= c.water })
  if (c.food) items.push({ label: '🌾 Food', amount: c.food, enough: a.food >= c.food })
  if (c.scrap) items.push({ label: '⚙️ Scrap', amount: c.scrap, enough: a.scrap >= c.scrap })
  if (c.fuel) items.push({ label: '⛽ Fuel', amount: c.fuel, enough: a.fuel >= c.fuel })
  if (c.energy) items.push({ label: '⚡ Energy', amount: c.energy, enough: a.energy >= c.energy })
  if (c.rareTech) items.push({ label: '🧬 RareTech', amount: c.rareTech, enough: a.rareTech >= c.rareTech })
  return items
})

const missingPrerequisites = computed(() => {
  return (props.building.prerequisites || []).filter(req => !req.isMet)
})

const canAfford = computed(() => costItems.value.every(c => c.enough))

// Per-building queue items (from full queue list passed in)
const buildingQueue = computed(() => (props.queueItems || []).filter(q => q.type === props.building.type))
const buildingActiveQueue = computed(() => buildingQueue.value.filter(q => q.isActive))
const buildingWaitingQueue = computed(() => buildingQueue.value.filter(q => q.isWaiting))

const canUpgradeNow = computed(() => {
  return (
      props.building.canUpgrade &&
      !props.building.isFutureFeature &&
      props.building.isBuildable !== false &&
      canAfford.value &&
      missingPrerequisites.value.length === 0
  )
})

const buttonLabel = computed(() => {
  if (upgrading.value) return 'PROCESSING...'
  if (props.building.isFutureFeature) return 'FUTURE FEATURE'
  if (props.building.isBuildable === false) return 'NOT AVAILABLE'
  // Do not block upgrade button when queue is full; waiting entries are allowed.
  if (missingPrerequisites.value.length > 0) return 'REQUIREMENTS NOT MET'
  if (!canAfford.value) return 'INSUFFICIENT RESOURCES'
  if (!props.building.canUpgrade) return 'UPGRADE UNAVAILABLE'
  // If this building already has an active or waiting queue entry, indicate queuing next
  if ((buildingActiveQueue.value.length + buildingWaitingQueue.value.length) > 0) return 'QUEUE NEXT UPGRADE'
  return 'AUTHORIZE UPGRADE'
})

function formatTime(seconds) {
  if (!seconds) return '00:00'
  const h = Math.floor(seconds / 3600)
  const m = Math.floor((seconds % 3600) / 60)
  const s = seconds % 60
  if (h > 0) return `${h}h ${m.toString().padStart(2, '0')}m`
  return `${m}m ${s.toString().padStart(2, '0')}s`
}

async function doUpgrade() {
  if (!props.settlement?.id || !props.building?.type) return

  upgrading.value = true
  upgradeError.value = ''

  try {
    await upgradeBuilding(props.settlement.id, props.building.type)
    emit('upgrade', props.building.type)
  } catch (err) {
    upgradeError.value = err?.response?.data || 'Upgrade failed.'
  } finally {
    upgrading.value = false
  }
}
</script>

<style scoped>
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0,0,0,.9);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}
.modal-card {
  background: var(--bg2);
  border: 1px solid var(--border-bright);
  max-width: 520px;
  width: 92%;
  position: relative;
  overflow: hidden;
  box-shadow: 0 0 60px rgba(0,212,255,.06);
}
.modal-accent {
  height: 2px;
  background: linear-gradient(90deg, transparent, var(--cyan), transparent);
}
.modal-body {
  padding: 24px 28px;
}

.modal-header {
  display: flex;
  gap: 14px;
  align-items: center;
  margin-bottom: 20px;
}
.modal-header-main {
  min-width: 0;
}
.modal-icon {
  font-size: 36px;
}
.modal-name {
  font-size: 16px;
  color: var(--cyan);
  font-family: var(--ff-title);
  font-weight: 700;
  letter-spacing: 2px;
}
.modal-level {
  font-size: 10px;
  color: var(--muted);
  font-family: var(--ff-title);
  letter-spacing: 1px;
}
.modal-close {
  margin-left: auto;
  background: var(--bg3);
  border: 1px solid var(--border);
  color: var(--muted);
  cursor: pointer;
  padding: 4px 12px;
  font-family: var(--ff);
  font-size: 11px;
  font-weight: 600;
}
.modal-close:hover {
  border-color: var(--cyan-dim);
  color: var(--text);
}

.modal-desc {
  font-size: 11px;
  color: var(--text);
  line-height: 1.6;
  margin-bottom: 16px;
  padding-bottom: 14px;
  border-bottom: 1px solid var(--border);
}

.modal-current-prod {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
  padding: 8px 12px;
  background: rgba(48,255,128,.04);
  border: 1px solid rgba(48,255,128,.15);
}

.modal-effects-box,
.modal-prereq-box,
.modal-future-box,
.modal-costs-wrap {
  margin-bottom: 16px;
}

.modal-prod-val {
  font-size: 11px;
  color: var(--green);
  font-family: var(--ff-title);
}

.modal-section-title {
  font-size: 8px;
  color: var(--cyan);
  text-transform: uppercase;
  letter-spacing: 3px;
  font-family: var(--ff-title);
  font-weight: 700;
  margin-bottom: 8px;
}

.modal-effects-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 6px;
}
.modal-effect-item {
  display: flex;
  justify-content: space-between;
  padding: 7px 10px;
  background: var(--bg3);
  border: 1px solid var(--border);
  font-size: 10px;
}
.modal-effect-label {
  color: var(--muted);
  font-family: var(--ff-title);
  letter-spacing: 1px;
}
.modal-effect-value {
  color: var(--cyan);
  font-family: var(--ff-title);
  font-weight: 700;
}

.modal-prereq-box {
  padding: 10px 12px;
  background: rgba(255,154,64,.04);
  border: 1px solid rgba(255,154,64,.18);
}
.modal-prereq-item {
  font-size: 11px;
  color: #ffb070;
  line-height: 1.5;
}
.modal-prereq-item span {
  color: var(--muted);
}

.modal-future-box {
  padding: 10px 12px;
  background: rgba(255,200,48,.05);
  border: 1px solid rgba(255,200,48,.18);
}
.modal-future-text {
  font-size: 11px;
  color: #ffd76a;
  line-height: 1.5;
}

.costs-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 4px;
}
.cost-item {
  display: flex;
  justify-content: space-between;
  padding: 6px 12px;
  background: var(--bg3);
  border: 1px solid var(--border);
  font-size: 11px;
}
.cost-label {
  color: var(--muted);
}
.cost-value {
  color: var(--cyan);
  font-family: var(--ff-title);
  font-weight: 700;
}
.cost-value--insufficient {
  color: var(--red);
}

.modal-meta {
  display: flex;
  justify-content: space-between;
  font-size: 10px;
  color: var(--muted);
  margin-bottom: 12px;
}

.modal-queue-info {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  margin-bottom: 16px;
  border: 1px solid var(--border);
  background: rgba(0,212,255,.02);
  font-size: 10px;
}
.modal-queue-info--full {
  border-color: rgba(255,96,64,.25);
  background: rgba(255,96,64,.04);
}
.queue-info-label {
  font-family: var(--ff-title);
  font-size: 8px;
  color: var(--cyan-dim);
  letter-spacing: 1.5px;
}
.queue-info-value {
  font-family: var(--ff-title);
  font-size: 11px;
  color: var(--cyan);
  font-weight: 700;
}
.modal-queue-info--full .queue-info-value {
  color: #ff6040;
}
.queue-info-msg {
  color: var(--muted);
  font-size: 9px;
}
.queue-info-msg--ok {
  color: var(--green);
}
.modal-queue-info--full .queue-info-msg {
  color: #ff6040;
}

.modal-error {
  color: var(--red);
  font-size: 10px;
  padding: 8px 12px;
  background: rgba(255,48,64,.06);
  border: 1px solid rgba(255,48,64,.2);
  margin-bottom: 12px;
}

.modal-btn {
  width: 100%;
  padding: 12px;
  background: linear-gradient(180deg, var(--cyan-dark), var(--cyan-dim));
  border: 1px solid var(--cyan);
  color: #fff;
  font-family: var(--ff-title);
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  text-transform: uppercase;
  letter-spacing: 3px;
  box-shadow: 0 0 20px rgba(0,212,255,.2);
  transition: all .2s;
}
.modal-btn:hover:not(:disabled) {
  box-shadow: 0 0 30px rgba(0,212,255,.35);
}
.modal-btn--disabled {
  background: var(--bg3);
  border-color: var(--border);
  color: var(--muted);
  box-shadow: none;
  cursor: not-allowed;
}
</style>