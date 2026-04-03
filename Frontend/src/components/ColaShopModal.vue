<template>
  <div class="cola-overlay" @click.self="!paying && $emit('close')">
    <div class="cola-modal">

      <!-- ─── STEP 1: Package selection ─── -->
      <template v-if="step === 'select'">
        <div class="cola-header">
          <div class="cola-header-left">
            <img :src="colaIcon" class="cola-header-icon" alt="Cola" />
            <div>
              <div class="cola-title">WASTELAND COLA SHOP</div>
              <div class="cola-subtitle">Fuel your conquest with premium currency</div>
            </div>
          </div>
          <div class="cola-balance">
            <img :src="colaIcon" class="cola-balance-icon" alt="" />
            <span class="cola-balance-amt">{{ (player?.wastelandCoins ?? 0).toLocaleString() }}</span>
          </div>
          <button class="cola-close" @click="$emit('close')">&#x2715;</button>
        </div>

        <div class="cola-accent" />

        <div v-if="error" class="cola-error">{{ error }}</div>

        <div class="cola-grid">
          <div
            v-for="pkg in packages"
            :key="pkg.id"
            class="cola-card"
            :class="{
              'cola-card--popular': pkg.popular,
              'cola-card--best': pkg.best,
              'cola-card--loading': loading === pkg.id
            }"
          >
            <div v-if="pkg.popular" class="cola-badge cola-badge--popular">POPULAR</div>
            <div v-else-if="pkg.best" class="cola-badge cola-badge--best">BEST VALUE</div>

            <div class="cola-pkg-icon-wrap">
              <img :src="colaIcon" class="cola-pkg-icon" :style="{ width: pkg.iconSize + 'px', height: pkg.iconSize + 'px' }" alt="Cola" />
            </div>

            <div class="cola-pkg-amount">{{ pkg.amount.toLocaleString() }}</div>
            <div class="cola-pkg-label">WASTELAND COLA</div>

            <div v-if="pkg.bonus" class="cola-pkg-bonus">+{{ pkg.bonus }}% MORE COLA</div>
            <div v-else class="cola-pkg-bonus cola-pkg-bonus--spacer">&amp;nbsp;</div>

            <button
              class="cola-buy-btn"
              :class="{ 'cola-buy-btn--best': pkg.best }"
              :disabled="!!loading"
              @click="selectPackage(pkg)"
            >
              <span v-if="loading === pkg.id" class="cola-spinner">&#x25CF;</span>
              <span v-else>{{ pkg.price }}</span>
            </button>
          </div>
        </div>

        <div class="cola-footer">
          <span class="cola-footer-note">
            Payments are processed securely by Stripe. Wasteland Cola is credited to your account after payment confirmation.
          </span>
        </div>
      </template>

      <!-- ─── STEP 2: Stripe Payment Element ─── -->
      <template v-else-if="step === 'pay'">
        <div class="cola-header">
          <div class="cola-header-left">
            <img :src="colaIcon" class="cola-header-icon" alt="Cola" />
            <div>
              <div class="cola-title">SECURE PAYMENT</div>
              <div class="cola-subtitle">
                {{ selectedPackage?.amount.toLocaleString() }} Wasteland Cola &mdash; {{ selectedPackage?.price }}
              </div>
            </div>
          </div>
          <button class="cola-close" :disabled="paying" @click="cancelPayment">&#x2715;</button>
        </div>

        <div class="cola-accent" />

        <div v-if="error" class="cola-error">{{ error }}</div>

        <div class="pay-body">
          <div id="stripe-payment-element" class="stripe-element-container"></div>

          <div class="pay-actions">
            <button class="pay-back-btn" :disabled="paying" @click="cancelPayment">
              &#8592; BACK
            </button>
            <button class="pay-confirm-btn" :disabled="paying || !stripeReady" @click="confirmPayment">
              <span v-if="paying" class="cola-spinner">&#x25CF;</span>
              <span v-else>PAY {{ selectedPackage?.price }}</span>
            </button>
          </div>

          <div class="pay-secure-note">
            <span class="pay-lock">&#x1F512;</span> Secured by Stripe &mdash; Bancontact, iDEAL, Visa, Mastercard accepted
          </div>
        </div>
      </template>

      <!-- ─── STEP 3: Success ─── -->
      <template v-else-if="step === 'success'">
        <div class="result-body">
          <img :src="colaIcon" class="result-icon" alt="Cola" />
          <div class="result-title result-title--ok">PURCHASE COMPLETE</div>
          <div class="result-msg">
            {{ selectedPackage?.amount.toLocaleString() }} Wasteland Cola have been added to your account.
          </div>
          <button class="result-btn result-btn--ok" @click="finish">CONTINUE</button>
        </div>
      </template>

    </div>
  </div>
