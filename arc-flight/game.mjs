// Arc Flight v0.1 - deterministic, dependency-free game simulation.
export const clamp=(v,min,max)=>Math.max(min,Math.min(max,v));
const rand=s=>{s.seed=(Math.imul(s.seed,1664525)+1013904223)>>>0;return s.seed/4294967296;};
export function createState(seed=1234567){
 return {seed:seed>>>0, status:'ready', time:0, score:0, kills:0, wave:1,
 player:{x:0,y:25,z:0,hp:100,energy:100,vx:0,vy:0,speed:23},
 enemies:[],shots:[],enemyShots:[],particles:[],events:[],
 spawnTimer:1.5,cooldown:0,missileCooldown:0,enemyId:1,hand:1,combo:0,
 distance:0};
}
export function startGame(s){if(s.status==='ready'||s.status==='gameover')s.status='running';return s;}
export function togglePause(s){
 if(s.status==='running')s.status='paused';
 else if(s.status==='paused')s.status='running';
 return s.status;
}
export function spawnEnemy(s,opts={}){
 const e={id:s.enemyId++,x:opts.x??s.player.x+(rand(s)-.5)*55,y:opts.y??(15+rand(s)*30),z:opts.z??(s.player.z+110+rand(s)*70),hp:opts.hp??Math.min(5,1+Math.floor(s.wave/3)),fireTimer:opts.fireTimer??(1.3+rand(s)*2),phase:rand(s)*6.283,size:opts.size??2.5};
 s.enemies.push(e);return e;
}
export function getNearestTarget(s){
 let target=null, best=Infinity;
 for(const e of s.enemies){
  const dz=e.z-s.player.z,dx=e.x-s.player.x,dy=e.y-s.player.y;
  if(dz<0||dz>180||Math.abs(dx)>42||Math.abs(dy)>30)continue;
  const dist=dz+Math.abs(dx)*1.2+Math.abs(dy);
  if(dist<best){best=dist;target=e;}
 }
 return target;
}
export function fireRepulsor(s){
 if(s.status!=='running'||s.cooldown>0)return false;
 s.hand*=-1;
 s.shots.push({type:'repulsor',x:s.player.x+s.hand*.9,y:s.player.y+.1,z:s.player.z+4,vz:150,damage:1,ttl:1.25});
 s.cooldown=.14;
 s.events.push({type:'repulsor'});
 return true;
}
export function fireMissile(s){
 if(s.status!=='running'||s.missileCooldown>0||s.player.energy<20)return false;
 const target=getNearestTarget(s);
 if(!target)return false;
 s.shots.push({type:'missile',x:s.player.x,y:s.player.y+.8,z:s.player.z+4,vz:110,damage:3,ttl:2.5,targetId:target.id});
 s.player.energy-=20;s.missileCooldown=2.8;
 s.events.push({type:'missile'});return true;
}
function damageEnemy(s,e,n){
 e.hp-=n;
 s.events.push({type:'impact',x:e.x,y:e.y,z:e.z});
 if(e.hp<=0){
  s.enemies.splice(s.enemies.indexOf(e),1);
  s.kills++;s.combo++;s.score+=100+Math.min(500,s.combo*15);
  s.events.push({type:'destroy',x:e.x,y:e.y,z:e.z});
  if(s.kills%6===0){s.wave++;s.events.push({type:'wave',wave:s.wave});}
  if(s.kills%5===0){s.player.hp=clamp(s.player.hp+15,0,100);s.events.push({type:'repair'});}
 }
}
function oneStep(s,input,dt){
 s.time+=dt;
 s.cooldown=Math.max(0,s.cooldown-dt);s.missileCooldown=Math.max(0,s.missileCooldown-dt);
 const p=s.player;
 const boost=!!input.boost&&p.energy>1;
 p.speed=boost?52:23;
 p.energy=clamp(p.energy+(boost?-24:11)*dt,0,100);
 const ax=((input.right?1:0)-(input.left?1:0))*60;
 const ay=((input.up?1:0)-(input.down?1:0))*52;
 p.vx=clamp((p.vx+ax*dt)*Math.exp(-3.1*dt),-27,27);
 p.vy=clamp((p.vy+ay*dt)*Math.exp(-3.1*dt),-23,23);
 p.x=clamp(p.x+p.vx*dt,-100,100);
 p.y=clamp(p.y+p.vy*dt,6,62);
 p.z+=p.speed*dt;s.distance=p.z;
 if(input.fire)fireRepulsor(s);
 if(input.missile)fireMissile(s);
 s.spawnTimer-=dt;
 if(s.spawnTimer<=0){
  if(s.enemies.length<8)spawnEnemy(s);
  s.spawnTimer=clamp(2.7-s.wave*.12,1,2.7);
 }
 for(const e of s.enemies){
  e.phase+=dt*1.6;
  e.x+=Math.sin(e.phase)*dt*.65;
  if(e.z<p.z-35){s.enemies.splice(s.enemies.indexOf(e),1);s.combo=0;continue;}
  e.fireTimer-=dt;
  if(e.fireTimer<=0&&e.z-p.z<110&&e.z>p.z+15){
   const delta=Math.max(8,e.z-p.z),flight=delta/55;
   s.enemyShots.push({x:e.x,y:e.y,z:e.z,vx:(p.x-e.x)/flight,vy:(p.y-e.y)/flight,vz:-55,damage:12});
   e.fireTimer=2.1+rand(s)*1.5;
   s.events.push({type:'enemyShot'});
  }
 }
 for(let i=s.shots.length-1;i>=0;i--){
  const b=s.shots[i];const oldZ=b.z;
  b.z+=b.vz*dt;b.ttl-=dt;
  if(b.type==='missile'){
   const target=s.enemies.find(e=>e.id===b.targetId);
   if(target){const k=Math.min(1,dt*9);b.x+=(target.x-b.x)*k;b.y+=(target.y-b.y)*k;}
  }
  let hit=false;
  for(const e of [...s.enemies]){
   const crossed=b.z>=e.z-2&&oldZ<=e.z+2;
   if(crossed&&Math.hypot(b.x-e.x,b.y-e.y)<(b.type==='missile'?6:3.5)){
    damageEnemy(s,e,b.damage);hit=true;break;
   }
  }
  if(hit||b.ttl<=0)s.shots.splice(i,1);
 }
 for(let i=s.enemyShots.length-1;i>=0;i--){
  const b=s.enemyShots[i],oldZ=b.z;
  b.x+=b.vx*dt;b.y+=b.vy*dt;b.z+=b.vz*dt;
  if(oldZ>=p.z&&b.z<=p.z+3&&Math.hypot(b.x-p.x,b.y-p.y)<4){
   p.hp=clamp(p.hp-b.damage,0,100);
   s.events.push({type:'damage'});s.combo=0;s.enemyShots.splice(i,1);
   if(p.hp<=0){s.status='gameover';s.events.push({type:'gameover'});}
  }else if(b.z<p.z-12)s.enemyShots.splice(i,1);
 }
 if(s.shots.length>100)s.shots.splice(0,s.shots.length-100);
 if(s.enemyShots.length>100)s.enemyShots.splice(0,s.enemyShots.length-100);
}
export function advanceGame(s,input={},dt=1/60){
 if(s.status!=='running')return s;
 const duration=clamp(Number.isFinite(dt)?dt:0,0,2);
 const steps=Math.max(1,Math.ceil(duration/.025));
 for(let i=0;i<steps&&s.status==='running';i++)oneStep(s,input,duration/steps);
 return s;
}
