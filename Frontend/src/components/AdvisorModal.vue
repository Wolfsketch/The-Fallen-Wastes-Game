<template>
  <div class="advisor-overlay" @click.self="$emit('close')">
    <div class="advisor-modal">
      <div class="modal-header">
        <h2 class="modal-title">WASTELAND ADVISORS</h2>
        <p class="modal-subtitle">Activate advisors to unlock powerful bonuses for your settlement</p>
        <button class="modal-close" @click="$emit('close')">✕</button>
      </div>

      <div class="modal-accent" />

      <div class="advisors-grid">
        <div
            v-for="adv in advisors"
            :key="adv.id"
            class="adv-card"
            :class="{
              'adv-card--active': adv.active,
              'adv-card--selected': selectedId === adv.id
            }"
            @click="selectedId = adv.id"
        >
          <div class="adv-portrait">
            <img v-if="adv.image" :src="adv.image" :alt="adv.name" class="adv-portrait-img" />
            <div v-else class="adv-portrait-placeholder">
              <span class="adv-portrait-icon">{{ adv.icon }}</span>
            </div>
            <div v-if="adv.active" class="adv-active-badge">ACTIVE</div>
          </div>

          <div class="adv-name">{{ adv.name }}</div>
          <div class="adv-role">{{ adv.role }}</div>

          <div class="adv-bonuses-mini">
            <div v-for="b in adv.bonuses" :key="b.label" class="adv-bonus-mini">
              <span class="bonus-mini-icon">{{ b.icon }}</span>
              <span class="bonus-mini-value">{{ b.value }}</span>
            </div>
          </div>

          <div class="adv-duration">
            <span class="adv-duration-icon">⏱</span>
            {{ adv.duration }} days
          </div>

          <div class="adv-status">
            <template v-if="adv.active">
              <span class="adv-status-active">{{ adv.daysRemaining }}d remaining</span>
            </template>
            <template v-else>
              <span class="adv-status-inactive">Not activated</span>
            </template>
          </div>

      <button
              class="adv-activate-btn"
              :class="{
                'adv-activate-btn--active': adv.active,
                'adv-activate-btn--extend': adv.active
              }"
              @click.stop="$emit('activate', adv.id)"
          >
            <template v-if="adv.active">
              EXTEND &mdash; {{ adv.cost }}&nbsp;<img :src="colaIcon" class="advisor-cola-icon" alt="Cola" />
            </template>
            <template v-else>
              ACTIVATE &mdash; {{ adv.cost }}&nbsp;<img :src="colaIcon" class="advisor-cola-icon" alt="Cola" />
            </template>
          </button>
        </div>
      </div>

      <!-- Detail panel for selected advisor -->
      <div v-if="selectedAdvisor" class="adv-detail">
        <div class="detail-accent" />
        <div class="detail-body">
          <div class="detail-header">
            <span class="detail-icon">{{ selectedAdvisor.icon }}</span>
            <div>
              <div class="detail-name">{{ selectedAdvisor.name }}</div>
              <div class="detail-role">{{ selectedAdvisor.role }}</div>
            </div>
          </div>

          <p class="detail-desc">{{ selectedAdvisor.description }}</p>

          <div class="detail-bonuses">
            <div class="detail-bonuses-title">BONUSES WHEN ACTIVE</div>
            <div
                v-for="b in selectedAdvisor.bonuses"
                :key="b.label"
                class="detail-bonus-row"
            >
              <div class="bonus-row-left">
                <span class="bonus-row-icon">{{ b.icon }}</span>
                <div>
                  <div class="bonus-row-label">{{ b.label }}</div>
                  <div class="bonus-row-desc">{{ b.description }}</div>
                </div>
              </div>
              <div class="bonus-row-value">{{ b.value }}</div>
            </div>
          </div>
        </div>
      </div>

      <div class="modal-footer">
        <div class="footer-cola-balance">
          <img :src="colaIcon" class="footer-cola-icon" alt="Cola" />
          <span class="footer-cola-amt">{{ wastelandCoins.toLocaleString() }} Wasteland Cola</span>
        </div>
        <div class="footer-note">
          Premium advisors will automatically renew unless deactivated. All bonuses apply settlement-wide.
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import colaIcon from '../images/Currency/Wasteland Cola.png'

const props = defineProps({
  advisor: Object,
  advisors: Array,
  wastelandCoins: { type: Number, default: 0 }
})

defineEmits(['close', 'activate'])

const selectedId = ref(props.advisor?.id || props.advisors?.[0]?.id || '')

