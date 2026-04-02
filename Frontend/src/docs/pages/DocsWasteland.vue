<template>
  <div>
    <div class="breadcrumb">
      <a href="#">Docs</a> <span class="sep">›</span> <span>Wasteland &amp; POI</span>
    </div>

    <h1>WASTELAND <span class="tag">WORLD MAP</span></h1>
    <p class="page-lead">
      The Wasteland is the open world map shared by all players. It contains neutral Points of
      Interest (POIs) filled with NPC defenders and salvage loot, as well as other player
      settlements that can be scouted and raided.
    </p>

    <h2 id="overview">Map Overview</h2>
    <p>
      The map is divided into a grid of sectors. Each sector shows which player settlements
      and POIs occupy it. Operations travel between sectors in real time — the further the
      target, the longer the travel time.
    </p>

    <div class="card-grid">
      <div class="data-card">
        <div class="data-card-header">
          <div class="data-card-icon">🔴</div>
          <div>
            <div class="data-card-title">POINTS OF INTEREST</div>
            <div class="data-card-sub">Neutral objectives</div>
          </div>
        </div>
        <div class="data-card-body">
          Fixed locations on the map containing NPC defenders and salvage loot.
          They respawn periodically after being cleared.
        </div>
      </div>
      <div class="data-card">
        <div class="data-card-header">
          <div class="data-card-icon">🏚️</div>
          <div>
            <div class="data-card-title">PLAYER SETTLEMENTS</div>
            <div class="data-card-sub">PvP targets</div>
          </div>
        </div>
        <div class="data-card-body">
          Other players' bases visible on the map. You can scout them, raid for resources,
          or attempt a full siege and conquest.
        </div>
      </div>
    </div>

    <h2 id="poi">Points of Interest</h2>
    <p>
      Each POI has a <strong class="hl">name</strong>, a <strong class="hl">sector location</strong>,
      a pool of <strong class="hl">NPC defenders</strong>, and a set of <strong class="hl">salvage items</strong>.
      POIs are color-coded on the map by status:
    </p>

    <table class="stat-table">
      <thead>
        <tr><th>Color</th><th>Status</th><th>Meaning</th></tr>
      </thead>
      <tbody>
        <tr>
          <td><span style="color: var(--green); font-size: 18px">●</span></td>
          <td class="val">Active</td>
          <td>POI has defenders and/or loot — can be raided or scouted</td>
        </tr>
        <tr>
          <td><span style="color: var(--muted); font-size: 18px">●</span></td>
          <td class="val">Cleared</td>
          <td>All defenders dead AND all loot taken — waiting to respawn</td>
        </tr>
        <tr>
          <td><span style="color: var(--amber); font-size: 18px">●</span></td>
          <td class="val">Relocating</td>
          <td>POI is in the process of moving to a new sector (periodic event)</td>
        </tr>
      </tbody>
    </table>

    <div class="info-box">
      <div class="info-label">☢ PARTIAL CLEAR</div>
      A POI is only <strong class="hl">CLEARED</strong> when <em>both</em> conditions are true:
      all NPC defenders are dead <strong class="hl">AND</strong> all salvage items have been collected.
      If enemies are dead but loot remains, the POI is still active and can be raided again.
    </div>

    <h2 id="cleared">Cleared &amp; Respawn</h2>
    <p>
      Once a POI reaches the CLEARED state, a respawn timer begins. After the timer expires,
      the POI spawns a new set of NPC defenders and fresh salvage items and becomes active again.
    </p>

    <table class="stat-table">
      <thead>
        <tr><th>Aspect</th><th>Detail</th></tr>
      </thead>
      <tbody>
        <tr><td class="val">Cleared condition</td><td>NPCs = 0 AND loot items = 0</td></tr>
        <tr><td class="val">Respawn timer</td><td>Fixed per POI type (varies by POI)</td></tr>
        <tr><td class="val">After respawn</td><td>New NPC defenders + new random loot pool</td></tr>
        <tr><td class="val">Scout on cleared</td><td>Not allowed — receive an error. Wait for respawn.</td></tr>
        <tr><td class="val">Raid on cleared</td><td>Not useful — POI has nothing to attack or take</td></tr>
      </tbody>
    </table>

    <h2 id="loot">Loot System</h2>
    <p>
      When your raid arrives at a POI and wins the battle, you automatically take salvage items
      up to your army's carry capacity. The process works like this:
    </p>

    <div style="font-size: 14px; display: flex; flex-direction: column; gap: 8px; margin: 16px 0;">
      <div style="display: flex; gap: 10px;">
        <span style="color: var(--cyan); font-family: var(--ff-title); font-size: 12px; min-width: 80px;">ARRIVE</span>
        <span>Your forces reach the POI and fight the NPC defenders.</span>
      </div>
      <div style="display: flex; gap: 10px;">
        <span style="color: var(--cyan); font-family: var(--ff-title); font-size: 12px; min-width: 80px;">VICTORY</span>
        <span>NPC units are eliminated. Loot items become available.</span>
      </div>
      <div style="display: flex; gap: 10px;">
        <span style="color: var(--cyan); font-family: var(--ff-title); font-size: 12px; min-width: 80px;">COLLECT</span>
        <span>Items are collected up to total CARRY capacity of your units. Items beyond capacity stay in the POI.</span>
      </div>
      <div style="display: flex; gap: 10px;">
        <span style="color: var(--cyan); font-family: var(--ff-title); font-size: 12px; min-width: 80px;">RETURN</span>
        <span>Army travels home. Items are displayed as "in transit" on the return journey.</span>
      </div>
      <div style="display: flex; gap: 10px;">
        <span style="color: var(--cyan); font-family: var(--ff-title); font-size: 12px; min-width: 80px;">ARRIVE</span>
        <span>Loot is added to your inventory. A battle report is sent to Messages.</span>
      </div>
    </div>

    <div class="info-box warning">
      <div class="info-label">⚠ LEFTOVER LOOT</div>
      If your carry capacity was insufficient to take all items, the remaining items stay at the
      POI. Another player can raid and take them. Send a follow-up raid with more units if you
      want everything.
    </div>

    <div class="page-nav">
      <router-link to="/salvage">
        <span class="nav-hint">← Previous</span>
        <span class="nav-label">Salvage</span>
      </router-link>
      <router-link to="/alliances" style="text-align:right">
        <span class="nav-hint">Next →</span>
        <span class="nav-label">Alliances</span>
      </router-link>
    </div>
  </div>
</template>
