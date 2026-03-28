<template>
  <div class="alliance-wrap fade-in">

    <!-- ═══════════════════════════════════════════════════════
         HEADER
    ═══════════════════════════════════════════════════════════ -->
    <div class="al-header">
      <div class="al-header-left">
        <h2 class="al-title">ALLIANCE</h2>
        <span class="al-sub" v-if="myAlliance">[ {{ myAlliance.tag }} ] {{ myAlliance.name }}</span>
        <span class="al-sub" v-else>NO FACTION</span>
      </div>
      <div class="al-header-right" v-if="myAlliance">
        <span class="al-badge" :class="statusClass">{{ statusDisplay }}</span>
        <span class="al-members">{{ myAlliance.memberCount }} MEMBERS</span>
      </div>
    </div>

    <div class="accent-line" style="margin-bottom:0" />

    <!-- ═══════════════════════════════════════════════════════
         NO ALLIANCE — Browse / Create / Invitations
    ═══════════════════════════════════════════════════════════ -->
    <template v-if="!myAlliance">
      <div class="al-tabs">
        <button v-for="t in noAllianceTabs" :key="t.id"
                class="al-tab" :class="{ 'al-tab--active': activeTab === t.id }"
                @click="activeTab = t.id">
          <span class="al-tab-icon">{{ t.icon }}</span>{{ t.label }}
          <span v-if="t.id === 'invitations' && invitations.length" class="al-tab-badge">{{ invitations.length }}</span>
        </button>
      </div>

      <!-- BROWSE -->
      <div v-if="activeTab === 'browse'" class="al-panel">
        <div class="al-section-hdr">
          <span>ACTIVE FACTIONS</span>
          <span class="al-section-sub">Sorted by total score</span>
        </div>

        <div v-if="loadingAlliances" class="al-loading">
          <div class="al-spinner"></div> SCANNING FACTIONS...
        </div>

        <table v-else class="al-table">
          <thead>
            <tr>
              <th>TAG</th><th>NAME</th><th>STATUS</th><th>MEMBERS</th><th>SCORE</th><th></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="a in alliances" :key="a.id" @click="expandedAlliance = expandedAlliance === a.id ? null : a.id"
                class="al-row" :class="{ 'al-row--expanded': expandedAlliance === a.id }">
              <td><span class="al-tag">{{ a.tag }}</span></td>
              <td class="al-name-cell">{{ a.name }}</td>
              <td><span class="al-status-dot" :class="getStatusClass(a.status)">{{ statusToString(a.status) }}</span></td>
              <td>{{ a.memberCount }}</td>
              <td class="al-score-cell">{{ a.totalScore?.toLocaleString() }}</td>
              <td>
                <button v-if="statusToString(a.status) !== 'Closed'" class="al-btn al-btn--sm"
                        @click.stop="openApplyModal(a)">
                  {{ statusToString(a.status) === 'Open' ? 'JOIN' : 'APPLY' }}
                </button>
              </td>
            </tr>
            <tr v-if="alliances.length === 0">
              <td colspan="6" class="al-empty">No factions found in the wasteland.</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- CREATE -->
      <div v-if="activeTab === 'create'" class="al-panel">
        <div class="al-section-hdr"><span>FOUND A NEW FACTION</span></div>

        <div class="al-form">
          <div class="al-form-row">
            <label class="al-label">FACTION NAME</label>
            <input v-model="createForm.name" class="al-input" placeholder="e.g. Iron Brotherhood" maxlength="60" />
          </div>
          <div class="al-form-row">
            <label class="al-label">TAG <span class="al-label-sub">(max 6 chars)</span></label>
            <input v-model="createForm.tag" class="al-input al-input--sm" placeholder="e.g. IB"
                   maxlength="6" style="text-transform:uppercase" />
          </div>
          <div v-if="createError" class="al-error">{{ createError }}</div>
          <button class="al-btn al-btn--primary" @click="doCreateAlliance" :disabled="createLoading">
            {{ createLoading ? 'FOUNDING...' : '⊕ FOUND FACTION' }}
          </button>
        </div>
      </div>

      <!-- INVITATIONS -->
      <div v-if="activeTab === 'invitations'" class="al-panel">
        <div class="al-section-hdr"><span>PENDING INVITATIONS</span></div>

        <div v-if="invitations.length === 0" class="al-empty-state">
          <span class="al-empty-icon">◌</span>
          <div>No pending invitations.</div>
        </div>

        <div v-for="inv in invitations" :key="inv.id" class="al-invite-card">
          <div class="al-invite-info">
            <span class="al-tag">{{ inv.allianceTag }}</span>
            <span class="al-invite-name">{{ inv.allianceName }}</span>
          </div>
          <div class="al-invite-msg" v-if="inv.message">{{ inv.message }}</div>
          <div class="al-invite-actions">
            <button class="al-btn al-btn--accept" @click="doAcceptInvite(inv)">ACCEPT</button>
            <button class="al-btn al-btn--reject" @click="doRejectInvite(inv)">DECLINE</button>
          </div>
        </div>
      </div>
    </template>

    <!-- ═══════════════════════════════════════════════════════
         IN ALLIANCE — Full management interface
    ═══════════════════════════════════════════════════════════ -->
    <template v-else>
      <div class="al-tabs">
        <button v-for="t in allianceTabs" :key="t.id"
                class="al-tab" :class="{ 'al-tab--active': activeTab === t.id }"
                @click="switchTab(t.id)">
          <span class="al-tab-icon">{{ t.icon }}</span>{{ t.label }}
          <span v-if="t.id === 'recruitment' && pendingAppsCount" class="al-tab-badge">{{ pendingAppsCount }}</span>
        </button>
      </div>

      <!-- ─── OVERVIEW ─────────────────────────────────────── -->
      <div v-if="activeTab === 'overview'" class="al-panel al-panel--two-col">
        <div class="al-col">
          <div class="al-section-hdr">
            <span>EVENTS</span>
            <div class="al-events-tabs">
              <button class="al-evt-tab" :class="{'al-evt-tab--active': eventsTab==='events'}" @click="eventsTab='events'">Events</button>
              <button class="al-evt-tab" :class="{'al-evt-tab--active': eventsTab==='conquests'}" @click="eventsTab='conquests'">Conquests</button>
            </div>
          </div>
          <div class="al-events-list">
            <div class="al-event-item" v-if="myAlliance">
              <span class="al-event-icon">◈</span>
              <div class="al-event-text">
                <span class="al-event-actor">Faction founded</span>
                <span class="al-event-time">{{ formatDate(myAlliance.foundedAtUtc) }}</span>
              </div>
            </div>
            <div class="al-event-empty" v-if="eventsTab === 'conquests'">No conquests recorded yet.</div>
          </div>
        </div>

        <div class="al-col">
          <div class="al-section-hdr">
            <span>ANNOUNCEMENTS</span>
            <button v-if="myRank <= 2" class="al-btn al-btn--xs" @click="editingAnnouncement = true">EDIT</button>
          </div>
          <div v-if="!editingAnnouncement" class="al-announcement">
            <span v-if="announcement">{{ announcement }}</span>
            <span v-else class="al-muted">No announcement set.</span>
          </div>
          <div v-else class="al-announcement-edit">
            <textarea v-model="announcementDraft" class="al-textarea" rows="5"
                      placeholder="Write an announcement for your faction members..."></textarea>
            <div class="al-form-actions">
              <button class="al-btn al-btn--primary al-btn--sm" @click="saveAnnouncement">SAVE</button>
              <button class="al-btn al-btn--sm" @click="editingAnnouncement = false">CANCEL</button>
            </div>
          </div>
        </div>
      </div>

      <!-- ─── MEMBERS ───────────────────────────────────────── -->
      <div v-if="activeTab === 'members'" class="al-panel">
        <div class="al-section-hdr">
          <span>FACTION ROSTER</span>
          <span class="al-section-sub">{{ myAlliance.memberCount }} members</span>
        </div>

        <table class="al-table">
          <thead>
            <tr>
              <th>RANK</th><th>PLAYER</th><th>SCORE</th><th>SETTLEMENTS</th><th>JOINED</th>
              <th v-if="myRank <= 1">PERMISSIONS</th>
              <th v-if="myRank <= 2"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="m in members" :key="m.playerId" class="al-row">
              <td>
                <span class="al-rank-badge" :class="'al-rank--' + rankToString(m.rank).toLowerCase()">
                  {{ rankToString(m.rank) }}
                </span>
              </td>
              <td class="al-name-cell">
                <span class="al-online-dot" v-if="m.playerId === player?.id"></span>
                {{ m.username }}
                <span v-if="m.playerId === player?.id" class="al-you-tag">YOU</span>
              </td>
              <td class="al-score-cell">{{ m.score?.toLocaleString() }}</td>
              <td>{{ m.settlementCount }}</td>
              <td class="al-muted al-date-cell">{{ formatDate(m.joinedAtUtc) }}</td>
              <td v-if="myRank <= 1" class="al-perm-cell">
                <div class="al-perm-icons">
                  <template v-for="p in PERMISSIONS" :key="p.key">
                    <span v-if="getRankValue(m.rank) <= 2"
                          class="al-perm-icon al-perm-icon--locked"
                          :title="p.label + ': granted by rank'">
                      {{ p.icon }}
                    </span>
                    <button v-else
                            type="button"
                            class="al-perm-icon"
                            :class="{ 'al-perm-icon--on': m[p.key] }"
                            :title="p.label + ': ' + (m[p.key] ? 'revoke' : 'grant')"
                            @click="doTogglePermission(m, p.key)">
                      {{ p.icon }}
                    </button>
                  </template>
                </div>
              </td>
              <td v-if="myRank <= 2">
                <div class="al-member-actions" v-if="m.playerId !== player?.id && getRankValue(m.rank) > myRank">
                  <select class="al-select al-select--sm" @change="e => doSetRank(m, e.target.value)"
                          :value="m.rank">
                    <option value="Member">Member</option>
                    <option value="Officer">Officer</option>
                    <option v-if="myRank === 0" value="Leader">Leader</option>
                  </select>
                  <button class="al-btn al-btn--danger al-btn--xs" @click="doKickMember(m)">KICK</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- ─── RECRUITMENT ───────────────────────────────────── -->
      <div v-if="activeTab === 'recruitment'" class="al-panel al-panel--two-col">
        <!-- LEFT: Invite -->
        <div class="al-col">
          <div class="al-section-hdr"><span>INVITE PLAYER</span></div>
          <div class="al-form">
            <div class="al-form-row">
              <label class="al-label">PLAYER NAME</label>
              <input v-model="inviteForm.name" class="al-input" placeholder="Enter player name..."
                     @input="searchPlayers" />
              <div v-if="playerSearchResults.length" class="al-search-dropdown">
                <div v-for="p in playerSearchResults" :key="p.id" class="al-search-item"
                     @click="selectInvitePlayer(p)">
                  {{ p.username }}
                  <span class="al-search-score">{{ p.score }} pts</span>
                </div>
              </div>
            </div>
            <div class="al-form-row">
              <label class="al-label">MESSAGE <span class="al-label-sub">(optional)</span></label>
              <input v-model="inviteForm.message" class="al-input" placeholder="Personal message..." />
            </div>
            <div v-if="inviteError" class="al-error">{{ inviteError }}</div>
            <div v-if="inviteSuccess" class="al-success">{{ inviteSuccess }}</div>
            <button class="al-btn al-btn--primary" @click="doInvitePlayer" :disabled="!inviteForm.targetId || inviteLoading">
              {{ inviteLoading ? 'SENDING...' : '✉ SEND INVITATION' }}
            </button>
          </div>
        </div>

        <!-- RIGHT: Applications -->
        <div class="al-col">
          <div class="al-section-hdr">
            <span>APPLICATIONS</span>
            <span class="al-tab-badge" v-if="pendingApplications.length">{{ pendingApplications.length }}</span>
          </div>

          <div v-if="pendingApplications.length === 0" class="al-empty-state">
            <span class="al-empty-icon">◌</span>
            <div>No pending applications.</div>
          </div>

          <div v-for="app in pendingApplications" :key="app.id" class="al-app-card">
            <div class="al-app-header">
              <span class="al-app-player">{{ app.playerName ?? app.playerId }}</span>
              <span class="al-app-date">{{ formatDate(app.createdAtUtc) }}</span>
            </div>
            <div class="al-app-msg" v-if="app.message">{{ app.message }}</div>
            <div class="al-app-actions" v-if="myRank <= 2">
              <button class="al-btn al-btn--accept al-btn--sm" @click="doAcceptApp(app)">ACCEPT</button>
              <button class="al-btn al-btn--reject al-btn--sm" @click="doRejectApp(app)">REJECT</button>
            </div>
          </div>
        </div>
      </div>

      <!-- ─── SETTINGS ──────────────────────────────────────── -->
      <div v-if="activeTab === 'settings'" class="al-panel">
        <div class="al-section-hdr"><span>FACTION SETTINGS</span></div>

        <div v-if="myRank <= 1" class="al-form al-form--wide">
          <div class="al-form-grid">
            <div class="al-form-row">
              <label class="al-label">FACTION NAME</label>
              <input v-model="settingsForm.name" class="al-input" maxlength="60" />
            </div>
            <div class="al-form-row">
              <label class="al-label">TAG</label>
              <input v-model="settingsForm.tag" class="al-input al-input--sm" maxlength="6" />
            </div>
            <div class="al-form-row al-form-row--full">
              <label class="al-label">DESCRIPTION</label>
              <textarea v-model="settingsForm.description" class="al-textarea" rows="4" maxlength="2000"
                        placeholder="Describe your faction..."></textarea>
            </div>
            <div class="al-form-row">
              <label class="al-label">STATUS</label>
              <select v-model="settingsForm.status" class="al-select">
                <option value="Open">Open — anyone can join</option>
                <option value="ApplicationRequired">Application required</option>
                <option value="Closed">Closed</option>
              </select>
            </div>
            <div class="al-form-row">
              <label class="al-label">MIN SCORE</label>
              <div class="al-num-wrap">
                <input v-model.number="settingsForm.minPoints" type="number" min="0" class="al-input al-num-input" />
                <div class="al-num-btn-group">
                  <button type="button" class="al-num-btn" @click="settingsForm.minPoints = (settingsForm.minPoints || 0) + 1">▲</button>
                  <button type="button" class="al-num-btn" @click="settingsForm.minPoints = Math.max(0, (settingsForm.minPoints || 0) - 1)">▼</button>
                </div>
              </div>
            </div>
          </div>

          <div v-if="settingsError" class="al-error">{{ settingsError }}</div>
          <div v-if="settingsSuccess" class="al-success">{{ settingsSuccess }}</div>

          <div class="al-form-actions">
            <button class="al-btn al-btn--primary" @click="doSaveSettings" :disabled="settingsLoading">
              {{ settingsLoading ? 'SAVING...' : 'SAVE SETTINGS' }}
            </button>
          </div>

          <div class="al-danger-zone">
            <div class="al-danger-title">DANGER ZONE</div>
            <button class="al-btn al-btn--danger" @click="confirmDissolve = true">DISSOLVE FACTION</button>
          </div>
        </div>

        <div v-else class="al-empty-state">
          <span class="al-empty-icon">⊘</span>
          <div>Only Leaders and Founders can change settings.</div>
        </div>

        <!-- Leave button for non-founders -->
        <div class="al-leave-zone" v-if="myRank > 0">
          <button class="al-btn al-btn--danger" @click="confirmLeave = true">LEAVE FACTION</button>
        </div>
      </div>

      <!-- ─── FORUM ─────────────────────────────────────────── -->
      <div v-if="activeTab === 'forum'" class="al-panel">
        <!-- TOPIC LIST -->
        <template v-if="!activeTopic">
          <div class="al-section-hdr">
            <span>FACTION FORUM</span>
            <button class="al-btn al-btn--primary al-btn--sm" @click="showNewTopicForm = !showNewTopicForm">
              {{ showNewTopicForm ? 'CANCEL' : '+ NEW TOPIC' }}
            </button>
          </div>

          <!-- New topic form -->
          <div v-if="showNewTopicForm" class="al-new-topic-form">
            <div class="al-form-row">
              <label class="al-label">TITLE</label>
              <input v-model="newTopicForm.title" class="al-input" placeholder="Topic title..." maxlength="120" />
            </div>
            <div class="al-form-row">
              <label class="al-label">OPENING POST</label>
              <div class="al-editor">
                <div class="al-editor-toolbar">
                  <button type="button" class="al-editor-btn" title="Bold"
                          @mousedown.prevent="insertFormat(newTopicEditorRef, '[b]', '[/b]')"><b>B</b></button>
                  <button type="button" class="al-editor-btn al-editor-btn--i" title="Italic"
                          @mousedown.prevent="insertFormat(newTopicEditorRef, '[i]', '[/i]')"><i>I</i></button>
                  <button type="button" class="al-editor-btn al-editor-btn--u" title="Underline"
                          @mousedown.prevent="insertFormat(newTopicEditorRef, '[u]', '[/u]')">U</button>
                  <button type="button" class="al-editor-btn al-editor-btn--s" title="Strikethrough"
                          @mousedown.prevent="insertFormat(newTopicEditorRef, '[s]', '[/s]')"><s>S</s></button>
                  <span class="al-editor-sep"></span>
                  <button type="button" class="al-editor-btn" title="Inline code"
                          @mousedown.prevent="insertFormat(newTopicEditorRef, '[code]', '[/code]')">{ }</button>
                  <button type="button" class="al-editor-btn" title="Code block"
                          @mousedown.prevent="insertFormat(newTopicEditorRef, '[pre]', '[/pre]')">&lt;/&gt;</button>
                  <button type="button" class="al-editor-btn" title="Quote"
                          @mousedown.prevent="insertFormat(newTopicEditorRef, '[quote]', '[/quote]')">❝</button>
                  <button type="button" class="al-editor-btn" title="Bullet list"
                          @mousedown.prevent="insertListItem(newTopicEditorRef)">• List</button>
                </div>
                <textarea ref="newTopicEditorRef" v-model="newTopicForm.content" class="al-textarea" rows="5"
                          placeholder="Write your opening message..."></textarea>
              </div>
            </div>
            <div v-if="topicError" class="al-error">{{ topicError }}</div>
            <button class="al-btn al-btn--primary" @click="doCreateTopic" :disabled="topicLoading">
              {{ topicLoading ? 'POSTING...' : 'POST TOPIC' }}
            </button>
          </div>

          <div v-if="loadingForum" class="al-loading">
            <div class="al-spinner"></div> LOADING FORUM...
          </div>

          <table v-else class="al-table al-forum-table">
            <thead>
              <tr>
                <th>TOPIC</th><th>AUTHOR</th><th>REPLIES</th><th>LAST ACTIVITY</th><th></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="t in forumTopics" :key="t.id" class="al-row al-forum-row"
                  @click="openTopic(t)">
                <td>
                  <span v-if="t.isPinned" class="al-pin">📌</span>
                  {{ t.title }}
                </td>
                <td class="al-muted">{{ t.authorName }}</td>
                <td>{{ t.replyCount }}</td>
                <td class="al-muted al-date-cell">{{ t.lastPostAt ? formatDate(t.lastPostAt) : formatDate(t.createdAtUtc) }}</td>
                <td class="al-forum-actions" @click.stop>
                  <button v-if="t.authorPlayerId === player?.id || myPermissions.isForumModerator"
                          class="al-post-delete" title="Delete topic"
                          @click="doDeleteTopicFromList(t)">✕</button>
                </td>
              </tr>
              <tr v-if="forumTopics.length === 0">
                <td colspan="5" class="al-empty">No topics yet. Be the first to post.</td>
              </tr>
            </tbody>
          </table>
        </template>

        <!-- TOPIC DETAIL / THREAD -->
        <template v-else>
          <div class="al-section-hdr">
            <button class="al-back-btn" @click="closeTopic">← BACK</button>
            <span class="al-topic-title">{{ activeTopic.title }}</span>
          </div>

          <div v-if="loadingPosts" class="al-loading">
            <div class="al-spinner"></div> LOADING THREAD...
          </div>

          <div v-else class="al-thread">
            <div v-for="post in topicPosts" :key="post.id" class="al-post">
              <div class="al-post-meta">
                <span class="al-post-author">{{ post.authorName }}</span>
                <span class="al-post-time">{{ formatDate(post.createdAtUtc) }}</span>
                <span v-if="post.editedAtUtc" class="al-post-edited">(edited)</span>
                <button v-if="post.authorId === player?.id || myPermissions.isForumModerator"
                        class="al-post-delete" @click="doDeletePost(post)">✕</button>
              </div>
              <div class="al-post-content" v-html="renderContent(post.content)"></div>
            </div>
          </div>

          <div class="al-reply-box">
            <div class="al-section-hdr" style="margin-top:16px"><span>REPLY</span></div>
            <div class="al-editor">
              <div class="al-editor-toolbar">
                <button type="button" class="al-editor-btn" title="Bold"
                        @mousedown.prevent="insertFormat(replyEditorRef, '[b]', '[/b]')"><b>B</b></button>
                <button type="button" class="al-editor-btn al-editor-btn--i" title="Italic"
                        @mousedown.prevent="insertFormat(replyEditorRef, '[i]', '[/i]')"><i>I</i></button>
                <button type="button" class="al-editor-btn al-editor-btn--u" title="Underline"
                        @mousedown.prevent="insertFormat(replyEditorRef, '[u]', '[/u]')">U</button>
                <button type="button" class="al-editor-btn al-editor-btn--s" title="Strikethrough"
                        @mousedown.prevent="insertFormat(replyEditorRef, '[s]', '[/s]')"><s>S</s></button>
                <span class="al-editor-sep"></span>
                <button type="button" class="al-editor-btn" title="Inline code"
                        @mousedown.prevent="insertFormat(replyEditorRef, '[code]', '[/code]')">{ }</button>
                <button type="button" class="al-editor-btn" title="Code block"
                        @mousedown.prevent="insertFormat(replyEditorRef, '[pre]', '[/pre]')">&lt;/&gt;</button>
                <button type="button" class="al-editor-btn" title="Quote"
                        @mousedown.prevent="insertFormat(replyEditorRef, '[quote]', '[/quote]')">❝</button>
                <button type="button" class="al-editor-btn" title="Bullet list"
                        @mousedown.prevent="insertListItem(replyEditorRef)">• List</button>
              </div>
              <textarea ref="replyEditorRef" v-model="replyContent" class="al-textarea" rows="4"
                        placeholder="Write your reply..."></textarea>
            </div>
            <div v-if="replyError" class="al-error">{{ replyError }}</div>
            <button class="al-btn al-btn--primary" @click="doAddPost" :disabled="replyLoading || !replyContent.trim()">
              {{ replyLoading ? 'POSTING...' : 'POST REPLY' }}
            </button>
          </div>
        </template>
      </div>
    </template>

    <!-- ═══════════════════════════════════════════════════════
         MODALS
    ═══════════════════════════════════════════════════════════ -->

    <!-- Apply modal -->
    <Teleport to="body">
      <div v-if="applyModal" class="al-overlay" @click.self="applyModal = null">
        <div class="al-modal">
          <div class="al-modal-accent" />
          <div class="al-modal-body">
            <div class="al-modal-header">
              <div>
                <div class="al-modal-title">{{ statusToString(applyModal.status) === 'Open' ? 'JOIN' : 'APPLY TO' }}</div>
                <div class="al-modal-name">{{ applyModal.name }}</div>
              </div>
              <button class="al-modal-close" @click="applyModal = null">✕</button>
            </div>
            <div v-if="statusToString(applyModal.status) !== 'Open'" class="al-form">
              <label class="al-label">APPLICATION MESSAGE <span class="al-label-sub">(optional)</span></label>
              <textarea v-model="applyMessage" class="al-textarea" rows="3"
                        placeholder="Tell them why you want to join..."></textarea>
            </div>
            <div v-if="applyError" class="al-error">{{ applyError }}</div>
            <button class="al-btn al-btn--primary al-modal-action" @click="doApply" :disabled="applyLoading">
              {{ applyLoading ? 'SENDING...' : (statusToString(applyModal.status) === 'Open' ? 'JOIN FACTION' : 'SEND APPLICATION') }}
            </button>
          </div>
        </div>
      </div>

      <!-- Confirm dissolve -->
      <div v-if="confirmDissolve" class="al-overlay" @click.self="confirmDissolve = false">
        <div class="al-modal al-modal--danger">
          <div class="al-modal-accent al-modal-accent--red" />
          <div class="al-modal-body">
            <div class="al-modal-header">
              <div class="al-modal-title">DISSOLVE FACTION</div>
              <button class="al-modal-close" @click="confirmDissolve = false">✕</button>
            </div>
            <p class="al-modal-text">This will permanently destroy the faction and remove all members. This action cannot be undone.</p>
            <button class="al-btn al-btn--danger al-modal-action" @click="doDissolve">CONFIRM DISSOLVE</button>
          </div>
        </div>
      </div>

      <!-- Confirm leave -->
      <div v-if="confirmLeave" class="al-overlay" @click.self="confirmLeave = false">
        <div class="al-modal al-modal--danger">
          <div class="al-modal-accent al-modal-accent--red" />
          <div class="al-modal-body">
            <div class="al-modal-header">
              <div class="al-modal-title">LEAVE FACTION</div>
              <button class="al-modal-close" @click="confirmLeave = false">✕</button>
            </div>
            <p class="al-modal-text">Are you sure you want to leave {{ myAlliance?.name }}?</p>
            <button class="al-btn al-btn--danger al-modal-action" @click="doLeave">CONFIRM LEAVE</button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch, nextTick } from 'vue'
