<template>
  <div class="messages-wrap">
    <div class="messages-header">
      <div class="messages-header-left">
        <h2 class="messages-title">MESSAGES</h2>
        <span class="messages-sub">CLASSIFIED</span>
      </div>

      <button class="compose-top-btn" @click="startCompose">
        + NEW MESSAGE
      </button>
    </div>

    <div class="messages-layout">
      <aside class="messages-sidebar">
        <div class="sidebar-top sidebar-top--tabs">
          <button
              class="sidebar-tab"
              :class="{ 'sidebar-tab--active': activeFolder === 'inbox' }"
              @click="switchFolder('inbox')"
          >
            INBOX
            <span class="sidebar-count">{{ inboxMessages.length }}</span>
          </button>

          <button
              class="sidebar-tab"
              :class="{ 'sidebar-tab--active': activeFolder === 'sent' }"
              @click="switchFolder('sent')"
          >
            SENT
            <span class="sidebar-count">{{ sentMessages.length }}</span>
          </button>

          <button
              class="sidebar-tab"
              :class="{ 'sidebar-tab--active': activeFolder === 'reports' }"
              @click="switchFolder('reports')"
          >
            REPORTS
            <span class="sidebar-count">{{ reportMessages.length }}</span>
          </button>
        </div>

        <div v-if="activeMessages.length === 0" class="msg-list-empty">
          {{ activeFolder === 'reports' ? 'No battle or scout reports yet.' : (activeFolder === 'inbox' ? 'No messages received.' : 'No sent messages.') }}
        </div>

        <div v-else class="msg-list">
          <button
              v-for="msg in activeMessages"
              :key="msg.id"
              class="msg-list-item"
              :class="{
              'msg-list-item--active': selectedMessage?.id === msg.id && !composeMode,
              'msg-list-item--unread': activeFolder === 'inbox' && !msg.isRead
            }"
              @click="selectMessage(msg)"
          >
            <div class="msg-list-top">
              <span class="msg-from">
                {{ activeFolder === 'inbox' ? msg.senderName : msg.receiverName }}
              </span>
              <span class="msg-date">{{ formatDate(msg.sentAtUtc) }}</span>
            </div>

            <div class="msg-subject">
              <span v-if="activeFolder === 'inbox' && !msg.isRead" class="msg-unread-dot"></span>
              <span v-if="getMessageIcon(msg)" class="msg-type-icon">{{ getMessageIcon(msg) }}</span>
              {{ msg.subject }}
            </div>

            <div class="msg-preview">
              {{ msg.body }}
            </div>
          </button>
        </div>
      </aside>

      <section class="messages-content">
        <div v-if="composeMode" class="compose-box">
          <div class="content-topbar">
            <div>
              <div class="content-title">COMPOSE TRANSMISSION</div>
              <div class="content-sub">Secure player-to-player channel</div>
            </div>
          </div>

          <div class="compose-grid">
            <div class="compose-row compose-row--recipient">
              <label>TO</label>

              <input
                  v-model="playerSearch"
                  placeholder="Type player name..."
                  autocomplete="off"
                  @input="onRecipientInput"
                  @keydown="onRecipientKeydown"
                  @focus="onRecipientFocus"
                  @blur="onRecipientBlur"
              />

              <div v-if="showPlayerDropdown" class="recipient-dropdown">
                <button
                    v-for="(player, index) in playerResults"
                    :key="player.id"
                    type="button"
                    class="recipient-option"
                    :class="{ 'recipient-option--active': index === highlightedIndex }"
                    @mousedown.prevent="selectRecipient(player)"
                >
                  {{ player.username }}
                </button>
              </div>
            </div>

            <div class="compose-row">
              <label>SUBJECT</label>
              <input v-model="compose.subject" placeholder="Enter subject..." />
            </div>

            <div class="compose-row compose-row--body">
              <label>MESSAGE</label>
              <textarea v-model="compose.body" placeholder="Write your transmission..." />
            </div>
          </div>

          <div v-if="sendError" class="msg-error">{{ sendError }}</div>
          <div v-if="sendSuccess" class="msg-success">{{ sendSuccess }}</div>

          <div class="compose-actions">
            <button class="msg-btn msg-btn--ghost" @click="cancelCompose">
              CANCEL
            </button>
            <button class="msg-btn" @click="sendMessage" :disabled="sending">
              {{ sending ? 'SENDING...' : 'SEND MESSAGE' }}
            </button>
          </div>
        </div>

        <div v-else-if="selectedMessage" class="message-detail">
          <div class="content-topbar content-topbar--detail">
            <div>
              <div class="content-title">{{ selectedMessage.subject }}</div>
              <div class="content-sub">Encrypted settlement communication</div>
            </div>
            <button class="msg-delete-btn" @click="deleteCurrentMessage" title="Delete message">🗑 Delete</button>
          </div>

          <div class="detail-meta-grid">
            <div class="detail-stat">
              <div class="detail-stat-label">FROM</div>
              <div class="detail-stat-value">{{ selectedMessage.senderName }}</div>
            </div>

            <div class="detail-stat">
              <div class="detail-stat-label">TO</div>
              <div class="detail-stat-value">{{ selectedMessage.receiverName }}</div>
            </div>

            <div class="detail-stat">
              <div class="detail-stat-label">DATE</div>
              <div class="detail-stat-value">{{ formatDate(selectedMessage.sentAtUtc, true) }}</div>
            </div>
          </div>

          <div class="detail-body">
            {{ selectedMessage.body }}
          </div>

          <div class="compose-actions">
            <button
                v-if="activeFolder === 'inbox'"
                class="msg-btn"
                @click="replyToMessage(selectedMessage)"
            >
              REPLY
            </button>

            <button
                v-else
                class="msg-btn"
                @click="messageAgain(selectedMessage)"
            >
              MESSAGE AGAIN
            </button>
          </div>
        </div>

        <div v-else class="messages-empty">
          <div class="messages-empty-icon">✉</div>
          <div class="messages-empty-title">NO MESSAGE SELECTED</div>
          <div class="messages-empty-sub">
            Select a transmission from the inbox or start a new one.
          </div>
          <button class="msg-btn" @click="startCompose">COMPOSE MESSAGE</button>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import {
  getInboxMessages,
  getSentMessages,
  sendPlayerMessage,
  searchPlayers,
  getUnreadMessageCount,
  markMessageAsRead,
  getReportMessages,
  deleteMessage
} from '../services/api.js'

