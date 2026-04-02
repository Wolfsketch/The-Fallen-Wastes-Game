// ── UNITS ───────────────────────────────────────────────────────────────────
export const UNITS = [
  // BARRACKS
  {
    name: 'Scavenger', role: 'Early raid filler', desc: 'Cheap wasteland survivor. Weak alone, dangerous in numbers.',
    facility: 'Barracks', category: 'Infantry', damage: 'Ballistic',
    atk: 10, spd: 14, carry: 10, pop: 1,
    defBal: 5, defImp: 4, defEne: 3,
    trainTime: '4m 00s', costs: { food: 40, water: 30 },
  },
  {
    name: 'Raider', role: 'Fast raid infantry', desc: 'Fast assault infantry specialized in hit-and-run attacks.',
    facility: 'Barracks', category: 'Infantry', damage: 'Ballistic',
    atk: 16, spd: 18, carry: 14, pop: 1,
    defBal: 6, defImp: 4, defEne: 3,
    trainTime: '4m 30s', costs: { food: 70, water: 50 },
  },
  {
    name: 'Outpost Defender', role: 'Defensive core unit', desc: 'Core settlement defender trained to hold the line behind walls.',
    facility: 'Barracks', category: 'Infantry', damage: 'Impact',
    atk: 8, spd: 10, carry: 2, pop: 2,
    defBal: 16, defImp: 12, defEne: 7,
    trainTime: '5m 30s', costs: { food: 90, water: 70 },
  },
  {
    name: 'Rifleman', role: 'All-round infantry', desc: 'Reliable all-round infantry with stable battlefield performance.',
    facility: 'Barracks', category: 'Ranged', damage: 'Ballistic',
    atk: 18, spd: 12, carry: 6, pop: 2,
    defBal: 9, defImp: 7, defEne: 5,
    trainTime: '6m 00s', costs: { food: 80, water: 70, scrap: 40 },
  },
  {
    name: 'Shock Fighter', role: 'Assault infantry', desc: 'Close-range bruiser built to smash through defending lines.',
    facility: 'Barracks', category: 'Heavy', damage: 'Impact',
    atk: 22, spd: 12, carry: 4, pop: 2,
    defBal: 8, defImp: 10, defEne: 5,
    trainTime: '7m 30s', costs: { food: 100, water: 80, scrap: 60 },
  },
  {
    name: 'Sniper', role: 'Ranged specialist', desc: 'Precision marksman focused on high-value enemy targets.',
    facility: 'Barracks', category: 'Ranged', damage: 'Ballistic',
    atk: 28, spd: 11, carry: 4, pop: 3,
    defBal: 5, defImp: 4, defEne: 5,
    trainTime: '8m 30s', costs: { food: 120, water: 90, scrap: 80 },
  },
  {
    name: 'Flame Trooper', role: 'Anti-swarm infantry', desc: 'Area denial infantry effective against swarms and defenders.',
    facility: 'Barracks', category: 'Heavy', damage: 'Impact',
    atk: 24, spd: 10, carry: 2, pop: 3,
    defBal: 9, defImp: 8, defEne: 4,
    trainTime: '9m 00s', costs: { food: 110, water: 90, fuel: 90 },
  },
  {
    name: 'Power Trooper', role: 'Heavy elite infantry', desc: 'Elite power-armored soldier with excellent frontline staying power.',
    facility: 'Barracks', category: 'Heavy', damage: 'Impact',
    atk: 32, spd: 9, carry: 6, pop: 4,
    defBal: 14, defImp: 11, defEne: 9,
    trainTime: '11m 00s', costs: { food: 130, water: 100, scrap: 120, energy: 80 },
  },
  // GARAGE
  {
    name: 'Assault Bike', role: 'Light vehicle harassment', desc: 'Light vehicle for fast military pressure and map control.',
    facility: 'Garage', category: 'Vehicle', damage: 'Ballistic',
    atk: 20, spd: 22, carry: 12, pop: 2,
    defBal: 8, defImp: 5, defEne: 4,
    trainTime: '6m 30s', costs: { scrap: 100, fuel: 70 },
  },
  {
    name: 'Rust Buggy', role: 'Fast vehicle raider', desc: 'Fast wasteland buggy built for plunder and pursuit.',
    facility: 'Garage', category: 'Vehicle', damage: 'Ballistic',
    atk: 24, spd: 24, carry: 24, pop: 3,
    defBal: 9, defImp: 6, defEne: 5,
    trainTime: '8m 00s', costs: { scrap: 140, fuel: 90 },
  },
  {
    name: 'War Rig', role: 'Main assault vehicle', desc: 'Heavy assault vehicle used as the backbone of larger attacks.',
    facility: 'Garage', category: 'Vehicle', damage: 'Impact',
    atk: 38, spd: 13, carry: 30, pop: 5,
    defBal: 14, defImp: 10, defEne: 8,
    trainTime: '14m 00s', costs: { scrap: 220, fuel: 150, energy: 60 },
  },
  {
    name: 'Interceptor', role: 'Anti-vehicle hunter', desc: 'Mobile hunter specialized in destroying armored targets and support lines.',
    facility: 'Garage', category: 'Vehicle', damage: 'Energy',
    atk: 27, spd: 17, carry: 8, pop: 4,
    defBal: 10, defImp: 8, defEne: 8,
    trainTime: '12m 00s', costs: { scrap: 180, fuel: 130, energy: 90 },
  },
  {
    name: 'Siege Carrier', role: 'Siege backbone', desc: 'Slow armored platform that supports heavy breach operations.',
    facility: 'Garage', category: 'Transport', damage: 'Impact',
    atk: 18, spd: 7, carry: 10, pop: 5,
    defBal: 16, defImp: 13, defEne: 8,
    trainTime: '16m 00s', costs: { scrap: 260, fuel: 180, energy: 80 },
  },
  // WORKSHOP
  {
    name: 'Wall Breaker', role: 'Anti-structure siege', desc: 'Dedicated siege unit designed to tear through perimeter fortifications.',
    facility: 'Workshop', category: 'Siege', damage: 'Impact',
    atk: 20, spd: 7, carry: 0, pop: 4,
    defBal: 8, defImp: 9, defEne: 6,
    trainTime: '13m 00s', costs: { scrap: 180, fuel: 120, energy: 120 },
  },
  {
    name: 'EMP Launcher', role: 'Disruption support', desc: 'Disruption platform that fries electronics and weakens advanced defenses.',
    facility: 'Workshop', category: 'Support', damage: 'Energy',
    atk: 19, spd: 8, carry: 0, pop: 4,
    defBal: 7, defImp: 6, defEne: 10,
    trainTime: '12m 00s', costs: { scrap: 140, energy: 110, rareTech: 20 },
  },
  {
    name: 'Laser Squad', role: 'Tech damage dealer', desc: 'Advanced precision team armed with unstable laser weaponry.',
    facility: 'Workshop', category: 'Ranged', damage: 'Energy',
    atk: 31, spd: 10, carry: 4, pop: 4,
    defBal: 8, defImp: 6, defEne: 11,
    trainTime: '12m 30s', costs: { food: 100, water: 80, energy: 120, rareTech: 25 },
  },
  {
    name: 'Rad Bomber', role: 'Heavy artillery', desc: 'Heavy bombardment unit for breaking dense defensive formations.',
    facility: 'Workshop', category: 'Siege', damage: 'Energy',
    atk: 42, spd: 6, carry: 0, pop: 5,
    defBal: 8, defImp: 6, defEne: 10,
    trainTime: '18m 00s', costs: { scrap: 160, fuel: 120, energy: 130, rareTech: 35 },
  },
  {
    name: 'Drone Swarm', role: 'Mobile tech harassment', desc: 'Autonomous strike drones used for harassment and flanking pressure.',
    facility: 'Workshop', category: 'Support', damage: 'Energy',
    atk: 25, spd: 19, carry: 6, pop: 3,
    defBal: 6, defImp: 4, defEne: 9,
    trainTime: '10m 30s', costs: { scrap: 100, energy: 80, rareTech: 20 },
  },
  // COMMAND CENTER
  {
    name: 'Convoy', role: 'Expansion & takeover', desc: 'Strategic convoy used to found a new settlement or claim an enemy settlement after a successful siege.',
    facility: 'CommandCenter', category: 'Colony', damage: 'None',
    atk: 0, spd: 5, carry: 0, pop: 6,
    defBal: 6, defImp: 6, defEne: 6,
    trainTime: '30m 00s', costs: { food: 300, water: 300, scrap: 240, fuel: 160, energy: 120, rareTech: 50 },
  },
]

