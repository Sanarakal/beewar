using System.Text.Json;
using PlagueGame.Kingdom;

const double Epoch = 1800000000;
var checks = 0;
void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; }
bool Near(double a, double b) => Math.Abs(a - b) < .00001;
KingdomSimulation Fresh() => new(KingdomState.NewGame(Epoch));
KingdomState Clone(KingdomState state) => JsonSerializer.Deserialize<KingdomState>(JsonSerializer.Serialize(state, new JsonSerializerOptions { IncludeFields = true }), new JsonSerializerOptions { IncludeFields = true });

var game = Fresh();
Check(game.State.IsValid(), "new game is valid");
Check(!game.SendMarch(0, 10).success && game.State.march == null, "cannot march without troops");
var startWood = game.State.wood;
var cost = game.UpgradeCost(BuildingKind.Castle);
Check(game.StartUpgrade(BuildingKind.Castle).success && Near(game.State.wood, startWood - cost.wood), "upgrade charges resources once");
var chargedWood = game.State.wood;
Check(!game.StartUpgrade(BuildingKind.Farm).success && Near(game.State.wood, chargedWood), "only one construction queue and no duplicate charge");
var ends = game.State.upgrade.endsAt;
game.AdvanceTo(ends - .1);
Check(game.Level(BuildingKind.Castle) == 1, "upgrade does not finish early");
game.AdvanceTo(ends);
Check(game.Level(BuildingKind.Castle) == 2 && game.State.upgradesFinished == 1 && game.State.upgrade == null, "upgrade finishes exactly on deadline");
game.AdvanceTo(ends + 1);
Check(game.State.upgradesFinished == 1, "completed upgrade is not applied twice");
Check(!game.StartUpgrade(BuildingKind.Castle).success, "castle requires supporting buildings");
Check(game.StartUpgrade(BuildingKind.Farm).success, "castle unlocks production upgrade");
Check(game.StartTraining(1, 10).success, "training may run alongside construction");
Check(!game.StartTraining(1, 10).success, "cannot double queue troops");
Check(!game.StartTraining(2, 10).success, "tier II requires camp 3");
game.AdvanceTo(Math.Max(game.State.training.endsAt, game.State.upgrade.endsAt));
Check(game.AvailableTroops == 10 && game.State.trainedTotal == 10, "training grants exact quantity");
Check(game.State.IsValid(), "state remains valid after parallel completions");

var beforeMarchPower = game.Power;
Check(game.SendMarch(0, 10).success && game.AvailableTroops == 0, "dispatch reserves soldiers");
Check(game.Power == beforeMarchPower, "troops in transit retain power before combat");
Check(!game.SendMarch(1, 10).success, "same army cannot march twice");
var arrival = game.State.march.arrivesAt;
var home = game.State.march.homeAt;
var beforeFood = game.State.food;
var marchStart = game.State.lastUtc;
game.AdvanceTo(arrival);
Check(game.State.report.won && game.State.monstersDefeated == 1, "ten basic troops defeat first monster");
Check(game.State.march.phase == MarchPhase.Returning && !game.State.report.delivered, "battle starts return journey");
Check(Near(game.State.food, beforeFood + game.FoodPerSecond * (arrival - marchStart)), "loot is not credited before returning");
var saved = Clone(game.State);
Check(saved.IsValid(), "in-flight save validates after serialization");
var restored = new KingdomSimulation(saved);
restored.AdvanceTo(home);
Check(restored.State.march == null && restored.State.report.delivered && restored.State.marchesReturned == 1, "saved march returns successfully");
Check(restored.AvailableTroops == restored.State.report.survivors && restored.AvailableTroops < 10, "only surviving troops return");
Check(restored.State.heroExperience == 20, "hero experience credited once on return");
var goldAfterReturn = restored.State.gold;
var reloaded = new KingdomSimulation(Clone(restored.State));
reloaded.AdvanceTo(home);
Check(Near(reloaded.State.gold, goldAfterReturn) && reloaded.State.marchesReturned == 1, "reload does not duplicate loot or troops");
Check(!reloaded.SendMarch(0, 10).success, "defeated target has respawn cooldown");

