<template>
  <div>
    <div class="breadcrumb">
      <a href="#">Docs</a> <span class="sep">›</span> <span>Operations</span>
    </div>

    <h1>OPERATIONS <span class="tag">MILITARY</span></h1>
    <p class="page-lead">
      Operations are real-time missions you send your units on from the Wasteland map.
      Each type has a distinct purpose, risk level, and reward. Armies travel in real time —
      plan for the return journey too.
    </p>

    <div class="info-box">
      <div class="info-label">☢ PHASES</div>
      Every operation has a <strong class="hl">travel phase</strong> (going), an
      <strong class="hl">at-target phase</strong> (arrived), and a
      <strong class="hl">return phase</strong> (coming home). You can recall your army at any time.
    </div>

    <!-- RAID -->
    <h2 id="raid">Raid POI</h2>
    <p>
      A Raid operation sends your army to attack a Point of Interest on the Wasteland map.
      The POI is defended by NPC units. If your forces win, you can take loot items.
    </p>

    <table class="stat-table">
      <thead>
        <tr><th>Aspect</th><th>Detail</th></tr>
      </thead>
      <tbody>
        <tr><td class="val">Target</td><td>Any non-cleared Wasteland POI</td></tr>
        <tr><td class="val">Combat</td><td>Your army fights NPC defenders</td></tr>
        <tr><td class="val">Loot</td><td>Salvage items, limited by unit carry capacity</td></tr>
        <tr><td class="val">Carry limit</td><td>Sum of all unit CARRY values in the operation</td></tr>
        <tr><td class="val">Item limit</td><td>Available items in the POI at arrival</td></tr>
        <tr><td class="val">Losses</td><td>Units that die in combat are permanently lost</td></tr>
        <tr><td class="val">POI cleared</td><td>When all NPC units AND all loot items are gone</td></tr>
      </tbody>
    </table>

    <div class="info-box warning">
      <div class="info-label">⚠ CARRY CAPACITY MATTERS</div>
      Each unit type has a <strong class="hl">CARRY</strong> stat. If the total carry capacity of
      your raid force is less than the number of loot items at the POI, you won't be able to take
      all of them in a single trip. A follow-up raid can collect the remainder.
    </div>

    <!-- SCOUT -->
    <h2 id="scout">Scout</h2>
    <p>
      Scouting is a low-risk reconnaissance operation. Your scouts fly to the target, assess the
      situation, and return with a report — no combat occurs.
    </p>

    <table class="stat-table">
      <thead>
        <tr><th>Aspect</th><th>Detail</th></tr>
      </thead>
      <tbody>
        <tr><td class="val">Target</td><td>Any non-cleared POI or enemy settlement</td></tr>
        <tr><td class="val">Combat</td><td>None — purely recon</td></tr>
        <tr><td class="val">Report</td><td>NPC count, loot items present, enemy army strength</td></tr>
        <tr><td class="val">Cost</td><td>Small amount of Vault RareTech per scout</td></tr>
        <tr><td class="val">Speed</td><td>Scouts travel at a fixed high speed</td></tr>
        <tr><td class="val">Block</td><td>Cannot scout a cleared POI — wait for respawn</td></tr>
      </tbody>
    </table>

    <div class="info-box">
      <div class="info-label">☢ SCOUT REPORT</div>
      After a successful scout, a report appears in your Messages. The report shows
      unit counts by type, estimated defense strength, and available loot items.
    </div>

    <!-- SIEGE -->
    <h2 id="siege">Siege</h2>
    <p>
      A Siege is the most complex operation — it targets another player's settlement.
      You must breach their Perimeter Wall before engaging their garrison.
      A successful siege allows you to <strong class="hl">plunder resources</strong> or,
      if you follow it with a Convoy, <strong class="hl">claim the settlement</strong>.
    </p>

    <table class="stat-table">
      <thead>
        <tr><th>Phase</th><th>Description</th></tr>
      </thead>
      <tbody>
        <tr><td class="val">Travel</td><td>Army moves to the target settlement</td></tr>
        <tr><td class="val">Wall Breach</td><td>Attackers engage the Perimeter Wall; Impact units are most effective</td></tr>
        <tr><td class="val">Garrison Battle</td><td>Remaining attackers fight the defender's stationed units</td></tr>
        <tr><td class="val">Plunder</td><td>Victorious attackers take a portion of the settlement's resources</td></tr>
        <tr><td class="val">Retreat</td><td>Surviving attackers return home with the plunder</td></tr>
      </tbody>
    </table>

    <div class="info-box warning">
      <div class="info-label">⚠ SIEGE REQUIREMENTS</div>
      Siege operations require the <strong class="hl">Convoy Protocols</strong> research (Logistics branch, TechLab 5).
      Without it, you cannot initiate siege-type operations.
    </div>

    <div class="info-box danger">
      <div class="info-label">⛔ SETTLEMENT CONQUEST</div>
      To fully conquer and claim an enemy settlement after a successful siege, you must send a
      <strong class="hl">Convoy</strong> unit (trained at the Command Center) to arrive at the
      settlement <em>after</em> the breach. The original owner loses control permanently.
    </div>

    <!-- CONVOY -->
    <h2 id="convoy">Convoy</h2>
    <p>
      The Convoy unit has two distinct uses depending on its destination:
    </p>
    <div class="card-grid">
      <div class="data-card">
        <div class="data-card-header">
          <div class="data-card-icon">🏗️</div>
          <div>
            <div class="data-card-title">FOUND NEW SETTLEMENT</div>
            <div class="data-card-sub">Expansion</div>
          </div>
        </div>
        <div class="data-card-body">
          Send a Convoy to an unclaimed map tile to establish a brand-new settlement.
          Each player can manage multiple settlements simultaneously.
        </div>
      </div>
      <div class="data-card">
        <div class="data-card-header">
          <div class="data-card-icon">🎯</div>
          <div>
            <div class="data-card-title">CLAIM AFTER SIEGE</div>
            <div class="data-card-sub">Conquest</div>
          </div>
        </div>
        <div class="data-card-body">
          Send a Convoy to a recently besieged enemy settlement (wall breached, garrison defeated)
          to formally claim it as your own. The enemy loses the settlement permanently.
        </div>
      </div>
    </div>

    <!-- RECALL -->
    <h2 id="recall">Recall</h2>
    <p>
      Any active operation can be <strong class="hl">recalled</strong> at any time from the
      Wasteland view. When recalled:
    </p>
    <ul style="padding-left: 20px; font-size: 14px; margin-bottom: 16px;">
      <li>Units immediately turn around and start the return trip</li>
      <li>Any loot already collected by the raiding party is kept</li>
      <li>Loot items are removed from the POI and will arrive with your army</li>
      <li>The operation report appears in Messages upon arrival</li>
    </ul>

    <div class="info-box">
      <div class="info-label">☢ LOG OFF SAFELY</div>
      Always recall your armies before logging off for an extended period.
      Operations may be recalled without losing already-collected loot.
    </div>

    <div class="page-nav">
      <router-link to="/combat">
        <span class="nav-hint">← Previous</span>
        <span class="nav-label">Combat System</span>
      </router-link>
      <router-link to="/research" style="text-align:right">
        <span class="nav-hint">Next →</span>
        <span class="nav-label">Research</span>
      </router-link>
    </div>
  </div>
</template>
