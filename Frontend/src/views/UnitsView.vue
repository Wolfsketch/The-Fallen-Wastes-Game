<template>
  <div class="units-page">
    <div class="page-header">
      <div>
        <h1 class="page-title">UNITS</h1>
        <p class="page-subtitle">Classified wasteland personnel registry</p>
      </div>

      <div class="army-power-grid" v-if="!loading && totalUnitsOwned > 0">
        <div class="power-stat">
          <img :src="ballisticIcon" alt="Ballistic" class="power-icon" />
          <div class="power-details">
            <div class="power-label">BALLISTIC</div>
            <div class="power-values">
              <span class="power-attack">{{ totalArmyPower.ballistic }}</span>
              <span class="power-separator">/</span>
              <span class="power-defense">{{ totalArmyDefense.ballistic }}</span>
            </div>
          </div>
        </div>

        <div class="power-stat">
          <img :src="impactIcon" alt="Impact" class="power-icon" />
          <div class="power-details">
            <div class="power-label">IMPACT</div>
            <div class="power-values">
              <span class="power-attack">{{ totalArmyPower.impact }}</span>
              <span class="power-separator">/</span>
              <span class="power-defense">{{ totalArmyDefense.impact }}</span>
            </div>
          </div>
        </div>

        <div class="power-stat">
          <img :src="energyIcon" alt="Energy" class="power-icon" />
          <div class="power-details">
            <div class="power-label">ENERGY</div>
            <div class="power-values">
              <span class="power-attack">{{ totalArmyPower.energy }}</span>
              <span class="power-separator">/</span>
              <span class="power-defense">{{ totalArmyDefense.energy }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="queue-panel" v-if="!loading && hasAnyFacility">
      <div class="queue-header">
        <div class="queue-title-row">
          <span class="queue-title">TRAINING QUEUE</span>
          <span class="queue-slots" :class="{ 'queue-slots--full': trainingQueueFull }">
            {{ trainingQueue.length }} / {{ trainingQueueLimit }}
          </span>
        </div>

        <div
            class="queue-commander"
            v-if="!trainingCommanderActive"
            @click="showTrainingCommanderInfo = !showTrainingCommanderInfo"
        >
          <span class="commander-icon">⭐</span>
          <span class="commander-text">COMMANDER: +5 SLOTS</span>
        </div>

        <div class="queue-commander queue-commander--active" v-else>
          <span class="commander-icon">⭐</span>
          <span class="commander-text">COMMANDER ACTIVE</span>
        </div>
      </div>

      <div class="commander-info" v-if="showTrainingCommanderInfo && !trainingCommanderActive">
        <div class="commander-info-text">
          Activate a <strong>Commander</strong> to expand your training queue from 2 to 7 simultaneous recruitments.
        </div>
        <button class="commander-activate-btn" @click="activateTrainingCommander">
          ACTIVATE COMMANDER (14 DAYS)
        </button>
      </div>

      <div class="queue-items" v-if="trainingQueue.length > 0">
        <div
            v-for="(item, idx) in trainingQueue"
            :key="item.id ?? `queue-${idx}`"
            class="queue-item"
        >
          <div class="queue-item-index">{{ idx + 1 }}</div>

          <div class="queue-item-portrait">
            <img
                v-if="item?.unitName && getQueueUnitImage(item.unitName)"
                :src="getQueueUnitImage(item.unitName)"
                :alt="item.unitName"
                class="queue-item-portrait-img"
            />
            <div v-else class="queue-item-portrait-fallback">
              {{ getUnitInitial(item?.unitName || '?') }}
            </div>
          </div>

          <div class="queue-item-info">
            <div class="queue-item-name">{{ item?.unitName || 'Unknown Unit' }}</div>
            <div class="queue-item-meta">×{{ item?.quantity ?? 0 }}</div>
          </div>

                  <div class="queue-item-timer">
                    <!-- Active training shows countdown and progress. Waiting items show a WAITING label and empty bar. -->
                    <div v-if="item.isActive" class="queue-item-time">{{ formatQueueTime(getQueueRemaining(item)) }}</div>
                    <div v-else class="queue-item-waiting">WAITING</div>

                    <div class="queue-item-bar">
                      <div v-if="item.isActive" class="queue-item-bar-fill" :style="{ width: getQueuePct(item) + '%' }" />
                      <div v-else class="queue-item-bar-empty" />
                    </div>
                  </div>
        </div>
      </div>

      <div class="queue-empty" v-else>
        <span class="queue-empty-text">
          No active training orders. Recruitment bays are standing by.
        </span>
      </div>

      <div class="queue-slot-dots">
        <div
            v-for="i in trainingQueueLimit"
            :key="i"
            class="queue-dot"
            :class="{
            'queue-dot--active': i <= trainingQueue.length,
            'queue-dot--commander': i > 2
          }"
        />
      </div>
    </div>

    <div v-if="loading" class="units-empty">
      <div class="empty-core">◌</div>
      <div class="empty-title">LOADING UNIT DATA</div>
      <div class="empty-subtitle">Accessing tactical archives...</div>
    </div>

    <div v-else-if="error" class="units-empty units-empty--error">
      <div class="empty-core">!</div>
      <div class="empty-title">DATA LINK FAILURE</div>
      <div class="empty-subtitle">{{ error }}</div>
    </div>

    <div v-else-if="!hasAnyFacility" class="units-empty">
      <div class="empty-core">🔒</div>
      <div class="empty-title">NO MILITARY FACILITIES</div>
      <div class="empty-subtitle">
        Build a <strong>Barracks</strong> to start recruiting units.
      </div>

      <div class="empty-hint">
        <div class="hint-row" v-for="f in facilities" :key="f.building">
          <span class="hint-icon">{{ f.locked ? '🔒' : '✓' }}</span>
          <span class="hint-name">{{ f.label }}</span>
          <span class="hint-desc">{{ f.unlocksLabel }}</span>
        </div>
      </div>
    </div>

    <template v-else>
      <div class="units-filters">
        <div class="filter-row">
          <button
              v-for="f in facilities"
              :key="f.building"
              class="filter-btn"
              :class="{ 'filter-btn--active': facilityFilter === f.building, 'filter-btn--locked': f.locked }"
              @click="!f.locked && selectFacility(f.building)"
              :disabled="f.locked"
          >
            <span v-if="f.locked" class="lock-icon">🔒</span>
            {{ f.label }}
            <span v-if="!f.locked" class="filter-lvl">L{{ f.level }}</span>
          </button>
        </div>

        <div class="filter-row" v-if="facilityFilter && availableTypes.length > 1">
          <button
              v-for="t in availableTypes"
              :key="t"
              class="filter-btn filter-btn--sm"
              :class="{ 'filter-btn--active': typeFilter === t }"
              @click="typeFilter = t"
          >
            {{ t }}
          </button>
        </div>
      </div>

      <div v-if="!filteredUnits.length" class="units-empty units-empty--small">
        <div class="empty-core">◌</div>
        <div class="empty-title">NO MATCHING UNITS</div>
        <div class="empty-subtitle">Adjust filters or upgrade your facility.</div>
      </div>

      <div v-else class="units-grid">
        <article v-for="unit in filteredUnits" :key="unit.name" class="unit-card">
          <div class="unit-visual">
            <div class="unit-visual-frame">
              <img
                  v-if="getUnitImage(unit.name)"
                  :src="getUnitImage(unit.name)"
                  :alt="unit.name"
                  class="unit-portrait"
              />
              <template v-else>
                <div class="unit-visual-placeholder">{{ getUnitInitial(unit.name) }}</div>
                <div class="unit-visual-label">NO IMAGE</div>
              </template>
            </div>

            <div class="unit-owned-bar">
              <span class="unit-owned-label">OWNED</span>
              <span class="unit-owned-value">{{ getUnitQuantity(unit.name) }}</span>
            </div>
          </div>

          <div class="unit-main">
            <div class="unit-card-top">
              <div class="unit-card-title-wrap">
                <div class="unit-name">{{ unit.name }}</div>
                <div class="unit-meta-row">
                  <span class="unit-badge">{{ unit.unitType }}</span>
                  <span class="unit-badge unit-badge--facility">{{ formatFacility(unit.facility) }}</span>
                  <span class="unit-badge unit-badge--type">{{ unit.attackType }}</span>
                </div>
              </div>
              <div class="unit-build-time">{{ formatBuildTime(unit.buildTimeSeconds) }}</div>
            </div>

            <p class="unit-description">{{ unit.description }}</p>
            <div class="unit-role">ROLE: {{ unit.role }}</div>

            <div class="unit-stats-compact unit-stats-compact--top">
              <div class="stat-pill stat-pill--attack stat-hoverable" :title="`${formatAttackType(unit.attackType)} damage`">
                <div class="stat-pill-top">
                  <div class="stat-icon-wrap stat-icon-wrap--attack">
                    <img :src="getAttackTypeIcon(unit.attackType)" :alt="unit.attackType" class="stat-pill-icon" />
                  </div>
                  <span>{{ formatAttackType(unit.attackType) }}</span>
                </div>
                <strong>{{ unit.attackPower }}</strong>
              </div>

              <div class="stat-pill stat-hoverable" title="Movement speed">
                <div class="stat-pill-top">
                  <div class="stat-icon-wrap"><img :src="speedIcon" alt="Speed" class="stat-pill-icon" /></div>
                  <span>SPD</span>
                </div>
                <strong>{{ unit.speed }}</strong>
              </div>

              <div class="stat-pill stat-hoverable" title="Carry capacity">
                <div class="stat-pill-top">
                  <div class="stat-icon-wrap"><img :src="carryIcon" alt="Carry" class="stat-pill-icon" /></div>
                  <span>CARRY</span>
                </div>
                <strong>{{ unit.carryCapacity }}</strong>
              </div>
            </div>

            <div class="defense-panel">
              <div class="defense-title">DEFENSE PROFILE</div>
              <div class="defense-grid">
                <div class="secondary-stat secondary-stat--defense stat-hoverable" title="Defense vs Ballistic">
                  <div class="secondary-top">
                    <div class="stat-icon-wrap stat-icon-wrap--defense">
                      <img :src="ballisticIcon" alt="Ballistic" class="defense-icon" />
                    </div>
                    <span class="secondary-label">BALLISTIC</span>
                  </div>
                  <span class="secondary-value">{{ unit.defenseVsBallistic }}</span>
                </div>

                <div class="secondary-stat secondary-stat--defense stat-hoverable" title="Defense vs Impact">
                  <div class="secondary-top">
                    <div class="stat-icon-wrap stat-icon-wrap--defense">
                      <img :src="impactIcon" alt="Impact" class="defense-icon" />
                    </div>
                    <span class="secondary-label">IMPACT</span>
                  </div>
                  <span class="secondary-value">{{ unit.defenseVsImpact }}</span>
                </div>

                <div class="secondary-stat secondary-stat--defense stat-hoverable" title="Defense vs Energy">
                  <div class="secondary-top">
                    <div class="stat-icon-wrap stat-icon-wrap--defense">
                      <img :src="energyIcon" alt="Energy" class="defense-icon" />
                    </div>
                    <span class="secondary-label">ENERGY</span>
                  </div>
                  <span class="secondary-value">{{ unit.defenseVsEnergy }}</span>
                </div>
              </div>
            </div>

            <div class="unit-cost">
              <div class="cost-title">
                RECRUITMENT COST
                <span class="cost-quantity" v-if="getQuantity(unit.name) > 1">
                  (×{{ getQuantity(unit.name) }})
                </span>
              </div>

              <div class="cost-grid">
                <div
                    class="cost-item cost-item--hover"
                    :class="{ 'cost-item--insufficient': !hasEnoughResource(unit, 'water', getQuantity(unit.name)) }"
                    title="Water"
                >
                  <span class="cost-icon">💧</span>
                  <span>{{ getTotalCost(unit, 'water', getQuantity(unit.name)) }}</span>
                </div>

                <div
                    class="cost-item cost-item--hover"
                    :class="{ 'cost-item--insufficient': !hasEnoughResource(unit, 'food', getQuantity(unit.name)) }"
                    title="Food"
                >
                  <span class="cost-icon">🥫</span>
                  <span>{{ getTotalCost(unit, 'food', getQuantity(unit.name)) }}</span>
                </div>

                <div
                    class="cost-item cost-item--hover"
                    :class="{ 'cost-item--insufficient': !hasEnoughResource(unit, 'population', getQuantity(unit.name)) }"
                    title="Population"
                >
                  <span class="cost-icon">👥</span>
                  <span>{{ unit.capacityCost * getQuantity(unit.name) }}</span>
                </div>

                <div
                    class="cost-item cost-item--hover"
                    :class="{ 'cost-item--insufficient': !hasEnoughResource(unit, 'scrap', getQuantity(unit.name)) }"
                    title="Scrap"
                >
                  <span class="cost-icon">⚙️</span>
                  <span>{{ getTotalCost(unit, 'scrap', getQuantity(unit.name)) }}</span>
                </div>

                <div
                    class="cost-item cost-item--hover"
                    :class="{ 'cost-item--insufficient': !hasEnoughResource(unit, 'fuel', getQuantity(unit.name)) }"
                    title="Fuel"
                >
                  <span class="cost-icon">⛽</span>
                  <span>{{ getTotalCost(unit, 'fuel', getQuantity(unit.name)) }}</span>
                </div>

                <div
                    class="cost-item cost-item--hover"
                    :class="{ 'cost-item--insufficient': !hasEnoughResource(unit, 'energy', getQuantity(unit.name)) }"
                    title="Energy"
                >
                  <span class="cost-icon">⚡</span>
                  <span>{{ getTotalCost(unit, 'energy', getQuantity(unit.name)) }}</span>
                </div>

                <div
                    class="cost-item cost-item--hover"
                    :class="{ 'cost-item--insufficient': !hasEnoughResource(unit, 'rareTech', getQuantity(unit.name)) }"
                    title="Rare Tech"
                >
                  <span class="cost-icon">🧬</span>
                  <span>{{ getTotalCost(unit, 'rareTech', getQuantity(unit.name)) }}</span>
                </div>
              </div>
            </div>

            <div class="training-panel">
              <div class="training-title">RECRUITMENT CONTROL</div>
              <div class="unit-actions">
                <div class="train-qty-box">
                  <button class="qty-btn" @click="decreaseQuantity(unit.name)" :disabled="getQuantity(unit.name) <= 1" type="button">-</button>
                  <input class="qty-input" type="number" min="1" :value="getQuantity(unit.name)" @input="setQuantity(unit.name, $event.target.value)" />
                  <button class="qty-btn" @click="increaseQuantity(unit.name)" type="button">+</button>
                  <button class="qty-btn qty-btn--max" @click="setMaxQuantity(unit)" type="button">MAX</button>
                </div>

                <button
                    class="train-btn"
                    :class="{ 'train-btn--disabled': !canAffordUnit(unit, getQuantity(unit.name)) || trainingBusy || trainingQueueFull }"
                    :disabled="!canAffordUnit(unit, getQuantity(unit.name)) || trainingBusy || trainingQueueFull"
                    type="button"
                    @click="trainSelectedUnit(unit)"
                >
                  {{
                    trainingBusy
                        ? 'PROCESSING'
                        : trainingQueueFull
                            ? 'QUEUE FULL'
                            : !canAffordUnit(unit, getQuantity(unit.name))
                                ? 'INSUFFICIENT RESOURCES'
                                : 'TRAIN'
                  }}
                </button>
              </div>
            </div>
          </div>
        </article>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import {
  getUnits,
  getBuildings,
  getSettlement,
  getUnitTrainingQueue,
  completeReadyUnitTraining,
  trainUnit,
  activateCommanderApi
} from '../services/api'