// ── BUILDINGS ────────────────────────────────────────────────────────────────
export const BUILDINGS = [
  // CORE
  {
    name: 'HeadQuarter', icon: '🏛️', category: 'Core',
    desc: 'The nerve center of your settlement. Increases build speed and is required for all major upgrades.',
    effects: ['Reduces building time by 1.5% per level (max 45% at level 30)', 'Unlocks new buildings as it levels up', 'Uses 4 population per level'],
    prereqs: null,
  },
  {
    name: 'Shelter', icon: '🏚️', category: 'Core',
    desc: 'Provides population capacity. Every unit and every building level costs population.',
    effects: ['Base pop cap: 200 at level 1', 'Each level adds ~35 + 6×(level−1) additional capacity', 'Does not consume population itself'],
    prereqs: null,
  },
  {
    name: 'CouncilHall', icon: '⚖️', category: 'Core',
    desc: 'Advanced administrative building that unlocks multi-settlement coordination and diplomatic options.',
    effects: ['Enables alliance participation at higher levels', 'Uses 3 population per level'],
    prereqs: 'HQ 6, Shelter 8, TechLab 4',
  },
  // PRODUCTION
  {
    name: 'FarmDome', icon: '🌾', category: 'Production',
    desc: 'Produces Food. Starts at level 0 — upgrade it immediately as the basis of your economy.',
    effects: ['Production: 23 × level × 1.12^(level−1) Food/hour', 'Uses 2 population per level'],
    prereqs: 'None (start at level 0)',
  },
  {
    name: 'WaterPurifier', icon: '💧', category: 'Production',
    desc: 'Produces Water. Water is consumed by most high-tier units and is critical for early growth.',
    effects: ['Production: 23 × level × 1.12^(level−1) Water/hour', 'Uses 2 population per level'],
    prereqs: 'None (start at level 0)',
  },
  {
    name: 'ScrapForge', icon: '⚙️', category: 'Production',
    desc: 'Produces Scrap. Essential for vehicles, walls, and late-game technology.',
    effects: ['Production: 23 × level × 1.12^(level−1) Scrap/hour', 'Uses 2 population per level'],
    prereqs: 'None (start at level 0)',
  },
  {
    name: 'FuelRefinery', icon: '⛽', category: 'Production',
    desc: 'Produces Fuel. Vehicles and siege units require large quantities.',
    effects: ['Production: 23 × level × 1.12^(level−1) Fuel/hour', 'Uses 2 population per level'],
    prereqs: 'None (start at level 0)',
  },
  {
    name: 'SolarArray', icon: '⚡', category: 'Production',
    desc: 'Produces Energy. Advanced units and the Workshop demand Energy.',
    effects: ['Production: 23 × level × 1.12^(level−1) Energy/hour', 'Uses 2 population per level'],
    prereqs: 'None (start at level 0)',
  },
  // STORAGE
  {
    name: 'FoodSilo', icon: '🥫', category: 'Storage',
    desc: 'Stores Food. Base 1000, +900 per additional level.',
    effects: ['Storage: 1000 + (level−1) × 900', 'No population cost'],
    prereqs: 'Starter building (level 1)',
  },
  {
    name: 'WaterTank', icon: '🪣', category: 'Storage',
    desc: 'Stores Water. Base 1000, +900 per additional level.',
    effects: ['Storage: 1000 + (level−1) × 900', 'No population cost'],
    prereqs: 'Starter building (level 1)',
  },
  {
    name: 'ScrapVault', icon: '🗄️', category: 'Storage',
    desc: 'Stores Scrap.',
    effects: ['Storage: 1000 + (level−1) × 900', 'No population cost'],
    prereqs: 'Starter building (level 1)',
  },
  {
    name: 'FuelDepot', icon: '🛢️', category: 'Storage',
    desc: 'Stores Fuel.',
    effects: ['Storage: 1000 + (level−1) × 900', 'No population cost'],
    prereqs: 'Starter building (level 1)',
  },
  {
    name: 'PowerBank', icon: '🔋', category: 'Storage',
    desc: 'Stores Energy.',
    effects: ['Storage: 1000 + (level−1) × 900', 'No population cost'],
    prereqs: 'Starter building (level 1)',
  },
  {
    name: 'TechVault', icon: '🔬', category: 'Storage',
    desc: 'Stores RareTech. Smaller base but covers the slower RareTech economy.',
    effects: ['Storage: 200 + (level−1) × 180', 'No population cost'],
    prereqs: 'Starter building (level 1)',
  },
  {
    name: 'RaidVault', icon: '💰', category: 'Storage',
    desc: 'Special vault that protects Scouting resources. Reaches unlimited capacity at level 10.',
    effects: ['Storage: level × 100 (levels 1–9)', 'Unlimited from level 10', 'No population cost'],
    prereqs: 'TechLab 3, TechVault 5',
  },
  // MILITARY
  {
    name: 'Barracks', icon: '⚔️', category: 'Military',
    desc: 'Trains infantry units — the backbone of every early army.',
    effects: ['Unlocks 8 infantry unit types', 'Training time reduced 5% per level', 'Uses 3 population per level'],
    prereqs: 'HQ 2, Shelter 3, all 5 resource buildings at 3',
  },
  {
    name: 'Garage', icon: '🔧', category: 'Military',
    desc: 'Produces vehicle units. Faster and more powerful than infantry.',
    effects: ['Unlocks 5 vehicle unit types', 'Training time reduced 5% per level', 'Uses 4 population per level'],
    prereqs: 'HQ 4, Shelter 5, all 5 resource buildings at 7',
  },
  {
    name: 'Workshop', icon: '🏗️', category: 'Military',
    desc: 'Produces advanced and siege units. Requires RareTech for some trainees.',
    effects: ['Unlocks 5 advanced/siege unit types', 'Training time reduced 5% per level', 'Uses 4 population per level'],
    prereqs: 'HQ 4, Shelter 7, TechLab 4, all 5 resource buildings at 10',
  },
  {
    name: 'CommandCenter', icon: '📡', category: 'Military',
    desc: 'Trains strategic units including the Convoy for expansion and siege operations.',
    effects: ['Unlocks Convoy unit', 'Uses 5 population per level'],
    prereqs: 'HQ 5, Shelter 8, TechLab 8, all 5 resource buildings at 12',
  },
  // DEFENSE
  {
    name: 'PerimeterWall', icon: '🛡️', category: 'Defense',
    desc: 'Fortified outer walls that defend your settlement against raids.',
    effects: ['Wall HP scales with level', 'Forces attackers to breach before reaching buildings', 'No population cost'],
    prereqs: 'HQ 3, Shelter 2, Barracks 1',
  },
  // RESEARCH / SPECIAL
  {
    name: 'TechLab', icon: '🧪', category: 'Research',
    desc: 'Enables research and unlocks advanced buildings. Gate for all high-tier content.',
    effects: ['Unlocks research queue', 'Generates Research Points passively', 'Uses 3 population per level'],
    prereqs: 'HQ 3, Shelter 4, all 5 resource buildings at 5',
  },
  {
    name: 'TechSalvager', icon: '🤖', category: 'Salvage',
    desc: 'Processes salvage items recovered from Wasteland raids into RareTech.',
    effects: ['Queue salvage items for processing', 'Each item yields RareTech based on rarity', 'Uses 2 population per level'],
    prereqs: 'HQ 4, TechLab 2, ScrapForge 6, SolarArray 6',
  },
]

