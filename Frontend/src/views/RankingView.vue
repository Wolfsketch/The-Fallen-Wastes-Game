<template>
  <section class="ranking-screen">
    <header class="ranking-header">
      <div>
        <h1 class="ranking-title">RANKING</h1>
        <p class="ranking-subtitle">CLASSIFIED PLAYER STANDINGS</p>
      </div>
      <div class="ranking-meta">{{ ranking.length }} OPERATORS</div>
    </header>

    <div class="ranking-layout">
      <div class="ranking-panel">
        <div class="panel-head">
          <span class="panel-dot" />
          <span class="panel-title">GLOBAL STANDINGS</span>
        </div>

        <div class="ranking-filters">
          <button
              v-for="type in rankingTypes"
              :key="type.value"
              type="button"
              class="filter-btn"
              :class="{ 'filter-btn--active': selectedType === type.value }"
              @click="changeRankingType(type.value)"
          >
            {{ type.label }}
          </button>
        </div>

        <div v-if="loading" class="panel-empty">
          SYNCING RANKING DATA...
        </div>

        <div v-else-if="ranking.length === 0" class="panel-empty">
          NO PLAYER SIGNALS DETECTED
        </div>

        <div v-else class="ranking-table">
          <div class="ranking-row ranking-row--head">
            <div>Rank</div>
            <div>Trend</div>
            <div>Operator</div>
            <div>Score</div>
          </div>

          <button
              v-for="entry in ranking"
              :key="entry.id"
              type="button"
              class="ranking-row ranking-row--button"
              :class="{
              'ranking-row--self': props.player && entry.id === props.player.id,
              'ranking-row--top': entry.rank === 1,
              'ranking-row--selected': selectedPlayer && entry.id === selectedPlayer.id
            }"
              @click="selectPlayer(entry.id)"
          >
            <div>
              <span
                  class="rank-chip"
                  :class="entry.rank === 1 ? 'rank-chip--gold' : 'rank-chip--cyan'"
              >
                #{{ entry.rank }}
              </span>
            </div>

            <div class="trend-cell">
              <span
                  v-if="entry.rankChange > 0"
                  class="trend trend--up"
              >
                ▲ {{ entry.rankChange }}
              </span>

              <span
                  v-else-if="entry.rankChange < 0"
                  class="trend trend--down"
              >
                ▼ {{ Math.abs(entry.rankChange) }}
              </span>

              <span v-else class="trend trend--neutral">—</span>
            </div>

            <div class="operator-cell">
              <div class="operator-name">{{ entry.username }}</div>
              <div v-if="props.player && entry.id === props.player.id" class="operator-tag">
                YOU
              </div>
            </div>

            <div class="score-cell">{{ entry.score }}</div>
          </button>
        </div>
      </div>

      <div class="side-panel">
        <div class="panel-head">
          <span class="panel-dot" />
          <span class="panel-title">PLAYER INTEL</span>
        </div>

        <div v-if="profileLoading" class="panel-empty">
          LOADING OPERATOR PROFILE...
        </div>

        <div v-else-if="selectedProfile" class="profile-card">
          <div class="profile-rank">{{ selectedRankText }}</div>
          <div class="profile-name">{{ selectedProfile.username }}</div>

          <div class="profile-stats">
            <div class="intel-row">
              <span>Total Score</span>
              <strong>{{ selectedProfile.score }}</strong>
            </div>
            <div class="intel-row">
              <span>Attack Score</span>
              <strong>{{ selectedProfile.attackScore }}</strong>
            </div>
            <div class="intel-row">
              <span>Defense Score</span>
              <strong>{{ selectedProfile.defenseScore }}</strong>
            </div>
            <div class="intel-row">
              <span>War Score</span>
              <strong>{{ selectedProfile.warScore }}</strong>
            </div>
            <div class="intel-row">
              <span>Settlements</span>
              <strong>{{ selectedProfile.settlementCount }}</strong>
            </div>
          </div>

          <div class="panel-head panel-head--spaced">
            <span class="panel-dot" />
            <span class="panel-title">SETTLEMENTS</span>
          </div>

          <div v-if="selectedProfile.settlements?.length" class="settlement-list">
            <div
                v-for="settlement in selectedProfile.settlements"
                :key="settlement.id"
                class="settlement-item"
            >
              {{ settlement.name }}
            </div>
          </div>

          <div v-else class="panel-empty panel-empty--compact">
            NO KNOWN SETTLEMENTS
          </div>
        </div>

        <div v-else class="panel-empty">
          SELECT AN OPERATOR TO VIEW PUBLIC PROFILE
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { getPlayerRanking, getPlayerPublicProfile } from '../services/api.js'