import scavengerImg from '../images/Scavenger.png'
import raiderImg from '../images/Raider.png'
import outpostDefenderImg from '../images/Outpost Defender.png'
import riflemanImg from '../images/Rifleman.png'
import shockFighterImg from '../images/Shock Fighter.png'
import sniperImg from '../images/Sniper.png'
import flameTrooperImg from '../images/Flame Trooper.png'
import powerTrooperImg from '../images/Power Trooper.png'
import assaultBikeImg from '../images/Assault Bike.png'
import rustBuggyImg from '../images/Rust Buggy.png'
import warRigImg from '../images/War Rig.png'
import interceptorImg from '../images/Interceptor.png'
import siegeCarrierImg from '../images/Siege Carrier.png'
import scoutDroneImg from '../images/Scout Drone.png'
import convoyImg from '../images/Convoy.png'
import wallBreakerImg from '../images/Wall Breaker.png'

import ballisticIcon from '../images/special icons/Ballistic.png'
import impactIcon from '../images/special icons/Impact.png'
import energyIcon from '../images/special icons/Energy.png'
import speedIcon from '../images/special icons/Speed.png'
import carryIcon from '../images/special icons/Carry.png'

const props = defineProps({
  player: Object,
  settlement: Object,
  refreshSettlement: Function,
  liveResources: {
    type: Object,
    default: () => ({})
  },
  livePopulation: {
    type: Object,
    default: () => ({})
  }
})

