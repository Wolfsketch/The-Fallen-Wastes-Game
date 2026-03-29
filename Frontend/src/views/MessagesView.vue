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
              'msg-list-item--unread': !msg.isRead
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
              <span v-if="!msg.isRead" class="msg-unread-dot"></span>
              <span v-if="getMessageIcon(msg)" class="msg-type-icon">{{ getMessageIcon(msg) }}</span>
              {{ msg.subject }}
            </div>

            <div class="msg-preview">
              {{ getMessagePreview(msg) }}
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
            <div class="msg-action-btns">
              <button class="msg-delete-btn" @click="deleteCurrentMessage" title="Delete message">🗑 Delete</button>
              <button class="msg-unread-btn"
                      @click="toggleReadStatus(selectedMessage)"
                      :title="selectedMessage?.isRead ? 'Mark as unread' : 'Mark as read'">
                {{ selectedMessage?.isRead ? '● Unread' : '✓ Read' }}
              </button>
            </div>
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

          <!-- SETTLEMENT BATTLE REPORT -->
          <div v-if="parsedReportBody?.isSettlementBattleReport" class="report-card">
            <div class="report-outcome"
              :class="(parsedReportBody.isDefenseReport ? !parsedReportBody.attackerWins : parsedReportBody.attackerWins) ? 'outcome--win' : 'outcome--loss'">
              <template v-if="parsedReportBody.isDefenseReport">
                {{ parsedReportBody.attackerWins ? '⚠ YOUR SETTLEMENT WAS RAIDED' : '✓ ATTACK REPELLED' }}
              </template>
              <template v-else>
                {{ parsedReportBody.attackerWins ? '⚔ VICTORY' : '✗ DEFEAT' }}
              </template>
            </div>

            <div class="report-section-title">{{ parsedReportBody.isDefenseReport ? 'ATTACKED BY' : 'TARGET SETTLEMENT' }}</div>
            <div class="report-poi-name">{{ parsedReportBody.isDefenseReport ? parsedReportBody.attackerSettlementName : parsedReportBody.defenderSettlementName }}</div>

            <div class="report-columns">
              <div class="report-col">
                <div class="report-section-title">ATTACKING FORCES</div>
                <div v-for="(qty, name) in parsedReportBody.attackerSentUnits" :key="'as'+name" class="report-unit-row">
                  <span class="report-unit-icon">
                    <img v-if="getUnitImage(name)" :src="getUnitImage(name)" class="unit-img-small" :alt="name" />
                    <span v-else>⚔</span>
                  </span>
                  <span class="report-unit-name">{{ name }}</span>
                  <span class="report-unit-qty">× {{ qty }}</span>
                </div>
                <template v-if="Object.keys(parsedReportBody.attackerLosses ?? {}).length">
                  <div class="report-section-title" style="margin-top:8px;color:#ff6060">LOSSES</div>
                  <div v-for="(qty, name) in parsedReportBody.attackerLosses" :key="'al'+name" class="report-unit-row report-unit-row--loss">
                    <span class="report-unit-icon">
                      <img v-if="getUnitImage(name)" :src="getUnitImage(name)" class="unit-img-small" :alt="name" />
                      <span v-else>💀</span>
                    </span>
                    <span class="report-unit-name">{{ name }}</span>
                    <span class="report-unit-qty report-qty--red">−{{ qty }}</span>
                  </div>
                </template>
                <div v-else style="font-size:11px;color:var(--green);margin-top:4px">No losses</div>
              </div>

              <div class="report-col">
                <div class="report-section-title">DEFENDER GARRISON</div>
                <template v-if="Object.keys(parsedReportBody.defenderUnits ?? {}).length">
                  <div v-for="(qty, name) in parsedReportBody.defenderUnits" :key="'du'+name" class="report-unit-row">
                    <span class="report-unit-icon">
                      <img v-if="getUnitImage(name)" :src="getUnitImage(name)" class="unit-img-small" :alt="name" />
                      <span v-else>🛡</span>
                    </span>
                    <span class="report-unit-name">{{ name }}</span>
                    <span class="report-unit-qty">× {{ qty }}</span>
                  </div>
                </template>
                <div v-else style="font-size:11px;color:var(--muted);padding:4px 0">No garrison</div>
                <template v-if="Object.keys(parsedReportBody.defenderLosses ?? {}).length">
                  <div class="report-section-title" style="margin-top:8px;color:#ff6060">LOSSES</div>
                  <div v-for="(qty, name) in parsedReportBody.defenderLosses" :key="'dl'+name" class="report-unit-row report-unit-row--loss">
                    <span class="report-unit-icon">💀</span>
                    <span class="report-unit-name">{{ name }}</span>
                    <span class="report-unit-qty report-qty--red">−{{ qty }}</span>
                  </div>
                </template>
              </div>
            </div>

            <template v-if="Object.values(parsedReportBody.lootedResources ?? {}).some(v => v > 0)">
              <div class="report-section-title" style="margin-top:10px">
                {{ parsedReportBody.isDefenseReport ? 'RESOURCES PLUNDERED' : 'PLUNDER COLLECTED' }}
              </div>
              <div class="report-loot-box">
                <div v-if="parsedReportBody.lootedResources.water  > 0" class="report-loot-item">💧 Water  {{ parsedReportBody.isDefenseReport ? '−' : '+' }}{{ parsedReportBody.lootedResources.water }}</div>
                <div v-if="parsedReportBody.lootedResources.food   > 0" class="report-loot-item">🥫 Food   {{ parsedReportBody.isDefenseReport ? '−' : '+' }}{{ parsedReportBody.lootedResources.food }}</div>
                <div v-if="parsedReportBody.lootedResources.scrap  > 0" class="report-loot-item">⚙️ Scrap  {{ parsedReportBody.isDefenseReport ? '−' : '+' }}{{ parsedReportBody.lootedResources.scrap }}</div>
                <div v-if="parsedReportBody.lootedResources.fuel   > 0" class="report-loot-item">⛽ Fuel   {{ parsedReportBody.isDefenseReport ? '−' : '+' }}{{ parsedReportBody.lootedResources.fuel }}</div>
                <div v-if="parsedReportBody.lootedResources.energy > 0" class="report-loot-item">⚡ Energy {{ parsedReportBody.isDefenseReport ? '−' : '+' }}{{ parsedReportBody.lootedResources.energy }}</div>
              </div>
            </template>
            <div v-else-if="parsedReportBody.attackerWins && !parsedReportBody.isDefenseReport"
                 style="font-size:11px;color:var(--muted)">No resources to plunder.</div>
          </div>

          <!-- POI/NPC BATTLE REPORT -->
          <div v-else-if="parsedReportBody?.attackerWins !== undefined" class="report-card">
            <div class="report-outcome" :class="parsedReportBody.attackerWins ? 'outcome--win' : 'outcome--loss'">
              {{ parsedReportBody.attackerWins ? '⚔ VICTORY' : '✗ DEFEATED' }}
            </div>
            <div class="report-section-title">TARGET</div>
            <div class="report-poi-name">{{ parsedReportBody.poiName }}</div>

            <div class="report-columns">
              <div class="report-col">
                <div class="report-section-title">YOUR FORCES SENT</div>
                <div v-for="(qty, name) in parsedReportBody.attackerSentUnits" :key="'sent'+name" class="report-unit-row">
                  <span class="report-unit-icon">
                    <img v-if="getUnitImage(name)" :src="getUnitImage(name)" class="unit-img-small" :alt="name" />
                    <span v-else>{{ getUnitIcon(name) }}</span>
                  </span>
                  <span class="report-unit-name">{{ name }}</span>
                  <span class="report-unit-qty">× {{ qty }}</span>
                </div>
              </div>
              <div class="report-col">
                <div class="report-section-title">LOSSES</div>
                <div v-for="(qty, name) in parsedReportBody.attackerLosses" :key="'loss'+name" class="report-unit-row report-unit-row--loss">
                  <span class="report-unit-icon">
                    <img v-if="getUnitImage(name)" :src="getUnitImage(name)" class="unit-img-small" :alt="name" />
                    <span v-else>{{ getUnitIcon(name) }}</span>
                  </span>
                  <span class="report-unit-name">{{ name }}</span>
                  <span class="report-unit-qty report-qty--red">-{{ qty }}</span>
                </div>
                <div v-if="!Object.keys(parsedReportBody.attackerLosses ?? {}).length"
                     style="font-size:11px;color:var(--green)">No losses</div>
              </div>
            </div>

            <div class="report-section-title" style="margin-top:14px">NPC CASUALTIES</div>
            <div v-for="(qty, name) in parsedReportBody.npcLosses" :key="'npc'+name" class="report-unit-row">
              <span class="report-unit-icon">💀</span>
              <span class="report-unit-name">{{ name }}</span>
              <span class="report-unit-qty report-qty--orange">-{{ qty }} killed</span>
            </div>

            <div v-if="parsedReportBody.lootCollected?.length" class="report-loot-box">
              <div class="report-section-title">LOOT COLLECTED</div>
              <div v-for="item in parsedReportBody.lootCollected" :key="item" class="report-loot-item">
                🧬 {{ item }}
              </div>
            </div>
          </div>

          <!-- SCOUT REPORT (rich card) -->
          <div v-else-if="parsedReportBody?.isScoutReport" class="report-card scout-report-card">
            <div class="scout-report-header">
              <div class="scout-report-title">👁 INTELLIGENCE REPORT</div>
              <div class="scout-tier-badge" :class="`tier--${parsedReportBody.tier}`">
                TIER {{ parsedReportBody.tier }}
              </div>
            </div>

            <div class="scout-target-row">
              <span class="scout-label">TARGET</span>
              <span class="scout-poi-name">{{ parsedReportBody.poiName }}</span>
            </div>

            <div class="report-section-title" style="margin-top:6px">DEFENDERS DETECTED</div>
            <div v-if="!Object.keys(parsedReportBody.npcUnits ?? {}).length" class="scout-no-defenders">
              ☉ No defenders remaining at this location
            </div>
            <div v-else class="scout-unit-grid">
              <div v-for="(qty, name) in parsedReportBody.npcUnits" :key="name" class="scout-unit-card">
                <div class="scout-unit-img-wrap">
                  <img v-if="getUnitImage(name)" :src="getUnitImage(name)" :alt="name" class="scout-unit-img" />
                  <span v-else class="scout-unit-fallback">💀</span>
                </div>
                <div class="scout-unit-name">{{ name }}</div>
                <div class="scout-unit-count">× {{ qty }}</div>
              </div>
            </div>

            <template v-if="parsedReportBody.lootItems?.length">
              <div class="report-section-title" style="margin-top:8px">SALVAGEABLE LOOT</div>
              <div class="scout-loot-grid">
                <div v-for="item in parsedReportBody.lootItems" :key="item" class="scout-loot-item">
                  <span class="scout-loot-icon">🧬</span>
                  <span class="scout-loot-name">{{ item }}</span>
                </div>
              </div>
            </template>
            <div v-else class="scout-no-loot">No salvageable loot detected at this location.</div>
          </div>

          <!-- Plain text fallback (old messages / regular messages) -->
          <div v-else class="detail-body">{{ selectedMessage.body }}</div>

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
  markMessageAsUnread,
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