// ── RESEARCH ─────────────────────────────────────────────────────────────────
export const RESEARCH = [
  // ECONOMY
  { key: 'rationing_protocols', name: 'Rationing Protocols', branch: 'Economy', desc: 'Increases the efficiency of Food consumption and internal distribution.', techLab: 1, rt: 10, rp: 1, time: '30m', requires: [] },
  { key: 'water_recycling', name: 'Water Recycling', branch: 'Economy', desc: 'Increases the efficiency of Water processing in the settlement.', techLab: 2, rt: 14, rp: 1, time: '45m', requires: ['Rationing Protocols'] },
  { key: 'scrap_sorting', name: 'Scrap Sorting', branch: 'Economy', desc: 'Improves the yield of recovered Scrap materials.', techLab: 3, rt: 18, rp: 2, time: '60m', requires: [] },
  // MILITARY
  { key: 'improved_ballistics', name: 'Improved Ballistics', branch: 'Military', desc: 'Enhances Ballistic weapon platforms and ammunition output.', techLab: 2, rt: 16, rp: 1, time: '50m', requires: [] },
  { key: 'impact_plating', name: 'Impact Plating', branch: 'Military', desc: 'Increases protection against heavy Impact-type attacks.', techLab: 3, rt: 22, rp: 2, time: '70m', requires: [] },
  { key: 'energy_focusing', name: 'Energy Focusing', branch: 'Military', desc: 'Improves stability and output of Energy-based weapon systems.', techLab: 4, rt: 28, rp: 2, time: '90m', requires: ['Improved Ballistics'] },
  // DEFENSE
  { key: 'fortified_barriers', name: 'Fortified Barriers', branch: 'Defense', desc: 'Reinforces the structural efficiency of Perimeter Wall systems.', techLab: 3, rt: 24, rp: 2, time: '70m', requires: [] },
  { key: 'emergency_response_drills', name: 'Emergency Response Drills', branch: 'Defense', desc: 'Improves defense response time and internal combat readiness.', techLab: 4, rt: 26, rp: 2, time: '80m', requires: [] },
  // LOGISTICS
  { key: 'field_logistics', name: 'Field Logistics', branch: 'Logistics', desc: 'Improves transport coordination and operational deployment efficiency.', techLab: 4, rt: 30, rp: 2, time: '90m', requires: [] },
  { key: 'convoy_protocols', name: 'Convoy Protocols', branch: 'Logistics', desc: 'Unlocks and supports advanced Convoy operations.', techLab: 5, rt: 35, rp: 3, time: '120m', requires: ['Fortified Barriers', 'Improved Ballistics'] },
  // SALVAGE
  { key: 'salvage_optimization', name: 'Salvage Optimization', branch: 'Salvage', desc: 'Increases the efficiency of the Tech Salvager during standard operations.', techLab: 2, rt: 15, rp: 1, time: '40m', requires: [] },
  { key: 'datacore_reconstruction', name: 'Datacore Reconstruction', branch: 'Salvage', desc: 'Enables complex event and POI artefacts to be processed for research use. Requires a Military Datacore.', techLab: 5, rt: 40, rp: 3, time: '130m', requires: ['Salvage Optimization'], salvageItems: ['Military Datacore'] },
  // EXPANSION
  { key: 'settlement_coordination', name: 'Settlement Coordination', branch: 'Expansion', desc: 'Improves administrative coordination between multiple settlements.', techLab: 6, rt: 45, rp: 3, time: '150m', requires: ['Convoy Protocols', 'Field Logistics'] },
]

