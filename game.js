const ores = [
  { key: "copper", name: "Bakır", icon: "Cu", value: 9, color: 0xc7773f },
  { key: "iron", name: "Demir", icon: "Fe", value: 12, color: 0x9aa3ad },
  { key: "silver", name: "Gümüş", icon: "Ag", value: 26, color: 0xd8ecff },
  { key: "crystal", name: "Kristal", icon: "◇", value: 44, color: 0x7efcff },
  { key: "gold", name: "Altın", icon: "Au", value: 70, color: 0xffc857 },
];

const fossils = [
  { key: "trilobite", name: "Trilobit Fosili", icon: "𖤐", value: 80, color: 0xffa6c1 },
  { key: "ammonite", name: "Ammonit Fosili", icon: "◎", value: 120, color: 0xff8f70 },
  { key: "dino", name: "Dinozor Kemiği", icon: "◇", value: 180, color: 0xf8ead0 },
];

const modes = {
  walk: { name: "Yaya", text: "Yakın alanı sessizce gezer, fosil şansı yüksektir.", scan: 1, cost: 10, fuel: 0, luck: 0.18 },
  car: { name: "Araba", text: "Yol hattında hızlı kazı ve geniş 3D tarama yapar.", scan: 2, cost: 14, fuel: 8, luck: 0.24 },
  train: { name: "Tren", text: "Raydan güçlü radar dalgası gönderir, nadir buluntu çıkarır.", scan: 3, cost: 18, fuel: 13, luck: 0.34 },
};

const state = {
  day: 1,
  energy: 100,
  fuel: 55,
  credits: 0,
  rep: 0,
  mode: "walk",
  selected: 0,
  inventory: {},
  cells: [],
};

const $ = (id) => document.getElementById(id);
const allFinds = [...ores, ...fossils];
let scene;
let camera;
let renderer;
let raycaster;
let pointer;
let clock;
let terrainGroup;
let markerGroup;
let actorGroup;

function rng(seed) {
  const n = Math.sin(seed * 999) * 10000;
  return n - Math.floor(n);
}

function makeMap() {
  state.cells = Array.from({ length: 64 }, (_, i) => {
    const roll = rng(i + 31);
    const fossil = roll > 0.76 ? fossils[Math.floor(rng(i + 43) * fossils.length)] : null;
    return {
      id: i,
      ore: ores[Math.floor(rng(i + 21) * ores.length)],
      fossil,
      richness: Math.floor(rng(i + 7) * 4) + (i % 11 === 0 ? 2 : 0),
      discovered: i === 0,
      depleted: false,
      feature: i % 8 === 3 ? "rail" : i % 7 === 2 ? "road" : i % 19 === 0 ? "camp" : "",
    };
  });
}

function log(text) {
  const item = document.createElement("li");
  item.textContent = text;
  $("log").prepend(item);
  while ($("log").children.length > 8) $("log").lastChild.remove();
}

function setStatus(text) {
  $("status").textContent = text;
}

function canUseMode(mode) {
  const cell = state.cells[state.selected];
  if (mode === "train" && cell.feature !== "rail") return "Tren sadece 3D ray istasyonlarından tarama yapabilir.";
  if (mode === "car" && state.fuel < modes.car.fuel) return "Araba için yakıt yetersiz.";
  if (mode === "train" && state.fuel < modes.train.fuel) return "Tren için yakıt yetersiz.";
  return "";
}

function spend(mode) {
  const config = modes[mode];
  if (state.energy < config.cost) {
    setStatus("Enerji yetersiz. Kampta dinlen.");
    return false;
  }
  const reason = canUseMode(mode);
  if (reason) {
    setStatus(reason);
    return false;
  }
  state.energy -= config.cost;
  state.fuel -= config.fuel;
  state.day += 1;
  return true;
}

function neighbors(id, range) {
  const width = 8;
  const row = Math.floor(id / width);
  const col = id % width;
  return state.cells.filter((cell) => {
    const distance = Math.abs(Math.floor(cell.id / width) - row) + Math.abs((cell.id % width) - col);
    return distance > 0 && distance <= range;
  });
}

