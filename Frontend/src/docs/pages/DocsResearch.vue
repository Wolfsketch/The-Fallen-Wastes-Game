<template>
  <div>
    <div class="breadcrumb">
      <a href="#">Docs</a> <span class="sep">›</span> <span>Research</span>
    </div>

    <h1>RESEARCH <span class="tag">TECH TREE</span></h1>
    <p class="page-lead">
      The Tech Lab unlocks a research queue where you spend RareTech and Research Points to
      permanently upgrade your settlement's capabilities. Research is organized into six branches.
    </p>

    <h2 id="how-it-works">How It Works</h2>
    <p>
      Each research project has a <strong class="hl">RareTech cost</strong>,
      a <strong class="hl">Research Point cost</strong>, a <strong class="hl">minimum TechLab level</strong>,
      and a <strong class="hl">base duration</strong>. Some projects also require prerequisite
      research or specific salvage items.
    </p>

    <table class="stat-table">
      <thead>
        <tr>
          <th>Field</th>
          <th>Description</th>
        </tr>
      </thead>
      <tbody>
        <tr><td class="val">RareTech Cost</td><td>Deducted immediately when research starts</td></tr>
        <tr><td class="val">Research Points</td><td>Generated passively by the TechLab; consumed on start</td></tr>
        <tr><td class="val">TechLab Level</td><td>Minimum TechLab upgrade level required</td></tr>
        <tr><td class="val">Base Duration</td><td>Time in minutes before the research completes</td></tr>
        <tr><td class="val">Prerequisites</td><td>Other research that must be completed first</td></tr>
        <tr><td class="val">Salvage Items</td><td>Rare: some techs require a specific salvage item in your vault</td></tr>
      </tbody>
    </table>

    <!-- ECONOMY -->
    <h2 id="economy-b">Economy Branch</h2>
    <div class="card-grid">
      <div class="data-card" v-for="r in byBranch('Economy')" :key="r.key">
        <div class="data-card-header">
          <div class="data-card-icon">📈</div>
          <div>
            <div class="data-card-title">{{ r.name.toUpperCase() }}</div>
            <div class="data-card-sub"><span class="tag-inline tag-economy">Economy</span></div>
          </div>
        </div>
        <div class="data-card-body">
          <p>{{ r.desc }}</p>
          <ResearchMeta :r="r" />
        </div>
      </div>
    </div>

    <!-- MILITARY -->
    <h2 id="military-b">Military Branch</h2>
    <div class="card-grid">
      <div class="data-card" v-for="r in byBranch('Military')" :key="r.key">
        <div class="data-card-header">
          <div class="data-card-icon">⚔️</div>
          <div>
            <div class="data-card-title">{{ r.name.toUpperCase() }}</div>
            <div class="data-card-sub"><span class="tag-inline tag-military">Military</span></div>
          </div>
        </div>
        <div class="data-card-body">
          <p>{{ r.desc }}</p>
          <ResearchMeta :r="r" />
        </div>
      </div>
    </div>

    <!-- DEFENSE -->
    <h2 id="defense-b">Defense Branch</h2>
    <div class="card-grid">
      <div class="data-card" v-for="r in byBranch('Defense')" :key="r.key">
        <div class="data-card-header">
          <div class="data-card-icon">🛡️</div>
          <div>
            <div class="data-card-title">{{ r.name.toUpperCase() }}</div>
            <div class="data-card-sub"><span class="tag-inline tag-defense">Defense</span></div>
          </div>
        </div>
        <div class="data-card-body">
          <p>{{ r.desc }}</p>
          <ResearchMeta :r="r" />
        </div>
      </div>
    </div>

    <!-- LOGISTICS -->
    <h2 id="logistics-b">Logistics Branch</h2>
    <div class="card-grid">
      <div class="data-card" v-for="r in byBranch('Logistics')" :key="r.key">
        <div class="data-card-header">
          <div class="data-card-icon">🚛</div>
          <div>
            <div class="data-card-title">{{ r.name.toUpperCase() }}</div>
            <div class="data-card-sub"><span class="tag-inline tag-logistics">Logistics</span></div>
          </div>
        </div>
        <div class="data-card-body">
          <p>{{ r.desc }}</p>
          <ResearchMeta :r="r" />
        </div>
      </div>
    </div>

    <!-- SALVAGE -->
    <h2 id="salvage-b">Salvage Branch</h2>
    <div class="card-grid">
      <div class="data-card" v-for="r in byBranch('Salvage')" :key="r.key">
        <div class="data-card-header">
          <div class="data-card-icon">🔍</div>
          <div>
            <div class="data-card-title">{{ r.name.toUpperCase() }}</div>
            <div class="data-card-sub"><span class="tag-inline tag-salvage">Salvage</span></div>
          </div>
        </div>
        <div class="data-card-body">
          <p>{{ r.desc }}</p>
          <ResearchMeta :r="r" />
        </div>
      </div>
    </div>

    <!-- EXPANSION -->
    <h2 id="expansion-b">Expansion Branch</h2>
    <div class="card-grid">
      <div class="data-card" v-for="r in byBranch('Expansion')" :key="r.key">
        <div class="data-card-header">
          <div class="data-card-icon">🌍</div>
          <div>
            <div class="data-card-title">{{ r.name.toUpperCase() }}</div>
            <div class="data-card-sub"><span class="tag-inline tag-expansion">Expansion</span></div>
          </div>
        </div>
        <div class="data-card-body">
          <p>{{ r.desc }}</p>
          <ResearchMeta :r="r" />
        </div>
      </div>
    </div>

    <div class="page-nav">
      <router-link to="/operations">
        <span class="nav-hint">← Previous</span>
        <span class="nav-label">Operations</span>
      </router-link>
      <router-link to="/salvage" style="text-align:right">
        <span class="nav-hint">Next →</span>
        <span class="nav-label">Salvage</span>
      </router-link>
    </div>
  </div>
</template>

<script setup>
import { defineComponent, h } from 'vue'
import { RESEARCH } from '../docs-data.js'

function byBranch(b) { return RESEARCH.filter(r => r.branch === b) }

// Small inline component for research metadata rows
const ResearchMeta = defineComponent({
  props: ['r'],
  setup(props) {
    return () => h('div', { style: 'font-size: 12px; color: var(--muted); margin-top: 10px; display: flex; flex-direction: column; gap: 3px;' }, [
      h('span', null, [`🔬 RareTech: `, h('strong', { style: 'color: var(--cyan)' }, props.r.rt)]),
      h('span', null, [`📊 Research Points: `, h('strong', { style: 'color: var(--bright)' }, props.r.rp)]),
      h('span', null, [`🏗️ TechLab required: `, h('strong', { style: 'color: var(--bright)' }, props.r.techLab)]),
      h('span', null, [`⏱ Duration: `, h('strong', { style: 'color: var(--amber)' }, props.r.time)]),
      props.r.requires?.length
        ? h('span', null, [`🔒 Requires: `, h('strong', { style: 'color: var(--text)' }, props.r.requires.join(', '))])
        : null,
      props.r.salvageItems?.length
        ? h('span', null, [`📦 Salvage item: `, h('strong', { style: 'color: var(--purple)' }, props.r.salvageItems.join(', '))])
        : null,
    ])
  }
})
</script>
