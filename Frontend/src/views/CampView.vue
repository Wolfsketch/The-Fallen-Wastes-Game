<template>
  <div class="fade-in">
    <div class="page-header">
      <h2 class="page-title">CAMP</h2>
      <span class="page-subtitle">CAMP OVERVIEW</span>
    </div>
    <div class="accent-line" style="margin-bottom: 20px;" />

    <div class="camp-grid">
      <!-- Threat Assessment -->
      <div class="panel">
        <div class="panel-accent" />
        <div class="panel-body">
          <div class="panel-title">
            <span class="panel-dot" />
            THREAT ASSESSMENT
          </div>
          <div class="intel-list">
            <div v-for="(item, i) in intel" :key="i" class="intel-item">
              <span class="tag" :class="'tag--' + item.type">{{ item.tag }}</span>
              <span :style="{ color: item.color }">{{ item.msg }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Operational Status -->
      <div class="panel">
        <div class="panel-accent" />
        <div class="panel-body">
          <div class="panel-title">
            <span class="panel-dot" />
            OPERATIONAL STATUS
          </div>
          <div class="stats-grid">
            <div v-for="stat in stats" :key="stat.label" class="stat-item">
              <div class="stat-label">{{ stat.label }}</div>
              <div class="stat-value" :style="{ color: stat.color }">{{ stat.value }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Activity Log -->
    <div class="panel" style="margin-top: 14px;">
      <div class="panel-accent" />
      <div class="panel-body">
        <div class="panel-title">
          <span class="panel-dot" />
          OPERATIONS LOG
        </div>
        <div class="log-list">
          <div v-for="(entry, i) in log" :key="i" class="log-entry">
            <span class="log-time">{{ entry.time }}</span>
            <span class="tag" :class="'tag--' + entry.type">{{ entry.tag }}</span>
            <span class="log-msg" :style="{ color: entry.color }">{{ entry.msg }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  player: Object,
  settlement: Object
})

const intel = computed(() => [
  { msg: 'No hostile activity detected', color: 'var(--green)', tag: 'ALL CLEAR', type: 'green' },
  { msg: `Population: ${props.settlement?.population}/${props.settlement?.populationCapacity}`, color: 'var(--amber)', tag: 'INFO', type: 'amber' },
  { msg: `Water reserves: ${props.settlement?.water}`, color: props.settlement?.water < 200 ? 'var(--red)' : 'var(--text)', tag: props.settlement?.water < 200 ? 'WARNING' : 'OK', type: props.settlement?.water < 200 ? 'red' : 'cyan' },
  { msg: `${props.settlement?.availablePopulation} personnel unassigned`, color: 'var(--muted)', tag: 'INFO', type: 'muted' },
])

const stats = computed(() => [
  { label: 'Personnel',    value: `${props.settlement?.population}/${props.settlement?.populationCapacity}`, color: 'var(--cyan)' },
  { label: 'Idle',          value: props.settlement?.availablePopulation,  color: 'var(--amber)' },
  { label: 'Facilities',    value: '0', color: 'var(--cyan)' },
  { label: 'Defense Rating', value: '0', color: 'var(--green)' },
  { label: 'Strike Power',  value: '0', color: 'var(--cyan)' },
  { label: 'Reputation',    value: props.player?.score, color: 'var(--muted)' },
])

const log = computed(() => [
  { time: new Date(props.player?.createdAtUtc).toLocaleTimeString('en-GB'),
    msg: `Outpost "${props.settlement?.name}" established`, tag: 'NEW', type: 'cyan',
    color: 'var(--bright)' },
  { time: new Date(props.player?.createdAtUtc).toLocaleTimeString('en-GB'),
    msg: `Operator ${props.player?.username} authenticated`, tag: 'AUTH', type: 'muted',
    color: 'var(--text)' },
])
</script>

<style scoped>
.page-header {
  display: flex;
  align-items: baseline;
  gap: 12px;
  margin-bottom: 4px;
}
.page-title {
  font-family: var(--ff-title);
  font-size: 16px;
  color: var(--cyan);
  letter-spacing: 3px;
  font-weight: 700;
  text-shadow: 0 0 10px rgba(0, 212, 255, 0.12);
  text-transform: uppercase;
}
.page-subtitle {
  font-size: 8px;
  color: var(--cyan-dim);
  letter-spacing: 2px;
  font-family: var(--ff-title);
}

.camp-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 14px;
}

/* Panel */
.panel {
  background: var(--bg2);
  border: 1px solid var(--border);
  position: relative;
  overflow: hidden;
}
.panel-accent {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 1px;
  background: linear-gradient(90deg, var(--cyan), transparent);
  opacity: 0.4;
}
.panel-body {
  padding: 20px;
}
.panel-title {
  font-size: 9px;
  color: var(--cyan);
  text-transform: uppercase;
  letter-spacing: 3px;
  font-weight: 700;
  font-family: var(--ff-title);
  margin-bottom: 14px;
  display: flex;
  align-items: center;
  gap: 8px;
}
.panel-dot {
  width: 6px;
  height: 6px;
  background: var(--cyan);
  box-shadow: 0 0 6px var(--cyan);
  display: inline-block;
}

/* Intel */
.intel-list {
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.intel-item {
  font-size: 11px;
  line-height: 2.4;
  display: flex;
  align-items: center;
  gap: 8px;
}

/* Stats */
.stats-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px;
}
.stat-label {
  font-size: 8px;
  color: var(--muted);
  text-transform: uppercase;
  letter-spacing: 1.5px;
  font-weight: 700;
}
.stat-value {
  font-size: 18px;
  font-family: var(--ff-title);
  font-weight: 700;
  letter-spacing: 1px;
}

/* Log */
.log-list {
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.log-entry {
  font-size: 10px;
  line-height: 2.6;
  display: flex;
  align-items: center;
  gap: 10px;
}
.log-time {
  font-family: var(--ff-title);
  font-size: 9px;
  color: var(--cyan-dim);
  letter-spacing: 1px;
  min-width: 70px;
}
.log-msg {
  color: var(--text);
}
</style>
