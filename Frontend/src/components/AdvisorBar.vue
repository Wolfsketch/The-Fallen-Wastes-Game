<template>
  <div class="advisor-bar">
    <div
        v-for="adv in advisors"
        :key="adv.id"
        class="advisor-slot"
        :class="{
        'advisor-slot--active': adv.active,
        'advisor-slot--locked': !adv.active
      }"
        @click="openAdvisor(adv)"
        :title="adv.name"
    >
      <img
          v-if="adv.image"
          :src="adv.image"
          :alt="adv.name"
          class="advisor-img"
      />
      <div v-else class="advisor-placeholder">
        <span class="advisor-placeholder-icon">{{ adv.icon }}</span>
      </div>

      <div v-if="adv.active" class="advisor-active-dot" />
      <div v-if="!adv.active" class="advisor-lock">🔒</div>
    </div>

    <Teleport to="body">
      <AdvisorModal
          v-if="selectedAdvisor"
          :advisor="selectedAdvisor"
          :advisors="advisors"
          :wasteland-coins="props.player?.wastelandCoins ?? 0"
          @close="selectedAdvisor = null"
          @activate="onActivate"
      />
    </Teleport>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { activateAdvisor } from '../services/api'
import AdvisorModal from './AdvisorModal.vue'

const props = defineProps({
  settlement: Object,
  player: Object
})

const emit = defineEmits(['refresh'])

const selectedAdvisor = ref(null)

function getRemainingDays(expiresUtc) {
  if (!expiresUtc) return 0
  const end = new Date(expiresUtc)
  const diffMs = end.getTime() - Date.now()
  if (diffMs <= 0) return 0
  return Math.ceil(diffMs / (1000 * 60 * 60 * 24))
}

const advisors = computed(() => {
  const apiAdvisors = props.player?.advisors || {}

  return [
    {
      id: 'commander',
      name: 'Commander',
      icon: '🏗️',
      image: null,
      role: 'Construction & Command',
      description: 'The Commander expands build and training capacity across your settlement.',
      active: apiAdvisors.commander?.active ?? false,
      daysRemaining: getRemainingDays(apiAdvisors.commander?.expiresUtc),
      cost: 100,
      duration: 14,
      bonuses: [
        { icon: '🏗️', label: 'Build Queue', value: '+5 slots', description: 'Expand construction queue from 2 to 7 simultaneous builds' },
        { icon: '🎖️', label: 'Training Queue', value: '+3 slots', description: 'Expand training queue from 2 to 5 simultaneous recruitments' },
        { icon: '⚡', label: 'Command Efficiency', value: 'Active', description: 'Central command oversight for operational expansion' },
      ]
    },
    {
      id: 'quartermaster',
      name: 'Quartermaster',
      icon: '📦',
      image: null,
      role: 'Resources & Trade',
      description: 'The Quartermaster optimizes resource gathering and logistics.',
      active: apiAdvisors.quartermaster?.active ?? false,
      daysRemaining: getRemainingDays(apiAdvisors.quartermaster?.expiresUtc),
      cost: 100,
      duration: 14,
      bonuses: [
        { icon: '💧', label: 'Production Boost', value: '+30%', description: 'All resource production increased by 30%' },
        { icon: '🤝', label: 'Trade Routes', value: 'Unlocked', description: 'Access to premium trade routes with better exchange rates' },
        { icon: '📊', label: 'Market Intel', value: 'Enabled', description: 'See resource prices across all settlements' },
      ]
    },
    {
      id: 'techpriest',
      name: 'Tech Priest',
      icon: '🔬',
      image: null,
      role: 'Research & Technology',
      description: 'The Tech Priest accelerates research and recovers lost knowledge.',
      active: apiAdvisors.techPriest?.active ?? false,
      daysRemaining: getRemainingDays(apiAdvisors.techPriest?.expiresUtc),
      cost: 100,
      duration: 14,
      bonuses: [
        { icon: '🔬', label: 'Research Speed', value: '+50%', description: 'All research times reduced by 50%' },
        { icon: '⚡', label: 'Tech Queue', value: '+2 slots', description: 'Research 2 additional technologies simultaneously' },
        { icon: '📡', label: 'Rare Tech', value: '+20%', description: 'Rare tech salvage rate increased by 20%' },
      ]
    },
    {
      id: 'warlord',
      name: 'Warlord',
      icon: '⚔️',
      image: null,
      role: 'Military & Combat',
      description: 'The Warlord improves your forces and battlefield performance.',
      active: apiAdvisors.warlord?.active ?? false,
      daysRemaining: getRemainingDays(apiAdvisors.warlord?.expiresUtc),
      cost: 100,
      duration: 14,
      bonuses: [
        { icon: '⚔️', label: 'Attack Power', value: '+20%', description: 'All unit attack power increased by 20%' },
        { icon: '🛡️', label: 'Defense Bonus', value: '+20%', description: 'Settlement defense increased by 20%' },
        { icon: '🎯', label: 'Combat Focus', value: 'Enabled', description: 'Improved battlefield command and pressure' },
      ]
    },
    {
      id: 'scoutmaster',
      name: 'Scout Master',
      icon: '🗺️',
      image: null,
      role: 'Exploration & Intelligence',
      description: 'The Scout Master improves movement and intelligence gathering.',
      active: apiAdvisors.scoutMaster?.active ?? false,
      daysRemaining: getRemainingDays(apiAdvisors.scoutMaster?.expiresUtc),
      cost: 100,
      duration: 14,
      bonuses: [
        { icon: '👁', label: 'Map Vision', value: '+50%', description: 'Expanded fog of war reveal radius' },
        { icon: '🏃', label: 'Movement Speed', value: '+20%', description: 'All unit movement speed increased by 20%' },
        { icon: '📋', label: 'Attack Planner', value: 'Unlocked', description: 'Plan and schedule attacks in advance' },
      ]
    }
  ]
})

