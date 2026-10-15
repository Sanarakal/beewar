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

var scholar = Fresh();
var academyGold = scholar.State.gold;
Check(!scholar.StartResearch(ResearchKind.Nectar).success && scholar.State.research == null && scholar.State.gold == academyGold, "academy requires citadel 2 without charging");
Check(!scholar.StartResearch((ResearchKind)(-1)).success && !scholar.StartResearch((ResearchKind)99).success, "invalid research commands rejected");
scholar.State.buildings[0].level = 2;
var researchPrice = scholar.ResearchCost(ResearchKind.Nectar);
var researchFood = scholar.State.food;
Check(scholar.StartResearch(ResearchKind.Nectar).success && Near(scholar.State.food, researchFood - researchPrice.food), "research charges exact cost on start");
var chargedResearchGold = scholar.State.gold;
Check(!scholar.StartResearch(ResearchKind.Wax).success && scholar.State.gold == chargedResearchGold, "only one research queue and no duplicate charge");
Check(scholar.StartUpgrade(BuildingKind.Barracks).success && scholar.StartTraining(1, 10).success, "research construction and training run independently");
var researchEnd = scholar.State.research.endsAt;
var scholarReload = new KingdomSimulation(Clone(scholar.State));
Check(scholarReload.State.research != null && scholarReload.State.IsValid(), "active research survives serialization with other queues");
scholarReload.AdvanceTo(researchEnd - .01);
Check(scholarReload.ResearchLevel(ResearchKind.Nectar) == 0 && Near(scholarReload.FoodPerSecond, 1.2), "research bonus not applied early");
var preResearchPower = scholarReload.Power;
scholarReload.AdvanceTo(researchEnd);
Check(scholarReload.ResearchLevel(ResearchKind.Nectar) == 1 && scholarReload.State.research == null && Near(scholarReload.FoodPerSecond, 1.38), "research completes on boundary and affects production");
Check(scholarReload.Power == preResearchPower + 100, "completed research contributes to power");
var afterResearchFood = scholarReload.State.food;
scholarReload.AdvanceTo(researchEnd + 10);
Check(Near(scholarReload.State.food, afterResearchFood + 13.8), "researched production rate grants real resources");
Check(!scholarReload.StartResearch(ResearchKind.Nectar).success && scholarReload.ResearchBlock(ResearchKind.Nectar).Contains("академия 2"), "research level limited by academy");
var completedScholar = new KingdomSimulation(Clone(scholarReload.State));
completedScholar.AdvanceTo(researchEnd + 10);
Check(completedScholar.ResearchTotal == 1 && completedScholar.Power == scholarReload.Power, "research reload does not duplicate level or power");

var boundary = Fresh(); boundary.State.buildings[0].level = 2;
boundary.StartResearch(ResearchKind.Wax);
var boundaryReload = new KingdomSimulation(Clone(boundary.State));
var boundaryWood = boundary.State.wood;
boundary.AdvanceTo(Epoch + 46);
for (var i = 1; i <= 46; i++) boundaryReload.AdvanceTo(Epoch + i);
Check(Near(boundary.State.wood, boundaryWood + .9 * 26 + 1.035 * 20), "offline income changes exactly at research completion");
Check(Near(boundary.State.wood, boundaryReload.State.wood), "partitioned and offline research give identical production");
boundaryReload.AdvanceTo(Epoch + 9 * 3600);
Check(boundaryReload.State.research == null && boundaryReload.State.wood == boundaryReload.StorageCapacity && boundaryReload.ResearchTotal == 1, "long offline period completes research once and respects storage");

var builders = Fresh(); builders.State.buildings[0].level = 2;
builders.StartResearch(ResearchKind.Construction); builders.StartUpgrade(BuildingKind.Farm);
builders.State.upgrade.endsAt = Epoch + 100;
builders.AdvanceTo(builders.State.research.endsAt);
Check(builders.State.upgrade.endsAt == Epoch + 100 && Near(builders.UpgradeDuration(BuildingKind.Warehouse), 11 / 1.1), "construction research accelerates new work and preserves running deadline");

