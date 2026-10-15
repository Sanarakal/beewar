using System.Text.Json;
using PlagueGame.Clinic;

var passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
    passed++;
}

ClinicState Copy(ClinicState state)
{
    var options = new JsonSerializerOptions { IncludeFields = true };
    return JsonSerializer.Deserialize<ClinicState>(JsonSerializer.Serialize(state, options), options);
}

var clinic = new ClinicSimulation(ClinicState.NewGame());
Check(clinic.State.IsValid() && clinic.WaitingCount == 3, "New clinic has a valid initial queue");
Check(clinic.Admit().success && clinic.State.medicine == 2 && clinic.BusyBeds == 1, "Admission reserves one bed and one dose");
Check(clinic.Admit().success && !clinic.Admit().success && clinic.State.medicine == 1, "Full clinic rejects another admission without spending medicine");
Check(clinic.State.IsValid(), "Concurrent admissions use distinct beds");
clinic.Tick(2);
Check(clinic.State.patients.Count(p => p.phase == PatientPhase.Treating) == 2 && clinic.State.cured == 0, "Patients reach their beds before treatment completes");
clinic.Tick(9);
Check(clinic.State.cured == 2 && clinic.State.coins == 60 && clinic.BusyBeds == 0, "Each treatment pays once and releases its bed");
var restored = new ClinicSimulation(Copy(clinic.State));
restored.Tick(2);
Check(restored.State.cured == 2 && restored.State.coins == 60 && restored.State.IsValid(), "Reloading a departing patient never duplicates its reward");
clinic.State.medicine = 0;
Check(!clinic.Admit().success && clinic.State.medicine == 0, "Zero medicine cannot start treatment");

clinic = new ClinicSimulation(ClinicState.NewGame());
Check(clinic.Gather().success && !clinic.Gather().success && clinic.State.herbs == 10, "Gathering cooldown prevents repeated rewards");
clinic.Tick(6);
Check(clinic.Gather().success && clinic.State.herbs == 14, "Garden can be collected again after its cooldown");
Check(clinic.Craft().success && clinic.State.herbs == 11 && !clinic.Craft().success, "A medicine batch consumes its herbs once");
clinic.Tick(4);
Check(clinic.State.medicine == 5 && clinic.State.batchesCrafted == 1, "A finished batch produces two doses");
clinic.Tick(4);
Check(clinic.State.medicine == 5 && clinic.State.batchesCrafted == 1, "A completed batch cannot produce medicine twice");
clinic.State.herbs = 2;
Check(!clinic.Craft().success && clinic.State.herbs == 2, "Insufficient herbs cannot be spent");

clinic = new ClinicSimulation(ClinicState.NewGame());
Check(!clinic.BuyBed().success && clinic.State.coins == 20, "An unaffordable bed does not spend coins");
clinic.State.coins = 30;
Check(clinic.BuyBed().success && clinic.State.bedCount == 3 && clinic.State.coins == 0, "A third bed costs exactly thirty coins");
clinic.State.coins = 55;
Check(clinic.BuyBed().success && !clinic.BuyBed().success && clinic.State.bedCount == 4 && clinic.State.coins == 0, "A fourth bed costs fifty-five and the bed limit is enforced");
clinic.State.coins = 180;
Check(clinic.Upgrade().success && clinic.TreatmentSeconds == 7 && clinic.State.coins == 120, "Doctor improvement accelerates future treatment");
Check(clinic.Upgrade().success && !clinic.Upgrade().success && clinic.State.level == 3 && clinic.State.coins == 0, "Doctor upgrade cost and maximum level are enforced");

clinic = new ClinicSimulation(ClinicState.NewGame());
clinic.Admit();
clinic.Tick(5);
restored = new ClinicSimulation(Copy(clinic.State));
restored.Tick(6);
Check(restored.State.cured == 1 && restored.State.medicine == 2 && restored.State.IsValid(), "An in-progress treatment survives a save and reload");
restored.Tick(60);
Check(restored.WaitingCount == ClinicSimulation.MaximumWaiting && restored.State.IsValid(), "Patient arrivals stop at queue capacity");
var invalid = Copy(restored.State);
invalid.coins = -1;
Check(!invalid.IsValid(), "Corrupt negative currency is rejected");
invalid = Copy(restored.State);
invalid.patients[1].id = invalid.patients[0].id;
Check(!invalid.IsValid(), "Duplicate patient identities are rejected");
invalid = Copy(restored.State);
invalid.arrivalIn = float.NaN;
Check(!invalid.IsValid(), "Invalid timer values are rejected");
Console.WriteLine($"CLINIC_CHECKS_OK {passed}");
