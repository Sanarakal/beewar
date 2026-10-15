using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace PlagueGame.Kingdom
{
    public sealed class KingdomTarget : MonoBehaviour { public bool monster; public int index; }
    public sealed partial class KingdomWorldView : MonoBehaviour
    {
        private const string Wax = "#F2E4BE", Amber = "#E8AC45", Bark = "#604735", Green = "#386751", Dark = "#302D2C", Blight = "#8769B2";
        private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        private readonly Dictionary<PrimitiveType, Mesh> primitives = new Dictionary<PrimitiveType, Mesh>();
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Transform> citizens = new List<Transform>(), wings = new List<Transform>();
        private readonly Transform[] buildings = new Transform[KingdomState.BuildingCount], monsters = new Transform[3];
        private readonly Transform[,] growth = new Transform[KingdomState.BuildingCount, 2];
        private readonly Vector3[] sites = { new Vector3(0,0,4.3f), new Vector3(-5,0,-4), new Vector3(5.2f,0,-3.6f), new Vector3(-5.2f,0,1.1f), new Vector3(5.1f,0,1.5f), new Vector3(0,0,-4.7f), new Vector3(5.6f,0,6.8f) };
        private readonly Vector3[] targets = { new Vector3(-7,0,4), new Vector3(6,0,1), new Vector3(7,0,10) };
        private readonly Vector3 home = new Vector3(-8,0,-7);
        private KingdomSimulation simulation;
        private Material template;
        private Camera cameraView;
        private Transform village, wilderness, heroStage, hero, selection, mapSelection, scaffold, marching;
        private KingdomTab currentTab;
        private Vector3 pan;
        private float zoom;
        public void Initialize(KingdomSimulation rules, Material material)
        {
            simulation = rules; template = material; cameraView = Camera.main;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = Hex("#B4C6AD");
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (light.type == LightType.Directional) { light.color = Hex("#FFF0CE"); light.intensity = 1.25f; light.shadowBias = .06f; light.shadowNormalBias = .25f; light.shadowStrength = .6f; light.transform.rotation = Quaternion.Euler(48,-38,0); }
            var backdrop = GameObject.Find("Background camera"); if (backdrop != null) backdrop.GetComponent<Camera>().backgroundColor = Hex("#E9E4D1");
            village = Root("Hive among roots", transform, Vector3.zero); wilderness = Root("Tsvetolesye", transform, Vector3.zero); heroStage = Root("Ruta showcase", transform, Vector3.zero);
            BuildVillage(); BuildMap(); BuildHeroStage();
            selection = Ring(village, Vector3.zero, 2.65f, "#F5C66A"); mapSelection = Ring(wilderness, targets[0], 2.6f, "#F5C66A");
            scaffold = Root("Construction scaffold", village, Vector3.zero);
            for (var x=-1;x<=1;x+=2) for (var z=-1;z<=1;z+=2) Beam(scaffold,new Vector3(x*1.7f,0,z*1.4f),new Vector3(x*1.7f,3.2f,z*1.4f),.1f,Bark);
            Beam(scaffold,new Vector3(-1.7f,2.7f,-1.4f),new Vector3(1.7f,2.7f,-1.4f),.12f,Amber); Batch(scaffold);
            marching = Root("Wing guard convoy", wilderness, home); Bee(marching,new Vector3(0,.1f,.7f),1,true,true);
            for (var i=0;i<2;i++) Bee(marching,new Vector3((i==0?-1:1)*.65f,0,-.35f),.75f,true,false);
            SetTab(KingdomTab.Settlement);
        }
        public Vector3 LabelPosition(int index,bool monster) => monster ? targets[index]+Vector3.up*3.4f : sites[index]+Vector3.up*(index==0?5.7f:3.15f);
        public void SetTab(KingdomTab tab)
        {
            currentTab=tab; pan=Vector3.zero; zoom=tab==KingdomTab.Map?18:tab==KingdomTab.Army?5.0f:12.6f;
            village.gameObject.SetActive(tab==KingdomTab.Settlement); wilderness.gameObject.SetActive(tab==KingdomTab.Map); heroStage.gameObject.SetActive(tab==KingdomTab.Army);
        }
        public void Pan(Vector2 delta)
        {
            var right=cameraView.transform.right; right.y=0; var up=cameraView.transform.up; up.y=0; up.Normalize();
            pan-=(right*delta.x+up*delta.y)*(zoom*2/Mathf.Max(1,cameraView.pixelHeight));
            var bound=currentTab==KingdomTab.Map?6:currentTab==KingdomTab.Army?1.5f:4; pan.x=Mathf.Clamp(pan.x,-bound,bound); pan.z=Mathf.Clamp(pan.z,-bound,bound);
        }
        public void Zoom(float delta) => zoom=Mathf.Clamp(zoom+delta,currentTab==KingdomTab.Army?3:currentTab==KingdomTab.Map?12:9,currentTab==KingdomTab.Army?6:currentTab==KingdomTab.Map?24:17);
        public void Render(BuildingKind selected,int selectedMonster)
        {
            var scale=Screen.width/1080f; var safe=Screen.safeArea; var bottom=safe.yMin+750*scale; var top=safe.yMax-365*scale;
            cameraView.rect=new Rect(0,bottom/Screen.height,1,Mathf.Max(.15f,(top-bottom)/Screen.height));
            var center=(currentTab==KingdomTab.Map?new Vector3(0,.3f,1):currentTab==KingdomTab.Army?new Vector3(0,2.8f,0):new Vector3(0,.8f,0))+pan;
            cameraView.transform.position=center+(currentTab==KingdomTab.Army?new Vector3(7,5,-14):new Vector3(17,25,-23)); cameraView.transform.LookAt(center);
            cameraView.orthographicSize=currentTab==KingdomTab.Settlement?Mathf.Max(zoom,12.6f/cameraView.aspect):zoom;
            cameraView.backgroundColor=Hex(currentTab==KingdomTab.Army?"#243F35":"#526B4D");
            for (var i=0;i<buildings.Length;i++) { var level=simulation.Level((BuildingKind)i); buildings[i].localScale=Vector3.one*(1+(level-1)*.025f); for(var j=0;j<2;j++) growth[i,j].gameObject.SetActive(level>=(j==0?3:5)); }
            selection.localPosition=sites[(int)selected]+Vector3.up*.18f; selection.localScale=Vector3.one*(selected==BuildingKind.Castle?1.25f:.85f); mapSelection.localPosition=targets[selectedMonster]+Vector3.up*.1f;
            scaffold.gameObject.SetActive(simulation.State.upgrade!=null); if(simulation.State.upgrade!=null) scaffold.localPosition=sites[(int)simulation.State.upgrade.building];
            for(var i=0;i<citizens.Count;i++) { var t=Time.time*.25f+i*1.6f; citizens[i].localPosition=new Vector3(Mathf.Sin(t)*(2.2f+i%2),.35f+Mathf.Sin(Time.time*3+i)*.1f,Mathf.Cos(t)*2-.5f); citizens[i].localRotation=Quaternion.Euler(0,-t*Mathf.Rad2Deg+180,0); }
            foreach(var wing in wings) if(wing.gameObject.activeInHierarchy) wing.localRotation=Quaternion.Euler(0,0,Mathf.Sin(Time.time*20)*16*Mathf.Sign(wing.localPosition.x));
            hero.localRotation=Quaternion.Euler(0,-12+Mathf.Sin(Time.time*.4f)*9,0); hero.localPosition=new Vector3(0,.45f+Mathf.Sin(Time.time*2)*.06f,0);
            for(var i=0;i<3;i++) { monsters[i].gameObject.SetActive(simulation.State.monsterReadyAt[i]<=simulation.State.lastUtc); monsters[i].localPosition=targets[i]+Vector3.up*(.08f+Mathf.Sin(Time.time*2+i)*.05f); }
            var march=simulation.State.march; marching.gameObject.SetActive(march!=null);
            if(march!=null) { var outbound=march.phase==MarchPhase.Outbound; var start=outbound?march.startedAt:march.arrivesAt; var end=outbound?march.arrivesAt:march.homeAt; var t=Mathf.Clamp01((float)((simulation.State.lastUtc-start)/(end-start))); var from=outbound?home:targets[march.monster]; var to=outbound?targets[march.monster]:home; marching.localPosition=Vector3.Lerp(from,to,t)+Vector3.up*(.25f+Mathf.Abs(Mathf.Sin(Time.time*5))*.1f); marching.localRotation=Quaternion.LookRotation(from-to); }
        }
        private static Transform Root(string name,Transform parent,Vector3 p) { var root=new GameObject(name).transform; root.SetParent(parent,false); root.localPosition=p; return root; }
        private static Color Hex(string value) { ColorUtility.TryParseHtmlString(value,out var color); return color; }
        private Material Mat(string color) { if(materials.TryGetValue(color,out var mat)) return mat; mat=new Material(template); mat.color=Hex(color); mat.SetFloat("_Smoothness",.18f); mat.enableInstancing=true; materials.Add(color,mat); return mat; }
        private void OnDestroy() { foreach(var mat in materials.Values) Destroy(mat); foreach(var mesh in meshes) Destroy(mesh); }
    }
}
