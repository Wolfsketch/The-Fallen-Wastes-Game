import axios from 'axios'

const api = axios.create({
    baseURL: '/api',
    headers: {
        'Content-Type': 'application/json'
    }
})

// ── Players ──────────────────────────────────

export async function createPlayer(username, email, settlementName) {
    const response = await api.post('/Players', {
        username,
        email,
        settlementName
    })
    return response.data
}

export async function getPlayerById(id) {
    const response = await api.get(`/Players/${id}`)
    return response.data
}

export async function getPlayerRanking(type = 'total') {
    const response = await api.get(`/Players/ranking?type=${type}`)
    return response.data
}

export async function getPlayerPublicProfile(playerId) {
    const response = await api.get(`/Players/${playerId}/public-profile`)
    return response.data
}

export async function loginByUsername(username) {
    const response = await api.get(`/Players/login/${username}`)
    return response.data
}

// ── Advisors ─────────────────────────────────

export async function getPlayerAdvisors(playerId) {
    const response = await api.get(`/Players/${playerId}/advisors`)
    return response.data
}

export async function activateAdvisor(playerId, advisorId, durationDays = 14) {
    const response = await api.post(`/Players/${playerId}/advisors/${advisorId}/activate`, {
        durationDays
    })
    return response.data
}

export async function deactivateAdvisor(playerId, advisorId) {
    const response = await api.post(`/Players/${playerId}/advisors/${advisorId}/deactivate`)
    return response.data
}

export async function purchaseWastelandCoins(playerId, packageId) {
    const response = await api.post(`/Players/${playerId}/purchase-coins`, { packageId })
    return response.data
}

export async function organizeTriumph(playerId) {
    const response = await api.post(`/Players/${playerId}/triumph`)
    return response.data
}
// ── Settlements ──────────────────────────────

export async function getSettlement(id) {
    const response = await api.get(`/Settlements/${id}`)
    return response.data
}

export async function getBuildings(settlementId) {
    const response = await api.get(`/Settlements/${settlementId}/buildings`)
    return response.data
}

export async function getBuildQueue(settlementId) {
    const response = await api.get(`/Settlements/${settlementId}/queue`)
    return response.data
}

export async function upgradeBuilding(settlementId, buildingType) {
    const response = await api.post(`/Settlements/${settlementId}/buildings/upgrade`, {
        buildingType
    })
    return response.data
}

export async function cancelBuilding(settlementId, buildingType, targetLevel) {
    const response = await api.post(`/Settlements/${settlementId}/buildings/cancel`, {
        buildingType,
        ...(targetLevel != null ? { targetLevel } : {})
    })
    return response.data
}

export async function instantFinishBuilding(settlementId) {
    const response = await api.post(`/Settlements/${settlementId}/buildings/instant-finish`)
    return response.data
}

export async function getWorldSettlements(seed, playerId) {
    const response = await api.get(`/Settlements/world/${seed}`, {
        headers: playerId ? { 'X-Player-Id': playerId } : {}
    })
    return response.data
}

// ── Units ────────────────────────────────────

export async function getUnits() {
    const response = await api.get('/Units')
    return response.data
}

export async function trainUnit(settlementId, unitName, quantity = 1) {
    const response = await api.post(`/Settlements/${settlementId}/units/train`, {
        unitName,
        quantity
    })
    return response.data
}

export async function getUnitTrainingQueue(settlementId) {
    const response = await api.get(`/Settlements/${settlementId}/units/queue`)
    return response.data
}

export async function completeReadyUnitTraining(settlementId) {
    const response = await api.post(`/Settlements/${settlementId}/units/complete-ready`)
    return response.data
}

// ── Research ─────────────────────────────────

export async function getResearchState(settlementId) {
    const response = await api.get(`/Research/${settlementId}`)
    return response.data
}

export async function startResearch(settlementId, researchKey) {
    const response = await api.post(`/Research/${settlementId}/start`, {
        researchKey
    })
    return response.data
}

export async function cancelResearch(settlementId, researchKey) {
    const response = await api.post(`/Research/${settlementId}/cancel`, {
        researchKey
    })
    return response.data
}

export async function completeReadyResearch(settlementId) {
    const response = await api.post(`/Research/${settlementId}/complete-ready`)
    return response.data
}

// ── Commander ────────────────────────────────

export async function activateCommanderApi(settlementId, days = 7) {
    const response = await api.post(`/Settlements/${settlementId}/commander/activate`, { days })
    return response.data
}

export async function deactivateCommanderApi(settlementId) {
    const response = await api.post(`/Settlements/${settlementId}/commander/deactivate`)
    return response.data
}

// ── Diplomacy ────────────────────────────────

