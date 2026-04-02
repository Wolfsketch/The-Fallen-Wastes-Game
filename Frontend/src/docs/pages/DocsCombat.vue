<template>
  <div>
    <div class="breadcrumb">
      <a href="#">Docs</a> <span class="sep">›</span> <span>Combat System</span>
    </div>

    <h1>COMBAT SYSTEM <span class="tag">MILITARY</span></h1>
    <p class="page-lead">
      Every engagement in The Fallen Wastes resolves through a damage-versus-defense calculation.
      Three damage types each interact differently with the three defense values every unit carries.
    </p>

    <h2 id="damage-types">Damage Types</h2>
    <div style="display: flex; gap: 12px; flex-wrap: wrap; margin: 16px 0;">
      <div class="dmg-card">
        <div class="dmg-icon" style="background:rgba(0,212,255,.15); color: var(--cyan);">🔫</div>
        <div style="font-family: var(--ff-title); font-size: 11px; letter-spacing: 2px; color: var(--cyan); margin-bottom: 6px;">BALLISTIC</div>
        <p style="font-size: 13px; margin: 0;">Conventional projectile weapons. High availability, solid general-purpose damage. Used by most early infantry.</p>
      </div>
      <div class="dmg-card">
        <div class="dmg-icon" style="background:rgba(255,170,32,.15); color: var(--amber);">💥</div>
        <div style="font-family: var(--ff-title); font-size: 11px; letter-spacing: 2px; color: var(--amber); margin-bottom: 6px;">IMPACT</div>
        <p style="font-size: 13px; margin: 0;">Heavy melee and explosive. Effective against structures, walls, and tightly grouped defenders. Used by assault units.</p>
      </div>
      <div class="dmg-card">
        <div class="dmg-icon" style="background:rgba(138,100,255,.15); color: var(--purple);">⚡</div>
        <div style="font-family: var(--ff-title); font-size: 11px; letter-spacing: 2px; color: var(--purple); margin-bottom: 6px;">ENERGY</div>
        <p style="font-size: 13px; margin: 0;">Advanced tech weaponry. Expensive to field but devastating against armored targets. Workshop and Garage specialists.</p>
      </div>
    </div>

    <h2 id="defense-vals">Defense Values</h2>
    <p>
      Every unit has <strong class="hl">three separate defense values</strong> — one for each damage type.
      A unit's defense is subtracted from incoming damage of that type before health is reduced.
      Units can be tanky against one type while remaining vulnerable to another.
    </p>

    <div class="info-box">
      <div class="info-label">☢ TACTICAL TIP</div>
      A unit with high Ballistic defense may still be annihilated by Energy weapons. Always
      check the composition of your enemy before committing.
    </div>

    <table class="stat-table">
      <thead>
        <tr>
          <th>Unit</th>
          <th>Facility</th>
          <th>Damage</th>
          <th>DEF vs Ballistic</th>
          <th>DEF vs Impact</th>
          <th>DEF vs Energy</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="u in UNITS" :key="u.name">
          <td class="val">{{ u.name }}</td>
          <td>
            <span :class="facilityTag(u.facility)" class="tag-inline">{{ u.facility }}</span>
          </td>
          <td :style="{ color: dmgColor(u.damage) }">{{ u.damage }}</td>
          <td>{{ u.defBal }}</td>
          <td>{{ u.defImp }}</td>
          <td>{{ u.defEne }}</td>
        </tr>
      </tbody>
    </table>

    <h2 id="formula">Combat Formula</h2>
    <p>
      Combat is resolved in real-time simulation rounds. Attackers and defenders exchange fire
      simultaneously. The damage dealt to a unit per round is:
    </p>

    <div class="code-block">
      <span class="comment">// Damage dealt to a single target unit in one round</span><br>
      <span class="key">RawDamage</span> = attacker.ATK<br>
      <span class="key">NetDamage</span>  = max(<span class="val">1</span>, RawDamage − target.DEF[attacker.DamageType])<br><br>
      <span class="comment">// A unit is eliminated when accumulated damage exceeds its total HP</span>
    </div>

    <div class="info-box warning">
      <div class="info-label">⚠ MINIMUM DAMAGE</div>
      Damage is always at least 1 regardless of defense. A unit can never be completely immune
      to damage — stacking a single defense type provides diminishing returns.
    </div>

    <h2 id="tips-combat">Combat Tips</h2>

    <div class="info-box">
      <div class="info-label">☢ MIX YOUR ARMY</div>
      Sending only Ballistic units against high-Ballistic-defense defenders is inefficient.
      Use a balanced mix of Ballistic, Impact, and Energy units to cover all weaknesses.
    </div>

    <div class="info-box">
      <div class="info-label">☢ WALL BREAKERS FOR SIEGES</div>
      When besieging a player with a high-level Perimeter Wall, bring
      <strong class="hl">Wall Breakers</strong>. They deal Impact damage directly against
      wall HP and ignore most wall resistance.
    </div>

    <div class="info-box">
      <div class="info-label">☢ SCOUT BEFORE RAIDING PLAYERS</div>
      Scouting reveals the defender's army composition and the wall HP.
      Never commit to a player raid without scouting intelligence first.
    </div>

    <div class="page-nav">
      <router-link to="/units">
        <span class="nav-hint">← Previous</span>
        <span class="nav-label">Units</span>
      </router-link>
      <router-link to="/operations" style="text-align:right">
        <span class="nav-hint">Next →</span>
        <span class="nav-label">Operations</span>
      </router-link>
    </div>
  </div>
</template>

<script setup>
import { UNITS } from '../docs-data.js'

function facilityTag(f) {
  return {
    Barracks: 'tag-barracks', Garage: 'tag-garage',
    Workshop: 'tag-workshop', CommandCenter: 'tag-command',
  }[f] ?? ''
}
function dmgColor(d) {
  return { Ballistic: 'var(--cyan)', Impact: 'var(--amber)', Energy: 'var(--purple)', None: 'var(--muted)' }[d] ?? 'var(--text)'
}
</script>

<style scoped>
.dmg-card {
  flex: 1; min-width: 170px; background: var(--bg2);
  border: 1px solid var(--border); border-radius: 8px; padding: 16px;
}
.dmg-icon {
  width: 32px; height: 32px; border-radius: 50%;
  display: grid; place-items: center; font-size: 16px; margin-bottom: 10px;
}
</style>
