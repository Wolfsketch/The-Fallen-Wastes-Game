// World island generator — supports large maps with proper slot placement

class SimplexNoise {
    constructor(seed = 0) {
        this.grad3 = [[1,1,0],[-1,1,0],[1,-1,0],[-1,-1,0],[1,0,1],[-1,0,1],[1,0,-1],[-1,0,-1],[0,1,1],[0,-1,1],[0,1,-1],[0,-1,-1]]
        this.perm = new Uint8Array(512)
        const p = new Uint8Array(256)
        let s = seed || 1
        for (let i = 0; i < 256; i++) { s = (s * 16807) % 2147483647; p[i] = i }
        for (let i = 255; i > 0; i--) { s = (s * 16807) % 2147483647; const j = s % (i + 1); const t = p[i]; p[i] = p[j]; p[j] = t }
        for (let i = 0; i < 512; i++) this.perm[i] = p[i & 255]
    }
    noise2D(x, y) {
        const F2=0.5*(Math.sqrt(3)-1),G2=(3-Math.sqrt(3))/6,s=(x+y)*F2,i=Math.floor(x+s),j=Math.floor(y+s),t=(i+j)*G2,x0=x-(i-t),y0=y-(j-t)
        let i1,j1;if(x0>y0){i1=1;j1=0}else{i1=0;j1=1}
        const x1=x0-i1+G2,y1=y0-j1+G2,x2=x0-1+2*G2,y2=y0-1+2*G2,ii=i&255,jj=j&255,dot=(g,a,b)=>g[0]*a+g[1]*b
        let n0=0,n1=0,n2=0,t0=0.5-x0*x0-y0*y0;if(t0>0){t0*=t0;n0=t0*t0*dot(this.grad3[this.perm[ii+this.perm[jj]]%12],x0,y0)}
        let t1=0.5-x1*x1-y1*y1;if(t1>0){t1*=t1;n1=t1*t1*dot(this.grad3[this.perm[ii+i1+this.perm[jj+j1]]%12],x1,y1)}
        let t2=0.5-x2*x2-y2*y2;if(t2>0){t2*=t2;n2=t2*t2*dot(this.grad3[this.perm[ii+1+this.perm[jj+1]]%12],x2,y2)}
        return 70*(n0+n1+n2)
    }
    fbm(x,y,oct=5,lac=2,gain=0.5){let s=0,a=1,f=1,m=0;for(let i=0;i<oct;i++){s+=this.noise2D(x*f,y*f)*a;m+=a;a*=gain;f*=lac};return s/m}
}

function lerp3(a,b,t){return[a[0]+(b[0]-a[0])*t,a[1]+(b[1]-a[1])*t,a[2]+(b[2]-a[2])*t]}

const COL = {
    oceanDeep:[3,6,14],oceanMid:[5,12,22],oceanShore:[8,20,34],
    beach:[16,32,42],beachWet:[12,26,36],
    lowDark:[12,28,36],lowMid:[16,34,44],lowLight:[20,40,50],
    highDark:[18,38,48],highMid:[24,46,58],highLight:[30,54,66],
    peakDark:[26,50,62],peakMid:[34,60,74],peakLight:[40,68,82],
}