</template>

<script setup>
import { ref, nextTick } from 'vue'
import { loadStripe } from '@stripe/stripe-js'
import { getStripePublishableKey, createPaymentIntent } from '../services/api'
import colaIcon from '../images/Currency/Wasteland Cola.png'

const props = defineProps({
  player: Object
})

const emit = defineEmits(['close', 'purchased'])

// ── State ────────────────────────────────────────────────────────────────────

const step = ref('select')     // select | pay | success
const loading = ref(null)      // package id currently loading
const paying = ref(false)
const stripeReady = ref(false)
const error = ref(null)
const selectedPackage = ref(null)

let stripe = null
let elements = null
let paymentElement = null

const packages = [
  { id: 'starter',   amount: 500,   price: '€4.99',  bonus: null, popular: false, best: false, iconSize: 56 },
  { id: 'scout',     amount: 1500,  price: '€9.99',  bonus: 50,   popular: false, best: false, iconSize: 64 },
  { id: 'commander', amount: 4000,  price: '€19.99', bonus: 100,  popular: true,  best: false, iconSize: 72 },
  { id: 'warlord',   amount: 12500, price: '€49.99', bonus: 150,  popular: false, best: false, iconSize: 80 },
  { id: 'overlord',  amount: 25000, price: '€79.99', bonus: 212,  popular: false, best: true,  iconSize: 88 },
]

// ── Package selection → create PaymentIntent ─────────────────────────────────

async function selectPackage(pkg) {
  if (!props.player?.id || loading.value) return
  error.value = null
  loading.value = pkg.id

  try {
    const publishableKey = await getStripePublishableKey()
    const { clientSecret } = await createPaymentIntent(props.player.id, pkg.id)

    selectedPackage.value = pkg
    step.value = 'pay'

    await nextTick()
    await mountStripeElement(publishableKey, clientSecret)
  } catch (err) {
    error.value = err?.response?.data ?? err?.message ?? 'Could not initialize payment. Please try again.'
  } finally {
    loading.value = null
  }
}

async function mountStripeElement(publishableKey, clientSecret) {
  stripe = await loadStripe(publishableKey)

  const appearance = {
    theme: 'night',
    variables: {
      colorPrimary: '#ffa600',
      colorBackground: '#0a1018',
      colorText: '#c8d8ea',
      colorDanger: '#ff4030',
      fontFamily: 'monospace, system-ui',
      borderRadius: '0px',
      spacingUnit: '4px',
    },
    rules: {
      '.Input': {
        border: '1px solid rgba(255,166,0,0.3)',
        backgroundColor: 'rgba(0,0,0,0.4)',
      },
      '.Input:focus': {
        border: '1px solid rgba(255,166,0,0.8)',
        boxShadow: '0 0 0 2px rgba(255,166,0,0.15)',
      },
      '.Label': {
        color: '#8ca0b8',
        letterSpacing: '1px',
        textTransform: 'uppercase',
        fontSize: '10px',
      },
      '.Tab': {
        border: '1px solid rgba(255,166,0,0.2)',
        backgroundColor: 'rgba(0,0,0,0.3)',
      },
      '.Tab--selected': {
        border: '1px solid rgba(255,166,0,0.7)',
        backgroundColor: 'rgba(255,166,0,0.08)',
      },
    },
  }

  elements = stripe.elements({ clientSecret, appearance })

  paymentElement = elements.create('payment', {
    layout: 'tabs',
    paymentMethodOrder: ['bancontact', 'ideal', 'card'],
  })

  paymentElement.mount('#stripe-payment-element')

  paymentElement.on('ready', () => { stripeReady.value = true })
}

// ── Confirm payment ───────────────────────────────────────────────────────────

async function confirmPayment() {
  if (!stripe || !elements || paying.value) return
  paying.value = true
  error.value = null

  const { error: stripeError } = await stripe.confirmPayment({
    elements,
    confirmParams: {
      return_url: `${window.location.origin}/payment-return`,
    },
    redirect: 'if_required',
  })

  paying.value = false

  if (stripeError) {
    error.value = stripeError.message
  } else {
    step.value = 'success'
  }
}

// ── Navigation ────────────────────────────────────────────────────────────────

function cancelPayment() {
  destroyElement()
  step.value = 'select'
  selectedPackage.value = null
  error.value = null
}

function finish() {
  emit('purchased')
  emit('close')
}

function destroyElement() {
  if (paymentElement) {
    paymentElement.unmount()
    paymentElement = null
  }
  elements = null
  stripe = null
  stripeReady.value = false
}
</script>

<style scoped>
/* ── Overlay ── */
.cola-overlay {
  position: fixed;
  inset: 0;
  z-index: 999999;
  background: rgba(0, 4, 10, 0.88);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
}