var tactics = Fresh(); tactics.State.buildings[0].level = 2; tactics.State.tier1 = 20;
tactics.StartResearch(ResearchKind.Attack); tactics.State.research.endsAt = Epoch + 3;
var tacticsPreview = tactics.Preview(1, 20);
tactics.SendMarch(1, 20);
Check(tactics.State.march.attackResearchLevel == 0, "march snapshots attack research on departure");
var tacticsReload = new KingdomSimulation(Clone(tactics.State));
tacticsReload.AdvanceTo(tacticsReload.State.march.homeAt);
Check(tacticsReload.ResearchLevel(ResearchKind.Attack) == 1 && tacticsReload.State.report.won == tacticsPreview.won && tacticsReload.State.report.survivors == tacticsPreview.survivors1 + tacticsPreview.survivors2, "mid-march research and reload preserve advertised battle outcome");
tacticsReload.State.tier1 = 20;
tacticsReload.SendMarch(0, 20);
Check(tacticsReload.State.march.attackResearchLevel == 1, "next march receives newly researched attack");
var combatImproved = false;
for (var soldiers = 2; soldiers <= 40; soldiers++)
{
    var normal = KingdomSimulation.ResolveBattle(soldiers, 0, 1, KingdomSimulation.Monsters[1]);
    var educated = KingdomSimulation.ResolveBattle(soldiers, 0, 1, KingdomSimulation.Monsters[1], 5);
    if (educated.survivors1 > normal.survivors1 || (educated.won && !normal.won)) combatImproved = true;
}
Check(combatImproved, "attack research changes actual survival or victory");
var master = Fresh(); master.State.buildings[0].level = master.State.buildings[6].level = 5; master.State.researchLevels[0] = 5;
Check(master.State.IsValid() && !master.StartResearch(ResearchKind.Nectar).success, "five research levels is the final cap");
var poorScholar = Fresh(); poorScholar.State.buildings[0].level = 2; poorScholar.State.food = poorScholar.State.wood = poorScholar.State.gold = 0;
Check(!poorScholar.StartResearch(ResearchKind.Attack).success && poorScholar.State.research == null && poorScholar.State.gold == 0, "unaffordable research leaves treasury and queue unchanged");

var legacy = Clone(scholar.State);
legacy.version = 1; legacy.research = null; legacy.researchLevels = null;
Array.Resize(ref legacy.buildings, 6);
var legacyTraining = legacy.training.count; var legacyWood = legacy.wood; var legacyDeadline = legacy.upgrade.endsAt;
Check(legacy.MigrateAndValidate() && legacy.buildings.Length == 7 && legacy.version == KingdomState.CurrentVersion, "version 1 migrates to seven buildings and research");
Check(legacy.training.count == legacyTraining && legacy.upgrade.endsAt == legacyDeadline && legacy.wood == legacyWood && Array.TrueForAll(legacy.researchLevels, level => level == 0), "migration preserves resources levels and queues");
Check(legacy.MigrateAndValidate() && legacy.buildings.Length == 7, "migration is idempotent");
var invalidResearch = Clone(Fresh().State); invalidResearch.researchLevels = new int[2];
Check(!invalidResearch.IsValid(), "truncated research array rejected");
invalidResearch = Clone(Fresh().State); invalidResearch.researchLevels[0] = -1;
Check(!invalidResearch.IsValid(), "negative research level rejected");
invalidResearch = Clone(Fresh().State); invalidResearch.version = 1; Array.Resize(ref invalidResearch.buildings, 5);
Check(!invalidResearch.MigrateAndValidate(), "malformed legacy data cannot migrate");
invalidResearch = Clone(Fresh().State); invalidResearch.version = 99;
Check(!invalidResearch.MigrateAndValidate(), "unknown future save version rejected");

Console.WriteLine($"KINGDOM_CHECKS_OK {checks} checks");
