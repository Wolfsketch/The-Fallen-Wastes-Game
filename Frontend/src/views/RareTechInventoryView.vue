<template>
  <div class="inventory-page">
    <div class="page-header">
      <div>
        <h1 class="page-title">RARE TECH INVENTORY</h1>
        <p class="page-subtitle">Recovered pre-fall artifacts, schematics and salvage assets</p>
      </div>

      <div class="page-actions">
        <button class="refresh-btn" @click="reloadInventory" :disabled="loading">
          {{ loading ? 'SYNCING...' : 'REFRESH' }}
        </button>
      </div>
    </div>

    <div v-if="loading" class="inventory-empty">
      <div class="empty-core">◌</div>
      <div class="empty-title">LOADING SALVAGE INVENTORY</div>
      <div class="empty-subtitle">Scanning recovered wasteland technology caches...</div>
    </div>

    <div v-else-if="error" class="inventory-empty inventory-empty--error">
      <div class="empty-core">!</div>
      <div class="empty-title">INVENTORY LINK FAILURE</div>
      <div class="empty-subtitle">{{ error }}</div>
    </div>

    <template v-else>
      <div class="top-grid">
        <section class="panel summary-panel">
          <div class="panel-head">
            <div>
              <div class="panel-kicker">Settlement Resource</div>
              <h2 class="panel-title">RareTech Stock</h2>
            </div>
            <div class="panel-badge panel-badge--cyan">
              {{ currentRareTech.toLocaleString() }}
            </div>
          </div>

          <div class="summary-cards">
            <div class="summary-card">
              <div class="summary-label">Inventory Items</div>
              <div class="summary-value">{{ totalItems }}</div>
              <div class="summary-sub">Unique salvage entries</div>
            </div>

            <div class="summary-card">
              <div class="summary-label">Total Quantity</div>
              <div class="summary-value">{{ totalQuantity }}</div>
              <div class="summary-sub">All stored recovered pieces</div>
            </div>

            <div class="summary-card">
              <div class="summary-label">Salvage Yield</div>
              <div class="summary-value">{{ totalYield }}</div>
              <div class="summary-sub">Potential RareTech conversion</div>
            </div>

            <div class="summary-card">
              <div class="summary-label">Highest Tier</div>
              <div class="summary-value">{{ highestRarity }}</div>
              <div class="summary-sub">Best recovered tech classification</div>
            </div>
          </div>

          <div class="summary-note">
            This inventory is meant for POI rewards, event rewards and later integration with the Tech Salvager.
            High-value salvage can later be converted into RareTech or used as direct research requirements.
          </div>
        </section>

        <section class="panel category-panel">
          <div class="panel-head">
            <div>
              <div class="panel-kicker">Classification Matrix</div>
              <h2 class="panel-title">Salvage Categories</h2>
            </div>
          </div>

          <div class="category-list">
            <div class="category-row">
              <span class="category-label">Artifacts</span>
              <span class="category-value">{{ categoryCounts.artifact }}</span>
            </div>
            <div class="category-row">
              <span class="category-label">Schematics</span>
              <span class="category-value">{{ categoryCounts.schematic }}</span>
            </div>
            <div class="category-row">
              <span class="category-label">Data Cores</span>
              <span class="category-value">{{ categoryCounts.dataCore }}</span>
            </div>
            <div class="category-row">
              <span class="category-label">Components</span>
              <span class="category-value">{{ categoryCounts.component }}</span>
            </div>
            <div class="category-row">
              <span class="category-label">Fragments</span>
              <span class="category-value">{{ categoryCounts.fragment }}</span>
            </div>
            <div class="category-row">
              <span class="category-label">Unknown</span>
              <span class="category-value">{{ categoryCounts.unknown }}</span>
            </div>
          </div>
        </section>
      </div>

      <section class="panel filters-panel">
        <div class="filters-bar">
          <input
              v-model="searchTerm"
              class="search-input"
              type="text"
              placeholder="Search recovered tech..."
          />

          <select v-model="selectedCategory" class="filter-select">
            <option value="all">All Categories</option>
            <option value="artifact">Artifacts</option>
            <option value="schematic">Schematics</option>
            <option value="dataCore">Data Cores</option>
            <option value="component">Components</option>
            <option value="fragment">Fragments</option>
            <option value="unknown">Unknown</option>
          </select>

          <select v-model="selectedRarity" class="filter-select">
            <option value="all">All Rarities</option>
            <option value="common">Common</option>
            <option value="uncommon">Uncommon</option>
            <option value="rare">Rare</option>
            <option value="epic">Epic</option>
            <option value="legendary">Legendary</option>
          </select>
        </div>
      </section>

      <section class="panel inventory-panel">
        <div class="panel-head">
          <div>
            <div class="panel-kicker">Recovered Assets</div>
            <h2 class="panel-title">Inventory Registry</h2>
          </div>
          <div class="registry-count">{{ filteredItems.length }} MATCHES</div>
        </div>

        <div v-if="filteredItems.length === 0" class="registry-empty">
          <div class="registry-empty-title">NO RECOVERED TECH FOUND</div>
          <div class="registry-empty-sub">
            No inventory entries matched the current filters.
          </div>
        </div>

        <div v-else class="inventory-table">
          <div class="inventory-head">
            <span>Item</span>
            <span>Category</span>
            <span>Rarity</span>
            <span>Qty</span>
            <span>Yield</span>
          </div>

          <div
              v-for="item in filteredItems"
              :key="item.id"
              class="inventory-row"
          >
            <div class="item-main">
              <div class="item-icon" :class="rarityClass(item.rarity)">
                {{ item.icon || '◈' }}
              </div>
              <div>
                <div class="item-name">{{ item.name }}</div>
                <div class="item-desc">{{ item.description }}</div>
              </div>
            </div>

            <span class="cell-text">{{ displayCategory(item.category) }}</span>
            <span class="cell-text" :class="rarityTextClass(item.rarity)">
              {{ item.rarity.toUpperCase() }}
            </span>
            <span class="cell-text">{{ item.quantity }}</span>
            <span class="cell-text cell-text--cyan">{{ item.salvageYield * item.quantity }}</span>
          </div>
        </div>
      </section>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'

