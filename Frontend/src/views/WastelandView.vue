<template>
  <div class="wl-wrap">
    <div class="wl-toolbar">
      <div class="tb-left">
        <h2 class="tb-title">WASTELAND</h2>
        <span class="tb-sub">{{ worldName }}</span>
      </div>
      <div class="tb-center">[ {{ viewX }}, {{ viewY }} ] — {{ claimedCount }}/{{ slots.length }} claimed</div>
      <div class="tb-right">
        <div class="tb-movements" v-if="operations.length > 0 || activeOperations.length > 0 || incomingOps.length > 0">
          <button class="tb-btn tb-movements-btn"
                  @click="showMovementsPanel = !showMovementsPanel"
                  :class="{ 'tb-movements-btn--active': showMovementsPanel }">
            <span style="font-size:11px">⚡</span>
            <span style="font-size:11px;font-family:var(--ff-title);font-weight:700">
              {{ allMovements.length + incomingOps.length }}
            </span>
            <span v-if="incomingOps.length > 0" class="movements-incoming-badge">{{ incomingOps.length }}</span>
          </button>

          <div v-if="showMovementsPanel" class="movements-panel">
            <div class="movements-header">
              <span class="movements-title">ACTIVE OPERATIONS</span>
              <button class="movements-close" @click="showMovementsPanel = false">✕</button>
            </div>

            <div class="movements-list">
              <!-- INCOMING ATTACKS -->
              <template v-if="incomingOps.length > 0">
                <div class="movements-section-title movements-section-title--incoming">
                  <span>⚠ INCOMING ATTACK</span>
                  <button class="overlay-mute-btn" @click.stop="toggleOverlayMute" :title="overlayMuted ? 'Enable alert overlay' : 'Dismiss alert overlay'">
                    {{ overlayMuted ? '🔔' : '🔕' }}
                  </button>
                </div>
                <div v-for="op in incomingOps" :key="'inc-'+op.id" class="movement-row movement-row--incoming" @click="focusOperation(op)">
                  <div class="movement-icon">💀</div>
                  <div class="movement-info">
                    <div class="movement-target" style="color:#ff5050">{{ op.playerName ?? 'Unknown' }}</div>
                    <div class="movement-units">{{ op.unitSummary ?? '?' }}</div>
                  </div>
                  <div class="movement-right">
                    <div class="movement-status" style="color:#ff4040">{{ op.phase === 'returning' ? '↩' : '→' }}</div>
                    <div class="movement-time" style="color:#ff5050">
                      {{ op.phase === 'returning' && op.returnsAtUtc
                        ? formatTravelTime(Math.max(0, Math.ceil((new Date(op.returnsAtUtc).getTime() - Date.now()) / 1000)))
                        : op.arrivesAtUtc
                          ? formatTravelTime(Math.max(0, Math.ceil((new Date(op.arrivesAtUtc).getTime() - Date.now()) / 1000)))
                          : '—' }}
                    </div>
                  </div>
                </div>
              </template>

              <!-- EN ROUTE -->
              <template v-if="allMovements.filter(o => o.phase === 'outbound').length > 0">
                <div class="movements-section-title">EN ROUTE →</div>
                <div v-for="op in allMovements.filter(o => o.phase === 'outbound')"
                     :key="op.id" class="movement-row" @click="focusOperation(op)">
                  <div class="movement-icon">
                    {{ op.operationType?.includes('scout') ? '👁' : op.operationType === 'reinforce_poi' ? '⛊' : '⚔' }}
                  </div>
                  <div class="movement-info">
                    <div class="movement-target">{{ op.poiLabel ?? op.poiId ?? 'Settlement' }}</div>
                    <div class="movement-units">{{ op.operationType?.includes('scout') ? 'Scout' : (op.unitSummary ?? '—') }}</div>
                  </div>
                  <div class="movement-right">
                    <div class="movement-status status--outbound">→</div>
                    <div class="movement-time">
                      {{ formatTravelTime(Math.max(0, Math.ceil((new Date(op.arrivesAtUtc).getTime() - Date.now()) / 1000))) }}
                    </div>
                  </div>
                </div>
              </template>

              <!-- PRESENT (troops only, no scouts) -->
              <template v-if="allMovements.filter(o => o.phase === 'arrived' && !o.operationType?.includes('scout')).length > 0">
                <div class="movements-section-title">PRESENT ●</div>
                <div v-for="op in allMovements.filter(o => o.phase === 'arrived' && !o.operationType?.includes('scout'))"
                     :key="op.id" class="movement-row" @click="focusOperation(op)">
                  <div class="movement-icon">{{ op.operationType === 'reinforce_poi' ? '⛊' : '⚔' }}</div>
                  <div class="movement-info">
                    <div class="movement-target">{{ op.poiLabel ?? op.poiId ?? 'Settlement' }}</div>
                    <div class="movement-units">{{ op.unitSummary ?? '—' }}</div>
                  </div>
                  <div class="movement-right">
                    <div class="movement-status status--arrived">●</div>
                    <div class="movement-time" style="color:var(--green)">ACTIVE</div>
                  </div>
                </div>
              </template>

              <!-- RETURNING -->
              <template v-if="allMovements.filter(o => o.phase === 'returning').length > 0">
                <div class="movements-section-title">RETURNING ←</div>
                <div v-for="op in allMovements.filter(o => o.phase === 'returning')"
                     :key="op.id" class="movement-row" @click="focusOperation(op)">
                  <div class="movement-icon">{{ op.operationType?.includes('scout') ? '👁' : '⚔' }}</div>
                  <div class="movement-info">
                    <div class="movement-target">{{ op.poiLabel ?? op.poiId ?? 'Settlement' }}</div>
                    <div class="movement-units">{{ op.unitSummary ?? '—' }}</div>
                  </div>
                  <div class="movement-right">
                    <div class="movement-status status--returning">←</div>
                    <div class="movement-time" style="color:#ffc840">
                      {{ op.returnsAtUtc
                        ? formatTravelTime(Math.max(0, Math.ceil((new Date(op.returnsAtUtc).getTime() - Date.now()) / 1000)))
                        : '—' }}
                    </div>
                  </div>
                </div>
              </template>

              <div v-if="allMovements.length === 0 && incomingOps.length === 0" class="movements-empty">
                Geen actieve bewegingen
              </div>
            </div>
          </div>
        </div>

        <button v-if="scoutMasterActive"
                class="tb-btn tb-planner-btn"
                @click="showAttackPlanner = true"
                title="Attack Planner">
          📋 PLANNER
        </button>

        <div class="tb-movements">
          <button class="tb-btn tb-notes-btn" @click="showNotes = !showNotes" title="Notities">
            📋
          </button>

          <div v-if="showNotes" class="notes-panel">
            <div class="movements-header">
              <span class="movements-title">NOTITIES</span>
              <button class="movements-close" @click="showNotes = false">✕</button>
            </div>
            <textarea class="notes-textarea"
                      v-model="notesContent"
                      placeholder="Schrijf hier je notities..."
                      @input="saveNotes"></textarea>
          </div>
        </div>

        <button class="tb-btn" @click="centerOnPlayer">⊕</button>
        <button class="tb-btn" @click="zoomIn" :disabled="zoom >= 3">+</button>
        <span class="tb-zoom">{{ Math.round(zoom * 100) }}%</span>
        <button class="tb-btn" @click="zoomOut" :disabled="zoom <= minZoom">−</button>
      </div>
    </div>
    <div class="map-area" ref="mapCont" @mousedown="startPan" @mousemove="onPan" @mouseup="endPan" @mouseleave="endPan" @wheel.prevent="onWheel" @click="onClick">
      <canvas ref="cvs" class="map-cvs" />
      <div class="marker-layer" :class="{ 'marker-layer--panning': isPanning }" :style="{ transform: `translate(${panX}px,${panY}px) scale(${zoom})` }">
        <div v-for="s in slots" :key="s.id" class="sm" :class="[`sm--${s.status}`, { sel: sel?.id === s.id }]" :style="{ left: (s.x - 12) + 'px', top: (s.y - 12) + 'px' }" @mouseenter="onMarkerEnter(s, 'slot', $event)" @mouseleave="onMarkerLeave" @mousemove="onMarkerMove">
          <div class="sm-ring">
            <svg v-if="s.status !== 'empty'" viewBox="0 0 24 24" class="sm-house"><path d="M12 4L3 12h2.5v7h4.5v-4h4v4h4.5v-7H21L12 4z" fill="currentColor" /></svg>
            <svg v-else viewBox="0 0 24 24" class="sm-house sm-house--empty"><path d="M12 4L3 12h2.5v7h4.5v-4h4v4h4.5v-7H21L12 4z" fill="currentColor" /></svg>
          </div>
        </div>
        <div v-for="p in visiblePois" :key="p.id" class="poi" :class="[`poi--${p.type}`, { 'poi--active': arrivedPoiIds.has(p.id), 'poi--relocating': poiStates[p.id]?.isRelocating }]" :style="{ left: p.x + 'px', top: p.y + 'px' }" @click.stop="selectPoi(p)" @mouseenter="onMarkerEnter(p, 'poi', $event)" @mouseleave="onMarkerLeave" @mousemove="onMarkerMove">
          <div v-if="arrivedPoiIds.has(p.id)" class="poi-raid-glow" />
          <svg v-if="p.icon === 'factory'" viewBox="0 0 24 24" class="poi-svg"><path d="M2 20h20V12l-5 3V12l-5 3V12l-5 3V8l-5 4v8z" fill="currentColor"/><rect x="17" y="3" width="3" height="9" rx=".5" fill="currentColor" opacity=".8"/><rect x="13" y="5" width="2.5" height="7" rx=".5" fill="currentColor" opacity=".6"/></svg>
          <svg v-else-if="p.icon === 'gear'" viewBox="0 0 24 24" class="poi-svg"><path d="M12 15.5A3.5 3.5 0 018.5 12 3.5 3.5 0 0112 8.5a3.5 3.5 0 013.5 3.5 3.5 3.5 0 01-3.5 3.5m7.43-2.53c.04-.32.07-.64.07-.97s-.03-.66-.07-1l2.11-1.63a.5.5 0 00.12-.64l-2-3.46c-.12-.22-.39-.3-.61-.22l-2.49 1c-.52-.4-1.08-.73-1.69-.98l-.38-2.65A.49.49 0 0014 2h-4c-.25 0-.46.18-.49.42l-.38 2.65c-.61.25-1.17.59-1.69.98l-2.49-1c-.23-.09-.49 0-.61.22l-2 3.46c-.13.22-.07.49.12.64L4.57 11c-.04.34-.07.67-.07 1s.03.65.07.97l-2.11 1.66a.5.5 0 00-.12.64l2 3.46c.12.22.39.3.61.22l2.49-1.01c.52.4 1.08.73 1.69.98l.38 2.65c.03.24.24.42.49.42h4c.25 0 .46-.18.49-.42l.38-2.65c.61-.25 1.17-.58 1.69-.98l2.49 1.01c.22.08.49 0 .61-.22l2-3.46a.5.5 0 00-.12-.64l-2.11-1.66z" fill="currentColor"/></svg>
          <svg v-else-if="p.icon === 'ruins'" viewBox="0 0 24 24" class="poi-svg"><path d="M3 21h2V10L3 12v9zm4 0h2V6l2-2v17h2V8l-2-4v17zm7 0h2V10l2-4v15h2V5l-2-2v18z" fill="currentColor"/></svg>
          <svg v-else-if="p.icon === 'radiation'" viewBox="0 0 24 24" class="poi-svg"><circle cx="12" cy="12" r="2.5" fill="currentColor"/><path d="M12 2C8.1 2 5 5.1 5 9l7 3 7-3c0-3.9-3.1-7-7-7z" fill="currentColor" opacity=".6"/><path d="M5 9a8 8 0 002.7 9.2L12 12 5 9z" fill="currentColor" opacity=".6"/><path d="M16.3 18.2A8 8 0 0019 9l-7 3 4.3 6.2z" fill="currentColor" opacity=".6"/></svg>
          <svg v-else-if="p.icon === 'bunker'" viewBox="0 0 24 24" class="poi-svg"><path d="M4 18h16v3H4z" fill="currentColor"/><path d="M6 14h12a2 2 0 012 2v2H4v-2a2 2 0 012-2z" fill="currentColor" opacity=".8"/><path d="M8 14v-3a4 4 0 018 0v3" fill="none" stroke="currentColor" stroke-width="1.5"/></svg>
          <svg v-else-if="p.icon === 'tower'" viewBox="0 0 24 24" class="poi-svg"><path d="M11 21h2V10l3-1.5V6.5L13 8V3h-2v5L8 6.5v2L11 10v11z" fill="currentColor"/><circle cx="12" cy="3" r="1.5" fill="currentColor"/><path d="M8.5 4L6 2m9.5 2L18 2" stroke="currentColor" stroke-width=".8" opacity=".7"/><path d="M7.5 6L5 4.5m11.5 1.5L19 4.5" stroke="currentColor" stroke-width=".6" opacity=".4"/></svg>
          <svg v-else-if="p.icon === 'wreck'" viewBox="0 0 24 24" class="poi-svg"><path d="M3 15h18l-2-6H5l-2 6z" fill="currentColor" opacity=".6"/><circle cx="7" cy="17" r="2" fill="currentColor"/><circle cx="17" cy="17" r="2" fill="currentColor"/><path d="M6 9l2-3h8l2 3" fill="none" stroke="currentColor" stroke-width="1" opacity=".5"/></svg>
          <svg v-else-if="p.icon === 'water'" viewBox="0 0 24 24" class="poi-svg"><path d="M12 2C7 8 4 11.5 4 15a8 8 0 0016 0c0-3.5-3-7-8-13z" fill="currentColor" opacity=".6"/><path d="M9 16a3 3 0 003 3" fill="none" stroke="currentColor" stroke-width="1" opacity=".3"/></svg>
          <svg v-else-if="p.icon === 'cache'" viewBox="0 0 24 24" class="poi-svg"><rect x="4" y="10" width="16" height="10" rx="1" fill="currentColor" opacity=".6"/><path d="M4 10l2-4h12l2 4" fill="currentColor" opacity=".8"/><rect x="10" y="13" width="4" height="3" fill="currentColor" opacity=".3"/></svg>
          <svg v-else-if="p.icon === 'crater'" viewBox="0 0 24 24" class="poi-svg"><ellipse cx="12" cy="15" rx="9" ry="5" fill="currentColor" opacity=".4"/><ellipse cx="12" cy="15" rx="4.5" ry="2.5" fill="currentColor" opacity=".25"/><path d="M8 8l1 4m7-4l-1 4m-3-7v5" stroke="currentColor" stroke-width=".8" opacity=".5"/></svg>
        </div>
        <template v-if="zoom > 0.8 && !isPanning">
          <div v-for="p in visiblePois" :key="'pl' + p.id" class="poi-label" :style="{ left: p.x + 'px', top: (p.y + 16) + 'px' }">{{ p.label }}</div>
        </template>
        <template v-if="!isPanning">
          <div v-for="s in labeledSlots" :key="'l' + s.id" class="sl" :class="`sl--${s.status}`" :style="{ left: s.x + 'px', top: (s.y + 16) + 'px' }">
            <span class="sl-n">{{ s.name }}</span>
            <span v-if="s.owner" class="sl-o">[{{ s.owner }}]</span>
          </div>
        </template>
        <!-- Stippen: ENKEL eigen (cyaan) + alliantie (paars). Neutraal/vijand onzichtbaar. -->
        <div v-for="op in activeOperationMarkers" :key="'op-' + op.id" class="op-dot" :class="{ 'op-dot--own': op.isOwn, 'op-dot--alliance': !op.isOwn && op.isAlliance, 'op-dot--incoming': op.isIncoming }" :style="{ left: op.x + 'px', top: op.y + 'px' }" />
        <!-- Bewegingstimers op de kaart boven de dots -->
        <div v-for="op in activeOperationMarkers" :key="'timer-' + op.id"
             class="op-timer"
             :style="{ left: op.x + 'px', top: (op.y - 24) + 'px' }">
          {{ op.phase === 'returning' && op.returnsAtUtc
            ? formatTravelTime(Math.max(0, Math.ceil((new Date(op.returnsAtUtc).getTime() - Date.now()) / 1000)))
            : formatTravelTime(Math.max(0, Math.ceil((new Date(op.arrivesAtUtc ?? 0).getTime() - Date.now()) / 1000)))
          }}
        </div>
      </div>
      <div class="minimap">
        <div class="mm-t">RADAR — {{ worldName }}</div>
        <canvas ref="mmCvs" width="170" height="115" class="mm-c"/>
        <div class="mm-l">
          <span class="ml"><span class="md md--y"/>You</span>
          <span class="ml"><span class="md md--a"/>Alliance</span>
          <span class="ml"><span class="md md--n"/>Neutral</span>
          <span class="ml"><span class="md md--e"/>Enemy</span>
          <span class="ml"><span class="md md--o"/>Open</span>
        </div>
      </div>
      <div v-if="hoverItem" class="map-tooltip" :style="{ left: hoverPos.x + 14 + 'px', top: hoverPos.y - 10 + 'px' }">
        <div class="tt-header">
          <span class="tt-icon" :class="`tt-icon--${hoverItem.status || hoverItem.type}`">{{ hoverItem._type === 'poi' ? '☢' : statusIcon(hoverItem.status) }}</span>
          <div>
            <div class="tt-name">{{ hoverItem.name || hoverItem.label }}</div>
            <div class="tt-sub">
              <template v-if="hoverItem._type === 'poi'">{{ hoverItem.type?.toUpperCase() }} — Landmark</template>
              <template v-else-if="hoverItem.status === 'empty'">UNCLAIMED SECTOR</template>
              <template v-else-if="hoverItem.status === 'yours'">YOUR SETTLEMENT</template>
              <template v-else>{{ hoverItem.status?.toUpperCase() }} — {{ hoverItem.owner || 'Unknown' }}</template>
            </div>
          </div>
        </div>
        <div class="tt-stats" v-if="hoverItem._type === 'slot' && hoverItem.status !== 'empty'"><span class="tt-stat">⚔ {{ hoverItem.defense || 0 }}</span><span class="tt-stat">★ {{ hoverItem.score || 0 }}</span></div>
        <div class="tt-stats" v-if="hoverItem._type === 'slot' && hoverItem.status === 'empty'"><span class="tt-stat-hint">Click to claim</span></div>
        <div class="tt-stats" v-if="hoverItem._type === 'poi'"><span class="tt-stat-hint">{{ arrivedPoiIds.has(hoverItem.id) ? 'Activity detected' : 'Click to inspect' }}</span></div>
      </div>
      <div v-if="loading" class="ld-ov">
        <div class="ld-box">
          <div class="ld-phase">{{ loadingPhase }}</div>
          <div class="ld-t">{{ loadingText }}</div>
          <div class="ld-bar"><div class="ld-bar-fill" :style="{ width: loadingPercent + '%' }" /></div>
          <div class="ld-meta">{{ loadingPercent }}%<span v-if="loadingPhase === 'PREPARING TERRAIN'"> — {{ loadingLoaded }}/{{ loadingTotal }} chunks</span></div>
        </div>
      </div>
    </div>

    <!-- Settlement panel -->
    <transition name="ps">
      <div v-if="sel" class="sp">
        <div class="sp-ln"/>
        <div class="sp-b">
          <button class="sp-x" @click="sel = null">✕</button>
          <div class="sp-h">
            <div class="sp-i" :class="`si--${sel.status}`">{{ statusIcon(sel.status) }}</div>
            <div><div class="sp-n">{{ sel.name }}</div><div class="sp-s"><span class="sp-t" :class="`st--${sel.status}`">{{ sel.status.toUpperCase() }}</span>{{ sel.owner ? 'Owner: ' + sel.owner : 'Unclaimed' }}</div></div>
          </div>
          <template v-if="sel.status === 'yours'">
            <div class="ss-w"><div class="ss"><div class="ssl">SCORE</div><div class="ssv">{{ player?.score }}</div></div><div class="ss"><div class="ssl">ATTACK</div><div class="ssv">{{ player?.attackScore ?? 0 }}</div></div><div class="ss"><div class="ssl">DEFENSE</div><div class="ssv">{{ player?.defenseScore ?? 0 }}</div></div></div>
          </template>
          <template v-if="sel.status === 'ally'">
            <div class="ss-w"><div class="ss"><div class="ssl">OWNER</div><div class="ssv ssv--a">{{ sel.owner }}</div></div><div class="ss"><div class="ssl">ALLIANCE</div><div class="ssv ssv--a">{{ sel.alliance || '—' }}</div></div><div class="ss"><div class="ssl">SCORE</div><div class="ssv">{{ sel.score }}</div></div></div>
          </template>
          <template v-if="sel.status === 'neutral' || sel.status === 'enemy'">
            <div class="ss-w"><div class="ss"><div class="ssl">OWNER</div><div class="ssv" :class="ownerColor(sel.status)">{{ sel.owner }}</div></div><div class="ss"><div class="ssl">SCORE</div><div class="ssv">{{ sel.score }}</div></div><div class="ss"><div class="ssl">DEF</div><div class="ssv">{{ sel.defense }}</div></div></div>
            <div class="sp-actions">
              <div class="sa-w"><button class="sa sa--a" @click="openAttackModal('settlement')">⚔ Attack</button><button class="sa sa--s" @click="openScoutModal('settlement')">👁 Scout</button></div>
              <div class="sa-w sa-w--diplo">
                <button class="sa sa--m" @click="openMailToSelected">✉ Mail</button>
                <button v-if="sel.status !== 'enemy'" class="sa sa--e" @click="markAs('Enemy')">⚔ Enemy</button>
                <button v-if="sel.status !== 'neutral'" class="sa sa--n" @click="markNeutral">● Neutral</button>
              </div>
            </div>
          </template>
          <template v-if="sel.status === 'empty'">
            <div class="sp-d">Unclaimed location. Cost: 500 Scrap, 300 Food, 200 Fuel</div>
            <div class="sa-w"><button class="sa sa--c">🏗 Claim</button></div>
          </template>
        </div>
      </div>
    </transition>

    <!-- POI panel -->
    <transition name="ps">
      <div v-if="selPoi && !sel" class="sp">
        <div class="sp-ln"/>
        <div class="sp-b">
          <button class="sp-x" @click="selPoi = null">✕</button>
          <div class="sp-h">
            <div class="sp-i si--poi" :class="{ 'si--active': arrivedPoiIds.has(selPoi.id) }">☢</div>
            <div>
              <div class="sp-n">{{ selPoi.label }}</div>
              <div class="sp-s">
                <span class="sp-t st--poi">{{ selPoi.type.toUpperCase() }}</span>
                <span v-if="arrivedPoiIds.has(selPoi.id)" class="poi-active-badge">● ACTIVITY DETECTED</span>
                <span v-else>Wasteland landmark</span>
              </div>
            </div>
          </div>
          <div class="ss-w">
            <div class="ss"><div class="ssl">NPC TIER</div><div class="ssv ssv--unknown">
              <span>{{ selectedPoiScoutResult?.tier ? 'TIER ' + selectedPoiScoutResult.tier : '???' }}</span>
            </div></div>
            <div class="ss"><div class="ssl">LOOT</div><div class="ssv ssv--unknown">
              <span>{{ selectedPoiScoutResult?.lootItems?.length
                ? selectedPoiScoutResult.lootItems.length + ' items'
                : '???' }}</span>
            </div></div>
            <div class="ss"><div class="ssl">STATUS</div><div class="ssv" :class="arrivedPoiIds.has(selPoi.id) ? 'ssv--active' : 'ssv--available'">{{ arrivedPoiIds.has(selPoi.id) ? 'ACTIVE' : 'AVAILABLE' }}</div></div>
          </div>
          <div v-if="allianceAtPoi(selPoi.id).length > 0" class="poi-alliance-intel">
            <div class="pai-title">◆ ALLIANCE PRESENCE</div>
            <div v-for="op in allianceAtPoi(selPoi.id)" :key="op.id" class="pai-row">
              <span class="pai-name">{{ op.playerName }}</span>
              <span class="pai-units">{{ op.unitSummary }}</span>
            </div>
          </div>
          <div v-if="poiStates[selPoi?.id]?.isRelocating"
               style="padding:6px 12px;background:rgba(255,144,64,.1);border:1px solid rgba(255,144,64,.3);color:#ff9040;font-size:11px;font-family:var(--ff-title);letter-spacing:1px;margin-bottom:8px">
            ⚠ POI RELOCATING — Troops returning automatically
          </div>
          <div class="sp-actions">
            <div class="sa-w" v-if="!ownArrivedAtSelectedPoi">
              <button class="sa sa--s" @click="openScoutModal('poi')">👁 Scout <span class="sa-cost">{{ poiScoutCost(selPoi) }} RT</span></button>
              <button class="sa sa--raid" @click="openAttackModal('poi')" :disabled="poiStates[selPoi?.id]?.isCleared">⚔ Raid</button>
              <button v-if="selectedPoiHasNonOwnArrival" class="sa sa--reinforce" @click="openReinforceModal">⛊ Reinforce</button>
            </div>
            <div class="sa-w" v-else>
              <button class="sa sa--s" @click="openScoutModal('poi')">👁 Scout <span class="sa-cost">{{ poiScoutCost(selPoi) }} RT</span></button>
              <button class="sa sa--reinforce" @click="openAttackModal('poi')">⛊ Reinforce</button>
              <button class="sa sa--recall" @click="recallTroops(ownArrivedAtSelectedPoi)">↩ Return Troops</button>
              <div v-if="ownArrivedAtSelectedPoi.lootItemsEarned > 0"
                   style="font-size:10px;color:var(--green);font-family:var(--ff-title);padding:4px 8px;">
                🧲 {{ ownArrivedAtSelectedPoi.lootItemsEarned }} loot collected
              </div>
            </div>
          </div>
          <div v-if="poiStates[selPoi.id]?.isCleared" class="poi-cleared-badge">
            ⏳ CLEARED — Respawning in {{ formatTravelTime(poiStates[selPoi.id]?.respawnInSeconds) }}
          </div>
        </div>
      </div>
    </transition>

    <!-- Scout modal -->
    <transition name="modal-fade">
      <div v-if="scoutModal.open" class="modal-backdrop" @click.self="scoutModal.open = false">
        <div class="modal-box">
          <div class="modal-header"><span class="modal-title">DEPLOY SCOUT</span><button class="modal-close" @click="scoutModal.open = false">✕</button></div>
          <div class="modal-body">
            <div class="modal-target">Target: <strong>{{ scoutModal.type === 'poi' ? selPoi?.label : sel?.name }}</strong></div>
            <template v-if="scoutModal.type === 'poi'">
              <div class="modal-info">Scout to reveal NPC tier, loot quality and active operators. Fixed cost: <strong>{{ poiScoutCost(selPoi) }} RT</strong></div>
              <div class="modal-fixed-cost"><span class="modal-label">SCOUT COST</span><span class="modal-value">{{ poiScoutCost(selPoi) }} RareTech</span></div>
              <div class="modal-vault-info">
                <span class="modal-label">YOUR VAULT BALANCE</span>
                <span class="modal-value" :class="vaultRareTech >= scoutModal.amount ? 'vault-ok' : 'vault-low'">
                  {{ vaultRareTech }} RT
                  {{ raidVaultLevel < 1 ? '— No Relic Vault built' : '' }}
                </span>
              </div>
              <div class="modal-vault-info">
                <span class="modal-label">TRAVEL TIME</span>
                <span class="modal-value">{{ formatTravelTime(estimatedTravelSeconds) }}</span>
              </div>
            </template>
            <template v-else>
              <div class="modal-info">Send more RT than enemy Vault to succeed. Both sides lose RT. Min: <strong>100 RT</strong>.</div>
              <div class="modal-row">
                <label class="modal-label">RARETECH TO SEND</label>
                <div class="modal-input-row">
                  <button class="qty-btn" @click="scoutModal.amount = Math.max(100, scoutModal.amount - 50)">−</button>
                  <input class="modal-input" type="number" min="100" v-model.number="scoutModal.amount" />
                  <button class="qty-btn" @click="scoutModal.amount += 50">+</button>
                </div>
                <div class="modal-hint">Available: {{ currentRareTech }} RT</div>
              </div>
              <div class="modal-vault-info">
                <span class="modal-label">YOUR VAULT BALANCE</span>
                <span class="modal-value" :class="vaultRareTech >= scoutModal.amount ? 'vault-ok' : 'vault-low'">
                  {{ vaultRareTech }} RT
                  {{ raidVaultLevel < 1 ? '— No Relic Vault built' : '' }}
                </span>
              </div>
              <div class="modal-vault-info">
                <span class="modal-label">TRAVEL TIME</span>
                <span class="modal-value">{{ formatTravelTime(estimatedTravelSeconds) }}</span>
              </div>
            </template>
            <div v-if="scoutError" class="modal-error-msg">{{ scoutError }}</div>
          </div>
          <div class="modal-footer"><button class="modal-cancel" @click="scoutModal.open = false">CANCEL</button><button class="modal-confirm" @click="confirmScout" :disabled="vaultRareTech < scoutModal.amount || raidVaultLevel < 1" :title="raidVaultLevel < 1 ? 'Build a Relic Vault first' : vaultRareTech < scoutModal.amount ? 'Not enough RT in your Relic Vault' : ''">SEND SCOUT</button></div>
        </div>
      </div>
    </transition>

    <!-- Scout report popup -->
    <div v-if="scoutReport" class="modal-backdrop" @click.self="scoutReport = null">
      <div class="wl-modal">
        <div class="modal-header">
          <span class="modal-title">SCOUT REPORT — {{ scoutReport.poiLabel ?? scoutReport.poiId }}</span>
          <button class="modal-close" @click="scoutReport = null">✕</button>
        </div>
        <div class="modal-body" style="padding:16px">
          <div style="font-size:9px;color:var(--cyan);letter-spacing:2px;margin-bottom:12px">
            DEFENDERS — TIER {{ scoutReport.tier }}
          </div>
          <div v-for="(qty, name) in scoutReport.npcUnits" :key="name"
               style="display:flex;justify-content:space-between;padding:6px 0;border-bottom:1px solid var(--border);font-size:11px">
            <span style="color:var(--text)">{{ name }}</span>
            <span style="color:var(--red);font-family:var(--ff-title);font-weight:700">× {{ qty }}</span>
          </div>
          <div v-if="scoutReport.lootItems?.length"
               style="font-size:9px;color:var(--cyan);letter-spacing:2px;margin:14px 0 8px">
            SALVAGEABLE LOOT
          </div>
          <div v-for="item in (scoutReport.lootItems ?? [])" :key="item"
               style="padding:5px 0;border-bottom:1px solid var(--border);font-size:11px;color:var(--green)">
            🧬 {{ item }}
          </div>
        </div>
        <div class="modal-footer">
          <button class="modal-confirm" @click="scoutReport = null">CLOSE</button>
        </div>
      </div>
    </div>

    <!-- Attack / Reinforce modal -->
    <transition name="modal-fade">
      <div v-if="attackModal.open" class="modal-backdrop" @click.self="attackModal.open = false">
        <div class="modal-box modal-box--wide">
          <div class="modal-header">
            <span class="modal-title">{{ attackModal.isReinforce ? 'REINFORCE' : 'DEPLOY FORCES' }}</span>
            <button class="modal-close" @click="attackModal.open = false">✕</button>
          </div>
          <div class="modal-body">
            <div class="modal-target">Target: <strong>{{ attackModal.type === 'poi' ? selPoi?.label : sel?.name }}</strong></div>
            <div v-if="attackModal.type === 'poi' && !attackModal.isReinforce" class="modal-row">
              <label class="modal-label">OPERATION MODE</label>
              <div class="modal-op-grid">
                <button v-for="mode in raidModes" :key="mode.key" class="modal-op-btn" :class="{ 'modal-op-btn--active': attackModal.raidMode === mode.key }" @click="attackModal.raidMode = mode.key">
                  <span class="modal-op-name">{{ mode.name }}</span>
                  <span class="modal-op-time">{{ mode.duration }}</span>
                  <span class="modal-op-desc">{{ mode.desc }}</span>
                </button>
              </div>
            </div>
            <div class="modal-row">
              <label class="modal-label">SELECT UNITS</label>
              <div v-if="ownedUnits.length === 0" class="modal-hint">No units available in inventory.</div>
              <div v-else class="modal-unit-list">
                <div v-for="u in ownedUnits" :key="u.name" class="modal-unit-row">
                  <img v-if="getUnitImage(u.name)" :src="getUnitImage(u.name)" class="modal-unit-img" :alt="u.name" />
                  <span class="modal-unit-name">{{ u.name }}</span>
                  <span class="modal-unit-avail">{{ u.qty }} avail</span>
                  <div class="modal-input-row">
                    <button class="qty-btn" @click="attackModal.selectedUnits[u.name] = Math.max(0, (attackModal.selectedUnits[u.name] || 0) - 1)">−</button>
                    <input class="modal-input modal-input--sm" type="number" min="0" :max="u.qty"
                           :value="attackModal.selectedUnits[u.name] || 0"
                           @input="attackModal.selectedUnits[u.name] = Math.min(u.qty, Math.max(0, +$event.target.value))" />
                    <button class="qty-btn" @click="attackModal.selectedUnits[u.name] = Math.min(u.qty, (attackModal.selectedUnits[u.name] || 0) + 1)">+</button>
                  </div>
                </div>
              </div>
              <div class="modal-hint">Total selected: {{ Object.values(attackModal.selectedUnits).reduce((s, v) => s + (v || 0), 0) }}</div>
              <div class="modal-vault-info" style="margin-top:8px">
                <span class="modal-label">TRAVEL TIME</span>
                <span class="modal-value">{{ formatTravelTime(estimatedTravelSeconds) }}</span>
              </div>
            </div>
            <div v-if="attackModal.error" class="modal-error-msg">{{ attackModal.error }}</div>
          </div>
          <div class="modal-footer">
            <button class="modal-cancel" @click="attackModal.open = false">CANCEL</button>
            <button class="modal-confirm" @click="confirmAttack">SEND FORCES</button>
          </div>
        </div>
      </div>
    </transition>

    <!-- Combat result popup -->
    <div v-if="combatResult" class="modal-backdrop" @click.self="combatResult = null">
      <div class="wl-modal">
        <div class="modal-header">
          <span class="modal-title">⚔️ FORCES ARRIVED — {{ combatResult.poiLabel ?? combatResult.poiId }}</span>
          <button class="modal-close" @click="combatResult = null">✕</button>
        </div>
        <div class="modal-body" style="padding:16px">
          <div v-if="combatResult.attackerWins !== null"
               :style="combatResult.attackerWins
                 ? 'color:var(--green);font-family:var(--ff-title);font-size:13px;font-weight:700;margin-bottom:12px'
                 : 'color:var(--red);font-family:var(--ff-title);font-size:13px;font-weight:700;margin-bottom:12px'">
            {{ combatResult.attackerWins ? '✓ VICTORY' : '✗ DEFEATED' }}
          </div>

          <div style="font-size:9px;color:var(--cyan);letter-spacing:2px;margin-bottom:8px">YOUR FORCES</div>
          <div v-for="(qty, name) in combatResult.attackerSurvived" :key="'s'+name"
               style="display:flex;justify-content:space-between;padding:4px 0;font-size:11px">
            <span style="color:var(--text)">{{ name }}</span>
            <span style="color:var(--green);font-family:var(--ff-title)">{{ qty }} survived</span>
          </div>
          <div v-for="(qty, name) in combatResult.attackerLosses" :key="'l'+name"
               style="display:flex;justify-content:space-between;padding:4px 0;font-size:11px">
            <span style="color:var(--muted)">{{ name }}</span>
            <span style="color:var(--red);font-family:var(--ff-title)">-{{ qty }} lost</span>
          </div>

          <div v-if="Object.keys(combatResult.npcLosses).length"
               style="font-size:9px;color:var(--cyan);letter-spacing:2px;margin:12px 0 8px">
            NPC CASUALTIES
          </div>
          <div v-for="(qty, name) in combatResult.npcLosses" :key="'n'+name"
               style="display:flex;justify-content:space-between;padding:4px 0;font-size:11px">
            <span style="color:var(--muted)">{{ name }}</span>
            <span style="color:var(--orange, #ff9040);font-family:var(--ff-title)">-{{ qty }}</span>
          </div>

          <div v-if="!combatResult.attackerWins && !combatResult.attackerLosses"
               style="font-size:11px;color:var(--text)">
            {{ combatResult.units }} have arrived.
          </div>
        </div>
        <div class="modal-footer">
          <button class="modal-confirm" @click="combatResult = null">CLOSE</button>
        </div>
      </div>
    </div>

    <!-- Attack Planner Modal -->
    <div v-if="showAttackPlanner" class="modal-backdrop" @click.self="showAttackPlanner = false">
      <div class="wl-modal wl-modal--wide">
        <div class="modal-header">
          <span class="modal-title">⚔ ATTACK PLANNER</span>
          <button class="modal-close" @click="showAttackPlanner = false">✕</button>
        </div>
        <div class="modal-body" style="padding:20px;display:flex;flex-direction:column;gap:16px">

          <div style="font-size:11px;color:var(--muted)">
            Set an arrival time. Your troops will depart automatically
            so they arrive exactly on schedule.
          </div>

          <!-- Target selection -->
          <div>
            <div class="modal-label" style="margin-bottom:6px">TARGET</div>
            <div style="font-size:12px;color:var(--cyan)">
              {{ plannerTarget
                ? (plannerTarget.label ?? plannerTarget.name ?? plannerTarget.id)
                : 'Select a POI or Settlement on the map first' }}
            </div>
          </div>

          <!-- Arrival time -->
          <div>
            <div class="modal-label" style="margin-bottom:6px">ARRIVE AT</div>
            <input type="datetime-local" v-model="plannerArrivalTime"
                   style="background:var(--bg3);border:1px solid var(--border);
                          color:var(--text);padding:8px 12px;font-family:var(--ff);
                          font-size:11px;width:100%" />
          </div>

          <!-- Departure calculation -->
          <div v-if="plannerDepartureTime"
               style="padding:12px;background:var(--bg3);border:1px solid var(--border)">
            <div class="modal-label" style="margin-bottom:4px">DEPARTURE</div>
            <div style="font-size:13px;color:var(--cyan);font-family:var(--ff-title);font-weight:700">
              {{ plannerDepartureTime }}
            </div>
            <div style="font-size:10px;color:var(--muted);margin-top:4px">
              Travel time: {{ formatTravelTime(plannerTravelSeconds) }}
            </div>
          </div>

          <!-- Unit selection -->
          <div v-if="plannerTarget">
            <div class="modal-label" style="margin-bottom:8px">SELECT UNITS</div>
            <div v-if="ownedUnits.length === 0" style="font-size:11px;color:var(--muted)">
              No units available
            </div>
            <div v-for="u in ownedUnits" :key="u.name"
                 style="display:flex;align-items:center;gap:10px;padding:6px 0;
                        border-bottom:1px solid var(--border)">
              <span style="flex:1;font-size:11px;color:var(--text)">{{ u.name }}</span>
              <span style="font-size:10px;color:var(--muted)">{{ u.qty }} avail</span>
              <button class="qty-btn" @click="plannerUnits[u.name] = Math.max(0, (plannerUnits[u.name]||0) - 1)">−</button>
              <span style="width:30px;text-align:center;font-family:var(--ff-title);font-size:11px">
                {{ plannerUnits[u.name] || 0 }}
              </span>
              <button class="qty-btn" @click="plannerUnits[u.name] = Math.min(u.qty, (plannerUnits[u.name]||0) + 1)">+</button>
            </div>
          </div>

          <div v-if="plannerError" style="color:var(--red);font-size:11px">{{ plannerError }}</div>

          <div style="display:flex;gap:10px;justify-content:flex-end">
            <button class="modal-cancel" @click="showAttackPlanner = false">CANCEL</button>
            <button class="modal-confirm"
                    :disabled="!plannerTarget || !plannerArrivalTime || plannerDepartureInPast"
                    @click="confirmPlannedAttack">
              SCHEDULE ATTACK
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, nextTick, watch } from 'vue'
import { createChunkedWorldRenderer } from '../composables/useChunkedMapRenderer.js'
import { generateSlotsFromElevation, generatePOIsFromElevation } from '../composables/useWorldPlacement.js'
import { getWorldSettlements, setPlayerRelation, removePlayerRelation, getSettlementOperations, scoutPoi, scoutSettlement, attackPoi, attackSettlement, reinforcePoi, getPoiStates, recallOperation } from '../services/api.js'
import { useRouter } from 'vue-router'
import islandImage from '../images/Island/IslandV2.png'
const unitImages = import.meta.glob('../images/*.png', { eager: true, import: 'default' })
function getUnitImage(name) {
  return unitImages[`../images/${name}.png`] ?? null
}

