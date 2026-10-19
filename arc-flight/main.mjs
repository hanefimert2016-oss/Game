import {createState,startGame,advanceGame,togglePause,getNearestTarget} from './game.mjs';

const canvas=document.querySelector('#game');
const ctx=canvas.getContext('2d',{alpha:false});
const byId=id=>document.getElementById(id);
const overlay=byId('overlay'),startButton=byId('start'),intro=byId('intro');
let game=createState(),W=1280,H=720,DPR=1,last=0,frame=0;
const held=new Set(),touch=new Set(),particles=[],scene=[];
const camera={x:0,y:32,z:-22};
let toastTimer=0,hitFlash=0,shotFlash=0,muted=false,audio=null;
const PI=Math.PI;
const palette={wall:'#17354b',roof:'#204963',side:'#0b2539',road:'#081928',line:'#1d5270'};
const clip=(x,a,b)=>Math.max(a,Math.min(b,x));
const hash=(a,b,k=1)=>{const v=Math.sin((a*121.41+b*311.71+k*97.13)*.37)*43758.5453;return v-Math.floor(v)};
const input=()=>({
 left:held.has('KeyA')||held.has('ArrowLeft')||touch.has('left'),
 right:held.has('KeyD')||held.has('ArrowRight')||touch.has('right'),
 up:held.has('Space')||held.has('ArrowUp')||held.has('KeyW')||touch.has('up'),
 down:held.has('KeyC')||held.has('ArrowDown')||held.has('KeyS')||touch.has('down'),
 boost:held.has('ShiftLeft')||held.has('ShiftRight')||touch.has('boost'),
 fire:held.has('KeyF')||held.has('MouseFire')||touch.has('fire'),
 missile:held.has('KeyE')||touch.has('missile')
});
function resize(){
 DPR=Math.min(devicePixelRatio||1,1.6);W=window.innerWidth;H=window.innerHeight;
 canvas.width=Math.ceil(W*DPR);canvas.height=Math.ceil(H*DPR);ctx.setTransform(DPR,0,0,DPR,0,0);
}
window.addEventListener('resize',resize);resize();
window.addEventListener('keydown',e=>{
 if(['Space','ArrowUp','ArrowDown','ArrowLeft','ArrowRight'].includes(e.code))e.preventDefault();
 if(e.code==='KeyP'&&!e.repeat){pause();}
 if(e.code==='KeyR'&&!e.repeat&&game.status==='gameover')newGame();
 if(e.code==='KeyM'&&!e.repeat)muted=!muted;
 held.add(e.code);
});
window.addEventListener('keyup',e=>held.delete(e.code));
window.addEventListener('blur',()=>{held.clear();touch.clear();if(game.status==='running')pause();});
canvas.addEventListener('pointerdown',e=>{if(game.status==='running'){held.add('MouseFire');canvas.setPointerCapture?.(e.pointerId);}});
canvas.addEventListener('pointerup',()=>held.delete('MouseFire'));
canvas.addEventListener('pointercancel',()=>held.delete('MouseFire'));
for(const el of document.querySelectorAll('.tbtn')){
 const key=el.dataset.btn;
 el.addEventListener('pointerdown',e=>{e.preventDefault();el.setPointerCapture(e.pointerId);touch.add(key);el.classList.add('on');});
 const done=()=>{touch.delete(key);el.classList.remove('on');};
 el.addEventListener('pointerup',done);el.addEventListener('pointercancel',done);el.addEventListener('lostpointercapture',done);
}
byId('pause').addEventListener('click',pause);
startButton.addEventListener('click',()=>{if(game.status==='paused'){pause();return;}newGame();});
function newGame(){
 game=createState((Math.random()*1e9)>>>0);startGame(game);overlay.style.display='none';held.clear();touch.clear();
 spawnMessage('FLIGHT SYSTEM ONLINE',1.5);last=performance.now();
}
function pause(){
 if(game.status!=='running'&&game.status!=='paused')return;
 togglePause(game);
 if(game.status==='paused'){startButton.textContent='RESUME FLIGHT';intro.textContent='Sistem duraklatıldı. Uçuşa devam etmek için düğmeye bas.';overlay.style.display='grid';}
 else {overlay.style.display='none';last=performance.now();}
}
function spawnMessage(msg,t=1){byId('toast').textContent=msg;toastTimer=t;byId('toast').style.opacity='1';}
function beep(frequency=450,duration=.04,wave='sawtooth',volume=.025){
 if(muted)return;
 try{
  audio??=new (window.AudioContext||window.webkitAudioContext)();
  if(audio.state==='suspended')audio.resume();
  const o=audio.createOscillator(),g=audio.createGain(),now=audio.currentTime;
  o.type=wave;o.frequency.setValueAtTime(frequency,now);o.frequency.exponentialRampToValueAtTime(Math.max(60,frequency*.48),now+duration);
  g.gain.setValueAtTime(volume,now);g.gain.exponentialRampToValueAtTime(.0001,now+duration);
  o.connect(g);g.connect(audio.destination);o.start(now);o.stop(now+duration+.004);
 }catch{}
}
function processEvents(){
 for(const ev of game.events){
  if(ev.type==='repulsor'){shotFlash=.12;beep(700,.045,'sawtooth',.012);}
  if(ev.type==='missile'){beep(300,.25,'triangle',.05);spawnMessage('MISSILE AWAY',.9);}
  if(ev.type==='damage'){hitFlash=.35;beep(160,.22,'sawtooth',.07);}
  if(ev.type==='destroy'){
   for(let i=0;i<28;i++){const a=Math.random()*PI*2,b=Math.random()*PI*2,sp=5+Math.random()*24;
    particles.push({x:ev.x,y:ev.y,z:ev.z,vx:Math.cos(a)*sp,vy:Math.sin(a)*sp,vz:Math.cos(b)*sp,life:.5+Math.random()*.5,hue:i%3===0?185:25});}
   beep(105,.23,'square',.045);
  }
  if(ev.type==='wave')spawnMessage('WAVE '+String(ev.wave).padStart(2,'0')+' INBOUND',2);
  if(ev.type==='repair')spawnMessage('+15 ARMOR REPAIRED',1.5);
  if(ev.type==='gameover'){
   overlay.style.display='grid';startButton.textContent='RESTART MISSION';
   intro.textContent='Uçuş sona erdi. Skor: '+game.score.toLocaleString('en-US')+' • Eliminasyon: '+game.kills;
  }
 }
 game.events.length=0;
}
function project(x,y,z){
 const dx=x-camera.x,dy=y-camera.y,dz=z-camera.z;
 const pitch=.15;
 const depth=dz*Math.cos(pitch)-dy*Math.sin(pitch);
 if(depth<=1.5)return null;
 const vertical=dy*Math.cos(pitch)+dz*Math.sin(pitch);
 const f=Math.min(W*1.2,H*1.05);
 const sc=f/depth;
 return {x:W*.5+dx*sc,y:H*.49-vertical*sc,d:depth,s:sc};
}
function polygon(points,fill,stroke=null,alpha=1){
 const pts=points.map(p=>project(...p));
 if(pts.some(p=>!p))return;
 const depth=pts.reduce((a,p)=>a+p.d,0)/pts.length;
 if(pts.every(p=>p.x<-W*.4)||pts.every(p=>p.x>W*1.4)||pts.every(p=>p.y<-H*.5)||pts.every(p=>p.y>H*1.5))return;
 scene.push({depth,pts,fill,stroke,alpha});
}
function cube(x,y,z,w,h,d,col,variant=0){
 const x0=x-w/2,x1=x+w/2,z0=z-d/2,z1=z+d/2,y0=y-h/2,y1=y+h/2;
 const P=[
 [x0,y0,z0],[x1,y0,z0],[x1,y1,z0],[x0,y1,z0],
 [x0,y0,z1],[x1,y0,z1],[x1,y1,z1],[x0,y1,z1]];
 const shade=Array.isArray(col)?col:[col,col,col,col];
 polygon([P[4],P[5],P[6],P[7]],shade[2]);  // rear
 polygon([P[0],P[4],P[7],P[3]],shade[1]);  // left
 polygon([P[1],P[5],P[6],P[2]],shade[1]);  // right
 polygon([P[0],P[1],P[2],P[3]],shade[0]);  // front
 polygon([P[3],P[2],P[6],P[7]],shade[3]||shade[0]); // roof
}
function segment(x1,y1,z1,x2,y2,z2,color,width=1,alpha=1){
 const a=project(x1,y1,z1),b=project(x2,y2,z2);if(!a||!b)return;
 ctx.globalAlpha=alpha;ctx.strokeStyle=color;ctx.lineWidth=width;
 ctx.beginPath();ctx.moveTo(a.x,a.y);ctx.lineTo(b.x,b.y);ctx.stroke();ctx.globalAlpha=1;
}
function glow(x,y,z,radius,color){
 const p=project(x,y,z);if(!p||p.x<-50||p.x>W+50||p.y<-50||p.y>H+50)return;
 const r=clip(radius*p.s,2,60);
 const g=ctx.createRadialGradient(p.x,p.y,0,p.x,p.y,r);
 g.addColorStop(0,color);g.addColorStop(.24,color+'aa');g.addColorStop(1,color+'00');
 ctx.fillStyle=g;ctx.beginPath();ctx.arc(p.x,p.y,r,0,PI*2);ctx.fill();
}
function sky(){
 let bg=ctx.createLinearGradient(0,0,0,H);bg.addColorStop(0,'#020a1b');bg.addColorStop(.55,'#0b2840');bg.addColorStop(1,'#21465c');
 ctx.fillStyle=bg;ctx.fillRect(0,0,W,H);
 const sunX=W*.79,sunY=H*.24;const g=ctx.createRadialGradient(sunX,sunY,0,sunX,sunY,Math.max(W,H)*.42);
 g.addColorStop(0,'#3698b344');g.addColorStop(.35,'#246f8e21');g.addColorStop(1,'#12334a00');ctx.fillStyle=g;ctx.fillRect(0,0,W,H);
 ctx.fillStyle='#e1ffff';for(let i=0;i<60;i++){const xx=hash(i,5)*W,yy=hash(i,8)*H*.47;ctx.globalAlpha=.09+hash(i,11)*.38;ctx.fillRect(xx,yy,1.3,1.3);}ctx.globalAlpha=1;
}
function ground(){
 const far=240;const startZ=Math.floor((camera.z+3)/20)*20;
 ctx.fillStyle='#0a1c2d';
 const a=project(camera.x-450,0,camera.z+3),b=project(camera.x+450,0,camera.z+3);
 if(a&&b){ctx.fillRect(0,Math.max(a.y,b.y),W,H);}
 for(let z=startZ;z<=camera.z+far;z+=20){
  segment(camera.x-150,0,z,camera.x+150,0,z,'#2c6680',1,z>camera.z+150?.20:.32);
 }
 for(let ix=Math.floor((camera.x-150)/20);ix<=Math.ceil((camera.x+150)/20);ix++){
  segment(ix*20,0,camera.z+3,ix*20,0,camera.z+far,'#27566a',1,.25);
 }
 // Cyan transit corridors in the city.
 for(let ix=Math.floor((camera.x-100)/80);ix<=Math.ceil((camera.x+100)/80);ix++){
  segment(ix*80,0,camera.z+4,ix*80,0,camera.z+far,'#07afd0',2,.28);
 }
}
function city(){
 const minX=Math.floor((camera.x-105)/20),maxX=Math.ceil((camera.x+105)/20);
 const minZ=Math.floor((camera.z+10)/20),maxZ=Math.ceil((camera.z+240)/20);
 for(let iz=minZ;iz<=maxZ;iz++){
  for(let ix=minX;ix<=maxX;ix++){
   const v=hash(ix,iz);
   if(v<.24||ix%5===0||iz%6===0)continue;
   const h=7+Math.pow(hash(ix,iz,2),1.3)*43,w=11+hash(ix,iz,3)*5,d=12+hash(ix,iz,4)*4;
   const cx=ix*20,cz=iz*20;
   const tint=Math.round(hash(ix,iz,7)*26);
   cube(cx,h/2,cz,w,h,d,['rgb('+(19+tint)+','+(49+tint)+','+(70+tint)+')','#0e2d45','#112b41','#2b5a71']);
   if(cz-camera.z<120&&h>13){
    const rows=Math.min(6,Math.floor(h/5));
    for(let row=1;row<=rows;row++){
     for(let k=-1;k<=1;k++){
      if(hash(ix*12+row,iz*15+k,6)>.35){
       const color=hash(ix+row,iz+k,10)>.55?'#34adcc':'#d89c54';
       const yw=3.5+row*4.2,xw=cx+k*(w*.23),zw=cz-d/2-.035;
       polygon([[xw-.5,yw,zw],[xw+.5,yw,zw],[xw+.5,yw+1.25,zw],[xw-.5,yw+1.25,zw]],color,null,.55);
      }
     }
    }
   }
  }
 }
}
function drones(){
 for(const e of game.enemies){
  const z=e.z,y=e.y+Math.sin(game.time*2+e.phase)*.7,x=e.x;
  cube(x,y,z,4,2.3,3.8,['#9d3846','#4e2538','#632936','#c55c52']);
  cube(x-3.7,y-.3,z-.1,4.5,.5,1.7,['#a23a4f','#661b36','#711d38','#c65f58']);
  cube(x+3.7,y-.3,z-.1,4.5,.5,1.7,['#a23a4f','#661b36','#711d38','#c65f58']);
  glow(x,y,z-2.05,.8,'#ff465b');
  glow(x-5.3,y-.3,z-.8,.7,'#ffae4d');
  glow(x+5.3,y-.3,z-.8,.7,'#ffae4d');
 }
}
function suit(){
 const p=game.player,bob=Math.sin(game.time*10)*.13,boost=input().boost;
 const x=p.x,y=p.y+bob,z=p.z+6;
 const armor=['#b8343c','#731927','#942a34','#e6a84a'];
 const gold=['#eab45b','#9c5038','#b57c47','#ffe39c'];
 // Dynamic thruster cones and luminous jets.
 const jet=boost?2.3:1.5;
 for(const dx of [-1.25,1.25]){
  polygon([[x+dx-.56,y-2.6,z-.7],[x+dx+.56,y-2.6,z-.7],[x+dx,y-3.6*jet,z-3.8*jet]],'#00dfff',null,.54);
  glow(x+dx,y-2.2,z-2.2,1.15,'#58efff');
 }
 cube(x-.85,y-1.75,z-.1,1.4,3,2,armor);
 cube(x+.85,y-1.75,z-.1,1.4,3,2,armor);
 cube(x,y+.7,z,3.3,3.6,2.1,armor);
 cube(x,y+3.08,z+.15,2.25,2.1,1.8,gold);
 cube(x-2.45,y+.67,z-.05,1.3,2.7,1.3,armor);
 cube(x+2.45,y+.67,z-.05,1.3,2.7,1.3,armor);
 cube(x-2.55,y-.65,z-.1,1.3,1.6,1.3,gold);
 cube(x+2.55,y-.65,z-.1,1.3,1.6,1.3,gold);
 cube(x,y+.8,z+1.12,.95,1.2,.18,['#52feff','#23b7d5','#2bc9e4','#bfffff']);
 glow(x,y+.7,z+1.22,1.5,'#57eeff');
 glow(x-2.5,y-1.5,z-.8,.8,'#65f6ff');glow(x+2.5,y-1.5,z-.8,.8,'#65f6ff');
}
function renderScene(){
 scene.length=0;
 city();drones();suit();
 scene.sort((a,b)=>b.depth-a.depth);
 for(const o of scene){
  ctx.globalAlpha=o.alpha;ctx.fillStyle=o.fill;
  ctx.beginPath();ctx.moveTo(o.pts[0].x,o.pts[0].y);
  for(let i=1;i<o.pts.length;i++)ctx.lineTo(o.pts[i].x,o.pts[i].y);
  ctx.closePath();ctx.fill();
  if(o.stroke){ctx.strokeStyle=o.stroke;ctx.stroke();}
 }ctx.globalAlpha=1;
 // Light sources are painted after faces.
 for(const e of game.enemies){glow(e.x,e.y,e.z-2,.65,'#ff344b');}
 for(const shot of game.shots){
  const p=project(shot.x,shot.y,shot.z);if(!p)continue;
  const radius=shot.type==='missile'?6:3;
  ctx.fillStyle=shot.type==='missile'?'#ffb56d':'#a9ffff';
  ctx.shadowBlur=22;ctx.shadowColor=ctx.fillStyle;
  ctx.beginPath();ctx.arc(p.x,p.y,clip(radius*p.s*.15,2,10),0,2*PI);ctx.fill();ctx.shadowBlur=0;
 }
 for(const shot of game.enemyShots){
  const p=project(shot.x,shot.y,shot.z);if(!p)continue;
  ctx.fillStyle='#ff526b';ctx.shadowBlur=16;ctx.shadowColor='#fc4561';
  ctx.beginPath();ctx.arc(p.x,p.y,4,0,PI*2);ctx.fill();ctx.shadowBlur=0;
 }
 for(const part of particles){
  const p=project(part.x,part.y,part.z);if(!p)continue;
  ctx.globalAlpha=Math.min(1,part.life*1.7);ctx.fillStyle=part.hue===185?'#5afaff':'#ffaf61';
  ctx.fillRect(p.x,p.y,Math.max(2,p.s*.12),Math.max(2,p.s*.12));
 }ctx.globalAlpha=1;
 const p=game.player;
 glow(p.x,p.y+.65,p.z+7.7,1.3,'#8dfbff');
 // Velocity streaks at high speed.
 if(p.speed>40){
  ctx.strokeStyle='#51f3ff30';ctx.lineWidth=1.2;
  for(let i=0;i<20;i++){let x=hash(i,2,Math.floor(game.time))*W,y=hash(i,9,Math.floor(game.time))*H;ctx.beginPath();ctx.moveTo(x,y);ctx.lineTo(x-9,y+30);ctx.stroke();}
 }
}
function paintHUD(dt){
 byId('hpbar').style.width=game.player.hp+'%';byId('hpnum').textContent=Math.round(game.player.hp)+'%';
 byId('engbar').style.width=game.player.energy+'%';byId('engnum').textContent=Math.round(game.player.energy)+'%';
 byId('speed').textContent=String(Math.round(game.player.speed)).padStart(3,'0');
 byId('alt').textContent=String(Math.round(game.player.y)).padStart(3,'0');
 byId('score').textContent=String(game.score).padStart(6,'0');
 byId('kills').textContent=String(game.kills).padStart(2,'0');
 byId('wave').textContent=String(game.wave).padStart(2,'0');
 byId('hostiles').textContent=String(game.enemies.length);
 const target=getNearestTarget(game);
 byId('lock').textContent=target?'◈ TARGET LOCK // E':'NO MISSILE LOCK';
 byId('lock').style.color=target?'#83fbec':'#8eb1c3';
 if(toastTimer>0){toastTimer-=dt;if(toastTimer<=0)byId('toast').style.opacity='0';}
}
function draw(dt){
 sky();ground();
 camera.x+=(game.player.x-camera.x)*Math.min(1,dt*6);
 camera.y+=(game.player.y+5-camera.y)*Math.min(1,dt*6);
 camera.z=game.player.z-22;
 renderScene();
 // Vignette.
 const vignette=ctx.createRadialGradient(W*.5,H*.5,H*.12,W*.5,H*.5,W*.7);
 vignette.addColorStop(0,'#03091500');vignette.addColorStop(1,'#010510aa');ctx.fillStyle=vignette;ctx.fillRect(0,0,W,H);
 if(hitFlash>0){ctx.fillStyle='rgba(255,37,65,'+(hitFlash*.65)+')';ctx.fillRect(0,0,W,H);hitFlash=Math.max(0,hitFlash-dt);}
 if(shotFlash>0)shotFlash=Math.max(0,shotFlash-dt);
 // Fake angular brackets/reticle lines.
 ctx.strokeStyle='#29cef177';ctx.lineWidth=1;
 const x=W/2,y=H*.47;
 for(const sign of [-1,1]){ctx.beginPath();ctx.moveTo(x+sign*49,y-12);ctx.lineTo(x+sign*60,y-12);ctx.lineTo(x+sign*60,y+12);ctx.lineTo(x+sign*49,y+12);ctx.stroke();}
}
function loop(now){
 const dt=clip((now-last)/1000,0,.05);last=now;
 if(game.status==='running'){
  advanceGame(game,input(),dt);
  for(let i=particles.length-1;i>=0;i--){const p=particles[i];p.x+=p.vx*dt;p.y+=p.vy*dt;p.z+=p.vz*dt;p.life-=dt;if(p.life<=0)particles.splice(i,1);}
  processEvents();
 }
 draw(dt);
 if(frame++%3===0)paintHUD(dt*3);
 requestAnimationFrame(loop);
}
requestAnimationFrame(t=>{last=t;requestAnimationFrame(loop);});
window.__ARC_FLIGHT__={getState:()=>game,start:newGame,pause};