export async function setPlayerRelation(playerId, targetPlayerId, type) {
    const response = await api.post(`/Players/${playerId}/relations`, {
        targetPlayerId,
        type
    })
    return response.data
}

export async function removePlayerRelation(playerId, targetPlayerId) {
    const response = await api.delete(`/Players/${playerId}/relations/${targetPlayerId}`)
    return response.data
}

// ── Messages ─────────────────────────────────

export async function getInboxMessages(playerId) {
    const response = await api.get(`/messages/inbox/${playerId}`)
    return response.data
}

export async function sendPlayerMessage(payload) {
    const response = await api.post('/messages/send', payload)
    return response.data
}

export async function searchPlayers(query, excludePlayerId) {
    const response = await api.get('/messages/search', {
        params: {
            q: query,
            excludePlayerId
        }
    })
    return response.data
}

export async function getSentMessages(playerId) {
    const response = await api.get(`/messages/sent/${playerId}`)
    return response.data
}

export async function getUnreadMessageCount(playerId) {
    const response = await api.get(`/messages/unread-count/${playerId}`)
    return response.data
}

export async function markMessageAsRead(messageId) {
    const response = await api.post(`/messages/mark-read/${messageId}`)
    return response.data
}

export async function markMessageAsUnread(messageId) {
    const response = await api.post(`/Messages/mark-unread/${messageId}`)
    return response.data
}

// Operations
export async function getSettlementOperations(settlementId) {
    const response = await api.get(`/Operations/settlement/${settlementId}`)
    return response.data
}

export async function scoutPoi(settlementId, targetPoiId, rareTechAmount, targetPoiLabel = '', travelSeconds = 60) {
    const response = await api.post(`/Operations/settlement/${settlementId}/scout-poi`,
        { targetPoiId, rareTechAmount, targetPoiLabel, travelSeconds })
    return response.data
}

export async function scoutSettlement(settlementId, targetSettlementId, rareTechAmount, travelSeconds = 120) {
    const response = await api.post(`/Operations/settlement/${settlementId}/scout-settlement`,
        { targetSettlementId, rareTechAmount, travelSeconds })
    return response.data
}

export async function attackPoi(settlementId, targetPoiId, units, raidMode, travelSeconds = 300) {
    const response = await api.post(`/Operations/settlement/${settlementId}/attack-poi`,
        { targetPoiId, units, raidMode, travelSeconds })
    return response.data
}

export async function attackSettlement(settlementId, targetSettlementId, units, travelSeconds = 120) {
    const response = await api.post(`/Operations/settlement/${settlementId}/attack-settlement`,
        { targetSettlementId, units, travelSeconds })
    return response.data
}

export async function reinforcePoi(settlementId, targetPoiId, units, travelSeconds = 120) {
    const response = await api.post(`/Operations/settlement/${settlementId}/reinforce-poi`,
        { targetPoiId, units, travelSeconds })
    return response.data
}

export async function reinforceSettlement(settlementId, targetSettlementId, units, travelSeconds = 120) {
    const response = await api.post(`/Operations/settlement/${settlementId}/reinforce-settlement`,
        { targetSettlementId, units, travelSeconds })
    return response.data
}

export async function sendResources(settlementId, targetSettlementId, resources, travelSeconds = 120) {
    const response = await api.post(`/Operations/settlement/${settlementId}/send-resources`,
        { targetSettlementId, ...resources, travelSeconds })
    return response.data
}

export async function getPoiStates(settlementId) {
    const response = await api.get('/Operations/poi-states', {
        params: settlementId ? { settlementId } : {}
    })
    return response.data
}

export async function recallOperation(settlementId, operationId) {
    const response = await api.post(
        `/Operations/settlement/${settlementId}/recall/${operationId}`
    )
    return response.data
}

export async function depositToVault(settlementId, amount) {
    const response = await api.post(`/Settlements/${settlementId}/vault/deposit`, { amount })
    return response.data
}

export async function deleteMessage(messageId, playerId) {
    const response = await api.delete(`/Messages/${messageId}/player/${playerId}`)
    return response.data
}

// ── Settlement rename + founding ─────────────────────────────
export async function renameSettlement(settlementId, name, playerId) {
    const response = await api.patch(`/Settlements/${settlementId}/rename`, { name }, {
        headers: { 'X-Player-Id': playerId }
    })
    return response.data
}

export async function foundSettlement(playerId, sourceSettlementId, name, convoy, travelSeconds, sectorX, sectorY) {
    const response = await api.post('/Settlements/found', { playerId, sourceSettlementId, name, convoy, travelSeconds, sectorX, sectorY })
    return response.data
}

export async function getPlayerReports(playerId) {
    const response = await api.get(`/messages/${playerId}/reports`)
    return response.data
}

// Reports
export async function getReportMessages(playerId) {
    const response = await api.get(`/Messages/${playerId}/reports`)
    return response.data
}

