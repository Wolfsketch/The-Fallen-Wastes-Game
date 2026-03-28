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

export async function getPoiStates() {
    const response = await api.get('/Operations/poi-states')
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

// Reports
export async function getReportMessages(playerId) {
    const response = await api.get(`/Messages/${playerId}/reports`)
    return response.data
}

export default api