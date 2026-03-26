<template>
  <div class="fade-in">
    <div class="page-header">
      <h2 class="page-title">{{ title }}</h2>
      <span class="page-subtitle">CLASSIFIED</span>
    </div>
    <div class="accent-line" style="margin-bottom: 20px;" />

    <div class="placeholder">
      <div class="placeholder-radar">
        <div class="placeholder-ring" />
        <div class="placeholder-core">◌</div>
      </div>
      <div class="placeholder-title">SECTOR LOCKED</div>
      <div class="placeholder-text">Awaiting clearance authorization.</div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  title: { type: String, default: 'UNKNOWN' },
  icon: { type: String, default: '◌' },
  player: Object,
  settlement: Object
})
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
}
.page-subtitle {
  font-size: 8px;
  color: var(--cyan-dim);
  letter-spacing: 2px;
  font-family: var(--ff-title);
}

.placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 80px 20px;
  text-align: center;
}

.placeholder-radar {
  position: relative;
  width: 60px;
  height: 60px;
  margin-bottom: 16px;
}
.placeholder-ring {
  position: absolute;
  inset: 0;
  border: 2px solid var(--border);
  border-radius: 50%;
  border-top-color: var(--cyan-dim);
  animation: radarSweep 4s linear infinite;
}
.placeholder-core {
  position: absolute;
  inset: 8px;
  background: var(--bg2);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  color: var(--cyan-dim);
}

.placeholder-title {
  font-family: var(--ff-title);
  font-size: 12px;
  color: var(--cyan-dim);
  letter-spacing: 3px;
}
.placeholder-text {
  font-size: 10px;
  color: var(--muted);
  margin-top: 6px;
}
</style>
