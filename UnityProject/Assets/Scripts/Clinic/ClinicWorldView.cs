using System.Collections.Generic;
using UnityEngine;

namespace PlagueGame.Clinic
{
    public sealed class ClinicWorldView : MonoBehaviour
    {
        private sealed class Person
        {
            public Transform root;
            public Transform body;
            public Transform leftArm;
            public Transform rightArm;
        }

        private readonly Dictionary<int, Person> patients = new Dictionary<int, Person>();
        private readonly List<int> removed = new List<int>();
        private readonly List<Transform> plants = new List<Transform>();
        private readonly Transform[] beds = new Transform[4];
        private readonly Transform[] progress = new Transform[4];
        private readonly Transform[] steam = new Transform[3];
        private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        private ClinicSimulation simulation;
        private Person doctor;
        private Camera viewCamera;
        private Material materialTemplate;

        public void Initialize(ClinicSimulation clinic, Material template)
        {
            simulation = clinic;
            materialTemplate = template;
            viewCamera = Camera.main;
            BuildGround();
            BuildClinic();
            BuildGarden();
            BuildWorkshop();
            doctor = MakePerson("Doctor", true, 0);
            doctor.root.position = new Vector3(0, .22f, 1.4f);
        }

        private Material Mat(string color)
        {
            if (materials.TryGetValue(color, out var material)) return material;
            material = materialTemplate != null ? new Material(materialTemplate) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ColorUtility.TryParseHtmlString(color, out var tint);
            material.color = tint;
            material.SetFloat("_Smoothness", .15f);
            materials.Add(color, material);
            return material;
        }

        private Transform Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale, string color, Transform parent = null)
        {
            var item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent == null ? transform : parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = Mat(color);
            Destroy(item.GetComponent<Collider>());
            return item.transform;
        }

        private Transform Box(string name, Vector3 position, Vector3 scale, string color, Transform parent = null) =>
            Shape(name, PrimitiveType.Cube, position, scale, color, parent);

        private void BuildGround()
        {
            Box("Settlement foundation", new Vector3(0, -.3f, -.7f), new Vector3(12, .55f, 13.5f), "#B7BBA2");
            Box("Footpath", new Vector3(0, .005f, -4.6f), new Vector3(2, .06f, 6.2f), "#DED4BF");
            for (var i = 0; i < 7; i++)
                Box("Path stone", new Vector3((i % 2 == 0 ? -.22f : .2f), .05f, -2.3f - i * .65f), new Vector3(.8f, .035f, .43f), "#CEC2AA");
            for (var side = -1; side <= 1; side += 2)
            {
                for (var i = 0; i < 5; i++)
                    Box("Fence post", new Vector3(side * 5.7f, .45f, -5.6f + i * 2.3f), new Vector3(.13f, .9f, .13f), "#8E806A");
                Box("Fence rail", new Vector3(side * 5.7f, .65f, -.9f), new Vector3(.1f, .12f, 9.6f), "#9E8E74");
            }
            for (var i = 0; i < 4; i++)
            {
                var rock = Shape("Stone", PrimitiveType.Sphere, new Vector3(4.7f - i * .65f, .1f, -5.8f - (i % 2) * .35f), new Vector3(.5f, .27f, .4f), "#9DADAA");
                rock.rotation = Quaternion.Euler(0, i * 41, 0);
            }
        }