const selectedAdvisor = computed(() =>
    props.advisors?.find(a => a.id === selectedId.value) || null
)
</script>

<style scoped>
.advisor-overlay{
  position: fixed;
  inset: 0;
  z-index: 999999;
  background: rgba(0, 4, 10, 0.82);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
  isolation: isolate;
}

.advisor-modal{
  position: relative;
  z-index: 1000000;
  width: min(960px, 95vw);
  max-height: 90vh;
  overflow-y: auto;
  background: linear-gradient(180deg, rgba(8,16,28,.98), rgba(4,10,18,.99));
  border: 1px solid var(--border-bright);
  box-shadow:
      0 0 40px rgba(0,0,0,.6),
      0 0 80px rgba(0,212,255,.04);
}

.modal-header{
  padding:20px 24px 12px;
  position:relative;
}

.modal-title{
  margin:0;
  font-family:var(--ff-title);
  font-size:22px;
  letter-spacing:4px;
  color:var(--cyan);
  text-shadow:0 0 14px rgba(0,212,255,.2);
}

.modal-subtitle{
  margin:6px 0 0;
  font-size:11px;
  letter-spacing:1.5px;
  color:var(--muted);
}

.modal-close{
  position:absolute;
  top:16px;
  right:16px;
  width:28px;
  height:28px;
  border:1px solid var(--border);
  background:var(--bg3);
  color:var(--muted);
  font-size:12px;
  cursor:pointer;
  display:flex;
  align-items:center;
  justify-content:center;
  transition:all .15s;
}

.modal-close:hover{
  border-color:#ff4030;
  color:#ff4030;
}

.modal-accent{
  height:1px;
  background:linear-gradient(90deg, transparent, var(--cyan), transparent);
  opacity:.4;
  margin:0 24px;
}

/* ═══ ADVISOR CARDS GRID ═══ */
.advisors-grid{
  display:grid;
  grid-template-columns:repeat(5, 1fr);
  gap:10px;
  padding:16px 24px;
}

.adv-card{
  border:1px solid var(--border);
  background:rgba(255,255,255,.02);
  padding:12px;
  display:flex;
  flex-direction:column;
  align-items:center;
  gap:8px;
  cursor:pointer;
  transition:all .15s;
  position:relative;
}

.adv-card:hover{
  border-color:var(--border-bright);
  background:rgba(0,212,255,.03);
}

.adv-card--selected{
  border-color:var(--cyan);
  background:rgba(0,212,255,.06);
  box-shadow:0 0 12px rgba(0,212,255,.1);
}

.adv-card--active{
  border-color:rgba(255,200,48,.4);
  background:rgba(255,200,48,.04);
}

.adv-card--active.adv-card--selected{
  border-color:#ffc830;
  box-shadow:0 0 12px rgba(255,200,48,.15);
}

.adv-portrait{
  width:72px;
  height:88px;
  border:1px solid var(--border);
  background:linear-gradient(180deg, rgba(0,212,255,.04), rgba(0,0,0,.3));
  display:flex;
  align-items:center;
  justify-content:center;
  position:relative;
  overflow:hidden;
}

.adv-portrait-img{
  width:100%;
  height:100%;
  object-fit:cover;
}

.adv-portrait-placeholder{
  display:flex;
  align-items:center;
  justify-content:center;
}

.adv-portrait-icon{
  font-size:28px;
  opacity:.4;
  filter:grayscale(1);
}

.adv-card--active .adv-portrait-icon,
.adv-card--selected .adv-portrait-icon{
  opacity:.7;
  filter:grayscale(0);
}

.adv-active-badge{
  position:absolute;
  bottom:0;
  left:0;
  right:0;
  background:rgba(255,200,48,.9);
  color:#000;
  font-size:7px;
  font-family:var(--ff-title);
  letter-spacing:1.5px;
  text-align:center;
  padding:2px;
  font-weight:700;
}

.adv-name{
  font-family:var(--ff-title);
  font-size:12px;
  letter-spacing:2px;
  color:var(--bright);
  text-align:center;
}

.adv-card--active .adv-name{
  color:#ffc830;
}

.adv-role{
  font-size:8px;
  letter-spacing:1px;
  color:var(--muted);
  text-align:center;
  text-transform:uppercase;
}

.adv-bonuses-mini{
  display:flex;
  flex-direction:column;
  gap:3px;
  width:100%;
}

.adv-bonus-mini{
  display:flex;
  align-items:center;
  gap:6px;
  font-size:10px;
}

.bonus-mini-icon{
  font-size:10px;
  width:16px;
  text-align:center;
}