function openAdvisor(adv) {
  selectedAdvisor.value = adv
}

async function onActivate(advisorId) {
  if (!props.player?.id) return

  const cost = 100
  if ((props.player.wastelandCoins ?? 0) < cost) {
    console.warn('Not enough Wasteland Cola to activate advisor.')
    return
  }

  try {
    await activateAdvisor(props.player.id, advisorId, 14)
    selectedAdvisor.value = null
    emit('refresh')
  } catch (err) {
    console.error('Failed to activate advisor:', err)
  }
}
</script>

<style scoped>
.advisor-bar{
  display:flex;
  gap:4px;
  align-items:center;
  flex-shrink:0;
}

.advisor-slot{
  width:32px;
  height:32px;
  border:1px solid var(--border);
  background:rgba(0,10,20,.6);
  cursor:pointer;
  position:relative;
  overflow:hidden;
  transition:all .15s;
  display:flex;
  align-items:center;
  justify-content:center;
}

.advisor-slot:hover{
  border-color:var(--cyan);
  box-shadow:0 0 8px rgba(0,212,255,.2);
  transform:translateY(-1px);
}

.advisor-slot--active{
  border-color:rgba(255,200,48,.5);
  box-shadow:0 0 6px rgba(255,200,48,.15);
  background:rgba(255,200,48,.06);
}

.advisor-slot--active:hover{
  border-color:#ffc830;
  box-shadow:0 0 10px rgba(255,200,48,.25);
}

.advisor-img{
  width:100%;
  height:100%;
  object-fit:cover;
}

.advisor-placeholder{
  display:flex;
  align-items:center;
  justify-content:center;
  width:100%;
  height:100%;
}

.advisor-placeholder-icon{
  font-size:14px;
  opacity:.5;
  filter:grayscale(1);
  transition:all .15s;
}

.advisor-slot:hover .advisor-placeholder-icon{
  opacity:.8;
  filter:grayscale(0);
}

.advisor-slot--active .advisor-placeholder-icon{
  opacity:1;
  filter:grayscale(0);
}

.advisor-active-dot{
  position:absolute;
  bottom:2px;
  right:2px;
  width:6px;
  height:6px;
  border-radius:50%;
  background:#ffc830;
  box-shadow:0 0 4px rgba(255,200,48,.6);
}

.advisor-lock{
  position:absolute;
  bottom:1px;
  right:1px;
  font-size:8px;
  opacity:.5;
}
</style>