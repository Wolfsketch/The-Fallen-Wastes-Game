<template>
  <div>

    <!-- ── TOPBAR ─────────────────────────────────────────────────────── -->
    <header class="topbar">
      <div class="topbar::after"></div>
      <div class="topbar-inner">
        <div class="topbar-brand">
          <div class="topbar-radiation">☢</div>
          <div>
            <div class="topbar-title">THE FALLEN WASTES</div>
            <div class="topbar-sub">COMMUNITY FORUM</div>
          </div>
        </div>

        <nav class="topbar-nav">
          <a href="#" class="active" @click.prevent="currentView = 'index'; activeCategoryKey = null">Forum</a>
          <a href="#" @click.prevent>What's New</a>
          <a href="#" @click.prevent>Members</a>
          <a href="http://localhost:5173">← Back to Game</a>
        </nav>

        <div class="topbar-right">
          <div class="topbar-search">⌕ Search forum... <kbd>Ctrl K</kbd></div>
          <div v-if="playerData" class="topbar-user">
            <div class="topbar-avatar">{{ playerData.username?.charAt(0).toUpperCase() }}</div>
            {{ playerData.username }}
            <button class="topbar-logout-btn" @click="logout" title="Log out">✕</button>
          </div>
          <button v-else class="btn btn-primary btn-topbar" @click="openAuthModal('login')">LOG IN / REGISTER</button>
        </div>
      </div>
    </header>

    <!-- ── HERO ──────────────────────────────────────────────────────── -->
    <div class="hero">
      <div class="hero-inner">
        <div>
          <h1>COMMUNITY FORUM</h1>
          <p>Discuss strategy, share ideas and shape the future of The Fallen Wastes</p>
        </div>
        <div class="hero-stats">
          <div class="hero-stat">
            <div class="hero-stat-val">{{ stats.totalTopics }}</div>
            <div class="hero-stat-label">TOPICS</div>
          </div>
          <div class="hero-stat">
            <div class="hero-stat-val">{{ stats.totalPosts }}</div>
            <div class="hero-stat-label">POSTS</div>
          </div>
          <div class="hero-stat">
            <div class="hero-stat-val">{{ onlineUsers.length }}</div>
            <div class="hero-stat-label">ONLINE</div>
          </div>
        </div>
      </div>
    </div>

    <div class="wrap">

      <!-- ── FORUM INDEX ─────────────────────────────────────────────── -->
      <template v-if="currentView === 'index'">
        <div class="toolbar">
          <div class="breadcrumb">
            <a href="#">Forum</a>
          </div>
          <div class="toolbar-actions">
            <button class="btn btn-primary" @click="openNewTopicModal(null)" v-if="playerId">NEW TOPIC</button>
          </div>
        </div>

        <div v-for="group in CATEGORY_GROUPS" :key="group.label" class="forum-section">
          <div class="forum-section-header">
            <div class="forum-section-icon" :style="group.iconStyle">{{ group.icon }}</div>
            <span class="forum-section-title">{{ group.label }}</span>
          </div>

          <div
            v-for="cat in group.categories" :key="cat.key"
            class="forum-row"
            @click="openCategory(cat)"
          >
            <div class="forum-row-icon">{{ cat.icon }}</div>
            <div class="forum-row-info">
              <div class="forum-row-name">
                <a href="#" @click.prevent>{{ cat.name }}</a>
                <span v-if="cat.badge" class="pin-badge" :class="cat.badge.toLowerCase()">{{ cat.badge }}</span>
              </div>
              <div class="forum-row-desc">{{ cat.desc }}</div>
              <div v-if="cat.subTags" class="forum-row-subs">
                <span v-for="tag in cat.subTags" :key="tag" class="forum-sub-link">{{ tag }}</span>
              </div>
            </div>
            <div class="forum-row-stat">
              <div class="forum-row-stat-val">{{ catStat(cat.key).topics }}</div>
              <div class="forum-row-stat-label">Topics</div>
            </div>
            <div class="forum-row-stat">
              <div class="forum-row-stat-val">{{ catStat(cat.key).posts }}</div>
              <div class="forum-row-stat-label">Posts</div>
            </div>
            <div class="forum-row-latest" @click.stop>
              <template v-if="catStat(cat.key).latestAuthor">
                <div class="forum-row-latest-avatar">{{ catStat(cat.key).latestAuthor.charAt(0).toUpperCase() }}</div>
                <div class="forum-row-latest-info">
                  <div class="forum-row-latest-title">
                    <a href="#" @click.prevent="openCategoryAndTopic(cat, catStat(cat.key).latestTopicId)">
                      {{ truncate(catStat(cat.key).latestTitle, 40) }}
                    </a>
                  </div>
                  <div class="forum-row-latest-meta">
                    <span style="color:var(--cyan)">{{ catStat(cat.key).latestAuthor }}</span>
                    <span>· {{ fmtRelative(catStat(cat.key).latestDate) }}</span>
                  </div>
                </div>
              </template>
              <span v-else style="font-size:12px;color:var(--muted)">No posts yet</span>
            </div>
          </div>
        </div>

        <!-- Online strip -->
        <div v-if="onlineUsers.length" class="online-strip">
          <div class="online-dot"></div>
          <span><span class="online-count">{{ onlineUsers.length }} survivor{{ onlineUsers.length === 1 ? '' : 's' }}</span> currently online</span>
          <span class="online-names">— {{ onlineUsers.slice(0, 6).join(', ') }}<span v-if="onlineUsers.length > 6"> and {{ onlineUsers.length - 6 }} others</span></span>
        </div>

        <div class="forum-footer">
          <p>The Fallen Wastes Community Forum · <a href="http://localhost:5173">Back to Game</a></p>
          <p style="margin-top:6px;font-family:var(--ff-title);font-size:9px;letter-spacing:3px;opacity:.4">© 2026 THE FALLEN WASTES</p>
        </div>
      </template>

      <!-- ── CATEGORY / TOPIC LIST ────────────────────────────────────── -->
      <template v-if="currentView === 'category'">
        <div class="toolbar">
          <div class="breadcrumb">
            <a href="#" @click.prevent="currentView = 'index'; activeCategoryKey = null">Forum</a>
            <span class="sep">›</span>
            <span>{{ activeCategory?.name }}</span>
          </div>
          <div class="toolbar-actions">
            <button class="btn btn-primary" @click="openNewTopicModal(activeCategory)" v-if="playerId">NEW TOPIC</button>
          </div>
        </div>

        <div v-if="loadingTopics" class="state-msg">Loading topics...</div>

        <div v-else-if="topics.length === 0" class="state-empty">
          <div>💬</div>
          <div>No topics yet — be the first!</div>
          <button class="btn btn-primary" @click="openNewTopicModal(activeCategory)" v-if="playerId">CREATE TOPIC</button>
        </div>

        <template v-else>
          <div class="forum-section">
            <div class="forum-section-header">
              <div class="forum-section-icon" style="background:var(--cyan-glow2);border:1px solid var(--cyan-dim)">{{ activeCategory?.icon }}</div>
              <span class="forum-section-title">{{ activeCategory?.name?.toUpperCase() }}</span>
              <span class="forum-section-count">{{ topics.length }} topics</span>
            </div>

            <div class="topic-list-header">
              <span>TOPIC</span>
              <span>REPLIES</span>
              <span>VIEWS</span>
              <span>LAST POST</span>
            </div>

            <div
              v-for="t in topics" :key="t.id"
              class="topic-row"
              @click="openTopic(t)"
            >
              <div class="topic-info">
                <div class="topic-avatar">{{ t.authorUsername?.charAt(0).toUpperCase() }}</div>
                <div>
                  <div class="topic-title">
                    <a href="#" @click.prevent>{{ t.title }}</a>
                    <span v-if="t.isPinned"  class="pin-badge official" style="margin-left:8px">PINNED</span>
                    <span v-if="t.isOfficial" class="pin-badge official" style="margin-left:8px">OFFICIAL</span>
                  </div>
                  <div class="topic-meta">by {{ t.authorUsername }} · {{ fmtDate(t.createdAtUtc) }}</div>
                </div>
              </div>
              <div class="topic-stat">{{ t.postCount ?? 0 }}</div>
              <div class="topic-stat">—</div>
              <div class="topic-latest">
                <div class="topic-latest-info">
                  <div>by <a href="#" @click.prevent>{{ t.lastPostUsername ?? t.authorUsername }}</a></div>
                  <div>{{ fmtRelative(t.lastPostAtUtc ?? t.createdAtUtc) }}</div>
                </div>
              </div>
            </div>
          </div>

          <div v-if="totalPages > 1" class="pagination">
            <button class="page-btn" :class="{ active: page === 1 }" :disabled="page === 1" @click="loadTopics(page - 1)">‹</button>
            <button
              v-for="p in totalPages" :key="p"
              class="page-btn" :class="{ active: page === p }"
              @click="loadTopics(p)"
            >{{ p }}</button>
            <button class="page-btn" :disabled="page === totalPages" @click="loadTopics(page + 1)">›</button>
          </div>
        </template>
      </template>

      <!-- ── TOPIC DETAIL ────────────────────────────────────────────── -->
      <template v-if="currentView === 'topic'">
        <div class="toolbar">
          <div class="breadcrumb">
            <a href="#" @click.prevent="currentView = 'index'; activeCategoryKey = null">Forum</a>
            <span class="sep">›</span>
            <a href="#" @click.prevent="currentView = 'category'">{{ activeCategory?.name }}</a>
            <span class="sep">›</span>
            <span>{{ activeTopic?.title }}</span>
          </div>
          <div class="toolbar-actions">
            <button class="btn btn-ghost" @click="currentView = 'category'">← BACK</button>
          </div>
        </div>

        <div v-if="loadingTopic" class="state-msg">Loading...</div>
        <div v-else-if="activeTopic">
          <div class="topic-detail-title-row">
            <h2 class="topic-detail-h2">{{ activeTopic.title }}</h2>
            <span v-if="activeTopic.isPinned"  class="pin-badge official">📌 PINNED</span>
            <span v-if="activeTopic.isOfficial" class="pin-badge official">🛡️ OFFICIAL</span>
          </div>

          <div class="post-list">
            <div
              v-for="(post, idx) in activeTopic.posts" :key="post.id"
              class="post-card"
              :class="{ 'post-op': idx === 0 }"
            >
              <div class="post-author-col">
                <div class="post-avatar-lg">{{ post.authorUsername?.charAt(0).toUpperCase() }}</div>
                <div class="post-author-name">{{ post.authorUsername }}</div>
                <div class="post-num">#{{ idx + 1 }}</div>
              </div>
              <div class="post-content-col">
                <div class="post-header-row">
                  <span class="post-date">{{ fmtDateTime(post.createdAtUtc) }}</span>
                </div>
                <div class="post-body">{{ post.content }}</div>
              </div>
            </div>
          </div>

          <!-- Reply -->
          <div v-if="playerId" class="reply-box">
            <div class="reply-title">POST REPLY</div>
            <textarea
              v-model="replyContent"
              class="reply-textarea"
              rows="5"
              placeholder="Write your reply..."
              maxlength="10000"
            ></textarea>
            <div class="reply-footer">
              <span class="char-count">{{ replyContent.length }} / 10000</span>
              <button
                class="btn btn-primary"
                @click="submitReply"
                :disabled="submittingReply || !replyContent.trim()"
              >{{ submittingReply ? 'POSTING...' : 'POST REPLY' }}</button>
            </div>
          </div>
          <div v-else class="reply-login-prompt">
            <button class="btn btn-primary" @click="openAuthModal('login')">LOG IN TO REPLY</button>
            <span style="margin-left:10px;font-size:12px">or <button class="link-btn" @click="openAuthModal('register')">create an account</button></span>
          </div>
        </div>
      </template>

    </div><!-- /wrap -->

    <!-- ── NEW TOPIC MODAL ────────────────────────────────────────────── -->
    <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
      <div class="modal-box">
        <div class="modal-head">
          <h3 class="modal-title">NEW TOPIC</h3>
          <button class="modal-close" @click="showModal = false">✕</button>
        </div>

        <div class="field-group">
          <label class="field-label">CATEGORY</label>
          <select v-model="newTopic.categoryKey" class="field-input">
            <option v-for="c in ALL_CATEGORIES" :key="c.key" :value="c.key">{{ c.name }}</option>
          </select>
        </div>
        <div class="field-group">
          <label class="field-label">TITLE <span class="req">*</span></label>
          <input v-model="newTopic.title" class="field-input" placeholder="Topic title" maxlength="200" />
        </div>
        <div class="field-group">
          <label class="field-label">MESSAGE <span class="req">*</span></label>
          <textarea v-model="newTopic.content" class="field-input field-textarea" rows="7" placeholder="Write your opening post..." maxlength="10000"></textarea>
        </div>

        <div class="modal-footer">
          <button class="btn btn-ghost" @click="showModal = false">CANCEL</button>
          <button
            class="btn btn-primary"
            @click="submitNewTopic"
            :disabled="submittingTopic || !newTopic.title.trim() || !newTopic.content.trim()"
          >{{ submittingTopic ? 'POSTING...' : 'CREATE TOPIC' }}</button>
        </div>
      </div>
    </div>

    <!-- Toast -->
    <div v-if="toast" class="toast" :class="toast.type">{{ toast.msg }}</div>

    <!-- ── AUTH MODAL ─────────────────────────────────────────────────── -->
    <div v-if="showAuthModal" class="modal-overlay" @click.self="showAuthModal = false">
      <div class="modal-box auth-modal-box">

        <div class="auth-logo">
          <div class="auth-radiation">☢</div>
          <div class="auth-brand-title">THE FALLEN WASTES</div>
          <div class="auth-brand-sub">{{ authMode === 'login' ? 'RECONNECT TO OUTPOST' : 'INITIALIZE NEW OUTPOST' }}</div>
        </div>

        <div class="mode-toggle">
          <button class="mode-btn" :class="{ 'mode-btn--active': authMode === 'login' }" @click="authMode = 'login'; authError = ''">LOGIN</button>
          <button class="mode-btn" :class="{ 'mode-btn--active': authMode === 'register' }" @click="authMode = 'register'; authError = ''">REGISTER</button>
        </div>

        <div v-if="authError" class="auth-error">
          <span class="pin-badge hot">ERROR</span>
          {{ authError }}
        </div>

        <div class="field-group" style="margin-top:16px">
          <label class="field-label">OPERATOR CALLSIGN</label>
          <input
            v-model="authUsername"
            type="text"
            class="field-input"
            :placeholder="authMode === 'login' ? 'Enter your username' : 'Choose username (min 5 chars)'"
            @keyup.enter="submitAuth"
          />
        </div>

        <template v-if="authMode === 'register'">
          <div class="field-group">
            <label class="field-label">COMM FREQUENCY (EMAIL)</label>
            <input v-model="authEmail" type="email" class="field-input" placeholder="Enter email address" @keyup.enter="submitAuth" />
          </div>
          <div class="field-group">
            <label class="field-label">OUTPOST DESIGNATION</label>
            <input v-model="authSettlementName" type="text" class="field-input" placeholder="Name your settlement (min 3 chars)" @keyup.enter="submitAuth" />
          </div>
        </template>

        <div v-if="authLoading" class="auth-progress">
          <div class="auth-progress-bar"><div class="auth-progress-fill" :style="{ width: authProgress + '%' }"></div></div>
          <div class="auth-progress-label">{{ authStep }}</div>
        </div>

        <div class="modal-footer" style="margin-top:20px">
          <button class="btn btn-ghost" @click="showAuthModal = false">CANCEL</button>
          <button
            class="btn btn-primary"
            @click="submitAuth"
            :disabled="authLoading"
          >{{ authLoading ? '...' : (authMode === 'login' ? 'ENTER THE WASTES' : 'DEPLOY OUTPOST') }}</button>
        </div>

      </div>
    </div>

  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'

