<template>
  <div class="pr-overlay">
    <div class="pr-card">

      <!-- ── Header ── -->
      <div class="pr-header">
        <img :src="colaIcon" class="pr-icon" alt="Wasteland Cola" />
        <div class="pr-header-text">
          <div class="pr-title">{{ title }}</div>
          <div class="pr-subtitle">WASTELAND COLA SHOP</div>
        </div>
      </div>

      <div class="pr-accent" />

      <!-- ── Loading ── -->
      <div v-if="phase === 'loading'" class="pr-body">
        <div class="pr-spinner-wrap">
          <span class="pr-spinner">&#x25CF;</span>
        </div>
        <div class="pr-msg">Verifying payment&hellip;</div>
      </div>

      <!-- ── Succeeded ── -->
      <div v-else-if="phase === 'succeeded'" class="pr-body">
        <div class="pr-status-icon pr-status-icon--ok">&#x2713;</div>
        <div class="pr-msg pr-msg--ok">
          Payment received. Your Wasteland Cola will be credited to your account after webhook confirmation.
        </div>
        <div v-if="colaAmount" class="pr-amount">
          <img :src="colaIcon" class="pr-amount-icon" alt="" />
          <span>{{ colaAmount.toLocaleString() }} Wasteland Cola</span>
        </div>
        <div class="pr-actions">
          <button class="pr-btn pr-btn--ok" @click="goToGame">RETURN TO GAME</button>
        </div>
      </div>

      <!-- ── Failed ── -->
      <div v-else-if="phase === 'failed'" class="pr-body">
        <div class="pr-status-icon pr-status-icon--fail">&#x2715;</div>
        <div class="pr-msg pr-msg--fail">
          Payment failed or was declined. No charges have been made. Please try again.
        </div>
        <div class="pr-actions">
          <button class="pr-btn pr-btn--back" @click="goToGame">&#8592; BACK TO SHOP</button>
        </div>
      </div>

      <!-- ── Canceled ── -->
      <div v-else-if="phase === 'canceled'" class="pr-body">
        <div class="pr-status-icon pr-status-icon--warn">&#x21;</div>
        <div class="pr-msg pr-msg--warn">
          Payment was canceled. You have not been charged.
        </div>
        <div class="pr-actions">
          <button class="pr-btn pr-btn--back" @click="goToGame">&#8592; BACK TO SHOP</button>
        </div>
      </div>

      <!-- ── Unknown ── -->
      <div v-else class="pr-body">
        <div class="pr-status-icon pr-status-icon--warn">?</div>
        <div class="pr-msg pr-msg--warn">
          Payment status is unknown. Check your email or visit the shop to verify.
        </div>
        <div class="pr-actions">
          <button class="pr-btn pr-btn--back" @click="goToGame">&#8592; RETURN TO GAME</button>
        </div>
      </div>

      <div class="pr-footer">
        Coins are only credited after Stripe confirms the payment via webhook.
        This page is for feedback only — it does not grant currency.
      </div>

    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { getPaymentStatus } from '../services/api'
import colaIcon from '../images/Currency/Wasteland Cola.png'

const router = useRouter()

// ── State ────────────────────────────────────────────────────────────────────

// phase: loading | succeeded | failed | canceled | unknown
const phase = ref('loading')
const colaAmount = ref(null)

// ── Derived heading ───────────────────────────────────────────────────────────

const title = computed(() => ({
  loading:   'VERIFYING PAYMENT',
  succeeded: 'PURCHASE COMPLETE',
  failed:    'PAYMENT FAILED',
  canceled:  'PAYMENT CANCELED',
}[phase.value] ?? 'PAYMENT STATUS'))

// ── On mount: read URL params and resolve status ──────────────────────────────

onMounted(async () => {
  const params = new URLSearchParams(window.location.search)
  const redirectStatus  = params.get('redirect_status')
  const paymentIntentId = params.get('payment_intent')

  // Fast-path: Stripe already tells us the outcome via redirect_status
  if (redirectStatus === 'failed') {
    phase.value = 'failed'
    return
  }
  if (redirectStatus === 'canceled') {
    phase.value = 'canceled'
    return
  }

  // For succeeded (and any ambiguous status), also verify with our backend so
  // the user sees accurate cola amount info once the webhook has processed it.
  if (paymentIntentId) {
    try {
      // Poll once immediately; webhook may already have processed
      const data = await getPaymentStatus(paymentIntentId)
      colaAmount.value = data.colaAmount ?? null

      if (data.status === 'succeeded') {
        phase.value = 'succeeded'
      } else if (data.status === 'failed') {
        phase.value = 'failed'
      } else if (redirectStatus === 'succeeded') {
        // Backend still shows pending — webhook hasn't fired yet, but Stripe says succeeded
        phase.value = 'succeeded'
      } else {
        phase.value = 'unknown'
      }
    } catch {
      // If the backend call fails, fall back to what Stripe told us
      if (redirectStatus === 'succeeded') {
        phase.value = 'succeeded'
      } else {
        phase.value = 'unknown'
      }
    }
  } else {
    // No payment_intent param at all — direct navigation to this page
    phase.value = 'unknown'
  }
})

