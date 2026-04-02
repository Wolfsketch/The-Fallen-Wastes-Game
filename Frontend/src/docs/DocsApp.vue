<template>
  <header class="topbar">
    <div class="topbar-brand">
      <div class="radiation">☢</div>
      THE FALLEN WASTES
    </div>

    <div class="topbar-search">
      <span>⌕</span>&nbsp;Search docs...
      <kbd>Ctrl K</kbd>
    </div>

    <nav class="topbar-nav">
      <router-link to="/introduction">Docs</router-link>
      <router-link to="/units">Units</router-link>
      <router-link to="/buildings">Buildings</router-link>
      <router-link to="/research">Research</router-link>
    </nav>

    <div class="topbar-version">v0.7 ALPHA</div>
  </header>

  <div class="layout">
    <!-- LEFT SIDEBAR -->
    <aside class="sidebar">
      <div class="sidebar-group">
        <div class="sidebar-label">Getting Started</div>
        <router-link class="sidebar-link" to="/introduction">Introduction</router-link>
        <router-link class="sidebar-link" to="/resources">Resources</router-link>
        <router-link class="sidebar-link" to="/population">Population</router-link>
      </div>

      <div class="sidebar-group">
        <div class="sidebar-label">Settlement</div>
        <router-link class="sidebar-link" to="/buildings">Buildings</router-link>
      </div>

      <div class="sidebar-group">
        <div class="sidebar-label">Military</div>
        <router-link class="sidebar-link" to="/units">Units</router-link>
        <router-link class="sidebar-link" to="/combat">Combat System</router-link>
        <router-link class="sidebar-link" to="/operations">Operations</router-link>
      </div>

      <div class="sidebar-group">
        <div class="sidebar-label">Research</div>
        <router-link class="sidebar-link" to="/research">Tech Tree</router-link>
      </div>

      <div class="sidebar-group">
        <div class="sidebar-label">Salvage</div>
        <router-link class="sidebar-link" to="/salvage">Salvage Items</router-link>
      </div>

      <div class="sidebar-group">
        <div class="sidebar-label">World Map</div>
        <router-link class="sidebar-link" to="/wasteland">Wasteland &amp; POI</router-link>
      </div>

      <div class="sidebar-group">
        <div class="sidebar-label">Social</div>
        <router-link class="sidebar-link" to="/alliances">Alliances</router-link>
      </div>
    </aside>

    <!-- MAIN CONTENT (router-view) -->
    <main class="main">
      <router-view />
    </main>

    <!-- RIGHT TOC -->
    <aside class="toc">
      <div class="toc-title">On This Page</div>
      <a
        v-for="item in currentToc"
        :key="item.id"
        :href="'#' + item.id"
        :class="{ 'toc-active': activeSection === item.id }"
      >{{ item.label }}</a>
    </aside>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()

const tocMap = {
  '/introduction':  [
    { id: 'what-is',    label: 'What is The Fallen Wastes' },
    { id: 'game-loop',  label: 'Core Game Loop' },
    { id: 'new-player', label: 'New Player Guide' },
    { id: 'tips',       label: 'Beginner Tips' },
  ],
  '/buildings': [
    { id: 'core',       label: 'Core Buildings' },
    { id: 'production', label: 'Resource Production' },
    { id: 'storage',    label: 'Storage' },
    { id: 'military',   label: 'Military' },
    { id: 'defense',    label: 'Defense' },
    { id: 'research',   label: 'Research & Special' },
    { id: 'formulas',   label: 'Cost Formulas' },
  ],
  '/resources': [
    { id: 'types',       label: 'Resource Types' },
    { id: 'production2', label: 'Production Formula' },
    { id: 'storage2',    label: 'Storage Caps' },
    { id: 'raretech',    label: 'RareTech Economy' },
  ],
  '/population': [
    { id: 'shelter',  label: 'Shelter Cap' },
    { id: 'usage',    label: 'Population Usage' },
    { id: 'strategy', label: 'Strategic Tradeoffs' },
  ],
  '/units': [
    { id: 'barracks',       label: 'Barracks Units' },
    { id: 'garage',         label: 'Garage Units' },
    { id: 'workshop',       label: 'Workshop Units' },
    { id: 'command-center', label: 'Command Center' },
    { id: 'training-speed', label: 'Training Speed' },
  ],
  '/combat': [
    { id: 'damage-types', label: 'Damage Types' },
    { id: 'defense-vals', label: 'Defense Values' },
    { id: 'formula',      label: 'Combat Formula' },
    { id: 'tips-combat',  label: 'Combat Tips' },
  ],
  '/operations': [
    { id: 'raid',   label: 'Raid POI' },
    { id: 'scout',  label: 'Scout' },
    { id: 'siege',  label: 'Siege' },
    { id: 'convoy', label: 'Convoy' },
    { id: 'recall', label: 'Recall' },
  ],
  '/research': [
    { id: 'how-it-works', label: 'How It Works' },
    { id: 'economy-b',    label: 'Economy Branch' },
    { id: 'military-b',   label: 'Military Branch' },
    { id: 'defense-b',    label: 'Defense Branch' },
    { id: 'logistics-b',  label: 'Logistics Branch' },
    { id: 'salvage-b',    label: 'Salvage Branch' },
    { id: 'expansion-b',  label: 'Expansion Branch' },
  ],
  '/salvage': [
    { id: 'items',       label: 'Salvage Items' },
    { id: 'salvager',    label: 'Tech Salvager' },
    { id: 'raretech-loop', label: 'RareTech Loop' },
  ],
  '/wasteland': [
    { id: 'overview',  label: 'Map Overview' },
    { id: 'poi',       label: 'Points of Interest' },
    { id: 'cleared',   label: 'Cleared & Respawn' },
    { id: 'loot',      label: 'Loot System' },
  ],
  '/alliances': [
    { id: 'create',    label: 'Creating an Alliance' },
    { id: 'ranks',     label: 'Member Ranks' },
    { id: 'diplomacy', label: 'Diplomacy States' },
    { id: 'forum',     label: 'Alliance Forum' },
  ],
}

const currentToc = computed(() => tocMap[route.path] ?? [])
const activeSection = ref('')

let observer = null

function setupObserver() {
  if (observer) observer.disconnect()
  observer = new IntersectionObserver(
    (entries) => {
      for (const entry of entries) {
        if (entry.isIntersecting) {
          activeSection.value = entry.target.id
          break
        }
      }
    },
    { rootMargin: '-60px 0px -60% 0px', threshold: 0 }
  )
  setTimeout(() => {
    document.querySelectorAll('.main h2[id], .main h3[id]').forEach(el => observer.observe(el))
  }, 100)
}

watch(() => route.path, () => {
  activeSection.value = ''
  setupObserver()
})

onMounted(setupObserver)
onUnmounted(() => observer?.disconnect())
</script>