var offline = new KingdomSimulation(Clone(game.State));
offline.AdvanceTo(home + 3600 * 24);
Check(offline.State.march == null && offline.State.report.delivered, "offline elapsed time completes march");
Check(offline.State.food == offline.StorageCapacity && offline.State.wood <= offline.StorageCapacity && offline.State.gold <= offline.StorageCapacity, "offline income respects storage limits");

var continuous = Fresh();
continuous.StartUpgrade(BuildingKind.Castle);
continuous.StartTraining(1, 10);
var partitioned = new KingdomSimulation(Clone(continuous.State));
continuous.AdvanceTo(Epoch + 90);
for (var i = 1; i <= 90; i++) partitioned.AdvanceTo(Epoch + i);
Check(Near(continuous.State.gold, partitioned.State.gold) && Near(continuous.State.food, partitioned.State.food) && continuous.AvailableTroops == partitioned.AvailableTroops,
    "large and small time steps give same income and completions");
var timestamp = partitioned.State.lastUtc;
var unchangedGold = partitioned.State.gold;
partitioned.AdvanceTo(timestamp - 100);
partitioned.AdvanceTo(double.NaN);
Check(partitioned.State.lastUtc == timestamp && partitioned.State.gold == unchangedGold, "clock rollback and invalid time cannot grant resources");

var poor = Fresh(); poor.State.food = poor.State.wood = poor.State.gold = 0;
Check(!poor.StartUpgrade(BuildingKind.Castle).success && !poor.StartTraining(1, 10).success && poor.State.gold == 0, "unaffordable commands leave balances unchanged");
Check(!poor.StartTraining(1, -10).success && !poor.StartUpgrade((BuildingKind)99).success && !poor.SendMarch(99, 10).success, "invalid commands are rejected");
poor.AdvanceTo(Epoch + 120);
Check(poor.StartTraining(1, 10).success, "passive income permits recovery from an empty treasury");

var veteran = Fresh(); veteran.State.tier1 = 30; veteran.State.tier2 = 15;
Check(veteran.SendMarch(1, 1000).success && veteran.State.march.tier2 == 15 && veteran.State.march.tier1 == veteran.ArmyCapacity - 15, "march respects capacity and selects higher tier first");
var weak = KingdomSimulation.ResolveBattle(10, 0, 1, KingdomSimulation.Monsters[2]);
var strong = KingdomSimulation.ResolveBattle(0, 40, 3, KingdomSimulation.Monsters[2]);
Check(!weak.won && strong.won, "combat stats change result against a difficult monster");
Check(strong.survivors2 <= 40 && weak.survivors1 >= 0, "battle conserves troop counts");

var broken = Clone(Fresh().State); broken.food = double.NaN;
Check(!broken.IsValid(), "NaN balances rejected");
broken = Clone(Fresh().State); broken.buildings[2].kind = BuildingKind.Castle;
Check(!broken.IsValid(), "duplicate building identity rejected");
broken = Clone(Fresh().State); broken.monsterReadyAt = new double[1];
Check(!broken.IsValid(), "truncated map data rejected");
broken = Clone(veteran.State); broken.march.survivors1 = broken.march.tier1 + 1;
Check(!broken.IsValid(), "impossible army return rejected");
var capped = Fresh(); capped.State.buildings[0].level = 5;
Check(!capped.StartUpgrade(BuildingKind.Castle).success, "level cap enforced");

var story = Fresh();
Check(story.StoryStage == 0, "fresh hive starts with refugee prologue");
story.StartUpgrade(BuildingKind.Castle);
story.AdvanceTo(story.State.upgrade.endsAt + .01);
Check(story.StoryStage == 1, "fortified hive opens guard chapter");
story.StartTraining(1, 10);
story.AdvanceTo(story.State.training.endsAt + .01);
story.SendMarch(0, 10);
var storyHomeAt = story.State.march.homeAt;
story.AdvanceTo(story.State.march.arrivesAt + .01);
Check(story.State.report.won && story.StoryStage == 2 && new KingdomSimulation(Clone(story.State)).StoryStage == 2, "battle and reload do not reveal undelivered discovery");
story.AdvanceTo(storyHomeAt + .01);
Check(story.StoryStage == 3 && new KingdomSimulation(Clone(story.State)).StoryStage == 3, "delivered victory and reload preserve discovery chapter");

Console.WriteLine($"KINGDOM_CHECKS_OK {checks} checks");