import {
  getAlliances, getAlliance, getPlayerAlliance, createAlliance,
  applyToAlliance, inviteToAlliance, acceptAllianceApplication,
  rejectAllianceApplication, acceptAllianceInvitation, rejectAllianceInvitation,
  getPlayerInvitations, kickAllianceMember, leaveAlliance, dissolveAlliance,
  updateAllianceSettings, setAllianceMemberRank,
  getAllianceForumTopics, createForumTopic, getForumTopic, addForumPost, deleteForumPost, deleteForumTopic,
  setAllianceMemberPermissions,
  searchPlayers as searchPlayersApi
} from '../services/api'

const props = defineProps({ player: Object, settlement: Object, refreshSettlement: Function })
const player = computed(() => props.player)

// ─── State ────────────────────────────────────────────────
const myAlliance    = ref(null)
const members       = ref([])
const alliances     = ref([])
const invitations   = ref([])
const pendingApplications = ref([])

const activeTab     = ref('browse')
const loadingAlliances = ref(false)
const eventsTab     = ref('events')

// No-alliance tabs
const noAllianceTabs = [
  { id: 'browse',      icon: '◈', label: 'BROWSE FACTIONS' },
  { id: 'invitations', icon: '✉', label: 'INVITATIONS' },
  { id: 'create',      icon: '⊕', label: 'FOUND FACTION' },
]