const route = useRoute()

const inboxMessages = ref([])
const sentMessages = ref([])
const reportMessages = ref([])
const activeFolder = ref('inbox')

const selectedMessage = ref(null)
const composeMode = ref(false)
const sending = ref(false)
const sendError = ref('')
const sendSuccess = ref('')

const playerSearch = ref('')
const playerResults = ref([])
const highlightedIndex = ref(-1)
const showPlayerDropdown = ref(false)
let searchTimer = null

const compose = ref({
  toPlayerId: '',
  toPlayerName: '',
  subject: '',
  body: ''
})

const activeMessages = computed(() => {
  if (activeFolder.value === 'reports') return reportMessages.value
  return activeFolder.value === 'inbox' ? inboxMessages.value : sentMessages.value
})

async function emitUnreadRefresh() {
  try {
    const playerId = sessionStorage.getItem('playerId')
    const result = await getUnreadMessageCount(playerId)
    window.dispatchEvent(
        new CustomEvent('messages-unread-updated', {
          detail: { count: result?.count ?? 0 }
        })
    )
  } catch (err) {
    console.error('Failed to refresh unread count', err)
  }
}

async function loadInbox() {
  try {
    const playerId = sessionStorage.getItem('playerId')
    const raw = await getInboxMessages(playerId)
    inboxMessages.value = (raw ?? []).map(m => ({
      ...m,
      id: m.id ?? m.Id,
      messageType: m.messageType ?? m.MessageType ?? 'message'
    }))
  } catch (err) {
    console.error('Failed to load inbox messages', err)
  }
}

async function loadSent() {
  try {
    const playerId = sessionStorage.getItem('playerId')
    const rawSent = await getSentMessages(playerId)
    sentMessages.value = (rawSent ?? []).map(m => ({
      ...m,
      id: m.id ?? m.Id,
      messageType: m.messageType ?? m.MessageType ?? 'message'
    }))
  } catch (err) {
    console.error('Failed to load sent messages', err)
  }
}

async function loadReports() {
  try {
    const playerId = sessionStorage.getItem('playerId')
    if (!playerId) { reportMessages.value = []; return }
    const rawReports = await getReportMessages(playerId)
    reportMessages.value = (rawReports ?? []).map(m => ({
      ...m,
      id: m.id ?? m.Id,
      messageType: m.messageType ?? m.MessageType ?? 'report'
    }))
  } catch {
    reportMessages.value = []
  }
}

