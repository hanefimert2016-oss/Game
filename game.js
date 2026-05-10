const ores = [
  { key: "copper", name: "Bakır", icon: "Cu", value: 9 },
  { key: "iron", name: "Demir", icon: "Fe", value: 12 },
  { key: "silver", name: "Gümüş", icon: "Ag", value: 26 },
  { key: "crystal", name: "Kristal", icon: "◇", value: 44 },
  { key: "gold", name: "Altın", icon: "Au", value: 70 },
];

const modes = {
  walk: { name: "Yaya", text: "Sessiz ve güvenli. Gizli damarları daha iyi bulur.", scan: 1, cost: 10, fuel: 0, luck: 0.12 },
  car: { name: "Araba", text: "Vadide hızlı keşif. Yolları daha verimli tarar.", scan: 2, cost: 14, fuel: 8, luck: 0.22 },
  train: { name: "Tren", text: "Ray hattında dev tarama. Büyük cevher bonusu verir.", scan: 3, cost: 18, fuel: 13, luck: 0.3 },
};

const state = {
  day: 1,
  energy: 100,
  fuel: 55,
  credits: 0,
  rep: 0,
  mode: "walk",
  selected: 0,
  inventory: Object.fromEntries(ores.map((ore) => [ore.key, 0])),
  cells: [],
};

const $ = (id) => document.getElementById(id);

function rng(seed) {
  let n = Math.sin(seed * 999) * 10000;
  return n - Math.floor(n);
}

function makeMap() {
  state.cells = Array.from({ length: 48 }, (_, i) => {
    const richness = Math.floor(rng(i + 7) * 4) + (i % 11 === 0 ? 2 : 0);
    return {
      id: i,
      ore: ores[Math.floor(rng(i + 21) * ores.length)],
      richness,
      discovered: i === 0,
      depleted: false,
      feature: i % 8 === 3 ? "rail" : i % 7 === 2 ? "road" : i % 17 === 0 ? "camp" : "",
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
  if (mode === "train" && cell.feature !== "rail") return "Tren sadece ray istasyonlarından tarama yapabilir.";
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
  const mode = state.mode;
  if (!spend(mode)) return;
  const config = modes[mode];
  const targets = neighbors(state.selected, config.scan).filter((cell) => !cell.discovered);
  const revealCount = Math.min(targets.length, Math.max(1, Math.ceil(targets.length * (0.45 + config.luck))));
  const revealed = targets
    .sort((a, b) => b.richness - a.richness)
    .slice(0, revealCount);
  revealed.forEach((cell) => {
    cell.discovered = true;
  });
  const found = revealed.filter((cell) => cell.richness > 3).length;
  log(revealCount ? `${modes[mode].name} taraması ${revealCount} bölge açtı${found ? `, ${found} zengin damar bulundu` : ""}.` : "Bu çevrede yeni bölge kalmadı.");
  render();
}

function mine() {
  const cell = state.cells[state.selected];
  if (!cell.discovered) return setStatus("Önce bu bölgeyi keşfet.");
  if (cell.depleted) return setStatus("Bu damar tükendi.");
  if (!spend(state.mode)) return;
  const modeBonus = state.mode === "train" ? 2 : state.mode === "car" && cell.feature === "road" ? 1 : 0;
  const amount = Math.max(1, Math.floor(cell.richness + 1 + modeBonus + Math.random() * 3));
  state.inventory[cell.ore.key] += amount;
  state.rep += cell.richness > 4 ? 3 : 1;
  cell.richness = Math.max(0, cell.richness - Math.ceil(amount / 2));
  cell.depleted = cell.richness === 0;
  log(`${amount} birim ${cell.ore.name} çıkarıldı.`);
  render();
}

function sell() {
  const total = ores.reduce((sum, ore) => sum + state.inventory[ore.key] * ore.value, 0);
  if (!total) return setStatus("Satılacak cevher yok.");
  ores.forEach((ore) => {
    state.inventory[ore.key] = 0;
  });
  state.credits += total;
  state.rep += Math.floor(total / 120);
  log(`Pazar satışı tamamlandı: ${total} kredi.`);
  render();
}

function rest() {
  const cell = state.cells[state.selected];
  const campBonus = cell.feature === "camp" ? 24 : 0;
  state.day += 1;
  state.energy = Math.min(100, state.energy + 34 + campBonus);
  state.fuel = Math.min(80, state.fuel + 12);
  log(campBonus ? "Kampta tamir ve dinlenme bonusu alındı." : "Ekip dinlendi, yakıt ikmali yapıldı.");
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

function renderMap() {
  $("map").innerHTML = state.cells
    .map((cell) => {
      const classes = ["cell", cell.feature, cell.discovered && "discovered", cell.richness > 4 && "rich", state.selected === cell.id && "selected"]
        .filter(Boolean)
        .join(" ");
      const label = cell.discovered ? (cell.depleted ? "Tükendi" : `${cell.ore.icon} · ${cell.richness}`) : "?";
      return `<button class="${classes}" data-cell="${cell.id}" aria-label="Bölge ${cell.id + 1}"><small>${label}</small></button>`;
    })
    .join("");
  document.querySelectorAll("[data-cell]").forEach((button) => {
    button.addEventListener("click", () => {
      state.selected = Number(button.dataset.cell);
      render();
    });
  });
}

function renderInventory() {
  $("inventory").innerHTML = ores
    .map((ore) => `<div class="ore"><span>${ore.name}</span><b>${state.inventory[ore.key]}</b></div>`)
    .join("");
}

function renderStats() {
  $("day").textContent = state.day;
  $("energy").textContent = state.energy;
  $("fuel").textContent = state.fuel;
  $("credits").textContent = state.credits;
  $("rep").textContent = state.rep;
}

function render() {
  renderStats();
  renderModes();
  renderMap();
  renderInventory();
  const cell = state.cells[state.selected];
  const warning = canUseMode(state.mode);
  setStatus(warning || `Seçili bölge: ${cell.discovered ? `${cell.ore.name} damarı` : "bilinmeyen arazi"}.`);
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
    inventory: Object.fromEntries(ores.map((ore) => [ore.key, 0])),
  });
  makeMap();
  $("log").innerHTML = "";
  log("Sefer başladı. İlk kamp bölgesi güvenli.");
  render();
}

$("scan").addEventListener("click", scan);
$("mine").addEventListener("click", mine);
$("sell").addEventListener("click", sell);
$("rest").addEventListener("click", rest);
$("newGame").addEventListener("click", newGame);

newGame();