const unitQuantities = ref({})
const units = ref([])
const buildings = ref([])
const loading = ref(true)
const error = ref('')
const now = ref(Date.now())
const trainingBusy = ref(false)
let tickInterval = null

const facilityFilter = ref('')
const typeFilter = ref('All')
const showTrainingCommanderInfo = ref(false)

const unitInventory = ref({})
const trainingQueue = ref([])
const queueTotals = ref({}) // Store initial remainingSeconds as total duration
const trainingQueueLimit = ref(2) // Dynamic limit from API (default 2 free slots)

const trainingCommanderActive = computed(() =>
    props.player?.advisors?.commander?.active ?? false
)

const trainingQueueFull = computed(() => {
  return trainingQueue.value.length >= trainingQueueLimit.value
})

const totalUnitsOwned = computed(() =>
    Object.values(unitInventory.value).reduce((sum, qty) => sum + qty, 0)
)

const totalArmyPower = computed(() => {
  const power = { ballistic: 0, impact: 0, energy: 0 }

  units.value.forEach(unit => {
    const quantity = getUnitQuantity(unit.name)
    if (quantity > 0) {
      const damageType = String(unit.attackType || '').toLowerCase()
      if (damageType === 'ballistic') power.ballistic += unit.attackPower * quantity
      else if (damageType === 'impact') power.impact += unit.attackPower * quantity
      else if (damageType === 'energy') power.energy += unit.attackPower * quantity
    }
  })

  return power
})

const totalArmyDefense = computed(() => {
  const defense = { ballistic: 0, impact: 0, energy: 0 }

  units.value.forEach(unit => {
    const quantity = getUnitQuantity(unit.name)
    if (quantity > 0) {
      defense.ballistic += unit.defenseVsBallistic * quantity
      defense.impact += unit.defenseVsImpact * quantity
      defense.energy += unit.defenseVsEnergy * quantity
    }
  })

  return defense
})

const unitImages = {
  Scavenger: scavengerImg,
  Raider: raiderImg,
  'Outpost Defender': outpostDefenderImg,
  Rifleman: riflemanImg,
  'Shock Fighter': shockFighterImg,
  Sniper: sniperImg,
  'Flame Trooper': flameTrooperImg,
  'Power Trooper': powerTrooperImg,
  'Assault Bike': assaultBikeImg,
  'Rust Buggy': rustBuggyImg,
  'War Rig': warRigImg,
  Interceptor: interceptorImg,
  'Siege Carrier': siegeCarrierImg,
  'Scout Drone': scoutDroneImg,
  Convoy: convoyImg,
  'Wall Breaker': wallBreakerImg
}

const FACILITY_MAP = [
  { building: 'Barracks', label: 'Barracks', unlocksLabel: 'Infantry and frontline troops' },
  { building: 'Garage', label: 'Garage', unlocksLabel: 'Vehicles and heavy strike units' },
  { building: 'Workshop', label: 'Workshop', unlocksLabel: 'Siege and advanced tech units' },
  { building: 'CommandCenter', label: 'Command Center', unlocksLabel: 'Strategic, recon and convoy units' }
]

const facilities = computed(() =>
    FACILITY_MAP.map(f => {
      const b = buildings.value.find(bld => bld.type === f.building)
      const level = b?.level ?? 0
      return { ...f, level, locked: level === 0 }
    })
)

const hasAnyFacility = computed(() => facilities.value.some(f => !f.locked))

