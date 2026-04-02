<template>
  <div>
    <div class="breadcrumb">
      <a href="#">Docs</a> <span class="sep">›</span> <span>Resources</span>
    </div>

    <h1>RESOURCES <span class="tag">ECONOMY</span></h1>
    <p class="page-lead">
      Six distinct resources drive your settlement's economy. Each has its own production
      building, storage cap, and consumption pattern. Understanding the resource ecosystem
      is fundamental to progression.
    </p>

    <h2 id="types">Resource Types</h2>
    <div class="card-grid">
      <div class="data-card" v-for="r in RESOURCE_INFO" :key="r.key">
        <div class="data-card-header">
          <div class="data-card-icon" :style="{ background: r.bg, color: r.color }">{{ r.icon }}</div>
          <div>
            <div class="data-card-title" :style="{ color: r.color }">{{ r.key.toUpperCase() }}</div>
            <div class="data-card-sub">{{ r.key === 'RareTech' ? 'Salvage economy' : 'Base resource' }}</div>
          </div>
        </div>
        <div class="data-card-body">{{ r.desc }}</div>
      </div>
    </div>

    <h2 id="production2">Production Formula</h2>
    <p>
      All five base resources (Water, Food, Scrap, Fuel, Energy) share the same exponential
      production curve. Higher levels give drastically more output but cost significantly more to
      upgrade:
    </p>

    <div class="code-block">
      <span class="comment">// Units produced per hour at a given building level</span><br>
      <span class="key">Output</span> = <span class="val">23</span> × level × <span class="val">1.12</span>^(level − 1)
    </div>

    <table class="stat-table">
      <thead>
        <tr>
          <th>Level</th>
          <th>Output/hr</th>
          <th>Pop used</th>
        </tr>
      </thead>
      <tbody>
        <tr><td class="val">1</td><td>~23</td><td>2</td></tr>
        <tr><td class="val">3</td><td>~87</td><td>6</td></tr>
        <tr><td class="val">5</td><td>~181</td><td>10</td></tr>
        <tr><td class="val">7</td><td>~332</td><td>14</td></tr>
        <tr><td class="val">10</td><td>~630</td><td>20</td></tr>
        <tr><td class="val">15</td><td>~1,600</td><td>30</td></tr>
        <tr><td class="val">20</td><td>~3,600</td><td>40</td></tr>
        <tr><td class="val">30</td><td>~16,000</td><td>60</td></tr>
      </tbody>
    </table>

    <div class="info-box">
      <div class="info-label">☢ NOTE — RareTech production</div>
      RareTech is <strong class="hl">not</strong> produced by a building. It is obtained exclusively
      by processing salvage items in the Tech Salvager or through scouting operations.
    </div>

    <h2 id="storage2">Storage Caps</h2>
    <p>
      When a storage building is full, its paired production building stops generating resources.
      Always keep storage slightly ahead of your production rate.
    </p>

    <div class="code-block">
      <span class="comment">// Storage cap for base resources (Water, Food, Scrap, Fuel, Energy)</span><br>
      <span class="key">Cap</span> = <span class="val">1000</span> + (level − <span class="val">1</span>) × <span class="val">900</span><br><br>
      <span class="comment">// Storage for RareTech (Tech Vault)</span><br>
      <span class="key">Cap</span> = <span class="val">200</span> + (level − <span class="val">1</span>) × <span class="val">180</span>
    </div>

    <div class="info-box warning">
      <div class="info-label">⚠ OVERFLOW WARNING</div>
      Resources over the cap are lost. Check your storage regularly and upgrade storage buildings
      before production outpaces them.
    </div>

    <h2 id="raretech">RareTech Economy</h2>
    <p>
      RareTech (<strong class="hl-cyan">RT</strong>) is the most valuable resource in the game.
      It cannot be produced with buildings — instead it comes from:
    </p>

    <table class="stat-table">
      <thead>
        <tr>
          <th>Source</th>
          <th>Quantity</th>
          <th>Notes</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td class="val">Salvage processing</td>
          <td>1–24 RT per item</td>
          <td>Depends on item rarity (Common → Legendary)</td>
        </tr>
        <tr>
          <td class="val">Scout operations</td>
          <td>Variable</td>
          <td>Goes to Raid Vault (separate pool for scout RT)</td>
        </tr>
      </tbody>
    </table>

    <div class="info-box">
      <div class="info-label">☢ TWO RT POOLS</div>
      There are <strong class="hl">two separate RareTech pools</strong>:<br><br>
      • <strong class="hl-cyan">General RareTech</strong> — shown in your resource bar. Used for research and unit training. Capped by Tech Vault.<br><br>
      • <strong class="hl-amber">Vault RareTech</strong> — earned from scouting. Stored in the Raid Vault. Used for scout-specific rewards.
    </div>

    <div class="page-nav">
      <router-link to="/buildings">
        <span class="nav-hint">← Previous</span>
        <span class="nav-label">Buildings</span>
      </router-link>
      <router-link to="/population" style="text-align:right">
        <span class="nav-hint">Next →</span>
        <span class="nav-label">Population</span>
      </router-link>
    </div>
  </div>
</template>

<script setup>
import { RESOURCE_INFO } from '../docs-data.js'
</script>
