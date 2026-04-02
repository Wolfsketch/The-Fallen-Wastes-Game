<template>
  <div class="report-view fade-in">
    <!-- Tabs -->
    <div class="report-tabs">
      <button
        v-for="tab in tabs" :key="tab.key"
        class="report-tab" :class="{ active: activeTab === tab.key }"
        @click="activeTab = tab.key"
      >
        {{ tab.label }}
        <span v-if="tab.key === 'mine' && myReports.length" class="tab-badge">{{ myReports.length }}</span>
      </button>
    </div>

    <!-- ── NEW REPORT ─────────────────────────────────────────────────── -->
    <div v-if="activeTab === 'new'">
      <div class="page-header">
        <h2 class="page-title">SUBMIT BUG REPORT</h2>
        <p class="page-subtitle">Help us improve The Fallen Wastes by reporting issues.</p>
      </div>
      <div class="accent-line" style="margin-bottom: 20px" />

      <div class="guidelines-box">
        <div class="guidelines-head">
          📋 REPORTING GUIDELINES
        </div>
        <ul class="guidelines-list">
          <li>Be specific — what you did, what happened, what you expected</li>
          <li>One bug per report</li>
          <li>Include steps to reproduce whenever possible</li>
          <li>Check Known Issues first</li>
        </ul>
      </div>

      <div v-if="submitSuccess" class="submit-success">
        <div class="success-check">✓</div>
        <h3>REPORT SUBMITTED</h3>
        <p>Track it in <strong>My Reports</strong>.</p>
        <button class="action-btn" @click="activeTab = 'mine'">VIEW MY REPORTS</button>
      </div>

      <form v-else class="report-form" @submit.prevent="submitReport">
        <div class="form-field">
          <label class="field-label">BUG TITLE <span class="req">*</span></label>
          <input v-model="form.title" class="field-input" placeholder="Brief description of the bug" maxlength="200" required />
        </div>

        <div class="form-row">
          <div class="form-field">
            <label class="field-label">CATEGORY <span class="req">*</span></label>
            <select v-model="form.category" class="field-input" required>
              <option value="">— Select —</option>
              <option v-for="c in CATEGORIES" :key="c" :value="c">{{ c }}</option>
            </select>
          </div>
          <div class="form-field">
            <label class="field-label">AREA <span class="req">*</span></label>
            <select v-model="form.area" class="field-input" required>
              <option value="">— Select —</option>
              <option v-for="a in AREAS" :key="a" :value="a">{{ a }}</option>
            </select>
          </div>
        </div>

        <div class="form-field">
          <label class="field-label">SEVERITY <span class="req">*</span></label>
          <div class="sev-row">
            <button
              v-for="s in SEVERITIES" :key="s.value" type="button"
              class="sev-btn" :class="[s.cls, { active: form.severity === s.value }]"
              @click="form.severity = s.value"
            >{{ s.label }}</button>
          </div>
          <div v-if="form.severity" class="sev-hint">{{ SEV_HINTS[form.severity] }}</div>
        </div>

        <div class="form-field">
          <label class="field-label">DESCRIPTION <span class="req">*</span></label>
          <textarea v-model="form.description" class="field-input field-textarea" placeholder="What happened? What did you expect?" rows="5" maxlength="5000" required></textarea>
          <div class="char-count">{{ form.description.length }} / 5000</div>
        </div>

        <div class="form-field">
          <label class="field-label">STEPS TO REPRODUCE</label>
          <textarea v-model="form.stepsToReproduce" class="field-input field-textarea" placeholder="1. Go to...&#10;2. Click on...&#10;3. Observe..." rows="4" maxlength="3000"></textarea>
        </div>

        <div class="form-row">
          <div class="form-field">
            <label class="field-label">SETTLEMENT NAME</label>
            <input v-model="form.settlementName" class="field-input" placeholder="Your settlement" maxlength="100" />
          </div>
          <div class="form-field">
            <label class="field-label">BROWSER / CLIENT</label>
            <input v-model="form.browser" class="field-input" placeholder="e.g. Chrome 124" maxlength="100" />
          </div>
        </div>

        <div class="form-actions">
          <button type="button" class="secondary-btn" @click="resetForm">CLEAR</button>
          <button type="submit" class="action-btn" :disabled="submitting">
            {{ submitting ? 'SUBMITTING...' : 'SUBMIT REPORT' }}
          </button>
        </div>
      </form>
    </div>

    <!-- ── MY REPORTS ─────────────────────────────────────────────────── -->
    <div v-if="activeTab === 'mine'">
      <div class="page-header" style="display:flex;align-items:center;justify-content:space-between">
        <div>
          <h2 class="page-title">MY REPORTS</h2>
          <p class="page-subtitle">Your submitted bug reports</p>
        </div>
        <button class="secondary-btn" @click="loadMyReports" :disabled="loadingMine">REFRESH</button>
      </div>
      <div class="accent-line" style="margin-bottom: 20px" />

      <div v-if="loadingMine" class="loading-msg">Loading...</div>

      <div v-else-if="myReports.length === 0" class="empty-panel">
        <div class="empty-icon">📋</div>
        <div class="empty-title">NO REPORTS YET</div>
        <div class="empty-sub">You haven't submitted any bug reports.</div>
        <button class="action-btn" @click="activeTab = 'new'">SUBMIT FIRST REPORT</button>
      </div>

      <div v-else class="reports-list">
        <div v-for="r in myReports" :key="r.id" class="report-card">
          <div class="rc-bar" :class="'rc-' + r.severity.toLowerCase()"></div>
          <div class="rc-body">
            <div class="rc-top">
              <span class="rc-title">{{ r.title }}</span>
              <span class="status-pill" :class="statusCls(r.status)">{{ r.status }}</span>
            </div>
            <div class="rc-meta">
              <span class="rc-tag">{{ r.category }}</span>
              <span class="rc-tag">{{ r.area }}</span>
              <span class="sev-pill" :class="'sev-pill-' + r.severity.toLowerCase()">{{ r.severity }}</span>
              <span class="rc-date">{{ fmtDate(r.createdAtUtc) }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ── KNOWN ISSUES ───────────────────────────────────────────────── -->
    <div v-if="activeTab === 'known'">
      <div class="page-header">
        <h2 class="page-title">KNOWN ISSUES</h2>
        <p class="page-subtitle">Issues we are aware of and actively tracking</p>
      </div>
      <div class="accent-line" style="margin-bottom: 20px" />

      <div class="known-list">
        <div v-for="issue in KNOWN_ISSUES" :key="issue.id" class="known-card">
          <span class="sev-pill" :class="'sev-pill-' + issue.severity.toLowerCase()">{{ issue.severity }}</span>
          <div class="known-body">
            <div class="known-title">{{ issue.title }}</div>
            <div class="known-desc">{{ issue.desc }}</div>
          </div>
          <span class="status-pill" :class="statusCls(issue.status)">{{ issue.status }}</span>
        </div>
      </div>
    </div>

    <!-- Toast -->
    <div v-if="toast" class="report-toast" :class="toast.type">{{ toast.msg }}</div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'

const props = defineProps({
  player: { type: Object, default: null },
  settlement: { type: Object, default: null },
})

// ── Tabs ──────────────────────────────────────────────────────────────────
const tabs = [
  { key: 'new',   label: 'NEW REPORT' },
  { key: 'mine',  label: 'MY REPORTS' },
  { key: 'known', label: 'KNOWN ISSUES' },
]
const activeTab = ref('new')

// ── Form options ──────────────────────────────────────────────────────────
const CATEGORIES = [
  'Gameplay', 'UI / Interface', 'Performance', 'Combat', 'Economy / Resources',
  'Buildings', 'Research', 'Alliance', 'Wasteland / Map', 'Messages',
  'Login / Account', 'Other',
]
const AREAS = [
  'Camp overview', 'Buildings panel', 'Research panel', 'Units panel',
  'Defense panel', 'Wasteland map', 'Alliance hub', 'Messages', 'Ranking',
  'Login screen', 'Resource bar', 'Other',
]
const SEVERITIES = [
  { value: 'Low',      label: 'LOW',      cls: 'sev-low' },
  { value: 'Medium',   label: 'MEDIUM',   cls: 'sev-med' },
  { value: 'High',     label: 'HIGH',     cls: 'sev-high' },
  { value: 'Critical', label: 'CRITICAL', cls: 'sev-crit' },
]
const SEV_HINTS = {
  Low:      'Minor cosmetic issue or typo. Workaround available.',
  Medium:   'Feature works but behavior is wrong or confusing.',
  High:     'Major feature broken. Significantly impacts gameplay.',
  Critical: "Game-breaking. Data loss, can't play, or security issue.",
}
const KNOWN_ISSUES = [
  { id: 1, title: 'Research queue shows wrong duration after refresh', desc: 'The displayed timer may be off by a few seconds.', severity: 'Low',    status: 'InProgress' },
  { id: 2, title: 'Alliance chat history not preserved on reload',     desc: 'Messages sent before the current session are not loaded.', severity: 'Medium', status: 'Open' },
  { id: 3, title: 'Wasteland sector tooltip overlaps sidebar',          desc: 'Affects 1280×720 screens.', severity: 'Low', status: 'Open' },
]

// ── Form state ────────────────────────────────────────────────────────────
const defaultForm = () => ({
  title: '',
  category: '',
  area: '',
  severity: '',
  description: '',
  stepsToReproduce: '',
  settlementName: props.settlement?.name ?? '',
  browser: navigator.userAgent.match(/(Chrome|Firefox|Safari|Edge)\/[\d.]+/)?.[0] ?? '',
})
const form = ref(defaultForm())
const submitting = ref(false)
const submitSuccess = ref(false)

// ── My reports ────────────────────────────────────────────────────────────
const myReports = ref([])
const loadingMine = ref(false)

// ── Toast ─────────────────────────────────────────────────────────────────
const toast = ref(null)
let toastTimer = null
function showToast(msg, type = 'info') {
  toast.value = { msg, type }
  clearTimeout(toastTimer)
  toastTimer = setTimeout(() => { toast.value = null }, 4000)
}

// ── Helpers ───────────────────────────────────────────────────────────────
function statusCls(s) {
  return { Open: 'st-open', InProgress: 'st-progress', Resolved: 'st-resolved', Closed: 'st-closed' }[s] ?? 'st-open'
}
function fmtDate(utc) {
  return new Date(utc).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
}

// ── Actions ───────────────────────────────────────────────────────────────
async function submitReport() {
  if (!form.value.severity) { showToast('Please select a severity level.', 'error'); return }
  submitting.value = true
  try {
    const res = await fetch('/api/bugreports', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        playerId: props.player?.id,
        title: form.value.title,
        category: form.value.category,
        area: form.value.area,
        severity: form.value.severity,
        description: form.value.description,
        stepsToReproduce: form.value.stepsToReproduce,
        settlementName: form.value.settlementName,
        browser: form.value.browser,
      }),
    })
    if (!res.ok) throw new Error(await res.text())
    submitSuccess.value = true
    form.value = defaultForm()
    await loadMyReports()
  } catch {
    showToast('Failed to submit report. Please try again.', 'error')
  } finally {
    submitting.value = false
  }
}