const availableTypes = computed(() => {
  if (!facilityFilter.value) return []
  const types = new Set(units.value.filter(u => u.facility === facilityFilter.value).map(u => u.unitType))
  return ['All', ...Array.from(types).sort()]
})

const filteredUnits = computed(() =>
    units.value.filter(u => {
      if (!facilityFilter.value) return false
      return u.facility === facilityFilter.value && (typeFilter.value === 'All' || u.unitType === typeFilter.value)
    })
)

const availablePopulation = computed(() => {
  if (props.livePopulation?.availablePopulation != null) {
    return props.livePopulation.availablePopulation
  }

  return props.settlement?.availablePopulation
      ?? props.settlement?.AvailablePopulation
      ?? Math.max(
          0,
          (props.settlement?.populationCapacity ?? 0) - (props.settlement?.usedPopulation ?? 0)
      )
})

function normalizeUnit(raw) {
  return {
    name: raw.name ?? raw.Name ?? '',
    description: raw.description ?? raw.Description ?? '',
    role: raw.role ?? raw.Role ?? '',
    unitType: raw.unitType ?? raw.UnitType ?? '',
    facility: raw.facility ?? raw.Facility ?? '',
    attackType: raw.attackType ?? raw.AttackType ?? '',
    iconKey: raw.iconKey ?? raw.IconKey ?? '',
    attackPower: raw.attackPower ?? raw.AttackPower ?? 0,
    defenseVsBallistic: raw.defenseVsBallistic ?? raw.DefenseVsBallistic ?? 0,
    defenseVsImpact: raw.defenseVsImpact ?? raw.DefenseVsImpact ?? 0,
    defenseVsEnergy: raw.defenseVsEnergy ?? raw.DefenseVsEnergy ?? 0,
    speed: raw.speed ?? raw.Speed ?? 0,
    capacityCost: raw.capacityCost ?? raw.CapacityCost ?? 0,
    carryCapacity: raw.carryCapacity ?? raw.CarryCapacity ?? 0,
    upkeep: raw.upkeep ?? raw.Upkeep ?? 0,
    buildTimeSeconds: raw.buildTimeSeconds ?? raw.BuildTimeSeconds ?? 0,
    cost: raw.cost ?? raw.Cost ?? {}
  }
}

function normalizeBuilding(raw) {
  return {
    type: raw.type ?? raw.Type ?? '',
    level: raw.level ?? raw.Level ?? 0
  }
}

function normalizeTrainingQueue(raw) {
  const queueRaw = raw?.queue ?? raw?.Queue ?? raw ?? []
  const queue = Array.isArray(queueRaw) ? queueRaw.filter(Boolean) : []
  return queue
      .map((item, idx) => {
        // Normalize basic fields
        const base = {
          id: item?.id ?? item?.Id ?? idx,
          unitName: item?.unitName ?? item?.UnitName ?? 'Unknown Unit',
          quantity: item?.quantity ?? item?.Quantity ?? 0,
          startedAtUtc: item?.startedAtUtc ?? item?.StartedAtUtc ?? null,
          completesAtUtc: item?.completesAtUtc ?? item?.CompletesAtUtc ?? null,
          remainingSeconds: item?.remainingSeconds ?? item?.RemainingSeconds ?? 0,
          totalDurationSeconds: item?.totalDurationSeconds ?? item?.TotalDurationSeconds ?? 0,
          queueOrder: item?.queueOrder ?? item?.QueueOrder ?? idx
        }

        // Derive status: prefer explicit fields but fall back to heuristics
        let rawStatus = item?.status ?? item?.Status ?? item?.state ?? item?.State
        if (typeof rawStatus === 'string') {
          const s = rawStatus.toLowerCase()
          if (s.includes('wait') || s.includes('queue') || s.includes('queued')) rawStatus = 'waiting'
          else if (s.includes('complete') || s.includes('done')) rawStatus = 'completed'
          else if (s.includes('active') || s.includes('inprogress') || s.includes('training')) rawStatus = 'active'
        }

        const hintActive = item?.isActive ?? item?.IsActive ?? item?.active ?? item?.Active
        const hintWaiting = item?.isWaiting ?? item?.IsWaiting ?? item?.queuePosition ?? item?.QueuePosition
        const hintCompleted = item?.isCompleted ?? item?.IsCompleted ?? item?.completed ?? item?.Completed

        let status = rawStatus
        if (!status) {
          // If startedAtUtc is present or this is the first item (queueOrder === 0 or 1), treat as active
          if (hintActive || base.startedAtUtc || base.queueOrder === 0 || base.queueOrder === 1) status = 'active'
          else if (hintWaiting || (typeof base.queueOrder === 'number' && base.queueOrder > 1)) status = 'waiting'
          else if (hintCompleted) status = 'completed'
        }

        const isActive = status === 'active'
        const isWaiting = status === 'waiting'
        const isCompleted = status === 'completed'

        // Add aliases matching building queue item naming so shared helpers can be used
        const normalized = { ...base, status, isActive, isWaiting, isCompleted }
        normalized.constructionEndUtc = normalized.completesAtUtc
        normalized.buildTimeSeconds = normalized.totalDurationSeconds
        return normalized
      })
      .filter(q => !q.isCompleted)
      .sort((a, b) => a.queueOrder - b.queueOrder)
}

function selectFacility(building) {
  facilityFilter.value = building
  typeFilter.value = 'All'
}

function getUnitImage(name) {
  return unitImages[name] || null
}

function getUnitInitial(name) {
  return name?.charAt(0)?.toUpperCase() || '?'
}

function formatAttackType(type) {
  return String(type || 'ATTACK').toUpperCase()
}

function formatFacility(v) {
  return v === 'CommandCenter' ? 'Command Center' : v
}

function getAttackTypeIcon(type) {
  const n = String(type || '').toLowerCase()
  if (n === 'ballistic') return ballisticIcon
  if (n === 'impact') return impactIcon
  if (n === 'energy') return energyIcon
  return ballisticIcon
}

function formatBuildTime(seconds) {
  if (!seconds) return '--'
  const mins = Math.floor(seconds / 60)
  const secs = seconds % 60
  if (mins <= 0) return `${secs}s`
  if (secs === 0) return `${mins}m`
  return `${mins}m ${secs}s`
}

function getUnitQuantity(unitName) {
  return unitInventory.value[unitName] || 0
}

function getTotalCost(unit, resourceType, quantity) {
  if (resourceType === 'population') return unit.capacityCost * quantity
  const baseCost = unit.cost?.[resourceType] ?? 0
  return baseCost * quantity
}

function getCurrentResource(resourceType) {
  const live = props.liveResources || {}
  const settlement = props.settlement || {}
  const resources = settlement.resources || settlement.Resources || {}

  return (
      live[resourceType] ??
      resources[resourceType] ??
      resources[capitalize(resourceType)] ??
      settlement[resourceType] ??
      settlement[capitalize(resourceType)] ??
      0
  )
}