// In-alliance tabs
const allianceTabs = [
  { id: 'overview',    icon: '◈', label: 'OVERVIEW' },
  { id: 'members',     icon: '⚔', label: 'ROSTER' },
  { id: 'recruitment', icon: '✉', label: 'RECRUITMENT' },
  { id: 'settings',    icon: '⚙', label: 'SETTINGS' },
  { id: 'forum',       icon: '📋', label: 'FORUM' },
]

// My rank: 0=Founder, 1=Leader, 2=Officer, 3=Member
const myRankEntry = computed(() =>
  members.value.find(m => m.playerId === player.value?.id)
)
const myRank = computed(() => getRankValue(myRankEntry.value?.rank ?? 'Member'))

const myPermissions = computed(() => {
  const r = myRank.value
  if (r <= 2) return { canInvite: true, canManageRecruitment: true, isForumModerator: true, canBroadcast: true, canManageReservations: true }
  const e = myRankEntry.value
  return {
    canInvite:             e?.canInvite ?? false,
    canManageRecruitment:  e?.canManageRecruitment ?? false,
    isForumModerator:      e?.isForumModerator ?? false,
    canBroadcast:          e?.canBroadcast ?? false,
    canManageReservations: e?.canManageReservations ?? false,
  }
})

const pendingAppsCount = computed(() => pendingApplications.value.length)