.bonus-mini-value{
  color:var(--cyan);
  font-family:var(--ff-title);
  font-size:10px;
  letter-spacing:.5px;
}

.adv-card--active .bonus-mini-value{
  color:#ffc830;
}

.adv-duration{
  font-size:9px;
  color:var(--muted);
  display:flex;
  align-items:center;
  gap:4px;
}

.adv-duration-icon{
  font-size:9px;
}

.adv-status{
  font-size:9px;
  font-family:var(--ff-title);
  letter-spacing:1px;
}

.adv-status-active{
  color:#ffc830;
}

.adv-status-inactive{
  color:var(--muted);
}

.adv-activate-btn{
  width:100%;
  padding:6px 8px;
  border:1px solid var(--cyan);
  background:rgba(0,212,255,.08);
  color:var(--cyan);
  font-family:var(--ff-title);
  font-size:8px;
  letter-spacing:1.5px;
  cursor:pointer;
  transition:all .15s;
  margin-top:auto;
}

.adv-activate-btn:hover{
  background:rgba(0,212,255,.15);
  box-shadow:0 0 10px rgba(0,212,255,.15);
}

.adv-activate-btn--active{
  border-color:#ffc830;
  background:rgba(255,200,48,.08);
  color:#ffc830;
}

.adv-activate-btn--active:hover{
  background:rgba(255,200,48,.15);
  box-shadow:0 0 10px rgba(255,200,48,.15);
}

/* ═══ DETAIL PANEL ═══ */
.adv-detail{
  margin:0 24px 16px;
  border:1px solid var(--border-bright);
  background:linear-gradient(180deg, rgba(0,212,255,.03), rgba(0,0,0,.2));
}

.detail-accent{
  height:1px;
  background:linear-gradient(90deg, var(--cyan), transparent);
}

.detail-body{
  padding:16px 20px;
}

.detail-header{
  display:flex;
  align-items:center;
  gap:12px;
  margin-bottom:12px;
}

.detail-icon{
  font-size:28px;
}

.detail-name{
  font-family:var(--ff-title);
  font-size:16px;
  letter-spacing:2px;
  color:var(--bright);
}

.detail-role{
  font-size:9px;
  letter-spacing:1.5px;
  color:var(--cyan);
  text-transform:uppercase;
}

.detail-desc{
  margin:0 0 16px;
  font-size:12px;
  line-height:1.6;
  color:var(--muted);
}

.detail-bonuses-title{
  font-size:9px;
  letter-spacing:2px;
  color:var(--muted);
  margin-bottom:10px;
  text-transform:uppercase;
}

.detail-bonus-row{
  display:flex;
  justify-content:space-between;
  align-items:center;
  padding:10px 12px;
  border:1px solid var(--border);
  background:rgba(255,255,255,.02);
  margin-bottom:6px;
  transition:all .15s;
}

.detail-bonus-row:hover{
  border-color:var(--border-bright);
  background:rgba(0,212,255,.04);
}

.bonus-row-left{
  display:flex;
  align-items:center;
  gap:10px;
}

.bonus-row-icon{
  font-size:16px;
  width:24px;
  text-align:center;
}

.bonus-row-label{
  font-size:11px;
  color:var(--bright);
  font-weight:700;
}

.bonus-row-desc{
  font-size:10px;
  color:var(--muted);
  margin-top:2px;
}

.bonus-row-value{
  font-family:var(--ff-title);
  font-size:14px;
  color:var(--cyan);
  letter-spacing:1px;
  white-space:nowrap;
}

/* ═══ FOOTER ═══ */
.modal-footer{
  padding:12px 24px;
  border-top:1px solid var(--border);
  display:flex;
  flex-direction:column;
  align-items:center;
  gap:6px;
}

.footer-cola-balance{
  display:flex;
  align-items:center;
  gap:6px;
}

.footer-cola-icon{
  width:20px;
  height:20px;
  object-fit:contain;
}

.footer-cola-amt{
  font-family:var(--ff-title),sans-serif;
  font-size:12px;
  color:#ffa600;
  letter-spacing:1px;
}

.footer-note{
  font-size:9px;
  color:var(--muted);
  text-align:center;
  letter-spacing:.5px;
}

.advisor-cola-icon{
  width:14px;
  height:14px;
  object-fit:contain;
  vertical-align:middle;
  margin-bottom:1px;
}

/* ═══ RESPONSIVE ═══ */
@media(max-width:800px){
  .advisors-grid{
    grid-template-columns:repeat(3, 1fr);
  }
}

@media(max-width:500px){
  .advisors-grid{
    grid-template-columns:repeat(2, 1fr);
  }
}
</style>