function hasEnoughResource(unit, resourceType, quantity) {
  const totalCost = getTotalCost(unit, resourceType, quantity)
  if (totalCost === 0) return true

  if (resourceType === 'population') {
    return availablePopulation.value >= totalCost
  }

  const current = getCurrentResource(resourceType)
  return current >= totalCost
}

function canAffordUnit(unit, quantity) {
  return ['water', 'food', 'scrap', 'fuel', 'energy', 'rareTech', 'population']
      .every(resource => hasEnoughResource(unit, resource, quantity))
}

function getQuantity(unitName) {
  return unitQuantities.value[unitName] ?? 1
}

function setQuantity(unitName, value) {
  unitQuantities.value[unitName] = Math.max(1, parseInt(value, 10) || 1)
}

function increaseQuantity(unitName) {
  unitQuantities.value[unitName] = getQuantity(unitName) + 1
}

function decreaseQuantity(unitName) {
  unitQuantities.value[unitName] = Math.max(1, getQuantity(unitName) - 1)
}

function setMaxQuantity(unit) {
  unitQuantities.value[unit.name] = Math.max(1, getMaxTrainable(unit))
}

function getMaxTrainable(unit) {
  const checks = [
    unit.cost?.water ? Math.floor(getCurrentResource('water') / unit.cost.water) : Infinity,
    unit.cost?.food ? Math.floor(getCurrentResource('food') / unit.cost.food) : Infinity,
    unit.cost?.scrap ? Math.floor(getCurrentResource('scrap') / unit.cost.scrap) : Infinity,
    unit.cost?.fuel ? Math.floor(getCurrentResource('fuel') / unit.cost.fuel) : Infinity,
    unit.cost?.energy ? Math.floor(getCurrentResource('energy') / unit.cost.energy) : Infinity,
    unit.cost?.rareTech ? Math.floor(getCurrentResource('rareTech') / unit.cost.rareTech) : Infinity,
    unit.capacityCost ? Math.floor(availablePopulation.value / unit.capacityCost) : Infinity
  ]

  const finite = checks.filter(v => Number.isFinite(v))
  return Math.max(0, finite.length ? Math.min(...finite) : 0)
}

async function fetchUnitInventory() {
  if (!props.settlement?.id) return

  try {
    const data = await getSettlement(props.settlement.id)
    unitInventory.value = data.unitInventory ?? data.UnitInventory ?? {}
  } catch (err) {
    console.error('Failed to fetch unit inventory', err)
    unitInventory.value = {}
  }
}

async function activateTrainingCommander() {
  if (!props.settlement?.id) return

  try {
    await activateCommanderApi(props.settlement.id, 14)
    showTrainingCommanderInfo.value = false

    // Fetch updated queue with new limit
    await fetchTrainingQueue()

    if (props.refreshSettlement) await props.refreshSettlement()
  } catch (err) {
    error.value = err?.response?.data || 'Commander activation failed.'
  }
}

async function trainSelectedUnit(unit) {
  if (!props.settlement?.id) return
  if (trainingQueueFull.value) return

  const quantity = getQuantity(unit.name)
  if (quantity < 1) return

  trainingBusy.value = true
  error.value = ''

  try {
    await trainUnit(props.settlement.id, unit.name, quantity)

    await Promise.all([
      fetchTrainingQueue(),
      fetchUnitInventory()
    ])

    if (props.refreshSettlement) {
      await props.refreshSettlement()
    }
  } catch (err) {
    error.value = err?.response?.data || 'Training failed.'
  } finally {
    trainingBusy.value = false
  }
}

async function loadUnitsData() {
  loading.value = true
  error.value = ''

  try {
    const settlementId = props.settlement.id

    const [buildingsData, unitsData] = await Promise.all([
      getBuildings(settlementId),
      getUnits()
    ])

    buildings.value = (buildingsData || []).map(normalizeBuilding)
    units.value = (unitsData || []).map(normalizeUnit)

    const firstUnlocked = facilities.value.find(f => !f.locked)
    if (firstUnlocked) facilityFilter.value = firstUnlocked.building

    await Promise.all([fetchUnitInventory(), fetchTrainingQueue()])
  } catch (err) {
    error.value = err?.response?.data || 'Unable to load tactical archives.'
    console.error(err)
  } finally {
    loading.value = false
  }

  tickInterval = setInterval(async () => {
    now.value = Date.now()

    try {
      // Only consider active items for completion; waiting items may have remainingSeconds === 0
      if (trainingQueue.value.some(q => q.isActive && (q.remainingSeconds || 0) <= 0)) {
        await completeReadyUnitTraining(props.settlement.id)
        await Promise.all([
          fetchTrainingQueue(),
          fetchUnitInventory()
        ])

        if (props.refreshSettlement) {
          await props.refreshSettlement()
        }
      } else if (trainingQueue.value.length > 0) {
        await fetchTrainingQueue()
      }
    } catch (err) {
      console.error('Training queue refresh failed', err)
    }
  }, 1000)
}

function capitalize(value) {
  return value ? value.charAt(0).toUpperCase() + value.slice(1) : value
}

onMounted(async () => {
  if (!props.settlement?.id) {
    loading.value = false
    error.value = 'Settlement data not yet available'

    const unwatchSettlement = watch(
        () => props.settlement?.id,
        async newId => {
          if (newId) {
            unwatchSettlement()
            await loadUnitsData()
          }
        }
    )
    return
  }

  await loadUnitsData()
})

onUnmounted(() => {
  if (tickInterval) clearInterval(tickInterval)
})

function getQueueUnitImage(unitName) {
  return getUnitImage(unitName)
}

function getQueuePct(item) {
  const remaining = getQueueRemaining(item) || 0

  // If we have a totalDurationSeconds or buildTimeSeconds use it when available
  if (item.totalDurationSeconds || item.buildTimeSeconds) {
    const total = item.totalDurationSeconds || item.buildTimeSeconds || 1
    return Math.min(100, Math.max(0, ((total - remaining) / Math.max(1, total)) * 100))
  }

  // If startedAtUtc is present, reconstruct total using startedAt + remaining
  if (item.startedAtUtc) {
    const completesAt = now.value + (remaining * 1000)
    const startedStr = item.startedAtUtc.endsWith('Z') ? item.startedAtUtc : `${item.startedAtUtc}Z`
    const startedAt = new Date(startedStr).getTime()
    const total = Math.max(1, Math.ceil((completesAt - startedAt) / 1000))
    return Math.min(100, Math.max(0, ((total - remaining) / total) * 100))
  }

  // Fallback: remember the initial remaining as the total for this item key so we can show progress
  const itemKey = `${item.unitName}_${item.id}`
  if (!queueTotals.value[itemKey]) {
    queueTotals.value[itemKey] = remaining || 1
  }
  const total = queueTotals.value[itemKey] || 1
  return Math.min(100, Math.max(0, ((total - remaining) / total) * 100))
}