function resetForm() {
  form.value = defaultForm()
  submitSuccess.value = false
}

async function loadMyReports() {
  if (!props.player?.id) return
  loadingMine.value = true
  try {
    const res = await fetch(`/api/bugreports/player/${props.player.id}`)
    if (res.ok) myReports.value = await res.json()
  } catch { /* ignore */ } finally {
    loadingMine.value = false
  }
}

onMounted(loadMyReports)
</script>

<style scoped>
.report-view { padding-bottom: 40px; }

/* tabs */
.report-tabs {
  display: flex;
  gap: 0;
  border-bottom: 1px solid var(--border);
  margin-bottom: 28px;
}
.report-tab {
  background: none;
  border: none;
  border-bottom: 2px solid transparent;
  padding: 10px 24px;
  font-family: var(--ff-title);
  font-size: 10px;
  letter-spacing: 2px;
  color: var(--muted);
  cursor: pointer;
  transition: all .2s;
  display: flex;
  align-items: center;
  gap: 6px;
}
.report-tab:hover { color: var(--text); }
.report-tab.active { color: var(--cyan); border-bottom-color: var(--cyan); }
.tab-badge {
  background: var(--cyan-dim);
  color: var(--cyan);
  border-radius: 10px;
  padding: 1px 6px;
  font-size: 9px;
}

