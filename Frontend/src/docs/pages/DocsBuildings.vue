<template>
  <div>
    <div class="breadcrumb">
      <a href="#">Docs</a> <span class="sep">›</span> <span>Buildings</span>
    </div>

    <h1>BUILDINGS <span class="tag">SETTLEMENT</span></h1>
    <p class="page-lead">
      Buildings are the foundation of your settlement. They produce resources, store materials,
      train armies, research technologies, and protect your base from attack.
    </p>

    <div class="info-box">
      <div class="info-label">☢ MAX LEVEL</div>
      All buildings cap at <strong class="hl">level 30</strong>. Cost and build time scale
      with level using the formulas shown at the bottom of this page.
    </div>

    <!-- CORE -->
    <h2 id="core">Core Buildings</h2>
    <p>Core buildings are available from the start and govern settlement-wide mechanics.</p>
    <div class="card-grid">
      <div class="data-card" v-for="b in byCategory('Core')" :key="b.name">
        <div class="data-card-header">
          <div class="data-card-icon"><img v-if="buildingImg(b.name)" :src="buildingImg(b.name)" :alt="b.name"><span v-else>{{ b.icon }}</span></div>
          <div>
            <div class="data-card-title">{{ b.name.toUpperCase() }}</div>
            <div class="data-card-sub">{{ b.category }}</div>
          </div>
        </div>
        <div class="data-card-body">
          <p>{{ b.desc }}</p>
          <ul style="padding-left: 16px; margin: 0; font-size: 13px;">
            <li v-for="e in b.effects" :key="e" style="color: var(--text); margin-bottom: 2px;">{{ e }}</li>
          </ul>
          <div v-if="b.prereqs" style="margin-top: 10px; font-size: 12px; color: var(--muted);">
            <span style="color: var(--cyan); font-family: var(--ff-title); font-size: 9px; letter-spacing: 1px;">REQUIRES:</span>
            {{ b.prereqs }}
          </div>
        </div>
      </div>
    </div>

    <!-- PRODUCTION -->
    <h2 id="production">Resource Production</h2>
    <p>
      Five buildings each produce one of the five base resources. All start at level 0 with
      zero production. Each one feeds into a corresponding storage building.
    </p>
    <table class="stat-table">
      <thead>
        <tr>
          <th>Building</th>
          <th>Resource</th>
          <th>Production @ L1</th>
          <th>Production @ L10</th>
          <th>Pop / Level</th>
        </tr>
      </thead>
      <tbody>
        <tr><td class="val">Farm Dome</td><td>🥫 Food</td><td>~23/h</td><td>~630/h</td><td>2</td></tr>
        <tr><td class="val">Water Purifier</td><td>💧 Water</td><td>~23/h</td><td>~630/h</td><td>2</td></tr>
        <tr><td class="val">Scrap Forge</td><td>⚙️ Scrap</td><td>~23/h</td><td>~630/h</td><td>2</td></tr>
        <tr><td class="val">Fuel Refinery</td><td>⛽ Fuel</td><td>~23/h</td><td>~630/h</td><td>2</td></tr>
        <tr><td class="val">Solar Array</td><td>⚡ Energy</td><td>~23/h</td><td>~630/h</td><td>2</td></tr>
      </tbody>
    </table>

    <div class="code-block">
      <span class="comment">// Hourly production formula</span><br>
      <span class="key">Production</span> = <span class="val">23</span> × level × <span class="val">1.12</span>^(level − 1)
    </div>

    <div class="card-grid" style="margin-top: 8px">
      <div class="data-card" v-for="b in byCategory('Production')" :key="b.name">
        <div class="data-card-header">
          <div class="data-card-icon"><img v-if="buildingImg(b.name)" :src="buildingImg(b.name)" :alt="b.name"><span v-else>{{ b.icon }}</span></div>
          <div>
            <div class="data-card-title">{{ b.name.toUpperCase() }}</div>
            <div class="data-card-sub">{{ b.category }}</div>
          </div>
        </div>
        <div class="data-card-body">
          <p>{{ b.desc }}</p>
          <ul style="padding-left: 16px; margin: 0; font-size: 13px;">
            <li v-for="e in b.effects" :key="e" style="color: var(--text); margin-bottom: 2px;">{{ e }}</li>
          </ul>
        </div>
      </div>
    </div>

    <!-- STORAGE -->
    <h2 id="storage">Storage Buildings</h2>
    <p>
      Six storage buildings hold your resources. All start at level 1 with base 1000 capacity
      (200 for RareTech). Upgrade them to avoid production overflow.
    </p>
    <table class="stat-table">
      <thead>
        <tr>
          <th>Building</th>
          <th>Resource</th>
          <th>L1 Cap</th>
          <th>L10 Cap</th>
          <th>L30 Cap</th>
          <th>Formula</th>
        </tr>
      </thead>
      <tbody>
        <tr><td class="val">Food Silo</td><td>🥫 Food</td><td>1,000</td><td>9,100</td><td>27,100</td><td class="formula">1000 + (L−1)×900</td></tr>
        <tr><td class="val">Water Tank</td><td>💧 Water</td><td>1,000</td><td>9,100</td><td>27,100</td><td class="formula">1000 + (L−1)×900</td></tr>
        <tr><td class="val">Scrap Vault</td><td>⚙️ Scrap</td><td>1,000</td><td>9,100</td><td>27,100</td><td class="formula">1000 + (L−1)×900</td></tr>
        <tr><td class="val">Fuel Depot</td><td>⛽ Fuel</td><td>1,000</td><td>9,100</td><td>27,100</td><td class="formula">1000 + (L−1)×900</td></tr>
        <tr><td class="val">Power Bank</td><td>⚡ Energy</td><td>1,000</td><td>9,100</td><td>27,100</td><td class="formula">1000 + (L−1)×900</td></tr>
        <tr><td class="val">Tech Vault</td><td>🔬 RareTech</td><td>200</td><td>1,820</td><td>5,420</td><td class="formula">200 + (L−1)×180</td></tr>
        <tr><td class="val">Raid Vault</td><td>🔬 RareTech (scout)</td><td>100</td><td>1,000</td><td>∞</td><td class="formula">L×100 (unlimited at L10)</td></tr>
      </tbody>
    </table>

    <!-- MILITARY -->
    <h2 id="military">Military Buildings</h2>
    <p>
      Military buildings train your units. Each facility specializes in a different combat role.
      Training time decreases by <strong class="hl">5% per building level</strong>.
    </p>
    <div class="card-grid">
      <div class="data-card" v-for="b in byCategory('Military')" :key="b.name">
        <div class="data-card-header">
          <div class="data-card-icon"><img v-if="buildingImg(b.name)" :src="buildingImg(b.name)" :alt="b.name"><span v-else>{{ b.icon }}</span></div>
          <div>
            <div class="data-card-title">{{ b.name.toUpperCase() }}</div>
            <div class="data-card-sub">{{ b.category }}</div>
          </div>
        </div>
        <div class="data-card-body">
          <p>{{ b.desc }}</p>
          <ul style="padding-left: 16px; margin: 0; font-size: 13px;">
            <li v-for="e in b.effects" :key="e" style="color: var(--text); margin-bottom: 2px;">{{ e }}</li>
          </ul>
          <div style="margin-top: 10px; font-size: 12px; color: var(--muted);">
            <span style="color: var(--cyan); font-family: var(--ff-title); font-size: 9px; letter-spacing: 1px;">REQUIRES:</span>
            {{ b.prereqs }}
          </div>
        </div>
      </div>
    </div>

    <!-- DEFENSE -->
    <h2 id="defense">Defense Buildings</h2>
    <div class="card-grid">
      <div class="data-card" v-for="b in byCategory('Defense')" :key="b.name">
        <div class="data-card-header">
          <div class="data-card-icon"><img v-if="buildingImg(b.name)" :src="buildingImg(b.name)" :alt="b.name"><span v-else>{{ b.icon }}</span></div>
          <div>
            <div class="data-card-title">{{ b.name.toUpperCase() }}</div>
            <div class="data-card-sub">{{ b.category }}</div>
          </div>
        </div>
        <div class="data-card-body">
          <p>{{ b.desc }}</p>
          <ul style="padding-left: 16px; margin: 0; font-size: 13px;">
            <li v-for="e in b.effects" :key="e" style="color: var(--text); margin-bottom: 2px;">{{ e }}</li>
          </ul>
          <div style="margin-top: 10px; font-size: 12px; color: var(--muted);">
            <span style="color: var(--cyan); font-family: var(--ff-title); font-size: 9px; letter-spacing: 1px;">REQUIRES:</span>
            {{ b.prereqs }}
          </div>
        </div>
      </div>
    </div>

    <!-- RESEARCH & SPECIAL -->
    <h2 id="research">Research &amp; Special Buildings</h2>
    <div class="card-grid">
      <div class="data-card" v-for="b in [...byCategory('Research'), ...byCategory('Salvage')]" :key="b.name">
        <div class="data-card-header">
          <div class="data-card-icon"><img v-if="buildingImg(b.name)" :src="buildingImg(b.name)" :alt="b.name"><span v-else>{{ b.icon }}</span></div>
          <div>
            <div class="data-card-title">{{ b.name.toUpperCase() }}</div>
            <div class="data-card-sub">{{ b.category }}</div>
          </div>
        </div>
        <div class="data-card-body">
          <p>{{ b.desc }}</p>
          <ul style="padding-left: 16px; margin: 0; font-size: 13px;">
            <li v-for="e in b.effects" :key="e" style="color: var(--text); margin-bottom: 2px;">{{ e }}</li>
          </ul>
          <div style="margin-top: 10px; font-size: 12px; color: var(--muted);">
            <span style="color: var(--cyan); font-family: var(--ff-title); font-size: 9px; letter-spacing: 1px;">REQUIRES:</span>
            {{ b.prereqs }}
          </div>
        </div>
      </div>
    </div>

    <!-- FORMULAS -->
    <h2 id="formulas">Cost &amp; Build Time Formulas</h2>

    <div class="code-block">
      <span class="comment">// Resource cost to upgrade to target level</span><br>
      <span class="key">Cost</span> = <span class="key">BaseCost</span> × level<sup>1.55</sup><br><br>
      <span class="comment">// Build time in seconds (before HQ bonus)</span><br>
      <span class="key">BuildTime</span> = <span class="key">BaseTime</span> × level<sup>1.42</sup><br><br>
      <span class="comment">// HQ build time reduction (capped at 45%)</span><br>
      <span class="key">Multiplier</span> = <span class="val">1</span> − min(<span class="val">0.45</span>, HQ_level × <span class="val">0.015</span>)
    </div>

    <div class="info-box warning">
      <div class="info-label">⚠ HQ SPEED BONUS</div>
      At HeadQuarter level 30, your build time multiplier is <strong class="hl">0.55</strong>
      — meaning buildings are built in 55% of their base time. Upgrading HQ early pays off significantly.
    </div>

    <table class="stat-table">
      <thead>
        <tr>
          <th>HQ Level</th>
          <th>Build Time Multiplier</th>
          <th>Speed Reduction</th>
        </tr>
      </thead>
      <tbody>
        <tr><td class="val">1</td><td>× 1.00</td><td>0%</td></tr>
        <tr><td class="val">10</td><td>× 0.85</td><td>15%</td></tr>
        <tr><td class="val">20</td><td>× 0.70</td><td>30%</td></tr>
        <tr><td class="val">30</td><td>× 0.55</td><td>45%</td></tr>
      </tbody>
    </table>

    <div class="page-nav">
      <router-link to="/introduction">
        <span class="nav-hint">← Previous</span>
        <span class="nav-label">Introduction</span>
      </router-link>
      <router-link to="/resources" style="text-align:right">
        <span class="nav-hint">Next →</span>
        <span class="nav-label">Resources</span>
      </router-link>
    </div>
  </div>
