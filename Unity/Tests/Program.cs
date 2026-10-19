using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using Hina.Core;
int checks=0,failures=0;
var results=new List<object>();
void Check(bool ok,string label){checks++;if(!ok)failures++;results.Add(new{name=label,passed=ok});Console.WriteLine((ok?"PASS ":"FAIL ")+label);}
var data=File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"../../../../ProjectHINA/Assets/Hina/Resources/Hina/stages.json"));
var stages=JsonSerializer.Deserialize<StageCollection>(data,new JsonSerializerOptions{IncludeFields=true}).stages;
GameModel New(){var g=new GameModel(stages);g.Start();return g;}
void Frames(GameModel g,int n,Controls c=default){for(int i=0;i<n;i++)g.Tick(1/60f,c);}
var g=New();Check(g.Lives==3&&g.Player.Stock==3,"initial_lives_special_stock");
float x=g.Player.X;Frames(g,20,new Controls{Move=1});Check(g.Player.X>x+55,"movement_180_pixels_second");
x=g.Player.X;Frames(g,3);Check(g.Player.X==x,"immediate_stop");
g.Tick(1/60f,new Controls{Jump=true});Check(g.Player.VY<0&&!g.Player.Grounded,"jump_rise");Frames(g,60);Check(g.Player.Grounded&&g.Player.Y==310,"jump_landing");
g=New();g.Tick(1/60f,new Controls{Shoot=true,Up=true});Check(g.Bullets.Count==1&&g.Bullets[0].VY<0&&g.Bullets[0].VX==0,"vertical_fire");
g=New();g.Tick(1/60f,new Controls{Shoot=true,Up=true,Move=1});Check(g.Bullets[0].VY<0&&g.Bullets[0].VX>0,"diagonal_fire");
g=New();g.Tick(1/60f,new Controls{Jump=true});g.Tick(1/60f,new Controls{Shoot=true,Down=true});Check(g.Player.Animation=="air_down"&&g.Bullets[0].VY>0,"air_down_fire");
g=New();g.Tick(1/60f,new Controls{Down=true,Move=1});Check(g.Player.X==80&&g.Player.Animation=="crouch","crouch_blocks_movement");
g=New();g.Enemies.Clear();var enemy=g.SpawnEnemy(190,0);Frames(g,25,new Controls{Shoot=true});Check(enemy.Dead&&g.Score>=100,"bullets_hit_enemy");int score=g.Score;g.HitEnemy(enemy,5);Check(g.Score==score,"kill_reward_once");
g=New();g.Enemies.Clear();enemy=g.SpawnEnemy(115,0);g.Tick(1/60f,new Controls{Shoot=true});Check(enemy.Dead&&g.Player.KickTimer>0&&g.Bullets.Count==0,"close_attack_auto_kick");
g=New();enemy=g.SpawnEnemy(1000,8);g.HitEnemy(enemy,1);Check(!enemy.Dead&&enemy.HP==17,"tank_survives_one_hit");g.HitEnemy(enemy,17);Check(enemy.Dead,"tank_dies_at_zero");
g=New();g.Player.Invulnerable=0;g.HitPlayer();Check(!g.Player.Alive&&g.Lives==2,"one_hit_life_loss");g.HitPlayer(true);Check(g.Lives==2,"duplicate_death_guard");Frames(g,40);Check(g.Player.Alive&&g.Player.Invulnerable>2&&g.Player.Stock==3,"respawn_same_actor_invulnerability");
g=New();g.Lives=1;g.Player.Invulnerable=0;g.Score=1234;g.HitPlayer();Frames(g,40);Check(g.Mode==Mode.Continue&&g.ContinueTime>9,"last_life_countdown");x=g.Player.X;float timer=g.MissionTime;Frames(g,30,new Controls{Move=1});Check(g.Player.X==x&&g.MissionTime==timer,"continue_freezes_world");g.InsertCoin();Check(g.Mode==Mode.Playing&&g.Lives==1&&g.Score==1234,"coin_continues_preserving_progress");
g=New();g.Lives=1;g.HitPlayer(true);Frames(g,700);Check(g.Mode==Mode.GameOver,"continue_expires");g.InsertCoin();Check(g.Mode==Mode.Playing&&g.Lives==1,"coin_after_gameover_restarts");
g=New();x=g.Player.X;timer=g.MissionTime;g.TogglePause();Frames(g,50,new Controls{Move=1});Check(g.Player.X==x&&g.MissionTime==timer,"pause_freezes_simulation");g.InsertCoin();Check(g.Lives==4&&g.Mode==Mode.Paused,"coin_during_pause");
g=New();g.Enemies.Clear();enemy=g.SpawnEnemy(200,0);g.Tick(1/60f,new Controls{Special=true});Check(g.Player.Stock==2&&g.Player.SpecialTimer>0,"special_consumes_stock");Frames(g,60);Check(enemy.Dead&&g.Player.Invulnerable>0,"special_area_attack_and_invulnerability");g.Player.Stock=0;Frames(g,60);g.Tick(1/60f,new Controls{Special=true});Check(g.Player.SpecialTimer<=0,"empty_special_blocked");
g=New();g.Player.X=450;Frames(g,1);int count=g.Enemies.Count;Frames(g,2);Check(count==g.Enemies.Count&&count>3,"waves_trigger_once");
g=New();var prop=g.Props[0];g.Bullets.Add(new Bullet{X=prop.Data.x-10,Y=300,VX=850,Friendly=true});Frames(g,2);Check(prop.HP==2,"bullet_hits_prop");
g=New();var platform=g.Stage.platforms[0];g.Bullets.Add(new Bullet{X=platform.x+10,Y=platform.y-6,VY=850,Friendly=true});Frames(g,1);Check(g.Bullets.Count==0,"bullet_terrain_collision");
for(int i=0;i<5;i++){
 g=New();g.Load(i);Check(g.Stage.length>4500&&g.Stage.platforms.Length>0,"stage_"+i+"_data");
 g.Player.X=g.Stage.length-600;Frames(g,1);Check(g.Boss!=null,"stage_"+i+"_boss_activation");
 g.Player.Invulnerable=9999;Frames(g,130);Check(g.Boss.Warning,"stage_"+i+"_warning_before_attack");
}
g=New();g.Load(2);g.StartBoss();g.Player.Invulnerable=999;Frames(g,130);float hp=g.Boss.HP;g.HitBoss(10);Check(g.Boss.HP==hp&&g.Boss.Targets.Count==2,"hod_pillars_block_boss_damage");foreach(var t in g.Boss.Targets)t.Dead=true;g.HitBoss(10);Check(g.Boss.HP==hp-10,"hod_vulnerable_after_pillars");
g=New();g.Load(4);g.StartBoss();hp=g.Boss.HP;g.HitBoss(10);Check(g.Boss.HP==hp,"chesed_closed_core_blocks_damage");g.Player.Invulnerable=9999;
for(int wave=0;wave<4;wave++){
 int attempts=0;while(g.Boss.Targets.Count==0&&attempts++<500)Frames(g,1);
 Check(g.Boss.Targets.Count==3,"chesed_wave_"+wave+"_targets");foreach(var t in g.Boss.Targets)t.Dead=true;Frames(g,1);
}
Check(g.Boss.CoreTime>0&&g.Boss.Wave==4,"chesed_core_opens_after_four_waves");g.HitBoss(10);Check(g.Boss.HP==hp-30,"chesed_open_core_multiplier");
g=New();g.StartBoss();g.HitBoss(99999);Check(g.Mode==Mode.Clear&&g.Boss.Dead&&g.Unlocked>=2,"boss_defeat_clear_unlock");Frames(g,300);Check(g.StageIndex==1&&g.Mode==Mode.Playing,"clear_next_stage_transition");
g=New();g.Load(4);g.StartBoss();g.Boss.CoreTime=5;g.HitBoss(99999);Frames(g,300);Check(g.Mode==Mode.Complete,"final_stage_completion");
g=New();g.Load(3);g.StartBoss();g.Player.Invulnerable=9999;Frames(g,310);Check(g.Boss.Targets.Count==2&&g.Boss.Targets.TrueForAll(t=>t.Kind=="goz_clone"),"goz_creates_two_redesigned_decoys");
g=New();g.Enemies.Clear();enemy=g.SpawnEnemy(220,8);enemy.Timer=.001f;Frames(g,1);Check(enemy.Recoil>0,"enemy_fire_triggers_attack_art_state");g.HitEnemy(enemy,1);Check(enemy.Hurt>0&&!enemy.Dead,"tank_hit_triggers_hurt_art_state");
g=New();prop=g.Props[0];prop.HP=1;g.Bullets.Add(new Bullet{X=prop.Data.x-10,Y=300,VX=850,Friendly=true});Frames(g,3);Check(prop.Dead&&prop.DeathTime>0,"prop_destroyed_state_tracks_debris_age");
g=New();g.Mode=Mode.Title;g.InsertCoin();g.InsertCoin();g.Start();Check(g.Lives==5&&g.Credits==0,"title_coins_become_starting_lives");
g=New();g.Player.Grounded=false;g.Player.Y=200;g.Player.VY=50;g.Player.Coyote=.06f;g.Tick(1/60f,new Controls{Jump=true});Check(g.Player.VY<0,"coyote_jump_after_leaving_ledge");
g=New();g.Player.Grounded=false;g.Player.Y=309;g.Player.VY=100;g.Tick(1/60f,new Controls{Jump=true});g.Tick(1/60f,default);Check(g.Player.VY<0,"jump_buffer_before_landing");
g=New();platform=g.Stage.platforms[0];g.Player.X=platform.x-10;Frames(g,5,new Controls{Move=1});Check(g.Player.X<=platform.x-8+.01f,"platform_side_blocks_running_through");
g=New();platform=g.Stage.platforms[0];g.Player.X=platform.x+20;g.Player.Y=platform.y-8;g.Player.VY=150;g.Player.Grounded=false;Frames(g,8);Check(g.Player.Grounded&&g.Player.Y==platform.y,"land_on_platform_top");
g=New();g.Enemies.Clear();g.Props.Clear();g.Props.Add(new Prop{Data=new PropData{x=g.Player.X+8,width=10,height=45}});g.Tick(1/60f,new Controls{Shoot=true});Check(g.Bullets.Count==0,"near_cover_blocks_muzzle_spawn_through_wall");Check(g.Props[0].HP==2,"point_blank_shot_damages_cover_instead_of_becoming_harmless");
g=New();g.StartBoss();g.HitBoss(99999);int lives=g.Lives;g.HitPlayer(true);Check(g.Lives==lives&&g.Mode==Mode.Clear,"clear_state_protects_player");g.Effects.Add(new Effect{Kind="explosion",Life=.05f});Frames(g,10);Check(g.Effects.Count==0,"effects_expire_during_clear_transition");
g=New();g.Enemies.Clear();g.StartBoss();g.Player.X=g.Stage.length-550;var raidTarget=new RaidTarget{X=g.Player.X+25,Y=310,HP=2,Kind="peroro"};g.Boss.Targets.Add(raidTarget);g.Tick(1/60f,new Controls{Shoot=true});Check(raidTarget.Dead&&g.Player.KickTimer>0&&g.Bullets.Count==0,"close_raid_add_uses_melee_instead_of_shooting_past_target");
g=New();g.Load(2);g.StartBoss();g.Player.X=g.Stage.length-550;raidTarget=new RaidTarget{X=g.Player.X+100,Y=310,HP=6,Kind="pillar"};g.Boss.Targets.Add(raidTarget);hp=g.Boss.HP;g.Tick(1/60f,new Controls{Special=true});Frames(g,60);Check(raidTarget.Dead&&g.Boss.HP<hp,"special_clears_raid_pillar_and_hits_newly_exposed_boss");
Console.WriteLine($"RESULT {checks} checks, {failures} failures");
File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"../../../../core_test_results.json"),JsonSerializer.Serialize(new{checks,failures,results},new JsonSerializerOptions{WriteIndented=true}));
return failures==0?0:1;