function scan() {
  if (!spend(state.mode)) return;
  const config = modes[state.mode];
  const targets = neighbors(state.selected, config.scan).filter((cell) => !cell.discovered);
  const revealCount = Math.min(targets.length, Math.max(1, Math.ceil(targets.length * (0.45 + config.luck))));
  const revealed = targets.sort((a, b) => b.richness + Number(Boolean(b.fossil)) - a.richness - Number(Boolean(a.fossil))).slice(0, revealCount);
  revealed.forEach((cell) => {
    cell.discovered = true;
  });
  const fossilsFound = revealed.filter((cell) => cell.fossil).length;
  log(revealCount ? `${modes[state.mode].name} 3D taraması ${revealCount} alan açtı${fossilsFound ? `, ${fossilsFound} fosil izi buldu` : ""}.` : "Bu çevrede yeni 3D alan kalmadı.");
  render();
}

function mine() {
  const cell = state.cells[state.selected];
  if (!cell.discovered) return setStatus("Önce bu 3D alanı keşfet.");
  if (cell.depleted) return setStatus("Bu alan tükendi.");
  if (!spend(state.mode)) return;
  const modeBonus = state.mode === "train" ? 2 : state.mode === "car" && cell.feature === "road" ? 1 : 0;
  const amount = Math.max(1, Math.floor(cell.richness + 1 + modeBonus + Math.random() * 3));
  state.inventory[cell.ore.key] += amount;
  let message = `${amount} birim ${cell.ore.name} çıkarıldı`;
  if (cell.fossil) {
    state.inventory[cell.fossil.key] += 1;
    state.rep += 6;
    message += ` ve ${cell.fossil.name} bulundu`;
    cell.fossil = null;
  }
  state.rep += cell.richness > 4 ? 3 : 1;
  cell.richness = Math.max(0, cell.richness - Math.ceil(amount / 2));
  cell.depleted = cell.richness === 0 && !cell.fossil;
  log(`${message}.`);
  render();
}

function sell() {
  const total = allFinds.reduce((sum, item) => sum + state.inventory[item.key] * item.value, 0);
  if (!total) return setStatus("Satılacak maden veya fosil yok.");
  allFinds.forEach((item) => {
    state.inventory[item.key] = 0;
  });
  state.credits += total;
  state.rep += Math.floor(total / 100);
  log(`Müze ve pazar satışı tamamlandı: ${total} kredi.`);
  render();
}

function rest() {
  const cell = state.cells[state.selected];
  const campBonus = cell.feature === "camp" ? 24 : 0;
  state.day += 1;
  state.energy = Math.min(100, state.energy + 34 + campBonus);
  state.fuel = Math.min(80, state.fuel + 12);
  log(campBonus ? "3D kampta tamir ve dinlenme bonusu alındı." : "Ekip dinlendi, yakıt ikmali yapıldı.");
  render();
}

function renderModes() {
  $("modes").innerHTML = Object.entries(modes)
    .map(
      ([key, mode]) => `
      <button class="mode ${state.mode === key ? "active" : ""}" data-mode="${key}">
        <b>${mode.name}</b><p>${mode.text}</p>
      </button>`,
    )
    .join("");
  document.querySelectorAll("[data-mode]").forEach((button) => {
    button.addEventListener("click", () => {
      state.mode = button.dataset.mode;
      render();
    });
  });
}

function renderInventory() {
  $("inventory").innerHTML = allFinds
    .map((item) => `<div class="ore ${fossils.includes(item) ? "fossil" : ""}"><span>${item.name}</span><b>${state.inventory[item.key]}</b></div>`)
    .join("");
}

function renderStats() {
  $("day").textContent = state.day;
  $("energy").textContent = state.energy;
  $("fuel").textContent = state.fuel;
  $("credits").textContent = state.credits;
  $("rep").textContent = state.rep;
}

function cellPosition(id) {
  const width = 8;
  return { x: (id % width - 3.5) * 2.25, z: (Math.floor(id / width) - 3.5) * 2.25 };
}

function material(color, roughness = 0.72, metalness = 0.05) {
  return new THREE.MeshStandardMaterial({ color, roughness, metalness });
}

