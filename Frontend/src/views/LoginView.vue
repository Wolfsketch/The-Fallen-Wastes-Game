<template>
  <div class="login-wrapper">
    <div class="login-bg-grid" />

    <div class="login-card">
      <div class="login-accent" />

      <div class="login-body">
        <div class="login-logo">
          <div class="radar">
            <div class="radar-ring" />
            <div class="radar-core">☢</div>
          </div>
          <h1 class="login-title">THE FALLEN WASTES</h1>
          <p class="login-subtitle">{{ isLogin ? 'RECONNECT TO OUTPOST' : 'INITIALIZE NEW OUTPOST' }}</p>
        </div>

        <!-- LOGIN / REGISTER toggle -->
        <div class="mode-toggle">
          <button class="mode-btn" :class="{ 'mode-btn--active': isLogin }" @click="isLogin = true; error = ''">LOGIN</button>
          <button class="mode-btn" :class="{ 'mode-btn--active': !isLogin }" @click="isLogin = false; error = ''">REGISTER</button>
        </div>

        <div v-if="error" class="login-error">
          <span class="tag tag--red">ERROR</span>
          {{ error }}
        </div>

        <div class="login-fields">
          <div class="field-group">
            <label class="field-label">OPERATOR CALLSIGN</label>
            <input v-model="username" type="text" class="field-input"
                   :placeholder="isLogin ? 'Enter your username' : 'Enter username (min 5 chars)'"
                   @keyup.enter="submit" />
          </div>

          <template v-if="!isLogin">
            <div class="field-group">
              <label class="field-label">COMM FREQUENCY</label>
              <input v-model="email" type="email" class="field-input" placeholder="Enter email address" @keyup.enter="submit" />
            </div>
            <div class="field-group">
              <label class="field-label">OUTPOST DESIGNATION</label>
              <input v-model="settlementName" type="text" class="field-input" placeholder="Name your settlement (min 3 chars)" @keyup.enter="submit" />
            </div>
          </template>
        </div>

        <button class="login-btn" :class="{ 'login-btn--loading': loading }" :disabled="loading" @click="submit">
          {{ loading ? (isLogin ? 'RECONNECTING...' : 'ESTABLISHING LINK...') : (isLogin ? 'ENTER THE WASTES' : 'DEPLOY OUTPOST') }}
        </button>

        <div v-if="loading" class="sync-status">
          <div class="sync-label">{{ syncStep }}</div>
          <div class="sync-bar">
            <div class="sync-fill" :style="{ width: syncProgress + '%' }"></div>
          </div>
        </div>

        <p class="login-footer">Sector 7 &nbsp;·&nbsp; 247 operators active</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { createPlayer, loginByUsername } from '../services/api'

const router = useRouter()
const isLogin = ref(true)
const username = ref('')
const email = ref('')
const settlementName = ref('')
const loading = ref(false)
const error = ref('')
const syncStep = ref('')
const syncProgress = ref(0)

async function submit() {
  error.value = ''

  if (!username.value) {
    error.value = 'Username is required.'
    return
  }

  if (!isLogin.value && (!email.value || !settlementName.value)) {
    error.value = 'All fields are required.'
    return
  }

  loading.value = true
  syncProgress.value = 10
  syncStep.value = isLogin.value
      ? 'CONNECTING TO WASTELAND SYSTEMS...'
      : 'INITIALIZING NEW OUTPOST...'

  try {
    let data

    syncProgress.value = 30
    syncStep.value = isLogin.value
        ? 'CHECKING ACCOUNT VERSION...'
        : 'REGISTERING OPERATOR...'

    if (isLogin.value) {
      data = await loginByUsername(username.value)
    } else {
      data = await createPlayer(username.value, email.value, settlementName.value)
    }

    if (data.wasMigrated) {
      syncProgress.value = 70
      syncStep.value = 'APPLYING PLAYER DATA UPDATES...'
      await new Promise(resolve => setTimeout(resolve, 700))
    } else {
      syncProgress.value = 70
      syncStep.value = 'SYNCHRONIZING OUTPOST DATA...'
      await new Promise(resolve => setTimeout(resolve, 300))
    }

    syncProgress.value = 95
    syncStep.value = 'FINALIZING LOGIN...'

    sessionStorage.setItem('playerId', data.id)
    sessionStorage.setItem('playerData', JSON.stringify(data))

    syncProgress.value = 100
    syncStep.value = 'SYNC COMPLETE'
    await new Promise(resolve => setTimeout(resolve, 250))

    router.push('/worlds')
  } catch (err) {
    if (err.response?.status === 404) {
      error.value = 'Player not found — register first.'
    } else if (err.response?.data) {
      error.value = typeof err.response.data === 'string'
          ? err.response.data
          : JSON.stringify(err.response.data)
    } else {
      error.value = 'Connection failed — is the backend running?'
    }
  } finally {
    loading.value = false
    syncStep.value = ''
    syncProgress.value = 0
  }
}
</script>