async function loadAllMessages() {
  await Promise.all([loadInbox(), loadSent(), loadReports()])
  await emitUnreadRefresh()
}

async function deleteCurrentMessage() {
  if (!selectedMessage.value) return
  const playerId = sessionStorage.getItem('playerId')
  if (!playerId) return
  try {
    console.log('Deleting message id:', selectedMessage.value.id, 'raw:', selectedMessage.value)
    await deleteMessage(selectedMessage.value.id, playerId)
    inboxMessages.value = inboxMessages.value.filter(m => m.id !== selectedMessage.value.id)
    sentMessages.value = sentMessages.value.filter(m => m.id !== selectedMessage.value.id)
    reportMessages.value = reportMessages.value.filter(m => m.id !== selectedMessage.value.id)
    selectedMessage.value = null
  } catch (e) {
    console.error('Delete failed', e)
  }
}

function getMessageIcon(msg) {
  const subject = (msg.subject ?? '').toLowerCase()
  const type = (msg.messageType ?? '').toLowerCase()
  if (type === 'report' || type === 'notification') {
    if (subject.includes('scout')) return '👁'
    if (subject.includes('battle') || subject.includes('attack') || subject.includes('raid')) return '⚔️'
    if (subject.includes('vault') || subject.includes('raided')) return '🧬'
    return '📋'
  }
  return null
}

function switchFolder(folder) {
  activeFolder.value = folder
  composeMode.value = false
  selectedMessage.value = null
  sendError.value = ''
  sendSuccess.value = ''
}

async function selectMessage(msg) {
  composeMode.value = false
  selectedMessage.value = msg
  sendError.value = ''
  sendSuccess.value = ''

  if (activeFolder.value === 'inbox' && !msg.isRead) {
    try {
      await markMessageAsRead(msg.id)
      msg.isRead = true

      const target = inboxMessages.value.find(m => m.id === msg.id)
      if (target) target.isRead = true

      await emitUnreadRefresh()
    } catch (err) {
      console.error('Failed to mark message as read', err)
    }
  }
}

function resetComposeState() {
  compose.value = {
    toPlayerId: '',
    toPlayerName: '',
    subject: '',
    body: ''
  }

  playerSearch.value = ''
  playerResults.value = []
  highlightedIndex.value = -1
  showPlayerDropdown.value = false
}

function startCompose() {
  composeMode.value = true
  selectedMessage.value = null
  sendError.value = ''
  sendSuccess.value = ''

  if (!compose.value.toPlayerId) {
    resetComposeState()
  }
}

function cancelCompose() {
  composeMode.value = false
  sendError.value = ''
  sendSuccess.value = ''
  resetComposeState()
}

function replyToMessage(msg) {
  composeMode.value = true
  selectedMessage.value = null
  sendError.value = ''
  sendSuccess.value = ''

  compose.value = {
    toPlayerId: msg.senderId,
    toPlayerName: msg.senderName,
    subject: msg.subject?.startsWith('RE: ') ? msg.subject : `RE: ${msg.subject}`,
    body: ''
  }

  playerSearch.value = msg.senderName
  playerResults.value = []
  highlightedIndex.value = -1
  showPlayerDropdown.value = false
}

function messageAgain(msg) {
  composeMode.value = true
  selectedMessage.value = null
  sendError.value = ''
  sendSuccess.value = ''

  compose.value = {
    toPlayerId: msg.receiverId,
    toPlayerName: msg.receiverName,
    subject: msg.subject,
    body: ''
  }

  playerSearch.value = msg.receiverName
  playerResults.value = []
  highlightedIndex.value = -1
  showPlayerDropdown.value = false
}

async function runPlayerSearch() {
  const q = playerSearch.value.trim()
  const currentPlayerId = sessionStorage.getItem('playerId')

  if (q.length < 1) {
    playerResults.value = []
    showPlayerDropdown.value = false
    highlightedIndex.value = -1
    return
  }

  try {
    const results = await searchPlayers(q, currentPlayerId)
    playerResults.value = results || []
    showPlayerDropdown.value = playerResults.value.length > 0
    highlightedIndex.value = playerResults.value.length > 0 ? 0 : -1
  } catch (err) {
    console.error('Failed to search players', err)
    playerResults.value = []
    showPlayerDropdown.value = false
    highlightedIndex.value = -1
  }
}

function onRecipientInput() {
  compose.value.toPlayerId = ''
  compose.value.toPlayerName = ''
  sendError.value = ''
  sendSuccess.value = ''

  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => {
    runPlayerSearch()
  }, 180)
}

