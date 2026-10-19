import test from 'node:test';
import assert from 'node:assert/strict';
import { createState, startGame, advanceGame, spawnEnemy, fireRepulsor, fireMissile, getNearestTarget, togglePause } from '../game.mjs';

test('flight advances and lateral/vertical inputs move the suit', () => {
  const s=createState(42); startGame(s); const z=s.player.z;
  advanceGame(s,{right:true,up:true},0.5);
  assert.ok(s.player.z>z); assert.ok(s.player.x>0); assert.ok(s.player.y>25);
});
test('boost consumes energy and increases forward speed',()=>{
  const a=createState(2),b=createState(2);startGame(a);startGame(b);
  advanceGame(a,{boost:true},0.5);advanceGame(b,{},0.5);
  assert.ok(a.player.z>b.player.z+5);assert.ok(a.player.energy<b.player.energy);
});
test('repulsor shots damage enemies and award points',()=>{
  const s=createState(3);startGame(s);s.spawnTimer=999;
  spawnEnemy(s,{x:0,y:25,z:40,hp:1,fireTimer:999});
  assert.equal(fireRepulsor(s),true);
  advanceGame(s,{},0.6);
  assert.equal(s.enemies.length,0); assert.ok(s.score>0);
});
test('missiles home on nearest target and consume energy',()=>{
 const s=createState(4);startGame(s);s.spawnTimer=999;
 const e=spawnEnemy(s,{x:8,y:27,z:95,hp:2,fireTimer:999});
 assert.equal(getNearestTarget(s).id,e.id);
 assert.equal(fireMissile(s),true);assert.ok(s.player.energy<100);
 advanceGame(s,{},1.5);
 assert.equal(s.enemies.length,0);
});
test('paused game freezes all motion',()=>{
 const s=createState();startGame(s);togglePause(s);const z=s.player.z;
 advanceGame(s,{boost:true,fire:true},1);
 assert.equal(s.player.z,z);assert.equal(s.shots.length,0);
});
test('altitude and frame delta remain bounded',()=>{
 const s=createState();startGame(s);
 advanceGame(s,{down:true},5);
 assert.ok(s.player.y>=6);assert.ok(Number.isFinite(s.player.z));
});
test('enemy fire can damage player and reach game over',()=>{
 const s=createState();startGame(s);s.spawnTimer=999;
 s.enemyShots.push({x:s.player.x,y:s.player.y,z:s.player.z+5,vx:0,vy:0,vz:-50,damage:110});
 advanceGame(s,{},0.4);
 assert.equal(s.status,'gameover');assert.equal(s.player.hp,0);
});