function formatQueueTime(sec) {
  if (sec <= 0) return 'Done!'
  const h = Math.floor(sec / 3600)
  const m = Math.floor((sec % 3600) / 60)
  const s = sec % 60
  return `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`
}

function getQueueRemaining(item) {
  // Use completesAtUtc when provided (authoritative); otherwise fall back to remainingSeconds
  if (item && item.completesAtUtc) {
    const endStr = item.completesAtUtc.endsWith('Z') ? item.completesAtUtc : `${item.completesAtUtc}Z`
    const end = new Date(endStr).getTime()
    return Math.max(0, Math.ceil((end - now.value) / 1000))
  }
  return item?.remainingSeconds ?? 0
}

async function fetchTrainingQueue() {
  if (!props.settlement?.id) return

  try {
    const data = await getUnitTrainingQueue(props.settlement.id)
    trainingQueue.value = normalizeTrainingQueue(data)

    // If backend did not flag any item as active, ensure the first queued item is treated as active
    if (trainingQueue.value.length > 0 && !trainingQueue.value.some(q => q.isActive)) {
      trainingQueue.value[0].isActive = true
      trainingQueue.value[0].status = trainingQueue.value[0].status || 'active'
    }

    // Set queue limit from API (like buildings)
    trainingQueueLimit.value = data?.Limit ?? data?.limit ?? (trainingCommanderActive.value ? 7 : 2)

    // Clean up queueTotals for items no longer in queue
    const currentKeys = new Set(trainingQueue.value.map(item => `${item.unitName}_${item.id}`))
    Object.keys(queueTotals.value).forEach(key => {
      if (!currentKeys.has(key)) {
        delete queueTotals.value[key]
      }
    })
  } catch (err) {
    console.error('Failed to fetch training queue', err)
    trainingQueue.value = []
  }
}
</script>

<style scoped>
.unit-owned-bar {
  width: 100px;
  margin-top: 8px;
  padding: 6px 8px;
  border: 1px solid rgba(61, 255, 156, 0.28);
  background: linear-gradient(180deg, rgba(61,255,156,.10), rgba(61,255,156,.03));
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  box-sizing: border-box;
}

.unit-owned-label {
  font-size: 8px;
  letter-spacing: 1.5px;
  color: rgba(61,255,156,.72);
  font-family: var(--ff-title);
}

.unit-owned-value {
  font-family: var(--ff-title);
  font-size: 15px;
  color: #3dff9c;
  letter-spacing: 1px;
  font-weight: 700;
  text-shadow: 0 0 8px rgba(61,255,156,.18);
}

.queue-slots--full {
  color: #ff6040;
  border-color: rgba(255,96,64,.35);
  background: rgba(255,96,64,.08);
}

.units-page {
  min-height: 100%;
  color: var(--bright);
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 16px;
  margin-bottom: 18px;
  padding-bottom: 14px;
  border-bottom: 1px solid var(--border);
}

.page-title {
  margin: 0;
  font-family: var(--ff-title);
  font-size: 34px;
  letter-spacing: 4px;
  color: var(--cyan);
  text-shadow: 0 0 14px rgba(0,212,255,.2);
}

.page-subtitle {
  margin: 6px 0 0;
  font-size: 11px;
  letter-spacing: 2px;
  text-transform: uppercase;
  color: var(--muted);
}

.army-power-grid {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}

.power-stat {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 10px;
  border: 1px solid var(--border);
  background: rgba(255,255,255,.01);
}

.power-icon {
  width: 20px;
  height: 20px;
  object-fit: contain;
  filter: drop-shadow(0 0 4px rgba(255,255,255,.1));
}

.power-details {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.power-label {
  font-size: 8px;
  letter-spacing: 1.5px;
  color: var(--muted);
  font-family: var(--ff-title);
}

.power-values {
  display: flex;
  align-items: baseline;
  gap: 4px;
  font-family: var(--ff-title);
}

.power-attack {
  font-size: 14px;
  color: #ff9966;
  letter-spacing: .5px;
}

.power-separator {
  font-size: 10px;
  color: var(--muted);
}

.power-defense {
  font-size: 12px;
  color: #66b3ff;
  letter-spacing: .5px;
}

.queue-panel {
  background: linear-gradient(180deg,rgba(0,212,255,.03),rgba(0,212,255,.01));
  border: 1px solid var(--border-bright);
  margin-bottom: 18px;
  overflow: hidden;
}

.queue-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 14px;
  background: linear-gradient(90deg,rgba(0,212,255,.06),transparent);
  border-bottom: 1px solid var(--border);
}

.queue-title-row {
  display: flex;
  align-items: center;
  gap: 10px;
}

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

.queue-commander {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  border: 1px dashed var(--border-bright);
  cursor: pointer;
  transition: all .15s;
}

.queue-commander:hover {
  border-color: var(--cyan);
  background: rgba(0,212,255,.04);
}

.queue-commander--active {
  border-style: solid;
  border-color: #ffc830;
  background: rgba(255,200,48,.06);
  cursor: default;
}

.commander-icon {
  font-size: 10px;
}

.commander-text {
  font-size: 8px;
  color: var(--cyan-dim);
  letter-spacing: 1.5px;
  font-family: var(--ff-title);
}

.queue-commander--active .commander-text {
  color: #ffc830;
}

.commander-info {
  padding: 12px 14px;
  background: rgba(255,200,48,.03);
  border-bottom: 1px solid var(--border);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 14px;
}

.commander-info-text {
  font-size: 10px;
  color: var(--muted);
  line-height: 1.5;
}

.commander-info-text strong {
  color: #ffc830;
}

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

.commander-activate-btn:hover {
  background: rgba(255,200,48,.15);
}

.queue-empty {
  padding: 14px;
  text-align: center;
}

.queue-empty-text {
  font-size: 10px;
  color: var(--muted);
  font-family: var(--ff-title);
  letter-spacing: 1px;
}

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


.queue-items {
  padding: 8px 12px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.queue-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 10px;
  border-bottom: 1px solid var(--border);
}

.queue-item:last-child {
  border-bottom: none;
}

.queue-item-index {
  width: 24px;
  height: 24px;
  border: 1px solid var(--border-bright);
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: var(--ff-title);
  font-size: 10px;
  color: var(--cyan);
  flex-shrink: 0;
}