const props = defineProps({
  player: {
    type: Object,
    default: null
  }
})

const rankingTypes = [
  { value: 'total', label: 'TOTAL' },
  { value: 'attack', label: 'ATTACK' },
  { value: 'defense', label: 'DEFENSE' },
  { value: 'war', label: 'WAR' }
]

const selectedType = ref('total')
const ranking = ref([])
const loading = ref(true)
const profileLoading = ref(false)
const selectedPlayer = ref(null)
const selectedProfile = ref(null)

const selectedRankText = computed(() => {
  if (!selectedPlayer.value) return '—'
  return `#${selectedPlayer.value.rank}`
})

async function loadRanking() {
  try {
    loading.value = true
    ranking.value = await getPlayerRanking(selectedType.value)

    if (ranking.value.length > 0) {
      let targetId = selectedPlayer.value?.id

      if (!targetId || !ranking.value.some(x => x.id === targetId)) {
        const selfEntry = props.player
            ? ranking.value.find(x => x.id === props.player.id)
            : null

        targetId = selfEntry?.id ?? ranking.value[0].id
      }

      await selectPlayer(targetId)
    } else {
      selectedPlayer.value = null
      selectedProfile.value = null
    }
  } catch (error) {
    console.error('Failed to load ranking:', error)
  } finally {
    loading.value = false
  }
}

async function changeRankingType(type) {
  if (selectedType.value === type) return
  selectedType.value = type
  await loadRanking()
}

async function selectPlayer(playerId) {
  try {
    profileLoading.value = true
    selectedPlayer.value = ranking.value.find(x => x.id === playerId) ?? null
    selectedProfile.value = await getPlayerPublicProfile(playerId)
  } catch (error) {
    console.error('Failed to load public profile:', error)
  } finally {
    profileLoading.value = false
  }
}

onMounted(async () => {
  await loadRanking()
})
</script>

