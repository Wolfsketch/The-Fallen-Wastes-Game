<template>
  <div>
    <div class="breadcrumb">
      <a href="#">Docs</a> <span class="sep">›</span> <span>Salvage</span>
    </div>

    <h1>SALVAGE <span class="tag">RARETECH</span></h1>
    <p class="page-lead">
      Salvage items are recovered during Wasteland raids and processed into RareTech by the
      Tech Salvager. They range from common scrap to legendary pre-fall relics, each yielding
      a different amount of RareTech when processed.
    </p>

    <h2 id="items">Salvage Items</h2>
    <p>
      Items are found in Points of Interest on the Wasteland map. Each POI spawns a random
      assortment when it (re-)spawns. Rarer items appear less frequently but yield substantially
      more RareTech.
    </p>

    <table class="stat-table">
      <thead>
        <tr>
          <th>Icon</th>
          <th>Item Name</th>
          <th>Rarity</th>
          <th>Process Time</th>
          <th>RT Yield</th>
          <th>Description</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="item in SALVAGE_ITEMS" :key="item.name">
          <td style="font-size: 18px;">{{ item.icon }}</td>
          <td class="val">{{ item.name }}</td>
          <td><span class="tag-r" :class="`tag-${item.rarity}`">{{ item.rarity }}</span></td>
          <td>{{ item.time }}</td>
          <td style="color: var(--cyan); font-weight: 600; font-family: var(--ff-title);">+{{ item.rt }} RT</td>
          <td style="color: var(--text); max-width: 200px; font-size: 12px;">{{ item.desc }}</td>
        </tr>
      </tbody>
    </table>

    <h2 id="salvager">Tech Salvager</h2>
    <p>
      The Tech Salvager is a special building that processes salvage items from your loot inventory.
      Items are processed one at a time in a queue — each item takes a fixed duration based on
      its rarity tier.
    </p>

    <table class="stat-table">
      <thead>
        <tr><th>Aspect</th><th>Detail</th></tr>
      </thead>
      <tbody>
        <tr><td class="val">Unlock</td><td>HQ 4, TechLab 2, ScrapForge 6, SolarArray 6</td></tr>
        <tr><td class="val">Queue</td><td>Items process one at a time; queue as many as you want</td></tr>
        <tr><td class="val">Output</td><td>RareTech added directly to your general resource pool</td></tr>
        <tr><td class="val">Speed</td><td>Higher Salvager level may reduce processing time (future)</td></tr>
        <tr><td class="val">Population</td><td>2 per level</td></tr>
      </tbody>
    </table>

    <div class="info-box warning">
      <div class="info-label">⚠ QUEUE YOUR ITEMS</div>
      Items sitting in your inventory do nothing. Queue them in the Tech Salvager immediately
      after returning from a raid — even while you're offline, the Salvager continues processing.
    </div>

    <h2 id="raretech-loop">RareTech Loop</h2>
    <p>
      RareTech drives all high-tier progression. Here is the complete loop:
    </p>

    <div style="display: flex; flex-direction: column; gap: 10px; margin: 20px 0;">
      <div class="loop-step">
        <div class="loop-num">1</div>
        <div>
          <strong class="hl">Raid a Wasteland POI</strong> — Fight NPC defenders and collect salvage loot items.
        </div>
      </div>
      <div class="loop-arrow">↓</div>
      <div class="loop-step">
        <div class="loop-num">2</div>
        <div>
          <strong class="hl">Return home</strong> — Your army travels back with the collected salvage.
          Items are added to your inventory.
        </div>
      </div>
      <div class="loop-arrow">↓</div>
      <div class="loop-step">
        <div class="loop-num">3</div>
        <div>
          <strong class="hl">Queue in Tech Salvager</strong> — Add items to the processing queue.
          Each item processes over a few minutes.
        </div>
      </div>
      <div class="loop-arrow">↓</div>
      <div class="loop-step">
        <div class="loop-num">4</div>
        <div>
          <strong class="hl">Earn RareTech</strong> — Each processed item adds RT to your
          general resource pool (capped by Tech Vault level).
        </div>
      </div>
      <div class="loop-arrow">↓</div>
      <div class="loop-step">
        <div class="loop-num">5</div>
        <div>
          <strong class="hl">Spend RT on research and units</strong> — Unlock new capabilities,
          train elite Workshop units, progress the tech tree.
        </div>
      </div>
    </div>

    <div class="info-box">
      <div class="info-label">☢ EFFICIENCY TIP</div>
      Higher rarity items yield far more RT per processing minute. Prioritize looting POIs
      that show Rare/Epic items in scout reports. A single Vault Artifact (Legendary) yields
      24 RT — the same as twelve Scrap Bundles.
    </div>

    <div class="page-nav">
      <router-link to="/research">
        <span class="nav-hint">← Previous</span>
        <span class="nav-label">Research</span>
      </router-link>
      <router-link to="/wasteland" style="text-align:right">
        <span class="nav-hint">Next →</span>
        <span class="nav-label">Wasteland</span>
      </router-link>
    </div>
  </div>
</template>

<script setup>
import { SALVAGE_ITEMS } from '../docs-data.js'
</script>

<style scoped>
.loop-step {
  display: flex; align-items: flex-start; gap: 14px;
  background: var(--bg2); border: 1px solid var(--border);
  border-radius: 8px; padding: 14px 16px;
}
.loop-num {
  width: 28px; height: 28px; border-radius: 50%; flex-shrink: 0;
  background: var(--cyan-dim); color: var(--cyan);
  font-family: var(--ff-title); font-size: 13px; font-weight: 700;
  display: grid; place-items: center; border: 1px solid var(--cyan-dark);
}
.loop-arrow { color: var(--muted); font-size: 20px; text-align: center; }
</style>