// ── Alliances ─────────────────────────────────

export async function getAlliances() {
    const response = await api.get('/alliances')
    return response.data
}

export async function getAlliance(id) {
    const response = await api.get(`/alliances/${id}`)
    return response.data
}

export async function getPlayerAlliance(playerId) {
    const response = await api.get(`/alliances/player/${playerId}`)
    return response.data
}

export async function createAlliance(playerId, name, tag) {
    const response = await api.post('/alliances', { playerId, name, tag })
    return response.data
}

export async function applyToAlliance(allianceId, playerId, message = '') {
    const response = await api.post(`/alliances/${allianceId}/apply`, { playerId, message })
    return response.data
}

export async function inviteToAlliance(allianceId, requesterId, targetPlayerId, message = '') {
    const response = await api.post(`/alliances/${allianceId}/invite`, { requesterId, targetPlayerId, message })
    return response.data
}

export async function acceptAllianceApplication(allianceId, appId, requesterId) {
    const response = await api.post(`/alliances/${allianceId}/applications/${appId}/accept`, { requesterId })
    return response.data
}

export async function rejectAllianceApplication(allianceId, appId, requesterId) {
    const response = await api.post(`/alliances/${allianceId}/applications/${appId}/reject`, { requesterId })
    return response.data
}

export async function acceptAllianceInvitation(allianceId, appId, playerId) {
    const response = await api.post(`/alliances/${allianceId}/invitations/${appId}/accept`, { playerId })
    return response.data
}

export async function rejectAllianceInvitation(allianceId, appId, playerId) {
    const response = await api.post(`/alliances/${allianceId}/invitations/${appId}/reject`, { playerId })
    return response.data
}

export async function getPlayerInvitations(playerId) {
    const response = await api.get(`/alliances/invitations/player/${playerId}`)
    return response.data
}

export async function getPlayerAllianceApplications(playerId) {
    const response = await api.get(`/alliances/applications/player/${playerId}`)
    return response.data
}

export async function kickAllianceMember(allianceId, targetPlayerId, requesterId) {
    const response = await api.post(`/alliances/${allianceId}/kick/${targetPlayerId}`, { requesterId })
    return response.data
}

export async function leaveAlliance(allianceId, playerId) {
    const response = await api.post(`/alliances/${allianceId}/leave`, { playerId })
    return response.data
}

export async function dissolveAlliance(allianceId, playerId) {
    const response = await api.delete(`/alliances/${allianceId}`, { data: { playerId } })
    return response.data
}

export async function updateAllianceSettings(allianceId, requesterId, name, tag, description, status, minPoints) {
    const response = await api.put(`/alliances/${allianceId}/settings`, {
        requesterId, name, tag, description, status, minPoints
    })
    return response.data
}

export async function setAllianceMemberRank(allianceId, targetPlayerId, requesterId, rank) {
    const response = await api.post(`/alliances/${allianceId}/members/${targetPlayerId}/rank`, { requesterId, rank })
    return response.data
}

export async function getAllianceForumTopics(allianceId) {
    const response = await api.get(`/alliances/${allianceId}/forum`)
    return response.data
}

export async function createForumTopic(allianceId, authorPlayerId, title, content = '') {
    const response = await api.post(`/alliances/${allianceId}/forum`, { authorPlayerId, title, content })
    return response.data
}

export async function getForumTopic(allianceId, topicId) {
    const response = await api.get(`/alliances/${allianceId}/forum/${topicId}`)
    return response.data
}

export async function addForumPost(allianceId, topicId, authorPlayerId, content) {
    const response = await api.post(`/alliances/${allianceId}/forum/${topicId}/posts`, { authorPlayerId, content })
    return response.data
}

export async function deleteForumPost(allianceId, topicId, postId, playerId) {
    const response = await api.delete(`/alliances/${allianceId}/forum/${topicId}/posts/${postId}`, { data: { playerId } })
    return response.data
}

export async function deleteForumTopic(allianceId, topicId, playerId) {
    const response = await api.delete(`/alliances/${allianceId}/forum/${topicId}`, { data: { playerId } })
    return response.data
}

export async function setAllianceMemberPermissions(allianceId, targetPlayerId, requesterId, permissions) {
    const response = await api.patch(`/alliances/${allianceId}/members/${targetPlayerId}/permissions`, {
        requesterId, ...permissions
    })
    return response.data
}

export async function getSalvageInventory(settlementId) {
    const response = await api.get(`/Settlements/${settlementId}/salvage`)
    return response.data
}

export async function processSalvageItem(settlementId, itemKey, quantity = 1) {
    const response = await api.post(`/Settlements/${settlementId}/salvage/process`, { itemKey, quantity })
    return response.data
}

export default api