<style scoped>
.login-wrapper{min-height:100vh;display:flex;align-items:center;justify-content:center;background:var(--bg);position:relative;overflow:hidden;padding:20px}
.login-bg-grid{position:absolute;inset:0;opacity:.025;background-image:linear-gradient(var(--cyan) 1px,transparent 1px),linear-gradient(90deg,var(--cyan) 1px,transparent 1px);background-size:60px 60px}
.login-card{width:100%;max-width:420px;background:var(--bg2);border:1px solid var(--border-bright);position:relative;overflow:hidden;box-shadow:0 0 80px rgba(0,212,255,.04)}
.login-accent{height:2px;background:linear-gradient(90deg,transparent,var(--cyan),transparent)}
.login-body{padding:36px 32px}
.login-logo{text-align:center;margin-bottom:24px}
.radar{position:relative;width:48px;height:48px;margin:0 auto 16px}
.radar-ring{position:absolute;inset:0;border:2px solid var(--cyan-dim);border-radius:50%;border-top-color:var(--cyan);animation:radarSweep 3s linear infinite}
.radar-core{position:absolute;inset:8px;background:radial-gradient(circle,var(--cyan-dim),transparent);border-radius:50%;display:flex;align-items:center;justify-content:center;font-size:16px;color:var(--cyan)}
.login-title{font-family:var(--ff-title);font-size:20px;color:var(--cyan);letter-spacing:5px;font-weight:700;text-shadow:0 0 20px rgba(0,212,255,.3)}
.login-subtitle{font-family:var(--ff-title);font-size:9px;color:var(--muted);letter-spacing:4px;margin-top:8px}
.mode-toggle{display:flex;margin-bottom:24px;border:1px solid var(--border)}
.mode-btn{flex:1;padding:8px;background:transparent;border:none;color:var(--muted);font-family:var(--ff-title);font-size:10px;font-weight:700;letter-spacing:2px;cursor:pointer;transition:all .2s}
.mode-btn:first-child{border-right:1px solid var(--border)}
.mode-btn--active{background:var(--cyan-glow);color:var(--cyan);box-shadow:inset 0 -2px 0 var(--cyan)}
.mode-btn:hover:not(.mode-btn--active){color:var(--text)}
.login-error{background:rgba(255,48,64,.06);border:1px solid rgba(255,48,64,.2);padding:10px 14px;margin-bottom:20px;font-size:11px;color:var(--red);display:flex;align-items:center;gap:8px}
.login-fields{display:flex;flex-direction:column;gap:16px;margin-bottom:24px}
.field-group{display:flex;flex-direction:column;gap:6px}
.field-label{font-family:var(--ff-title);font-size:8px;color:var(--cyan);letter-spacing:3px;font-weight:700}
.field-input{background:var(--bg3);border:1px solid var(--border);color:var(--bright);font-family:var(--ff);font-size:14px;font-weight:500;padding:10px 14px;outline:none;transition:border-color .2s,box-shadow .2s}
.field-input::placeholder{color:var(--muted);font-size:11px}
.field-input:focus{border-color:var(--cyan-dark);box-shadow:0 0 12px rgba(0,212,255,.08)}
.login-btn{width:100%;padding:13px;background:linear-gradient(180deg,var(--cyan-dark),var(--cyan-dim));border:1px solid var(--cyan);color:#fff;font-family:var(--ff-title);font-size:12px;font-weight:700;cursor:pointer;text-transform:uppercase;letter-spacing:3px;box-shadow:0 0 20px rgba(0,212,255,.2);transition:all .2s}
.login-btn:hover:not(:disabled){box-shadow:0 0 30px rgba(0,212,255,.35);background:linear-gradient(180deg,#009acc,var(--cyan-dark))}
.login-btn--loading{opacity:.6;cursor:wait}
.login-footer{text-align:center;margin-top:20px;font-size:9px;color:var(--muted);letter-spacing:2px}
.sync-status{margin-top:16px;display:flex;flex-direction:column;gap:8px}
.sync-label{font-family:var(--ff-title);font-size:9px;color:var(--cyan);letter-spacing:2px}
.sync-bar{width:100%;height:8px;border:1px solid var(--border);background:var(--bg3);overflow:hidden}
.sync-fill{height:100%;background:linear-gradient(90deg,var(--cyan-dark),var(--cyan));transition:width .25s ease}
</style>