// ── Auth ──────────────────────────────────────────────────────────────────
const playerId = ref(sessionStorage.getItem('playerId') || null)
const playerData = ref(null)
try {
  const raw = sessionStorage.getItem('playerData')
  if (raw) playerData.value = JSON.parse(raw)
} catch { /* ignore */ }

const showAuthModal = ref(false)
const authMode = ref('login')   // 'login' | 'register'
const authUsername = ref('')
const authEmail = ref('')
const authSettlementName = ref('')
const authError = ref('')
const authLoading = ref(false)
const authStep = ref('')
const authProgress = ref(0)

function openAuthModal(mode = 'login') {
  authMode.value = mode
  authUsername.value = ''
  authEmail.value = ''
  authSettlementName.value = ''
  authError.value = ''
  authLoading.value = false
  showAuthModal.value = true
}

function logout() {
  sessionStorage.removeItem('playerId')
  sessionStorage.removeItem('playerData')
  playerId.value = null
  playerData.value = null
  showToast('Logged out.', 'info')
}

async function submitAuth() {
  authError.value = ''
  const uname = authUsername.value.trim()

  if (!uname) { authError.value = 'Username is required.'; return }

  if (authMode.value === 'register') {
    if (uname.length < 5)              { authError.value = 'Username must be at least 5 characters.'; return }
    if (!authEmail.value.trim())       { authError.value = 'Email is required.'; return }
    if (!authSettlementName.value.trim() || authSettlementName.value.trim().length < 3) {
      authError.value = 'Settlement name must be at least 3 characters.'
      return
    }
  }

  authLoading.value = true
  authProgress.value = 20
  authStep.value = authMode.value === 'login' ? 'CONNECTING TO WASTELAND SYSTEMS...' : 'INITIALIZING NEW OUTPOST...'

  try {
    let data
    authProgress.value = 50
    authStep.value = authMode.value === 'login' ? 'CHECKING ACCOUNT...' : 'REGISTERING OPERATOR...'

    if (authMode.value === 'login') {
      const res = await fetch(`/api/players/login/${encodeURIComponent(uname)}`)
      if (res.status === 404) throw new Error('Player not found — register first.')
      if (!res.ok) throw new Error(await res.text() || 'Login failed.')
      data = await res.json()
    } else {
      const res = await fetch('/api/players', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          username: uname,
          email: authEmail.value.trim(),
          settlementName: authSettlementName.value.trim(),
        }),
      })
      if (!res.ok) throw new Error(await res.text() || 'Registration failed.')
      data = await res.json()
    }

    authProgress.value = 90
    authStep.value = 'SYNCHRONIZING DATA...'

    sessionStorage.setItem('playerId', data.id)
    sessionStorage.setItem('playerData', JSON.stringify(data))
    playerId.value = data.id
    playerData.value = data

    authProgress.value = 100
    showAuthModal.value = false
    showToast(`Welcome, ${data.username}!`, 'success')
  } catch (err) {
    authError.value = err.message || 'Connection failed — is the backend running?'
  } finally {
    authLoading.value = false
    authProgress.value = 0
    authStep.value = ''
  }
}