async function toggleReadStatus(msg) {
  if (!msg) return
  try {
    if (msg.isRead) {
      await markMessageAsUnread(msg.id)
      msg.isRead = false
      const t = inboxMessages.value.find(m => m.id === msg.id)
            ?? reportMessages.value.find(m => m.id === msg.id)
            ?? sentMessages.value.find(m => m.id === msg.id)
      if (t) t.isRead = false
    } else {
      await markMessageAsRead(msg.id)
      msg.isRead = true
      const t = inboxMessages.value.find(m => m.id === msg.id)
            ?? reportMessages.value.find(m => m.id === msg.id)
            ?? sentMessages.value.find(m => m.id === msg.id)
      if (t) t.isRead = true
    }
    await emitUnreadRefresh()
  } catch (e) { console.error('Toggle read failed', e) }
}

function getMessageIcon(msg) {
  const subject = (msg.subject ?? '').toLowerCase()
  const type = (msg.messageType ?? '').toLowerCase()
  if (type === 'alliance_invite') return '◈'
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

  if ((activeFolder.value === 'inbox' || activeFolder.value === 'reports') && !msg.isRead) {
    try {
      await markMessageAsRead(msg.id)
      msg.isRead = true

      const target = inboxMessages.value.find(m => m.id === msg.id)
              ?? reportMessages.value.find(m => m.id === msg.id)
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

const parsedReportBody = computed(() => {
  if (!selectedMessage.value) return null
  const msg = selectedMessage.value
  const type = (msg.messageType ?? '').toLowerCase()
  if (type !== 'report') return null
  try {
    const parsed = JSON.parse(msg.body)
    if (parsed && typeof parsed === 'object') return parsed
  } catch {}
  return null
})

const unitImages = import.meta.glob('../images/*.png', { eager: true, import: 'default' })

function getUnitImage(unitName) {
  const key = `../images/${unitName}.png`
  return unitImages[key] ?? null
}

function getMessagePreview(msg) {
  try {
    const parsed = JSON.parse(msg.body)
    if (parsed?.isScoutReport) {
      const units = Object.entries(parsed.npcUnits ?? {}).map(([k, v]) => `${v}x ${k}`).join(', ')
      return `Tier ${parsed.tier} POI. Defenders: ${units || 'None'}.`
    }
    if (parsed?.attackerWins !== undefined) {
      return parsed.attackerWins
        ? `Victory at ${parsed.poiName}`
        : `Defeated at ${parsed.poiName}`
    }
  } catch {}
  return msg.body
}

function getUnitIcon(unitName) {
  const name = (unitName ?? '').toLowerCase()
  if (name.includes('scavenger')) return '🏃'
  if (name.includes('raider')) return '⚔️'
  if (name.includes('rifleman')) return '🎯'
  if (name.includes('sniper')) return '🔭'
  if (name.includes('shock')) return '💥'
  if (name.includes('flame')) return '🔥'
  if (name.includes('power')) return '⚡'
  if (name.includes('outpost')) return '🛡️'
  if (name.includes('bike') || name.includes('assault')) return '🏍️'
  if (name.includes('buggy') || name.includes('rust')) return '🚗'
  if (name.includes('war rig')) return '🚛'
  if (name.includes('interceptor')) return '⚡'
  if (name.includes('siege')) return '🏰'
  if (name.includes('wall breaker')) return '💣'
  if (name.includes('emp')) return '📡'
  if (name.includes('laser')) return '🔫'
  if (name.includes('drone')) return '🤖'
  if (name.includes('rad')) return '☢️'
  if (name.includes('convoy')) return '🚐'
  return '⚔️'
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

.msg-action-btns {
  display: flex;
  gap: 6px;
  flex-shrink: 0;
}

.msg-unread-btn {
  background: rgba(0,212,255,.06);
  border: 1px solid rgba(0,212,255,.3);
  color: var(--cyan);
  padding: 5px 12px;
  cursor: pointer;
  font-family: var(--ff);
  font-size: 11px;
}

.msg-unread-btn:hover {
  background: rgba(0,212,255,.15);
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

.report-card {
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: 16px;
}
.report-outcome {
  font-family: var(--ff-title);
  font-size: 16px;
  font-weight: 700;
  letter-spacing: 2px;
  padding: 10px 16px;
  border: 1px solid;
  margin-bottom: 6px;
}
.outcome--win { color: var(--green); border-color: rgba(48,255,128,.3); background: rgba(48,255,128,.05); }
.outcome--loss { color: var(--red); border-color: rgba(255,48,64,.3); background: rgba(255,48,64,.05); }
.report-poi-name { font-size: 13px; color: var(--text); font-weight: 700; margin-bottom: 8px; }
.report-section-title {
  font-size: 8px;
  color: var(--cyan);
  letter-spacing: 3px;
  font-family: var(--ff-title);
  font-weight: 700;
  margin-bottom: 6px;
}
.report-columns { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
.report-col { display: flex; flex-direction: column; gap: 4px; }
.report-unit-row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 5px 8px;
  background: var(--bg3);
  border: 1px solid var(--border);
  font-size: 11px;
}
.report-unit-row--loss { opacity: .75; }
.report-unit-icon { font-size: 13px; width: 20px; text-align: center; }
.report-unit-name { flex: 1; color: var(--text); }
.report-unit-qty { font-family: var(--ff-title); font-weight: 700; color: var(--cyan); }
.report-qty--red { color: var(--red); }
.report-qty--orange { color: #ff9040; }
.report-loot-box {
  margin-top: 8px;
  padding: 10px 12px;
  background: rgba(0,212,255,.03);
  border: 1px solid var(--border);
}
.report-loot-item { font-size: 11px; color: var(--green); padding: 3px 0; }

/* Unit images in battle report */
.unit-img-small { width: 26px; height: 26px; object-fit: contain; display: block; }

/* Scout report card */
.scout-report-card { gap: 12px; }
.scout-report-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 18px;
  background: linear-gradient(90deg, rgba(0,212,255,.09), rgba(0,212,255,.03));
  border: 1px solid rgba(0,212,255,.28);
  margin-bottom: 4px;
}
.scout-report-title {
  font-family: var(--ff-title);
  font-size: 14px;
  font-weight: 700;
  letter-spacing: 3px;
  color: var(--cyan);
}
.scout-tier-badge {
  font-family: var(--ff-title);
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 2px;
  padding: 4px 14px;
  border: 1px solid;
}
.tier--1 { color: #80c8d8; border-color: rgba(128,200,216,.45); background: rgba(128,200,216,.08); }
.tier--2 { color: #ffc830; border-color: rgba(255,200,48,.45); background: rgba(255,200,48,.08); }
.tier--3 { color: #ff9040; border-color: rgba(255,144,64,.45); background: rgba(255,144,64,.09); }
.scout-target-row {
  display: flex;
  align-items: baseline;
  gap: 12px;
  padding: 4px 2px 2px;
}
.scout-label {
  font-size: 8px;
  color: var(--cyan-dim);
  font-family: var(--ff-title);
  letter-spacing: 2px;
}
.scout-poi-name {
  font-size: 15px;
  color: var(--bright);
  font-weight: 700;
}
.scout-unit-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(96px, 1fr));
  gap: 8px;
}
.scout-unit-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 5px;
  padding: 12px 8px 10px;
  background: var(--bg3);
  border: 1px solid var(--border);
  transition: border-color .15s;
}
.scout-unit-card:hover { border-color: rgba(255,100,60,.4); }
.scout-unit-img-wrap {
  width: 48px;
  height: 48px;
  display: flex;
  align-items: center;
  justify-content: center;
}
.scout-unit-img {
  width: 48px;
  height: 48px;
  object-fit: contain;
  filter: drop-shadow(0 0 6px rgba(255,80,40,.35));
}
.scout-unit-fallback { font-size: 26px; }
.scout-unit-name {
  font-size: 9px;
  color: var(--text);
  font-family: var(--ff-title);
  letter-spacing: .5px;
  text-align: center;
  line-height: 1.3;
}
.scout-unit-count {
  font-family: var(--ff-title);
  font-size: 14px;
  font-weight: 700;
  color: var(--red);
}
.scout-no-defenders {
  font-size: 11px;
  color: var(--green);
  padding: 10px 14px;
  border: 1px solid rgba(48,255,128,.2);
  background: rgba(48,255,128,.04);
}
.scout-loot-grid { display: flex; flex-direction: column; gap: 4px; }
.scout-loot-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 7px 12px;
  background: var(--bg3);
  border: 1px solid var(--border);
}
.scout-loot-icon { font-size: 14px; flex-shrink: 0; }
.scout-loot-name { font-size: 11px; color: var(--green); font-family: var(--ff-title); letter-spacing: .5px; }
.scout-no-loot { font-size: 11px; color: var(--muted); font-style: italic; padding: 6px 2px; }

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