const props = defineProps({ player: Object, settlement: Object, refreshSettlement: Function })
const player = computed(() => props.player)
const settlement = computed(() => props.settlement)

const WW = 5400
const WH = 4200
const worldSeed = parseInt(sessionStorage.getItem('worldSeed') || '42')
const worldName = sessionStorage.getItem('worldName') || 'World ' + worldSeed

const mapCont = ref(null)
const cvs = ref(null)
const mmCvs = ref(null)
const zoom = ref(0.32)
const panX = ref(0)
const panY = ref(0)
const isPanning = ref(false)
const panSt = ref({ x: 0, y: 0 })
const panStP = ref({ x: 0, y: 0 })
const moved = ref(false)
const sel = ref(null)
const selPoi = ref(null)
const cW = ref(1200)
const cH = ref(700)
const loading = ref(true)
const loadingPhase = ref('INITIALIZING WORLD')
const loadingText = ref('Booting wasteland systems...')
const loadingPercent = ref(0)
const loadingLoaded = ref(0)
const loadingTotal = ref(0)
const slots = ref([])
const pois = ref([])
const hoverItem = ref(null)
const hoverPos = ref({ x: 0, y: 0 })

const frameTime = ref(Date.now())
let frameInterval = null
const operations = ref([])

// Glow only when troops are PRESENT at a POI (phase === 'arrived')
const arrivedPoiIds = computed(() =>
  new Set(
    operations.value
      .filter(op =>
        op.phase === 'arrived' &&
        op.poiId &&
        !String(op.id).startsWith('local-') &&
        (op.operationType === 'raid_poi' || op.operationType === 'reinforce_poi')
      )
      .map(op => op.poiId)
  )
)