// ── Navigation ────────────────────────────────────────────────────────────────

function goToGame() {
  router.push({ name: 'camp' })
}
</script>

<style scoped>
.pr-overlay {
  position: fixed;
  inset: 0;
  z-index: 100;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
}

.pr-card {
  width: min(560px, 96vw);
  background: linear-gradient(180deg, rgba(10,16,26,0.99), rgba(4,10,18,0.99));
  border: 1px solid rgba(255,166,0,0.4);
  box-shadow: 0 0 60px rgba(0,0,0,0.8), 0 0 40px rgba(255,166,0,0.06);
}

/* ── Header ── */
.pr-header {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 22px 28px 16px;
}

.pr-icon {
  width: 52px;
  height: 52px;
  object-fit: contain;
  filter: drop-shadow(0 0 12px rgba(255,166,0,0.5));
  flex-shrink: 0;
}

.pr-header-text {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.pr-title {
  font-family: var(--ff-title), sans-serif;
  font-size: 18px;
  letter-spacing: 4px;
  color: #ffa600;
  text-shadow: 0 0 14px rgba(255,166,0,0.3);
}

.pr-subtitle {
  font-size: 9px;
  letter-spacing: 2px;
  color: var(--muted, #6a82a0);
}

.pr-accent {
  height: 1px;
  background: linear-gradient(90deg, transparent, rgba(255,166,0,0.5), transparent);
  margin: 0 28px;
}

/* ── Body ── */
.pr-body {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 20px;
  padding: 36px 28px 28px;
  text-align: center;
}

/* ── Spinner ── */
.pr-spinner-wrap {
  font-size: 28px;
  color: #ffa600;
}

.pr-spinner {
  display: inline-block;
  animation: spin 0.7s linear infinite;
}

@keyframes spin { to { transform: rotate(360deg); } }

/* ── Status icons ── */
.pr-status-icon {
  width: 56px;
  height: 56px;
  border-radius: 50%;
  border: 2px solid;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
  font-family: var(--ff-title), sans-serif;
  flex-shrink: 0;
}

.pr-status-icon--ok   { border-color: #4cff80; color: #4cff80; text-shadow: 0 0 12px rgba(76,255,128,0.4); }
.pr-status-icon--fail { border-color: #ff4030; color: #ff4030; text-shadow: 0 0 12px rgba(255,60,40,0.4); }
.pr-status-icon--warn { border-color: #ffa600; color: #ffa600; text-shadow: 0 0 12px rgba(255,166,0,0.4); }

/* ── Messages ── */
.pr-msg {
  font-size: 12px;
  letter-spacing: 0.8px;
  line-height: 1.7;
  color: var(--muted, #8ca0b8);
  max-width: 380px;
}

.pr-msg--ok   { color: #c8d8ea; }
.pr-msg--fail { color: #ff8070; }
.pr-msg--warn { color: #c8d8ea; }

/* ── Cola amount badge ── */
.pr-amount {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 18px;
  border: 1px solid rgba(255,166,0,0.35);
  background: rgba(255,166,0,0.07);
  font-family: var(--ff-title), sans-serif;
  font-size: 15px;
  letter-spacing: 1px;
  color: #ffa600;
}

.pr-amount-icon {
  width: 22px;
  height: 22px;
  object-fit: contain;
}

/* ── Buttons ── */
.pr-actions {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
  justify-content: center;
}

.pr-btn {
  padding: 12px 28px;
  font-family: var(--ff-title), sans-serif;
  font-size: 12px;
  letter-spacing: 2px;
  cursor: pointer;
  border: 1px solid;
  background: transparent;
  transition: all 0.15s;
}

.pr-btn--ok {
  border-color: rgba(76,255,128,0.5);
  color: #4cff80;
}
.pr-btn--ok:hover {
  background: rgba(76,255,128,0.1);
  border-color: #4cff80;
  box-shadow: 0 0 14px rgba(76,255,128,0.15);
}

.pr-btn--back {
  border-color: rgba(255,166,0,0.4);
  color: #ffa600;
}
.pr-btn--back:hover {
  background: rgba(255,166,0,0.1);
  border-color: #ffa600;
  box-shadow: 0 0 14px rgba(255,166,0,0.15);
}

/* ── Footer note ── */
.pr-footer {
  padding: 12px 28px 18px;
  font-size: 9px;
  letter-spacing: 1px;
  color: var(--muted, #6a82a0);
  line-height: 1.6;
  border-top: 1px solid rgba(255,166,0,0.1);
  text-align: center;
}
</style>
