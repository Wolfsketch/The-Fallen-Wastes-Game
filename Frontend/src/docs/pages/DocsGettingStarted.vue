<template>
  <div>
    <div class="breadcrumb">
      <a href="#">Docs</a> <span class="sep">›</span> <span>Getting Started</span>
    </div>

    <h1>INTRODUCTION <span class="tag">GETTING STARTED</span></h1>
    <p class="page-lead">
      Welcome to The Fallen Wastes — a post-apocalyptic browser strategy game where you build
      a settlement, raise an army, and fight for dominance in a shattered world.
    </p>

    <!-- WHAT IS TFW -->
    <h2 id="what-is">What is The Fallen Wastes</h2>
    <p>
      The Fallen Wastes is a real-time strategy game set in a crumbling post-collapse civilization.
      You manage a <strong class="hl">Settlement</strong> — your home base — while raiding neutral
      Points of Interest on the Wasteland map for salvage, competing with other players for
      territory, and climbing the global ranking.
    </p>

    <div class="card-grid">
      <div class="data-card">
        <div class="data-card-header">
          <div class="data-card-icon">🏚️</div>
          <div>
            <div class="data-card-title">SETTLEMENT</div>
            <div class="data-card-sub">Your home base</div>
          </div>
        </div>
        <div class="data-card-body">
          Upgrade buildings, manage population, research technologies, and train your army.
          Everything starts and ends here.
        </div>
      </div>
      <div class="data-card">
        <div class="data-card-header">
          <div class="data-card-icon">🗺️</div>
          <div>
            <div class="data-card-title">WASTELAND</div>
            <div class="data-card-sub">The open world</div>
          </div>
        </div>
        <div class="data-card-body">
          Explore Points of Interest to raid for salvage. Fight NPC defenders, loot rare tech,
          and watch out for other players on the map.
        </div>
      </div>
      <div class="data-card">
        <div class="data-card-header">
          <div class="data-card-icon">⚔️</div>
          <div>
            <div class="data-card-title">COMBAT</div>
            <div class="data-card-sub">Three damage types</div>
          </div>
        </div>
        <div class="data-card-body">
          Ballistic, Impact, and Energy damage each interact differently with enemy defenses.
          Mixing unit types gives you a decisive edge.
        </div>
      </div>
      <div class="data-card">
        <div class="data-card-header">
          <div class="data-card-icon">🔬</div>
          <div>
            <div class="data-card-title">RARETECH</div>
            <div class="data-card-sub">The premium resource</div>
          </div>
        </div>
        <div class="data-card-body">
          RareTech is the lifeblood of late-game progression. Earn it by salvaging POI loot,
          then spend it on research and elite units.
        </div>
      </div>
    </div>

    <!-- CORE GAME LOOP -->
    <h2 id="game-loop">Core Game Loop</h2>
    <p>
      Every session revolves around the same repeating cycle:
    </p>

    <table class="stat-table">
      <thead>
        <tr>
          <th>#</th>
          <th>Action</th>
          <th>Result</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td class="val">1</td>
          <td>Upgrade production buildings</td>
          <td>More resources per hour</td>
        </tr>
        <tr>
          <td class="val">2</td>
          <td>Train infantry and vehicles</td>
          <td>Larger, stronger army</td>
        </tr>
        <tr>
          <td class="val">3</td>
          <td>Send raids to Wasteland POIs</td>
          <td>Collect salvage items</td>
        </tr>
        <tr>
          <td class="val">4</td>
          <td>Process salvage in Tech Salvager</td>
          <td>Earn RareTech</td>
        </tr>
        <tr>
          <td class="val">5</td>
          <td>Spend RareTech on research</td>
          <td>Unlock new capabilities</td>
        </tr>
        <tr>
          <td class="val">6</td>
          <td>Scout and raid other players / POIs</td>
          <td>Gain resources &amp; ranking</td>
        </tr>
      </tbody>
    </table>

    <!-- NEW PLAYER GUIDE -->
    <h2 id="new-player">New Player Guide</h2>
    <p>When you first start you will have:</p>

    <div class="info-box success">
      <div class="info-label">✔ STARTER BUILDINGS</div>
      HeadQuarter (L1), Shelter (L1), FoodSilo (L1), WaterTank (L1), ScrapVault (L1),
      FuelDepot (L1), PowerBank (L1), TechVault (L1) — plus all resource production buildings
      at level 0.
    </div>

    <p>
      <strong class="hl">Step 1 — Upgrade all five resource production buildings to level 3.</strong>
      This unlocks your Barracks. Without production buildings you have no income.
    </p>
    <p>
      <strong class="hl">Step 2 — Build your Barracks and train a small army of Scavengers and Raiders.</strong>
      These are cheap soldiers perfect for early POI raids.
    </p>
    <p>
      <strong class="hl">Step 3 — Raid your first POI.</strong>
      Open the Wasteland map, find a nearby Point of Interest, and send your army to raid it.
      Return with salvage loot.
    </p>
    <p>
      <strong class="hl">Step 4 — Research TechLab.</strong>
      Unlock the TechLab at HQ 3 to start your research progression and unlock the Tech Salvager.
    </p>
    <p>
      <strong class="hl">Step 5 — Process salvage and unlock the Tech Salvager.</strong>
      Salvage items from raids turn into RareTech when processed in the Tech Salvager.
    </p>

    <!-- TIPS -->
    <h2 id="tips">Beginner Tips</h2>

    <div class="info-box">
      <div class="info-label">☢ TIP — Balance production and storage</div>
      If your storage is full, your production buildings produce nothing. Always keep storage
      capacity ahead of production output.
    </div>

    <div class="info-box warning">
      <div class="info-label">⚠ TIP — Watch your population</div>
      Every building level and every unit trained consumes population from your Shelter.
      If you hit the cap, you cannot train more units. Upgrade your Shelter regularly.
    </div>

    <div class="info-box">
      <div class="info-label">☢ TIP — Scout before you raid</div>
      Use a Scout operation to check the defenders and loot at a POI before committing your full army.
      Scouting is fast and costs very few units compared to a failed raid.
    </div>

    <div class="info-box danger">
      <div class="info-label">⛔ TIP — Don't leave armies out overnight</div>
      Operations in progress can be recalled at any time. Recall your forces when you log off
      to keep them safe and let the loot be processed.
    </div>

    <div class="page-nav">
      <span></span>
      <router-link to="/buildings" style="text-align:right">
        <span class="nav-hint">Next →</span>
        <span class="nav-label">Buildings</span>
      </router-link>
    </div>
  </div>
</template>
