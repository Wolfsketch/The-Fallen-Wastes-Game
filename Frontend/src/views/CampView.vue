<template>
  <div class="fade-in">
    <div class="page-header">
      <h2 class="page-title">CAMP</h2>
      <span class="page-subtitle">COMMAND OVERVIEW</span>
    </div>
    <div class="accent-line" style="margin-bottom: 20px;" />

    <!-- Row 1: Settlement Info + Operational Status -->
    <div class="camp-grid">
      <!-- Settlement info + rename -->
      <div class="panel">
        <div class="panel-accent" />
        <div class="panel-body">
          <div class="panel-title"><span class="panel-dot" /> OUTPOST STATUS</div>

          <div class="settle-name-row">
            <div class="outpost-img-wrap" @click="showOutpostModal = true">
              <img src="../images/Settlement/Settlement start.png" class="outpost-img" alt="outpost" />
              <span class="outpost-zoom-hint">&#128269;</span>
            </div>
            <span v-if="!renaming" class="settle-name">{{ settlement?.name }}</span>
            <input v-else ref="renameInput" v-model="renameValue" class="rename-input"
              maxlength="40" @keyup.enter="submitRename" @keyup.escape="renaming=false" />
            <button v-if="!renaming" class="rename-btn" @click="startRename" title="Rename settlement">✏</button>
            <div v-else class="rename-actions">
              <button class="rename-confirm" @click="submitRename">✓</button>
              <button class="rename-cancel" @click="renaming=false">✕</button>
            </div>
          </div>
          <div v-if="renameError" class="rename-error">{{ renameError }}</div>

          <div class="settle-meta-grid">
            <div class="settle-meta-item">
              <div class="smilabel">POPULATION</div>
              <div class="smivalue">{{ settlement?.usedPopulation ?? '—' }} / {{ settlement?.populationCapacity ?? '—' }}</div>
            </div>
            <div class="settle-meta-item">
              <div class="smilabel">FREE POP</div>
              <div class="smivalue" :style="{color: (settlement?.availablePopulation ?? 0) < 10 ? 'var(--amber)' : 'var(--text)'}">
                {{ settlement?.availablePopulation ?? '—' }}
              </div>
            </div>
            <div class="settle-meta-item">
              <div class="smilabel">MORALE</div>
              <div class="smivalue" :style="{color: moralColor}">{{ settlement?.morale ?? '—' }}%</div>
            </div>
            <div class="settle-meta-item">
              <div class="smilabel">BUILDINGS</div>
              <div class="smivalue">{{ settlement?.buildingCount ?? 0 }}</div>
            </div>
            <div class="settle-meta-item">
              <div class="smilabel">UNITS</div>
              <div class="smivalue">{{ totalUnits }}</div>
            </div>
          </div>
        </div>
      </div>

      <!-- Military / score status -->
      <div class="panel">
        <div class="panel-accent" />
        <div class="panel-body">
          <div class="panel-title"><span class="panel-dot" /> OPERATIONAL STATUS</div>
          <div class="stats-grid">
            <div class="stat-item">
              <div class="stat-label">Score</div>
              <div class="stat-value" style="color:var(--cyan)">{{ player?.score ?? 0 }}</div>
            </div>
            <div class="stat-item">
              <div class="stat-label">Attack Score</div>
              <div class="stat-value" style="color:var(--orange, #ff9040)">{{ player?.attackScore ?? 0 }}</div>
            </div>
            <div class="stat-item">
              <div class="stat-label">Defense Score</div>
              <div class="stat-value" style="color:var(--green)">{{ player?.defenseScore ?? 0 }}</div>
            </div>
            <div class="stat-item">
              <div class="stat-label">War Score</div>
              <div class="stat-value" style="color:var(--amber)">{{ player?.warScore ?? 0 }}</div>
            </div>
            <div class="stat-item">
              <div class="stat-label">Triumph Points</div>
              <div class="stat-value" style="color:#d4af37">{{ player?.triumphPoints ?? 0 }}</div>
            </div>
            <div class="stat-item">
              <div class="stat-label">Available BP</div>
              <div class="stat-value" style="color:#e07b39">{{ player?.availableWarPoints ?? 0 }}</div>
            </div>
            <div class="stat-item">
              <div class="stat-label">Settlements</div>
              <div class="stat-value" style="color:var(--text)">{{ player?.settlements?.length ?? 1 }} / {{ player?.maxSettlements ?? 1 }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Resources row -->
    <div class="panel" style="margin-top:14px">
      <div class="panel-accent" />
      <div class="panel-body">
        <div class="panel-title"><span class="panel-dot" /> RESOURCE STOCKPILE</div>
        <div class="res-grid">
          <div class="res-item">
            <img :src="waterResIcon" class="res-icon" alt="Water" />
            <div class="res-label">WATER</div>
            <div class="res-value" :style="{color: (settlement?.water ?? 0) < 200 ? 'var(--red)' : 'var(--text)'}">
              {{ (settlement?.water ?? 0).toLocaleString() }}
            </div>
          </div>
          <div class="res-item">
            <img :src="foodResIcon" class="res-icon" alt="Food" />
            <div class="res-label">FOOD</div>
            <div class="res-value">{{ (settlement?.food ?? 0).toLocaleString() }}</div>
          </div>
          <div class="res-item">
            <img :src="scrapResIcon" class="res-icon" alt="Scrap" />
            <div class="res-label">SCRAP</div>
            <div class="res-value">{{ (settlement?.scrap ?? 0).toLocaleString() }}</div>
          </div>
          <div class="res-item">
            <img :src="fuelResIcon" class="res-icon" alt="Fuel" />
            <div class="res-label">FUEL</div>
            <div class="res-value">{{ (settlement?.fuel ?? 0).toLocaleString() }}</div>
          </div>
          <div class="res-item">
            <img :src="energyResIcon" class="res-icon" alt="Energy" />
            <div class="res-label">ENERGY</div>
            <div class="res-value">{{ (settlement?.energy ?? 0).toLocaleString() }}</div>
          </div>
          <div class="res-item">
            <img :src="rareTechResIcon" class="res-icon" alt="RareTech" />
            <div class="res-label">RARETECH</div>
            <div class="res-value" style="color:#d4af37">{{ (settlement?.rareTech ?? 0).toLocaleString() }}</div>
          </div>
        </div>
      </div>
    </div>

    <!-- Row 2: War Raids -->
    <div class="panel" style="margin-top:14px">
      <div class="panel-accent" style="background:#e07b39" />
      <div class="panel-body">
        <div class="panel-title"><span class="panel-dot" style="background:#e07b39;box-shadow:0 0 6px #e07b39" /> WAR RAIDS</div>
        <div class="war-raid-section">
          <div class="war-raid-info">
            <div class="war-raid-desc">
              Spend <strong>300 BP</strong> to organize a War Raid. Your battle points (BP) are consumed but your ranking scores are never affected.
            </div>
            <div class="war-raid-bp">
              <span class="wr-label">Available BP: </span>
              <span class="wr-val" :style="{ color: (player?.availableWarPoints ?? 0) >= 300 ? '#e07b39' : 'var(--muted)' }">
                {{ player?.availableWarPoints ?? 0 }}
              </span>
              <span class="wr-sep"> / </span>
              <span class="wr-cost">300 BP</span>
            </div>
          </div>
          <div class="war-raid-action">
            <button
              class="war-raid-btn"
              :disabled="(player?.availableWarPoints ?? 0) < 300 || triumphPending"
              @click="submitWarRaid"
            >
              {{ triumphPending ? 'ORGANIZING...' : '⚔ ORGANIZE WAR RAID' }}
            </button>
            <div v-if="triumphError" class="war-raid-error">{{ triumphError }}</div>
            <div v-if="triumphSuccess" class="war-raid-success">{{ triumphSuccess }}</div>
          </div>
        </div>
      </div>
    </div>

    <!-- Row 3: Conquest Progress -->
    <div class="panel" style="margin-top:14px">
      <div class="panel-accent" />
      <div class="panel-body">
        <div class="panel-title"><span class="panel-dot" style="background:#d4af37;box-shadow:0 0 6px #d4af37" /> CONQUEST — EXPANSION LEVEL {{ player?.conquestLevel ?? 1 }}</div>
        <div class="conquest-row">
          <div class="conquest-info">
            <div class="conquest-desc">
              Earn <strong>Triumph Points</strong> by winning battles. Each conquest level unlocks a new outpost slot.
            </div>
            <div class="conquest-tp">
              <span class="tp-current">{{ player?.triumphPoints ?? 0 }}</span>
              <span class="tp-sep"> / </span>
              <span class="tp-needed">{{ player?.triumphPointsForNextLevel ?? 3 }}</span>
              <span class="tp-label"> TP</span>
            </div>
          </div>
          <div class="conquest-bar-wrap">
            <div class="conquest-bar">
              <div class="conquest-fill" :style="{width: conquestPercent + '%'}" />
            </div>
            <div class="conquest-sublabel">→ Level {{ (player?.conquestLevel ?? 1) + 1 }} unlocks outpost {{ (player?.conquestLevel ?? 1) + 1 }}</div>
          </div>
        </div>
        <div class="conquest-level-list">
          <div v-for="lvl in conquestLevelRange" :key="lvl" class="conquest-level-badge"
            :class="{ 'clb--achieved': (player?.conquestLevel ?? 1) >= lvl }">
            <span class="clb-lvl">L{{ lvl }}</span>
            <span class="clb-tp">{{ conquestTpRequired(lvl) }} TP</span>
            <span class="clb-unlock">{{ lvl }} outpost{{ lvl !== 1 ? 's' : '' }}</span>
          </div>
        </div>
        <div v-if="canFoundNewSettlement" class="found-settlement-section">
          <div class="found-ready">🏗 CONQUEST LEVEL {{ player?.conquestLevel }} — New outpost slot available!</div>
          <div class="found-hint">Go to the Wasteland Map, select an unclaimed sector, and click <strong>Claim</strong> to found your outpost there.</div>
          <button class="found-btn" @click="$router.push({ name: 'wasteland' })">
            🗺 OPEN WASTELAND MAP
          </button>
        </div>
      </div>
    </div>

    <!-- Row 3: Operations Log -->
    <div class="panel" style="margin-top:14px">
      <div class="panel-accent" />
      <div class="panel-body">
        <div class="panel-title"><span class="panel-dot" /> OPERATIONS LOG</div>
        <div v-if="logLoading" class="log-empty">Loading...</div>
        <div v-else-if="logEntries.length === 0" class="log-empty">No activity recorded yet.</div>
        <template v-else>
          <div class="log-list">
            <div v-for="(entry, i) in pagedLogEntries" :key="i" class="log-entry">
              <span class="log-time">{{ entry.time }}</span>
              <span class="tag" :class="'tag--' + entry.type">{{ entry.tag }}</span>
              <span class="log-msg" :style="{ color: entry.color }">{{ entry.msg }}</span>
            </div>
          </div>
          <div v-if="logPageCount > 1" class="log-pagination">
            <button class="log-pg-btn" :disabled="logPage === 0" @click="logPage--">&#8249; Prev</button>
            <span class="log-pg-info">Page {{ logPage + 1 }} / {{ logPageCount }}</span>
            <button class="log-pg-btn" :disabled="logPage >= logPageCount - 1" @click="logPage++">Next &#8250;</button>
          </div>
        </template>
      </div>
    </div>
  </div>
  <!-- Outpost image modal -->
  <Teleport to="body">
    <div v-if="showOutpostModal" class="outpost-modal-overlay" @click="showOutpostModal = false">
      <div class="outpost-modal-box" @click.stop>
        <button class="outpost-modal-close" @click="showOutpostModal = false">&#10005;</button>
        <img src="../images/Settlement/Settlement start.png" class="outpost-modal-img" alt="Settlement" />
      </div>
    </div>
  </Teleport>
</template>

<script setup>
import { ref, computed, onMounted, nextTick } from 'vue'
import { renameSettlement, getPlayerReports, getPlayerById, organizeTriumph } from '../services/api.js'
import waterResIcon from '../images/Resources/Water.png'
import foodResIcon from '../images/Resources/Food.png'
import scrapResIcon from '../images/Resources/Scrap.png'
import fuelResIcon from '../images/Resources/Fuel.png'
import energyResIcon from '../images/Resources/Energy.png'
import rareTechResIcon from '../images/Resources/RareTech.png'

const props = defineProps({
  player: Object,
  settlement: Object,
  refreshSettlement: Function
})

// ── Rename ──────────────────────────────────────────────────
const showOutpostModal = ref(false)
const renaming = ref(false)
const renameValue = ref('')
const renameInput = ref(null)
const renameError = ref('')

function startRename() {
  renameValue.value = props.settlement?.name ?? ''
  renameError.value = ''
  renaming.value = true
  nextTick(() => renameInput.value?.focus())
}

async function submitRename() {
  renameError.value = ''
  const name = renameValue.value.trim()
  if (name.length < 3) { renameError.value = 'Name must be at least 3 characters.'; return }
  try {
    await renameSettlement(props.settlement.id, name, props.player.id)
    renaming.value = false
    if (props.refreshSettlement) await props.refreshSettlement()
  } catch (e) {
    renameError.value = e?.response?.data || 'Could not rename.'
  }
}

// ── Found new settlement ─────────────────────────────────────
const canFoundNewSettlement = computed(() =>
  (props.player?.settlements?.length ?? 1) < (props.player?.maxSettlements ?? 1)
)

// ── War Raid (Triumph) ───────────────────────────────────────
const triumphPending = ref(false)
const triumphError = ref('')
const triumphSuccess = ref('')

async function submitWarRaid() {
  triumphError.value = ''
  triumphSuccess.value = ''
  triumphPending.value = true
  try {
    await organizeTriumph(props.player.id)
    triumphSuccess.value = 'War Raid organized! −300 BP'
    if (props.refreshSettlement) await props.refreshSettlement()
  } catch (e) {
    const msg = e?.response?.data?.message || e?.response?.data || 'Could not organize War Raid.'
    triumphError.value = typeof msg === 'string' ? msg : JSON.stringify(msg)
  } finally {
    triumphPending.value = false
  }
}

// ── Computed helpers ─────────────────────────────────────────
const moralColor = computed(() => {
  const m = props.settlement?.morale ?? 100
  if (m >= 70) return 'var(--green)'
  if (m >= 40) return 'var(--amber)'
  return 'var(--red)'
})

const totalUnits = computed(() => {
  const inv = props.settlement?.unitInventory
  if (!inv) return 0
  return Object.values(inv).reduce((s, v) => s + v, 0)
})

const conquestLevelRange = computed(() => {
  const max = Math.max(6, (props.player?.conquestLevel ?? 1) + 2)
  return Array.from({ length: max }, (_, i) => i + 1)
})

const conquestPercent = computed(() => {
  const tp = props.player?.triumphPoints ?? 0
  const needed = props.player?.triumphPointsForNextLevel ?? 3
  if (needed <= 0) return 100
  const prevNeeded = conquestTpRequired(props.player?.conquestLevel ?? 1)
  const range = needed - prevNeeded
  if (range <= 0) return 100
  return Math.min(100, Math.max(0, Math.round(((tp - prevNeeded) / range) * 100)))
})

function conquestTpRequired(level) {
  if (level <= 1) return 0
  return Math.round(1.5 * (level - 1) * level)
}

// ── Operations log ────────────────────────────────────────────
const LOG_PAGE_SIZE = 20
const logPage = ref(0)
const logEntries = ref([])
const logLoading = ref(true)
const logPageCount = computed(() => Math.max(1, Math.ceil(logEntries.value.length / LOG_PAGE_SIZE)))
const pagedLogEntries = computed(() =>
  logEntries.value.slice(logPage.value * LOG_PAGE_SIZE, (logPage.value + 1) * LOG_PAGE_SIZE)
)

function parseLogEntry(msg) {
  const time = new Date(msg.sentAtUtc).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' })
  const date = new Date(msg.sentAtUtc).toLocaleDateString('en-GB', { day:'2-digit', month:'2-digit' })
  const label = `${date} ${time}`

  // Try to parse JSON body
  let body = null
  try { body = JSON.parse(msg.body) } catch { /* ignore */ }

  if (body?.isSettlementBattleReport) {
    const won = body.attackerWins
    const isDefense = body.isDefenseReport
    const other = isDefense ? (body.attackerSettlementName ?? 'enemy') : (body.defenderSettlementName ?? 'target')
    if (isDefense) {
      return { time: label, tag: won ? 'DEFEAT' : 'REPELLED', type: won ? 'red' : 'cyan',
        msg: won ? `Settlement raided by ${other}` : `Attack repelled from ${other}`, color: won ? 'var(--red)' : 'var(--green)' }
    } else {
      return { time: label, tag: won ? 'VICTORY' : 'DEFEAT', type: won ? 'cyan' : 'red',
        msg: won ? `Raid victory on ${other}` : `Attack failed on ${other}`, color: won ? 'var(--green)' : 'var(--red)' }
    }
  }
  if (body?.isScoutReport) {
    return { time: label, tag: 'SCOUT', type: 'muted',
      msg: `Scouted ${body.poiName ?? 'POI'} — Tier ${body.tier ?? '?'}`, color: 'var(--text)' }
  }
  if (body?.attackerWins !== undefined) {
    const won = body.attackerWins
    return { time: label, tag: won ? 'RAID' : 'FAIL', type: won ? 'cyan' : 'red',
      msg: `Battle at ${body.poiName ?? 'POI'} — ${won ? 'Victory' : 'Defeated'}`, color: won ? 'var(--green)' : 'var(--red)' }
  }

  return { time: label, tag: 'MSG', type: 'muted', msg: msg.subject ?? '—', color: 'var(--text)' }
}

async function loadLog() {
  logLoading.value = true
  try {
    const reports = await getPlayerReports(props.player?.id)
    const entries = (reports ?? [])
      .map(parseLogEntry)

    // Always add account creation entry at the bottom
    const created = props.player?.createdAtUtc
    if (created) {
      const t = new Date(created)
      const label = `${t.toLocaleDateString('en-GB', { day:'2-digit', month:'2-digit' })} ${t.toLocaleTimeString('en-GB', { hour:'2-digit', minute:'2-digit' })}`
      entries.push({ time: label, tag: 'AUTH', type: 'muted',
        msg: `Operator ${props.player?.username} authenticated`, color: 'var(--muted)' })
      entries.push({ time: label, tag: 'NEW', type: 'cyan',
        msg: `Outpost "${props.settlement?.name}" established`, color: 'var(--bright)' })
    }

    logEntries.value = entries
  } catch {
    logEntries.value = []
  } finally {
    logLoading.value = false
  }
}

onMounted(() => {
  if (props.player?.id) loadLog()
})
</script>

<style scoped>
.page-header { display:flex;align-items:baseline;gap:12px;margin-bottom:4px }
.page-title { font-family:var(--ff-title);font-size:16px;color:var(--cyan);letter-spacing:3px;font-weight:700;text-shadow:0 0 10px rgba(0,212,255,.12);text-transform:uppercase }
.page-subtitle { font-size:8px;color:var(--cyan-dim);letter-spacing:2px;font-family:var(--ff-title) }

.camp-grid { display:grid;grid-template-columns:1fr 1fr;gap:14px }

.panel { background:var(--bg2);border:1px solid var(--border);position:relative;overflow:hidden }
.panel-accent { position:absolute;top:0;left:0;right:0;height:1px;background:linear-gradient(90deg,var(--cyan),transparent);opacity:.4 }
.panel-body { padding:20px }
.panel-title { font-size:9px;color:var(--cyan);text-transform:uppercase;letter-spacing:3px;font-weight:700;font-family:var(--ff-title);margin-bottom:14px;display:flex;align-items:center;gap:8px }
.panel-dot { width:6px;height:6px;background:var(--cyan);box-shadow:0 0 6px var(--cyan);display:inline-block;flex-shrink:0 }

/* Settlement name + rename */
.settle-name-row { display:flex;align-items:center;gap:10px;margin-bottom:14px }
.outpost-img-wrap { width:70px;height:70px;flex-shrink:0;display:flex;align-items:center;justify-content:center;position:relative;cursor:pointer; }
.outpost-img { width:70px;height:70px;object-fit:contain; }
.outpost-zoom-hint { position:absolute;inset:0;display:flex;align-items:center;justify-content:center;font-size:20px;background:rgba(0,0,0,.55);opacity:0;transition:opacity .15s }
.outpost-img-wrap:hover .outpost-zoom-hint { opacity:1 }
.settle-name { font-family:var(--ff-title);font-size:15px;color:var(--bright);font-weight:700;letter-spacing:1px }
.rename-input { background:rgba(0,212,255,.07);border:1px solid var(--cyan);color:var(--bright);font-family:var(--ff-title);font-size:14px;padding:3px 8px;flex:1;min-width:0;outline:none }
.rename-btn { background:none;border:none;color:var(--muted);cursor:pointer;font-size:12px;padding:2px 6px;transition:color .15s }
.rename-btn:hover { color:var(--cyan) }
.rename-actions { display:flex;gap:4px }
.rename-confirm,.rename-cancel { background:none;border:1px solid;cursor:pointer;padding:2px 8px;font-size:11px }
.rename-confirm { border-color:var(--green);color:var(--green) }
.rename-cancel { border-color:var(--muted);color:var(--muted) }
.rename-error { font-size:10px;color:var(--red);margin:-10px 0 10px }

.settle-meta-grid { display:grid;grid-template-columns:1fr 1fr 1fr;gap:10px }
.settle-meta-item { background:rgba(0,212,255,.03);border:1px solid rgba(0,212,255,.08);padding:8px 10px }
.smilabel { font-size:7px;color:var(--muted);letter-spacing:1.5px;text-transform:uppercase;margin-bottom:3px }
.smivalue { font-family:var(--ff-title);font-size:14px;color:var(--text);font-weight:700 }

/* Stats */
.stats-grid { display:grid;grid-template-columns:1fr 1fr;gap:10px }
.stat-item { padding:8px 0 }
.stat-label { font-size:8px;color:var(--muted);text-transform:uppercase;letter-spacing:1.5px;font-weight:700 }
.stat-value { font-size:18px;font-family:var(--ff-title);font-weight:700;letter-spacing:1px }

/* War Raids */
.war-raid-section { display:flex;gap:24px;align-items:flex-start;flex-wrap:wrap }
.war-raid-info { flex:1;min-width:0 }
.war-raid-desc { font-size:10px;color:var(--muted);margin-bottom:10px;line-height:1.6 }
.war-raid-desc strong { color:var(--text) }
.war-raid-gp { font-family:var(--ff-title);font-size:15px }
.wr-label { font-size:9px;color:var(--muted);text-transform:uppercase;letter-spacing:1px }
.wr-val { font-size:22px;font-weight:700 }
.wr-sep,.wr-cost { color:var(--muted);font-size:14px }
.war-raid-action { display:flex;flex-direction:column;gap:8px;align-items:flex-start }
.war-raid-btn { padding:10px 22px;background:rgba(224,123,57,.12);border:1px solid #e07b39;color:#e07b39;font-family:var(--ff-title);font-size:11px;letter-spacing:2px;cursor:pointer;text-transform:uppercase;transition:background .2s }
.war-raid-btn:hover:not(:disabled) { background:rgba(224,123,57,.25) }
.war-raid-btn:disabled { opacity:.4;cursor:not-allowed }
.war-raid-error { font-size:10px;color:var(--red) }
.war-raid-success { font-size:10px;color:var(--green) }

/* Conquest */
.conquest-row { display:flex;gap:24px;align-items:flex-start;margin-bottom:14px }
.conquest-info { flex:1;min-width:0 }
.conquest-desc { font-size:10px;color:var(--muted);margin-bottom:8px;line-height:1.6 }
.conquest-desc strong { color:var(--text) }
.conquest-tp { font-family:var(--ff-title);font-size:16px;color:var(--text) }
.tp-current { color:#d4af37;font-size:22px }
.tp-sep,.tp-label { color:var(--muted) }
.tp-needed { color:var(--text) }
.conquest-bar-wrap { flex:1;min-width:0 }
.conquest-bar { height:6px;background:rgba(0,212,255,.1);border:1px solid rgba(0,212,255,.15);margin-bottom:6px }
.conquest-fill { height:100%;background:linear-gradient(90deg,#d4af37,#ffd700);transition:width .4s ease }
.conquest-sublabel { font-size:9px;color:var(--muted);letter-spacing:1px }
.conquest-level-list { display:flex;gap:8px;flex-wrap:wrap;margin-top:4px }
.conquest-level-badge { display:flex;flex-direction:column;align-items:center;padding:6px 10px;border:1px solid rgba(0,212,255,.12);background:rgba(0,212,255,.03);min-width:60px;opacity:.5;transition:opacity .2s }
.clb--achieved { border-color:rgba(212,175,55,.4);background:rgba(212,175,55,.06);opacity:1 }
.clb-lvl { font-family:var(--ff-title);font-size:10px;color:#d4af37;font-weight:700 }
.clb-tp { font-size:9px;color:var(--muted);margin-top:2px }
.clb-unlock { font-size:8px;color:var(--text);margin-top:2px }
.found-settlement-section { margin-top:16px;padding:12px 14px;border:1px solid rgba(212,175,55,.3);background:rgba(212,175,55,.04) }
.found-ready { font-size:10px;color:#d4af37;letter-spacing:2px;font-family:var(--ff-title);margin-bottom:8px }
.found-hint { font-size:11px;color:var(--muted);margin-bottom:12px;line-height:1.5 }
.found-hint strong { color:var(--bright) }
.found-btn { background:rgba(212,175,55,.15);border:1px solid rgba(212,175,55,.5);color:#d4af37;font-family:var(--ff-title);font-size:9px;letter-spacing:1.5px;padding:7px 18px;cursor:pointer;font-weight:700 }
.found-btn:hover { background:rgba(212,175,55,.25) }

/* Resources */
.res-grid { display:grid;grid-template-columns:repeat(6,1fr);gap:10px }
.res-item { background:rgba(0,212,255,.03);border:1px solid rgba(0,212,255,.08);padding:10px;text-align:center }
.res-icon { width:90px;height:90px;object-fit:contain;margin-bottom:4px }
.res-label { font-size:7px;color:var(--muted);letter-spacing:1.5px;text-transform:uppercase;margin-bottom:4px }
.res-value { font-family:var(--ff-title);font-size:14px;color:var(--text);font-weight:700 }

/* Log */
.log-list { display:flex;flex-direction:column;gap:2px;max-height:400px;overflow-y:auto;padding-right:4px }
.log-list::-webkit-scrollbar { width:4px }
.log-list::-webkit-scrollbar-track { background:rgba(0,212,255,.05) }
.log-list::-webkit-scrollbar-thumb { background:rgba(0,212,255,.25);border-radius:2px }
.log-pagination { display:flex;align-items:center;justify-content:center;gap:14px;margin-top:10px;padding-top:8px;border-top:1px solid rgba(0,212,255,.08) }
.log-pg-btn { background:rgba(0,212,255,.06);border:1px solid rgba(0,212,255,.2);color:var(--cyan);font-family:var(--ff-title);font-size:9px;letter-spacing:1px;padding:4px 10px;cursor:pointer }
.log-pg-btn:disabled { opacity:.3;cursor:default }
.log-pg-info { font-size:9px;color:var(--muted);letter-spacing:1px }
.log-empty { font-size:11px;color:var(--muted);padding:4px 0 }
.log-entry { font-size:10px;line-height:2.6;display:flex;align-items:center;gap:10px;border-bottom:1px solid rgba(0,212,255,.04) }
.log-time { font-family:var(--ff-title);font-size:9px;color:var(--cyan-dim);letter-spacing:1px;min-width:82px }
.log-msg { color:var(--text) }

/* Tags */
.tag { font-family:var(--ff-title);font-size:7px;letter-spacing:1px;padding:2px 5px;font-weight:700;border:1px solid }
.tag--cyan { color:var(--cyan);border-color:rgba(0,212,255,.3);background:rgba(0,212,255,.06) }
.tag--green { color:var(--green);border-color:rgba(0,255,120,.3);background:rgba(0,255,120,.06) }
.tag--amber { color:var(--amber);border-color:rgba(255,184,0,.3);background:rgba(255,184,0,.06) }
.tag--red { color:var(--red);border-color:rgba(255,60,60,.3);background:rgba(255,60,60,.06) }
.tag--muted { color:var(--muted);border-color:rgba(150,150,150,.2);background:rgba(150,150,150,.04) }

/* Outpost image modal */
.outpost-modal-overlay {
  position:fixed;inset:0;background:rgba(0,0,0,.82);display:flex;align-items:center;justify-content:center;z-index:9999;cursor:pointer;
}
.outpost-modal-box {
  position:relative;background:var(--bg2,#111);border:1px solid rgba(0,212,255,.35);border-radius:8px;padding:32px;cursor:default;box-shadow:0 8px 40px rgba(0,0,0,.9);
}
.outpost-modal-img {
  display:block;width:320px;height:320px;object-fit:contain;image-rendering:pixelated;
}
.outpost-modal-close {
  position:absolute;top:8px;right:10px;background:transparent;border:none;color:var(--muted,#666);font-size:14px;cursor:pointer;line-height:1;
}
.outpost-modal-close:hover { color:var(--cyan,#00d4ff) }
</style>
