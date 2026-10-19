using System;
using ArcSquad;
static class Runner {
  static int n;
  static void Assert(bool condition, string name) { n++; if(!condition) throw new Exception("FAIL: "+name); Console.WriteLine("PASS "+name); }
  static void Main(){
    Assert(Math.Abs(SquadRules.ClampEnergy(110)-100)<.001f,"energy upper bound");
    Assert(Math.Abs(SquadRules.ClampEnergy(-5))<.001f,"energy lower bound");
    Assert(SquadRules.CanBoost(3),"boost energy present");
    Assert(!SquadRules.CanBoost(0),"boost zero energy");
    Assert(SquadRules.CanMissile(40,0),"missile enough energy cooldown zero");
    Assert(!SquadRules.CanMissile(19.9f,0),"missile below cost rejected");
    Assert(!SquadRules.CanMissile(80,.5f),"missile cooldown enforced");
    Assert(SquadRules.ApplyDamage(100,25)==75,"damage server calculation");
    Assert(SquadRules.ApplyDamage(10,100)==0,"damage cannot go negative");
    Assert(SquadRules.ScoreForKill(3)==160,"wave score");
    Assert(SquadRules.ValidPort(7777),"UDP port valid");
    Assert(!SquadRules.ValidPort(0),"UDP zero port invalid");
    Assert(!SquadRules.ValidPort(70000),"UDP too high invalid");
    Assert(SquadRules.LimitMovement(2)==.05f,"big frame time clamped");
    Assert(SquadRules.LimitMovement(-1)==0f,"negative frame time rejected");
    Console.WriteLine("RULES VERIFIED "+n+"/15");
  }
}