// useChunkedMapRenderer.js — Image-based island renderer
// Loads a PNG island image, draws ocean water behind it,
// uses pixel brightness as elevation for slot/POI placement

export function createChunkedWorldRenderer({
                                               worldWidth,
                                               worldHeight,
                                               chunkSize = 256,
                                               seed,
                                               imageSrc = '/images/Island/island.png'
                                           }) {
    const waterLevel = 0.18

    let islandImg = null
    let imgCanvas = null
    let imgPixels = null
    let imgW = 0, imgH = 0
    let imageReady = false

    const chunkCache = new Map()
    let previewCanvas = null
    let oceanPattern = null

    const loadState = { total: 0, loaded: 0 }

    function resetLoadProgress(total = 0) { loadState.total = total; loadState.loaded = 0 }
    function getLoadProgress() {
        return {
            total: loadState.total,
            loaded: loadState.loaded,
            percent: loadState.total > 0
                ? Math.min(100, Math.round((loadState.loaded / loadState.total) * 100))
                : 100
        }
    }

    function loadImage() {
        return new Promise((resolve, reject) => {
            const img = new Image()
            img.crossOrigin = 'anonymous'
            img.onload = () => {
                islandImg = img
                imgCanvas = document.createElement('canvas')
                imgCanvas.width = worldWidth
                imgCanvas.height = worldHeight
                const ctx = imgCanvas.getContext('2d', { willReadFrequently: true })
                ctx.drawImage(img, 0, 0, worldWidth, worldHeight)
                const imageData = ctx.getImageData(0, 0, worldWidth, worldHeight)
                imgPixels = imageData.data
                imgW = worldWidth
                imgH = worldHeight
                imageReady = true
                console.log(`Island image loaded: ${img.naturalWidth}x${img.naturalHeight} → ${worldWidth}x${worldHeight}`)
                resolve()
            }
            img.onerror = (e) => {
                console.error('Failed to load island image:', imageSrc, e)
                reject(e)
            }
            img.src = imageSrc
        })
    }

    function getElevation(globalX, globalY) {
        if (!imageReady) return 0
        const x = Math.max(0, Math.min(imgW - 1, Math.round(globalX)))
        const y = Math.max(0, Math.min(imgH - 1, Math.round(globalY)))
        const i = (y * imgW + x) * 4
        const r = imgPixels[i]
        const g = imgPixels[i + 1]
        const b = imgPixels[i + 2]
        const brightness = r + g + b
        if (brightness < 30) return 0
        const normBright = Math.min(1, (brightness - 30) / 500)
        return waterLevel + normBright * (1 - waterLevel)
    }

    function createOceanCanvas() {
        const tile = document.createElement('canvas')
        tile.width = 128; tile.height = 128
        const ctx = tile.getContext('2d')
        ctx.fillStyle = '#081830'
        ctx.fillRect(0, 0, 128, 128)
        const imgData = ctx.getImageData(0, 0, 128, 128)
        const d = imgData.data
        for (let y = 0; y < 128; y++) {
            for (let x = 0; x < 128; x++) {
                const pi = (y * 128 + x) * 4
                const wave = Math.sin(x * 0.15 + y * 0.08) * 2 +
                    Math.sin(x * 0.08 - y * 0.12) * 1.5
                d[pi]     = Math.max(0, Math.min(255, d[pi] + wave))
                d[pi + 1] = Math.max(0, Math.min(255, d[pi + 1] + wave * 1.8))
                d[pi + 2] = Math.max(0, Math.min(255, d[pi + 2] + wave * 2.5))
            }
        }
        ctx.putImageData(imgData, 0, 0)
        return tile
    }

    function renderChunk(chunkX, chunkY) {
        const key = `${chunkX},${chunkY}`
        if (chunkCache.has(key)) return chunkCache.get(key)

        const canvas = document.createElement('canvas')
        canvas.width = chunkSize; canvas.height = chunkSize
        const ctx = canvas.getContext('2d')
        const sx = chunkX * chunkSize
        const sy = chunkY * chunkSize

        // 1. Ocean background — dark blue, not black
        ctx.fillStyle = '#0a1e3a'
        ctx.fillRect(0, 0, chunkSize, chunkSize)

        // Subtle ocean wave pattern
        if (!oceanPattern) oceanPattern = createOceanCanvas()
        ctx.globalAlpha = 0.4
        ctx.drawImage(oceanPattern,
            sx % 128, sy % 128, chunkSize, chunkSize,
            0, 0, chunkSize, chunkSize)
        ctx.globalAlpha = 1

        // 2. Island image slice on top
        if (imageReady) {
            const sw = Math.min(chunkSize, worldWidth - sx)
            const sh = Math.min(chunkSize, worldHeight - sy)

            if (sw > 0 && sh > 0) {
                const srcCtx = imgCanvas.getContext('2d')
                const srcData = srcCtx.getImageData(sx, sy, sw, sh)
                const src = srcData.data
                const dstData = ctx.getImageData(0, 0, sw, sh)
                const dst = dstData.data

                for (let i = 0; i < src.length; i += 4) {
                    const brightness = src[i] + src[i + 1] + src[i + 2]
                    if (brightness >= 30) {
                        dst[i]     = src[i]
                        dst[i + 1] = src[i + 1]
                        dst[i + 2] = src[i + 2]
                        dst[i + 3] = 255
                    }
                }
                ctx.putImageData(dstData, 0, 0)
            }
        }

        const chunk = { canvas, chunkX, chunkY }
        chunkCache.set(key, chunk)
        loadState.loaded += 1
        return chunk
    }

    function ensureChunk(cx, cy) { return renderChunk(cx, cy) }

    async function preloadChunks(chunkCoords, onProgress) {
        if (!imageReady) await loadImage()
        for (let i = 0; i < chunkCoords.length; i++) {
            ensureChunk(chunkCoords[i].x, chunkCoords[i].y)
            if (onProgress) onProgress(getLoadProgress())
            if (i % 4 === 0) {
                await new Promise(r => requestAnimationFrame(r))
            }
        }
    }

    function drawVisibleChunks(ctx, viewLeft, viewTop, viewWidth, viewHeight) {
        const sCX = Math.max(0, Math.floor(viewLeft / chunkSize))
        const sCY = Math.max(0, Math.floor(viewTop / chunkSize))
        const eCX = Math.min(Math.ceil(worldWidth / chunkSize), Math.ceil((viewLeft + viewWidth) / chunkSize) + 1)
        const eCY = Math.min(Math.ceil(worldHeight / chunkSize), Math.ceil((viewTop + viewHeight) / chunkSize) + 1)
        for (let cy = sCY; cy < eCY; cy++)
            for (let cx = sCX; cx < eCX; cx++)
                ctx.drawImage(ensureChunk(cx, cy).canvas, cx * chunkSize, cy * chunkSize)
    }

    function buildPreview(width = 170, height = 115) {
        if (previewCanvas) return previewCanvas
        const canvas = document.createElement('canvas')
        canvas.width = width; canvas.height = height
        const ctx = canvas.getContext('2d')
        ctx.fillStyle = '#0a1e3a'
        ctx.fillRect(0, 0, width, height)
        if (imageReady) {
            ctx.drawImage(imgCanvas, 0, 0, width, height)
        }
        previewCanvas = canvas
        return previewCanvas
    }

    function buildElevGrid() {}
    function generateRivers() {}
    function generateRoads() {}

    function clearChunkCache() {
        chunkCache.clear()
        previewCanvas = null
    }

    return {
        waterLevel,
        getElevation,
        drawVisibleChunks,
        buildPreview,
        chunkCache,
        chunkSize,
        resetLoadProgress,
        getLoadProgress,
        ensureChunk,
        preloadChunks,
        buildElevGrid,
        generateRivers,
        generateRoads,
        clearChunkCache,
        loadImage,
    }
}