const statusClass = computed(() => {
  if (!myAlliance.value) return ''
  return getStatusClass(myAlliance.value.status)
})

const statusDisplay = computed(() => {
  if (!myAlliance.value) return ''
  return statusToString(myAlliance.value.status).toUpperCase()
})

// ─── Create form ──────────────────────────────────────────
const createForm    = ref({ name: '', tag: '' })
const createError   = ref('')
const createLoading = ref(false)

// ─── Apply modal ──────────────────────────────────────────
const applyModal    = ref(null)
const applyMessage  = ref('')
const applyError    = ref('')
const applyLoading  = ref(false)
const expandedAlliance = ref(null)

// ─── Invite ───────────────────────────────────────────────
const inviteForm    = ref({ name: '', targetId: null, message: '' })
const inviteError   = ref('')
const inviteSuccess = ref('')
const inviteLoading = ref(false)
const playerSearchResults = ref([])
const searchDebounce = ref(null)

// ─── Settings ────────────────────────────────────────────
const settingsForm  = ref({ name: '', tag: '', description: '', status: 'ApplicationRequired', minPoints: 0 })
const settingsError = ref('')
const settingsSuccess = ref('')
const settingsLoading = ref(false)

// ─── Announcement ────────────────────────────────────────
const announcement  = ref('')
const announcementDraft = ref('')
const editingAnnouncement = ref(false)

// ─── Confirm modals ───────────────────────────────────────
const confirmDissolve = ref(false)
const confirmLeave    = ref(false)

// ─── Forum ───────────────────────────────────────────────
const forumTopics   = ref([])
const loadingForum  = ref(false)
const activeTopic   = ref(null)
const topicPosts    = ref([])
const loadingPosts  = ref(false)
const replyContent  = ref('')
const replyError    = ref('')
const replyLoading  = ref(false)
const showNewTopicForm = ref(false)
const newTopicForm  = ref({ title: '', content: '' })
const topicError    = ref('')
const topicLoading  = ref(false)

// Editor template refs
const replyEditorRef    = ref(null)
const newTopicEditorRef = ref(null)

// ─── Helpers ─────────────────────────────────────────────
const RANK_NAMES  = ['Founder', 'Leader', 'Officer', 'Member']
const STATUS_NAMES = ['Open', 'ApplicationRequired', 'Closed']

const PERMISSIONS = [
  { key: 'canInvite',             icon: '✉', label: 'Can Invite' },
  { key: 'canManageRecruitment',  icon: '◎', label: 'Manage Applications' },
  { key: 'canBroadcast',          icon: '📢', label: 'Can Broadcast' },
  { key: 'isForumModerator',      icon: '✎', label: 'Forum Moderator' },
  { key: 'canManageReservations', icon: '◈', label: 'Manage Reservations' },
]

function rankToString(rank) {
  if (typeof rank === 'number') return RANK_NAMES[rank] ?? 'Member'
  return rank ?? 'Member'
}

function statusToString(status) {
  if (typeof status === 'number') return STATUS_NAMES[status] ?? 'Closed'
  return status ?? ''
}

function getRankValue(rank) {
  const str = rankToString(rank)
  const map = { Founder: 0, Leader: 1, Officer: 2, Member: 3 }
  return map[str] ?? 3
}

function getStatusClass(status) {
  const s = statusToString(status)
  if (s === 'Open') return 'al-status--open'
  if (s === 'ApplicationRequired') return 'al-status--apply'
  return 'al-status--closed'
}