const props = defineProps({
  settlement: {
    type: Object,
    default: null
  },
  liveResources: {
    type: Object,
    default: () => ({})
  },
  refreshSettlement: {
    type: Function,
    default: null
  }
})

const loading = ref(true)
const error = ref('')
const searchTerm = ref('')
const selectedCategory = ref('all')
const selectedRarity = ref('all')

const inventoryItems = ref([])

const demoInventory = [
  {
    id: 'rt-001',
    name: 'Pre-Fall Circuit Array',
    description: 'Recovered electronics cluster from an abandoned relay node.',
    category: 'component',
    rarity: 'uncommon',
    quantity: 4,
    salvageYield: 3,
    icon: '◈'
  },
  {
    id: 'rt-002',
    name: 'Encrypted Data Core',
    description: 'Partial archive core containing corrupted military research logs.',
    category: 'dataCore',
    rarity: 'rare',
    quantity: 2,
    salvageYield: 8,
    icon: '⬢'
  },
  {
    id: 'rt-003',
    name: 'Prototype Weapon Schematic',
    description: 'Blueprint fragments for advanced wasteland combat hardware.',
    category: 'schematic',
    rarity: 'epic',
    quantity: 1,
    salvageYield: 18,
    icon: '⟁'
  },
  {
    id: 'rt-004',
    name: 'Reactor Fragment',
    description: 'Damaged micro-reactor shard with salvageable energy systems.',
    category: 'fragment',
    rarity: 'rare',
    quantity: 3,
    salvageYield: 6,
    icon: '⬡'
  },
  {
    id: 'rt-005',
    name: 'Vault Artifact',
    description: 'Intact pre-fall relic recovered from a sealed underground vault.',
    category: 'artifact',
    rarity: 'legendary',
    quantity: 1,
    salvageYield: 30,
    icon: '✦'
  },
  {
    id: 'rt-006',
    name: 'Broken Sensor Housing',
    description: 'Low-value scrap technology salvage from ruined POI remains.',
    category: 'component',
    rarity: 'common',
    quantity: 7,
    salvageYield: 1,
    icon: '△'
  }
]

