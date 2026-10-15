using System;

namespace PlagueGame.Kingdom
{
    public enum BuildingKind { Castle, Farm, Lumbermill, Warehouse, Barracks, Camp }
    public enum MarchPhase { Outbound, Returning }

    [Serializable]
    public sealed class BuildingState
    {
        public BuildingKind kind;
        public int level = 1;
    }

    [Serializable]
    public sealed class UpgradeOrder
    {
        public BuildingKind building;
        public int targetLevel;
        public double startedAt, endsAt;
    }

    [Serializable]
    public sealed class TrainingOrder
    {
        public int tier, count;
        public double startedAt, endsAt;
    }

    [Serializable]
    public sealed class MarchOrder
    {
        public int id, monster, tier1, tier2, survivors1, survivors2;
        public MarchPhase phase;
        public double startedAt, arrivesAt, homeAt;
        public bool won;
        public int food, wood, gold, experience;
    }

    [Serializable]
    public sealed class BattleReport
    {
        public int marchId, monster, sent, survivors, lost, food, wood, gold, experience;
        public bool won, delivered;
    }

    [Serializable]
    public sealed class KingdomState
    {
        public int version = 1;
        public double lastUtc, food, wood, gold;
        public BuildingState[] buildings;
        public UpgradeOrder upgrade;
        public TrainingOrder training;
        public MarchOrder march;
        public BattleReport report;
        public int tier1, tier2, heroExperience, nextMarchId = 1;
        public int upgradesFinished, trainedTotal, monstersDefeated, marchesReturned;
        public double[] monsterReadyAt = new double[3];

        public static KingdomState NewGame(double now)
        {
            var state = new KingdomState { lastUtc = now, food = 450, wood = 450, gold = 240, buildings = new BuildingState[6] };
            for (var i = 0; i < state.buildings.Length; i++) state.buildings[i] = new BuildingState { kind = (BuildingKind)i };
            return state;
        }

        public bool IsValid()
        {
            if (version != 1 || !TimeValid(lastUtc) || lastUtc < 1 || !ResourceValid(food) || !ResourceValid(wood) || !ResourceValid(gold)) return false;
            if (buildings == null || buildings.Length != 6 || monsterReadyAt == null || monsterReadyAt.Length != 3) return false;
            for (var i = 0; i < buildings.Length; i++)
                if (buildings[i] == null || buildings[i].kind != (BuildingKind)i || buildings[i].level < 1 || buildings[i].level > 5) return false;
            foreach (var time in monsterReadyAt) if (!TimeValid(time)) return false;
            if (tier1 < 0 || tier2 < 0 || tier1 + (long)tier2 > 1000000 || heroExperience < 0 || heroExperience > 100000000 || nextMarchId < 1) return false;
            if (upgradesFinished < 0 || trainedTotal < 0 || monstersDefeated < 0 || marchesReturned < 0) return false;
            if (upgrade != null && ((int)upgrade.building < 0 || (int)upgrade.building >= 6 ||
                upgrade.targetLevel != buildings[(int)upgrade.building].level + 1 || upgrade.targetLevel > 5 || !OrderValid(upgrade.startedAt, upgrade.endsAt))) return false;
            if (training != null && ((training.tier != 1 && training.tier != 2) || training.count < 1 || training.count > 200 || !OrderValid(training.startedAt, training.endsAt))) return false;
            if (march != null)
            {
                if (march.id < 1 || march.id >= nextMarchId || march.monster < 0 || march.monster > 2 ||
                    march.tier1 < 0 || march.tier2 < 0 || march.tier1 + (long)march.tier2 < 1 || march.tier1 + (long)march.tier2 > 1000 ||
                    march.survivors1 < 0 || march.survivors1 > march.tier1 || march.survivors2 < 0 || march.survivors2 > march.tier2 ||
                    (march.phase != MarchPhase.Outbound && march.phase != MarchPhase.Returning) ||
                    !OrderValid(march.startedAt, march.arrivesAt) || !OrderValid(march.arrivesAt, march.homeAt) ||
                    march.food < 0 || march.wood < 0 || march.gold < 0 || march.experience < 0) return false;
            }
            if (report != null && (report.marchId < 1 || report.monster < 0 || report.monster > 2 || report.sent < 1 || report.sent > 1000 ||
                report.survivors < 0 || report.lost < 0 || report.survivors + report.lost != report.sent ||
                report.food < 0 || report.wood < 0 || report.gold < 0 || report.experience < 0)) return false;
            return true;
        }