// Generate the full world terrain + heightmap
export function generateWorld(width, height, worldSeed) {
    const canvas = document.createElement('canvas')
    canvas.width = width; canvas.height = height
    const ctx = canvas.getContext('2d')
    const imgData = ctx.createImageData(width, height)
    const data = imgData.data

    const n1 = new SimplexNoise(worldSeed)
    const n2 = new SimplexNoise(worldSeed + 500)
    const n3 = new SimplexNoise(worldSeed + 1200)
    const cx = width/2, cy = height/2

    const hMap = new Float32Array(width * height)
    const wl = 0.18 // water level

    for (let y = 0; y < height; y++) {
        for (let x = 0; x < width; x++) {
            const idx = y*width+x
            const dx = (x-cx)/(cx*0.87), dy = (y-cy)/(cy*0.82)
            const dist = Math.sqrt(dx*dx + dy*dy)
            const coastNoise = n3.fbm(x/width*6, y/height*6, 3, 2, 0.6) * 0.18
            const falloff = 1 - Math.pow(Math.min((dist+coastNoise)*1.05, 1), 3)

            const nx = x/width, ny = y/height
            const v1 = n1.fbm(nx*5, ny*5, 6, 2.1, 0.52)*0.5+0.5
            const v2 = n2.fbm(nx*8+10, ny*8+10, 4, 2, 0.5)*0.5+0.5
            const v3 = n3.fbm(nx*3+20, ny*3+20, 3, 2, 0.55)*0.5+0.5

            let e = (v1*0.55 + v2*0.25 + v3*0.2) * falloff
            e = Math.pow(Math.max(0,e), 1.2)
            const cd = Math.sqrt(((x-cx)/(cx*0.5))**2+((y-cy)/(cy*0.5))**2)
            e += Math.max(0,1-cd)*0.15*v1*falloff
            hMap[idx] = Math.max(0, Math.min(1, e))
        }
    }

    // Render
    const bl=0.23,ll=0.42,hl=0.6,pl=0.78,lx=-0.5,ly=-0.7
    for (let y = 0; y < height; y++) {
        for (let x = 0; x < width; x++) {
            const idx=y*width+x, pi=idx*4, e=hMap[idx]
            let c
            if(e<wl*0.4) c=COL.oceanDeep
            else if(e<wl*0.75) c=lerp3(COL.oceanDeep,COL.oceanMid,(e-wl*0.4)/(wl*0.35))
            else if(e<wl) c=lerp3(COL.oceanMid,COL.oceanShore,(e-wl*0.75)/(wl*0.25))
            else if(e<bl) c=lerp3(COL.beachWet,COL.beach,(e-wl)/(bl-wl))
            else if(e<ll){const t=(e-bl)/(ll-bl);c=t<0.5?lerp3(COL.lowDark,COL.lowMid,t*2):lerp3(COL.lowMid,COL.lowLight,(t-0.5)*2)}
            else if(e<hl){const t=(e-ll)/(hl-ll);c=t<0.5?lerp3(COL.highDark,COL.highMid,t*2):lerp3(COL.highMid,COL.highLight,(t-0.5)*2)}
            else if(e<pl) c=lerp3(COL.peakDark,COL.peakMid,(e-hl)/(pl-hl))
            else c=lerp3(COL.peakMid,COL.peakLight,Math.min(1,(e-pl)/(1-pl)))

            let sh=0
            if(x>0&&x<width-1&&y>0&&y<height-1&&e>=wl){sh=(hMap[idx+1]-hMap[idx-1])*lx+(hMap[idx+width]-hMap[idx-width])*ly;sh=Math.max(-0.35,Math.min(0.45,sh*5))}
            const mi=n1.noise2D(x*0.4,y*0.4)*0.025
            let al=255
            if(e<wl){al=Math.round(120+(1-1+e/wl)*135);sh+=n2.noise2D(x*0.06,y*0.06)*0.015}

            data[pi]=Math.max(0,Math.min(255,c[0]+(sh+mi)*70))
            data[pi+1]=Math.max(0,Math.min(255,c[1]+(sh+mi)*110))
            data[pi+2]=Math.max(0,Math.min(255,c[2]+(sh+mi)*130))
            data[pi+3]=al
        }
    }
    ctx.putImageData(imgData, 0, 0)

    // Coast glow
    ctx.save();ctx.globalCompositeOperation='screen';ctx.filter='blur(2px)'
    const tmp=document.createElement('canvas');tmp.width=width;tmp.height=height
    const tc=tmp.getContext('2d'),ti=tc.createImageData(width,height)
    for(let y=0;y<height;y++)for(let x=0;x<width;x++){
        const idx=y*width+x,e=hMap[idx];let coast=false
        if(e>=wl&&((x>0&&hMap[idx-1]<wl)||(x<width-1&&hMap[idx+1]<wl)||(y>0&&hMap[idx-width]<wl)||(y<height-1&&hMap[idx+width]<wl)))coast=true
        const p=idx*4;if(coast){ti.data[p]=0;ti.data[p+1]=140;ti.data[p+2]=180;ti.data[p+3]=60}
    }
    tc.putImageData(ti,0,0);ctx.drawImage(tmp,0,0);ctx.restore()

    generateRivers(ctx, hMap, width, height, wl, worldSeed)

    return { canvas, heightMap: hMap, waterLevel: wl }
}