function formatDate(d) {
  if (!d) return '—'
  const dt = new Date(d)
  return dt.toLocaleDateString('nl-NL', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

// ─── Load ────────────────────────────────────────────────
async function loadData() {
  if (!player.value?.id) return
  try {
    const [allianceData, inviteData] = await Promise.all([
      getPlayerAlliance(player.value.id).catch(() => null),
      getPlayerInvitations(player.value.id).catch(() => [])
    ])

    invitations.value = inviteData ?? []

    if (allianceData) {
      myAlliance.value = allianceData
      members.value = allianceData.members ?? []
      pendingApplications.value = await loadPendingApps(allianceData.pendingApplications ?? [])
      populateSettings()
      activeTab.value = 'overview'
    } else {
      myAlliance.value = null
      loadAlliances()
      activeTab.value = 'browse'
    }
  } catch (e) {
    console.error('Alliance load error', e)
  }
}

async function loadPendingApps(apps) {
  const result = []
  for (const app of apps) {
    try {
      const players = await searchPlayersApi(app.playerId, null)
      const p = players?.[0]
      result.push({ ...app, playerName: p?.username ?? app.playerId })
    } catch {
      result.push({ ...app, playerName: String(app.playerId) })
    }
  }
  return result
}

async function loadAlliances() {
  loadingAlliances.value = true
  try {
    alliances.value = await getAlliances()
  } finally {
    loadingAlliances.value = false
  }
}

function populateSettings() {
  if (!myAlliance.value) return
  settingsForm.value = {
    name: myAlliance.value.name,
    tag: myAlliance.value.tag,
    description: myAlliance.value.description ?? '',
    status: statusToString(myAlliance.value.status),
    minPoints: myAlliance.value.minPoints || 0
  }
}

async function switchTab(tabId) {
  activeTab.value = tabId
  if (tabId === 'forum') loadForumTopics()
}

async function loadForumTopics() {
  if (!myAlliance.value?.id) return
  loadingForum.value = true
  try {
    forumTopics.value = await getAllianceForumTopics(myAlliance.value.id)
  } finally {
    loadingForum.value = false
  }
}

// ─── Create Alliance ─────────────────────────────────────
async function doCreateAlliance() {
  createError.value = ''
  if (!createForm.value.name.trim() || !createForm.value.tag.trim()) {
    createError.value = 'Both name and tag are required.'
    return
  }
  createLoading.value = true
  try {
    await createAlliance(player.value.id, createForm.value.name, createForm.value.tag)
    if (props.refreshSettlement) await props.refreshSettlement()
    await loadData()
  } catch (e) {
    const msg = e.response?.data
    createError.value = typeof msg === 'string' ? msg : 'Failed to create faction.'
  } finally {
    createLoading.value = false
  }
}

// ─── Apply to Alliance ────────────────────────────────────
function openApplyModal(a) {
  applyModal.value = a
  applyMessage.value = ''
  applyError.value = ''
}

async function doApply() {
  applyError.value = ''
  applyLoading.value = true
  try {
    await applyToAlliance(applyModal.value.id, player.value.id, applyMessage.value)
    applyModal.value = null
    if (props.refreshSettlement) await props.refreshSettlement()
    await loadData()
  } catch (e) {
    const msg = e.response?.data
    applyError.value = typeof msg === 'string' ? msg : 'Failed to apply.'
  } finally {
    applyLoading.value = false
  }
}

// ─── Accept / Reject invitation ───────────────────────────
async function doAcceptInvite(inv) {
  try {
    await acceptAllianceInvitation(inv.allianceId, inv.id, player.value.id)
    if (props.refreshSettlement) await props.refreshSettlement()
    await loadData()
  } catch (e) {
    console.error(e)
  }
}

async function doRejectInvite(inv) {
  try {
    await rejectAllianceInvitation(inv.allianceId, inv.id, player.value.id)
    invitations.value = invitations.value.filter(i => i.id !== inv.id)
  } catch (e) {
    console.error(e)
  }
}

// ─── Invite player ────────────────────────────────────────
async function searchPlayers() {
  clearTimeout(searchDebounce.value)
  if (!inviteForm.value.name.trim()) {
    playerSearchResults.value = []
    return
  }
  searchDebounce.value = setTimeout(async () => {
    try {
      const results = await searchPlayersApi(inviteForm.value.name, player.value?.id)
      playerSearchResults.value = (results ?? []).filter(p => !members.value.some(m => m.playerId === p.id))
    } catch { playerSearchResults.value = [] }
  }, 300)
}

function selectInvitePlayer(p) {
  inviteForm.value.name = p.username
  inviteForm.value.targetId = p.id
  playerSearchResults.value = []
}

async function doInvitePlayer() {
  inviteError.value = ''
  inviteSuccess.value = ''
  if (!inviteForm.value.targetId) return
  inviteLoading.value = true
  try {
    await inviteToAlliance(myAlliance.value.id, player.value.id, inviteForm.value.targetId, inviteForm.value.message)
    inviteSuccess.value = 'Invitation sent!'
    inviteForm.value = { name: '', targetId: null, message: '' }
  } catch (e) {
    inviteError.value = e.response?.data ?? 'Failed to send invitation.'
  } finally {
    inviteLoading.value = false
  }
}

// ─── Applications ────────────────────────────────────────
async function doAcceptApp(app) {
  try {
    await acceptAllianceApplication(myAlliance.value.id, app.id, player.value.id)
    pendingApplications.value = pendingApplications.value.filter(a => a.id !== app.id)
    await loadData()
  } catch (e) { console.error(e) }
}

async function doRejectApp(app) {
  try {
    await rejectAllianceApplication(myAlliance.value.id, app.id, player.value.id)
    pendingApplications.value = pendingApplications.value.filter(a => a.id !== app.id)
  } catch (e) { console.error(e) }
}

// ─── Kick / Rank ──────────────────────────────────────────
async function doKickMember(m) {
  if (!confirm(`Kick ${m.username} from the faction?`)) return
  try {
    await kickAllianceMember(myAlliance.value.id, m.playerId, player.value.id)
    await loadData()
  } catch (e) { console.error(e) }
}

async function doSetRank(m, rank) {
  try {
    await setAllianceMemberRank(myAlliance.value.id, m.playerId, player.value.id, rank)
    await loadData()
  } catch (e) { console.error(e) }
}

// ─── Settings ────────────────────────────────────────────
async function doSaveSettings() {
  settingsError.value = ''
  settingsSuccess.value = ''
  settingsLoading.value = true
  try {
    await updateAllianceSettings(
      myAlliance.value.id, player.value.id,
      settingsForm.value.name, settingsForm.value.tag,
      settingsForm.value.description, settingsForm.value.status,
      settingsForm.value.minPoints
    )
    settingsSuccess.value = 'Settings saved.'
    await loadData()
  } catch (e) {
    settingsError.value = e.response?.data ?? 'Failed to save settings.'
  } finally {
    settingsLoading.value = false
  }
}

async function doDissolve() {
  try {
    await dissolveAlliance(myAlliance.value.id, player.value.id)
    confirmDissolve.value = false
    if (props.refreshSettlement) await props.refreshSettlement()
    await loadData()
  } catch (e) { console.error(e) }
}

async function doLeave() {
  try {
    await leaveAlliance(myAlliance.value.id, player.value.id)
    confirmLeave.value = false
    if (props.refreshSettlement) await props.refreshSettlement()
    await loadData()
  } catch (e) { console.error(e) }
}

// ─── Announcement (local-only for now) ────────────────────
function saveAnnouncement() {
  announcement.value = announcementDraft.value
  editingAnnouncement.value = false
}

// ─── Forum ───────────────────────────────────────────────
async function openTopic(t) {
  activeTopic.value = t
  replyContent.value = ''
  replyError.value = ''
  loadingPosts.value = true
  try {
    const data = await getForumTopic(myAlliance.value.id, t.id)
    topicPosts.value = data.posts ?? []
  } finally {
    loadingPosts.value = false
  }
}

function closeTopic() {
  activeTopic.value = null
  topicPosts.value = []
}

async function doCreateTopic() {
  topicError.value = ''
  if (!newTopicForm.value.title.trim()) {
    topicError.value = 'Title is required.'
    return
  }
  topicLoading.value = true
  try {
    await createForumTopic(myAlliance.value.id, player.value.id, newTopicForm.value.title, newTopicForm.value.content)
    newTopicForm.value = { title: '', content: '' }
    showNewTopicForm.value = false
    await loadForumTopics()
  } catch (e) {
    topicError.value = e.response?.data ?? 'Failed to create topic.'
  } finally {
    topicLoading.value = false
  }
}

async function doAddPost() {
  replyError.value = ''
  if (!replyContent.value.trim()) return
  replyLoading.value = true
  try {
    const post = await addForumPost(myAlliance.value.id, activeTopic.value.id, player.value.id, replyContent.value)
    topicPosts.value.push(post)
    replyContent.value = ''
  } catch (e) {
    replyError.value = e.response?.data ?? 'Failed to post reply.'
  } finally {
    replyLoading.value = false
  }
}

async function doDeletePost(post) {
  if (!confirm('Delete this post?')) return
  try {
    await deleteForumPost(myAlliance.value.id, activeTopic.value.id, post.id, player.value.id)
    topicPosts.value = topicPosts.value.filter(p => p.id !== post.id)
  } catch (e) { console.error(e) }
}

async function doDeleteTopicFromList(t) {
  if (!confirm(`Delete topic "${t.title}"? This will remove all replies.`)) return
  try {
    await deleteForumTopic(myAlliance.value.id, t.id, player.value.id)
    forumTopics.value = forumTopics.value.filter(x => x.id !== t.id)
  } catch (e) { console.error(e) }
}

async function doTogglePermission(m, permKey) {
  if (myRank.value > 1) return
  const perms = {
    canInvite:             m.canInvite ?? false,
    canManageRecruitment:  m.canManageRecruitment ?? false,
    isForumModerator:      m.isForumModerator ?? false,
    canBroadcast:          m.canBroadcast ?? false,
    canManageReservations: m.canManageReservations ?? false,
  }
  perms[permKey] = !perms[permKey]
  try {
    await setAllianceMemberPermissions(myAlliance.value.id, m.playerId, player.value.id, perms)
    m[permKey] = perms[permKey]
  } catch (e) { console.error('Failed to update permission', e) }
}

// ─── Rich text editor helpers ─────────────────────────────
function insertFormat(textareaRef, open, close) {
  const el = textareaRef.value
  if (!el) return
  const start = el.selectionStart
  const end   = el.selectionEnd
  const val   = el.value
  const sel   = val.substring(start, end)
  el.value = val.substring(0, start) + open + sel + close + val.substring(end)
  el.dispatchEvent(new Event('input'))
  nextTick(() => {
    el.focus()
    el.setSelectionRange(start + open.length, start + open.length + sel.length)
  })
}

function insertListItem(textareaRef) {
  const el = textareaRef.value
  if (!el) return
  const pos = el.selectionStart
  const val = el.value
  const lineStart = val.lastIndexOf('\n', pos - 1) + 1
  el.value = val.substring(0, lineStart) + '• ' + val.substring(lineStart)
  el.dispatchEvent(new Event('input'))
  nextTick(() => {
    el.focus()
    el.setSelectionRange(pos + 2, pos + 2)
  })
}

function renderContent(raw) {
  if (!raw) return ''
  // 1. Sanitize HTML
  let s = raw
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
  // 2. BBCode → HTML
  s = s
    .replace(/\[b\]([\s\S]*?)\[\/b\]/gi,     '<strong>$1</strong>')
    .replace(/\[i\]([\s\S]*?)\[\/i\]/gi,     '<em>$1</em>')
    .replace(/\[u\]([\s\S]*?)\[\/u\]/gi,     '<u>$1</u>')
    .replace(/\[s\]([\s\S]*?)\[\/s\]/gi,     '<s>$1</s>')
    .replace(/\[code\]([\s\S]*?)\[\/code\]/gi, '<code>$1</code>')
    .replace(/\[pre\]([\s\S]*?)\[\/pre\]/gi,  '<pre><code>$1</code></pre>')
    .replace(/\[quote\]([\s\S]*?)\[\/quote\]/gi, '<blockquote>$1</blockquote>')
  // 3. Collapse bullet lines into <ul>
  s = s.replace(/((?:^|\n)• .+)+/g, block => {
    const items = block.trim().split('\n').map(l => `<li>${l.replace(/^• /, '')}</li>`).join('')
    return `\n<ul>${items}</ul>`
  })
  // 4. Newlines to <br> (skip block-level elements)
  s = s.replace(/\n(?!<\/?(?:ul|li|pre|blockquote))/g, '<br>')
  return s
}

onMounted(loadData)
</script>

<style scoped>
/* ════════════════════════════════════════════════
   ALLIANCE VIEW — The Fallen Wastes
   ════════════════════════════════════════════════ */

.alliance-wrap {
  height: 100%;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

/* ── Header ───────────────────────────────────── */
.al-header {
  display: flex;
  align-items: baseline;
  gap: 16px;
  padding: 0 0 12px 0;
  flex-shrink: 0;
}

.al-title {
  font-family: var(--ff-title);
  font-size: 16px;
  color: var(--cyan);
  letter-spacing: 3px;
  font-weight: 700;
  text-shadow: 0 0 10px rgba(0, 212, 255, 0.15);
}

.al-sub {
  font-size: 10px;
  color: var(--muted);
  font-family: var(--ff-title);
  letter-spacing: 2px;
}

.al-header-right {
  margin-left: auto;
  display: flex;
  align-items: center;
  gap: 12px;
}

.al-badge {
  font-size: 8px;
  font-family: var(--ff-title);
  letter-spacing: 2px;
  padding: 2px 8px;
  border: 1px solid;
  font-weight: 700;
}

.al-members {
  font-size: 9px;
  color: var(--muted);
  font-family: var(--ff-title);
  letter-spacing: 1px;
}

/* ── Tabs ─────────────────────────────────────── */
.al-tabs {
  display: flex;
  gap: 0;
  border-bottom: 1px solid var(--border);
  flex-shrink: 0;
  overflow-x: auto;
}

.al-tab {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 10px 18px;
  background: transparent;
  border: none;
  border-bottom: 2px solid transparent;
  color: var(--muted);
  font-family: var(--ff-title);
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 2px;
  cursor: pointer;
  transition: all 0.15s;
  white-space: nowrap;
  position: relative;
}

.al-tab:hover { color: var(--text); }
.al-tab--active { color: var(--cyan); border-bottom-color: var(--cyan); }

.al-tab-icon { font-size: 11px; }

.al-tab-badge {
  background: var(--cyan);
  color: var(--bg);
  font-size: 8px;
  font-family: var(--ff-title);
  font-weight: 700;
  border-radius: 8px;
  padding: 1px 5px;
  min-width: 16px;
  text-align: center;
}

/* ── Panel ────────────────────────────────────── */
.al-panel {
  flex: 1;
  overflow-y: auto;
  padding: 20px 0;
}

.al-panel--two-col {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 24px;
  align-items: start;
}

.al-col { display: flex; flex-direction: column; gap: 12px; }

/* ── Section header ───────────────────────────── */
.al-section-hdr {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 12px;
  padding-bottom: 8px;
  border-bottom: 1px solid var(--border);
  font-family: var(--ff-title);
  font-size: 9px;
  color: var(--cyan);
  font-weight: 700;
  letter-spacing: 3px;
}

.al-section-sub {
  font-size: 8px;
  color: var(--muted);
  font-family: var(--ff-title);
  letter-spacing: 1px;
  margin-left: auto;
}

/* ── Tables ───────────────────────────────────── */
.al-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 12px;
}

.al-table thead tr {
  border-bottom: 1px solid var(--border);
}

.al-table th {
  font-family: var(--ff-title);
  font-size: 8px;
  color: var(--muted);
  letter-spacing: 2px;
  font-weight: 700;
  padding: 6px 10px;
  text-align: left;
}

.al-table td {
  padding: 9px 10px;
  border-bottom: 1px solid rgba(20, 32, 48, 0.6);
  color: var(--text);
  vertical-align: middle;
}

.al-row { cursor: pointer; transition: background 0.1s; }
.al-row:hover { background: rgba(0, 212, 255, 0.03); }
.al-row--expanded { background: rgba(0, 212, 255, 0.04); }

.al-name-cell { color: var(--bright); font-weight: 600; }
.al-score-cell { color: var(--cyan); font-family: var(--ff-title); font-weight: 700; }
.al-date-cell { font-size: 10px; color: var(--muted); }
.al-muted { color: var(--muted); }
.al-empty { text-align: center; color: var(--muted); padding: 24px; font-size: 11px; }

/* ── Alliance tag ─────────────────────────────── */
.al-tag {
  font-family: var(--ff-title);
  font-size: 10px;
  font-weight: 700;
  color: var(--cyan);
  background: var(--cyan-glow);
  border: 1px solid var(--cyan-dim);
  padding: 2px 7px;
  letter-spacing: 1px;
}

/* ── Status indicator ────────────────────────── */
.al-status-dot {
  font-size: 9px;
  font-family: var(--ff-title);
  letter-spacing: 1px;
  padding: 2px 8px;
  border: 1px solid;
}
.al-status--open { color: var(--green); border-color: rgba(48, 255, 128, 0.3); background: rgba(48, 255, 128, 0.06); }
.al-status--apply { color: var(--amber); border-color: rgba(255, 170, 32, 0.3); background: rgba(255, 170, 32, 0.06); }
.al-status--closed { color: var(--muted); border-color: var(--border); background: transparent; }

/* al-badge uses same status classes */
.al-badge.al-status--open { color: var(--green); border-color: rgba(48, 255, 128, 0.3); }
.al-badge.al-status--apply { color: var(--amber); border-color: rgba(255, 170, 32, 0.3); }
.al-badge.al-status--closed { color: var(--muted); border-color: var(--border); }

/* ── Rank badge ───────────────────────────────── */
.al-rank-badge {
  font-family: var(--ff-title);
  font-size: 8px;
  font-weight: 700;
  letter-spacing: 1px;
  padding: 2px 8px;
  border: 1px solid;
}
.al-rank--founder { color: #ffc830; border-color: rgba(255, 200, 48, 0.4); background: rgba(255, 200, 48, 0.08); }
.al-rank--leader  { color: var(--cyan); border-color: rgba(0, 212, 255, 0.3); background: rgba(0, 212, 255, 0.06); }
.al-rank--officer { color: var(--amber); border-color: rgba(255, 170, 32, 0.3); background: rgba(255, 170, 32, 0.06); }
.al-rank--member  { color: var(--text); border-color: var(--border); background: transparent; }

/* ── Buttons ──────────────────────────────────── */
.al-btn {
  background: var(--bg3);
  border: 1px solid var(--border-bright);
  color: var(--text);
  font-family: var(--ff-title);
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 2px;
  padding: 8px 16px;
  cursor: pointer;
  transition: all 0.15s;
  white-space: nowrap;
}
.al-btn:hover:not(:disabled) { border-color: var(--cyan-dim); color: var(--bright); }
.al-btn:disabled { opacity: 0.4; cursor: not-allowed; }

.al-btn--primary {
  background: linear-gradient(180deg, var(--cyan-dark), var(--cyan-dim));
  border-color: var(--cyan);
  color: #fff;
  box-shadow: 0 0 16px rgba(0, 212, 255, 0.15);
}
.al-btn--primary:hover:not(:disabled) { box-shadow: 0 0 24px rgba(0, 212, 255, 0.3); }

.al-btn--danger { border-color: rgba(255, 48, 64, 0.5); color: var(--red); }
.al-btn--danger:hover:not(:disabled) { background: rgba(255, 48, 64, 0.08); border-color: var(--red); }

.al-btn--accept { border-color: rgba(48, 255, 128, 0.4); color: var(--green); }
.al-btn--accept:hover:not(:disabled) { background: rgba(48, 255, 128, 0.08); }

.al-btn--reject { border-color: var(--border); color: var(--muted); }
.al-btn--reject:hover:not(:disabled) { border-color: rgba(255, 48, 64, 0.4); color: var(--red); }

.al-btn--sm { padding: 5px 12px; font-size: 9px; }
.al-btn--xs { padding: 3px 8px; font-size: 8px; }

/* ── Forms ────────────────────────────────────── */
.al-form {
  display: flex;
  flex-direction: column;
  gap: 14px;
  max-width: 480px;
}

.al-form--wide { max-width: 100%; }

.al-form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px;
}

.al-form-row { display: flex; flex-direction: column; gap: 6px; position: relative; }
.al-form-row--full { grid-column: 1 / -1; }

.al-label {
  font-family: var(--ff-title);
  font-size: 8px;
  color: var(--cyan);
  letter-spacing: 2px;
  font-weight: 700;
}
.al-label-sub { color: var(--muted); font-size: 7px; font-weight: 400; letter-spacing: 0; }

.al-input {
  background: var(--bg3);
  border: 1px solid var(--border-bright);
  color: var(--bright);
  font-family: var(--ff);
  font-size: 12px;
  padding: 8px 12px;
  outline: none;
  transition: border-color 0.15s;
}
.al-input:focus { border-color: var(--cyan-dim); }
.al-input[type=number] { -moz-appearance: textfield; }
.al-input[type=number]::-webkit-outer-spin-button,
.al-input[type=number]::-webkit-inner-spin-button { -webkit-appearance: none; margin: 0; }
.al-input--sm { max-width: 120px; }

/* ── Custom number input ─────────────────────── */
.al-num-wrap { display: inline-flex; align-items: stretch; }
.al-num-input { max-width: 80px; border-right: none !important; }
.al-num-btn-group {
  display: flex;
  flex-direction: column;
  border: 1px solid var(--border-bright);
  border-left: none;
}
.al-num-btn {
  background: var(--bg3);
  border: none;
  color: var(--muted);
  cursor: pointer;
  padding: 0 7px;
  font-size: 7px;
  line-height: 1;
  flex: 1;
  transition: all 0.12s;
  min-width: 22px;
}
.al-num-btn:first-child { border-bottom: 1px solid var(--border-bright); }
.al-num-btn:hover { background: var(--bg); color: var(--cyan); }

.al-textarea {
  background: var(--bg3);
  border: 1px solid var(--border-bright);
  color: var(--bright);
  font-family: var(--ff);
  font-size: 12px;
  padding: 8px 12px;
  outline: none;
  resize: vertical;
  min-height: 80px;
  transition: border-color 0.15s;
}
.al-textarea:focus { border-color: var(--cyan-dim); }

.al-select {
  background: var(--bg3);
  border: 1px solid var(--border-bright);
  color: var(--bright);
  font-family: var(--ff);
  font-size: 12px;
  padding: 8px 12px;
  outline: none;
  cursor: pointer;
}

.al-select--sm { font-size: 10px; padding: 4px 8px; }

.al-form-actions { display: flex; gap: 10px; align-items: center; }

/* ── Search Dropdown ──────────────────────────── */
.al-search-dropdown {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  background: var(--bg2);
  border: 1px solid var(--border-bright);
  z-index: 100;
  max-height: 200px;
  overflow-y: auto;
}

.al-search-item {
  padding: 8px 12px;
  cursor: pointer;
  font-size: 12px;
  display: flex;
  justify-content: space-between;
  transition: background 0.1s;
}
.al-search-item:hover { background: rgba(0, 212, 255, 0.05); color: var(--bright); }
.al-search-score { color: var(--muted); font-size: 10px; }

/* ── Feedback ─────────────────────────────────── */
.al-error {
  font-size: 10px;
  color: var(--red);
  padding: 6px 10px;
  background: rgba(255, 48, 64, 0.06);
  border: 1px solid rgba(255, 48, 64, 0.2);
  font-family: var(--ff-title);
  letter-spacing: 1px;
}
.al-success {
  font-size: 10px;
  color: var(--green);
  padding: 6px 10px;
  background: rgba(48, 255, 128, 0.06);
  border: 1px solid rgba(48, 255, 128, 0.2);
  font-family: var(--ff-title);
  letter-spacing: 1px;
}

/* ── Empty state ──────────────────────────────── */
.al-empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 40px 20px;
  gap: 8px;
  color: var(--muted);
  font-size: 11px;
}
.al-empty-icon { font-size: 24px; opacity: 0.3; }

/* ── Loading ──────────────────────────────────── */
.al-loading {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 20px;
  color: var(--muted);
  font-family: var(--ff-title);
  font-size: 9px;
  letter-spacing: 2px;
}

.al-spinner {
  width: 16px;
  height: 16px;
  border: 2px solid var(--border);
  border-top-color: var(--cyan);
  border-radius: 50%;
  animation: radarSweep 0.8s linear infinite;
}

/* ── Invite/App cards ─────────────────────────── */
.al-invite-card, .al-app-card {
  background: var(--bg3);
  border: 1px solid var(--border);
  padding: 14px 16px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  transition: border-color 0.15s;
}
.al-invite-card:hover, .al-app-card:hover { border-color: var(--border-bright); }

.al-invite-info, .al-app-header {
  display: flex;
  align-items: center;
  gap: 10px;
}
.al-invite-name, .al-app-player { color: var(--bright); font-weight: 600; font-size: 12px; }
.al-app-date { margin-left: auto; font-size: 10px; color: var(--muted); }
.al-invite-msg, .al-app-msg { font-size: 11px; color: var(--text); padding: 6px; background: rgba(0,0,0,0.2); }
.al-invite-actions, .al-app-actions { display: flex; gap: 8px; }

/* ── Events ───────────────────────────────────── */
.al-events-tabs { display: flex; gap: 4px; margin-left: auto; }
.al-evt-tab {
  background: transparent;
  border: 1px solid var(--border);
  color: var(--muted);
  font-family: var(--ff-title);
  font-size: 8px;
  font-weight: 700;
  letter-spacing: 1px;
  padding: 3px 10px;
  cursor: pointer;
  transition: all 0.15s;
}
.al-evt-tab--active { border-color: var(--cyan-dim); color: var(--cyan); }

.al-events-list { display: flex; flex-direction: column; gap: 4px; }

.al-event-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 10px;
  border-left: 2px solid var(--cyan-dim);
  background: rgba(0, 212, 255, 0.02);
}
.al-event-icon { color: var(--cyan-dim); font-size: 12px; }
.al-event-text { display: flex; flex-direction: column; gap: 2px; }
.al-event-actor { font-size: 11px; color: var(--text); }
.al-event-time { font-size: 9px; color: var(--muted); }
.al-event-empty { font-size: 10px; color: var(--muted); padding: 12px; }

