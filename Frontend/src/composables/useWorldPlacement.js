// useWorldPlacement.js — uniform distribution across entire island

function seededFactory(seed) {
    return () => {
        seed |= 0
        seed = seed + 0x6D2B79F5 | 0
        let t = Math.imul(seed ^ seed >>> 15, 1 | seed)
        t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t
        return ((t ^ t >>> 14) >>> 0) / 4294967296
    }
}

export function generateSlotsFromElevation({
                                               worldWidth,
                                               worldHeight,
                                               worldSeed,
                                               getElevation,
                                               waterLevel,
                                               settlements = [],
                                               maxSlots = 6000,
                                               minDist = 90,
                                               padding = 80
                                           }) {
    const R = seededFactory(worldSeed)
    const positions = []

    function isValidPos(x, y) {
        if (x < padding || x > worldWidth - padding || y < padding || y > worldHeight - padding) return false
        return getElevation(x, y) >= waterLevel + 0.15
    }

    function isFarEnough(x, y) {
        for (const p of positions) {
            if (Math.hypot(x - p.x, y - p.y) < minDist) return false
        }
        return true
    }

    // ── Phase 1: Grid-based candidates across ENTIRE map ──
    // Divide the world into a grid, try to place one slot per cell
    const cellSize = Math.max(minDist * 1.2, Math.sqrt((worldWidth * worldHeight) / maxSlots))
    const cols = Math.floor((worldWidth - padding * 2) / cellSize)
    const rows = Math.floor((worldHeight - padding * 2) / cellSize)

    // Shuffle grid cells for random fill order
    const cells = []
    for (let row = 0; row < rows; row++) {
        for (let col = 0; col < cols; col++) {
            cells.push({ col, row })
        }
    }
    // Fisher-Yates shuffle with seeded random
    for (let i = cells.length - 1; i > 0; i--) {
        const j = Math.floor(R() * (i + 1))
        const tmp = cells[i]
        cells[i] = cells[j]
        cells[j] = tmp
    }

    // Try to place a slot in each grid cell (with jitter)
    for (const cell of cells) {
        if (positions.length >= maxSlots) break

        const baseX = padding + cell.col * cellSize
        const baseY = padding + cell.row * cellSize

        // Try a few jittered positions within the cell
        let placed = false
        for (let attempt = 0; attempt < 8; attempt++) {
            const x = Math.round(baseX + R() * cellSize)
            const y = Math.round(baseY + R() * cellSize)

            if (!isValidPos(x, y)) continue
            if (!isFarEnough(x, y)) continue

            positions.push({ x, y })
            placed = true
            break
        }
    }

    // ── Phase 2: Fill gaps with random placement ──
    // If grid didn't fill enough, try random positions
    const extraAttempts = maxSlots * 10
    for (let attempt = 0; attempt < extraAttempts && positions.length < maxSlots; attempt++) {
        const x = Math.round(padding + R() * (worldWidth - padding * 2))
        const y = Math.round(padding + R() * (worldHeight - padding * 2))

        if (!isValidPos(x, y)) continue
        if (!isFarEnough(x, y)) continue

        positions.push({ x, y })
    }

    return positions.map((pos, index) => {
        const settlement = settlements.find(s => s.slotIndex === index)

        if (settlement) {
            const status = settlement.status || (settlement.isOwn ? 'yours' : 'neutral')
            return {
                id: settlement.id || `slot${index}`,
                x: pos.x, y: pos.y,
                name: settlement.name, status,
                owner: settlement.playerName,
                playerId: settlement.playerId,
                score: settlement.score || 0,
                defense: settlement.defense || 0,
                morale: settlement.morale || 100,
                population: settlement.population || 0,
                buildingCount: settlement.buildingCount || 0,
            }
        }

        return {
            id: `slot${index}`,
            x: pos.x, y: pos.y,
            name: `Sector ${index}`,
            status: 'empty',
            owner: null, score: 0, defense: 0,
        }
    })
}