function addBox(group, color, size, position, name = "") {
  const mesh = new THREE.Mesh(new THREE.BoxGeometry(size.x, size.y, size.z), material(color));
  mesh.position.set(position.x, position.y, position.z);
  mesh.castShadow = true;
  mesh.receiveShadow = true;
  mesh.name = name;
  group.add(mesh);
  return mesh;
}

function addTerrainCell(cell) {
  const pos = cellPosition(cell.id);
  const height = 0.1 + cell.richness * 0.035 + rng(cell.id + 12) * 0.12;
  const color = cell.discovered ? (cell.depleted ? 0x2a2f35 : 0x3a5f49) : 0x1b2534;
  const tile = addBox(terrainGroup, color, { x: 2, y: height, z: 2 }, { x: pos.x, y: height / 2, z: pos.z }, `cell-${cell.id}`);
  tile.userData.cellId = cell.id;
  if (cell.id === state.selected) {
    const ring = new THREE.Mesh(new THREE.TorusGeometry(1.18, 0.045, 8, 36), material(0xffc857, 0.35, 0.15));
    ring.position.set(pos.x, 0.2, pos.z);
    ring.rotation.x = Math.PI / 2;
    markerGroup.add(ring);
  }
  if (cell.feature === "rail") addRail(pos);
  if (cell.feature === "road") addRoad(pos);
  if (cell.feature === "camp") addCamp(pos);
  if (cell.discovered && !cell.depleted) addFindMarkers(cell, pos);
}

function addRail(pos) {
  addBox(markerGroup, 0x70e3ff, { x: 1.65, y: 0.06, z: 0.12 }, { x: pos.x, y: 0.22, z: pos.z - 0.34 });
  addBox(markerGroup, 0x70e3ff, { x: 1.65, y: 0.06, z: 0.12 }, { x: pos.x, y: 0.22, z: pos.z + 0.34 });
  for (let i = -2; i <= 2; i += 1) addBox(markerGroup, 0x40566a, { x: 0.12, y: 0.08, z: 1.1 }, { x: pos.x + i * 0.38, y: 0.24, z: pos.z });
}

function addRoad(pos) {
  addBox(markerGroup, 0x2f4a43, { x: 1.72, y: 0.045, z: 1.72 }, { x: pos.x, y: 0.2, z: pos.z });
}

function addCamp(pos) {
  const tent = new THREE.Mesh(new THREE.ConeGeometry(0.5, 0.85, 4), material(0xffc857));
  tent.position.set(pos.x, 0.72, pos.z);
  tent.rotation.y = Math.PI / 4;
  tent.castShadow = true;
  markerGroup.add(tent);
}

function addFindMarkers(cell, pos) {
  const ore = new THREE.Mesh(new THREE.OctahedronGeometry(0.28 + cell.richness * 0.02), material(cell.ore.color, 0.4, 0.25));
  ore.position.set(pos.x - 0.45, 0.5, pos.z + 0.35);
  ore.castShadow = true;
  ore.userData.spin = 0.012;
  markerGroup.add(ore);
  if (cell.fossil) {
    const fossil = new THREE.Mesh(new THREE.TorusKnotGeometry(0.22, 0.06, 42, 8), material(cell.fossil.color, 0.55, 0.02));
    fossil.position.set(pos.x + 0.45, 0.52, pos.z - 0.35);
    fossil.castShadow = true;
    fossil.userData.spin = 0.02;
    markerGroup.add(fossil);
  }
}

function addActors() {
  const pos = cellPosition(state.selected);
  actorGroup.clear();
  const miner = addBox(actorGroup, 0xf8ead0, { x: 0.22, y: 0.65, z: 0.22 }, { x: pos.x, y: 0.7, z: pos.z }, "miner");
  miner.add(new THREE.Mesh(new THREE.SphereGeometry(0.18), material(0xffd1a1)));
  miner.children[0].position.y = 0.48;
  if (state.mode === "car") addBox(actorGroup, 0x6ff0a7, { x: 0.9, y: 0.34, z: 0.55 }, { x: pos.x + 0.65, y: 0.42, z: pos.z + 0.55 }, "car");
  if (state.mode === "train") {
    addBox(actorGroup, 0x52dcff, { x: 1.15, y: 0.5, z: 0.62 }, { x: pos.x + 0.75, y: 0.48, z: pos.z }, "train");
    addBox(actorGroup, 0x355d78, { x: 0.62, y: 0.42, z: 0.58 }, { x: pos.x + 1.55, y: 0.44, z: pos.z }, "wagon");
  }
}