/* ── Announcement ─────────────────────────────── */
.al-announcement {
  background: var(--bg3);
  border: 1px solid var(--border);
  padding: 14px 16px;
  font-size: 12px;
  color: var(--text);
  min-height: 80px;
  line-height: 1.6;
}
.al-announcement-edit { display: flex; flex-direction: column; gap: 8px; }

/* ── Member actions ───────────────────────────── */
.al-member-actions { display: flex; gap: 6px; align-items: center; }
.al-online-dot {
  display: inline-block;
  width: 6px; height: 6px;
  border-radius: 50%;
  background: var(--green);
  box-shadow: 0 0 4px rgba(48, 255, 128, 0.5);
  margin-right: 4px;
}
.al-you-tag {
  font-size: 8px;
  font-family: var(--ff-title);
  color: var(--cyan);
  border: 1px solid var(--cyan-dim);
  padding: 1px 5px;
  letter-spacing: 1px;
  margin-left: 4px;
}

/* ── Settings / Danger zone ───────────────────── */
.al-danger-zone {
  margin-top: 32px;
  padding-top: 20px;
  border-top: 1px solid rgba(255, 48, 64, 0.2);
}
.al-danger-title {
  font-family: var(--ff-title);
  font-size: 8px;
  color: var(--red);
  letter-spacing: 3px;
  margin-bottom: 12px;
}
.al-leave-zone {
  margin-top: 24px;
  padding-top: 16px;
  border-top: 1px solid var(--border);
}

