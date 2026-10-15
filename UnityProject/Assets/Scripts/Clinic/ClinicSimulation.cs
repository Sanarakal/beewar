using System;
using System.Collections.Generic;

namespace PlagueGame.Clinic
{
    public enum PatientPhase { Waiting, WalkingToBed, Treating, Leaving }

    [Serializable]
    public sealed class PatientState
    {
        public int id;
        public int bedIndex = -1;
        public PatientPhase phase;
        public float secondsLeft;
        public float treatmentDuration;
    }

    [Serializable]
    public sealed class ClinicState
    {
        public int version = 1;
        public int coins = 20;
        public int herbs = 6;
        public int medicine = 3;
        public int cured;
        public int bedCount = 2;
        public int level = 1;
        public int batchesCrafted;
        public int nextPatientId = 1;
        public float arrivalIn = 8;
        public float craftRemaining;
        public float gardenCooldown;
        public List<PatientState> patients = new List<PatientState>();

        public static ClinicState NewGame()
        {
            var state = new ClinicState();
            for (var i = 0; i < 3; i++)
                state.patients.Add(new PatientState { id = state.nextPatientId++ });
            return state;
        }

        public bool IsValid()
        {
            if (version != 1 || coins < 0 || herbs < 0 || medicine < 0 || cured < 0 ||
                batchesCrafted < 0 || bedCount < 2 || bedCount > 4 || level < 1 || level > 3 ||
                patients == null || patients.Count > 13 || !ValidTime(arrivalIn, 8) ||
                !ValidTime(craftRemaining, 4) || !ValidTime(gardenCooldown, 6)) return false;
            var ids = new HashSet<int>();
            var occupied = new HashSet<int>();
            var largestId = 0;
            foreach (var patient in patients)
            {
                if (patient == null || patient.id < 1 || !ids.Add(patient.id) ||
                    patient.phase < PatientPhase.Waiting || patient.phase > PatientPhase.Leaving ||
                    !ValidTime(patient.secondsLeft, 9) || !ValidTime(patient.treatmentDuration, 9)) return false;
                largestId = Math.Max(largestId, patient.id);
                if (patient.phase == PatientPhase.Waiting)
                {
                    if (patient.bedIndex != -1) return false;
                }
                else if (patient.bedIndex < 0 || patient.bedIndex >= bedCount) return false;
                if ((patient.phase == PatientPhase.WalkingToBed || patient.phase == PatientPhase.Treating) &&
                    (!occupied.Add(patient.bedIndex) || patient.treatmentDuration <= 0)) return false;
            }
            return nextPatientId > largestId && nextPatientId > 0;
        }

        private static bool ValidTime(float value, float maximum) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value <= maximum;
    }

    public readonly struct ClinicAction
    {
        public readonly bool success;
        public readonly string message;
        public ClinicAction(bool success, string message) { this.success = success; this.message = message; }
    }

    public sealed class ClinicSimulation
    {
        public const int MaximumWaiting = 5;
        public const float CraftSeconds = 4;
        public const float GardenSeconds = 6;
        public readonly ClinicState State;
        public int NextBedCost => 30 + (State.bedCount - 2) * 25;
        public int UpgradeCost => State.level * 60;
        public float TreatmentSeconds => 9 - (State.level - 1) * 2;
        public int WaitingCount => CountPatients(false);
        public int BusyBeds => CountPatients(true);
        public bool CanAdmit => WaitingCount > 0 && BusyBeds < State.bedCount && State.medicine > 0;

        public ClinicSimulation(ClinicState state)
        {
            if (state == null || !state.IsValid()) throw new ArgumentException("Invalid clinic save");
            State = state;
        }

        private int CountPatients(bool inBeds)
        {
            var count = 0;
            foreach (var patient in State.patients)
                if (inBeds ? patient.phase == PatientPhase.WalkingToBed || patient.phase == PatientPhase.Treating : patient.phase == PatientPhase.Waiting) count++;
            return count;
        }