// ── Category definitions ──────────────────────────────────────────────────
const ALL_CATEGORIES = [
  { key: 'announcements',       name: 'Announcements',       icon: '📢', desc: 'Patch notes, updates, events and official news from the development team.',          badge: 'OFFICIAL' },
  { key: 'release-notes',       name: 'Release Notes',        icon: '📋', desc: 'Detailed changelog for every version. Read before reporting bugs.' },
  { key: 'suggestions',         name: 'Suggestions',          icon: '🗳️', desc: 'Got an idea to improve the game? Post it here.',                                    badge: 'HOT', subTags: ['UNITS','BUILDINGS','RESEARCH','MAP / OUTPOSTS','UI / UX'] },
  { key: 'ideas-vote',          name: 'Ideas — Up for Vote',  icon: '👍', desc: 'These suggestions have been selected for community voting. Your vote counts.' },
  { key: 'general-feedback',    name: 'General Feedback',     icon: '💬', desc: 'Share your opinions about existing features, balance, pacing and overall direction.' },
  { key: 'q-and-a',             name: 'Questions & Answers',  icon: '❓', desc: 'New to the game or stuck on something? Ask the community for help.' },
  { key: 'guides',              name: 'Guides & Tutorials',   icon: '🗺️', desc: 'Player-written guides on combat, economy, settlement planning and more.',           subTags: ['BEGINNER','COMBAT','ECONOMY','ADVANCED'] },
  { key: 'strategy',            name: 'Strategy Discussion',  icon: '⚔️', desc: 'Debate tactics, army compositions, defense setups and conquest strategies.' },
  { key: 'introductions',       name: 'Introductions',        icon: '👋', desc: 'New here? Say hello and introduce yourself to the community.' },
  { key: 'off-topic',           name: 'The Watering Hole',    icon: '🍺', desc: 'Off-topic chat, memes, screenshots and anything that doesn\'t fit elsewhere.' },
  { key: 'alliance-recruitment', name: 'Alliance Recruitment', icon: '🏴', desc: 'Looking for members or searching for an alliance? Post here.',                    badge: 'NEW' },
]