        private void BuildClinic()
        {
            Box("Clinic floor", new Vector3(0, .13f, 1.15f), new Vector3(7.3f, .25f, 6.4f), "#C9B99B");
            for (var i = 0; i < 8; i++)
                Box("Floor joint", new Vector3(-3.2f + i * .9f, .262f, 1.15f), new Vector3(.018f, .008f, 6.2f), "#B6A68C");
            Box("Back wall", new Vector3(0, 1.1f, 4.25f), new Vector3(7.4f, 1.9f, .22f), "#EAE5D7");
            Box("West wall", new Vector3(-3.55f, .7f, 1.2f), new Vector3(.2f, 1.1f, 6.1f), "#EAE5D7");
            Box("East wall", new Vector3(3.55f, .7f, 1.2f), new Vector3(.2f, 1.1f, 6.1f), "#DDD8CA");
            for (var side = -1; side <= 1; side += 2)
            {
                Box("Front wall", new Vector3(side * 2.3f, .5f, -1.95f), new Vector3(2.5f, .7f, .2f), "#DDD8CA");
                Box("Window surround", new Vector3(side * 2.2f, 1.25f, 4.1f), new Vector3(1.3f, .8f, .13f), "#8FACA8");
                Box("Window mullion", new Vector3(side * 2.2f, 1.25f, 4.01f), new Vector3(.08f, .8f, .08f), "#F4F0E4");
            }
            Box("Clinic sign", new Vector3(0, 2.3f, 4.1f), new Vector3(1.35f, 1.1f, .13f), "#F3F0E5");
            Box("Medical cross horizontal", new Vector3(0, 2.3f, 3.99f), new Vector3(.74f, .23f, .08f), "#4D8276");
            Box("Medical cross vertical", new Vector3(0, 2.3f, 3.98f), new Vector3(.23f, .74f, .08f), "#4D8276");
            Box("Entry step", new Vector3(0, .15f, -2.2f), new Vector3(1.8f, .3f, .7f), "#DDD6C4");
            for (var i = 0; i < 4; i++)
            {
                var root = new GameObject("Bed " + (i + 1)).transform;
                root.SetParent(transform, false);
                root.position = BedPosition(i);
                beds[i] = root;
                Box("Frame", new Vector3(0, .3f, 0), new Vector3(1.25f, .28f, 2.05f), "#8B7964", root);
                Box("Mattress", new Vector3(0, .53f, 0), new Vector3(1.15f, .2f, 1.95f), "#F2EDDF", root);
                Box("Blanket", new Vector3(0, .65f, -.24f), new Vector3(1.15f, .045f, 1.2f), "#729B97", root);
                Box("Pillow", new Vector3(0, .68f, .68f), new Vector3(.87f, .13f, .37f), "#FFFAED", root);
                Box("Headboard", new Vector3(0, .75f, 1.02f), new Vector3(1.26f, .8f, .1f), "#A28F73", root);
                Box("Progress background", new Vector3(0, 1.65f, 0), new Vector3(1.18f, .1f, .16f), "#D7DECF", root);
                var fill = Box("Treatment progress", new Vector3(0, 1.66f, -.015f), new Vector3(1.15f, .12f, .17f), "#4E8A73", root);
                progress[i] = fill;
            }
        }

        public static Vector3 BedPosition(int index) => new Vector3(index % 2 == 0 ? -1.8f : 1.8f, .24f, index < 2 ? 2.55f : -.05f);

        private void BuildGarden()
        {
            Box("Herb garden frame", new Vector3(-4.75f, .16f, -.2f), new Vector3(1.65f, .32f, 3.25f), "#8B7964");
            Box("Garden soil", new Vector3(-4.75f, .34f, -.2f), new Vector3(1.48f, .06f, 3.06f), "#6C6552");
            for (var x = 0; x < 2; x++)
            for (var z = 0; z < 5; z++)
            {
                var plant = Shape("Medicinal plant", PrimitiveType.Capsule, new Vector3(-5.1f + x * .65f, .63f, -1.4f + z * .57f), new Vector3(.25f, .24f, .25f), z % 2 == 0 ? "#668B65" : "#7F9D6C");
                plants.Add(plant);
            }
        }