.cola-modal {
  position: relative;
  width: min(920px, 96vw);
  max-height: 92vh;
  overflow-y: auto;
  background: linear-gradient(180deg, rgba(10, 16, 26, 0.99), rgba(4, 10, 18, 0.99));
  border: 1px solid rgba(255, 166, 0, 0.4);
  box-shadow: 0 0 60px rgba(0, 0, 0, 0.7), 0 0 60px rgba(255, 166, 0, 0.06);
}

/* ── Header ── */
.cola-header {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 20px 24px 14px;
  position: relative;
}

.cola-header-left {
  display: flex;
  align-items: center;
  gap: 14px;
  flex: 1;
}

.cola-header-icon {
  width: 56px;
  height: 56px;
  object-fit: contain;
  filter: drop-shadow(0 0 12px rgba(255, 166, 0, 0.5));
}

.cola-title {
  font-family: var(--ff-title), sans-serif;
  font-size: 20px;
  letter-spacing: 4px;
  color: #ffa600;
  text-shadow: 0 0 14px rgba(255, 166, 0, 0.35);
}

.cola-subtitle {
  margin-top: 4px;
  font-size: 10px;
  letter-spacing: 1.5px;
  color: var(--muted);
}

.cola-balance {
  display: flex;
  align-items: center;
  gap: 7px;
  padding: 6px 14px;
  border: 1px solid rgba(255, 166, 0, 0.3);
  background: rgba(255, 166, 0, 0.06);
  margin-right: 16px;
}

.cola-balance-icon {
  width: 22px;
  height: 22px;
  object-fit: contain;
}

.cola-balance-amt {
  font-family: var(--ff-title), sans-serif;
  font-size: 14px;
  color: #ffa600;
  letter-spacing: 1px;
}

.cola-close {
  position: absolute;
  top: 16px;
  right: 16px;
  width: 28px;
  height: 28px;
  border: 1px solid var(--border);
  background: var(--bg3);
  color: var(--muted);
  font-size: 12px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.15s;
}

.cola-close:hover:not(:disabled) {
  border-color: #ff4030;
  color: #ff4030;
}

.cola-close:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.cola-accent {
  height: 1px;
  background: linear-gradient(90deg, transparent, rgba(255, 166, 0, 0.5), transparent);
  margin: 0 24px;
}

/* ── Error ── */
.cola-error {
  margin: 12px 24px 0;
  padding: 10px 14px;
  background: rgba(255, 60, 40, 0.1);
  border: 1px solid rgba(255, 60, 40, 0.4);
  color: #ff6050;
  font-size: 11px;
  letter-spacing: 1px;
}

/* ── Package grid ── */
.cola-grid {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 12px;
  padding: 24px;
}