function rebuildScene() {
  terrainGroup.clear();
  markerGroup.clear();
  state.cells.forEach(addTerrainCell);
  addActors();
}

function renderScene() {
  if (!renderer) return;
  rebuildScene();
  renderer.render(scene, camera);
}

function render() {
  renderStats();
  renderModes();
  renderInventory();
  const cell = state.cells[state.selected];
  const warning = canUseMode(state.mode);
  const target = cell.fossil ? `${cell.ore.name} + ${cell.fossil.name}` : `${cell.ore.name} damarı`;
  setStatus(warning || `Seçili 3D alan: ${cell.discovered ? target : "bilinmeyen arazi"}.`);
  renderScene();
}

function newGame() {
  Object.assign(state, {
    day: 1,
    energy: 100,
    fuel: 55,
    credits: 0,
    rep: 0,
    mode: "walk",
    selected: 0,
    inventory: Object.fromEntries(allFinds.map((item) => [item.key, 0])),
  });
  makeMap();
  $("log").innerHTML = "";
  log("3D sefer başladı. İlk kamp bölgesi güvenli.");
  render();
}

function initScene() {
  const canvas = $("scene");
  scene = new THREE.Scene();
  scene.background = new THREE.Color(0x07101a);
  scene.fog = new THREE.Fog(0x07101a, 18, 34);
  camera = new THREE.PerspectiveCamera(48, canvas.clientWidth / canvas.clientHeight, 0.1, 100);
  camera.position.set(8.5, 11, 13);
  camera.lookAt(0, 0, 0);
  renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
  renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
  renderer.setSize(canvas.clientWidth, canvas.clientHeight, false);
  renderer.shadowMap.enabled = true;
  clock = new THREE.Clock();
  raycaster = new THREE.Raycaster();
  pointer = new THREE.Vector2();
  terrainGroup = new THREE.Group();
  markerGroup = new THREE.Group();
  actorGroup = new THREE.Group();
  scene.add(terrainGroup, markerGroup, actorGroup);
  scene.add(new THREE.HemisphereLight(0xbdefff, 0x243316, 1.9));
  const sun = new THREE.DirectionalLight(0xffffff, 2.2);
  sun.position.set(8, 14, 8);
  sun.castShadow = true;
  scene.add(sun);
  const ground = new THREE.Mesh(new THREE.PlaneGeometry(28, 28), material(0x102015));
  ground.rotation.x = -Math.PI / 2;
  ground.receiveShadow = true;
  scene.add(ground);
  canvas.addEventListener("click", selectFromScene);
  window.addEventListener("resize", resizeScene);
  animate();
}

function resizeScene() {
  const canvas = $("scene");
  camera.aspect = canvas.clientWidth / canvas.clientHeight;
  camera.updateProjectionMatrix();
  renderer.setSize(canvas.clientWidth, canvas.clientHeight, false);
  renderScene();
}

function selectFromScene(event) {
  const rect = renderer.domElement.getBoundingClientRect();
  pointer.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
  pointer.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
  raycaster.setFromCamera(pointer, camera);
  const hit = raycaster.intersectObjects(terrainGroup.children).find((item) => Number.isInteger(item.object.userData.cellId));
  if (!hit) return;
  state.selected = hit.object.userData.cellId;
  render();
}

function animate() {
  requestAnimationFrame(animate);
  const time = clock.getElapsedTime();
  actorGroup.position.y = Math.sin(time * 2.4) * 0.035;
  markerGroup.children.forEach((child) => {
    if (!child.userData.spin) return;
    child.rotation.y += child.userData.spin;
    child.position.y += Math.sin(time * 3 + child.position.x) * 0.0009;
  });
  renderer.render(scene, camera);
}

$("scan").addEventListener("click", scan);
$("mine").addEventListener("click", mine);
$("sell").addEventListener("click", sell);
$("rest").addEventListener("click", rest);
$("newGame").addEventListener("click", newGame);

initScene();
newGame();