        private void BuildWorkshop()
        {
            Box("Apothecary table", new Vector3(4.6f, .63f, .6f), new Vector3(1.55f, .22f, 2.4f), "#A68C6C");
            for (var i = 0; i < 2; i++)
                Box("Table leg", new Vector3(4.6f, .3f, -.15f + i * 1.5f), new Vector3(1.15f, .6f, .16f), "#8B7964");
            Shape("Medicine pot", PrimitiveType.Cylinder, new Vector3(4.6f, .93f, 1), new Vector3(.9f, .22f, .9f), "#626B65");
            Shape("Herbal broth", PrimitiveType.Cylinder, new Vector3(4.6f, 1.16f, 1), new Vector3(.75f, .015f, .75f), "#97AE79");
            for (var i = 0; i < 3; i++)
                steam[i] = Shape("Steam", PrimitiveType.Sphere, new Vector3(4.6f, 1.6f, 1), Vector3.one * .18f, "#F0EFE3");
            for (var i = 0; i < 3; i++)
                Shape("Medicine bottle", PrimitiveType.Capsule, new Vector3(4.2f + .38f * i, .93f, -.1f), new Vector3(.17f, .22f, .17f), "#668B7F");
            Box("Supplies crate", new Vector3(4.65f, .4f, -1.9f), new Vector3(1.1f, .8f, 1), "#AF9978");
            Box("Crate band", new Vector3(4.65f, .42f, -2.41f), new Vector3(.15f, .8f, .03f), "#8E806A");
        }

        private Person MakePerson(string name, bool isDoctor, int variant)
        {
            var person = new Person();
            person.root = new GameObject(name).transform;
            person.root.SetParent(transform, false);
            person.body = new GameObject("Animated body").transform;
            person.body.SetParent(person.root, false);
            var coat = isDoctor ? "#F5F0E4" : new[] { "#A77E63", "#839194", "#A29A77" }[variant % 3];
            Shape("Coat", PrimitiveType.Capsule, new Vector3(0, .65f, 0), new Vector3(.42f, .35f, .32f), coat, person.body);
            Shape("Head", PrimitiveType.Sphere, new Vector3(0, 1.13f, 0), Vector3.one * .3f, "#D0A989", person.body);
            Box("Hair", new Vector3(0, 1.25f, .02f), new Vector3(.28f, .1f, .26f), isDoctor ? "#D8D5C9" : "#695F53", person.body);
            Box("Leg left", new Vector3(-.1f, .22f, 0), new Vector3(.13f, .43f, .16f), "#5C696A", person.body);
            Box("Leg right", new Vector3(.1f, .22f, 0), new Vector3(.13f, .43f, .16f), "#5C696A", person.body);
            person.leftArm = Box("Left arm", new Vector3(-.28f, .68f, 0), new Vector3(.13f, .47f, .15f), coat, person.body);
            person.rightArm = Box("Right arm", new Vector3(.28f, .68f, 0), new Vector3(.13f, .47f, .15f), coat, person.body);
            if (isDoctor)
            {
                Box("Cross horizontal", new Vector3(0, .75f, -.18f), new Vector3(.19f, .06f, .02f), "#4D8276", person.body);
                Box("Cross vertical", new Vector3(0, .75f, -.19f), new Vector3(.06f, .19f, .02f), "#4D8276", person.body);
            }
            return person;
        }