.queue-item-portrait {
  width: 52px;
  height: 52px;
  border: 1px solid var(--border);
  background: linear-gradient(180deg, rgba(0,212,255,.03), rgba(255,255,255,.01));
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.queue-item-portrait-img {
  width: 100%;
  height: 100%;
  object-fit: contain;
  display: block;
}

.queue-item-portrait-fallback {
  font-family: var(--ff-title);
  font-size: 18px;
  color: var(--cyan);
}

.queue-item-info {
  min-width: 180px;
}

.queue-item-name {
  font-size: 12px;
  color: var(--bright);
  font-weight: 700;
  letter-spacing: .6px;
}

.queue-item-meta {
  margin-top: 3px;
  font-size: 10px;
  color: var(--muted);
  font-family: var(--ff-title);
  letter-spacing: 1px;
}

.queue-item-timer {
  flex: 1;
  min-width: 100px;
  text-align: right;
}

.queue-item-time {
  font-family: var(--ff-title);
  font-size: 11px;
  color: var(--cyan);
  letter-spacing: 1px;
}

.queue-item-bar {
  height: 2px;
  background: var(--border);
  margin-top: 4px;
  border-radius: 1px;
  overflow: hidden;
}

.queue-item-bar-fill {
  height: 100%;
  background: linear-gradient(90deg, var(--cyan-dark), var(--cyan));
  border-radius: 1px;
  box-shadow: 0 0 4px rgba(0,212,255,.4);
  transition: width 1s linear;
}

.units-filters {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-bottom: 18px;
}

.filter-row {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.filter-btn {
  height: 34px;
  padding: 0 14px;
  border: 1px solid var(--border);
  background: rgba(255,255,255,.02);
  color: var(--muted);
  font-size: 11px;
  letter-spacing: 1px;
  cursor: pointer;
  transition: all .15s;
  display: flex;
  align-items: center;
  gap: 6px;
}

.filter-btn:hover:not(:disabled) {
  color: var(--bright);
  border-color: var(--border-bright);
  background: rgba(0,212,255,.04);
}

.filter-btn--active {
  color: var(--cyan);
  border-color: var(--border-bright);
  background: rgba(0,212,255,.08);
  box-shadow: inset 0 0 12px rgba(0,212,255,.04);
}

.filter-btn--locked {
  opacity: .45;
  cursor: not-allowed;
}

.filter-btn--sm {
  height: 30px;
  padding: 0 12px;
  font-size: 10px;
}

.filter-lvl {
  font-size: 10px;
  color: var(--bright);
  opacity: .8;
}

.lock-icon {
  font-size: 11px;
}

.units-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit,minmax(440px,1fr));
  gap: 14px;
}

.unit-card {
  display: grid;
  grid-template-columns: 120px minmax(0,1fr);
  gap: 16px;
  padding: 16px;
  border: 1px solid var(--border);
  background: linear-gradient(180deg,rgba(7,16,28,.95),rgba(5,12,20,.98));
  box-shadow: inset 0 1px 0 rgba(255,255,255,.02);
}

.unit-visual {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: flex-start;
  gap: 8px;
  position: relative;
}

.unit-visual-frame {
  width: 100px;
  height: 120px;
  border: 1px solid var(--border);
  background: linear-gradient(180deg,rgba(0,212,255,.03),rgba(255,255,255,.01));
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  overflow: hidden;
}

.unit-portrait {
  width: 100%;
  height: 100%;
  object-fit: contain;
  display: block;
}

.unit-visual-placeholder {
  width: 52px;
  height: 52px;
  border-radius: 50%;
  border: 1px solid var(--border-bright);
  display: flex;
  align-items: center;
  justify-content: center;
  font-family: var(--ff-title);
  font-size: 24px;
  color: var(--cyan);
}

.unit-visual-label {
  font-size: 9px;
  letter-spacing: 2px;
  color: var(--muted);
}