const CATEGORY_GROUPS = [
  {
    label: 'OFFICIAL', icon: '📡',
    iconStyle: 'background:var(--cyan-glow2);border:1px solid var(--cyan-dim)',
    categories: ALL_CATEGORIES.filter(c => ['announcements','release-notes'].includes(c.key)),
  },
  {
    label: 'FEEDBACK & IDEAS', icon: '💡',
    iconStyle: 'background:rgba(48,255,128,.1);border:1px solid rgba(48,255,128,.2)',
    categories: ALL_CATEGORIES.filter(c => ['suggestions','ideas-vote','general-feedback'].includes(c.key)),
  },
  {
    label: 'HELP & STRATEGY', icon: '📖',
    iconStyle: 'background:rgba(255,170,32,.1);border:1px solid rgba(255,170,32,.2)',
    categories: ALL_CATEGORIES.filter(c => ['q-and-a','guides','strategy'].includes(c.key)),
  },
  {
    label: 'COMMUNITY', icon: '🎭',
    iconStyle: 'background:rgba(170,102,255,.1);border:1px solid rgba(170,102,255,.2)',
    categories: ALL_CATEGORIES.filter(c => ['introductions','off-topic','alliance-recruitment'].includes(c.key)),
  },
]

// ── Remote data ───────────────────────────────────────────────────────────
const stats = ref({ totalTopics: 0, totalPosts: 0 })
const categoryStatsMap = ref({})
const onlineUsers = ref([])

// ── Navigation ────────────────────────────────────────────────────────────
const currentView = ref('index')   // 'index' | 'category' | 'topic'
const activeCategory = ref(null)
const activeCategoryKey = ref(null)
const activeTopic = ref(null)

const topics = ref([])
const page = ref(1)
const totalPages = ref(1)
const loadingTopics = ref(false)
const loadingTopic = ref(false)

// ── New topic modal ───────────────────────────────────────────────────────
const showModal = ref(false)
const newTopic = ref({ categoryKey: '', title: '', content: '' })
const submittingTopic = ref(false)

// ── Reply ─────────────────────────────────────────────────────────────────
const replyContent = ref('')
const submittingReply = ref(false)

// ── Toast ─────────────────────────────────────────────────────────────────
const toast = ref(null)
let toastTimer = null
function showToast(msg, type = 'info') {
  toast.value = { msg, type }
  clearTimeout(toastTimer)
  toastTimer = setTimeout(() => { toast.value = null }, 4000)
}