const ownArrivedAtSelectedPoi = computed(() => {
  if (!selPoi.value) return null
  return operations.value.find(op =>
    op.isOwn &&
    op.phase === 'arrived' &&
    op.poiId === selPoi.value.id &&
    op.operationType === 'raid_poi'
  ) ?? null
})

const selectedPoiScoutResult = computed(() => {
  if (!selPoi.value) return null
  const scoutOp = scoutResultOps.value.find(op =>
    op.poiId === selPoi.value.id
  )
  if (!scoutOp) return null
  try { return JSON.parse(scoutOp.resultJson) }
  catch { return null }
})

// Stippen enkel voor eigen + alliantie — neutrale/vijand volledig verborgen
let _lastReturnLogS = 0
const activeOperationMarkers = computed(() => {
  const now = frameTime.value
  const outbound = operations.value
    .filter(op => op.phase === 'outbound' && (op.isOwn || op.isAlliance))
    .map(op => {
      const progress = Math.min(1, (now - op.startTime) / op.travelDuration)
      return {
        ...op,
        x: op.originX + (op.destX - op.originX) * progress,
        y: op.originY + (op.destY - op.originY) * progress,
      }
    })

  // Debug: log all ops that have returning phase, regardless of filters
  const _s = Math.floor(Date.now() / 1000)
  if (_s !== _lastReturnLogS) {
    _lastReturnLogS = _s
    const allReturning = operations.value.filter(op => op.phase === 'returning')
    if (allReturning.length > 0) {
      allReturning.forEach(op => {
        console.log(`[RETURN-CHECK] id=${op.id} isOwn=${op.isOwn} returnsAtUtc=${op.returnsAtUtc} travelDuration=${op.travelDuration} originX=${op.originX} destX=${op.destX}`)
      })
    }
  }

  const returning = operations.value
    .filter(op => op.phase === 'returning' && op.isOwn && op.returnsAtUtc)
    .map(op => {
      const returnStartMs = new Date(op.returnsAtUtc).getTime() - op.travelDuration
      const elapsed = now - returnStartMs
      const progress = op.travelDuration > 0
        ? Math.min(1, Math.max(0, elapsed / op.travelDuration))
        : 1
      return {
        ...op,
        x: op.destX + (op.originX - op.destX) * progress,
        y: op.destY + (op.originY - op.destY) * progress,
      }
    })

  const isEnemyOp = op => !op.isOwn && !op.isAlliance && op.targetSettlementId === props.settlement?.id

  const incoming = operations.value
    .filter(op => op.phase === 'outbound' && isEnemyOp(op))
    .map(op => {
      const progress = Math.min(1, (now - op.startTime) / op.travelDuration)
      return {
        ...op,
        x: op.originX + (op.destX - op.originX) * progress,
        y: op.originY + (op.destY - op.originY) * progress,
        isIncoming: true,
      }
    })

  // Enemy returning after attacking: red dot moving back to their settlement
  const enemyReturning = operations.value
    .filter(op => op.phase === 'returning' && isEnemyOp(op) && op.returnsAtUtc)
    .map(op => {
      const returnStartMs = new Date(op.returnsAtUtc).getTime() - op.travelDuration
      const elapsed = now - returnStartMs
      const progress = op.travelDuration > 0
        ? Math.min(1, Math.max(0, elapsed / op.travelDuration))
        : 1
      return {
        ...op,
        x: op.destX + (op.originX - op.destX) * progress,
        y: op.destY + (op.originY - op.destY) * progress,
        isIncoming: true,
      }
    })

  return [...outbound, ...returning, ...incoming, ...enemyReturning]
})