<style scoped>
.ranking-screen {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.ranking-header {
  display: flex;
  justify-content: space-between;
  align-items: end;
  gap: 16px;
  border-bottom: 1px solid var(--border);
  padding-bottom: 8px;
}

.ranking-title {
  margin: 0;
  font-family: var(--ff-title);
  font-size: 22px;
  color: var(--cyan);
  letter-spacing: 2px;
  text-shadow: 0 0 12px rgba(0, 212, 255, 0.2);
}

.ranking-subtitle {
  margin: 4px 0 0;
  font-size: 10px;
  color: var(--cyan-dim);
  letter-spacing: 2px;
  text-transform: uppercase;
}

.ranking-meta {
  padding: 6px 10px;
  border: 1px solid var(--border-bright);
  background: rgba(0, 212, 255, 0.05);
  color: var(--cyan);
  font-family: var(--ff-title);
  font-size: 10px;
  letter-spacing: 2px;
}

.ranking-layout {
  display: grid;
  grid-template-columns: 1.7fr 0.8fr;
  gap: 16px;
}

.ranking-panel,
.side-panel {
  background: linear-gradient(180deg, rgba(10, 20, 32, 0.92), rgba(6, 12, 20, 0.94));
  border: 1px solid var(--border);
  position: relative;
  overflow: hidden;
}

.ranking-panel::before,
.side-panel::before {
  content: "";
  position: absolute;
  inset: 0;
  background:
      linear-gradient(rgba(0, 212, 255, 0.03) 1px, transparent 1px),
      linear-gradient(90deg, rgba(0, 212, 255, 0.03) 1px, transparent 1px);
  background-size: 40px 40px;
  opacity: 0.18;
  pointer-events: none;
}

.panel-head {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 16px;
  border-bottom: 1px solid rgba(0, 212, 255, 0.08);
  position: relative;
  z-index: 1;
}

.panel-head--spaced {
  margin-top: 12px;
}

.panel-dot {
  width: 8px;
  height: 8px;
  background: var(--cyan);
  box-shadow: 0 0 8px rgba(0, 212, 255, 0.8);
}

.panel-title {
  font-family: var(--ff-title);
  font-size: 12px;
  letter-spacing: 2px;
  color: var(--cyan);
}

.ranking-filters {
  display: flex;
  gap: 8px;
  padding: 12px 16px;
  border-bottom: 1px solid rgba(0, 212, 255, 0.06);
  position: relative;
  z-index: 1;
  flex-wrap: wrap;
}

.filter-btn {
  min-width: 86px;
  padding: 8px 12px;
  border: 1px solid rgba(0, 212, 255, 0.18);
  background: rgba(0, 212, 255, 0.03);
  color: var(--muted);
  font-family: var(--ff-title);
  font-size: 10px;
  letter-spacing: 1.5px;
  cursor: pointer;
  transition: all 0.15s ease;
}

.filter-btn:hover {
  color: var(--bright);
  border-color: rgba(0, 212, 255, 0.35);
  background: rgba(0, 212, 255, 0.06);
}

.filter-btn--active {
  color: var(--cyan);
  border-color: var(--cyan);
  background: rgba(0, 212, 255, 0.08);
  box-shadow: 0 0 8px rgba(0, 212, 255, 0.15);
}

.panel-empty {
  padding: 24px 16px;
  color: var(--muted);
  font-family: var(--ff-title);
  font-size: 11px;
  letter-spacing: 2px;
  position: relative;
  z-index: 1;
}

.panel-empty--compact {
  padding-top: 12px;
}

.ranking-table {
  position: relative;
  z-index: 1;
}

.ranking-row {
  display: grid;
  grid-template-columns: 90px 90px 1.4fr 120px;
  gap: 12px;
  align-items: center;
  padding: 12px 16px;
  border-bottom: 1px solid rgba(0, 212, 255, 0.05);
}

.ranking-row--head {
  color: var(--cyan-dim);
  font-size: 10px;
  text-transform: uppercase;
  letter-spacing: 2px;
  font-weight: 700;
  background: rgba(0, 212, 255, 0.03);
}

.ranking-row--button {
  width: 100%;
  background: transparent;
  border: 0;
  text-align: left;
  cursor: pointer;
  color: inherit;
}

.ranking-row--button:hover {
  background: rgba(0, 212, 255, 0.04);
}

.ranking-row--self {
  box-shadow: inset 2px 0 0 var(--cyan);
}

.ranking-row--top {
  background: linear-gradient(90deg, rgba(255, 200, 60, 0.06), transparent);
}

.ranking-row--selected {
  background: linear-gradient(90deg, rgba(0, 212, 255, 0.10), transparent);
}

.rank-chip {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 52px;
  padding: 4px 8px;
  border: 1px solid;
  font-family: var(--ff-title);
  font-size: 11px;
  letter-spacing: 1px;
}

.rank-chip--gold {
  color: #ffc83c;
  border-color: rgba(255, 200, 60, 0.35);
  background: rgba(255, 200, 60, 0.08);
}

.rank-chip--cyan {
  color: var(--cyan);
  border-color: rgba(0, 212, 255, 0.25);
  background: rgba(0, 212, 255, 0.05);
}

.trend-cell {
  display: flex;
  align-items: center;
}

.trend {
  font-family: var(--ff-title);
  font-size: 11px;
  letter-spacing: 1px;
}

.trend--up {
  color: #30ff80;
}

.trend--down {
  color: #ff7060;
}

.trend--neutral {
  color: var(--muted);
}

.operator-cell {
  display: flex;
  flex-direction: column;
  gap: 3px;
}

.operator-name {
  color: var(--bright);
  font-size: 14px;
  font-weight: 700;
}

.operator-tag {
  display: inline-block;
  width: fit-content;
  padding: 2px 6px;
  font-size: 9px;
  letter-spacing: 1px;
  color: var(--bg);
  background: var(--cyan);
  font-weight: 700;
}

.score-cell,
.profile-rank {
  color: var(--cyan);
  font-family: var(--ff-title);
}

.score-cell {
  font-size: 16px;
  letter-spacing: 1px;
}

.profile-card {
  position: relative;
  z-index: 1;
  padding: 16px;
}

.profile-rank {
  font-size: 28px;
  letter-spacing: 2px;
  color: #ffc83c;
}

.profile-name {
  margin-top: 6px;
  color: var(--bright);
  font-size: 20px;
  font-weight: 700;
}

.profile-stats {
  margin-top: 14px;
}

.intel-row {
  display: flex;
  justify-content: space-between;
  padding: 10px 0;
  border-bottom: 1px solid rgba(0, 212, 255, 0.05);
  color: var(--muted);
  font-size: 12px;
}

.intel-row strong {
  color: var(--bright);
  font-family: var(--ff-title);
  font-size: 12px;
  letter-spacing: 1px;
}

.settlement-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-top: 12px;
}

.settlement-item {
  padding: 10px 12px;
  border: 1px solid rgba(0, 212, 255, 0.12);
  background: rgba(0, 212, 255, 0.04);
  color: var(--bright);
  font-size: 13px;
}

@media (max-width: 1100px) {
  .ranking-layout {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 760px) {
  .ranking-row,
  .ranking-row--head {
    grid-template-columns: 80px 80px 1fr;
  }

  .ranking-row > :nth-child(4),
  .ranking-row--head > :nth-child(4) {
    display: none;
  }
}
</style>