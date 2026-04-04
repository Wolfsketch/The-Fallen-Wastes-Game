<template>
  <div class="modal-overlay" @click="$emit('close')">
    <div class="modal-card" @click.stop>
      <div class="modal-accent" />

      <div class="modal-body">
        <div class="modal-header">
          <span class="modal-icon">
            <img v-if="buildingImage" :src="buildingImage" class="modal-icon-img" />
            <span v-else>{{ icon }}</span>
          </span>

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

        <div v-if="nextLevelGains.length > 0" class="modal-next-box">
          <div class="modal-section-title">NEXT LEVEL GAINS</div>
          <div class="modal-next-grid">
            <div v-for="g in nextLevelGains" :key="g.label" class="modal-next-item">
              <span class="modal-next-label">{{ g.label }}</span>
              <div class="modal-next-vals">
                <span class="modal-next-cur">{{ g.current }}</span>
                <span class="modal-next-arrow">→</span>
                <span class="modal-next-nxt">{{ g.next }}</span>
                <span class="modal-next-delta">({{ g.delta }})</span>
              </div>
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
              <img :src="cost.img" class="cost-icon-img" :alt="cost.label" />
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

          <span class="queue-info-msg" v-if="isQueueFull">
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

        </div>

        <div v-if="upgradeError" class="modal-error">{{ upgradeError }}</div>

        <button
            v-if="building.isConstructing && liveRemaining <= 300"
            class="modal-btn modal-btn--free"
            :disabled="instantFinishing"
            @click="doInstantFinish"
        >
          {{ instantFinishing ? 'COMPLETING...' : '⚡ FREE — COMPLETE NOW (' + formatTime(liveRemaining) + ')' }}
        </button>

        <button
            class="modal-btn"
            :class="{ 'modal-btn--disabled': !canUpgradeNow || upgrading }"
            :disabled="!canUpgradeNow || upgrading"
            @click="doUpgrade"
        >
          {{ buttonLabel }}
        </button>

        <div v-if="isQueueFull && canUpgradeNow" style="margin-top:8px; font-size:11px; color:var(--muted);">
          This upgrade will be placed in the waiting queue when active slots are occupied.
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { upgradeBuilding, instantFinishBuilding } from '../services/api.js'
import HQIcon from '../images/BuildingIcons/Headquarter.png'
import ShelterIcon from '../images/BuildingIcons/Shelter.png'
import CouncilHallIcon from '../images/BuildingIcons/council hall.png'
import BarracksIcon from '../images/BuildingIcons/Barracks.png'
import PerimeterWallIcon from '../images/BuildingIcons/PerimeterWall.png'
import GarageIcon from '../images/BuildingIcons/Garage.png'
import CommandoCenterIcon from '../images/BuildingIcons/Commando Center.png'
import FuelRefineryIcon from '../images/BuildingIcons/Fuel Refinery.png'
import FarmDomeIcon from '../images/BuildingIcons/Farm Dome.png'
import ScrapForgeIcon from '../images/BuildingIcons/ScrapForge.png'
import WaterPurifierIcon from '../images/BuildingIcons/WaterPurifier.png'
import SolarArrayIcon from '../images/BuildingIcons/SolarArray.png'
import RelicVaultIcon from '../images/BuildingIcons/RelicVault.png'
import WorkshopIcon from '../images/BuildingIcons/Workshop.png'
import FoodSiloIcon from '../images/BuildingIcons/FoodSilo.png'
import FuelDepotIcon from '../images/BuildingIcons/FuelDepot.png'
import PowerBankIcon from '../images/BuildingIcons/PowerBank.png'
import ScrapVaultIcon from '../images/BuildingIcons/ScrapVault.png'
import WaterTankIcon from '../images/BuildingIcons/WaterTank.png'
import TechVaultIcon from '../images/BuildingIcons/TechVault.png'
import WatchTowerIcon from '../images/BuildingIcons/WatchTower.png'
import TechLabIcon from '../images/BuildingIcons/TechLab.png'
import TechSalvagerIcon from '../images/BuildingIcons/TechSalvager.png'
import resWaterIcon from '../images/Resources/Water.png'
import resFoodIcon from '../images/Resources/Food.png'
import resScrapIcon from '../images/Resources/Scrap.png'
import resFuelIcon from '../images/Resources/Fuel.png'
import resEnergyIcon from '../images/Resources/Energy.png'
import resRareTechIcon from '../images/Resources/RareTech.png'

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
const instantFinishing = ref(false)

const now = ref(Date.now())
let nowTimer = null

onMounted(() => { nowTimer = setInterval(() => { now.value = Date.now() }, 1000) })
onUnmounted(() => { if (nowTimer) clearInterval(nowTimer) })

const liveRemaining = computed(() => {
  if (!props.building.isConstructing) return 0
  if (props.building.constructionEndUtc) {
    const endStr = props.building.constructionEndUtc.endsWith('Z')
      ? props.building.constructionEndUtc
      : `${props.building.constructionEndUtc}Z`
    return Math.max(0, Math.ceil((new Date(endStr).getTime() - now.value) / 1000))
  }
  return props.building.remainingSeconds || 0
})

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