function generateRivers(ctx, hMap, w, h, wl, seed) {
    function seeded(s){return()=>{s|=0;s=s+0x6D2B79F5|0;let t=Math.imul(s^s>>>15,1|s);t=t+Math.imul(t^t>>>7,61|t)^t;return((t^t>>>14)>>>0)/4294967296}}
    const R = seeded(seed + 7777)

    const numRivers = 12
    const rivers = []

    for (let r = 0; r < numRivers; r++) {
        let sx, sy, startElev = 0
        for (let attempt = 0; attempt < 200; attempt++) {
            sx = Math.round(w * 0.15 + R() * w * 0.7)
            sy = Math.round(h * 0.15 + R() * h * 0.7)
            const e = hMap[sy * w + sx]
            if (e > 0.5 && e > startElev) { startElev = e; break }
        }
        if (startElev < 0.4) continue

        const path = [{x: sx, y: sy}]
        let cx = sx, cy = sy
        let stuck = 0

        for (let step = 0; step < 600; step++) {
            const idx = Math.round(cy) * w + Math.round(cx)
            const curElev = hMap[idx] || 0
            if (curElev < wl) break

            let bestX = cx, bestY = cy, bestElev = curElev
            const searchR = 2
            for (let dy = -searchR; dy <= searchR; dy++) {
                for (let dx = -searchR; dx <= searchR; dx++) {
                    if (dx === 0 && dy === 0) continue
                    const nx = Math.round(cx + dx), ny = Math.round(cy + dy)
                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue
                    const ne = hMap[ny * w + nx]
                    const jitter = (R() - 0.5) * 0.005
                    if (ne + jitter < bestElev) {
                        bestElev = ne + jitter
                        bestX = nx; bestY = ny
                    }
                }
            }

            if (bestX === cx && bestY === cy) {
                stuck++
                if (stuck > 5) break
                cx += (R() - 0.5) * 4
                cy += (R() - 0.5) * 4
                continue
            }
            stuck = 0

            cx = bestX; cy = bestY
            path.push({x: cx, y: cy})
        }

        if (path.length > 15) rivers.push(path)
    }

    rivers.forEach(path => {
        ctx.beginPath()
        ctx.moveTo(path[0].x, path[0].y)

        for (let i = 1; i < path.length - 1; i += 2) {
            const p1 = path[i], p2 = path[Math.min(i + 1, path.length - 1)]
            ctx.quadraticCurveTo(p1.x, p1.y, (p1.x + p2.x) / 2, (p1.y + p2.y) / 2)
        }
        ctx.lineTo(path[path.length - 1].x, path[path.length - 1].y)

        ctx.strokeStyle = 'rgba(6, 18, 30, 0.7)'
        ctx.lineWidth = 3.5
        ctx.lineCap = 'round'
        ctx.lineJoin = 'round'
        ctx.stroke()

        ctx.strokeStyle = 'rgba(0, 130, 180, 0.18)'
        ctx.lineWidth = 2
        ctx.stroke()

        ctx.strokeStyle = 'rgba(0, 160, 210, 0.06)'
        ctx.lineWidth = 6
        ctx.stroke()
    })
}

