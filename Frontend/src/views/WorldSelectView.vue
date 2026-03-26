<template>
  <div class="ws-wrapper">
    <div class="ws-bg-grid" />

    <div class="ws-container">
      <!-- Header -->
      <div class="ws-header">
        <div class="ws-logo">
          <div class="radar"><div class="radar-ring" /><div class="radar-core">☢</div></div>
          <h1 class="ws-title">THE FALLEN WASTES</h1>
          <p class="ws-subtitle">SELECT YOUR WORLD</p>
        </div>
        <div class="ws-player">
          <span class="ws-welcome">Welcome back, <strong>{{ username }}</strong></span>
          <button class="ws-logout" @click="logout">Log out</button>
        </div>
      </div>

      <!-- Newest world banner -->
      <div class="ws-newest" @click="joinWorld(newestWorld)">
        <div class="ws-newest-badge">NEW</div>
        <div class="ws-newest-info">
          <div class="ws-newest-title">{{ newestWorld.name }}</div>
          <div class="ws-newest-desc">Newest world — {{ newestWorld.players }} players — Started {{ newestWorld.started }}</div>
        </div>
        <button class="ws-newest-btn">JOIN WORLD →</button>
      </div>

      <!-- World list -->
      <div class="ws-list-header">
        <span class="ws-lh-label">ALL WORLDS</span>
        <span class="ws-lh-count">{{ worlds.length }} worlds available</span>
      </div>

      <div class="ws-list">
        <div
            v-for="w in worlds"
            :key="w.id"
            class="ws-world"
            :class="{ 'ws-world--active': w.hasCharacter, 'ws-world--full': w.status === 'full' }"
            @click="w.status !== 'full' ? joinWorld(w) : null"
        >
          <div class="ws-world-left">
            <div class="ws-world-icon" :class="`ws-icon--${w.status}`">
              {{ w.status === 'full' ? '🔒' : w.hasCharacter ? '⌂' : '☢' }}
            </div>
            <div class="ws-world-info">
              <div class="ws-world-name">
                {{ w.name }}
                <span v-if="w.hasCharacter" class="ws-world-char">— {{ w.characterName }}</span>
              </div>
              <div class="ws-world-meta">
                <span class="ws-meta-item">Seed: {{ w.seed }}</span>
                <span class="ws-meta-sep">·</span>
                <span class="ws-meta-item">{{ w.players }}/{{ w.maxPlayers }} players</span>
                <span class="ws-meta-sep">·</span>
                <span class="ws-meta-item">Started {{ w.started }}</span>
              </div>
            </div>
          </div>
          <div class="ws-world-right">
            <div class="ws-world-status" :class="`ws-status--${w.status}`">
              {{ w.status === 'full' ? 'FULL' : w.status === 'new' ? 'NEW' : 'ONLINE' }}
            </div>
            <div class="ws-world-bar">
              <div class="ws-world-bar-fill" :style="{ width: (w.players / w.maxPlayers * 100) + '%' }" />
            </div>
            <button
                class="ws-join-btn"
                :class="{ 'ws-join--play': w.hasCharacter, 'ws-join--disabled': w.status === 'full' }"
                :disabled="w.status === 'full'"
            >
              {{ w.status === 'full' ? 'FULL' : w.hasCharacter ? 'PLAY' : 'JOIN' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()

const username = ref(JSON.parse(sessionStorage.getItem('playerData') || '{}').username || 'Operator')

// World list — dit wordt later vanuit de API geladen
const worlds = ref([
  { id: 1, name: 'Wasteland Alpha', seed: 42, players: 247, maxPlayers: 5000, status: 'online', started: '10 Mar 2026', hasCharacter: true, characterName: 'The First Settlement' },
  { id: 2, name: 'Scorched Earth', seed: 108, players: 1832, maxPlayers: 5000, status: 'online', started: '1 Feb 2026', hasCharacter: false, characterName: null },
  { id: 3, name: 'Ashfall', seed: 256, players: 64, maxPlayers: 5000, status: 'new', started: '15 Mar 2026', hasCharacter: false, characterName: null },
  { id: 4, name: 'Dead Zone', seed: 777, players: 4210, maxPlayers: 5000, status: 'online', started: '5 Jan 2026', hasCharacter: false, characterName: null },
  { id: 5, name: 'Irradiated Shores', seed: 333, players: 5000, maxPlayers: 5000, status: 'full', started: '20 Dec 2025', hasCharacter: false, characterName: null },
  { id: 6, name: 'Crater Basin', seed: 512, players: 891, maxPlayers: 5000, status: 'online', started: '28 Feb 2026', hasCharacter: false, characterName: null },
])

const newestWorld = computed(() => {
  return worlds.value.find(w => w.status === 'new') || worlds.value[worlds.value.length - 1]
})

function joinWorld(world) {
  if (world.status === 'full') return

  // Sla gekozen wereld op
  sessionStorage.setItem('worldId', world.id)
  sessionStorage.setItem('worldSeed', world.seed)
  sessionStorage.setItem('worldName', world.name)

  if (world.hasCharacter) {
    // Direct naar game
    router.push('/game')
  } else {
    // Naar settlement-naam kiezen voor deze wereld
    router.push('/game/new-settlement')
  }
}

function logout() {
  sessionStorage.clear()
  router.push('/')
}
</script>

<style scoped>
.ws-wrapper {
  min-height: 100vh;
  background: var(--bg);
  position: relative;
  overflow: auto;
  padding: 30px 20px;
}
.ws-bg-grid {
  position: fixed;
  inset: 0;
  opacity: 0.02;
  background-image:
      linear-gradient(var(--cyan) 1px, transparent 1px),
      linear-gradient(90deg, var(--cyan) 1px, transparent 1px);
  background-size: 60px 60px;
  pointer-events: none;
}

.ws-container {
  max-width: 700px;
  margin: 0 auto;
  position: relative;
}

/* Header */
.ws-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 28px;
}
.ws-logo { display: flex; align-items: center; gap: 14px; }
.radar { position: relative; width: 36px; height: 36px; }
.radar-ring { position: absolute; inset: 0; border: 2px solid var(--cyan-dim); border-radius: 50%; border-top-color: var(--cyan); animation: radarSweep 3s linear infinite; }
.radar-core { position: absolute; inset: 6px; background: radial-gradient(circle, var(--cyan-dim), transparent); border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 12px; color: var(--cyan); }
.ws-title { font-family: var(--ff-title); font-size: 18px; color: var(--cyan); letter-spacing: 4px; font-weight: 700; text-shadow: 0 0 16px rgba(0,212,255,0.3); }
.ws-subtitle { font-family: var(--ff-title); font-size: 8px; color: var(--muted); letter-spacing: 3px; margin-top: 4px; }

.ws-player { text-align: right; }
.ws-welcome { font-size: 12px; color: var(--text); }
.ws-welcome strong { color: var(--cyan); }
.ws-logout { display: block; margin-top: 6px; background: none; border: 1px solid var(--border); color: var(--muted); font-family: var(--ff); font-size: 10px; padding: 3px 12px; cursor: pointer; transition: all 0.15s; }
.ws-logout:hover { border-color: var(--cyan-dim); color: var(--text); }

/* Newest world banner */
.ws-newest {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 16px 20px;
  background: linear-gradient(135deg, rgba(0,212,255,0.06), rgba(0,212,255,0.02));
  border: 1px solid rgba(0,212,255,0.25);
  margin-bottom: 20px;
  cursor: pointer;
  transition: all 0.2s;
}
.ws-newest:hover {
  border-color: rgba(0,212,255,0.5);
  box-shadow: 0 0 20px rgba(0,212,255,0.08);
}
.ws-newest-badge {
  background: var(--cyan);
  color: var(--bg);
  font-family: var(--ff-title);
  font-size: 9px;
  font-weight: 700;
  padding: 3px 8px;
  letter-spacing: 2px;
}
.ws-newest-info { flex: 1; }
.ws-newest-title { font-size: 14px; color: var(--cyan); font-weight: 700; }
.ws-newest-desc { font-size: 10px; color: var(--muted); margin-top: 2px; }
.ws-newest-btn {
  background: linear-gradient(180deg, var(--cyan-dark), var(--cyan-dim));
  border: 1px solid var(--cyan);
  color: #fff;
  font-family: var(--ff-title);
  font-size: 10px;
  font-weight: 700;
  padding: 8px 18px;
  letter-spacing: 2px;
  cursor: pointer;
  box-shadow: 0 0 12px rgba(0,212,255,0.2);
}

/* List header */
.ws-list-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
  padding: 0 4px;
}
.ws-lh-label { font-family: var(--ff-title); font-size: 9px; color: var(--cyan); letter-spacing: 3px; font-weight: 700; }
.ws-lh-count { font-size: 9px; color: var(--muted); }