// ── Helpers ───────────────────────────────────────────────────────────────
function catStat(key) {
  return categoryStatsMap.value[key] ?? { topics: 0, posts: 0, latestTitle: null, latestAuthor: null, latestDate: null, latestTopicId: null }
}
function truncate(s, n) {
  if (!s) return ''
  return s.length > n ? s.slice(0, n) + '…' : s
}
function fmtDate(utc) {
  if (!utc) return ''
  return new Date(utc).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
}
function fmtDateTime(utc) {
  if (!utc) return ''
  return new Date(utc).toLocaleString('en-GB', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}
function fmtRelative(utc) {
  if (!utc) return ''
  const diff = Date.now() - new Date(utc).getTime()
  const m = Math.floor(diff / 60000)
  if (m < 1)  return 'just now'
  if (m < 60) return `${m}m ago`
  const h = Math.floor(m / 60)
  if (h < 24) return `${h}h ago`
  const d = Math.floor(h / 24)
  if (d < 7)  return `${d}d ago`
  return fmtDate(utc)
}

// ── API ───────────────────────────────────────────────────────────────────
async function loadStats() {
  try {
    const res = await fetch('/api/forum/stats')
    if (!res.ok) return
    const data = await res.json()
    stats.value.totalTopics = data.totalTopics ?? 0
    stats.value.totalPosts  = data.totalPosts  ?? 0
    const map = {}
    ;(data.categories ?? []).forEach(c => {
      map[c.categoryKey] = {
        topics:       c.topicCount ?? 0,
        posts:        c.postCount  ?? 0,
        latestTitle:  c.latestTopicTitle  ?? null,
        latestAuthor: c.latestTopicAuthor ?? null,
        latestDate:   c.latestTopicDate   ?? null,
        latestTopicId: c.latestTopicId    ?? null,
      }
    })
    categoryStatsMap.value = map
  } catch { /* ignore */ }
}

async function loadOnline() {
  try {
    const res = await fetch('/api/forum/online')
    if (res.ok) {
      const data = await res.json()
      onlineUsers.value = data.usernames ?? []
    }
  } catch { /* ignore */ }
}

async function loadTopics(p = 1) {
  if (!activeCategory.value) return
  loadingTopics.value = true
  page.value = p
  try {
    const res = await fetch(`/api/forum/topics?category=${activeCategory.value.key}&page=${p}&pageSize=25`)
    if (res.ok) {
      const data = await res.json()
      topics.value    = data.items ?? data
      totalPages.value = data.totalPages ?? 1
    }
  } catch { /* ignore */ } finally {
    loadingTopics.value = false
  }
}

async function loadTopic(id) {
  loadingTopic.value = true
  try {
    const res = await fetch(`/api/forum/topics/${id}`)
    if (res.ok) activeTopic.value = await res.json()
  } catch { /* ignore */ } finally {
    loadingTopic.value = false
  }
}

// ── Navigation actions ────────────────────────────────────────────────────
function openCategory(cat) {
  activeCategory.value  = cat
  activeCategoryKey.value = cat.key
  currentView.value     = 'category'
  topics.value          = []
  loadTopics(1)
}

async function openCategoryAndTopic(cat, topicId) {
  if (!topicId) return openCategory(cat)
  activeCategory.value = cat
  currentView.value    = 'topic'
  activeTopic.value    = null
  replyContent.value   = ''
  await loadTopic(topicId)
}

async function openTopic(topic) {
  activeTopic.value  = null
  replyContent.value = ''
  currentView.value  = 'topic'
  await loadTopic(topic.id)
}

function openNewTopicModal(cat) {
  newTopic.value = {
    categoryKey: cat?.key ?? ALL_CATEGORIES[0].key,
    title: '',
    content: '',
  }
  showModal.value = true
}

// ── Submit actions ────────────────────────────────────────────────────────
async function submitNewTopic() {
  if (!newTopic.value.title.trim() || !newTopic.value.content.trim()) return
  submittingTopic.value = true
  try {
    const res = await fetch('/api/forum/topics', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        playerId:       playerId.value,
        authorUsername: playerData.value?.username ?? 'Unknown',
        categoryKey:    newTopic.value.categoryKey,
        title:          newTopic.value.title,
        content:        newTopic.value.content,
      }),
    })
    if (!res.ok) throw new Error()
    const created = await res.json()
    showModal.value = false
    showToast('Topic created!', 'success')
    await loadStats()
    const cat = ALL_CATEGORIES.find(c => c.key === newTopic.value.categoryKey) ?? ALL_CATEGORIES[0]
    openCategory(cat)
  } catch {
    showToast('Failed to create topic. Try again.', 'error')
  } finally {
    submittingTopic.value = false
  }
}

async function submitReply() {
  if (!replyContent.value.trim() || !activeTopic.value) return
  submittingReply.value = true
  try {
    const res = await fetch(`/api/forum/topics/${activeTopic.value.id}/posts`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        playerId:       playerId.value,
        authorUsername: playerData.value?.username ?? 'Unknown',
        content:        replyContent.value,
      }),
    })
    if (!res.ok) throw new Error()
    replyContent.value = ''
    showToast('Reply posted!', 'success')
    await loadTopic(activeTopic.value.id)
  } catch {
    showToast('Failed to post reply.', 'error')
  } finally {
    submittingReply.value = false
  }
}

onMounted(async () => {
  await Promise.all([loadStats(), loadOnline()])
})
</script>

<style>
@import url('https://fonts.googleapis.com/css2?family=Orbitron:wght@400;500;600;700;800;900&family=Rajdhani:wght@300;400;500;600;700&display=swap');

*,*::before,*::after{margin:0;padding:0;box-sizing:border-box}
:root{
  --cyan:#00d4ff;--cyan-dark:#0088aa;--cyan-dim:#004466;
  --cyan-glow:rgba(0,212,255,.08);--cyan-glow2:rgba(0,212,255,.15);
  --red:#ff3040;--green:#30ff80;--amber:#ffaa20;--purple:#aa66ff;
  --bg:#060a10;--bg2:#0a1018;--bg3:#0e1620;--bg4:#111c28;
  --border:#142030;--border-bright:#1a3048;
  --muted:#3a5a70;--text:#8ab4cc;--bright:#c0e8ff;--white:#e8f4ff;
  --ff:'Rajdhani',sans-serif;--ff-title:'Orbitron',sans-serif;
}
html{scroll-behavior:smooth}
body{background:var(--bg);color:var(--text);font-family:var(--ff);font-size:15px;line-height:1.6}
a{color:var(--cyan);text-decoration:none;transition:color .2s}
a:hover{color:var(--bright)}
::selection{background:var(--cyan-dim);color:var(--bright)}
::-webkit-scrollbar{width:5px}::-webkit-scrollbar-track{background:var(--bg2)}::-webkit-scrollbar-thumb{background:var(--border-bright);border-radius:3px}