const BUILDING_IMAGES = {
  // ── Available ────────────────────────────────────────────
  HeadQuarter: HQIcon,
  Shelter: ShelterIcon,
  CouncilHall: CouncilHallIcon,
  Barracks: BarracksIcon,
  PerimeterWall: PerimeterWallIcon,
  Garage: GarageIcon,
  CommandCenter: CommandoCenterIcon,
  FuelRefinery: FuelRefineryIcon,
  FarmDome: FarmDomeIcon,
  ScrapForge: ScrapForgeIcon,
  WaterPurifier: WaterPurifierIcon,
  SolarArray: SolarArrayIcon,
  RaidVault: RelicVaultIcon,
  Workshop: WorkshopIcon,
  FoodSilo: FoodSiloIcon,
  FuelDepot: FuelDepotIcon,
  PowerBank: PowerBankIcon,
  ScrapVault: ScrapVaultIcon,
  WaterTank: WaterTankIcon,
  TechVault: TechVaultIcon,
  WatchTower: WatchTowerIcon,
  TechLab: TechLabIcon,
  TechSalvager: TechSalvagerIcon,
}
const buildingImage = computed(() => BUILDING_IMAGES[props.building.type] || null)

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

const nextLevelGains = computed(() => {
  const b = props.building
  const type = b.type
  const lvl = b.level
  const next = lvl + 1
  if (b.isFutureFeature || next > 30 || !b.canUpgrade) return []

  const gains = []

  const PROD_MAP = {
    FarmDome: '🌾 Food /h', FuelRefinery: '⛽ Fuel /h',
    ScrapForge: '⚙️ Scrap /h', WaterPurifier: '💧 Water /h', SolarArray: '⚡ Energy /h',
  }
  if (PROD_MAP[type]) {
    const calc = l => l <= 0 ? 0 : Math.round(23 * l * Math.pow(1.12, l - 1))
    const cur = calc(lvl); const nxt = calc(next)
    gains.push({ label: PROD_MAP[type], current: cur > 0 ? `+${cur.toLocaleString()}` : '—', next: `+${nxt.toLocaleString()}`, delta: `+${(nxt - cur).toLocaleString()}` })
  }

  const STORE_MAP = {
    WaterTank: '💧 Water storage', FoodSilo: '🌾 Food storage',
    ScrapVault: '⚙️ Scrap storage', FuelDepot: '⛽ Fuel storage', PowerBank: '⚡ Energy storage',
  }
  if (STORE_MAP[type]) {
    const calc = l => l <= 0 ? 0 : 1000 + ((l - 1) * 900)
    gains.push({ label: STORE_MAP[type], current: calc(lvl).toLocaleString(), next: calc(next).toLocaleString(), delta: '+900' })
  }

  if (type === 'TechVault') {
    const calc = l => l <= 0 ? 0 : 200 + ((l - 1) * 180)
    gains.push({ label: '🧬 RareTech storage', current: calc(lvl).toLocaleString(), next: calc(next).toLocaleString(), delta: '+180' })
  }

  if (type === 'RaidVault') {
    const curVal = lvl >= 10 ? '∞' : (lvl * 100).toLocaleString()
    const nxtVal = next >= 10 ? '∞ (unlimited)' : (next * 100).toLocaleString()
    gains.push({ label: '📦 Loot capacity', current: curVal, next: nxtVal, delta: next >= 10 ? '→ ∞' : '+100' })
  }

  if (type === 'PerimeterWall') {
    const calc = l => l <= 0 ? 0 : Math.round(35 * l * Math.pow(1.11, l - 1))
    const cur = calc(lvl); const nxt = calc(next)
    gains.push({ label: '🛡️ Defense strength', current: cur.toLocaleString(), next: nxt.toLocaleString(), delta: `+${(nxt - cur).toLocaleString()}` })
  }

  if (type === 'Shelter') {
    const calc = l => l <= 0 ? 0 : 200 + (35 * (l - 1)) + (6 * (l - 1) * (l - 1))
    const cur = calc(lvl); const nxt = calc(next)
    gains.push({ label: '👥 Population cap', current: cur.toLocaleString(), next: nxt.toLocaleString(), delta: `+${(nxt - cur).toLocaleString()}` })
  }

  if (['Barracks', 'Garage', 'Workshop', 'CommandCenter'].includes(type)) {
    const calc = l => l <= 0 ? 0 : (l - 1) * 5
    gains.push({ label: '⚡ Train speed bonus', current: `+${calc(lvl)}%`, next: `+${calc(next)}%`, delta: '+5%' })
  }

  if (type === 'HeadQuarter') {
    const calc = l => Math.min(45, l * 1.5)
    const cur = calc(lvl); const nxt = calc(next)
    gains.push({ label: '🔨 Build time reduction', current: `-${cur.toFixed(1)}%`, next: `-${nxt.toFixed(1)}%`, delta: `+${(nxt - cur).toFixed(1)}%` })
  }

  if (type === 'TechLab') {
    gains.push({ label: '🔬 Research speed', current: `+${lvl * 5}%`, next: `+${next * 5}%`, delta: '+5%' })
  }

  return gains
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
  if (c.water)    items.push({ img: resWaterIcon,    label: 'Water',    amount: c.water,    enough: a.water >= c.water })
  if (c.food)     items.push({ img: resFoodIcon,     label: 'Food',     amount: c.food,     enough: a.food >= c.food })
  if (c.scrap)    items.push({ img: resScrapIcon,    label: 'Scrap',    amount: c.scrap,    enough: a.scrap >= c.scrap })
  if (c.fuel)     items.push({ img: resFuelIcon,     label: 'Fuel',     amount: c.fuel,     enough: a.fuel >= c.fuel })
  if (c.energy)   items.push({ img: resEnergyIcon,   label: 'Energy',   amount: c.energy,   enough: a.energy >= c.energy })
  if (c.rareTech) items.push({ img: resRareTechIcon, label: 'RareTech', amount: c.rareTech, enough: a.rareTech >= c.rareTech })
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
  const upgradeFlag = props.building.canUpgrade || props.building.isConstructing
  return (
      upgradeFlag &&
      !props.building.isFutureFeature &&
      props.building.isBuildable !== false &&
      canAfford.value &&
      missingPrerequisites.value.length === 0 &&
      !props.building.queueFull
  )
})