/* World list */
.ws-list {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.ws-world {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  background: var(--bg2);
  border: 1px solid var(--border);
  cursor: pointer;
  transition: all 0.15s;
}
.ws-world:hover {
  border-color: var(--border-bright);
  background: var(--bg3);
}
.ws-world--active {
  border-color: rgba(0,212,255,0.2);
  background: linear-gradient(135deg, rgba(0,212,255,0.03), transparent);
}
.ws-world--active:hover {
  border-color: rgba(0,212,255,0.4);
}
.ws-world--full {
  opacity: 0.5;
  cursor: not-allowed;
}

.ws-world-left { display: flex; align-items: center; gap: 12px; flex: 1; }
.ws-world-icon {
  width: 34px; height: 34px; border-radius: 6px;
  border: 1px solid var(--border);
  background: var(--bg3);
  display: flex; align-items: center; justify-content: center;
  font-size: 16px;
  flex-shrink: 0;
}
.ws-icon--new { border-color: rgba(0,212,255,0.3); }
.ws-icon--online { border-color: var(--border-bright); }
.ws-icon--full { border-color: rgba(255,70,50,0.2); }

.ws-world-name { font-size: 13px; color: var(--bright); font-weight: 700; }
.ws-world-char { font-size: 11px; color: var(--cyan); font-weight: 400; }
.ws-world-meta { font-size: 9px; color: var(--muted); margin-top: 3px; display: flex; gap: 4px; }
.ws-meta-sep { color: var(--border-bright); }

.ws-world-right { display: flex; align-items: center; gap: 12px; }
.ws-world-status {
  font-family: var(--ff-title); font-size: 7px; font-weight: 700;
  letter-spacing: 2px; padding: 2px 8px; border: 1px solid;
}
.ws-status--online { color: var(--green); border-color: rgba(48,255,128,0.2); }
.ws-status--new { color: var(--cyan); border-color: rgba(0,212,255,0.3); background: rgba(0,212,255,0.05); }
.ws-status--full { color: var(--red); border-color: rgba(255,70,50,0.2); }

.ws-world-bar {
  width: 60px; height: 4px;
  background: var(--border);
  border-radius: 2px;
  overflow: hidden;
}
.ws-world-bar-fill {
  height: 100%;
  background: linear-gradient(90deg, var(--cyan-dark), var(--cyan));
  border-radius: 2px;
}

.ws-join-btn {
  font-family: var(--ff-title); font-size: 9px; font-weight: 700;
  letter-spacing: 2px; padding: 6px 16px;
  background: var(--bg3);
  border: 1px solid var(--border);
  color: var(--muted);
  cursor: pointer;
  transition: all 0.15s;
}
.ws-join-btn:hover:not(:disabled) {
  border-color: var(--cyan);
  color: var(--cyan);
  box-shadow: 0 0 8px rgba(0,212,255,0.1);
}
.ws-join--play {
  background: linear-gradient(180deg, var(--cyan-dark), var(--cyan-dim));
  border-color: var(--cyan);
  color: #fff;
  box-shadow: 0 0 10px rgba(0,212,255,0.15);
}
.ws-join--disabled {
  opacity: 0.4;
  cursor: not-allowed;
}
</style>