/* TOPBAR */
.topbar{background:linear-gradient(180deg,#0c1420,var(--bg));border-bottom:1px solid var(--border);position:sticky;top:0;z-index:100;position:relative}
.topbar::after{content:'';position:absolute;bottom:0;left:0;right:0;height:1px;background:linear-gradient(90deg,transparent,var(--cyan-dim),transparent)}
.topbar-inner{max-width:1400px;margin:0 auto;display:flex;align-items:center;height:54px;padding:0 40px;gap:24px}
.topbar-brand{display:flex;align-items:center;gap:12px;white-space:nowrap}
.topbar-radiation{width:32px;height:32px;border:2px solid var(--cyan-dim);border-top-color:var(--cyan);border-radius:50%;display:grid;place-items:center;animation:spin 8s linear infinite;font-size:16px;flex-shrink:0}
@keyframes spin{to{transform:rotate(360deg)}}
.topbar-title{font-family:var(--ff-title);font-size:13px;letter-spacing:3px;color:var(--cyan);font-weight:700}
.topbar-sub{font-family:var(--ff-title);font-size:9px;letter-spacing:2px;color:var(--muted)}
.topbar-nav{display:flex;gap:2px;margin-left:24px}
.topbar-nav a{font-family:var(--ff-title);font-size:10px;letter-spacing:2px;color:var(--muted);padding:8px 14px;border-radius:4px;transition:all .15s}
.topbar-nav a:hover{color:var(--text);background:var(--cyan-glow)}
.topbar-nav a.active{color:var(--cyan);background:var(--cyan-glow2)}
.topbar-right{display:flex;align-items:center;gap:12px;margin-left:auto}
.topbar-search{display:flex;align-items:center;gap:8px;background:var(--bg3);border:1px solid var(--border);border-radius:6px;padding:6px 12px;font-size:13px;color:var(--muted);cursor:pointer;min-width:180px}
.topbar-search:hover{border-color:var(--border-bright)}
.topbar-search kbd{font-family:var(--ff);font-size:11px;background:var(--bg2);border:1px solid var(--border);border-radius:3px;padding:1px 6px;margin-left:auto}
.topbar-user{display:flex;align-items:center;gap:8px;padding:5px 12px;background:var(--bg3);border:1px solid var(--border);border-radius:6px;font-size:13px;color:var(--bright);cursor:pointer}
.topbar-avatar{width:24px;height:24px;border-radius:4px;background:var(--cyan-dim);display:grid;place-items:center;font-family:var(--ff-title);font-size:10px;color:var(--cyan)}

/* WRAP */
.wrap{max-width:1100px;margin:0 auto;padding:0 24px}

/* HERO */
.hero{background:linear-gradient(135deg,var(--bg2),var(--bg),#080c14);border-bottom:1px solid var(--border);padding:32px 0 28px;position:relative;overflow:hidden}
.hero::before{content:'';position:absolute;top:0;left:50%;width:600px;height:600px;transform:translate(-50%,-60%);background:radial-gradient(circle,rgba(0,212,255,.04),transparent 70%);pointer-events:none}
.hero-inner{max-width:1100px;margin:0 auto;padding:0 24px;display:flex;align-items:center;justify-content:space-between}
.hero h1{font-family:var(--ff-title);font-size:20px;letter-spacing:4px;color:var(--bright);font-weight:700}
.hero p{font-size:14px;color:var(--muted);margin-top:4px}
.hero-stats{display:flex;gap:20px}
.hero-stat{text-align:center}
.hero-stat-val{font-family:var(--ff-title);font-size:20px;color:var(--cyan)}
.hero-stat-label{font-family:var(--ff-title);font-size:8px;letter-spacing:2px;color:var(--muted)}

/* TOOLBAR */
.toolbar{display:flex;align-items:center;justify-content:space-between;padding:16px 0;border-bottom:1px solid var(--border);margin-bottom:20px}
.breadcrumb{display:flex;align-items:center;gap:6px;font-size:12px;color:var(--muted)}
.breadcrumb a{color:var(--muted)}.breadcrumb a:hover{color:var(--cyan)}
.sep{opacity:.4}
.toolbar-actions{display:flex;gap:8px}
.btn{font-family:var(--ff-title);font-size:10px;letter-spacing:2px;padding:8px 18px;border-radius:4px;cursor:pointer;border:none;transition:all .2s}
.btn-primary{background:var(--cyan-dim);color:var(--cyan);border:1px solid var(--cyan-dark)}
.btn-primary:hover{background:var(--cyan-dark);color:var(--white);box-shadow:0 0 16px rgba(0,212,255,.15)}
.btn-primary:disabled{opacity:.4;cursor:not-allowed}
.btn-ghost{background:none;color:var(--muted);border:1px solid var(--border)}
.btn-ghost:hover{color:var(--text);border-color:var(--border-bright)}

/* FORUM SECTION */
.forum-section{margin-bottom:24px}
.forum-section-header{display:flex;align-items:center;gap:10px;padding:10px 16px;background:var(--bg3);border:1px solid var(--border);border-radius:8px 8px 0 0}
.forum-section-icon{width:28px;height:28px;border-radius:6px;display:grid;place-items:center;font-size:14px;flex-shrink:0}
.forum-section-title{font-family:var(--ff-title);font-size:11px;letter-spacing:3px;color:var(--bright);font-weight:600}
.forum-section-count{margin-left:auto;font-family:var(--ff-title);font-size:9px;letter-spacing:1px;color:var(--muted)}

/* FORUM ROW */
.forum-row{display:grid;grid-template-columns:44px 1fr 80px 80px 220px;align-items:center;gap:14px;padding:14px 16px;background:var(--bg2);border:1px solid var(--border);border-top:none;transition:background .12s;cursor:pointer}
.forum-row:last-child{border-radius:0 0 8px 8px}
.forum-row:hover{background:var(--bg3)}
.forum-row-icon{width:40px;height:40px;border-radius:8px;background:var(--bg);border:1px solid var(--border);display:grid;place-items:center;font-size:20px}
.forum-row-name{font-size:15px;font-weight:600;color:var(--bright);margin-bottom:2px}
.forum-row-name a{color:var(--bright)}.forum-row-name a:hover{color:var(--cyan)}
.forum-row-desc{font-size:12px;color:var(--muted);line-height:1.4}
.forum-row-subs{display:flex;gap:6px;margin-top:6px;flex-wrap:wrap}
.forum-sub-link{font-family:var(--ff-title);font-size:8px;letter-spacing:1.5px;padding:2px 8px;border-radius:3px;background:var(--bg);border:1px solid var(--border);color:var(--muted);transition:all .15s;cursor:default}
.forum-row-stat{text-align:center}
.forum-row-stat-val{font-family:var(--ff-title);font-size:16px;color:var(--bright)}
.forum-row-stat-label{font-family:var(--ff-title);font-size:7px;letter-spacing:1.5px;color:var(--muted);text-transform:uppercase}
.forum-row-latest{display:flex;align-items:center;gap:10px}
.forum-row-latest-avatar{width:32px;height:32px;border-radius:6px;flex-shrink:0;background:var(--bg);border:1px solid var(--border);display:grid;place-items:center;font-family:var(--ff-title);font-size:11px;color:var(--cyan-dim)}
.forum-row-latest-title{font-size:12px;color:var(--text);white-space:nowrap;overflow:hidden;text-overflow:ellipsis;max-width:160px}
.forum-row-latest-title a{color:var(--text)}.forum-row-latest-title a:hover{color:var(--cyan)}
.forum-row-latest-meta{font-size:11px;color:var(--muted);display:flex;gap:6px;margin-top:2px}

/* BADGES */
.pin-badge{font-family:var(--ff-title);font-size:7px;letter-spacing:1.5px;padding:2px 6px;border-radius:2px;vertical-align:middle;margin-left:6px;text-transform:uppercase}
.pin-badge.official{background:rgba(0,212,255,.1);color:var(--cyan);border:1px solid var(--cyan-dim)}
.pin-badge.hot{background:rgba(255,48,64,.1);color:var(--red);border:1px solid rgba(255,48,64,.2)}
.pin-badge.new{background:rgba(48,255,128,.1);color:var(--green);border:1px solid rgba(48,255,128,.2)}

/* TOPIC LIST */
.topic-list-header{display:grid;grid-template-columns:1fr 70px 70px 180px;gap:14px;padding:8px 16px;font-family:var(--ff-title);font-size:8px;letter-spacing:2px;color:var(--muted);text-transform:uppercase;border-bottom:1px solid var(--border)}
.topic-list-header span:nth-child(n+2){text-align:center}
.topic-list-header span:last-child{text-align:left}
.topic-row{display:grid;grid-template-columns:1fr 70px 70px 180px;align-items:center;gap:14px;padding:12px 16px;background:var(--bg2);border:1px solid var(--border);border-top:none;transition:background .12s;cursor:pointer}
.topic-row:hover{background:var(--bg3)}
.topic-row:last-child{border-radius:0 0 8px 8px}
.topic-info{display:flex;align-items:center;gap:10px}
.topic-avatar{width:36px;height:36px;border-radius:6px;flex-shrink:0;background:var(--bg);border:1px solid var(--border);display:grid;place-items:center;font-family:var(--ff-title);font-size:12px;color:var(--cyan-dim)}
.topic-title{font-size:14px;font-weight:600;color:var(--bright)}
.topic-title a{color:var(--bright)}.topic-title a:hover{color:var(--cyan)}
.topic-meta{font-size:11px;color:var(--muted);margin-top:2px}
.topic-stat{text-align:center;font-family:var(--ff-title);font-size:13px;color:var(--text)}
.topic-latest{display:flex;align-items:center;gap:8px}
.topic-latest-info{font-size:11px;color:var(--muted)}
.topic-latest-info a{color:var(--text);font-weight:500}

/* TOPIC DETAIL */
.topic-detail-title-row{display:flex;align-items:center;gap:12px;margin-bottom:16px;flex-wrap:wrap}
.topic-detail-h2{font-family:var(--ff-title);font-size:16px;letter-spacing:2px;color:var(--bright)}

/* POST LIST */
.post-list{display:flex;flex-direction:column;gap:8px;margin-bottom:20px}
.post-card{display:grid;grid-template-columns:110px 1fr;background:var(--bg2);border:1px solid var(--border);border-radius:8px;overflow:hidden}
.post-card.post-op{border-color:var(--cyan-dim)}
.post-author-col{background:var(--bg3);border-right:1px solid var(--border);padding:16px 12px;display:flex;flex-direction:column;align-items:center;gap:6px;text-align:center}
.post-avatar-lg{width:42px;height:42px;border-radius:8px;background:var(--cyan-dim);display:grid;place-items:center;font-family:var(--ff-title);font-size:16px;color:var(--cyan)}
.post-author-name{font-size:12px;color:var(--bright);font-weight:600;word-break:break-all}
.post-num{font-family:var(--ff-title);font-size:9px;color:var(--muted);letter-spacing:1px}
.post-content-col{padding:16px}
.post-header-row{display:flex;justify-content:flex-end;margin-bottom:10px}
.post-date{font-size:11px;color:var(--muted)}
.post-body{font-size:14px;color:var(--text);white-space:pre-wrap;line-height:1.7}

/* REPLY */
.reply-box{background:var(--bg2);border:1px solid var(--border);border-radius:8px;padding:20px;margin-top:8px}
.reply-title{font-family:var(--ff-title);font-size:9px;letter-spacing:2px;color:var(--muted);margin-bottom:12px}
.reply-textarea{width:100%;background:var(--bg3);border:1px solid var(--border);border-radius:6px;color:var(--bright);font-family:var(--ff);font-size:14px;padding:12px 14px;outline:none;resize:vertical;min-height:80px;transition:border .2s}
.reply-textarea:focus{border-color:var(--cyan-dim)}
.reply-footer{display:flex;justify-content:space-between;align-items:center;margin-top:10px}
.char-count{font-size:11px;color:var(--muted)}
.reply-login-prompt{text-align:center;padding:20px;font-size:13px;color:var(--muted);margin-top:8px;display:flex;align-items:center;justify-content:center;flex-wrap:wrap;gap:8px}
.link-btn{background:none;border:none;color:var(--cyan);font-family:var(--ff);font-size:12px;cursor:pointer;text-decoration:underline;padding:0}

/* ONLINE STRIP */
.online-strip{display:flex;align-items:center;gap:12px;padding:14px 18px;background:var(--bg2);border:1px solid var(--border);border-radius:8px;margin-top:24px;font-size:13px;flex-wrap:wrap}
.online-dot{width:8px;height:8px;border-radius:50%;background:var(--green);animation:pulse 2s ease-in-out infinite;flex-shrink:0}
@keyframes pulse{0%,100%{opacity:1}50%{opacity:.4}}
.online-count{font-weight:600;color:var(--bright)}
.online-names{color:var(--muted);font-size:12px}

/* PAGINATION */
.pagination{display:flex;justify-content:center;gap:4px;margin:28px 0}
.page-btn{font-family:var(--ff-title);font-size:11px;width:34px;height:34px;border-radius:4px;display:grid;place-items:center;background:var(--bg2);border:1px solid var(--border);color:var(--muted);cursor:pointer;transition:all .15s}
.page-btn:hover{border-color:var(--border-bright);color:var(--text)}
.page-btn.active{background:var(--cyan-dim);color:var(--cyan);border-color:var(--cyan-dark)}
.page-btn:disabled{opacity:.3;cursor:not-allowed}

/* FOOTER */
.forum-footer{margin-top:40px;padding:24px 0;border-top:1px solid var(--border);text-align:center}
.forum-footer p{font-size:12px;color:var(--muted)}
.forum-footer a{color:var(--cyan-dim)}

/* FORM (modal) */
.field-group{display:flex;flex-direction:column;gap:5px;margin-bottom:16px}
.field-label{font-family:var(--ff-title);font-size:9px;letter-spacing:2px;color:var(--muted)}
.req{color:var(--red)}
.field-input{width:100%;background:var(--bg3);border:1px solid var(--border);border-radius:6px;color:var(--bright);font-family:var(--ff);font-size:14px;padding:10px 14px;outline:none;transition:border .2s}
.field-input:focus{border-color:var(--cyan-dim)}
.field-textarea{resize:vertical;min-height:80px;font-family:var(--ff)}
select.field-input option{background:var(--bg3)}

/* MODAL */
.modal-overlay{position:fixed;inset:0;background:rgba(0,0,0,.75);backdrop-filter:blur(4px);z-index:200;display:flex;align-items:center;justify-content:center;padding:24px}
.modal-box{background:var(--bg2);border:1px solid var(--border);border-radius:12px;padding:28px;width:100%;max-width:600px;max-height:90vh;overflow-y:auto}
.modal-head{display:flex;align-items:center;justify-content:space-between;margin-bottom:24px}
.modal-title{font-family:var(--ff-title);font-size:12px;letter-spacing:3px;color:var(--bright)}
.modal-close{background:none;border:none;color:var(--muted);font-size:16px;cursor:pointer;padding:4px 8px;transition:color .2s}
.modal-close:hover{color:var(--bright)}
.modal-footer{display:flex;justify-content:flex-end;gap:10px;margin-top:8px}

/* STATES */
.state-msg{color:var(--muted);font-size:13px;padding:24px 0}
.state-empty{display:flex;flex-direction:column;align-items:center;gap:14px;padding:60px 24px;text-align:center;font-size:14px;color:var(--muted)}

/* TOAST */
.toast{position:fixed;bottom:24px;right:24px;background:var(--bg3);border:1px solid var(--border-bright);border-radius:8px;padding:12px 20px;font-size:13px;color:var(--bright);z-index:999;animation:toastIn .25s ease}
.toast.error{border-color:var(--red);color:var(--red)}
.toast.success{border-color:var(--green);color:var(--green)}
@keyframes toastIn{from{opacity:0;transform:translateY(10px)}to{opacity:1;transform:translateY(0)}}

/* TOPBAR AUTH */
.btn-topbar{font-size:9px;padding:6px 14px;white-space:nowrap}
.topbar-logout-btn{background:none;border:none;color:var(--muted);font-size:14px;cursor:pointer;padding:0 2px;line-height:1;transition:color .15s;margin-left:4px}
.topbar-logout-btn:hover{color:var(--red)}

/* AUTH MODAL */
.auth-modal-box{max-width:420px}
.auth-logo{text-align:center;margin-bottom:20px}
.auth-radiation{width:36px;height:36px;border:2px solid var(--cyan-dim);border-top-color:var(--cyan);border-radius:50%;display:grid;place-items:center;animation:spin 8s linear infinite;font-size:16px;margin:0 auto 10px}
.auth-brand-title{font-family:var(--ff-title);font-size:13px;letter-spacing:3px;color:var(--cyan)}
.auth-brand-sub{font-family:var(--ff-title);font-size:9px;letter-spacing:2px;color:var(--muted);margin-top:3px}
.mode-toggle{display:flex;gap:4px;background:var(--bg3);border:1px solid var(--border);border-radius:6px;padding:3px}
.mode-btn{flex:1;font-family:var(--ff-title);font-size:10px;letter-spacing:2px;padding:7px;border-radius:4px;background:none;border:none;color:var(--muted);cursor:pointer;transition:all .15s}
.mode-btn--active{background:var(--cyan-dim);color:var(--cyan)}
.auth-error{display:flex;align-items:center;gap:8px;padding:10px 12px;background:rgba(255,48,64,.07);border:1px solid rgba(255,48,64,.15);border-radius:6px;font-size:13px;color:var(--red);margin:12px 0}
.auth-progress{margin-top:14px}
.auth-progress-bar{height:2px;background:var(--bg3);border-radius:1px;overflow:hidden}
.auth-progress-fill{height:100%;background:var(--cyan);transition:width .4s ease;border-radius:1px}
.auth-progress-label{font-family:var(--ff-title);font-size:8px;letter-spacing:2px;color:var(--muted);margin-top:6px;text-align:center}

/* RESPONSIVE */
@media(max-width:900px){
  .forum-row{grid-template-columns:40px 1fr 180px;gap:10px}
  .forum-row-stat{display:none}
  .topic-row{grid-template-columns:1fr 50px 50px;gap:10px}
  .topic-row .topic-latest{display:none}
  .hero-stats{display:none}
}
@media(max-width:600px){
  .topbar-nav{display:none}
  .topbar-search{display:none}
  .forum-row{grid-template-columns:1fr;gap:8px}
  .forum-row-icon,.forum-row-stat,.forum-row-latest{display:none}
  .wrap{padding:0 12px}
}
</style>