</template>

<script setup>
import { BUILDINGS } from '../docs-data.js'

const BUILD_IMG = {
  HeadQuarter:    'Headquarter.png',
  Shelter:        'Shelter.png',
  CouncilHall:    'council hall.png',
  FarmDome:       'Farm Dome.png',
  WaterPurifier:  'WaterPurifier.png',
  ScrapForge:     'ScrapForge.png',
  FuelRefinery:   'Fuel Refinery.png',
  SolarArray:     'SolarArray.png',
  FoodSilo:       'FoodSilo.png',
  WaterTank:      'WaterTank.png',
  ScrapVault:     'ScrapVault.png',
  FuelDepot:      'FuelDepot.png',
  PowerBank:      'PowerBank.png',
  TechVault:      'TechVault.png',
  RaidVault:      'RelicVault.png',
  Barracks:       'Barracks.png',
  Garage:         'Garage.png',
  Workshop:       'Workshop.png',
  CommandCenter:  'Commando Center.png',
  PerimeterWall:  'PerimeterWall.png',
  TechLab:        'TechLab.png',
  TechSalvager:   'TechSalvager.png',
}

function byCategory(cat) { return BUILDINGS.filter(b => b.category === cat) }
function buildingImg(name) {
  const file = BUILD_IMG[name]
  return file ? new URL(`../../images/BuildingIcons/${file}`, import.meta.url).href : ''
}
</script>