function selectRecipient(player) {
  compose.value.toPlayerId = player.id
  compose.value.toPlayerName = player.username
  playerSearch.value = player.username
  showPlayerDropdown.value = false
  playerResults.value = []
  highlightedIndex.value = -1
}

function onRecipientKeydown(e) {
  if (!showPlayerDropdown.value || !playerResults.value.length) return

  if (e.key === 'ArrowDown') {
    e.preventDefault()
    highlightedIndex.value = Math.min(highlightedIndex.value + 1, playerResults.value.length - 1)
  } else if (e.key === 'ArrowUp') {
    e.preventDefault()
    highlightedIndex.value = Math.max(highlightedIndex.value - 1, 0)
  } else if (e.key === 'Enter') {
    if (highlightedIndex.value >= 0 && playerResults.value[highlightedIndex.value]) {
      e.preventDefault()
      selectRecipient(playerResults.value[highlightedIndex.value])
    }
  } else if (e.key === 'Escape') {
    showPlayerDropdown.value = false
  }
}

function onRecipientFocus() {
  if (playerResults.value.length > 0) {
    showPlayerDropdown.value = true
  }
}

function onRecipientBlur() {
  setTimeout(() => {
    showPlayerDropdown.value = false
  }, 150)
}

async function sendMessage() {
  sendError.value = ''
  sendSuccess.value = ''

  if (!compose.value.toPlayerId) {
    sendError.value = 'Select a valid recipient from the list.'
    return
  }

  if (!compose.value.subject?.trim()) {
    sendError.value = 'Subject is required.'
    return
  }

  if (!compose.value.body?.trim()) {
    sendError.value = 'Message body is required.'
    return
  }

  try {
    sending.value = true

    await sendPlayerMessage({
      senderPlayerId: sessionStorage.getItem('playerId'),
      receiverPlayerId: compose.value.toPlayerId,
      subject: compose.value.subject,
      body: compose.value.body
    })

    await loadAllMessages()

    composeMode.value = false
    activeFolder.value = 'sent'
    sendSuccess.value = ''
    selectedMessage.value = null
    resetComposeState()
  } catch (err) {
    sendError.value = err.response?.data || 'Failed to send message.'
  } finally {
    sending.value = false
  }
}

function applyRouteRecipient() {
  const toPlayerId = route.query.toPlayerId
  const toPlayerName = route.query.toPlayerName

  if (toPlayerId && toPlayerName) {
    composeMode.value = true
    selectedMessage.value = null
    sendError.value = ''
    sendSuccess.value = ''

    compose.value = {
      toPlayerId: String(toPlayerId),
      toPlayerName: String(toPlayerName),
      subject: '',
      body: ''
    }

    playerSearch.value = String(toPlayerName)
    playerResults.value = []
    showPlayerDropdown.value = false
    highlightedIndex.value = -1
  }
}

function formatDate(value, full = false) {
  if (!value) return ''
  const d = new Date(value)
  return full
      ? d.toLocaleString('en-GB')
      : d.toLocaleDateString('en-GB')
}

onMounted(async () => {
  await loadAllMessages()
  applyRouteRecipient()
})
watch(() => route.query, () => {
  applyRouteRecipient()
})
</script>

<style scoped>
.messages-wrap {
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-height: calc(100vh - 150px);
}

.messages-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.messages-header-left {
  display: flex;
  align-items: baseline;
  gap: 12px;
}

.messages-title {
  font-family: var(--ff-title);
  font-size: 16px;
  color: var(--cyan);
  letter-spacing: 3px;
  font-weight: 700;
}

.messages-sub {
  font-size: 8px;
  color: var(--cyan-dim);
  letter-spacing: 2px;
  font-family: var(--ff-title);
}

.compose-top-btn {
  padding: 8px 14px;
  border: 1px solid var(--border-bright);
  background: rgba(0,212,255,.05);
  color: var(--cyan);
  font-family: var(--ff-title);
  font-size: 9px;
  letter-spacing: 1.5px;
  cursor: pointer;
  transition: all .15s;
}

.compose-top-btn:hover {
  border-color: var(--cyan);
  box-shadow: 0 0 10px rgba(0,212,255,.12);
}

.messages-layout {
  display: grid;
  grid-template-columns: 380px 1fr;
  gap: 14px;
  min-height: calc(100vh - 230px);
}

.messages-sidebar,
.messages-content {
  background: linear-gradient(180deg, rgba(0,212,255,.03), rgba(0,212,255,.01));
  border: 1px solid var(--border-bright);
  min-height: 0;
}

