<template>
  <div class="cola-overlay" @click.self="$emit('close')">
    <div class="cola-modal">
      <!-- Header -->
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
        <button class="cola-close" @click="$emit('close')">✕</button>
      </div>

      <div class="cola-accent" />

      <!-- Error -->
      <div v-if="error" class="cola-error">{{ error }}</div>

      <!-- Packages grid -->
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
          <div v-else class="cola-pkg-bonus cola-pkg-bonus--spacer"></div>

          <button
            class="cola-buy-btn"
            :disabled="!!loading"
            @click="purchase(pkg)"
          >
            <span v-if="loading === pkg.id" class="cola-spinner">●</span>
            <span v-else>{{ pkg.price }}</span>
          </button>
        </div>
      </div>

      <!-- Footer -->
      <div class="cola-footer">
        <span class="cola-footer-note">
          Wasteland Cola is used to activate advisors and unlock premium features.
          All purchases are for demonstration purposes only.
        </span>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { purchaseWastelandCoins } from '../services/api'
import colaIcon from '../images/Currency/Wasteland Cola.png'

const props = defineProps({
  player: Object
})

const emit = defineEmits(['close', 'purchased'])

const loading = ref(null)
const error = ref(null)

const packages = [
  {
    id: 'starter',
    amount: 500,
    price: '€4.99',
    bonus: null,
    popular: false,
    best: false,
    iconSize: 56
  },
  {
    id: 'scout',
    amount: 1500,
    price: '€9.99',
    bonus: 50,
    popular: false,
    best: false,
    iconSize: 64
  },
  {
    id: 'commander',
    amount: 4000,
    price: '€19.99',
    bonus: 100,
    popular: true,
    best: false,
    iconSize: 72
  },
  {
    id: 'warlord',
    amount: 12500,
    price: '€49.99',
    bonus: 150,
    popular: false,
    best: false,
    iconSize: 80
  },
  {
    id: 'overlord',
    amount: 25000,
    price: '€79.99',
    bonus: 212,
    popular: false,
    best: true,
    iconSize: 88
  }
]

async function purchase(pkg) {
  if (!props.player?.id || loading.value) return
  error.value = null
  loading.value = pkg.id
  try {
    await purchaseWastelandCoins(props.player.id, pkg.id)
    emit('purchased')
    emit('close')
  } catch (err) {
    error.value = err?.response?.data?.message ?? 'Purchase failed. Please try again.'
  } finally {
    loading.value = null
  }
}
</script>

<style scoped>
.cola-overlay {
  position: fixed;
  inset: 0;
  z-index: 999999;
  background: rgba(0, 4, 10, 0.85);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
}

.cola-modal {
  position: relative;
  width: min(900px, 96vw);
  max-height: 92vh;
  overflow-y: auto;
  background: linear-gradient(180deg, rgba(10, 16, 26, 0.99), rgba(4, 10, 18, 0.99));
  border: 1px solid rgba(255, 166, 0, 0.4);
  box-shadow:
    0 0 60px rgba(0, 0, 0, 0.7),
    0 0 60px rgba(255, 166, 0, 0.06);
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
  transition: all .15s;
}

.cola-close:hover {
  border-color: #ff4030;
  color: #ff4030;
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

/* ── Grid ── */
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

/* ── Card ── */
.cola-card {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 20px 10px 16px;
  background: rgba(255, 166, 0, 0.04);
  border: 1px solid rgba(255, 166, 0, 0.2);
  transition: all .2s;
  cursor: default;
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

/* ── Badge ── */
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

.cola-badge--popular {
  background: #ffa600;
  color: #000;
}

.cola-badge--best {
  background: var(--cyan);
  color: #000;
}

/* ── Package content ── */
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

/* ── Buy button ── */
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
  transition: all .15s;
  margin-top: auto;
}

.cola-buy-btn:hover:not(:disabled) {
  background: rgba(255, 166, 0, 0.25);
  border-color: #ffa600;
  box-shadow: 0 0 14px rgba(255, 166, 0, 0.2);
}

.cola-buy-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.cola-card--best .cola-buy-btn {
  border-color: rgba(0, 212, 255, 0.5);
  color: var(--cyan);
  background: rgba(0, 212, 255, 0.1);
}

.cola-card--best .cola-buy-btn:hover:not(:disabled) {
  background: rgba(0, 212, 255, 0.2);
  border-color: var(--cyan);
}

.cola-spinner {
  animation: spin 0.6s linear infinite;
  display: inline-block;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* ── Footer ── */
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
</style>
