<template>
  <div>
    <div class="breadcrumb">
      <a href="#">Docs</a> <span class="sep">›</span> <span>Population</span>
    </div>

    <h1>POPULATION <span class="tag">SETTLEMENT</span></h1>
    <p class="page-lead">
      Population is a shared capacity that limits how many buildings and units you can have active
      at the same time. Your Shelter sets the maximum; everything else consumes from that pool.
    </p>

    <h2 id="shelter">Shelter Cap</h2>
    <p>
      The Shelter is the only building that <em>provides</em> population (rather than consuming it).
      Its cap formula grows faster at higher levels to support late-game armies:
    </p>

    <div class="code-block">
      <span class="comment">// Population capacity from Shelter</span><br>
      <span class="key">Capacity</span> = <span class="val">200</span> + <span class="val">35</span> × (level − <span class="val">1</span>) + <span class="val">6</span> × (level − <span class="val">1</span>)²
    </div>

    <table class="stat-table">
      <thead>
        <tr>
          <th>Shelter Level</th>
          <th>Population Cap</th>
        </tr>
      </thead>
      <tbody>
        <tr><td class="val">1</td><td>200</td></tr>
        <tr><td class="val">5</td><td>480</td></tr>
        <tr><td class="val">10</td><td>1,059</td></tr>
        <tr><td class="val">15</td><td>1,970</td></tr>
        <tr><td class="val">20</td><td>3,213</td></tr>
        <tr><td class="val">25</td><td>4,788</td></tr>
        <tr><td class="val">30</td><td>6,695</td></tr>
      </tbody>
    </table>

    <h2 id="usage">Population Usage Per Building</h2>
    <p>
      Population is consumed the moment a building is upgraded and when a unit is trained.
      Building population is permanent (until you downgrade); unit population returns when
      the unit dies or is dismissed.
    </p>

    <table class="stat-table">
      <thead>
        <tr>
          <th>Building</th>
          <th>Pop per Level</th>
          <th>At L30</th>
        </tr>
      </thead>
      <tbody>
        <tr><td class="val">HeadQuarter</td><td>4 / level</td><td>120</td></tr>
        <tr><td class="val">CouncilHall</td><td>3 / level</td><td>90</td></tr>
        <tr><td class="val">Barracks</td><td>3 / level</td><td>90</td></tr>
        <tr><td class="val">Garage</td><td>4 / level</td><td>120</td></tr>
        <tr><td class="val">Workshop</td><td>4 / level</td><td>120</td></tr>
        <tr><td class="val">CommandCenter</td><td>5 / level</td><td>150</td></tr>
        <tr><td class="val">TechLab</td><td>3 / level</td><td>90</td></tr>
        <tr><td class="val">TechSalvager</td><td>2 / level</td><td>60</td></tr>
        <tr><td class="val">FarmDome / WaterPurifier / ScrapForge / FuelRefinery / SolarArray</td><td>2 / level</td><td>60 each</td></tr>
        <tr><td class="val">Storage buildings, PerimeterWall</td><td>0</td><td>0</td></tr>
      </tbody>
    </table>

    <h3>Unit Population Cost</h3>
    <table class="stat-table">
      <thead>
        <tr>
          <th>Unit</th>
          <th>Pop Cost</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="u in UNITS" :key="u.name">
          <td class="val">{{ u.name }}</td>
          <td>{{ u.pop }}</td>
        </tr>
      </tbody>
    </table>

    <h2 id="strategy">Strategic Tradeoffs</h2>
    <p>
      At high levels, population becomes a critical bottleneck. You must make deliberate choices:
    </p>

    <div class="info-box danger">
      <div class="info-label">⛔ STRATEGIC WARNING</div>
      A fully upgraded settlement (all buildings at max) requires enormous population. At some point
      you will need to decide: more buildings, or more units? Consider <strong class="hl">downgrading
      your HeadQuarter</strong> from 30 → 20 to free up 40 population for additional troops.
      This is an intentional endgame tradeoff.
    </div>

    <div class="info-box">
      <div class="info-label">☢ TIP — Production vs Army</div>
      Five resource buildings at level 20 cost 5 × 40 = 200 population. That is the same
      as training ~50 Raiders. Consider running production buildings at a slightly lower level
      to keep an active army in the field.
    </div>

    <div class="page-nav">
      <router-link to="/resources">
        <span class="nav-hint">← Previous</span>
        <span class="nav-label">Resources</span>
      </router-link>
      <router-link to="/units" style="text-align:right">
        <span class="nav-hint">Next →</span>
        <span class="nav-label">Units</span>
      </router-link>
    </div>
  </div>
</template>

<script setup>
import { UNITS } from '../docs-data.js'
</script>