export function drawRoads(ctx, slots, hMap, width, height, wl) {
    const claimed = slots.filter(s => s.status !== 'empty')
    if (claimed.length < 2) return

    const connections = new Set()
    const maxRoadDist = 500

    claimed.forEach(a => {
        const neighbors = claimed
            .filter(b => b.id !== a.id)
            .map(b => ({ slot: b, dist: Math.hypot(a.x - b.x, a.y - b.y) }))
            .sort((x, y) => x.dist - y.dist)
            .slice(0, 3)
            .filter(n => n.dist < maxRoadDist)

        neighbors.forEach(n => {
            const key = [a.id, n.slot.id].sort().join('-')
            if (connections.has(key)) return
            connections.add(key)

            const roadPath = generateRoadPath(a, n.slot, hMap, width, height, wl)
            if (roadPath.length > 1) drawRoadPath(ctx, roadPath)
        })
    })

    const player = slots.find(s => s.status === 'yours')
    if (player) {
        const allies = claimed.filter(s => s.status === 'ally')
        allies.forEach(ally => {
            const path = generateRoadPath(player, ally, hMap, width, height, wl)
            if (path.length > 1) drawRoadPath(ctx, path, true)
        })
    }
}

function generateRoadPath(from, to, hMap, w, h, wl) {
    const points = []
    const steps = Math.ceil(Math.hypot(to.x - from.x, to.y - from.y) / 8)
    const noise = new SimplexNoise(from.x * 100 + to.y)

    for (let i = 0; i <= steps; i++) {
        const t = i / steps
        let x = from.x + (to.x - from.x) * t
        let y = from.y + (to.y - from.y) * t

        const wiggle = Math.sin(t * Math.PI) * 20
        x += noise.noise2D(t * 5, 0) * wiggle
        y += noise.noise2D(0, t * 5) * wiggle * 0.7

        const ix = Math.round(Math.max(0, Math.min(w - 1, x)))
        const iy = Math.round(Math.max(0, Math.min(h - 1, y)))
        const elev = hMap[iy * w + ix]
        if (elev < wl + 0.02) {
            for (let r = 1; r <= 5; r++) {
                const cx = Math.round(x + (from.x + to.x) / 2 - x > 0 ? r * 3 : -r * 3)
                const cy = Math.round(y + (from.y + to.y) / 2 - y > 0 ? r * 2 : -r * 2)
                if (cx >= 0 && cx < w && cy >= 0 && cy < h && hMap[cy * w + cx] > wl + 0.02) {
                    x = cx; y = cy; break
                }
            }
        }

        points.push({x, y})
    }
    return points
}

function drawRoadPath(ctx, path, isAllyRoad = false) {
    ctx.beginPath()
    ctx.moveTo(path[0].x, path[0].y)
    for (let i = 1; i < path.length - 1; i += 2) {
        const p1 = path[i], p2 = path[Math.min(i + 1, path.length - 1)]
        ctx.quadraticCurveTo(p1.x, p1.y, (p1.x + p2.x) / 2, (p1.y + p2.y) / 2)
    }
    ctx.lineTo(path[path.length - 1].x, path[path.length - 1].y)

    ctx.strokeStyle = isAllyRoad ? 'rgba(0, 212, 255, 0.08)' : 'rgba(0, 180, 220, 0.05)'
    ctx.lineWidth = isAllyRoad ? 2.5 : 2
    ctx.lineCap = 'round'
    ctx.lineJoin = 'round'
    ctx.setLineDash([6, 4])
    ctx.stroke()
    ctx.setLineDash([])
}