const currentRareTech = computed(() => {
  return props.liveResources?.rareTech
      ?? props.settlement?.rareTech
      ?? 0
})

const filteredItems = computed(() => {
  return inventoryItems.value.filter(item => {
    const matchesSearch =
        !searchTerm.value ||
        item.name.toLowerCase().includes(searchTerm.value.toLowerCase()) ||
        item.description.toLowerCase().includes(searchTerm.value.toLowerCase())

    const matchesCategory =
        selectedCategory.value === 'all' || item.category === selectedCategory.value

    const matchesRarity =
        selectedRarity.value === 'all' || item.rarity === selectedRarity.value

    return matchesSearch && matchesCategory && matchesRarity
  })
})

const totalItems = computed(() => inventoryItems.value.length)

const totalQuantity = computed(() =>
    inventoryItems.value.reduce((sum, item) => sum + item.quantity, 0)
)

const totalYield = computed(() =>
    inventoryItems.value.reduce((sum, item) => sum + item.quantity * item.salvageYield, 0)
)

const highestRarity = computed(() => {
  const order = ['common', 'uncommon', 'rare', 'epic', 'legendary']
  let highestIndex = 0

  for (const item of inventoryItems.value) {
    const idx = order.indexOf(item.rarity)
    if (idx > highestIndex) highestIndex = idx
  }

  return order[highestIndex]?.toUpperCase() ?? 'NONE'
})

const categoryCounts = computed(() => {
  const base = {
    artifact: 0,
    schematic: 0,
    dataCore: 0,
    component: 0,
    fragment: 0,
    unknown: 0
  }

  for (const item of inventoryItems.value) {
    if (base[item.category] != null) {
      base[item.category]++
    } else {
      base.unknown++
    }
  }

  return base
})

onMounted(async () => {
  await loadInventory()
})

async function reloadInventory() {
  await loadInventory()
}

async function loadInventory() {
  loading.value = true
  error.value = ''

  try {
    if (typeof props.refreshSettlement === 'function') {
      await props.refreshSettlement()
    }

    // TODO:
    // Replace this demo inventory with a real backend call, for example:
    // const result = await getRareTechInventory(props.settlement.id)
    // inventoryItems.value = result.items ?? []

    inventoryItems.value = demoInventory
  } catch (err) {
    console.error(err)
    error.value = 'Unable to load rare tech inventory.'
  } finally {
    loading.value = false
  }
}

function displayCategory(category) {
  switch (category) {
    case 'artifact': return 'Artifact'
    case 'schematic': return 'Schematic'
    case 'dataCore': return 'Data Core'
    case 'component': return 'Component'
    case 'fragment': return 'Fragment'
    default: return 'Unknown'
  }
}

function rarityClass(rarity) {
  return {
    'rarity-common': rarity === 'common',
    'rarity-uncommon': rarity === 'uncommon',
    'rarity-rare': rarity === 'rare',
    'rarity-epic': rarity === 'epic',
    'rarity-legendary': rarity === 'legendary'
  }
}

function rarityTextClass(rarity) {
  return {
    'text-common': rarity === 'common',
    'text-uncommon': rarity === 'uncommon',
    'text-rare': rarity === 'rare',
    'text-epic': rarity === 'epic',
    'text-legendary': rarity === 'legendary'
  }
}
</script>