const incomingOps = computed(() =>
  operations.value.filter(op =>
    !op.isOwn && !op.isAlliance &&
    (op.phase === 'outbound' || op.phase === 'returning') &&
    op.targetSettlementId === props.settlement?.id
  )
)

const isUnderAttack = computed(() =>
  incomingOps.value.some(op => op.phase === 'outbound')
)

const overlayMuted = ref(false)

watch(isUnderAttack, val => {
  // When no longer under attack, reset mute so the overlay shows again next time
  if (!val) overlayMuted.value = false
  window.dispatchEvent(new CustomEvent('under-attack-changed', { detail: { underAttack: val && !overlayMuted.value } }))
}, { immediate: true })

function toggleOverlayMute() {
  overlayMuted.value = !overlayMuted.value
  window.dispatchEvent(new CustomEvent('under-attack-changed', { detail: { underAttack: isUnderAttack.value && !overlayMuted.value } }))
}

// Alliantieleden aanwezig in POI — zichtbaar in panel met naam + troepen
function allianceAtPoi(poiId) {
  return operations.value.filter(op => op.poiId === poiId && op.phase === 'arrived' && op.isAlliance)
}

const scoutModal = ref({ open: false, type: 'poi', amount: 100 })
const raidModes = [
  { key: 'quick',      name: 'Quick Grab',     duration: '5 min',  desc: 'Low loot, fast return' },
  { key: 'sweep',      name: 'Field Sweep',    duration: '15 min', desc: 'Balanced risk & reward' },
  { key: 'extraction', name: 'Extraction Run', duration: '60 min', desc: 'High loot, contested risk' },
  { key: 'deep',       name: 'Deep Salvage',   duration: '4 hrs',  desc: 'Best drops, rare salvage items' },
]

const scoutError = ref('')
const scoutReport = ref(null)
const shownReportIds = new Set()

function reportIdsKey() { return `shownReportIds_${props.settlement?.id ?? 'unknown'}` }
function loadShownReportIds() {
  try {
    const stored = JSON.parse(localStorage.getItem(reportIdsKey()) ?? '[]')
    stored.forEach(id => shownReportIds.add(String(id)))
  } catch {}
}

function markReportShown(id) {
  shownReportIds.add(String(id))
  try {
    localStorage.setItem(reportIdsKey(),
      JSON.stringify([...shownReportIds].slice(-500)))
  } catch {}
}

const poiStates = ref({})
const poiGenerationSeeds = ref({})
const combatResult = ref(null)
const attackModal = ref({
  open: false, type: 'poi', raidMode: 'quick',
  selectedUnits: {}, error: '', isReinforce: false
})
const ownedUnits = computed(() => {
  const inv = props.settlement?.unitInventory ?? {}
  return Object.entries(inv)
    .filter(([, qty]) => qty > 0)
    .map(([name, qty]) => ({ name, qty }))
})
const selectedPoiHasNonOwnArrival = computed(() => {
  if (!selPoi.value) return false
  return operations.value.some(op =>
    op.poiId === selPoi.value.id && op.phase === 'arrived' && !op.isOwn
  )
})

const currentRareTech = computed(() =>
    props.settlement?.rareTech ?? props.settlement?.resources?.rareTech ?? 0
)
const vaultRareTech = computed(() => props.settlement?.vaultRareTech ?? 0)
const raidVaultLevel = computed(() => props.settlement?.raidVaultLevel ?? 0)

// Unit movement speeds (higher = faster). Reference speed = 12 (Rifleman).
const UNIT_SPEEDS = {
  'Scavenger': 14, 'Raider': 18, 'Outpost Defender': 10,
  'Rifleman': 12, 'Shock Fighter': 12, 'Sniper': 11,
  'Flame Trooper': 10, 'Power Trooper': 9,
  'Assault Bike': 22, 'Rust Buggy': 24, 'War Rig': 13,
  'Interceptor': 17, 'Siege Carrier': 7,
  'Wall Breaker': 7, 'EMP Launcher': 8, 'Laser Squad': 10,
  'Rad Bomber': 6, 'Drone Swarm': 19,
  'Convoy': 5, 'Settler Convoy': 5
}
const REFERENCE_SPEED = 12

const estimatedTravelSeconds = computed(() => {
  const own = slots.value.find(s => s.status === 'yours')
  if (!own) return null
  let destX = 0, destY = 0
  if (scoutModal.value.open) {
    if (scoutModal.value.type === 'poi' && selPoi.value) {
      destX = selPoi.value.x; destY = selPoi.value.y
    } else if (sel.value) {
      destX = sel.value.x; destY = sel.value.y
    }
  } else if (attackModal.value.open) {
    if (attackModal.value.type === 'poi' && selPoi.value) {
      destX = selPoi.value.x; destY = selPoi.value.y
    } else if (sel.value) {
      destX = sel.value.x; destY = sel.value.y
    }
  }
  const dist = Math.sqrt(Math.pow(destX - own.x, 2) + Math.pow(destY - own.y, 2))
  let speedFactor = 1
  if (attackModal.value.open) {
    const selected = Object.entries(attackModal.value.selectedUnits)
      .filter(([, qty]) => (qty || 0) > 0)
      .map(([name]) => UNIT_SPEEDS[name] ?? REFERENCE_SPEED)
    if (selected.length === 0) return null  // no units selected → show —
    const slowest = Math.min(...selected)
    speedFactor = slowest / REFERENCE_SPEED
  }
  return Math.max(30, Math.round(dist * 0.3 / speedFactor))
})