// settlements = array from API: [{id, name, playerName, slotIndex, score, defense, morale, isOwn}]
export function generateSlots(world, width, height, worldSeed, currentPlayerId, settlements = []) {
    const { heightMap, waterLevel } = world
    const minDist = 90
    const padding = 80

    function seeded(s){return()=>{s|=0;s=s+0x6D2B79F5|0;let t=Math.imul(s^s>>>15,1|s);t=t+Math.imul(t^t>>>7,61|t)^t;return((t^t>>>14)>>>0)/4294967296}}
    const R = seeded(worldSeed)

    function isValidPos(x, y) {
        if (x < padding || x > width-padding || y < padding || y > height-padding) return false
        const ix = Math.round(x), iy = Math.round(y)
        if (ix < 0 || ix >= width || iy < 0 || iy >= height) return false
        return heightMap[iy * width + ix] >= waterLevel + 0.03
    }

    function isFarEnough(x, y, placed) {
        for (const p of placed) {
            if (Math.hypot(x - p.x, y - p.y) < minDist) return false
        }
        return true
    }

    const positions = []
    const cx = width/2, cy = height/2
    positions.push({ x: cx, y: cy })

    const maxSlots = 200
    const maxAttempts = maxSlots * 20

    for (let attempt = 0; attempt < maxAttempts && positions.length < maxSlots; attempt++) {
        const angle = R() * Math.PI * 2
        const maxRadius = Math.min(cx, cy) * 0.75
        const dist = 50 + R() * maxRadius
        const x = Math.round(cx + Math.cos(angle) * dist)
        const y = Math.round(cy + Math.sin(angle) * dist * 0.75)

        if (!isValidPos(x, y)) continue
        if (!isFarEnough(x, y, positions)) continue

        positions.push({ x, y })
    }

    const slots = positions.map((pos, index) => {
        const settlement = settlements.find(s => s.slotIndex === index)

        if (settlement) {
            const status = settlement.status || (settlement.isOwn ? 'yours' : 'neutral')

            return {
                id: settlement.id || `slot${index}`,
                x: pos.x,
                y: pos.y,
                name: settlement.name,
                status,
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
            x: pos.x,
            y: pos.y,
            name: `Sector ${index}`,
            status: 'empty',
            owner: null,
            score: 0,
            defense: 0,
        }
    })

    return slots
}

export function generatePOIs(world, width, height, worldSeed, slots) {
    const { heightMap, waterLevel } = world

    function seeded(s){return()=>{s|=0;s=s+0x9E3779B9|0;let t=Math.imul(s^s>>>15,1|s);t=t+Math.imul(t^t>>>7,61|t)^t;return((t^t>>>14)>>>0)/4294967296}}
    const R = seeded(worldSeed + 7777)

    function isValidPos(x, y) {
        if (x < 60 || x > width-60 || y < 60 || y > height-60) return false
        const ix = Math.round(x), iy = Math.round(y)
        if (ix < 0 || ix >= width || iy < 0 || iy >= height) return false
        return heightMap[iy * width + ix] >= waterLevel + 0.03
    }

    function isFarEnough(x, y, placed, minDist) {
        for (const p of placed) {
            if (Math.hypot(x - p.x, y - p.y) < minDist) return false
        }
        for (const s of slots) {
            if (Math.hypot(x - s.x, y - s.y) < 60) return false
        }
        return true
    }

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

    const pois = []
    const maxPois = 70
    const maxAttempts = maxPois * 30
    const cx = width/2, cy = height/2

    for (let attempt = 0; attempt < maxAttempts && pois.length < maxPois; attempt++) {
        const angle = R() * Math.PI * 2
        const maxRadius = Math.min(cx, cy) * 0.8
        const dist = 80 + R() * maxRadius
        const x = Math.round(cx + Math.cos(angle) * dist)
        const y = Math.round(cy + Math.sin(angle) * dist * 0.75)

        if (!isValidPos(x, y)) continue
        if (!isFarEnough(x, y, pois, 90)) continue

        const template = pool[Math.floor(R() * pool.length)]
        pois.push({
            id: `poi${pois.length}`,
            x, y,
            type: template.type,
            label: template.label,
            icon: template.icon,
            discovered: R() < 0.6,
        })
    }

    return pois
}