.messages-sidebar {
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.sidebar-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 14px;
  border-bottom: 1px solid var(--border);
  background: linear-gradient(90deg, rgba(0,212,255,.06), transparent);
}

.sidebar-label {
  font-family: var(--ff-title);
  font-size: 9px;
  letter-spacing: 2px;
  color: var(--cyan);
}

.sidebar-count {
  font-family: var(--ff-title);
  font-size: 10px;
  color: var(--cyan-dim);
  border: 1px solid var(--border);
  padding: 2px 8px;
  background: rgba(0,212,255,.04);
}

.msg-list {
  display: flex;
  flex-direction: column;
  overflow-y: auto;
}

.msg-list-empty {
  padding: 24px 16px;
  color: var(--muted);
  font-size: 11px;
  text-align: center;
}

.msg-list-item {
  border: none;
  border-bottom: 1px solid var(--border);
  background: transparent;
  text-align: left;
  padding: 12px 14px;
  cursor: pointer;
  transition: all .15s;
}

.msg-list-item:hover {
  background: rgba(0,212,255,.04);
}

.msg-list-item--active {
  background: linear-gradient(90deg, rgba(0,212,255,.08), transparent);
  box-shadow: inset 2px 0 0 var(--cyan);
}

.msg-list-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  margin-bottom: 6px;
}

.msg-from {
  color: var(--bright);
  font-size: 11px;
  font-weight: 700;
}

.msg-date {
  color: var(--muted);
  font-size: 9px;
  font-family: var(--ff-title);
  letter-spacing: 1px;
  flex-shrink: 0;
}

.msg-subject {
  color: var(--cyan);
  font-family: var(--ff-title);
  font-size: 10px;
  letter-spacing: 1px;
  margin-bottom: 5px;
}