.unit-main {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.unit-card-top {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: flex-start;
}

.unit-name {
  font-family: var(--ff-title);
  font-size: 18px;
  letter-spacing: 2px;
  color: var(--bright);
}

.unit-meta-row {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 6px;
}

.unit-badge {
  padding: 4px 8px;
  border: 1px solid var(--border);
  font-size: 10px;
  letter-spacing: 1px;
  color: var(--muted);
  text-transform: uppercase;
}

.unit-badge--facility {
  color: var(--cyan);
}

.unit-badge--type {
  color: #ffd77a;
}

.unit-build-time {
  font-size: 11px;
  letter-spacing: 1px;
  color: #3dff9c;
  text-shadow: 0 0 8px rgba(61,255,156,.25);
  white-space: nowrap;
}

.unit-description {
  margin: 0;
  font-size: 13px;
  line-height: 1.5;
  color: #d7e5f2;
}

.unit-role {
  font-size: 11px;
  letter-spacing: 1px;
  color: var(--cyan);
}

.unit-stats-compact {
  display: grid;
  gap: 8px;
}

.unit-stats-compact--top {
  grid-template-columns: 1.45fr .85fr .95fr;
}

.stat-pill {
  border: 1px solid var(--border);
  padding: 10px 11px;
  background: rgba(255,255,255,.02);
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: 0;
}

.stat-pill span {
  font-size: 10px;
  letter-spacing: 1.2px;
  color: #8ea7bb;
}

.stat-pill strong {
  font-size: 18px;
  line-height: 1;
  color: #f4fbff;
  text-shadow: 0 0 10px rgba(255,255,255,.08);
}

.stat-pill--attack {
  justify-content: space-between;
  min-width: 0;
}

.stat-pill-top {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}

.stat-pill-icon {
  width: 18px;
  height: 18px;
  object-fit: contain;
  flex-shrink: 0;
  filter: drop-shadow(0 0 5px rgba(255,255,255,.12));
}

.stat-pill--attack .stat-pill-top {
  align-items: center;
}

.stat-pill--attack span {
  font-size: 9px;
  letter-spacing: .9px;
  color: #a8bfd0;
  white-space: nowrap;
  overflow: visible;
  text-overflow: clip;
  line-height: 1;
  display: block;
}

.defense-panel,
.unit-cost,
.training-panel {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.defense-title,
.cost-title,
.training-title {
  font-size: 10px;
  letter-spacing: 2px;
  color: var(--muted);
  text-transform: uppercase;
}

.cost-quantity {
  font-size: 9px;
  color: var(--cyan);
  margin-left: 6px;
}

.defense-grid {
  display: grid;
  grid-template-columns: repeat(3,minmax(0,1fr));
  gap: 8px;
}

.secondary-stat {
  border: 1px solid var(--border);
  padding: 10px 11px;
  background: rgba(255,255,255,.02);
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.secondary-label {
  font-size: 10px;
  letter-spacing: 1.2px;
  color: #8ea7bb;
}

.secondary-value {
  font-size: 18px;
  line-height: 1;
  color: #f4fbff;
  font-weight: 700;
  text-shadow: 0 0 10px rgba(255,255,255,.08);
}

.secondary-stat--defense {
  justify-content: space-between;
  min-height: 78px;
}

.secondary-top {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}

.defense-icon {
  width: 20px;
  height: 20px;
  object-fit: contain;
  flex-shrink: 0;
  filter: drop-shadow(0 0 6px rgba(255,255,255,.12));
}

.cost-grid {
  display: grid;
  grid-template-columns: repeat(4,minmax(0,1fr));
  gap: 8px;
}

.cost-item {
  border: 1px solid var(--border);
  background: rgba(255,255,255,.02);
  padding: 9px 8px;
  display: flex;
  align-items: center;
  gap: 6px;
  justify-content: center;
  font-size: 13px;
  color: #f0f7ff;
  min-width: 0;
  transition: all .15s;
}

.cost-item--insufficient {
  color: #ff6040;
  border-color: rgba(255,96,64,.4);
  background: rgba(255,96,64,.06);
}

.cost-item--insufficient .cost-icon {
  opacity: .6;
}

.cost-icon {
  opacity: .95;
  font-size: 14px;
  line-height: 1;
  flex-shrink: 0;
  transition: transform .18s ease,filter .18s ease;
}

.unit-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 10px;
}

.train-qty-box {
  display: flex;
  align-items: center;
  gap: 6px;
}

.qty-btn,
.qty-input,
.train-btn {
  height: 38px;
  border: 1px solid var(--border);
  background: rgba(0,212,255,.08);
  color: var(--cyan);
  font-family: var(--ff-title);
  transition: all .15s ease;
  box-sizing: border-box;
}

.qty-btn,
.train-btn {
  letter-spacing: 2px;
}

.qty-btn {
  min-width: 38px;
  padding: 0 10px;
  font-size: 11px;
  cursor: pointer;
}

.qty-btn:hover,
.train-btn:hover,
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

.train-btn {
  padding: 0 18px;
  font-size: 11px;
  cursor: pointer;
}

.train-btn--disabled {
  opacity: .4;
  cursor: not-allowed;
  color: #ff6040;
  border-color: rgba(255,96,64,.3);
  background: rgba(255,96,64,.04);
}

.train-btn--disabled:hover {
  box-shadow: none;
  background: rgba(255,96,64,.04);
}

.qty-input::-webkit-outer-spin-button,
.qty-input::-webkit-inner-spin-button {
  -webkit-appearance: none;
  margin: 0;
}

.stat-hoverable,
.cost-item--hover {
  position: relative;
  overflow: hidden;
  transition: transform .18s ease,border-color .18s ease,background .18s ease,box-shadow .18s ease;
}

.stat-hoverable::before,
.cost-item--hover::before {
  content: "";
  position: absolute;
  inset: 0;
  background: linear-gradient(135deg,rgba(0,212,255,.10),rgba(255,215,122,.05) 55%,transparent 100%);
  opacity: 0;
  transition: opacity .18s ease;
  pointer-events: none;
}

.stat-hoverable:hover,
.cost-item--hover:hover {
  transform: translateY(-2px);
  border-color: rgba(0,212,255,.45);
  background: linear-gradient(180deg,rgba(0,212,255,.08),rgba(255,255,255,.03));
  box-shadow: 0 0 0 1px rgba(0,212,255,.08),0 0 18px rgba(0,212,255,.10),inset 0 0 18px rgba(0,212,255,.04);
}

.stat-hoverable:hover::before,
.cost-item--hover:hover::before {
  opacity: 1;
}

.stat-icon-wrap {
  width: 28px;
  height: 28px;
  min-width: 28px;
  border: 1px solid rgba(255,255,255,.08);
  background: linear-gradient(180deg,rgba(255,215,122,.10),rgba(255,255,255,.02)),rgba(10,18,28,.95);
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: inset 0 0 8px rgba(255,255,255,.03),0 0 8px rgba(255,215,122,.06);
  transition: transform .18s ease,border-color .18s ease,box-shadow .18s ease,background .18s ease;
}

.stat-icon-wrap--attack {
  background: linear-gradient(180deg,rgba(255,215,122,.16),rgba(255,255,255,.02)),rgba(10,18,28,.95);
}

.stat-icon-wrap--defense {
  width: 30px;
  height: 30px;
  min-width: 30px;
}

.stat-hoverable:hover .stat-icon-wrap,
.cost-item--hover:hover .cost-icon {
  transform: scale(1.08);
}

.stat-hoverable:hover .stat-icon-wrap {
  border-color: rgba(255,215,122,.40);
  box-shadow: inset 0 0 10px rgba(255,255,255,.05),0 0 12px rgba(255,215,122,.18),0 0 22px rgba(0,212,255,.08);
}

.cost-item--hover:hover .cost-icon {
  filter: drop-shadow(0 0 8px rgba(255,215,122,.25));
}

.stat-hoverable:hover strong,
.stat-hoverable:hover .secondary-value,
.cost-item--hover:hover span:last-child {
  color: #ffffff;
  text-shadow: 0 0 10px rgba(255,255,255,.14),0 0 18px rgba(0,212,255,.10);
}

.stat-hoverable:hover span,
.stat-hoverable:hover .secondary-label {
  color: #d7ecfa;
}

.units-empty {
  min-height: 420px;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--border);
  background: linear-gradient(180deg,rgba(7,16,28,.92),rgba(4,10,18,.98));
  text-align: center;
  padding: 40px 20px;
}

.units-empty--small {
  min-height: 200px;
}

.units-empty--error .empty-core,
.units-empty--error .empty-title {
  color: #ff7b7b;
}

.empty-core {
  font-size: 34px;
  color: var(--cyan);
  margin-bottom: 8px;
}

.empty-title {
  font-family: var(--ff-title);
  font-size: 18px;
  letter-spacing: 3px;
  color: var(--bright);
}

.empty-subtitle {
  font-size: 12px;
  color: var(--muted);
  max-width: 420px;
  line-height: 1.6;
}

.empty-hint {
  margin-top: 18px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: min(100%,520px);
}

.hint-row {
  display: grid;
  grid-template-columns: 26px 150px 1fr;
  gap: 10px;
  align-items: center;
  border: 1px solid var(--border);
  padding: 10px 12px;
  background: rgba(255,255,255,.02);
  font-size: 12px;
  text-align: left;
}

.hint-name {
  color: var(--bright);
}

.hint-desc {
  color: var(--muted);
}

@media(max-width:1200px) {
  .units-grid {
    grid-template-columns: repeat(auto-fit,minmax(360px,1fr));
  }
  .unit-stats-compact--top {
    grid-template-columns: repeat(2,minmax(0,1fr));
  }
  .cost-grid {
    grid-template-columns: repeat(3,minmax(0,1fr));
  }
}

@media(max-width:700px) {
  .page-header {
    flex-direction: column;
    align-items: flex-start;
  }
  .units-grid {
    grid-template-columns: 1fr;
  }
  .unit-card {
    grid-template-columns: 1fr;
  }
  .unit-visual {
    justify-content: flex-start;
  }
  .defense-grid {
    grid-template-columns: repeat(2,minmax(0,1fr));
  }
  .cost-grid {
    grid-template-columns: repeat(2,minmax(0,1fr));
  }
  .unit-actions {
    flex-direction: column;
    align-items: stretch;
  }
  .train-qty-box {
    flex-wrap: wrap;
  }
  .train-btn {
    width: 100%;
  }
}
</style>