        public ClinicAction Admit()
        {
            if (WaitingCount == 0) return new ClinicAction(false, "Новые пациенты скоро придут");
            if (BusyBeds >= State.bedCount) return new ClinicAction(false, "Все кровати заняты");
            if (State.medicine < 1) return new ClinicAction(false, "Сначала приготовьте лекарства");
            var patient = State.patients.Find(p => p.phase == PatientPhase.Waiting);
            var bed = 0;
            while (State.patients.Exists(p => p.bedIndex == bed &&
                (p.phase == PatientPhase.WalkingToBed || p.phase == PatientPhase.Treating))) bed++;
            State.medicine--;
            patient.bedIndex = bed;
            patient.phase = PatientPhase.WalkingToBed;
            patient.secondsLeft = 2;
            patient.treatmentDuration = TreatmentSeconds;
            return new ClinicAction(true, "Пациент направляется в палату");
        }

        public ClinicAction Gather()
        {
            if (State.gardenCooldown > 0) return new ClinicAction(false, "Травам нужно немного времени");
            State.herbs += 4;
            State.gardenCooldown = GardenSeconds;
            return new ClinicAction(true, "+4 лечебные травы");
        }

        public ClinicAction Craft()
        {
            if (State.craftRemaining > 0) return new ClinicAction(false, "Лекарства уже готовятся");
            if (State.herbs < 3) return new ClinicAction(false, "Нужно 3 травы — соберите их в саду");
            State.herbs -= 3;
            State.craftRemaining = CraftSeconds;
            return new ClinicAction(true, "Готовим 2 порции лекарства");
        }

        public ClinicAction BuyBed()
        {
            if (State.bedCount >= 4) return new ClinicAction(false, "Палата полностью оборудована");
            if (State.coins < NextBedCost) return new ClinicAction(false, "Нужно больше монет");
            State.coins -= NextBedCost;
            State.bedCount++;
            return new ClinicAction(true, "В палате появилась новая кровать");
        }

        public ClinicAction Upgrade()
        {
            if (State.level >= 3) return new ClinicAction(false, "Врач достиг максимального уровня");
            if (State.coins < UpgradeCost) return new ClinicAction(false, "Нужно больше монет");
            State.coins -= UpgradeCost;
            State.level++;
            return new ClinicAction(true, "Врач работает быстрее. Доход вырос");
        }

        public void Tick(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0) return;
            // Keep a malformed or very large caller delta from creating an unbounded arrival loop.
            seconds = Math.Min(seconds, 60);
            State.gardenCooldown = Math.Max(0, State.gardenCooldown - seconds);
            if (State.craftRemaining > 0)
            {
                State.craftRemaining = Math.Max(0, State.craftRemaining - seconds);
                if (State.craftRemaining == 0)
                {
                    State.medicine += 2;
                    State.batchesCrafted++;
                }
            }
            for (var i = State.patients.Count - 1; i >= 0; i--)
            {
                var patient = State.patients[i];
                if (patient.phase == PatientPhase.Waiting) continue;
                var remaining = seconds;
                while (remaining > 0)
                {
                    if (patient.secondsLeft > remaining)
                    {
                        patient.secondsLeft -= remaining;
                        break;
                    }
                    remaining -= patient.secondsLeft;
                    if (patient.phase == PatientPhase.WalkingToBed)
                    {
                        patient.phase = PatientPhase.Treating;
                        patient.secondsLeft = patient.treatmentDuration;
                    }
                    else if (patient.phase == PatientPhase.Treating)
                    {
                        State.cured++;
                        State.coins += 18 + State.level * 2;
                        patient.phase = PatientPhase.Leaving;
                        patient.secondsLeft = 2;
                    }
                    else
                    {
                        State.patients.RemoveAt(i);
                        break;
                    }
                }
            }
            State.arrivalIn -= seconds;
            while (State.arrivalIn <= 0)
            {
                State.arrivalIn += 8;
                if (WaitingCount < MaximumWaiting)
                    State.patients.Add(new PatientState { id = State.nextPatientId++ });
            }
        }
    }
}