        public void Render(float delta)
        {
            if (simulation == null) return;
            if (viewCamera != null) viewCamera.orthographicSize = Mathf.Max(11.4f, 6.8f / viewCamera.aspect);
            for (var i = 0; i < 4; i++)
            {
                beds[i].gameObject.SetActive(i < simulation.State.bedCount);
                var occupant = simulation.State.patients.Find(p => p.bedIndex == i &&
                    (p.phase == PatientPhase.Treating || p.phase == PatientPhase.WalkingToBed));
                progress[i].parent.Find("Progress background").gameObject.SetActive(occupant != null);
                progress[i].gameObject.SetActive(occupant != null);
                if (occupant != null)
                {
                    var fraction = occupant.phase == PatientPhase.Treating ? 1 - occupant.secondsLeft / occupant.treatmentDuration : .02f;
                    progress[i].localScale = new Vector3(Mathf.Max(.02f, fraction) * 1.15f, .12f, .17f);
                    progress[i].localPosition = new Vector3(-.575f + fraction * .575f, 1.66f, -.015f);
                }
            }
            var queueIndex = 0;
            foreach (var patient in simulation.State.patients)
            {
                if (!patients.TryGetValue(patient.id, out var actor))
                {
                    actor = MakePerson("Patient " + patient.id, false, patient.id);
                    actor.root.position = new Vector3(0, .05f, -7.1f);
                    patients.Add(patient.id, actor);
                }
                Vector3 target;
                if (patient.phase == PatientPhase.Waiting)
                    target = new Vector3(-.7f, .04f, -2.9f - queueIndex++ * .72f);
                else if (patient.phase == PatientPhase.Leaving)
                    target = new Vector3(.7f, .04f, -7.6f);
                else target = BedPosition(patient.bedIndex) + new Vector3(0, patient.phase == PatientPhase.Treating ? .78f : 0, -.45f);
                if (patient.phase == PatientPhase.Treating)
                {
                    actor.root.position = target;
                    actor.root.rotation = Quaternion.Euler(90, 0, 0);
                    actor.body.localPosition = Vector3.zero;
                }
                else Move(actor, target, delta, 4.5f);
            }
            removed.Clear();
            foreach (var entry in patients)
                if (!simulation.State.patients.Exists(p => p.id == entry.Key)) removed.Add(entry.Key);
            foreach (var id in removed) { Destroy(patients[id].root.gameObject); patients.Remove(id); }
            var treating = simulation.State.patients.Find(p => p.phase == PatientPhase.Treating);
            var doctorTarget = treating == null ? new Vector3(0, .26f, 1.5f) : BedPosition(treating.bedIndex) + new Vector3(treating.bedIndex % 2 == 0 ? .9f : -.9f, 0, 0);
            Move(doctor, doctorTarget, delta, 3);
            if (treating != null && Vector3.Distance(doctor.root.position, doctorTarget) < .1f)
                doctor.rightArm.localRotation = Quaternion.Euler(-35 + Mathf.Sin(Time.time * 5) * 18, 0, 0);
            for (var i = 0; i < plants.Count; i++)
                plants[i].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 1.6f + i) * 5);
            for (var i = 0; i < steam.Length; i++)
            {
                steam[i].gameObject.SetActive(simulation.State.craftRemaining > 0);
                steam[i].localPosition = new Vector3(4.6f + Mathf.Sin(Time.time + i) * .17f, 1.35f + Mathf.Repeat(Time.time * .55f + i * .3f, 1), 1);
            }
        }

        private static void Move(Person actor, Vector3 target, float delta, float speed)
        {
            var displacement = target - actor.root.position;
            actor.root.position = Vector3.MoveTowards(actor.root.position, target, delta * speed);
            var walking = displacement.sqrMagnitude > .01f;
            if (walking)
            {
                displacement.y = 0;
                if (displacement.sqrMagnitude > .001f)
                    actor.root.rotation = Quaternion.Slerp(actor.root.rotation, Quaternion.LookRotation(displacement), delta * 12);
            }
            else actor.root.rotation = Quaternion.Euler(0, 180, 0);
            actor.body.localPosition = new Vector3(0, walking ? Mathf.Sin(Time.time * 12) * .045f : 0, 0);
            actor.leftArm.localRotation = Quaternion.Euler(walking ? Mathf.Sin(Time.time * 12) * 24 : 0, 0, 0);
            actor.rightArm.localRotation = Quaternion.Euler(walking ? -Mathf.Sin(Time.time * 12) * 24 : 0, 0, 0);
        }

        private void OnDestroy() { foreach (var material in materials.Values) Destroy(material); }
    }
}
