<template>
  <div class="forum-view fade-in">

    <!-- ── FORUM INDEX ─────────────────────────────────────────────────── -->
    <div v-if="view === 'index'">
      <div class="page-header" style="display:flex;align-items:center;justify-content:space-between">
        <div>
          <h2 class="page-title">COMMUNITY FORUM</h2>
          <p class="page-subtitle">Discuss strategy, share guides and connect with survivors</p>
        </div>
        <button class="action-btn" @click="openNewTopicModal(null)">+ NEW TOPIC</button>
      </div>
      <div class="accent-line" style="margin-bottom:16px" />

      <!-- Stats strip -->
      <div class="stats-strip">
        <div class="stat-item">
          <span class="stat-val">{{ stats.totalTopics }}</span>
          <span class="stat-lbl">TOPICS</span>
        </div>
        <div class="stat-div"></div>
        <div class="stat-item">
          <span class="stat-val">{{ stats.totalPosts }}</span>
          <span class="stat-lbl">POSTS</span>
        </div>
        <div class="stat-div"></div>
        <div class="stat-item">
          <span class="stat-val">{{ onlineUsers.length }}</span>
          <span class="stat-lbl">ONLINE</span>
        </div>
      </div>

      <!-- Category groups -->
      <div v-for="group in CATEGORY_GROUPS" :key="group.label" class="cat-group">
        <div class="cat-group-head">
          <span>{{ group.icon }}</span>{{ group.label }}
        </div>
        <div class="cat-list">
          <div
            v-for="cat in group.categories" :key="cat.key"
            class="cat-row" @click="openCategory(cat)"
          >
            <div class="cat-icon">{{ cat.icon }}</div>
            <div class="cat-info">
              <div class="cat-name">
                {{ cat.name }}
                <span v-if="cat.badge" class="cat-badge" :class="'badge-' + cat.badge.toLowerCase()">{{ cat.badge }}</span>
              </div>
              <div class="cat-desc">{{ cat.desc }}</div>
            </div>
            <div class="cat-counts">
              <div class="cat-count">
                <div class="cc-val">{{ catStat(cat.key).topics }}</div>
                <div class="cc-lbl">Topics</div>
              </div>
              <div class="cat-count">
                <div class="cc-val">{{ catStat(cat.key).posts }}</div>
                <div class="cc-lbl">Posts</div>
              </div>
            </div>
            <div class="cat-latest">
              <template v-if="catStat(cat.key).latestTitle">
                <div class="cl-title">{{ catStat(cat.key).latestTitle }}</div>
                <div class="cl-meta">{{ catStat(cat.key).latestAuthor }} · {{ fmtDate(catStat(cat.key).latestDate) }}</div>
              </template>
              <span v-else class="cl-empty">No topics yet</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Online users strip -->
      <div v-if="onlineUsers.length" class="online-strip">
        <span class="online-label">ONLINE:</span>
        <span v-for="u in onlineUsers" :key="u" class="online-chip">
          <span class="online-dot"></span>{{ u }}
        </span>
      </div>
    </div>

    <!-- ── CATEGORY / TOPIC LIST ────────────────────────────────────────── -->
    <div v-if="view === 'category'">
      <div class="page-header" style="display:flex;align-items:center;justify-content:space-between">
        <div>
          <nav class="breadcrumb">
            <a href="#" @click.prevent="view = 'index'">Forum</a>
            <span class="bc-sep">›</span>
            <span>{{ activeCategory?.name }}</span>
          </nav>
          <h2 class="page-title" style="margin-top:4px">{{ activeCategory?.name }}</h2>
        </div>
        <button class="action-btn" @click="openNewTopicModal(activeCategory)">+ NEW TOPIC</button>
      </div>
      <div class="accent-line" style="margin-bottom:16px" />

      <div v-if="loadingTopics" class="loading-msg">Loading topics...</div>

      <div v-else-if="topics.length === 0" class="empty-panel">
        <div class="empty-icon">💬</div>
        <div class="empty-title">NO TOPICS YET</div>
        <div class="empty-sub">Be the first to start a discussion.</div>
        <button class="action-btn" @click="openNewTopicModal(activeCategory)">CREATE FIRST TOPIC</button>
      </div>

      <div v-else>
        <div class="topics-head-row">
          <div></div>
          <div>TOPIC</div>
          <div class="th-center">POSTS</div>
          <div>LAST POST</div>
        </div>
        <div class="topics-list">
          <div
            v-for="t in topics" :key="t.id"
            class="topic-row"
            :class="{ pinned: t.isPinned, official: t.isOfficial }"
            @click="openTopic(t)"
          >
            <div class="tr-icon">
              <span v-if="t.isPinned">📌</span>
              <span v-else-if="t.isOfficial">🛡️</span>
              <span v-else>💬</span>
            </div>
            <div class="tr-info">
              <div class="tr-title">{{ t.title }}</div>
              <div class="tr-meta">by {{ t.authorUsername }} · {{ fmtDate(t.createdAtUtc) }}</div>
            </div>
            <div class="tr-posts">{{ t.postCount ?? 0 }}</div>
            <div class="tr-last">
              <div class="tl-author">{{ t.lastPostUsername ?? t.authorUsername }}</div>
              <div class="tl-date">{{ fmtDate(t.lastPostAtUtc ?? t.createdAtUtc) }}</div>
            </div>
          </div>
        </div>

        <div v-if="totalPages > 1" class="pagination">
          <button class="secondary-btn" :disabled="page === 1" @click="loadTopics(page - 1)">← PREV</button>
          <span class="page-info">{{ page }} / {{ totalPages }}</span>
          <button class="secondary-btn" :disabled="page === totalPages" @click="loadTopics(page + 1)">NEXT →</button>
        </div>
      </div>
    </div>

    <!-- ── TOPIC DETAIL ───────────────────────────────────────────────── -->
    <div v-if="view === 'topic'">
      <div class="page-header">
        <nav class="breadcrumb">
          <a href="#" @click.prevent="view = 'index'">Forum</a>
          <span class="bc-sep">›</span>
          <a href="#" @click.prevent="view = 'category'">{{ activeCategory?.name }}</a>
          <span class="bc-sep">›</span>
          <span>{{ activeTopic?.title }}</span>
        </nav>
      </div>

      <div v-if="loadingTopic" class="loading-msg">Loading topic...</div>
      <div v-else-if="activeTopic">
        <div class="topic-detail-head">
          <h2 class="page-title">{{ activeTopic.title }}</h2>
          <div style="display:flex;gap:8px;margin-top:6px">
            <span v-if="activeTopic.isPinned"  class="cat-badge badge-official">📌 PINNED</span>
            <span v-if="activeTopic.isOfficial" class="cat-badge badge-official">🛡️ OFFICIAL</span>
          </div>
        </div>
        <div class="accent-line" style="margin-bottom:16px" />

        <div class="posts-list">
          <div v-for="(post, idx) in activeTopic.posts" :key="post.id" class="post-card" :class="{ 'post-first': idx === 0 }">
            <div class="post-author">
              <div class="post-avatar">{{ post.authorUsername?.charAt(0).toUpperCase() }}</div>
              <div class="post-author-name">{{ post.authorUsername }}</div>
              <div class="post-num">#{{ idx + 1 }}</div>
            </div>
            <div class="post-body-col">
              <div class="post-date">{{ fmtDateTime(post.createdAtUtc) }}</div>
              <div class="post-body">{{ post.content }}</div>
            </div>
          </div>
        </div>

        <!-- Reply form -->
        <div class="reply-box">
          <div class="reply-head">POST REPLY</div>
          <textarea v-model="replyContent" class="field-input field-textarea" rows="5" placeholder="Write your reply..." maxlength="10000"></textarea>
          <div class="reply-actions">
            <span class="char-count">{{ replyContent.length }} / 10000</span>
            <button class="action-btn" @click="submitReply" :disabled="submittingReply || !replyContent.trim()">
              {{ submittingReply ? 'POSTING...' : 'POST REPLY' }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- ── NEW TOPIC MODAL ───────────────────────────────────────────── -->
    <Teleport to="body">
      <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
        <div class="modal-box">
          <div class="modal-head">
            <h3 class="modal-title">NEW TOPIC</h3>
            <button class="modal-close" @click="showModal = false">✕</button>
          </div>

          <div class="form-field">
            <label class="field-label">CATEGORY</label>
            <select v-model="newTopic.categoryKey" class="field-input">
              <option v-for="c in ALL_CATEGORIES" :key="c.key" :value="c.key">{{ c.name }}</option>
            </select>
          </div>
          <div class="form-field">
            <label class="field-label">TITLE <span class="req">*</span></label>
            <input v-model="newTopic.title" class="field-input" placeholder="Topic title" maxlength="200" />
          </div>
          <div class="form-field">
            <label class="field-label">MESSAGE <span class="req">*</span></label>
            <textarea v-model="newTopic.content" class="field-input field-textarea" rows="7" placeholder="Write your opening post..." maxlength="10000"></textarea>
          </div>

          <div class="modal-footer">
            <button class="secondary-btn" @click="showModal = false">CANCEL</button>
            <button class="action-btn" @click="submitNewTopic" :disabled="submittingTopic || !newTopic.title.trim() || !newTopic.content.trim()">
              {{ submittingTopic ? 'POSTING...' : 'CREATE TOPIC' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

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

// ── Navigation ────────────────────────────────────────────────────────────
const view = ref('index')  // 'index' | 'category' | 'topic'
const activeCategory = ref(null)
const activeTopic = ref(null)

// ── Category definitions ──────────────────────────────────────────────────
const ALL_CATEGORIES = [
  { key: 'announcements',      name: 'Announcements',       icon: '📢', desc: 'Official news from the dev team.',           badge: 'OFFICIAL' },
  { key: 'release-notes',      name: 'Release Notes',        icon: '📋', desc: 'Patch notes, changelogs and updates.' },
  { key: 'suggestions',        name: 'Suggestions',          icon: '🗳️', desc: 'Feature requests and improvement ideas.',    badge: 'HOT' },
  { key: 'ideas-vote',         name: 'Ideas — Up for Vote',  icon: '👍', desc: 'Community-voted features and proposals.' },
  { key: 'general-feedback',   name: 'General Feedback',     icon: '💬', desc: 'Share your general impressions.' },
  { key: 'q-and-a',            name: 'Questions & Answers',  icon: '❓', desc: 'Ask anything about the game.' },
  { key: 'guides',             name: 'Guides & Tutorials',   icon: '🗺️', desc: 'Player-written guides and tutorials.' },
  { key: 'strategy',           name: 'Strategy Discussion',  icon: '⚔️', desc: 'Talk tactics, meta and high-level play.' },
  { key: 'introductions',      name: 'Introductions',        icon: '👋', desc: 'Introduce yourself to the community.' },
  { key: 'off-topic',          name: 'The Watering Hole',    icon: '🍺', desc: 'Off-topic chat and general banter.' },
  { key: 'alliance-recruitment', name: 'Alliance Recruitment', icon: '🏴', desc: 'Looking for an alliance or recruiting?', badge: 'NEW' },
]
const CATEGORY_GROUPS = [
  { label: 'OFFICIAL',         icon: '🛡️', categories: ALL_CATEGORIES.filter(c => ['announcements','release-notes'].includes(c.key)) },
  { label: 'FEEDBACK & IDEAS', icon: '💡', categories: ALL_CATEGORIES.filter(c => ['suggestions','ideas-vote','general-feedback'].includes(c.key)) },
  { label: 'HELP & STRATEGY',  icon: '⚔️', categories: ALL_CATEGORIES.filter(c => ['q-and-a','guides','strategy'].includes(c.key)) },
  { label: 'COMMUNITY',        icon: '👥', categories: ALL_CATEGORIES.filter(c => ['introductions','off-topic','alliance-recruitment'].includes(c.key)) },
]

// ── Remote data ───────────────────────────────────────────────────────────
const stats = ref({ totalTopics: 0, totalPosts: 0 })
const categoryStatsMap = ref({})
const onlineUsers = ref([])
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
  return categoryStatsMap.value[key] ?? { topics: 0, posts: 0, latestTitle: null, latestAuthor: null, latestDate: null }
}
function fmtDate(utc) {
  if (!utc) return ''
  return new Date(utc).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
}
function fmtDateTime(utc) {
  if (!utc) return ''
  return new Date(utc).toLocaleString('en-GB', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
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
        topics:      c.topicCount ?? 0,
        posts:       c.postCount  ?? 0,
        latestTitle:  c.latestTopicTitle  ?? null,
        latestAuthor: c.latestTopicAuthor ?? null,
        latestDate:   c.latestTopicDate   ?? null,
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
    const res = await fetch(`/api/forum/topics?category=${activeCategory.value.key}&page=${p}&pageSize=20`)
    if (res.ok) {
      const data = await res.json()
      topics.value = data.items ?? data
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
  activeCategory.value = cat
  view.value = 'category'
  loadTopics(1)
}

async function openTopic(topic) {
  activeTopic.value = null
  replyContent.value = ''
  view.value = 'topic'
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
        playerId: props.player?.id,
        authorUsername: props.player?.username ?? 'Unknown',
        categoryKey: newTopic.value.categoryKey,
        title: newTopic.value.title,
        content: newTopic.value.content,
      }),
    })
    if (!res.ok) throw new Error()
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
        playerId: props.player?.id,
        authorUsername: props.player?.username ?? 'Unknown',
        content: replyContent.value,
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

<style scoped>
.forum-view { padding-bottom: 40px; }

/* stats strip */
.stats-strip {
  display: flex;
  align-items: center;
  gap: 0;
  background: var(--panel-bg, #0a1018);
  border: 1px solid var(--border);
  border-radius: 8px;
  padding: 12px 24px;
  margin-bottom: 20px;
  width: fit-content;
}
.stat-item { display: flex; flex-direction: column; align-items: center; padding: 0 20px; }
.stat-val { font-family: var(--ff-title); font-size: 20px; color: var(--cyan, #00d4ff); }
.stat-lbl { font-family: var(--ff-title); font-size: 9px; letter-spacing: 2px; color: var(--muted); }
.stat-div { width: 1px; height: 32px; background: var(--border); }

/* category groups */
.cat-group { margin-bottom: 16px; background: var(--panel-bg, #0a1018); border: 1px solid var(--border); border-radius: 8px; overflow: hidden; }
.cat-group-head {
  background: var(--input-bg, #0e1620);
  border-bottom: 1px solid var(--border);
  padding: 8px 16px;
  font-family: var(--ff-title);
  font-size: 9px;
  letter-spacing: 3px;
  color: var(--muted);
  display: flex;
  align-items: center;
  gap: 8px;
}
.cat-list { display: flex; flex-direction: column; }
.cat-row {
  display: grid;
  grid-template-columns: 40px 1fr 130px 180px;
  align-items: center;
  gap: 12px;
  padding: 12px 16px;
  cursor: pointer;
  border-bottom: 1px solid var(--border);
  transition: background .2s;
}
.cat-row:last-child { border-bottom: none; }
.cat-row:hover { background: rgba(0,212,255,.06); }
.cat-icon { font-size: 20px; text-align: center; }
.cat-name { font-size: 14px; color: var(--bright); font-weight: 600; display: flex; align-items: center; gap: 8px; }
.cat-desc { font-size: 12px; color: var(--muted); margin-top: 2px; }
.cat-badge { font-family: var(--ff-title); font-size: 8px; letter-spacing: 1px; padding: 2px 6px; border-radius: 3px; }
.badge-official { background: rgba(0,212,255,.08); color: #00d4ff; border: 1px solid #004466; }
.badge-hot      { background: rgba(255,128,64,.08); color: #ff8040; border: 1px solid rgba(255,128,64,.3); }
.badge-new      { background: rgba(48,255,128,.08); color: #30ff80; border: 1px solid #0a3318; }
.cat-counts { display: flex; gap: 16px; }
.cat-count { text-align: center; }
.cc-val { font-family: var(--ff-title); font-size: 14px; color: var(--bright); }
.cc-lbl { font-size: 10px; color: var(--muted); }
.cat-latest { font-size: 12px; overflow: hidden; }
.cl-title { color: var(--text); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.cl-meta  { color: var(--muted); font-size: 11px; margin-top: 2px; }
.cl-empty { color: var(--muted); }

/* online strip */
.online-strip {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
  padding: 10px 14px;
  background: var(--panel-bg, #0a1018);
  border: 1px solid var(--border);
  border-radius: 6px;
  font-size: 13px;
  margin-top: 8px;
}
.online-label { font-family: var(--ff-title); font-size: 9px; letter-spacing: 2px; color: var(--muted); }
.online-chip  { display: flex; align-items: center; gap: 5px; color: var(--text); }
.online-dot   { width: 6px; height: 6px; border-radius: 50%; background: #30ff80; flex-shrink: 0; }

/* breadcrumb */
.breadcrumb { display: flex; align-items: center; gap: 6px; font-size: 13px; color: var(--muted); margin-bottom: 4px; }
.breadcrumb a { color: var(--cyan, #00d4ff); text-decoration: none; }
.breadcrumb a:hover { color: var(--bright); }
.bc-sep { opacity: .5; }

/* topics list */
.topics-head-row {
  display: grid;
  grid-template-columns: 32px 1fr 80px 160px;
  gap: 12px;
  padding: 6px 14px;
  font-family: var(--ff-title);
  font-size: 9px;
  letter-spacing: 2px;
  color: var(--muted);
  border-bottom: 1px solid var(--border);
}
.th-center { text-align: center; }
.topics-list { border: 1px solid var(--border); border-radius: 8px; overflow: hidden; margin-bottom: 16px; }
.topic-row {
  display: grid;
  grid-template-columns: 32px 1fr 80px 160px;
  align-items: center;
  gap: 12px;
  padding: 12px 14px;
  background: var(--panel-bg, #0a1018);
  border-bottom: 1px solid var(--border);
  cursor: pointer;
  transition: background .2s;
}
.topic-row:last-child { border-bottom: none; }
.topic-row:hover { background: rgba(0,212,255,.06); }
.topic-row.pinned   { border-left: 3px solid var(--cyan-dim, #004466); }
.topic-row.official { border-left: 3px solid #00d4ff; }
.tr-icon  { font-size: 14px; text-align: center; }
.tr-title { font-size: 14px; color: var(--bright); font-weight: 600; }
.tr-meta  { font-size: 11px; color: var(--muted); margin-top: 2px; }
.tr-posts { font-family: var(--ff-title); font-size: 13px; color: var(--text); text-align: center; }
.tr-last  { font-size: 12px; }
.tl-author { color: var(--text); }
.tl-date   { color: var(--muted); }

/* pagination */
.pagination { display: flex; align-items: center; justify-content: center; gap: 16px; padding: 16px 0; }
.page-info  { font-family: var(--ff-title); font-size: 10px; letter-spacing: 2px; color: var(--muted); }

/* topic detail */
.topic-detail-head { margin-bottom: 8px; }

/* posts */
.posts-list { display: flex; flex-direction: column; gap: 8px; margin-bottom: 20px; }
.post-card {
  display: grid;
  grid-template-columns: 110px 1fr;
  background: var(--panel-bg, #0a1018);
  border: 1px solid var(--border);
  border-radius: 8px;
  overflow: hidden;
}
.post-card.post-first { border-color: var(--cyan-dim, #004466); }
.post-author {
  background: var(--input-bg, #0e1620);
  border-right: 1px solid var(--border);
  padding: 14px 12px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  text-align: center;
}
.post-avatar {
  width: 38px; height: 38px;
  border-radius: 6px;
  background: var(--cyan-dim, #004466);
  display: grid; place-items: center;
  font-family: var(--ff-title); font-size: 14px;
  color: #00d4ff;
}
.post-author-name { font-size: 12px; color: var(--bright); font-weight: 600; word-break: break-all; }
.post-num         { font-family: var(--ff-title); font-size: 9px; color: var(--muted); letter-spacing: 1px; }
.post-body-col    { padding: 14px 16px; }
.post-date        { font-size: 11px; color: var(--muted); text-align: right; margin-bottom: 8px; }
.post-body        { font-size: 14px; color: var(--text); white-space: pre-wrap; line-height: 1.7; }

/* reply */
.reply-box { background: var(--panel-bg, #0a1018); border: 1px solid var(--border); border-radius: 8px; padding: 18px; }
.reply-head { font-family: var(--ff-title); font-size: 9px; letter-spacing: 2px; color: var(--muted); margin-bottom: 10px; }
.reply-actions { display: flex; align-items: center; justify-content: space-between; margin-top: 10px; }

/* shared form elements (used in modal too) */
.form-field { display: flex; flex-direction: column; gap: 5px; margin-bottom: 14px; }
.field-label{ font-family: var(--ff-title); font-size: 9px; letter-spacing: 2px; color: var(--muted); }
.req { color: #ff3040; }
.field-input {
  width: 100%;
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
.char-count { font-size: 11px; color: var(--muted); }

/* buttons */
.action-btn {
  font-family: var(--ff-title); font-size: 10px; letter-spacing: 2px;
  padding: 9px 20px; border-radius: 4px; cursor: pointer;
  border: 1px solid var(--cyan-dim, #004466);
  background: var(--cyan-dim, #004466);
  color: var(--cyan, #00d4ff); transition: all .2s;
}
.action-btn:hover { background: var(--cyan-dark, #0088aa); color: #e8f4ff; }
.action-btn:disabled { opacity: .4; cursor: not-allowed; }
.secondary-btn {
  font-family: var(--ff-title); font-size: 10px; letter-spacing: 2px;
  padding: 9px 20px; border-radius: 4px; cursor: pointer;
  background: none; border: 1px solid var(--border); color: var(--muted); transition: all .2s;
}
.secondary-btn:hover { color: var(--text); border-color: var(--border-bright, #1a3048); }
.secondary-btn:disabled { opacity: .4; cursor: not-allowed; }

/* empty state */
.empty-panel { display: flex; flex-direction: column; align-items: center; gap: 12px; padding: 60px 24px; text-align: center; }
.empty-icon  { font-size: 36px; }
.empty-title { font-family: var(--ff-title); font-size: 12px; letter-spacing: 3px; color: var(--bright); }
.empty-sub   { font-size: 13px; color: var(--muted); }
.loading-msg { color: var(--muted); font-size: 13px; padding: 20px 0; }

/* modal */
.modal-overlay {
  position: fixed; inset: 0;
  background: rgba(0,0,0,.7);
  backdrop-filter: blur(4px);
  z-index: 200;
  display: flex; align-items: center; justify-content: center;
  padding: 24px;
}
.modal-box {
  background: var(--bg2, #0a1018);
  border: 1px solid var(--border);
  border-radius: 12px;
  padding: 26px;
  width: 100%; max-width: 580px;
  max-height: 90vh; overflow-y: auto;
}
.modal-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 20px; }
.modal-title{ font-family: var(--ff-title); font-size: 12px; letter-spacing: 3px; color: var(--bright); }
.modal-close{ background: none; border: none; color: var(--muted); font-size: 15px; cursor: pointer; padding: 4px 8px; transition: color .2s; }
.modal-close:hover { color: var(--bright); }
.modal-footer{ display: flex; justify-content: flex-end; gap: 10px; margin-top: 4px; }

/* toast */
.report-toast {
  position: fixed; bottom: 24px; right: 24px;
  background: var(--input-bg, #0e1620);
  border: 1px solid var(--border-bright, #1a3048);
  border-radius: 8px; padding: 12px 20px;
  font-size: 13px; color: var(--bright); z-index: 999;
  animation: fToastIn .25s ease;
}
.report-toast.error   { border-color: #ff3040; color: #ff3040; }
.report-toast.success { border-color: #30ff80; color: #30ff80; }
@keyframes fToastIn { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }
</style>