/* info banner */
.info-banner {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
  background: rgba(0,212,255,.06);
  border: 1px solid var(--cyan-dim);
  border-radius: 6px;
  padding: 10px 16px;
  font-size: 13px;
  margin-bottom: 20px;
}
.link-btn {
  background: none;
  border: none;
  color: var(--cyan);
  cursor: pointer;
  font-size: 13px;
  text-decoration: underline;
  padding: 0 4px;
}

/* guidelines */
.guidelines-box {
  background: var(--panel-bg, var(--bg2, #0a1018));
  border: 1px solid var(--border);
  border-radius: 8px;
  padding: 16px 20px;
  margin-bottom: 20px;
}
.guidelines-head {
  display: flex;
  justify-content: space-between;
  font-family: var(--ff-title);
  font-size: 10px;
  letter-spacing: 2px;
  color: var(--cyan);
  margin-bottom: 10px;
}
.guidelines-list {
  list-style: none;
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.guidelines-list li {
  font-size: 13px;
  color: var(--text);
  padding-left: 18px;
  position: relative;
}
.guidelines-list li::before { content: '▸'; position: absolute; left: 0; color: var(--cyan); }

/* form */
.report-form {
  background: var(--panel-bg, var(--bg2, #0a1018));
  border: 1px solid var(--border);
  border-radius: 10px;
  padding: 24px;
  display: flex;
  flex-direction: column;
  gap: 18px;
}
.form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 18px; }
.form-field { display: flex; flex-direction: column; gap: 5px; }
.field-label { font-family: var(--ff-title); font-size: 9px; letter-spacing: 2px; color: var(--muted); }
.req { color: #ff3040; }
.field-input {
  background: var(--input-bg, #0e1620);
  border: 1px solid var(--border);
  border-radius: 6px;
  color: var(--bright);
  font-family: var(--ff);
  font-size: 14px;
  padding: 9px 13px;
  outline: none;
  transition: border .2s;
}
.field-input:focus { border-color: var(--cyan-dim, #004466); }
.field-textarea { resize: vertical; min-height: 80px; font-family: var(--ff); }
select.field-input option { background: #0e1620; }
.char-count { font-size: 11px; color: var(--muted); text-align: right; }

/* severity */
.sev-row { display: flex; gap: 8px; flex-wrap: wrap; }
.sev-btn {
  font-family: var(--ff-title);
  font-size: 10px;
  letter-spacing: 2px;
  padding: 7px 16px;
  border-radius: 4px;
  cursor: pointer;
  border: 1px solid var(--border);
  background: var(--input-bg, #0e1620);
  color: var(--muted);
  transition: all .2s;
}
.sev-low  { color: #00d4ff; }  .sev-low.active  { background: rgba(0,212,255,.1); border-color: #00a0cc; }
.sev-med  { color: #ffaa20; }  .sev-med.active  { background: rgba(255,170,32,.1); border-color: #ffaa20; }
.sev-high { color: #ff8040; }  .sev-high.active { background: rgba(255,128,64,.1); border-color: #ff8040; }
.sev-crit { color: #ff3040; }  .sev-crit.active { background: rgba(255,48,64,.1);  border-color: #ff3040; }
.sev-hint { font-size: 12px; color: var(--muted); padding-top: 4px; }

/* form actions */
.form-actions { display: flex; justify-content: flex-end; gap: 10px; padding-top: 4px; }
.action-btn {
  font-family: var(--ff-title);
  font-size: 10px;
  letter-spacing: 2px;
  padding: 9px 20px;
  border-radius: 4px;
  cursor: pointer;
  border: 1px solid var(--cyan-dim, #004466);
  background: var(--cyan-dim, #004466);
  color: var(--cyan, #00d4ff);
  transition: all .2s;
}
.action-btn:hover { background: var(--cyan-dark, #0088aa); color: #e8f4ff; }
.action-btn:disabled { opacity: .4; cursor: not-allowed; }
.secondary-btn {
  font-family: var(--ff-title);
  font-size: 10px;
  letter-spacing: 2px;
  padding: 9px 20px;
  border-radius: 4px;
  cursor: pointer;
  background: none;
  border: 1px solid var(--border);
  color: var(--muted);
  transition: all .2s;
}
.secondary-btn:hover { color: var(--text); border-color: var(--border-bright, #1a3048); }

/* success */
.submit-success {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  padding: 48px;
  background: var(--panel-bg, #0a1018);
  border: 1px solid rgba(48,255,128,.2);
  border-radius: 10px;
  text-align: center;
  margin-bottom: 20px;
}
.success-check {
  width: 52px; height: 52px;
  border-radius: 50%;
  background: rgba(48,255,128,.08);
  border: 2px solid #30ff80;
  display: grid; place-items: center;
  font-size: 22px; color: #30ff80;
}
.submit-success h3 { font-family: var(--ff-title); font-size: 13px; letter-spacing: 3px; color: #30ff80; }
.submit-success p { font-size: 13px; color: var(--muted); }

/* my reports */
.loading-msg { color: var(--muted); font-size: 13px; padding: 20px 0; }
.empty-panel { display: flex; flex-direction: column; align-items: center; gap: 12px; padding: 60px 24px; text-align: center; }
.empty-icon { font-size: 36px; }
.empty-title { font-family: var(--ff-title); font-size: 12px; letter-spacing: 3px; color: var(--bright); }
.empty-sub { font-size: 13px; color: var(--muted); }
.reports-list { display: flex; flex-direction: column; gap: 8px; }
.report-card {
  display: flex;
  background: var(--panel-bg, #0a1018);
  border: 1px solid var(--border);
  border-radius: 8px;
  overflow: hidden;
  transition: border .2s;
}
.report-card:hover { border-color: var(--border-bright, #1a3048); }
.rc-bar { width: 4px; flex-shrink: 0; }
.rc-low    { background: #00d4ff; }
.rc-medium { background: #ffaa20; }
.rc-high   { background: #ff8040; }
.rc-critical { background: #ff3040; }
.rc-body { flex: 1; padding: 12px 16px; display: flex; flex-direction: column; gap: 6px; }
.rc-top { display: flex; justify-content: space-between; align-items: center; gap: 8px; }
.rc-title { font-size: 14px; color: var(--bright); font-weight: 600; }
.rc-meta { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.rc-tag {
  background: var(--input-bg, #0e1620);
  border: 1px solid var(--border);
  border-radius: 4px;
  font-size: 11px;
  padding: 2px 8px;
  color: var(--text);
}
.rc-date { font-size: 11px; color: var(--muted); margin-left: auto; }

/* status pills */
.status-pill { font-family: var(--ff-title); font-size: 9px; letter-spacing: 1px; padding: 3px 10px; border-radius: 10px; white-space: nowrap; }
.st-open     { background: rgba(0,212,255,.08); color: #00d4ff; border: 1px solid #004466; }
.st-progress { background: rgba(255,170,32,.08); color: #ffaa20; border: 1px solid #3d2a08; }
.st-resolved { background: rgba(48,255,128,.08); color: #30ff80; border: 1px solid #0a3318; }
.st-closed   { background: #0e1620; color: var(--muted); border: 1px solid var(--border); }

/* severity pills */
.sev-pill { font-family: var(--ff-title); font-size: 9px; letter-spacing: 1px; padding: 2px 8px; border-radius: 4px; }
.sev-pill-low      { background: rgba(0,212,255,.08); color: #00d4ff; border: 1px solid #004466; }
.sev-pill-medium   { background: rgba(255,170,32,.08); color: #ffaa20; border: 1px solid #3d2a08; }
.sev-pill-high     { background: rgba(255,128,64,.08); color: #ff8040; border: 1px solid rgba(255,128,64,.3); }
.sev-pill-critical { background: rgba(255,48,64,.08);  color: #ff3040; border: 1px solid #661420; }

/* known issues */
.known-list { display: flex; flex-direction: column; gap: 8px; }
.known-card {
  display: flex;
  align-items: flex-start;
  gap: 14px;
  background: var(--panel-bg, #0a1018);
  border: 1px solid var(--border);
  border-radius: 8px;
  padding: 14px 16px;
}
.known-body { flex: 1; }
.known-title { font-size: 14px; color: var(--bright); font-weight: 600; margin-bottom: 3px; }
.known-desc { font-size: 12px; color: var(--muted); }

/* toast */
.report-toast {
  position: fixed;
  bottom: 24px;
  right: 24px;
  background: var(--input-bg, #0e1620);
  border: 1px solid var(--border-bright, #1a3048);
  border-radius: 8px;
  padding: 12px 20px;
  font-size: 13px;
  color: var(--bright);
  z-index: 999;
  animation: rToastIn .25s ease;
}
.report-toast.error { border-color: #ff3040; color: #ff3040; }
@keyframes rToastIn { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }
</style>