/* ── Forum ────────────────────────────────────── */
.al-forum-table .al-row { cursor: pointer; }
.al-pin { margin-right: 4px; font-size: 11px; }

.al-back-btn {
  background: transparent;
  border: 1px solid var(--border);
  color: var(--muted);
  font-family: var(--ff-title);
  font-size: 9px;
  font-weight: 700;
  letter-spacing: 1px;
  padding: 4px 12px;
  cursor: pointer;
  transition: all 0.15s;
}
.al-back-btn:hover { border-color: var(--cyan-dim); color: var(--cyan); }

.al-topic-title {
  font-size: 12px;
  color: var(--bright);
  font-weight: 600;
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.al-thread { display: flex; flex-direction: column; gap: 2px; margin-bottom: 20px; }

.al-post {
  background: var(--bg3);
  border: 1px solid var(--border);
  transition: border-color 0.1s;
}
.al-post:hover { border-color: var(--border-bright); }

.al-post-meta {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 14px;
  border-bottom: 1px solid var(--border);
  background: rgba(0, 0, 0, 0.2);
}
.al-post-author { color: var(--cyan); font-family: var(--ff-title); font-size: 10px; letter-spacing: 1px; font-weight: 700; }
.al-post-time { color: var(--muted); font-size: 9px; }
.al-post-edited { color: var(--muted); font-size: 9px; font-style: italic; }
.al-post-delete {
  margin-left: auto;
  background: transparent;
  border: none;
  color: var(--muted);
  cursor: pointer;
  font-size: 10px;
  padding: 2px 6px;
  transition: color 0.1s;
}
.al-post-delete:hover { color: var(--red); }
.al-post-content {
  padding: 12px 14px;
  font-size: 12px;
  color: var(--text);
  line-height: 1.7;
  word-break: break-word;
}
.al-post-content strong { color: var(--text); font-weight: 700; }
.al-post-content em     { color: var(--text); font-style: italic; }
.al-post-content u      { text-decoration: underline; text-underline-offset: 2px; }
.al-post-content s      { color: var(--muted); }
.al-post-content code {
  background: var(--bg);
  color: var(--cyan);
  border: 1px solid var(--border);
  border-radius: 3px;
  padding: 1px 5px;
  font-family: 'Courier New', monospace;
  font-size: 11px;
}
.al-post-content pre {
  background: var(--bg);
  border: 1px solid var(--border);
  border-left: 3px solid var(--cyan);
  padding: 10px 12px;
  margin: 6px 0;
  border-radius: 3px;
  overflow-x: auto;
}
.al-post-content pre code {
  background: none;
  border: none;
  padding: 0;
  color: var(--cyan);
  font-size: 11px;
  white-space: pre;
}
.al-post-content blockquote {
  border-left: 3px solid var(--border-bright);
  margin: 6px 0;
  padding: 6px 12px;
  color: var(--muted);
  font-style: italic;
  background: rgba(255,255,255,0.02);
}
.al-post-content ul {
  margin: 6px 0;
  padding-left: 20px;
}
.al-post-content li { margin-bottom: 2px; }

/* ── Forum editor toolbar ─────────────────────── */
.al-editor { display: flex; flex-direction: column; gap: 0; }
.al-editor-toolbar {
  display: flex;
  align-items: center;
  gap: 2px;
  background: var(--bg);
  border: 1px solid var(--border);
  border-bottom: none;
  padding: 4px 6px;
  border-radius: 3px 3px 0 0;
  flex-wrap: wrap;
}
.al-editor .al-textarea {
  border-radius: 0 0 3px 3px;
  border-top: none;
}
.al-editor-btn {
  background: transparent;
  border: 1px solid transparent;
  color: var(--muted);
  font-family: var(--ff-body);
  font-size: 11px;
  padding: 2px 7px;
  cursor: pointer;
  border-radius: 2px;
  transition: all 0.12s;
  line-height: 18px;
  min-width: 24px;
  text-align: center;
}
.al-editor-btn:hover {
  background: var(--bg3);
  border-color: var(--border);
  color: var(--cyan);
}
.al-editor-btn--i i { font-style: italic; }
.al-editor-btn--u   { text-decoration: underline; text-underline-offset: 2px; }
.al-editor-btn--s s { text-decoration: line-through; }
.al-editor-sep {
  width: 1px;
  height: 14px;
  background: var(--border);
  margin: 0 4px;
}

/* ── Forum topic actions column ───────────────── */
.al-forum-actions { width: 36px; text-align: right; padding-right: 6px; }

/* ── Member permission toggles ────────────────── */
.al-perm-cell { padding: 6px 8px; }
.al-perm-icons { display: flex; gap: 3px; align-items: center; }
.al-perm-icon {
  background: transparent;
  border: 1px solid var(--border);
  color: var(--muted);
  font-size: 11px;
  width: 22px;
  height: 22px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 2px;
  cursor: pointer;
  transition: all 0.12s;
  padding: 0;
  line-height: 1;
}
.al-perm-icon:hover { border-color: var(--cyan-dim); color: var(--cyan); background: rgba(0,212,255,0.06); }
.al-perm-icon--on { border-color: var(--cyan); color: var(--cyan); background: rgba(0,212,255,0.1); }
.al-perm-icon--locked {
  width: 22px; height: 22px;
  display: inline-flex; align-items: center; justify-content: center;
  font-size: 11px;
  border: 1px solid var(--border);
  border-radius: 2px;
  color: var(--cyan);
  opacity: 0.5;
  cursor: default;
}

.al-reply-box { display: flex; flex-direction: column; gap: 10px; }

.al-new-topic-form {
  background: var(--bg3);
  border: 1px solid var(--border-bright);
  padding: 16px;
  margin-bottom: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

/* ── Modals ───────────────────────────────────── */
.al-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.88);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 2000;
}