export function generatePOIsFromElevation({
                                              worldWidth,
                                              worldHeight,
                                              worldSeed,
                                              getElevation,
                                              waterLevel,
                                              slots
                                          }) {
    const R = seededFactory(worldSeed + 7777)

    const POI_TYPES = [
        { type: 'factory',   label: 'Abandoned Factory',    icon: 'factory',   weight: 3 },
        { type: 'scrapyard', label: 'Scrap Yard',           icon: 'gear',      weight: 4 },
        { type: 'ruins',     label: 'Pre-War Ruins',        icon: 'ruins',     weight: 4 },
        { type: 'radzone',   label: 'Radiation Zone',       icon: 'radiation', weight: 2 },
        { type: 'bunker',    label: 'Underground Bunker',   icon: 'bunker',    weight: 2 },
        { type: 'tower',     label: 'Signal Tower',         icon: 'tower',     weight: 2 },
        { type: 'wreck',     label: 'Vehicle Wreckage',     icon: 'wreck',     weight: 3 },
        { type: 'well',      label: 'Water Source',         icon: 'water',     weight: 2 },
        { type: 'cache',     label: 'Supply Cache',         icon: 'cache',     weight: 2 },
        { type: 'crater',    label: 'Impact Crater',        icon: 'crater',    weight: 1 },
    ]

    const pool = []
    POI_TYPES.forEach(p => { for (let i = 0; i < p.weight; i++) pool.push(p) })

    const padding = 60
    const minDistPoi = 90
    const minDistSlot = 70

    function isValidPos(x, y) {
        if (x < padding || x > worldWidth - padding || y < padding || y > worldHeight - padding) return false
        return getElevation(x, y) >= waterLevel + 0.15
    }

    function isFarEnough(x, y, placed) {
        for (const p of placed) {
            if (Math.hypot(x - p.x, y - p.y) < minDistPoi) return false
        }
        for (const s of slots) {
            if (Math.hypot(x - s.x, y - s.y) < minDistSlot) return false
        }
        return true
    }

    const pois = []
    const maxPois = 100

    // ── Grid-based POI placement (same approach as slots) ──
    const cellSize = Math.max(minDistPoi * 1.3, Math.sqrt((worldWidth * worldHeight) / (maxPois * 1.5)))
    const cols = Math.floor((worldWidth - padding * 2) / cellSize)
    const rows = Math.floor((worldHeight - padding * 2) / cellSize)

    const cells = []
    for (let row = 0; row < rows; row++) {
        for (let col = 0; col < cols; col++) {
            cells.push({ col, row })
        }
    }
    for (let i = cells.length - 1; i > 0; i--) {
        const j = Math.floor(R() * (i + 1))
        const tmp = cells[i]; cells[i] = cells[j]; cells[j] = tmp
    }

    for (const cell of cells) {
        if (pois.length >= maxPois) break

        const baseX = padding + cell.col * cellSize
        const baseY = padding + cell.row * cellSize

        for (let attempt = 0; attempt < 6; attempt++) {
            const x = Math.round(baseX + R() * cellSize)
            const y = Math.round(baseY + R() * cellSize)

            if (!isValidPos(x, y)) continue
            if (!isFarEnough(x, y, pois)) continue

            const template = pool[Math.floor(R() * pool.length)]
            pois.push({
                id: `poi${pois.length}`,
                x, y,
                type: template.type,
                label: template.label,
                icon: template.icon,
                discovered: R() < 0.6,
            })
            break
        }
    }

    // Fill remaining with random placement
    const extraAttempts = maxPois * 15
    for (let attempt = 0; attempt < extraAttempts && pois.length < maxPois; attempt++) {
        const x = Math.round(padding + R() * (worldWidth - padding * 2))
        const y = Math.round(padding + R() * (worldHeight - padding * 2))

        if (!isValidPos(x, y)) continue
        if (!isFarEnough(x, y, pois)) continue

        const template = pool[Math.floor(R() * pool.length)]
        pois.push({
            id: `poi${pois.length}`,
            x, y,
            type: template.type,
            label: template.label,
            icon: template.icon,
            discovered: true,
        })
    }

    return pois
}