const buttonLabel = computed(() => {
  if (upgrading.value) return 'PROCESSING...'
  if (props.building.isFutureFeature) return 'FUTURE FEATURE'
  if (props.building.isBuildable === false) return 'NOT AVAILABLE'
  if (missingPrerequisites.value.length > 0) return 'REQUIREMENTS NOT MET'
  if (!canAfford.value) return 'INSUFFICIENT RESOURCES'
  if (!props.building.canUpgrade) return 'UPGRADE UNAVAILABLE'

  // If this building already has queued items, suggest queuing the next upgrade
  // If the player's active slots are full, prefer 'QUEUE NEXT UPGRADE' so intent is clear
  if (isQueueFull.value) return 'QUEUE NEXT UPGRADE'
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

async function doInstantFinish() {
  if (!props.settlement?.id) return
  instantFinishing.value = true
  upgradeError.value = ''
  try {
    await instantFinishBuilding(props.settlement.id)
    emit('upgrade', props.building.type)
  } catch (err) {
    upgradeError.value = err?.response?.data || 'Instant finish failed.'
  } finally {
    instantFinishing.value = false
  }
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
  gap: 16px;
  align-items: flex-start;
  margin-bottom: 20px;
}
.modal-header-main {
  min-width: 0;
  flex: 1;
}
.modal-icon {
  font-size: 52px;
  line-height: 1;
  flex-shrink: 0;
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
  align-items: center;
  gap: 6px;
  justify-content: space-between;
  padding: 6px 12px;
  background: var(--bg3);
  border: 1px solid var(--border);
  font-size: 11px;
}
.cost-icon-img {
  width: 36px;
  height: 36px;
  object-fit: contain;
  flex-shrink: 0;
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
.modal-btn--free {
  background: linear-gradient(180deg, rgba(61,255,156,.18), rgba(61,255,156,.08));
  border-color: #3dff9c;
  color: #3dff9c;
  box-shadow: 0 0 16px rgba(61,255,156,.18);
  margin-bottom: 8px;
}
.modal-btn--free:hover:not(:disabled) {
  background: linear-gradient(180deg, rgba(61,255,156,.28), rgba(61,255,156,.14));
  box-shadow: 0 0 24px rgba(61,255,156,.3);
}

/* ── Building image icon ───────────────────────────────── */
.modal-icon { display: flex; align-items: center; justify-content: center; width: 96px; height: 96px; flex-shrink: 0; overflow: hidden; }
.modal-icon-img { width: 100%; height: 100%; object-fit: contain; transform: scale(1); }

/* ── Next Level Gains ──────────────────────────────────── */
.modal-next-box {
  margin-bottom: 16px;
  border: 1px solid rgba(0,212,255,.15);
  background: rgba(0,212,255,.03);
  padding: 12px 14px;
}
.modal-next-grid { display: flex; flex-direction: column; gap: 7px; }
.modal-next-item { display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 4px; }
.modal-next-label { font-size: 10px; color: var(--text); }
.modal-next-vals { display: flex; align-items: center; gap: 5px; }
.modal-next-cur { font-size: 10px; color: var(--muted); font-family: var(--ff-title); letter-spacing: .5px; }
.modal-next-arrow { font-size: 9px; color: var(--cyan-dark); }
.modal-next-nxt { font-size: 11px; color: var(--cyan); font-family: var(--ff-title); font-weight: 700; letter-spacing: .5px; }
.modal-next-delta { font-size: 8px; color: var(--green); font-family: var(--ff-title); letter-spacing: .5px; }
</style>