// ── SALVAGE ITEMS ─────────────────────────────────────────────────────────────
export const SALVAGE_ITEMS = [
  { name: 'Scrap Bundle',            icon: '⚙️', rarity: 'common',    rt: 1,  time: '1m 00s', desc: 'Assorted wasteland scrap with salvage potential.' },
  { name: 'Cracked Circuit Board',   icon: '🔌', rarity: 'common',    rt: 2,  time: '2m 00s', desc: 'Damaged pre-fall electronics board.' },
  { name: 'Burned Power Cell',       icon: '🔋', rarity: 'common',    rt: 2,  time: '2m 00s', desc: 'A discharged power cell recovered from ruins.' },
  { name: 'Fractured Optics Module', icon: '🔭', rarity: 'common',    rt: 3,  time: '3m 00s', desc: 'A damaged optical sensor from a pre-fall drone.' },
  { name: 'Damaged Servo Bundle',    icon: '🦾', rarity: 'uncommon',  rt: 5,  time: '4m 00s', desc: 'A set of degraded servo motors from old machinery.' },
  { name: 'Broken Drone Core',       icon: '🤖', rarity: 'uncommon',  rt: 6,  time: '5m 00s', desc: 'Core unit of a destroyed recon drone.' },
  { name: 'Pre-War Guidance Chip',   icon: '💾', rarity: 'uncommon',  rt: 7,  time: '5m 00s', desc: 'Intact guidance chip from pre-fall military hardware.' },
  { name: 'Reactor Fragment',        icon: '⚛️', rarity: 'rare',      rt: 5,  time: '10m 00s', desc: 'Unstable power shard from a collapsed industrial zone.' },
  { name: 'Encrypted Datacore',      icon: '📡', rarity: 'rare',      rt: 8,  time: '8m 00s', desc: 'Recovered archive core with fragmented technical records.' },
  { name: 'Ancient Data Core',       icon: '🗄️', rarity: 'rare',      rt: 10, time: '8m 00s', desc: 'High-density archive from a pre-fall facility.' },
  { name: 'Prototype Schematic',     icon: '📋', rarity: 'epic',      rt: 16, time: '12m 00s', desc: 'A damaged blueprint for advanced military engineering.' },
  { name: 'Vault Artifact',          icon: '🏺', rarity: 'legendary', rt: 24, time: '20m 00s', desc: 'Intact pre-fall relic from a sealed underground vault.' },
]

export const RESOURCE_INFO = [
  { key: 'Water',   icon: '💧', color: '#30a0ff', bg: '#102030', desc: 'Required by all infantry and some tech processes. Gathered by the Water Purifier.' },
  { key: 'Food',    icon: '🥫', color: '#30ff80', bg: '#1a3020', desc: 'Consumed by infantry units. Produced by the Farm Dome.' },
  { key: 'Scrap',   icon: '⚙️', color: '#ccaa44', bg: '#2a2010', desc: 'Core building material for vehicles, walls, and most upgrades. ScrapForge produces it.' },
  { key: 'Fuel',    icon: '⛽', color: '#ff6644', bg: '#281818', desc: 'Powering vehicles and Siege Carriers. Comes from the Fuel Refinery.' },
  { key: 'Energy',  icon: '⚡', color: '#aa80ff', bg: '#201830', desc: 'Advanced resource needed for Workshop units and late tech. Solar Array produces it.' },
  { key: 'RareTech',icon: '🔬', color: '#00d4ff', bg: '#0a2030', desc: 'Precious resource used for research and elite units. Obtained mainly from salvage raids.' },
]