.al-modal {
  background: var(--bg2);
  border: 1px solid var(--border-bright);
  max-width: 440px;
  width: 92%;
  box-shadow: 0 0 60px rgba(0, 212, 255, 0.06);
  animation: fadeInUp 0.2s ease-out;
}

.al-modal--danger {
  border-color: rgba(255, 48, 64, 0.3);
  box-shadow: 0 0 40px rgba(255, 48, 64, 0.06);
}

.al-modal-accent {
  height: 2px;
  background: linear-gradient(90deg, transparent, var(--cyan), transparent);
}
.al-modal-accent--red {
  background: linear-gradient(90deg, transparent, var(--red), transparent);
}

.al-modal-body { padding: 20px 24px; display: flex; flex-direction: column; gap: 14px; }

.al-modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 2px;
}

.al-modal-title {
  font-family: var(--ff-title);
  font-size: 8px;
  color: var(--cyan);
  letter-spacing: 3px;
  font-weight: 700;
}

.al-modal-name {
  font-family: var(--ff-title);
  font-size: 14px;
  color: var(--bright);
  font-weight: 700;
  letter-spacing: 1px;
  margin-top: 4px;
}

.al-modal-close {
  background: var(--bg3);
  border: 1px solid var(--border);
  color: var(--muted);
  cursor: pointer;
  padding: 4px 10px;
  font-family: var(--ff-title);
  font-size: 10px;
  transition: all 0.15s;
}
.al-modal-close:hover { border-color: var(--cyan-dim); color: var(--text); }

.al-modal-text { font-size: 11px; color: var(--text); line-height: 1.6; }

.al-modal-action { width: 100%; padding: 12px; font-size: 11px; }
</style>