        private static bool OrderValid(double start, double end) => TimeValid(start) && TimeValid(end) && end > start;
        private static bool TimeValid(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0 && value < 100000000000;
        private static bool ResourceValid(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0 && value <= 1000000000;
    }

    public struct KingdomAction
    {
        public bool success;
        public string message;
        public KingdomAction(bool success, string message) { this.success = success; this.message = message; }
    }

    public struct ResourceCost
    {
        public int food, wood, gold;
        public ResourceCost(int food, int wood, int gold) { this.food = food; this.wood = wood; this.gold = gold; }
        public string Text => food + " нектара   ·   " + wood + " воска   ·   " + gold + " янтаря";
    }

    public sealed class MonsterDefinition
    {
        public readonly string name, description;
        public readonly int level, health, attack, defense, food, wood, gold, experience;
        public readonly double travelSeconds;
        public MonsterDefinition(string name, string description, int level, int health, int attack, int defense,
            int food, int wood, int gold, int experience, double travel)
        {
            this.name = name; this.description = description; this.level = level; this.health = health; this.attack = attack;
            this.defense = defense; this.food = food; this.wood = wood; this.gold = gold; this.experience = experience; travelSeconds = travel;
        }
    }

    public struct BattleOutcome
    {
        public bool won;
        public int survivors1, survivors2, rounds;
    }

    // Pure rules: the Unity scene supplies time and commands, and only displays the result.
    public sealed class KingdomSimulation
    {
        public const double MaxOfflineProduction = 8 * 3600;
        public readonly KingdomState State;
        public int EventVersion { get; private set; }
        public string LastEvent { get; private set; }
        public static readonly MonsterDefinition[] Monsters = {
            new MonsterDefinition("Заражённый долгоносик", "Небольшой отряд справится. Хорошая цель для первого похода.", 1, 170, 15, 1, 70, 70, 60, 20, 8),
            new MonsterDefinition("Шипокрыл", "Соберите больше солдат или откройте войска II ступени.", 2, 520, 36, 3, 140, 150, 120, 45, 12),
            new MonsterDefinition("Корневой паук", "Опасный противник. Нужны развитый лагерь и сильная армия.", 3, 1700, 82, 6, 300, 320, 240, 90, 16)
        };

        public KingdomSimulation(KingdomState state)
        {
            if (state == null || !state.IsValid()) throw new ArgumentException("Invalid kingdom state");
            State = state;
        }

        public int Level(BuildingKind kind) => State.buildings[(int)kind].level;
        public int StorageCapacity => 600 + Level(BuildingKind.Warehouse) * 400;
        public double FoodPerSecond => Level(BuildingKind.Farm) * 1.2;
        public double WoodPerSecond => Level(BuildingKind.Lumbermill) * .9;
        public double GoldPerSecond => Level(BuildingKind.Castle) * .35;
        public int AvailableTroops => State.tier1 + State.tier2;
        public int ArmyCapacity => 15 + Level(BuildingKind.Camp) * 5 + HeroLevel * 2;
        public int UnlockedTier => Level(BuildingKind.Camp) >= 3 ? 2 : 1;
        public int HeroLevel
        {
            get { var level = 1; var xp = State.heroExperience; while (level < 5 && xp >= level * 40) { xp -= level * 40; level++; } return level; }
        }
        public int Power
        {
            get
            {
                var power = HeroLevel * 150 + State.tier1 * 25 + State.tier2 * 45;
                foreach (var building in State.buildings) power += building.level * 80;
                if (State.march != null) power += (State.march.phase == MarchPhase.Outbound ? State.march.tier1 : State.march.survivors1) * 25 +
                    (State.march.phase == MarchPhase.Outbound ? State.march.tier2 : State.march.survivors2) * 45;
                return power;
            }
        }

        public static string BuildingName(BuildingKind kind)
        {
            return new[] { "Цитадель улья", "Нектарный сад", "Восковая мастерская", "Сотовое хранилище", "Казармы Жала", "Лагерь Крыльев" }[(int)kind];
        }

        public string BuildingBenefit(BuildingKind kind)
        {
            switch (kind)
            {
                case BuildingKind.Castle: return "Открывает развитие построек. Добыча янтаря: " + (GoldPerSecond * 60).ToString("0") + " янтаря/мин.";
                case BuildingKind.Farm: return "Производство нектара: " + (FoodPerSecond * 60).ToString("0") + "/мин. Нектар нужен для обучения и развития.";
                case BuildingKind.Lumbermill: return "Производство воска: " + (WoodPerSecond * 60).ToString("0") + "/мин. Воск нужен для построек и армии.";
                case BuildingKind.Warehouse: return "Вместимость: " + StorageCapacity + " каждого ресурса. При заполнении производство останавливается.";
                case BuildingKind.Barracks: return "Обучает войска. Чем выше уровень, тем быстрее готова партия солдат.";
                default: return "В походе до " + ArmyCapacity + " солдат. На 3 уровне открывает войска II ступени.";
            }
        }

        public ResourceCost UpgradeCost(BuildingKind kind)
        {
            var factor = Math.Pow(1.75, Level(kind) - 1);
            var initial = kind == BuildingKind.Castle ? new ResourceCost(80, 120, 60) : new ResourceCost(40, 90, 25);
            return new ResourceCost((int)Math.Ceiling(initial.food * factor), (int)Math.Ceiling(initial.wood * factor), (int)Math.Ceiling(initial.gold * factor));
        }
        public double UpgradeDuration(BuildingKind kind) => 6 + Level(kind) * 5 + (kind == BuildingKind.Castle ? 5 : 0);
        public bool CanAfford(ResourceCost cost) => State.food >= cost.food && State.wood >= cost.wood && State.gold >= cost.gold;
        public string UpgradeBlock(BuildingKind kind)
        {
            if (Level(kind) >= 5) return "Достигнут предел тестовой версии";
            if (State.upgrade != null) return "Строители заняты: " + BuildingName(State.upgrade.building);
            if (kind != BuildingKind.Castle && Level(kind) >= Level(BuildingKind.Castle)) return "Сначала улучшите цитадель до " + (Level(kind) + 1) + " уровня";
            if (kind == BuildingKind.Castle && (Level(BuildingKind.Barracks) < Level(kind) || Level(BuildingKind.Warehouse) < Level(kind)))
                return "Нужны казармы и склад " + Level(kind) + " уровня";
            return CanAfford(UpgradeCost(kind)) ? "" : "Недостаточно ресурсов — дождитесь производства или вернитесь с добычей";
        }

        public KingdomAction StartUpgrade(BuildingKind kind)
        {
            if ((int)kind < 0 || (int)kind > 5) return Fail("Неизвестная постройка");
            var block = UpgradeBlock(kind);
            if (block.Length > 0) return Fail(block);
            Spend(UpgradeCost(kind));
            State.upgrade = new UpgradeOrder { building = kind, targetLevel = Level(kind) + 1, startedAt = State.lastUtc, endsAt = State.lastUtc + UpgradeDuration(kind) };
            return Ok("Улучшение началось: " + BuildingName(kind));
        }

        public ResourceCost TrainingCost(int tier, int count) => new ResourceCost(count * (tier == 1 ? 6 : 10), count * (tier == 1 ? 2 : 4), count * (tier == 1 ? 1 : 2));
        public double TrainingDuration(int tier, int count) => count * (tier == 1 ? 1.2 : 2) / (1 + (Level(BuildingKind.Barracks) - 1) * .2);
        public string TrainingBlock(int tier, int count)
        {
            if (tier < 1 || tier > 2 || count < 1 || count > 200) return "Недопустимая партия";
            if (tier > UnlockedTier) return "Для II ступени нужен Лагерь Крыльев 3 уровня";
            if (State.training != null) return "В казармах уже идёт обучение";
            if (AvailableTroops + count > 1000) return "В тестовой версии можно держать до 1000 солдат";
            return CanAfford(TrainingCost(tier, count)) ? "" : "Недостаточно ресурсов для обучения";
        }

        public KingdomAction StartTraining(int tier, int count)
        {
            var block = TrainingBlock(tier, count);
            if (block.Length > 0) return Fail(block);
            Spend(TrainingCost(tier, count));
            State.training = new TrainingOrder { tier = tier, count = count, startedAt = State.lastUtc, endsAt = State.lastUtc + TrainingDuration(tier, count) };
            return Ok("Обучение началось: " + count + " солдат " + (tier == 1 ? "I" : "II") + " ступени");
        }

        public int SquadSize(int requested) => Math.Max(0, Math.Min(requested, Math.Min(AvailableTroops, ArmyCapacity)));
        public string MarchBlock(int monster, int requested)
        {
            if (monster < 0 || monster >= Monsters.Length) return "Неизвестная цель";
            if (State.march != null) return "Сначала дождитесь возвращения отряда";
            if (State.monsterReadyAt[monster] > State.lastUtc) return "Противник появится снова через " + Math.Ceiling(State.monsterReadyAt[monster] - State.lastUtc) + " сек.";
            return SquadSize(requested) > 0 ? "" : "Сначала обучите солдат в казармах";
        }

        public BattleOutcome Preview(int monster, int requested)
        {
            var count = SquadSize(requested);
            var tier2 = Math.Min(count, State.tier2);
            return ResolveBattle(count - tier2, tier2, HeroLevel, Monsters[monster]);
        }

        public KingdomAction SendMarch(int monster, int requested)
        {
            var block = MarchBlock(monster, requested);
            if (block.Length > 0) return Fail(block);
            var count = SquadSize(requested);
            var tier2 = Math.Min(count, State.tier2);
            var march = new MarchOrder { id = State.nextMarchId++, monster = monster, tier1 = count - tier2, tier2 = tier2,
                startedAt = State.lastUtc, arrivesAt = State.lastUtc + Monsters[monster].travelSeconds,
                homeAt = State.lastUtc + Monsters[monster].travelSeconds * 2, phase = MarchPhase.Outbound };
            State.tier1 -= march.tier1;
            State.tier2 -= march.tier2;
            State.march = march;
            return Ok("Отряд отправлен: " + count + " солдат");
        }

        public static BattleOutcome ResolveBattle(int tier1, int tier2, int heroLevel, MonsterDefinition monster)
        {
            var result = new BattleOutcome();
            if (tier1 + tier2 <= 0) return result;
            var maxHealth = tier1 * 30d + tier2 * 50d;
            var health = maxHealth;
            var monsterHealth = (double)monster.health;
            var attack = (tier1 * Math.Max(1, 7 - monster.defense) + tier2 * Math.Max(1, 12 - monster.defense)) * (1 + (heroLevel - 1) * .06);
            var defense = (tier1 + tier2 * 2d) / (tier1 + tier2);
            for (var round = 0; round < 100 && health > 0 && monsterHealth > 0; round++)
            {
                monsterHealth -= Math.Max(1, attack * health / maxHealth + heroLevel * 5);
                health -= Math.Max(1, monster.attack - defense);
                result.rounds++;
            }
            result.won = monsterHealth <= 0 && health > 0;
            var fraction = Math.Max(0, health / maxHealth);
            result.survivors1 = Math.Min(tier1, (int)Math.Floor(tier1 * fraction));
            result.survivors2 = Math.Min(tier2, (int)Math.Floor(tier2 * fraction));
            return result;
        }

        public void AdvanceTo(double now)
        {
            if (double.IsNaN(now) || double.IsInfinity(now) || now <= State.lastUtc || now >= 100000000000) return;
            var productionStart = Math.Max(State.lastUtc, now - MaxOfflineProduction);
            // Process ordered boundaries so a completed building changes production at its completion time.
            while (State.lastUtc < now)
            {
                var next = now;
                if (State.upgrade != null) next = Math.Min(next, Math.Max(State.lastUtc, State.upgrade.endsAt));
                if (State.training != null) next = Math.Min(next, Math.Max(State.lastUtc, State.training.endsAt));
                if (State.march != null) next = Math.Min(next, Math.Max(State.lastUtc,
                    State.march.phase == MarchPhase.Outbound ? State.march.arrivesAt : State.march.homeAt));
                var incomeSeconds = Math.Max(0, next - Math.Max(productionStart, State.lastUtc));
                AddIncome(incomeSeconds);
                State.lastUtc = next;
                CompleteOrders();
            }
        }

        private void CompleteOrders()
        {
            if (State.upgrade != null && State.upgrade.endsAt <= State.lastUtc)
            {
                var order = State.upgrade;
                State.buildings[(int)order.building].level = order.targetLevel;
                State.upgrade = null;
                State.upgradesFinished++;
                Notify(BuildingName(order.building) + " · уровень " + order.targetLevel + " готов!");
            }
            if (State.training != null && State.training.endsAt <= State.lastUtc)
            {
                var order = State.training;
                if (order.tier == 1) State.tier1 += order.count; else State.tier2 += order.count;
                State.trainedTotal += order.count;
                State.training = null;
                Notify("Обучение завершено: +" + order.count + " солдат");
            }
            var march = State.march;
            if (march == null) return;
            if (march.phase == MarchPhase.Outbound && march.arrivesAt <= State.lastUtc)
            {
                var monster = Monsters[march.monster];
                var outcome = ResolveBattle(march.tier1, march.tier2, HeroLevel, monster);
                march.won = outcome.won;
                march.survivors1 = outcome.survivors1;
                march.survivors2 = outcome.survivors2;
                march.phase = MarchPhase.Returning;
                if (march.won)
                {
                    march.food = monster.food; march.wood = monster.wood; march.gold = monster.gold; march.experience = monster.experience;
                    State.monstersDefeated++;
                    State.monsterReadyAt[march.monster] = State.lastUtc + 45;
                }
                State.report = new BattleReport { marchId = march.id, monster = march.monster, won = march.won,
                    sent = march.tier1 + march.tier2, survivors = march.survivors1 + march.survivors2,
                    lost = march.tier1 + march.tier2 - march.survivors1 - march.survivors2,
                    food = march.food, wood = march.wood, gold = march.gold, experience = march.experience };
                Notify(march.won ? "Победа! Рута ведёт отряд с добычей" : "Поражение. Выжившие отступают домой");
            }
            if (march.phase == MarchPhase.Returning && march.homeAt <= State.lastUtc)
            {
                State.tier1 += march.survivors1;
                State.tier2 += march.survivors2;
                AddResources(march.food, march.wood, march.gold);
                State.heroExperience += march.experience;
                if (State.report != null && State.report.marchId == march.id) State.report.delivered = true;
                State.marchesReturned++;
                State.march = null;
                Notify(march.won ? "Отряд дома. Добыча доставлена!" : "Поход завершён. Пополните армию перед новой атакой");
            }
        }

        private void AddIncome(double seconds) => AddResources(FoodPerSecond * seconds, WoodPerSecond * seconds, GoldPerSecond * seconds);
        private void AddResources(double food, double wood, double gold)
        {
            State.food = Math.Min(StorageCapacity, State.food + food);
            State.wood = Math.Min(StorageCapacity, State.wood + wood);
            State.gold = Math.Min(StorageCapacity, State.gold + gold);
        }
        private void Spend(ResourceCost cost) { State.food -= cost.food; State.wood -= cost.wood; State.gold -= cost.gold; }
        private void Notify(string message) { LastEvent = message; EventVersion++; }
        private static KingdomAction Fail(string message) => new KingdomAction(false, message);
        private static KingdomAction Ok(string message) => new KingdomAction(true, message);

        // Story follows persistent achievements, so losses and reloads cannot undo a chapter.
        public int StoryStage => Level(BuildingKind.Castle) < 2 ? 0 : State.trainedTotal < 10 ? 1 : State.heroExperience < 20 ? 2 : 3;
        public string StoryTitle => new[] { "Улей среди корней", "Стража нового дома", "Дорога к цветам", "Печать Первоулия" }[StoryStage];
        public string StoryText => new[] {
            "Рута привела беженцев к старому укрытию. За корнями ещё цветёт клевер, а Чёрная пыльца сюда не добралась.\n\n«Здесь можно остаться. Помоги укрепить цитадель — и у нас снова будет дом».\n\nКогда-то Первоулий хранил весь сад. Теперь его ворота закрыты, а пчелиные дома воюют за уцелевшие земли.",
            "В цитадели снова горят янтарные огни. Но дозорные заметили заражённого долгоносика на пути к цветочным полянам.\n\nРута: «Сначала подготовим стражу. Вдвоём мы эту дорогу не удержим».\n\nОткрой раздел «Армия» и обучи десять стражей Жала.",
            "Первые стражи готовы. За воротами начинается Цветолесье: огромные цветы, старые корни и существа, изменённые Чёрной пыльцой.\n\nРута: «Я проведу отряд. Добычу и вести получим, когда вернёмся домой».\n\nВыбери долгоносика на карте. Проверь прогноз боя и дождись возвращения победившего отряда.",
            "Среди возвращённой добычи нашлась печать конвоя Первоулия. Воск ещё свежий.\n\nРута: «Значит, в столице кто-то остался. Но до её ворот нам одним не добраться».\n\nЗа тишиной столицы скрывается опыт регента Морна: он хотел подарить саду вечную весну. Следы Чёрной пыльцы ведут к Сердцу Цветения.\n\nВпереди — союзы пчелиных домов и борьба за Янтарный престол. Сейчас укрепляй улей и готовь более сильную армию."
        }[StoryStage];

        public string Objective
        {
            get
            {
                if (Level(BuildingKind.Castle) < 2) return "Улучшите цитадель до 2 уровня";
                if (State.trainedTotal < 10) return "Обучите 10 солдат в разделе «Армия»";
                if (State.heroExperience < 20) return State.march != null ? "Дождитесь возвращения отряда Руты" : "Расчистите путь: победите долгоносика";
                if (Level(BuildingKind.Castle) < 3) return "Поднимите цитадель до 3 уровня";
                if (Level(BuildingKind.Camp) < 3) return "Лагерь 3 уровня откроет II ступень войск";
                if (State.monstersDefeated < 3) return "Усильте армию и победите 3 монстров: " + State.monstersDefeated + "/3";
                return "Развивайте улей и бросьте вызов Корневому пауку";
            }
        }
    }
}
