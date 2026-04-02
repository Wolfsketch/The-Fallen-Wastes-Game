<template>
  <div>
    <div class="breadcrumb">
      <a href="#">Docs</a> <span class="sep">›</span> <span>Units</span>
    </div>

    <h1>UNITS <span class="tag">MILITARY</span></h1>
    <p class="page-lead">
      Every unit has a unique combat profile. Understanding attack power, defense values,
      speed, and carrying capacity is critical for building effective armies and raid squads.
    </p>

    <div class="info-box">
      <div class="info-label">☢ STAT GLOSSARY</div>
      <strong class="hl">ATK</strong> — Raw attack power &nbsp;|&nbsp;
      <strong class="hl">SPD</strong> — Movement speed (higher = faster travel) &nbsp;|&nbsp;
      <strong class="hl">CARRY</strong> — Loot capacity in raids &nbsp;|&nbsp;
      <strong class="hl">POP</strong> — Population consumed when trained
    </div>

    <h2 id="barracks">Barracks Units</h2>
    <p>
      Infantry forms the backbone of every army. Cheap to train, quick to replace, and
      essential for early raiding. The Barracks unlocks at
      <strong class="hl">HQ 2 + Shelter 3 + all resource buildings at 3</strong>.
    </p>
    <div class="unit-grid">
      <div class="data-card" v-for="u in byFacility('Barracks')" :key="u.name">
        <div class="data-card-header">
          <div class="data-card-icon"><img v-if="unitImg(u.name)" :src="unitImg(u.name)" :alt="u.name"><span v-else>{{ damageIcon(u.damage) }}</span></div>
          <div>
            <div class="data-card-title">{{ u.name.toUpperCase() }}</div>
            <div class="data-card-sub">
              {{ u.role }} &nbsp;
              <span class="tag-inline tag-barracks">Barracks</span>
            </div>
          </div>
        </div>
        <div class="data-card-body" style="font-size: 13px; color: var(--text); padding-bottom: 0">
          {{ u.desc }}
        </div>
        <div class="data-card-stats">
          <div class="stat-cell"><div class="stat-cell-label">ATK</div><div class="stat-cell-val atk">{{ u.atk }}</div></div>
          <div class="stat-cell"><div class="stat-cell-label">SPEED</div><div class="stat-cell-val spd">{{ u.spd }}</div></div>
          <div class="stat-cell"><div class="stat-cell-label">CARRY</div><div class="stat-cell-val">{{ u.carry }}</div></div>
          <div class="stat-cell"><div class="stat-cell-label">POP</div><div class="stat-cell-val">{{ u.pop }}</div></div>
        </div>
        <div class="data-card-defense">
          <div class="def-cell"><div class="def-cell-label">VS BALLISTIC</div><div class="def-cell-val">{{ u.defBal }}</div></div>
          <div class="def-cell"><div class="def-cell-label">VS IMPACT</div><div class="def-cell-val">{{ u.defImp }}</div></div>
          <div class="def-cell"><div class="def-cell-label">VS ENERGY</div><div class="def-cell-val">{{ u.defEne }}</div></div>
        </div>
        <div class="data-card-costs">
          <span v-if="u.costs.food"    class="cost-item"><span class="res-icon res-food">🥫</span>{{ u.costs.food }}</span>
          <span v-if="u.costs.water"   class="cost-item"><span class="res-icon res-water">💧</span>{{ u.costs.water }}</span>
          <span v-if="u.costs.scrap"   class="cost-item"><span class="res-icon res-scrap">⚙️</span>{{ u.costs.scrap }}</span>
          <span v-if="u.costs.fuel"    class="cost-item"><span class="res-icon res-fuel">⛽</span>{{ u.costs.fuel }}</span>
          <span v-if="u.costs.energy"  class="cost-item"><span class="res-icon res-energy">⚡</span>{{ u.costs.energy }}</span>
          <span v-if="u.costs.rareTech" class="cost-item"><span class="res-icon res-rt">🔬</span>{{ u.costs.rareTech }}</span>
          <span style="margin-left:auto; font-family: var(--ff-title); font-size: 10px; color: var(--muted);">⏱ {{ u.trainTime }}</span>
        </div>
      </div>
    </div>

    <h2 id="garage">Garage Units</h2>
    <p>
      Vehicles dominate the open wasteland. Faster and stronger than infantry but costlier.
      The Garage unlocks at <strong class="hl">HQ 4 + Shelter 5 + all resource buildings at 7</strong>.
    </p>
    <div class="unit-grid">
      <div class="data-card" v-for="u in byFacility('Garage')" :key="u.name">
        <div class="data-card-header">
          <div class="data-card-icon"><img v-if="unitImg(u.name)" :src="unitImg(u.name)" :alt="u.name"><span v-else>{{ damageIcon(u.damage) }}</span></div>
          <div>
            <div class="data-card-title">{{ u.name.toUpperCase() }}</div>
            <div class="data-card-sub">
              {{ u.role }} &nbsp;
              <span class="tag-inline tag-garage">Garage</span>
            </div>
          </div>
        </div>
        <div class="data-card-body" style="font-size: 13px; color: var(--text); padding-bottom: 0">
          {{ u.desc }}
        </div>
        <div class="data-card-stats">
          <div class="stat-cell"><div class="stat-cell-label">ATK</div><div class="stat-cell-val atk">{{ u.atk }}</div></div>
          <div class="stat-cell"><div class="stat-cell-label">SPEED</div><div class="stat-cell-val spd">{{ u.spd }}</div></div>
          <div class="stat-cell"><div class="stat-cell-label">CARRY</div><div class="stat-cell-val">{{ u.carry }}</div></div>
          <div class="stat-cell"><div class="stat-cell-label">POP</div><div class="stat-cell-val">{{ u.pop }}</div></div>
        </div>
        <div class="data-card-defense">
          <div class="def-cell"><div class="def-cell-label">VS BALLISTIC</div><div class="def-cell-val">{{ u.defBal }}</div></div>
          <div class="def-cell"><div class="def-cell-label">VS IMPACT</div><div class="def-cell-val">{{ u.defImp }}</div></div>
          <div class="def-cell"><div class="def-cell-label">VS ENERGY</div><div class="def-cell-val">{{ u.defEne }}</div></div>
        </div>
        <div class="data-card-costs">
          <span v-if="u.costs.food"    class="cost-item"><span class="res-icon res-food">🥫</span>{{ u.costs.food }}</span>
          <span v-if="u.costs.water"   class="cost-item"><span class="res-icon res-water">💧</span>{{ u.costs.water }}</span>
          <span v-if="u.costs.scrap"   class="cost-item"><span class="res-icon res-scrap">⚙️</span>{{ u.costs.scrap }}</span>
          <span v-if="u.costs.fuel"    class="cost-item"><span class="res-icon res-fuel">⛽</span>{{ u.costs.fuel }}</span>
          <span v-if="u.costs.energy"  class="cost-item"><span class="res-icon res-energy">⚡</span>{{ u.costs.energy }}</span>
          <span v-if="u.costs.rareTech" class="cost-item"><span class="res-icon res-rt">🔬</span>{{ u.costs.rareTech }}</span>
          <span style="margin-left:auto; font-family: var(--ff-title); font-size: 10px; color: var(--muted);">⏱ {{ u.trainTime }}</span>
        </div>
      </div>
    </div>

    <h2 id="workshop">Workshop Units</h2>
    <p>
      Advanced and siege units. Many require RareTech to train. The Workshop unlocks at
      <strong class="hl">HQ 4 + Shelter 7 + TechLab 4 + all resource buildings at 10</strong>.
    </p>
    <div class="unit-grid">
      <div class="data-card" v-for="u in byFacility('Workshop')" :key="u.name">
        <div class="data-card-header">
          <div class="data-card-icon"><img v-if="unitImg(u.name)" :src="unitImg(u.name)" :alt="u.name"><span v-else>{{ damageIcon(u.damage) }}</span></div>
          <div>
            <div class="data-card-title">{{ u.name.toUpperCase() }}</div>
            <div class="data-card-sub">
              {{ u.role }} &nbsp;
              <span class="tag-inline tag-workshop">Workshop</span>
            </div>
          </div>
        </div>
        <div class="data-card-body" style="font-size: 13px; color: var(--text); padding-bottom: 0">
          {{ u.desc }}
        </div>
        <div class="data-card-stats">
          <div class="stat-cell"><div class="stat-cell-label">ATK</div><div class="stat-cell-val atk">{{ u.atk }}</div></div>
          <div class="stat-cell"><div class="stat-cell-label">SPEED</div><div class="stat-cell-val spd">{{ u.spd }}</div></div>
          <div class="stat-cell"><div class="stat-cell-label">CARRY</div><div class="stat-cell-val">{{ u.carry }}</div></div>
          <div class="stat-cell"><div class="stat-cell-label">POP</div><div class="stat-cell-val">{{ u.pop }}</div></div>
        </div>
        <div class="data-card-defense">
          <div class="def-cell"><div class="def-cell-label">VS BALLISTIC</div><div class="def-cell-val">{{ u.defBal }}</div></div>
          <div class="def-cell"><div class="def-cell-label">VS IMPACT</div><div class="def-cell-val">{{ u.defImp }}</div></div>
          <div class="def-cell"><div class="def-cell-label">VS ENERGY</div><div class="def-cell-val">{{ u.defEne }}</div></div>
        </div>
        <div class="data-card-costs">
          <span v-if="u.costs.food"    class="cost-item"><span class="res-icon res-food">🥫</span>{{ u.costs.food }}</span>
          <span v-if="u.costs.water"   class="cost-item"><span class="res-icon res-water">💧</span>{{ u.costs.water }}</span>
          <span v-if="u.costs.scrap"   class="cost-item"><span class="res-icon res-scrap">⚙️</span>{{ u.costs.scrap }}</span>
          <span v-if="u.costs.fuel"    class="cost-item"><span class="res-icon res-fuel">⛽</span>{{ u.costs.fuel }}</span>
          <span v-if="u.costs.energy"  class="cost-item"><span class="res-icon res-energy">⚡</span>{{ u.costs.energy }}</span>
          <span v-if="u.costs.rareTech" class="cost-item"><span class="res-icon res-rt">🔬</span>{{ u.costs.rareTech }}</span>
          <span style="margin-left:auto; font-family: var(--ff-title); font-size: 10px; color: var(--muted);">⏱ {{ u.trainTime }}</span>
        </div>
      </div>
    </div>

    <h2 id="command-center">Command Center</h2>
    <p>
      Strategic units for expansion and siege operations. CommandCenter unlocks at
      <strong class="hl">HQ 5 + Shelter 8 + TechLab 8 + all resource buildings at 12</strong>.
    </p>
    <div class="unit-grid">
      <div class="data-card" v-for="u in byFacility('CommandCenter')" :key="u.name">
        <div class="data-card-header">
          <div class="data-card-icon"><img v-if="unitImg(u.name)" :src="unitImg(u.name)" :alt="u.name"><span v-else>📡</span></div>
          <div>
            <div class="data-card-title">{{ u.name.toUpperCase() }}</div>
            <div class="data-card-sub">
              {{ u.role }} &nbsp;
              <span class="tag-inline tag-command">Command</span>
            </div>
          </div>
        </div>
        <div class="data-card-body" style="font-size: 13px; color: var(--text); padding-bottom: 0">
          {{ u.desc }}
        </div>
        <div class="data-card-stats">
          <div class="stat-cell"><div class="stat-cell-label">ATK</div><div class="stat-cell-val atk">{{ u.atk }}</div></div>
          <div class="stat-cell"><div class="stat-cell-label">SPEED</div><div class="stat-cell-val spd">{{ u.spd }}</div></div>
          <div class="stat-cell"><div class="stat-cell-label">CARRY</div><div class="stat-cell-val">{{ u.carry }}</div></div>
          <div class="stat-cell"><div class="stat-cell-label">POP</div><div class="stat-cell-val">{{ u.pop }}</div></div>
        </div>
        <div class="data-card-defense">
          <div class="def-cell"><div class="def-cell-label">VS BALLISTIC</div><div class="def-cell-val">{{ u.defBal }}</div></div>
          <div class="def-cell"><div class="def-cell-label">VS IMPACT</div><div class="def-cell-val">{{ u.defImp }}</div></div>
          <div class="def-cell"><div class="def-cell-label">VS ENERGY</div><div class="def-cell-val">{{ u.defEne }}</div></div>
        </div>
        <div class="data-card-costs">
          <span v-if="u.costs.food"    class="cost-item"><span class="res-icon res-food">🥫</span>{{ u.costs.food }}</span>
          <span v-if="u.costs.water"   class="cost-item"><span class="res-icon res-water">💧</span>{{ u.costs.water }}</span>
          <span v-if="u.costs.scrap"   class="cost-item"><span class="res-icon res-scrap">⚙️</span>{{ u.costs.scrap }}</span>
          <span v-if="u.costs.fuel"    class="cost-item"><span class="res-icon res-fuel">⛽</span>{{ u.costs.fuel }}</span>
          <span v-if="u.costs.energy"  class="cost-item"><span class="res-icon res-energy">⚡</span>{{ u.costs.energy }}</span>
          <span v-if="u.costs.rareTech" class="cost-item"><span class="res-icon res-rt">🔬</span>{{ u.costs.rareTech }}</span>
          <span style="margin-left:auto; font-family: var(--ff-title); font-size: 10px; color: var(--muted);">⏱ {{ u.trainTime }}</span>
        </div>
      </div>
    </div>

    <h2 id="training-speed">Training Speed</h2>
    <p>
      Each military building reduces training time by <strong class="hl">5% per level</strong>.
    </p>
    <div class="code-block">
      <span class="comment">// Effective training time formula</span><br>
      <span class="key">EffectiveTime</span> = <span class="key">BaseTime</span> / (<span class="val">1</span> + <span class="val">0.05</span> × (FacilityLevel − <span class="val">1</span>))
    </div>

    <table class="stat-table">
      <thead>
        <tr>
          <th>Facility Level</th>
          <th>Speed Multiplier</th>
          <th>4m unit becomes</th>
          <th>14m unit becomes</th>
        </tr>
      </thead>
      <tbody>
        <tr><td class="val">1</td><td>×1.00</td><td>4m 00s</td><td>14m 00s</td></tr>
        <tr><td class="val">5</td><td>×1.20</td><td>3m 20s</td><td>11m 40s</td></tr>
        <tr><td class="val">10</td><td>×1.45</td><td>2m 46s</td><td>9m 39s</td></tr>
        <tr><td class="val">15</td><td>×1.70</td><td>2m 21s</td><td>8m 14s</td></tr>
        <tr><td class="val">20</td><td>×1.95</td><td>2m 03s</td><td>7m 11s</td></tr>
        <tr><td class="val">30</td><td>×2.45</td><td>1m 38s</td><td>5m 43s</td></tr>
      </tbody>
    </table>

    <div class="page-nav">
      <router-link to="/population">
        <span class="nav-hint">← Previous</span>
        <span class="nav-label">Population</span>
      </router-link>
      <router-link to="/combat" style="text-align:right">
        <span class="nav-hint">Next →</span>
        <span class="nav-label">Combat System</span>
      </router-link>
    </div>
  </div>
</template>

<script setup>
import { UNITS } from '../docs-data.js'

const UNIT_IMG_OVERRIDE = { 'Drone Swarm': 'Scout Drone.png' }
const UNIT_NO_IMG = new Set(['EMP Launcher', 'Laser Squad', 'Rad Bomber'])

function byFacility(f) { return UNITS.filter(u => u.facility === f) }
function unitImg(name) {
  if (UNIT_NO_IMG.has(name)) return ''
  const file = UNIT_IMG_OVERRIDE[name] ?? `${name}.png`
  return new URL(`../../images/${file}`, import.meta.url).href
}
function damageIcon(d) {
  if (d === 'Ballistic') return '🔫'
  if (d === 'Impact')    return '💥'
  if (d === 'Energy')    return '⚡'
  return '🚐'
}
</script>

<style scoped>
.unit-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(330px, 1fr)); gap: 14px; margin: 18px 0; }
</style>