@media (max-width: 680px) {
  .cola-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

.cola-card {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 20px 10px 16px;
  background: rgba(255, 166, 0, 0.04);
  border: 1px solid rgba(255, 166, 0, 0.2);
  transition: all 0.2s;
}

.cola-card:hover {
  border-color: rgba(255, 166, 0, 0.55);
  background: rgba(255, 166, 0, 0.09);
  box-shadow: 0 0 20px rgba(255, 166, 0, 0.1);
  transform: translateY(-2px);
}

.cola-card--popular {
  border-color: rgba(255, 166, 0, 0.55);
  background: rgba(255, 166, 0, 0.08);
}

.cola-card--best {
  border-color: rgba(0, 212, 255, 0.5);
  background: rgba(0, 212, 255, 0.05);
}

.cola-card--loading {
  opacity: 0.7;
  pointer-events: none;
}

.cola-badge {
  position: absolute;
  top: -1px;
  left: 50%;
  transform: translateX(-50%);
  padding: 2px 10px;
  font-family: var(--ff-title), sans-serif;
  font-size: 8px;
  letter-spacing: 2px;
  white-space: nowrap;
}

.cola-badge--popular { background: #ffa600; color: #000; }
.cola-badge--best { background: var(--cyan); color: #000; }

.cola-pkg-icon-wrap {
  padding-top: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.cola-pkg-icon {
  object-fit: contain;
  filter: drop-shadow(0 0 8px rgba(255, 166, 0, 0.4)) brightness(1.05) contrast(1.1);
}

.cola-pkg-amount {
  font-family: var(--ff-title), sans-serif;
  font-size: 20px;
  letter-spacing: 1px;
  color: #ffa600;
  text-shadow: 0 0 10px rgba(255, 166, 0, 0.3);
  line-height: 1;
}

.cola-card--best .cola-pkg-amount {
  color: var(--cyan);
  text-shadow: 0 0 10px rgba(0, 212, 255, 0.3);
}

.cola-pkg-label {
  font-size: 8px;
  letter-spacing: 2px;
  color: var(--muted);
}

.cola-pkg-bonus {
  font-family: var(--ff-title), sans-serif;
  font-size: 9px;
  letter-spacing: 1.5px;
  color: #4cff80;
  text-shadow: 0 0 8px rgba(76, 255, 128, 0.4);
}

.cola-pkg-bonus--spacer {
  visibility: hidden;
  font-size: 9px;
}

.cola-buy-btn {
  width: 100%;
  padding: 8px 4px;
  background: rgba(255, 166, 0, 0.12);
  border: 1px solid rgba(255, 166, 0, 0.5);
  color: #ffa600;
  font-family: var(--ff-title), sans-serif;
  font-size: 13px;
  letter-spacing: 1px;
  cursor: pointer;
  transition: all 0.15s;
  margin-top: auto;
}

.cola-buy-btn:hover:not(:disabled) {
  background: rgba(255, 166, 0, 0.25);
  border-color: #ffa600;
  box-shadow: 0 0 14px rgba(255, 166, 0, 0.2);
}

.cola-buy-btn:disabled { opacity: 0.5; cursor: not-allowed; }
.cola-buy-btn--best { border-color: rgba(0, 212, 255, 0.5); color: var(--cyan); background: rgba(0, 212, 255, 0.1); }
.cola-buy-btn--best:hover:not(:disabled) { background: rgba(0, 212, 255, 0.2); border-color: var(--cyan); }

.cola-footer {
  padding: 12px 24px 16px;
  border-top: 1px solid var(--border);
}

.cola-footer-note {
  font-size: 9px;
  letter-spacing: 1px;
  color: var(--muted);
  line-height: 1.6;
}

/* ── Payment step ── */
.pay-body {
  padding: 24px;
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.stripe-element-container {
  background: rgba(0, 0, 0, 0.25);
  border: 1px solid rgba(255, 166, 0, 0.2);
  padding: 20px;
  min-height: 150px;
}

.pay-actions {
  display: flex;
  gap: 12px;
}

.pay-back-btn {
  padding: 12px 20px;
  background: transparent;
  border: 1px solid var(--border);
  color: var(--muted);
  font-family: var(--ff-title), sans-serif;
  font-size: 11px;
  letter-spacing: 2px;
  cursor: pointer;
  transition: all 0.15s;
  flex-shrink: 0;
}

.pay-back-btn:hover:not(:disabled) { border-color: var(--muted); color: var(--text); }
.pay-back-btn:disabled { opacity: 0.4; cursor: not-allowed; }

.pay-confirm-btn {
  flex: 1;
  padding: 14px;
  background: rgba(255, 166, 0, 0.15);
  border: 1px solid rgba(255, 166, 0, 0.6);
  color: #ffa600;
  font-family: var(--ff-title), sans-serif;
  font-size: 15px;
  letter-spacing: 3px;
  cursor: pointer;
  transition: all 0.15s;
}

.pay-confirm-btn:hover:not(:disabled) {
  background: rgba(255, 166, 0, 0.28);
  border-color: #ffa600;
  box-shadow: 0 0 20px rgba(255, 166, 0, 0.2);
}

.pay-confirm-btn:disabled { opacity: 0.4; cursor: not-allowed; }

.pay-secure-note {
  font-size: 10px;
  color: var(--muted);
  letter-spacing: 1px;
  text-align: center;
}

/* ── Result screens ── */
.result-body {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
  padding: 48px 32px;
  text-align: center;
}

.result-icon {
  width: 80px;
  height: 80px;
  object-fit: contain;
  filter: drop-shadow(0 0 20px rgba(255, 166, 0, 0.6));
}

.result-title {
  font-family: var(--ff-title), sans-serif;
  font-size: 22px;
  letter-spacing: 4px;
}

.result-title--ok { color: #4cff80; text-shadow: 0 0 14px rgba(76, 255, 128, 0.3); }

.result-msg {
  font-size: 12px;
  color: var(--muted);
  letter-spacing: 1px;
  max-width: 380px;
  line-height: 1.7;
}

.result-btn {
  padding: 12px 28px;
  font-family: var(--ff-title), sans-serif;
  font-size: 12px;
  letter-spacing: 2px;
  cursor: pointer;
  border: 1px solid;
  transition: all 0.15s;
  background: transparent;
}

.result-btn--ok { border-color: rgba(76, 255, 128, 0.5); color: #4cff80; }
.result-btn--ok:hover { background: rgba(76, 255, 128, 0.12); }

/* ── Spinner ── */
.cola-spinner {
  animation: spin 0.6s linear infinite;
  display: inline-block;
}

@keyframes spin { to { transform: rotate(360deg); } }
</style>