.msg-preview {
  color: var(--muted);
  font-size: 10px;
  line-height: 1.5;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.messages-content {
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.content-topbar {
  padding: 14px 16px;
  border-bottom: 1px solid var(--border);
  background: linear-gradient(90deg, rgba(0,212,255,.05), transparent);
}

.content-topbar--detail {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.msg-delete-btn {
  background: rgba(255,48,64,.08);
  border: 1px solid rgba(255,48,64,.3);
  color: #ff4040;
  padding: 5px 12px;
  cursor: pointer;
  font-family: var(--ff);
  font-size: 11px;
  flex-shrink: 0;
  transition: background .15s;
}

.msg-delete-btn:hover {
  background: rgba(255,48,64,.18);
}

.content-title {
  font-family: var(--ff-title);
  font-size: 12px;
  color: var(--cyan);
  letter-spacing: 2px;
  font-weight: 700;
}

.content-sub {
  margin-top: 4px;
  color: var(--muted);
  font-size: 10px;
}

.compose-box,
.message-detail,
.messages-empty {
  flex: 1;
  padding: 18px;
  display: flex;
  flex-direction: column;
}

.compose-grid {
  display: flex;
  flex-direction: column;
  gap: 14px;
  margin-top: 14px;
}

.compose-row {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.compose-row label {
  font-size: 8px;
  color: var(--cyan-dim);
  text-transform: uppercase;
  letter-spacing: 2px;
  font-family: var(--ff-title);
  font-weight: 700;
}

.compose-row input,
.compose-row textarea {
  width: 100%;
  background: var(--bg3);
  border: 1px solid var(--border);
  color: var(--bright);
  padding: 10px 12px;
  font-family: var(--ff);
  font-size: 11px;
  outline: none;
  transition: all .15s;
}

.compose-row input:focus,
.compose-row textarea:focus {
  border-color: var(--cyan);
  box-shadow: 0 0 12px rgba(0,212,255,.08);
}

.compose-row input:disabled {
  color: var(--cyan-dim);
  background: rgba(0,212,255,.03);
}

.compose-row--body {
  flex: 1;
}

.compose-row textarea {
  min-height: 240px;
  resize: vertical;
  line-height: 1.6;
}

.detail-meta-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 10px;
  margin-top: 14px;
  margin-bottom: 16px;
}

.detail-stat {
  padding: 10px 12px;
  background: var(--bg3);
  border: 1px solid var(--border);
}

.detail-stat-label {
  font-size: 8px;
  color: var(--cyan-dim);
  font-family: var(--ff-title);
  letter-spacing: 2px;
  margin-bottom: 6px;
}

.detail-stat-value {
  font-size: 11px;
  color: var(--bright);
  font-weight: 700;
}

.detail-body {
  flex: 1;
  white-space: pre-wrap;
  line-height: 1.7;
  color: var(--text);
  font-size: 12px;
  padding: 14px;
  background: rgba(0,212,255,.02);
  border: 1px solid var(--border);
}

.messages-empty {
  align-items: center;
  justify-content: center;
  text-align: center;
  gap: 10px;
}

.messages-empty-icon {
  width: 52px;
  height: 52px;
  border-radius: 50%;
  border: 1px solid var(--border-bright);
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--cyan);
  font-size: 22px;
  background: rgba(0,212,255,.03);
}

.messages-empty-title {
  font-family: var(--ff-title);
  font-size: 11px;
  letter-spacing: 2px;
  color: var(--cyan);
}

.messages-empty-sub {
  color: var(--muted);
  font-size: 11px;
  max-width: 320px;
  line-height: 1.6;
}

.compose-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 16px;
}

.msg-btn {
  padding: 10px 16px;
  background: linear-gradient(180deg, var(--cyan-dark), var(--cyan-dim));
  border: 1px solid var(--cyan);
  color: #fff;
  font-family: var(--ff-title);
  font-size: 10px;
  font-weight: 700;
  cursor: pointer;
  letter-spacing: 2px;
  transition: all .15s;
}

.msg-btn:hover:not(:disabled) {
  box-shadow: 0 0 18px rgba(0,212,255,.18);
}

.msg-btn:disabled {
  opacity: .6;
  cursor: not-allowed;
  box-shadow: none;
}

.msg-btn--ghost {
  background: transparent;
  border-color: var(--border-bright);
  color: var(--muted);
}

.msg-btn--ghost:hover {
  border-color: var(--cyan);
  color: var(--cyan);
  box-shadow: none;
}

.msg-error,
.msg-success {
  margin-top: 14px;
  padding: 10px 12px;
  font-size: 10px;
  border: 1px solid;
}

.msg-error {
  color: var(--red);
  background: rgba(255,48,64,.06);
  border-color: rgba(255,48,64,.2);
}

.msg-success {
  color: var(--green);
  background: rgba(48,255,128,.06);
  border-color: rgba(48,255,128,.2);
}

@media (max-width: 1100px) {
  .messages-layout {
    grid-template-columns: 1fr;
  }

  .messages-sidebar {
    max-height: 280px;
  }

  .detail-meta-grid {
    grid-template-columns: 1fr;
  }
}

.compose-row--recipient {
  position: relative;
}

.recipient-dropdown {
  position: absolute;
  top: calc(100% + 4px);
  left: 0;
  right: 0;
  z-index: 20;
  background: var(--bg2);
  border: 1px solid var(--border-bright);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.35);
  max-height: 220px;
  overflow-y: auto;
}

.recipient-option {
  width: 100%;
  text-align: left;
  border: none;
  background: transparent;
  color: var(--bright);
  padding: 10px 12px;
  font-size: 11px;
  cursor: pointer;
  border-bottom: 1px solid var(--border);
  transition: all .12s;
}

.recipient-option:last-child {
  border-bottom: none;
}

.recipient-option:hover,
.recipient-option--active {
  background: rgba(0, 212, 255, 0.08);
  color: var(--cyan);
}

.sidebar-top--tabs {
  gap: 8px;
  justify-content: flex-start;
}

.sidebar-tab {
  flex: 1;
  justify-content: center;
  display: flex;
  align-items: center;
  gap: 8px;
  border: 1px solid transparent;
  background: transparent;
  color: var(--muted);
  font-family: var(--ff-title);
  font-size: 9px;
  letter-spacing: 2px;
  padding: 6px 8px;
  cursor: pointer;
  transition: all .15s;
}

.sidebar-tab:hover {
  color: var(--cyan);
  border-color: rgba(0,212,255,.18);
  background: rgba(0,212,255,.04);
}

.sidebar-tab--active {
  color: var(--cyan);
  border-color: var(--border-bright);
  background: rgba(0,212,255,.06);
}

.msg-list-item--unread {
  background: linear-gradient(90deg, rgba(0,212,255,.05), transparent);
}

.msg-unread-dot {
  display: inline-block;
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--cyan);
  margin-right: 8px;
  box-shadow: 0 0 6px rgba(0,212,255,.45);
  vertical-align: middle;
}

.msg-type-icon {
  margin-right: 5px;
  font-size: 11px;
}
</style>