function formatTravelTime(seconds) {
  if (!seconds) return '—'
  if (seconds < 60) return `${seconds}s`
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m ${seconds % 60}s`
  return `${Math.floor(seconds / 3600)}h ${Math.floor((seconds % 3600) / 60)}m`
}

function poiScoutCost(poi) {
  if (!poi) return 5
  if (['bunker', 'cache', 'crater'].includes(poi.type)) return 30
  if (['factory', 'ruins', 'tower'].includes(poi.type)) return 15
  return 5
}

function openScoutModal(type) {
  scoutError.value = ''
  scoutModal.value = { open: true, type, amount: type === 'poi' ? poiScoutCost(selPoi.value) : 100 }
}

function openRaidModal() {
  raidModal.value = { open: true, mode: 'quick' }
}

async function confirmScout() {
  const modal = scoutModal.value
  if (!props.settlement?.id) return
  scoutError.value = ''
  // Capture travel time BEFORE modal closes (estimatedTravelSeconds depends on modal.open)
  const _travelSec = estimatedTravelSeconds.value ?? 60
  try {
    if (modal.type === 'poi' && selPoi.value) {
      await scoutPoi(
        props.settlement.id,
        selPoi.value.id,
        modal.amount,
        selPoi.value.label ?? selPoi.value.id,
        _travelSec
      )
    } else if (modal.type === 'settlement' && sel.value?.id) {
      await scoutSettlement(props.settlement.id, sel.value.id, modal.amount, _travelSec)
    }
    scoutModal.value.open = false

    // Inject local operation immediately so line appears without waiting for poll
    const ownSlot = slots.value.find(s => s.status === 'yours')
    const targetPoi = selPoi.value
    if (ownSlot && targetPoi) {
      const travelMs = _travelSec * 1000
      operations.value.push({
        id: 'local-scout-' + Date.now(),
        phase: 'outbound',
        operationType: 'scout_poi',
        originX: ownSlot.x, originY: ownSlot.y,
        destX: targetPoi.x, destY: targetPoi.y,
        startTime: Date.now(),
        travelDuration: travelMs,
        arrivesAtUtc: new Date(Date.now() + travelMs).toISOString(),
        isOwn: true,
        isAlliance: false,
        poiId: targetPoi.id,
        targetSettlementId: null,
        playerName: props.player?.username ?? 'You',
        unitSummary: null,
        isReinforcement: false,
        resultJson: null,
        lootItemsEarned: 0,
        lootIntervalSeconds: 300,
        poiLabel: targetPoi.label ?? targetPoi.id,
      })
      // Persist local ops to survive tab navigation
      try {
        const _key = localOpsKey()
        const stored = JSON.parse(sessionStorage.getItem(_key) ?? '[]')
        stored.push({
          ...operations.value[operations.value.length - 1],
          _savedAt: Date.now()
        })
        const alive = stored.filter(o => Date.now() - o._savedAt < o.travelDuration)
        sessionStorage.setItem(_key, JSON.stringify(alive))
      } catch {}
      renderMap()
    }

    if (props.refreshSettlement) await props.refreshSettlement()
  } catch (err) {
    const msg = err?.response?.data
    scoutError.value = typeof msg === 'string' ? msg
      : (msg?.message ?? 'Scout failed. Check your Relic Vault level and RareTech balance.')
  }
}

function confirmRaid() { /* replaced — see confirmAttack */ }

function localOpsKey() { return `localOps_${props.settlement?.id ?? 'unknown'}` }

async function loadOperations() {
  if (!props.settlement?.id) return
  loadShownReportIds()
  const _key = localOpsKey()
  try {
    // Aggressive cleanup: drop stale ops older than 2x their travel time
    try {
      const stored = JSON.parse(sessionStorage.getItem(_key) ?? '[]')
      const alive = stored.filter(o => {
        const age = Date.now() - o.startTime
        return age < o.travelDuration * 2
      })
      if (alive.length !== stored.length) {
        sessionStorage.setItem(_key, JSON.stringify(alive))
      }
    } catch {}

    // Restore local ops from sessionStorage if still traveling
    try {
      const stored = JSON.parse(sessionStorage.getItem(_key) ?? '[]')
      const alive = stored.filter(o => {
        const elapsed = Date.now() - o._savedAt
        return elapsed < o.travelDuration
      })
      alive.forEach(o => {
        if (!operations.value.some(existing => existing.id === o.id)) {
          operations.value.push(o)
        }
      })
      sessionStorage.setItem(_key, JSON.stringify(alive))
    } catch {}

    const data = await getSettlementOperations(props.settlement.id)
    // Normalize: .NET may serialize DateTime without Z if Kind=Unspecified → JS parses as local time
    const utcStr = s => !s ? null : (s.endsWith('Z') || s.includes('+') ? s : s + 'Z')
    const newOps = (data || []).map(op => ({
      id: op.id,
      phase: (() => {
        // If backend says outbound but arrival time is already past,
        // treat as arrived on the client side immediately.
        // This prevents stale old operations (from previous sessions)
        // from briefly showing as outbound with 900%+ progress.
        if (op.phase === 'outbound' && op.arrivesAtUtc) {
          const arrivesAt = new Date(utcStr(op.arrivesAtUtc)).getTime()
          if (Date.now() >= arrivesAt) return 'arrived'
        }
        return op.phase
      })(),
      operationType: op.operationType,
      originX: 0, originY: 0,
      destX: 0, destY: 0,
      startTime: op.startedAtUtc
        ? new Date(utcStr(op.startedAtUtc)).getTime()
        : op.arrivesAtUtc
          ? new Date(utcStr(op.arrivesAtUtc)).getTime() - ((op.travelSeconds ?? op.TravelSeconds ?? 60)) * 1000
          : Date.now(),
      travelDuration: ((op.travelSeconds ?? op.TravelSeconds ?? 60)) * 1000,
      arrivesAtUtc: utcStr(op.arrivesAtUtc),
      isOwn: op.isOwn,
      isAlliance: false,
      attackerSettlementId: op.attackerSettlementId ?? null,
      poiId: op.targetPoiId ?? null,
      targetSettlementId: op.targetSettlementId ?? null,
      playerName: op.originSettlementName ?? 'Unknown',
      unitSummary: op.sentUnits
        ? Object.entries(op.sentUnits).map(([k, v]) => `${v}x ${k}`).join(', ')
        : null,
      isReinforcement: op.operationType === 'reinforce_poi',
      resultJson: op.resultJson ?? null,
      lootItemsEarned: op.lootItemsEarned ?? op.LootItemsEarned ?? 0,
      lootIntervalSeconds: op.lootIntervalSeconds ?? op.LootIntervalSeconds ?? 300,
      poiLabel: op.targetPoiLabel ?? op.TargetPoiLabel ?? op.targetPoiId ?? null,
      returnsAtUtc: utcStr(op.returnsAtUtc ?? op.ReturnsAtUtc ?? null),
    }))

    // Keep currently-animating outbound ops not yet in the new data
    const existingOutbound = operations.value.filter(op => {
      if (op.phase !== 'outbound') return false
      // If this op is now in the new backend data, use the backend version
      if (newOps.some(n => n.id === op.id)) return false
      // For local ops: keep alive until travel time has elapsed
      if (String(op.id).startsWith('local-')) {
        const elapsed = Date.now() - op.startTime
        return elapsed < op.travelDuration
      }
      return true
    })

    operations.value = [...newOps, ...existingOutbound]
    operations.value.forEach(op => {
      const ownSlot = slots.value.find(s => s.id === props.settlement?.id || s.status === 'yours')
      if (ownSlot && op.isOwn) { op.originX = ownSlot.x; op.originY = ownSlot.y }
      if (!op.isOwn && op.attackerSettlementId) {
        const attackerSlot = slots.value.find(s => s.id === op.attackerSettlementId)
        if (attackerSlot) { op.originX = attackerSlot.x; op.originY = attackerSlot.y }
      }
      if (op.poiId) {
        const poi = pois.value.find(p => p.id === op.poiId)
        if (poi) { op.destX = poi.x; op.destY = poi.y }
      } else if (op.targetSettlementId) {
        const slot = slots.value.find(s => s.id === op.targetSettlementId)
        if (slot) { op.destX = slot.x; op.destY = slot.y }
      }
    })



    operations.value.forEach(op => {
      if (op.operationType === 'scout_poi' && op.phase === 'arrived' && op.resultJson
          && !shownReportIds.has(String(op.id))
          && op.arrivesAtUtc && (Date.now() - new Date(op.arrivesAtUtc).getTime()) < 5 * 60 * 1000) {
        markReportShown(op.id)
        try {
          const result = JSON.parse(op.resultJson)
          if (result?.npcUnits) {
            scoutReport.value = {
              poiId: op.poiId,
              poiLabel: op.poiLabel ?? op.poiId,
              npcUnits: result.npcUnits,
              tier: result.tier ?? 1,
              lootItems: result.lootItems ?? []
            }
          }
        } catch {}
      }
      if (op.operationType === 'raid_poi' && op.phase === 'arrived'
          && !shownReportIds.has(String(op.id))
          && op.arrivesAtUtc && (Date.now() - new Date(op.arrivesAtUtc).getTime()) < 5 * 60 * 1000) {
        markReportShown(op.id)
        let battleData = null
        try { battleData = op.resultJson ? JSON.parse(op.resultJson) : null } catch {}
        combatResult.value = {
          poiId: op.poiId,
          poiLabel: op.poiLabel ?? op.poiId,
          units: op.unitSummary ?? 'Unknown forces',
          status: 'ARRIVED',
          attackerWins: battleData?.attackerWins ?? null,
          attackerLosses: battleData?.attackerLosses ?? {},
          attackerSurvived: battleData?.attackerSurvived ?? {},
          npcLosses: battleData?.npcLosses ?? {},
          remainingNpcUnits: battleData?.remainingNpcUnits ?? {}
        }
      }
    })
    renderMap()
  } catch (e) {
    console.error('Failed to load operations:', e)
  }
}

async function loadPoiStates() {
  try {
    const data = await getPoiStates()
    poiStates.value = {}
    ;(data || []).forEach(s => { poiStates.value[s.poiId] = s })

    // Rebuild generation seeds as a fresh object so the watch detects changes
    const newSeeds = {}
    ;(data || []).forEach(s => { newSeeds[s.poiId] = s.generationSeed ?? 0 })
    poiGenerationSeeds.value = newSeeds

    // Auto-recall own troops if their POI is now relocating
    const ownArrivedOps = operations.value.filter(op =>
      op.isOwn && op.phase === 'arrived' && op.poiId
    )
    for (const op of ownArrivedOps) {
      const poiState = poiStates.value[op.poiId]
      if (poiState?.isRelocating && !String(op.id).startsWith('local-')) {
        try {
          await recallOperation(props.settlement.id, op.id)
          console.log('[POI RELOCATE] Auto-recalled troops from', op.poiId)
        } catch (e) {
          console.warn('[POI RELOCATE] Auto-recall failed:', e)
        }
      }
    }
  } catch {}
}

watch(poiGenerationSeeds, (newSeeds, oldSeeds) => {
  let changed = false
  for (const [poiId, seed] of Object.entries(newSeeds)) {
    if ((oldSeeds?.[poiId] ?? 0) !== seed) { changed = true; break }
  }
  if (!changed) return
  regeneratePoisWithSeeds(newSeeds)
})

function regeneratePoisWithSeeds(seeds) {
  if (!worldRenderer) return
  const combinedSeed = Object.values(seeds).reduce((acc, s) => acc + s, 0)
  const newPois = generatePOIsFromElevation({
    worldWidth: WW, worldHeight: WH,
    worldSeed: worldSeed + combinedSeed,
    getElevation: worldRenderer.getElevation,
    waterLevel: worldRenderer.waterLevel,
    slots: slots.value
  })
  pois.value = newPois.map((newPoi, i) => {
    const oldPoi = pois.value[i]
    if (oldPoi) newPoi.discovered = oldPoi.discovered
    return newPoi
  })
  renderMap(); renderMini()
}

function openAttackModal(type) {
  attackModal.value = { open: true, type, raidMode: 'quick', selectedUnits: {}, error: '', isReinforce: false }
}

function openReinforceModal() {
  attackModal.value = { open: true, type: 'poi', raidMode: 'quick', selectedUnits: {}, error: '', isReinforce: true }
}

async function confirmAttack() {
  const modal = attackModal.value
  const units = Object.fromEntries(
    Object.entries(modal.selectedUnits).filter(([, v]) => v > 0)
  )
  if (Object.keys(units).length === 0) {
    modal.error = 'Select at least 1 unit to send.'
    return
  }
  // Capture travel time BEFORE modal closes (estimatedTravelSeconds depends on modal.open)
  const _travelSec = estimatedTravelSeconds.value ?? 300
  try {
    if (modal.type === 'poi' && selPoi.value) {
      if (modal.isReinforce) {
        await reinforcePoi(props.settlement.id, selPoi.value.id, units, _travelSec)
      } else {
        await attackPoi(props.settlement.id, selPoi.value.id, units, modal.raidMode, _travelSec)
      }
    } else if (modal.type === 'settlement' && sel.value?.id) {
      await attackSettlement(props.settlement.id, sel.value.id, units, _travelSec)
    }
    const _wasReinforce = modal.isReinforce ?? false
    const _type = modal.type
    const _raidMode = modal.raidMode
    attackModal.value.open = false

    const ownSlotA = slots.value.find(s => s.status === 'yours')
    const targetPoiA = _type === 'poi' ? selPoi.value : null
    const targetSlotA = _type === 'settlement' ? sel.value : null
    if (ownSlotA && (targetPoiA || targetSlotA)) {
      const travelMs = _travelSec * 1000
      const destX = targetPoiA ? targetPoiA.x : (targetSlotA?.x ?? 0)
      const destY = targetPoiA ? targetPoiA.y : (targetSlotA?.y ?? 0)
      operations.value.push({
        id: 'local-attack-' + Date.now(),
        phase: 'outbound',
        operationType: _type === 'settlement' ? 'attack_settlement' : (_wasReinforce ? 'reinforce_poi' : 'raid_poi'),
        originX: ownSlotA.x, originY: ownSlotA.y,
        destX, destY,
        startTime: Date.now(),
        travelDuration: travelMs,
        arrivesAtUtc: new Date(Date.now() + travelMs).toISOString(),
        isOwn: true,
        isAlliance: false,
        poiId: targetPoiA?.id ?? null,
        targetSettlementId: targetSlotA?.id ?? null,
        playerName: props.player?.username ?? 'You',
        unitSummary: Object.entries(modal.selectedUnits ?? {})
          .filter(([, v]) => v > 0)
          .map(([k, v]) => `${v}x ${k}`).join(', '),
        isReinforcement: _wasReinforce,
        resultJson: null,
        lootItemsEarned: 0,
        lootIntervalSeconds: 300,
        poiLabel: targetPoiA?.label ?? null,
      })
      // Persist local ops to survive tab navigation
      try {
        const _key = localOpsKey()
        const stored = JSON.parse(sessionStorage.getItem(_key) ?? '[]')
        stored.push({
          ...operations.value[operations.value.length - 1],
          _savedAt: Date.now()
        })
        const alive = stored.filter(o => Date.now() - o._savedAt < o.travelDuration)
        sessionStorage.setItem(_key, JSON.stringify(alive))
      } catch {}
      renderMap()
    }

    if (props.refreshSettlement) await props.refreshSettlement()
  } catch (err) {
    modal.error = err?.response?.data ?? 'Attack failed.'
  }
}

async function recallTroops(op) {
  if (!op || !props.settlement?.id) return
  try {
    const result = await recallOperation(props.settlement.id, op.id)
    const toastMsg = result.message ?? 'Troops returning.'
    console.log('[RECALL]', toastMsg)
    await loadOperations()
    if (props.refreshSettlement) await props.refreshSettlement()
  } catch (err) {
    const msg = err?.response?.data
    console.error('[RECALL FAILED]', msg)
  }
}

const viewX = computed(() => Math.round((-panX.value / zoom.value + cW.value / 2 / zoom.value) / 15))
const viewY = computed(() => Math.round((-panY.value / zoom.value + cH.value / 2 / zoom.value) / 15))
const minZoom = computed(() => Math.max(cW.value / WW, cH.value / WH))
const claimedCount = computed(() => slots.value.filter(s => s.status !== 'empty').length)
const labeledSlots = computed(() => slots.value.filter(s => s.status !== 'empty' || zoom.value > 1.3))
const visiblePois = computed(() => pois.value.filter(p => p.discovered))

const showMovementsPanel = ref(false)

const activeOperations = computed(() =>
  operations.value.filter(op =>
    op.isOwn &&
    op.phase !== 'completed' &&
    !String(op.id).startsWith('local-') &&
    // Exclude arrived scout_poi ops — they are done,
    // only kept in 24h window for selectedPoiScoutResult
    !(op.operationType === 'scout_poi' && op.phase === 'arrived')
  )
)

const scoutResultOps = computed(() =>
  operations.value.filter(op =>
    op.operationType === 'scout_poi' &&
    (op.phase === 'arrived' || op.phase === 'returning' || op.phase === 'completed') &&
    op.resultJson
  )
)

const troopOperations = computed(() =>
  activeOperations.value.filter(op =>
    op.operationType === 'raid_poi' ||
    op.operationType === 'attack_settlement' ||
    op.operationType === 'reinforce_poi'
  )
)

// Includes local ops (recent dispatches not yet confirmed by backend)
function opKey(op) {
  return op.operationType === 'attack_settlement'
    ? `${op.targetSettlementId}|attack_settlement`
    : `${op.poiId}|${op.operationType}`
}
const allMovements = computed(() => {
  const backendOps = new Set(activeOperations.value.map(opKey))
  const localOps = operations.value.filter(op =>
    op.isOwn &&
    op.phase === 'outbound' &&
    String(op.id).startsWith('local-') &&
    !backendOps.has(opKey(op))
  )
  return [...activeOperations.value, ...localOps]
    .sort((a, b) => {
      const order = { outbound: 0, arrived: 1, returning: 2 }
      return (order[a.phase] ?? 3) - (order[b.phase] ?? 3)
    })
})

const troopMovements = computed(() =>
  allMovements.value.filter(op =>
    op.operationType === 'raid_poi' ||
    op.operationType === 'attack_settlement' ||
    op.operationType === 'reinforce_poi'
  )
)

function focusOperation(op) {
  if (!op.destX || !op.destY) return
  const targetX = -(op.destX * zoom.value) + cW.value / 2
  const targetY = -(op.destY * zoom.value) + cH.value / 2
  panX.value = targetX
  panY.value = targetY
  clampPan()
  renderMap(); renderMini()
  showMovementsPanel.value = false

  if (op.poiId) {
    const poi = pois.value.find(p => p.id === op.poiId)
    if (poi) { selPoi.value = poi; sel.value = null }
  }
}

const showAttackPlanner = ref(false)
const plannerArrivalTime = ref('')
const plannerUnits = ref({})
const plannerError = ref('')

const showNotes = ref(false)
const notesContent = ref(localStorage.getItem('wasteland-notes') ?? '')

function saveNotes() {
  try { localStorage.setItem('wasteland-notes', notesContent.value) } catch {}
}

const scoutMasterActive = computed(() =>
  props.player?.advisors?.scoutMaster?.active ?? false
)

const plannerTarget = computed(() => selPoi.value ?? sel.value ?? null)

const plannerTravelSeconds = computed(() => {
  if (!plannerTarget.value) return 0
  const own = slots.value.find(s => s.status === 'yours')
  if (!own) return 0
  const tx = plannerTarget.value.x ?? 0
  const ty = plannerTarget.value.y ?? 0
  const dist = Math.sqrt(Math.pow(tx - own.x, 2) + Math.pow(ty - own.y, 2))
  return Math.max(30, Math.round(dist * 0.3))
})

const plannerDepartureTime = computed(() => {
  if (!plannerArrivalTime.value || !plannerTravelSeconds.value) return null
  const arrival = new Date(plannerArrivalTime.value).getTime()
  const departure = arrival - plannerTravelSeconds.value * 1000
  if (departure <= Date.now()) return null
  return new Date(departure).toLocaleString()
})

const plannerDepartureInPast = computed(() => {
  if (!plannerArrivalTime.value || !plannerTravelSeconds.value) return true
  const arrival = new Date(plannerArrivalTime.value).getTime()
  const departure = arrival - plannerTravelSeconds.value * 1000
  return departure <= Date.now()
})

async function confirmPlannedAttack() {
  if (!plannerTarget.value || !plannerArrivalTime.value) return
  plannerError.value = ''

  const units = Object.fromEntries(
    Object.entries(plannerUnits.value).filter(([, v]) => v > 0)
  )
  if (Object.keys(units).length === 0) {
    plannerError.value = 'Select at least 1 unit.'
    return
  }

  const arrival = new Date(plannerArrivalTime.value).getTime()
  const departureDelay = Math.max(0, arrival - plannerTravelSeconds.value * 1000 - Date.now())

  setTimeout(async () => {
    try {
      if (selPoi.value) {
        await attackPoi(props.settlement.id, selPoi.value.id, units, 'sweep', plannerTravelSeconds.value)
      } else if (sel.value) {
        await attackSettlement(props.settlement.id, sel.value.id, units, plannerTravelSeconds.value)
      }
      await loadOperations()
    } catch (e) {
      console.error('Scheduled attack failed', e)
    }
  }, departureDelay)

  const departureStr = new Date(Date.now() + departureDelay).toLocaleTimeString()
  alert(`Attack scheduled! Troops will depart at ${departureStr}.`)
  showAttackPlanner.value = false
  plannerUnits.value = {}
}

const router = useRouter()

function openMailToSelected() {
  if (!sel.value?.playerId) return
  router.push({ name: 'messages', query: { toPlayerId: sel.value.playerId, toPlayerName: sel.value.owner || sel.value.name } })
}

// Geen friend — enkel ally (alliantie), neutral, enemy
function statusIcon(s) {
  return { yours: '⌂', ally: '♦', enemy: '⚔', neutral: '●', empty: '○' }[s] || '○'
}
function ownerColor(s) { return { enemy: 'ssv--e', neutral: 'ssv--n' }[s] || '' }
function selectPoi(p) { sel.value = null; selPoi.value = p }

let panFrame = null
function scheduleMapRender() {
  if (panFrame) return
  panFrame = requestAnimationFrame(() => { panFrame = null; renderMap() })
}

let miniTimer = null
function scheduleMiniRender() {
  if (miniTimer) return
  miniTimer = setTimeout(() => { miniTimer = null; renderMini() }, 60)
}

async function markAs(type) {
  if (!sel.value?.playerId) return
  try { await setPlayerRelation(sessionStorage.getItem('playerId'), sel.value.playerId, type); await reloadMap() }
  catch (e) { console.error('Failed to set relation', e) }
}

async function markNeutral() {
  if (!sel.value?.playerId) return
  try { await removePlayerRelation(sessionStorage.getItem('playerId'), sel.value.playerId); await reloadMap() }
  catch (e) { console.error('Failed to remove relation', e) }
}

async function loadWorldObjects() {
  let settlementsData = []
  try { settlementsData = await getWorldSettlements(worldSeed, sessionStorage.getItem('playerId')) }
  catch (e) { console.error(e) }
  slots.value = generateSlotsFromElevation({
    worldWidth: WW, worldHeight: WH, worldSeed,
    getElevation: worldRenderer.getElevation, waterLevel: worldRenderer.waterLevel,
    settlements: settlementsData, maxSlots: 150, minDist: 70, padding: 80
  })
  pois.value = generatePOIsFromElevation({
    worldWidth: WW, worldHeight: WH, worldSeed,
    getElevation: worldRenderer.getElevation, waterLevel: worldRenderer.waterLevel,
    slots: slots.value
  })
  worldRenderer.generateRoads(slots.value)
}

function getPlayerChunkCenter() {
  const own = slots.value.find(s => s.status === 'yours')
  if (!own) return { chunkX: 0, chunkY: 0 }
  return { chunkX: Math.floor(own.x / worldRenderer.chunkSize), chunkY: Math.floor(own.y / worldRenderer.chunkSize) }
}

function buildChunkRing(centerX, centerY, radius) {
  const coords = []
  for (let y = centerY - radius; y <= centerY + radius; y++)
    for (let x = centerX - radius; x <= centerX + radius; x++)
      if (x >= 0 && y >= 0) coords.push({ x, y })
  return coords
}

async function preloadStartArea() {
  loadingPhase.value = 'PREPARING TERRAIN'
  loadingText.value = 'Scanning nearby sectors...'
  const { chunkX, chunkY } = getPlayerChunkCenter()
  const startCoords = buildChunkRing(chunkX, chunkY, 2)
  worldRenderer.resetLoadProgress(startCoords.length)
  loadingTotal.value = startCoords.length
  loadingLoaded.value = 0
  loadingPercent.value = 0
  await worldRenderer.preloadChunks(startCoords, progress => {
    loadingLoaded.value = progress.loaded
    loadingTotal.value = progress.total
    loadingPercent.value = progress.percent
    loadingText.value = 'Preparing terrain cache...'
  })
}

let backgroundWarmupRunning = false
async function backgroundWarmup() {
  if (backgroundWarmupRunning) return
  backgroundWarmupRunning = true
  const { chunkX, chunkY } = getPlayerChunkCenter()
  for (let radius = 3; radius <= 5; radius++) {
    const coords = buildChunkRing(chunkX, chunkY, radius).filter(c => !worldRenderer.chunkCache.has(`${c.x},${c.y}`))
    if (!coords.length) continue
    await worldRenderer.preloadChunks(coords)
    await new Promise(resolve => setTimeout(resolve, 30))
  }
  backgroundWarmupRunning = false
}

let worldRenderer = null

async function reloadMap() {
  const sid = sel.value?.id
  worldRenderer.clearChunkCache()
  await loadWorldObjects()
  renderMap(); renderMini()
  if (sid) sel.value = slots.value.find(s => s.id === sid) || null
}

function renderMap() {
  const c = cvs.value
  if (!c || !worldRenderer || !mapCont.value) return
  c.width = cW.value; c.height = cH.value
  const ctx = c.getContext('2d')
  ctx.clearRect(0, 0, cW.value, cH.value)
  const viewLeft = -panX.value / zoom.value
  const viewTop = -panY.value / zoom.value
  ctx.setTransform(zoom.value, 0, 0, zoom.value, -viewLeft * zoom.value, -viewTop * zoom.value)
  worldRenderer.drawVisibleChunks(ctx, viewLeft, viewTop, cW.value / zoom.value, cH.value / zoom.value)

  // Stippellijnen enkel voor eigen + alliantie operaties
  const nowMs = Date.now()
  // Debug: log when drawing ops

  let _drewReturning = 0
  for (const op of operations.value) {
    if (op.phase !== 'outbound' && op.phase !== 'returning') continue
    const isIncomingAttack = !op.isOwn && !op.isAlliance && op.targetSettlementId === props.settlement?.id
    if (!op.isOwn && !op.isAlliance && !isIncomingAttack) continue
    if (op.phase === 'returning') _drewReturning++
    const color = isIncomingAttack ? 'rgba(255,40,40,0.85)' : op.isReinforcement ? 'rgba(48,255,128,0.6)' : op.isOwn ? 'rgba(0,212,255,0.6)' : 'rgba(255,48,64,0.6)'
    ctx.save()
    ctx.setLineDash([5 / zoom.value, 7 / zoom.value])
    ctx.lineWidth = 1.5 / zoom.value
    ctx.strokeStyle = color
    ctx.beginPath()
    if (op.phase === 'returning') {
      // Draw line from dest back to origin
      ctx.moveTo(op.destX, op.destY)
      ctx.lineTo(op.originX, op.originY)
    } else {
      ctx.moveTo(op.originX, op.originY)
      ctx.lineTo(op.destX, op.destY)
    }
    ctx.stroke()
    ctx.restore()
  }
  if (!window._lastReturnDrawLog || Date.now() - window._lastReturnDrawLog > 1000) {
    window._lastReturnDrawLog = Date.now()
    if (_drewReturning > 0) console.log(`[CANVAS] drew ${_drewReturning} returning line(s)`)
    else if (operations.value.some(op => op.phase === 'returning')) console.log('[CANVAS] returning op exists but NOT drawn (isOwn/isAlliance check failed?)')
  }

  ctx.setTransform(1, 0, 0, 1, 0, 0)
  const t = (Date.now() % 8000) / 8000
  const sweepX = t * (cW.value + 400) - 200
  const grd = ctx.createLinearGradient(sweepX - 200, 0, sweepX + 200, 0)
  grd.addColorStop(0, 'rgba(0,180,255,0)'); grd.addColorStop(0.4, 'rgba(0,180,255,0.04)')
  grd.addColorStop(0.5, 'rgba(0,200,255,0.08)'); grd.addColorStop(0.6, 'rgba(0,180,255,0.04)')
  grd.addColorStop(1, 'rgba(0,180,255,0)')
  ctx.fillStyle = grd; ctx.fillRect(0, 0, cW.value, cH.value)
  const vig = ctx.createRadialGradient(cW.value/2, cH.value/2, cW.value*0.25, cW.value/2, cH.value/2, cW.value*0.75)
  vig.addColorStop(0, 'rgba(0,20,40,0)'); vig.addColorStop(0.7, 'rgba(0,12,28,0.06)'); vig.addColorStop(1, 'rgba(0,8,20,0.2)')
  ctx.fillStyle = vig; ctx.fillRect(0, 0, cW.value, cH.value)
}

function renderMini() {
  const c = mmCvs.value
  if (!c || !worldRenderer) return
  const ctx = c.getContext('2d')
  const w = 170, h = 115
  ctx.clearRect(0, 0, w, h)
  const preview = worldRenderer.buildPreview(w, h)
  ctx.drawImage(preview, 0, 0, w, h)
  ctx.fillStyle = 'rgba(0,0,0,0.25)'; ctx.fillRect(0, 0, w, h)
  // Geen friend meer — ally is paars
  const dc = { yours: '#00d4ff', ally: '#b480ff', neutral: '#ffc830', enemy: '#ff4030' }
  slots.value.forEach(s => {
    const mx = s.x / WW * w, my = s.y / WH * h
    ctx.beginPath()
    const col = dc[s.status]
    if (col) { ctx.arc(mx, my, s.status === 'yours' ? 3 : 1.5, 0, Math.PI * 2); ctx.fillStyle = col; ctx.fill() }
    else { ctx.arc(mx, my, 0.8, 0, Math.PI * 2); ctx.fillStyle = 'rgba(0,212,255,0.15)'; ctx.fill() }
  })
  visiblePois.value.forEach(p => {
    ctx.beginPath(); ctx.arc(p.x / WW * w, p.y / WH * h, 0.8, 0, Math.PI * 2)
    ctx.fillStyle = 'rgba(0,200,230,0.3)'; ctx.fill()
  })
  const vx = (-panX.value / zoom.value) / WW * w, vy = (-panY.value / zoom.value) / WH * h
  const vw = cW.value / zoom.value / WW * w, vh = cH.value / zoom.value / WH * h
  ctx.strokeStyle = 'rgba(0,212,255,0.6)'; ctx.lineWidth = 1; ctx.strokeRect(vx, vy, vw, vh)
}

function getMinZoom() { return Math.max(cW.value / WW, cH.value / WH) }
function clampPan() {
  const mz = getMinZoom()
  if (zoom.value < mz) zoom.value = mz
  panX.value = Math.min(0, Math.max(-(WW * zoom.value) + cW.value, panX.value))
  panY.value = Math.min(0, Math.max(-(WH * zoom.value) + cH.value, panY.value))
}
function startPan(e) { if (e.button !== 0) return; isPanning.value = true; moved.value = false; panSt.value = { x: e.clientX, y: e.clientY }; panStP.value = { x: panX.value, y: panY.value } }
function onPan(e) {
  if (!isPanning.value) return
  const dx = e.clientX - panSt.value.x, dy = e.clientY - panSt.value.y
  if (Math.abs(dx) > 3 || Math.abs(dy) > 3) moved.value = true
  panX.value = panStP.value.x + dx; panY.value = panStP.value.y + dy
  clampPan(); scheduleMapRender()
}
function endPan() { if (!isPanning.value) return; isPanning.value = false; renderMap(); renderMini() }
function onClick(e) {
  if (moved.value) return
  selPoi.value = null
  const rect = mapCont.value.getBoundingClientRect()
  const wx = (e.clientX - rect.left - panX.value) / zoom.value
  const wy = (e.clientY - rect.top - panY.value) / zoom.value
  for (const s of slots.value) { if (Math.hypot(wx - s.x, wy - s.y) < 16) { sel.value = s; return } }
  sel.value = null
}
function onWheel(e) { const mz = getMinZoom(); zoom.value = Math.min(3, Math.max(mz, zoom.value + (e.deltaY > 0 ? -0.15 : 0.15))); clampPan(); renderMap(); scheduleMiniRender() }
function zoomIn() { zoom.value = Math.min(3, zoom.value + 0.25); clampPan(); renderMap(); scheduleMiniRender() }
function zoomOut() { const mz = getMinZoom(); zoom.value = Math.max(mz, zoom.value - 0.25); clampPan(); renderMap(); scheduleMiniRender() }
function centerOnPlayer() {
  const p = slots.value.find(s => s.status === 'yours')
  if (!p) return
  panX.value = -(p.x * zoom.value) + cW.value / 2; panY.value = -(p.y * zoom.value) + cH.value / 2
  clampPan(); sel.value = null; selPoi.value = null; renderMap(); renderMini()
}
function updateDims() {
  if (mapCont.value) { cW.value = mapCont.value.clientWidth; cH.value = mapCont.value.clientHeight; clampPan(); renderMap(); renderMini() }
}

onMounted(async () => {
  updateDims()
  window.addEventListener('resize', updateDims)
  loading.value = true
  function animateSweep() { if (!loading.value) renderMap(); requestAnimationFrame(animateSweep) }
  animateSweep()
  loadingPhase.value = 'INITIALIZING WORLD'; loadingText.value = 'Booting wasteland systems...'; loadingPercent.value = 10
  await nextTick()
  worldRenderer = createChunkedWorldRenderer({ worldWidth: WW, worldHeight: WH, chunkSize: 256, seed: worldSeed, imageSrc: islandImage })
  await worldRenderer.loadImage()
  loadingPercent.value = 25; loadingText.value = 'Loading world objects...'
  await loadWorldObjects()
  loadingPercent.value = 45; loadingText.value = 'Locating settlement...'
  await nextTick()
  centerOnPlayer()
  await preloadStartArea()
  loadingPercent.value = 100; loadingText.value = 'Terrain ready.'
  renderMap(); renderMini()
  await new Promise(resolve => setTimeout(resolve, 250))
  loading.value = false
  backgroundWarmup()

  // Mock operations for visual test (only enabled when sessionStorage 'showMockOps' === '1')
  const showMockOps = sessionStorage.getItem('showMockOps') === '1'
  if (showMockOps) {
    const dp = pois.value.filter(p => p.discovered)
    const ownSlot = slots.value.find(s => s.status === 'yours')
    const allySlots = slots.value.filter(s => s.status === 'ally')

    if (dp.length >= 3) {
      operations.value = [
        // Neutral arrived: glow visible, no moving dot
        { id: 'mock-neutral-arrived', phase: 'arrived', originX: 0, originY: 0, destX: dp[0].x, destY: dp[0].y, startTime: Date.now() - 999999, travelDuration: 1, isOwn: false, isAlliance: false, poiId: dp[0].id, playerName: null, unitSummary: null },
        // Ally arrived: glow + intel in panel
        ...(allySlots.length > 0 ? [{ id: 'mock-alliance-arrived', phase: 'arrived', originX: allySlots[0].x, originY: allySlots[0].y, destX: dp[1].x, destY: dp[1].y, startTime: Date.now() - 999999, travelDuration: 1, isOwn: false, isAlliance: true, poiId: dp[1].id, playerName: allySlots[0].owner ?? 'Ally', unitSummary: '8x Scavengers, 4x Raiders' }] : []),
        // Own troops outbound: static dashed route + moving pulsing dot
        ...(ownSlot ? [{ id: 'mock-own-outbound', phase: 'outbound', originX: ownSlot.x, originY: ownSlot.y, destX: dp[2].x, destY: dp[2].y, startTime: Date.now() - 25000, travelDuration: 240000, isOwn: true, isAlliance: false, poiId: dp[2].id, playerName: props.player?.name ?? 'You', unitSummary: '6x Scavengers' }] : []),
      ]
    }
  }
  else {
    // Load real operations from backend and poll every 5 s
    await loadOperations()
    await loadPoiStates()
    setInterval(async () => { await loadOperations(); await loadPoiStates() }, 5000)
  }

  frameInterval = setInterval(() => {
    frameTime.value = Date.now()
    const now = Date.now()
    const removedReturning = operations.value.filter(op => {
      if (op.phase !== 'returning') return false
      if (!op.returnsAtUtc) return false
      return now >= new Date(op.returnsAtUtc).getTime()
    })
    if (removedReturning.length > 0) {
      console.log('[FRAME-REMOVE-RETURNING]', removedReturning.map(o => `id=${o.id} returnsAtUtc=${o.returnsAtUtc} now=${new Date(now).toISOString()}`))
    }
    operations.value = operations.value.filter(op => {
      if (op.phase === 'arrived') return true
      if (op.phase === 'returning') {
        // Keep until returnsAtUtc has passed
        if (!op.returnsAtUtc) return true
        return now < new Date(op.returnsAtUtc).getTime()
      }
      return (now - op.startTime) / op.travelDuration < 1
    })
  }, 120)
})

function onMarkerEnter(item, type, e) {
  const rect = mapCont.value.getBoundingClientRect()
  hoverPos.value = { x: e.clientX - rect.left, y: e.clientY - rect.top }
  hoverItem.value = { ...item, _type: type }
}
function onMarkerLeave() { hoverItem.value = null }
function onMarkerMove(e) {
  if (!hoverItem.value) return
  const rect = mapCont.value.getBoundingClientRect()
  hoverPos.value = { x: e.clientX - rect.left, y: e.clientY - rect.top }
}

onUnmounted(() => {
  window.removeEventListener('resize', updateDims)
  if (frameInterval) clearInterval(frameInterval)
})
</script>

<style scoped>
.wl-wrap{display:flex;flex-direction:column;height:calc(100vh - 115px);margin:-22px;position:relative}
.wl-toolbar{display:flex;align-items:center;justify-content:space-between;padding:6px 16px;background:var(--bg2);border-bottom:1px solid var(--border);z-index:10;flex-shrink:0}
.tb-left{display:flex;align-items:baseline;gap:10px}
.tb-title{font-family:var(--ff-title);font-size:14px;color:var(--cyan);letter-spacing:3px;font-weight:700}
.tb-sub{font-size:8px;color:var(--cyan-dim);letter-spacing:2px;font-family:var(--ff-title)}
.tb-center{font-family:var(--ff-title);font-size:9px;color:var(--cyan);letter-spacing:1.5px;background:var(--bg3);padding:4px 14px;border:1px solid var(--border)}
.tb-right{display:flex;align-items:center;gap:6px}
.tb-btn{width:28px;height:28px;background:var(--bg3);border:1px solid var(--border);color:var(--cyan);font-size:14px;font-weight:700;cursor:pointer;display:flex;align-items:center;justify-content:center;transition:all .15s}
.tb-btn:hover{border-color:var(--cyan);box-shadow:0 0 8px rgba(0,212,255,.15)}
.tb-btn:disabled{opacity:.3;cursor:not-allowed}
.tb-zoom{font-family:var(--ff-title);font-size:9px;color:var(--muted);min-width:36px;text-align:center}
.map-area{flex:1;overflow:hidden;position:relative;cursor:grab;background:#06101e}
.map-area:active{cursor:grabbing}
.map-cvs{position:absolute;top:0;left:0;transform-origin:0 0;pointer-events:none;z-index:1;will-change:contents}
.marker-layer{position:absolute;top:0;left:0;z-index:3;transform-origin:0 0;pointer-events:none;will-change:transform}
.marker-layer--panning{pointer-events:none}
.sm{position:absolute;width:28px;height:28px;pointer-events:auto}
.sm-ring{width:100%;height:100%;border-radius:50%;border:1.5px solid transparent;display:flex;align-items:center;justify-content:center}
.sm-house{width:14px;height:14px;display:block}
.sm-house--empty{opacity:.3}
.sm--yours .sm-ring{border-color:rgba(0,212,255,.8);box-shadow:0 0 10px rgba(0,212,255,.4),0 0 20px rgba(0,212,255,.15),inset 0 0 4px rgba(0,212,255,.15);background:rgba(0,10,20,.6)}
.sm--yours .sm-house{color:#00e4ff;filter:drop-shadow(0 0 4px rgba(0,212,255,.6))}
.sm--enemy .sm-ring{border-color:rgba(255,70,50,.6);box-shadow:0 0 8px rgba(255,70,50,.3),0 0 16px rgba(255,70,50,.1);background:rgba(20,0,0,.5)}
.sm--enemy .sm-house{color:rgba(255,80,60,.95);filter:drop-shadow(0 0 4px rgba(0,0,0,.8))}
.sm--ally .sm-ring{border-color:rgba(180,128,255,.7);box-shadow:0 0 10px rgba(180,128,255,.35),0 0 20px rgba(180,128,255,.15),inset 0 0 4px rgba(180,128,255,.12);background:rgba(10,0,20,.6)}
.sm--ally .sm-house{color:rgba(195,148,255,.98);filter:drop-shadow(0 0 4px rgba(180,128,255,.5))}
.sm--neutral .sm-ring{border-color:rgba(255,200,60,.6);box-shadow:0 0 8px rgba(255,200,60,.3),0 0 16px rgba(255,200,60,.1);background:rgba(15,10,0,.5)}
.sm--neutral .sm-house{color:rgba(255,210,70,.95);filter:drop-shadow(0 0 4px rgba(0,0,0,.8))}
.sm--empty .sm-ring{border:1.5px dashed rgba(0,140,180,.5);background:rgba(0,30,50,.55);box-shadow:0 0 8px rgba(0,140,180,.2)}
.sm--empty .sm-house{color:rgba(0,160,200,.55);filter:drop-shadow(0 0 3px rgba(0,0,0,1))}
.sm.sel .sm-ring{animation:sp 1.5s infinite}
@keyframes sp{0%,100%{box-shadow:0 0 10px rgba(0,212,255,.5)}50%{box-shadow:0 0 22px rgba(0,212,255,.7)}}
.poi{position:absolute;width:34px;height:34px;transform:translate(-50%,-50%);pointer-events:auto;cursor:pointer;opacity:.85;transition:opacity .2s,transform .2s}
.poi:hover{opacity:1;transform:translate(-50%,-50%) scale(1.15)}
.poi-svg{width:100%;height:100%;filter:drop-shadow(0 0 2px rgba(0,0,0,1)) drop-shadow(0 0 5px rgba(0,180,220,.5)) drop-shadow(0 0 10px rgba(0,180,220,.2))}
.poi--factory .poi-svg{color:#00d4ff}
.poi--scrapyard .poi-svg{color:#00d4ff}
.poi--ruins .poi-svg{color:#80c8d8}
.poi--radzone .poi-svg{color:#ffc830;filter:drop-shadow(0 0 2px rgba(0,0,0,1)) drop-shadow(0 0 6px rgba(255,200,60,.4))}
.poi--bunker .poi-svg{color:#00c0e0}
.poi--tower .poi-svg{color:#00e4ff;filter:drop-shadow(0 0 2px rgba(0,0,0,1)) drop-shadow(0 0 8px rgba(0,212,255,.5))}
.poi--wreck .poi-svg{color:#00a8c8;filter:drop-shadow(0 0 2px rgba(0,0,0,1)) drop-shadow(0 0 5px rgba(0,180,220,.4))}
.poi--well .poi-svg{color:#40b0ff}
.poi--cache .poi-svg{color:#00d4ff}
.poi--crater .poi-svg{color:#e08840;filter:drop-shadow(0 0 2px rgba(0,0,0,1)) drop-shadow(0 0 6px rgba(200,110,50,.4))}
.poi-raid-glow{position:absolute;inset:-10px;border-radius:50%;background:radial-gradient(circle,rgba(61,255,156,.28),rgba(61,255,156,.08) 55%,transparent 72%);animation:raidPulse 2.2s ease-in-out infinite;pointer-events:none;z-index:-1}
@keyframes raidPulse{0%,100%{opacity:.5;transform:scale(1)}50%{opacity:1;transform:scale(1.3)}}
.op-dot{position:absolute;width:10px;height:10px;border-radius:50%;transform:translate(-50%,-50%);pointer-events:none;z-index:4}
.op-dot--own{background:rgba(0,212,255,.95);box-shadow:0 0 0 2px rgba(0,212,255,.25),0 0 10px rgba(0,212,255,.7),0 0 20px rgba(0,212,255,.3)}
.op-dot--alliance{background:rgba(180,128,255,.95);box-shadow:0 0 0 2px rgba(180,128,255,.25),0 0 10px rgba(180,128,255,.6),0 0 18px rgba(180,128,255,.25)}
.op-dot--incoming{background:rgba(255,40,40,.95);box-shadow:0 0 0 2px rgba(255,40,40,.3),0 0 10px rgba(255,40,40,.8),0 0 22px rgba(255,40,40,.4)}
.op-dot{animation:opPulse 1.6s infinite ease-in-out}
@keyframes opPulse{0%{transform:translate(-50%,-50%) scale(1);opacity:1}50%{transform:translate(-50%,-50%) scale(1.25);opacity:.9}100%{transform:translate(-50%,-50%) scale(1);opacity:1}}
.poi-label{position:absolute;transform:translate(-50%,0);font-size:7px;color:rgba(0,212,255,.7);font-family:var(--ff-title);letter-spacing:.8px;font-weight:700;pointer-events:none;text-shadow:0 0 3px rgba(0,0,0,1),0 0 6px rgba(0,0,0,1),0 0 10px rgba(0,0,0,.8);text-align:center;white-space:nowrap;line-height:1.1}
.sl{position:absolute;transform:translate(-50%,0);text-align:center;pointer-events:none}
.sl-n{display:block;font-size:9px;font-weight:700;letter-spacing:.2px;line-height:1.05;text-shadow:0 0 3px rgba(0,0,0,1),0 0 6px rgba(0,0,0,1),0 0 12px rgba(0,0,0,.8)}
.sl-o{display:block;font-size:6px;line-height:1.05;opacity:.5;text-shadow:0 1px 3px rgba(0,0,0,1)}
.sl--yours .sl-n{color:#00e4ff;font-size:8px}
.sl--ally .sl-n{color:#b480ff}
.sl--neutral .sl-n{color:#ffc830}
.sl--enemy .sl-n{color:#ff7060}
.sl--empty .sl-n{color:rgba(0,170,210,.45);font-size:6px}
.ld-ov{position:absolute;inset:0;display:flex;align-items:center;justify-content:center;z-index:50;background:rgba(2,6,12,.94)}
.ld-box{width:min(420px,84vw);padding:22px;border:1px solid var(--border-bright);background:linear-gradient(180deg,rgba(10,20,32,.96),rgba(6,12,20,.98));box-shadow:0 0 24px rgba(0,0,0,.4)}
.ld-phase{font-family:var(--ff-title);font-size:10px;letter-spacing:3px;color:var(--cyan);margin-bottom:10px}
.ld-t{font-family:var(--ff-title);font-size:12px;letter-spacing:2px;color:var(--bright);margin-bottom:16px}
.ld-bar{height:10px;border:1px solid var(--border);background:var(--bg3);position:relative;overflow:hidden}
.ld-bar-fill{height:100%;background:linear-gradient(90deg,rgba(0,212,255,.25),rgba(0,212,255,.85));box-shadow:0 0 12px rgba(0,212,255,.35);transition:width .15s ease-out}
.ld-meta{margin-top:10px;font-size:10px;color:var(--muted);letter-spacing:1px}
.minimap{position:absolute;bottom:10px;right:10px;width:180px;background:var(--bg2);border:1px solid var(--border-bright);z-index:20;box-shadow:0 0 20px rgba(0,0,0,.5)}
.mm-t{font-family:var(--ff-title);font-size:7px;color:var(--cyan);letter-spacing:2px;text-align:center;padding:3px;border-bottom:1px solid var(--border);font-weight:700}
.mm-c{display:block;width:100%}
.mm-l{display:flex;justify-content:center;gap:6px;padding:3px;border-top:1px solid var(--border);flex-wrap:wrap}
.ml{font-size:6px;color:var(--muted);display:flex;align-items:center;gap:2px}
.md{width:5px;height:5px;border-radius:50%;display:inline-block}
.md--y{background:#00d4ff}.md--a{background:#b480ff}.md--n{background:#ffc830}.md--e{background:#ff4030}
.md--o{border:1px solid rgba(0,212,255,.3)}
.map-tooltip{position:absolute;z-index:40;pointer-events:none;min-width:140px;max-width:220px;background:linear-gradient(180deg,rgba(6,14,24,.95),rgba(4,10,18,.98));border:1px solid var(--border-bright);box-shadow:0 0 16px rgba(0,0,0,.5),0 0 30px rgba(0,212,255,.04);padding:8px 10px;display:flex;flex-direction:column;gap:5px}
.tt-header{display:flex;align-items:center;gap:8px}
.tt-icon{width:22px;height:22px;border:1px solid var(--border);display:flex;align-items:center;justify-content:center;font-size:10px;background:var(--bg3);border-radius:3px;flex-shrink:0}
.tt-icon--yours{border-color:rgba(0,212,255,.4);color:#00d4ff}
.tt-icon--ally{border-color:rgba(180,128,255,.4);color:#b480ff}
.tt-icon--neutral{border-color:rgba(255,200,60,.3);color:#ffc830}
.tt-icon--enemy{border-color:rgba(255,60,60,.3);color:#ff7060}
.tt-icon--empty{border-style:dashed;border-color:rgba(0,212,255,.2);color:rgba(0,212,255,.4)}
.tt-icon--factory,.tt-icon--scrapyard,.tt-icon--ruins,.tt-icon--bunker,.tt-icon--tower,.tt-icon--wreck,.tt-icon--well,.tt-icon--cache{border-color:rgba(0,200,230,.3);color:rgba(0,200,230,.7)}
.tt-icon--radzone,.tt-icon--crater{border-color:rgba(255,180,50,.3);color:rgba(255,180,50,.7)}
.tt-name{font-size:11px;color:var(--bright);font-weight:700;line-height:1.2}
.tt-sub{font-size:8px;color:var(--muted);letter-spacing:.8px;margin-top:1px}
.tt-stats{display:flex;gap:10px;padding-top:3px;border-top:1px solid var(--border)}
.tt-stat{font-size:9px;color:var(--cyan);font-family:var(--ff-title);letter-spacing:.5px}
.tt-stat-hint{font-size:8px;color:var(--cyan-dim);letter-spacing:1px;font-style:italic}
.sp{position:absolute;bottom:0;left:0;right:0;background:var(--bg2);border-top:1px solid var(--border-bright);z-index:30;box-shadow:0 -4px 20px rgba(0,0,0,.4)}
.sp-ln{height:1px;background:linear-gradient(90deg,transparent,var(--cyan),transparent);opacity:.5}
.sp-b{padding:12px 20px;display:flex;align-items:center;gap:20px;position:relative;flex-wrap:wrap}
.sp-x{position:absolute;top:6px;right:12px;background:var(--bg3);border:1px solid var(--border);color:var(--muted);cursor:pointer;padding:2px 8px;font-family:var(--ff);font-size:10px}
.sp-h{display:flex;align-items:center;gap:12px;min-width:180px}
.sp-i{width:28px;height:28px;border:1px solid var(--border);display:flex;align-items:center;justify-content:center;font-size:12px;background:var(--bg3);border-radius:4px}
.si--yours{border-color:rgba(0,212,255,.4);color:#00d4ff;box-shadow:0 0 6px rgba(0,212,255,.15)}
.si--ally{border-color:rgba(180,128,255,.4);color:#b480ff;box-shadow:0 0 6px rgba(180,128,255,.15)}
.si--neutral{border-color:rgba(255,200,60,.3);color:#ffc830}
.si--enemy{border-color:rgba(255,60,60,.3);color:#ff7060}
.si--empty{border-style:dashed;border-color:rgba(0,212,255,.15);color:rgba(0,212,255,.3)}
.si--poi{border-color:rgba(0,200,230,.3);color:rgba(0,200,230,.7);box-shadow:0 0 4px rgba(0,200,230,.1)}
.si--active{border-color:rgba(61,255,156,.5);color:#3dff9c;box-shadow:0 0 8px rgba(61,255,156,.25)}
.sp-n{font-size:13px;color:var(--bright);font-weight:700}
.sp-s{font-size:9px;color:var(--muted);display:flex;align-items:center;gap:6px;margin-top:2px;flex-wrap:wrap}
.sp-t{font-size:7px;padding:1px 5px;border:1px solid;letter-spacing:1px;font-family:var(--ff-title);font-weight:700}
.st--yours{color:#00d4ff;border-color:rgba(0,212,255,.4)}
.st--ally{color:#b480ff;border-color:rgba(180,128,255,.4)}
.st--neutral{color:#ffc830;border-color:rgba(255,200,60,.3)}
.st--enemy{color:#ff7060;border-color:rgba(255,60,60,.3)}
.st--empty{color:rgba(0,212,255,.3);border-color:rgba(0,212,255,.15);border-style:dashed}
.st--poi{color:rgba(0,200,230,.6);border-color:rgba(0,200,230,.25)}
.poi-active-badge{color:#3dff9c;font-size:8px;letter-spacing:1px;animation:raidPulse 2s ease-in-out infinite}
.ss-w{display:flex;gap:16px}
.ss{min-width:50px}
.ssl{font-size:7px;color:var(--muted);text-transform:uppercase;letter-spacing:1.5px;font-weight:700}
.ssv{font-size:15px;color:var(--cyan);font-family:var(--ff-title);font-weight:700}
.ssv--e{color:#ff7060}.ssv--n{color:#ffc830}.ssv--a{color:#b480ff}
.ssv--unknown{color:var(--muted);font-size:13px}
.ssv--active{color:#3dff9c;font-size:12px}
.ssv--available{color:var(--cyan);font-size:12px}
.sp-d{font-size:10px;color:var(--text);max-width:300px;line-height:1.4}
.sp-actions{display:flex;align-items:center;gap:12px;flex:1;flex-wrap:wrap}
.sa-w{display:flex;gap:6px;flex-wrap:wrap}
.sa-w--diplo{margin-left:auto;border-left:1px solid var(--border);padding-left:12px}
.sa{padding:6px 14px;font-family:var(--ff);font-size:10px;font-weight:700;cursor:pointer;border:1px solid;transition:all .15s;display:flex;align-items:center;gap:5px}
.sa--a{background:rgba(255,60,60,.08);border-color:rgba(255,60,60,.3);color:#ff7060}
.sa--a:hover{box-shadow:0 0 10px rgba(255,60,60,.15)}
.sa--s{background:rgba(255,170,32,.06);border-color:rgba(255,170,32,.2);color:var(--amber)}
.sa--s:hover{box-shadow:0 0 10px rgba(255,170,32,.15)}
.sa--raid{background:rgba(255,60,60,.08);border-color:rgba(255,60,60,.3);color:#ff7060}
.sa--raid:hover{box-shadow:0 0 10px rgba(255,60,60,.15)}
.sa--p{background:rgba(180,128,255,.06);border-color:rgba(180,128,255,.2);color:#b480ff}
.sa--c{background:rgba(0,212,255,.08);border-color:rgba(0,212,255,.3);color:var(--cyan)}
.sa--c:hover{box-shadow:0 0 10px rgba(0,212,255,.15)}
.sa--e{background:rgba(255,60,60,.06);border-color:rgba(255,60,60,.25);color:#ff7060}
.sa--e:hover{box-shadow:0 0 10px rgba(255,60,60,.15)}
.sa--n{background:rgba(255,200,60,.06);border-color:rgba(255,200,60,.25);color:#ffc830}
.sa--n:hover{box-shadow:0 0 10px rgba(255,200,60,.15)}
.sa--m{background:rgba(0,212,255,.06);border-color:rgba(0,212,255,.25);color:var(--cyan)}
.sa--m:hover{box-shadow:0 0 10px rgba(0,212,255,.15)}
.sa-cost{font-size:8px;opacity:.75;font-weight:400}
.poi-alliance-intel{padding:10px 12px;border:1px solid rgba(180,128,255,.3);background:rgba(180,128,255,.05);display:flex;flex-direction:column;gap:6px}
.pai-title{font-family:var(--ff-title);font-size:8px;letter-spacing:2px;color:#b480ff;margin-bottom:2px}
.pai-row{display:flex;justify-content:space-between;align-items:center;gap:12px}
.pai-name{font-size:11px;color:var(--bright);font-weight:700}
.pai-units{font-size:9px;color:#b480ff;font-family:var(--ff-title);letter-spacing:.5px}
.modal-backdrop{position:fixed;inset:0;background:rgba(0,0,0,.65);z-index:100;display:flex;align-items:center;justify-content:center}
.modal-box{width:min(440px,92vw);background:linear-gradient(180deg,rgba(8,16,28,.98),rgba(5,10,18,.99));border:1px solid var(--border-bright);box-shadow:0 0 40px rgba(0,0,0,.6),0 0 80px rgba(0,212,255,.04)}
.wl-modal{width:min(400px,90vw);background:linear-gradient(180deg,rgba(8,16,28,.98),rgba(5,10,18,.99));border:1px solid var(--border-bright);box-shadow:0 0 40px rgba(0,0,0,.6),0 0 80px rgba(0,212,255,.04)}
.modal-header{display:flex;justify-content:space-between;align-items:center;padding:12px 16px;border-bottom:1px solid var(--border);background:linear-gradient(90deg,rgba(0,212,255,.05),transparent)}
.modal-title{font-family:var(--ff-title);font-size:11px;color:var(--cyan);letter-spacing:2.5px}
.modal-close{background:none;border:1px solid var(--border);color:var(--muted);cursor:pointer;padding:2px 8px;font-size:10px;transition:all .15s}
.modal-close:hover{border-color:var(--border-bright);color:var(--bright)}
.modal-body{padding:16px;display:flex;flex-direction:column;gap:14px}
.modal-target{font-size:12px;color:var(--bright)}
.modal-target strong{color:var(--cyan)}
.modal-info{font-size:11px;color:var(--muted);line-height:1.6;padding:10px;border:1px solid var(--border);background:rgba(0,212,255,.02)}
.modal-info strong{color:var(--bright)}
.modal-row{display:flex;flex-direction:column;gap:8px}
.modal-label{font-size:9px;color:var(--muted);letter-spacing:2px;font-family:var(--ff-title);text-transform:uppercase}
.modal-fixed-cost{display:flex;justify-content:space-between;align-items:center;padding:10px 12px;border:1px solid var(--border-bright);background:rgba(0,212,255,.04)}
.modal-value{font-family:var(--ff-title);color:var(--cyan);font-size:14px;letter-spacing:1px}
.modal-input-row{display:flex;align-items:center;gap:6px}
.modal-input{width:90px;height:36px;background:rgba(14,22,32,.9);border:1px solid var(--border);color:var(--bright);font-family:var(--ff-title);font-size:14px;text-align:center;outline:none;-moz-appearance:textfield;appearance:textfield}
.modal-input::-webkit-outer-spin-button,.modal-input::-webkit-inner-spin-button{-webkit-appearance:none}
.modal-input:focus{border-color:var(--cyan);box-shadow:0 0 0 1px rgba(0,212,255,.2)}
.modal-hint{font-size:9px;color:var(--muted);letter-spacing:1px}
.modal-op-grid{display:grid;grid-template-columns:1fr 1fr;gap:8px}
.modal-op-btn{padding:10px 12px;border:1px solid var(--border);background:rgba(255,255,255,.02);cursor:pointer;text-align:left;display:flex;flex-direction:column;gap:4px;transition:all .15s}
.modal-op-btn:hover{border-color:var(--border-bright);background:rgba(0,212,255,.04)}
.modal-op-btn--active{border-color:var(--cyan);background:rgba(0,212,255,.08);box-shadow:inset 0 0 12px rgba(0,212,255,.04)}
.modal-op-name{font-family:var(--ff-title);font-size:10px;color:var(--bright);letter-spacing:1px}
.modal-op-time{font-size:9px;color:var(--cyan);font-family:var(--ff-title)}
.modal-op-desc{font-size:9px;color:var(--muted);line-height:1.4}
.modal-footer{display:flex;justify-content:flex-end;gap:8px;padding:12px 16px;border-top:1px solid var(--border)}
.modal-cancel{padding:8px 16px;border:1px solid var(--border);background:transparent;color:var(--muted);font-family:var(--ff-title);font-size:10px;letter-spacing:1.5px;cursor:pointer;transition:all .15s}
.modal-cancel:hover{border-color:var(--border-bright);color:var(--bright)}
.modal-confirm{padding:8px 16px;border:1px solid var(--border-bright);background:linear-gradient(180deg,rgba(0,212,255,.12),rgba(0,212,255,.04));color:var(--cyan);font-family:var(--ff-title);font-size:10px;letter-spacing:1.5px;cursor:pointer;transition:all .15s}
.modal-confirm:hover{background:linear-gradient(180deg,rgba(0,212,255,.2),rgba(0,212,255,.08));box-shadow:0 0 14px rgba(0,212,255,.12)}
.qty-btn{width:36px;height:36px;border:1px solid var(--border);background:rgba(0,212,255,.06);color:var(--cyan);font-family:var(--ff-title);font-size:16px;cursor:pointer;display:flex;align-items:center;justify-content:center;transition:all .15s}
.qty-btn:hover{border-color:var(--border-bright);background:rgba(0,212,255,.12)}
.sa--reinforce{background:rgba(48,255,128,.08);border-color:rgba(48,255,128,.3);color:#30ff80}
.sa--reinforce:hover{box-shadow:0 0 10px rgba(48,255,128,.2)}
.sa--recall{border-color:rgba(255,200,64,.4);color:#ffc840;background:rgba(255,200,64,.06)}
.sa--recall:hover{background:rgba(255,200,64,.15)}
.sa--raid:disabled{opacity:.35;cursor:not-allowed}
.poi-cleared-badge{font-size:10px;color:#ff9040;font-family:var(--ff-title);letter-spacing:1px;padding:2px 8px;border:1px solid rgba(255,144,64,.3);background:rgba(255,144,64,.05)}
.poi--relocating{animation:poiRelocate 3s ease-out forwards;pointer-events:none}
@keyframes poiRelocate{0%{opacity:1;transform:scale(1)}70%{opacity:.3;transform:scale(1.4)}100%{opacity:0;transform:scale(0)}}
.modal-box--wide{width:min(560px,94vw)}
.modal-unit-list{display:flex;flex-direction:column;gap:8px;max-height:260px;overflow-y:auto}
.modal-unit-row{display:flex;align-items:center;gap:10px;padding:8px 10px;border:1px solid var(--border);background:rgba(0,212,255,.02)}
.modal-unit-img{width:28px;height:28px;object-fit:contain;flex-shrink:0;filter:drop-shadow(0 0 4px rgba(0,212,255,.3))}
.modal-unit-name{flex:1;font-size:11px;color:var(--bright);font-weight:700}
.modal-unit-avail{font-size:9px;color:var(--muted);font-family:var(--ff-title);letter-spacing:.8px;min-width:52px;text-align:right}
.modal-input--sm{width:58px}
.modal-error-msg{padding:8px 10px;border:1px solid rgba(255,60,60,.3);background:rgba(255,60,60,.06);color:#ff7060;font-size:10px}
.modal-vault-info{display:flex;justify-content:space-between;align-items:center;padding:8px 16px;background:rgba(0,212,255,.03);border-top:1px solid var(--border)}
.vault-ok{color:var(--green);font-family:var(--ff-title);font-weight:700}
.vault-low{color:var(--red);font-family:var(--ff-title);font-weight:700}
.modal-confirm:disabled{opacity:.35;cursor:not-allowed;box-shadow:none}
.ps-enter-active,.ps-leave-active{transition:transform .2s,opacity .2s}
.ps-enter-from,.ps-leave-to{transform:translateY(100%);opacity:0}
.modal-fade-enter-active,.modal-fade-leave-active{transition:opacity .18s}
.modal-fade-enter-from,.modal-fade-leave-to{opacity:0}
.tb-movements{position:relative}
.tb-movements-btn{position:relative;width:auto !important;padding:0 10px;gap:5px;font-size:11px;letter-spacing:0.5px}
.tb-movements-btn--active{border-color:var(--cyan);background:rgba(0,212,255,.1);color:var(--cyan)}
.op-timer{position:absolute;transform:translate(-50%,-100%);background:rgba(0,0,0,.85);border:1px solid var(--cyan);color:var(--cyan);font-family:var(--ff-title);font-size:9px;font-weight:700;padding:2px 6px;white-space:nowrap;pointer-events:none;letter-spacing:0.5px}
.notes-panel{position:absolute;top:calc(100% + 8px);right:0;width:300px;background:var(--bg2);border:1px solid var(--border-bright);box-shadow:0 8px 32px rgba(0,0,0,.6);z-index:200;display:flex;flex-direction:column}
.notes-textarea{width:100%;height:200px;background:var(--bg3);border:none;border-top:1px solid var(--border);color:var(--text);font-family:var(--ff);font-size:11px;padding:12px;resize:none;outline:none;line-height:1.6;box-sizing:border-box}
.notes-textarea::placeholder{color:var(--muted)}
.tb-notes-btn{position:relative}
.movements-panel{position:absolute;top:calc(100% + 8px);right:0;width:320px;background:var(--bg2);border:1px solid var(--border-bright);box-shadow:0 8px 32px rgba(0,0,0,.6);z-index:200;display:flex;flex-direction:column;max-height:420px}
.movements-list{overflow-y:auto;flex:1;max-height:340px}
.movements-section-title{padding:6px 14px;font-size:8px;color:var(--cyan);letter-spacing:2px;font-family:var(--ff-title);font-weight:700;background:rgba(0,212,255,.04);border-bottom:1px solid var(--border);border-top:1px solid var(--border)}
.movements-section-title--incoming{color:#ff5050;background:rgba(255,40,40,.07);border-color:rgba(255,40,40,.25);display:flex;align-items:center;justify-content:space-between}
.overlay-mute-btn{background:none;border:none;cursor:pointer;font-size:12px;padding:0 2px;line-height:1;opacity:.7;transition:opacity .15s}
.overlay-mute-btn:hover{opacity:1}
.movement-row--incoming{background:rgba(255,40,40,.04);border-bottom-color:rgba(255,40,40,.15)}
.movement-row--incoming:hover{background:rgba(255,40,40,.09)}
.movements-incoming-badge{position:absolute;top:-4px;right:-4px;min-width:14px;height:14px;border-radius:7px;background:#ff3030;color:#fff;font-size:9px;font-family:var(--ff-title);font-weight:700;display:flex;align-items:center;justify-content:center;padding:0 3px;line-height:1}
.movements-header{display:flex;justify-content:space-between;align-items:center;padding:10px 14px;border-bottom:1px solid var(--border);background:linear-gradient(90deg,rgba(0,212,255,.06),transparent)}
.movements-title{font-family:var(--ff-title);font-size:9px;color:var(--cyan);letter-spacing:2px;font-weight:700}
.movements-close{background:none;border:none;color:var(--muted);cursor:pointer;font-size:12px;padding:2px 6px}
.movements-close:hover{color:var(--text)}
.movements-empty{padding:16px;font-size:11px;color:var(--muted);text-align:center}
.movement-row{display:flex;align-items:center;gap:10px;padding:10px 14px;border-bottom:1px solid var(--border);cursor:pointer;transition:background .15s}
.movement-row:hover{background:rgba(0,212,255,.04)}
.movement-icon{font-size:14px;width:20px;text-align:center;flex-shrink:0}
.movement-info{flex:1;min-width:0}
.movement-target{font-size:11px;color:var(--text);font-weight:600;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.movement-units{font-size:9px;color:var(--muted);margin-top:2px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.movement-right{display:flex;flex-direction:column;align-items:flex-end;gap:2px;flex-shrink:0}
.movement-status{font-size:14px}
.status--outbound{color:var(--cyan)}
.status--arrived{color:var(--green)}
.status--returning{color:#ffc840}
.movement-time{font-family:var(--ff-title);font-size:10px;color:var(--cyan);font-weight:700}
.tb-planner-btn{border-color:rgba(255,200,64,.4);color:#ffc840;background:rgba(255,200,64,.06);font-size:10px;letter-spacing:1px}
.tb-planner-btn:hover{background:rgba(255,200,64,.15)}
.wl-modal--wide{max-width:480px !important;width:94vw !important}
</style>