<style scoped>
.inventory-page {
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

.top-grid {
  display: grid;
  grid-template-columns: 1.3fr 0.9fr;
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

.panel-badge {
  padding: 7px 10px;
  font-size: 10px;
  letter-spacing: 1.6px;
  border: 1px solid var(--border-bright);
  font-family: var(--ff-title), sans-serif;
}

.panel-badge--cyan {
  color: var(--cyan);
  border-color: rgba(0, 212, 255, 0.35);
  background: rgba(0, 212, 255, 0.08);
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

.category-list {
  margin-top: 18px;
  display: flex;
  flex-direction: column;
  gap: 10px;
  position: relative;
  z-index: 1;
}

.category-row {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  padding: 11px 12px;
  border: 1px solid rgba(20, 32, 48, 0.9);
  background: rgba(14, 22, 32, 0.72);
}

.category-label {
  color: var(--muted);
  text-transform: uppercase;
  letter-spacing: 1px;
  font-size: 11px;
}

.category-value {
  color: var(--bright);
  font-family: var(--ff-title), sans-serif;
}

.filters-bar {
  display: grid;
  grid-template-columns: 1.4fr 1fr 1fr;
  gap: 12px;
  position: relative;
  z-index: 1;
}

.search-input,
.filter-select {
  background: rgba(14, 22, 32, 0.88);
  border: 1px solid var(--border);
  color: var(--bright);
  padding: 12px 12px;
  font-size: 12px;
  outline: none;
}

.search-input:focus,
.filter-select:focus {
  border-color: var(--cyan);
  box-shadow: 0 0 0 1px rgba(0, 212, 255, 0.2);
}

.registry-count {
  color: var(--cyan);
  font-family: var(--ff-title), sans-serif;
  font-size: 11px;
  letter-spacing: 2px;
}

.inventory-table {
  margin-top: 18px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  position: relative;
  z-index: 1;
}

.inventory-head,
.inventory-row {
  display: grid;
  grid-template-columns: 2.5fr 1fr 1fr 80px 90px;
  gap: 12px;
  align-items: center;
}

.inventory-head {
  color: var(--cyan-dim);
  font-size: 10px;
  text-transform: uppercase;
  letter-spacing: 2px;
  padding: 0 12px 8px;
  border-bottom: 1px solid var(--border);
}

.inventory-row {
  padding: 12px;
  border: 1px solid rgba(20, 32, 48, 0.9);
  background: rgba(14, 22, 32, 0.72);
}

.item-main {
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;
}

.item-icon {
  width: 42px;
  height: 42px;
  border: 1px solid var(--border-bright);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  flex-shrink: 0;
  background: rgba(6, 10, 16, 0.9);
}

.item-name {
  color: var(--bright);
  font-weight: 700;
  font-size: 13px;
}

.item-desc {
  margin-top: 4px;
  color: var(--muted);
  font-size: 11px;
  line-height: 1.4;
}

.cell-text {
  color: var(--text);
  font-size: 12px;
}

.cell-text--cyan {
  color: var(--cyan);
  font-family: var(--ff-title), sans-serif;
}

.rarity-common,
.text-common {
  color: #94a3b8;
}

.rarity-uncommon,
.text-uncommon {
  color: #4ade80;
}

.rarity-rare,
.text-rare {
  color: #38bdf8;
}

.rarity-epic,
.text-epic {
  color: #c084fc;
}

.rarity-legendary,
.text-legendary {
  color: #f59e0b;
}

.inventory-empty {
  min-height: 360px;
  border: 1px solid var(--border);
  background: linear-gradient(180deg, rgba(10, 16, 24, 0.96), rgba(6, 10, 16, 0.96));
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
}

.inventory-empty--error {
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

.registry-empty {
  padding: 24px 10px 10px;
  text-align: center;
  position: relative;
  z-index: 1;
}

.registry-empty-title {
  color: var(--bright);
  font-family: var(--ff-title), sans-serif;
  letter-spacing: 2px;
  font-size: 13px;
}

.registry-empty-sub {
  margin-top: 8px;
  color: var(--muted);
  font-size: 12px;
}

@media (max-width: 1100px) {
  .top-grid {
    grid-template-columns: 1fr;
  }

  .filters-bar {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 820px) {
  .summary-cards {
    grid-template-columns: 1fr;
  }

  .inventory-head,
  .inventory-row {
    grid-template-columns: 1.8fr 1fr 1fr 70px 80px;
    font-size: 11px;
  }
}

@media (max-width: 720px) {
  .page-header {
    flex-direction: column;
    align-items: stretch;
  }
}
</style>