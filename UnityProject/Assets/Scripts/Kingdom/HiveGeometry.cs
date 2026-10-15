using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace PlagueGame.Kingdom
{
    public sealed partial class KingdomWorldView
    {
        private Transform Shape(string name,PrimitiveType type,Vector3 p,Vector3 size,string color,Transform root)
        {
            if(!primitives.TryGetValue(type,out var mesh))
            {
                if(type==PrimitiveType.Sphere) mesh=LowSphere();
                else if(type==PrimitiveType.Cylinder) mesh=HexPrism();
                else { var source=GameObject.CreatePrimitive(type); source.SetActive(false); mesh=source.GetComponent<MeshFilter>().sharedMesh; Destroy(source); }
                primitives.Add(type,mesh);
            }
            return MeshObject(name,root,p,size,mesh,color);
        }
        private Transform MeshObject(string name,Transform parent,Vector3 p,Vector3 size,Mesh mesh,string color)
        {
            var root=Root(name,parent,p); root.localScale=size; root.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=root.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial=Mat(color); renderer.shadowCastingMode=ShadowCastingMode.On; return root;
        }
        private Transform Box(string name,Vector3 p,Vector3 size,string color,Transform root) => Shape(name,PrimitiveType.Cube,p,size,color,root);
        private void Beam(Transform root,Vector3 from,Vector3 to,float width,string color)
        {
            var beam=Shape("Bark strut",PrimitiveType.Cylinder,(from+to)*.5f,new Vector3(width*2,Vector3.Distance(from,to),width*2),color,root); beam.localRotation=Quaternion.FromToRotation(Vector3.up,to-from);
        }
        private void Leaf(Transform root,Vector3 p,Vector3 size,string color,float angle)
        {
            var leaf=Shape("Carved leaf",PrimitiveType.Sphere,p,size,color,root); leaf.localRotation=Quaternion.Euler(-15,angle,0);
        }
        private void Roof(Transform root,Vector3 p,Vector3 size,string color,int sides)
        {
            var v=new List<Vector3>(); var t=new List<int>();
            for(var i=0;i<sides;i++) { var a=(i+.5f)*Mathf.PI*2/sides; var b=(i+1.5f)*Mathf.PI*2/sides; var n=v.Count; v.Add(new Vector3(Mathf.Cos(a),-.5f,Mathf.Sin(a))); v.Add(Vector3.up*.5f); v.Add(new Vector3(Mathf.Cos(b),-.5f,Mathf.Sin(b))); t.AddRange(new[]{n,n+1,n+2}); }
            var mesh=new Mesh{name="Leaf roof",vertices=v.ToArray(),triangles=t.ToArray()}; mesh.RecalculateNormals(); meshes.Add(mesh); MeshObject("Leaf roof",root,p,size,mesh,color);
        }
        private Transform Ring(Transform root,Vector3 p,float radius,string color)
        {
            var v=new List<Vector3>(); var t=new List<int>();
            for(var i=0;i<48;i++) { var a=i*Mathf.PI/24; var b=(i+1)*Mathf.PI/24; var n=v.Count; v.Add(new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*radius); v.Add(new Vector3(Mathf.Sin(b),0,Mathf.Cos(b))*radius); v.Add(new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*(radius-.12f)); v.Add(new Vector3(Mathf.Sin(b),0,Mathf.Cos(b))*(radius-.12f)); t.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3}); }
            var mesh=new Mesh{name="Amber selection ring",vertices=v.ToArray(),triangles=t.ToArray()}; mesh.RecalculateNormals(); meshes.Add(mesh); return MeshObject("Amber ring",root,p,Vector3.one,mesh,color);
        }
        private Mesh LowSphere()
        {
            const int slices=12,rings=8; var v=new List<Vector3>(); var t=new List<int>();
            for(var y=0;y<=rings;y++) for(var x=0;x<=slices;x++) { var a=x*Mathf.PI*2/slices; var b=y*Mathf.PI/rings; v.Add(new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a))*.5f); }
            for(var y=0;y<rings;y++) for(var x=0;x<slices;x++) { var n=y*(slices+1)+x; t.AddRange(new[]{n,n+1,n+slices+1,n+1,n+slices+2,n+slices+1}); }
            var mesh=new Mesh{name="Organic shape",vertices=v.ToArray(),triangles=t.ToArray()}; mesh.RecalculateNormals(); meshes.Add(mesh); return mesh;
        }
        private Mesh HexPrism()
        {
            var v=new List<Vector3>(); var t=new List<int>();
            for(var i=0;i<6;i++) { var a=(i+.5f)*Mathf.PI/3; var b=(i+1.5f)*Mathf.PI/3; var n=v.Count; var pa=new Vector3(Mathf.Cos(a)*.5f,0,Mathf.Sin(a)*.5f); var pb=new Vector3(Mathf.Cos(b)*.5f,0,Mathf.Sin(b)*.5f); v.AddRange(new[]{pa+Vector3.up*.5f,pb+Vector3.up*.5f,pa-Vector3.up*.5f,pb-Vector3.up*.5f,Vector3.up*.5f,Vector3.down*.5f}); t.AddRange(new[]{n,n+1,n+2,n+1,n+3,n+2,n+4,n+1,n,n+5,n+2,n+3}); }
            var mesh=new Mesh{name="Wax hex prism",vertices=v.ToArray(),triangles=t.ToArray()}; mesh.RecalculateNormals(); meshes.Add(mesh); return mesh;
        }
        private void Batch(Transform root)
        {
            var groups=new Dictionary<Material,List<CombineInstance>>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=filter.GetComponent<MeshRenderer>(); if(renderer==null||!renderer.enabled) continue;
                var mat=renderer.sharedMaterial; if(!groups.TryGetValue(mat,out var parts)) { parts=new List<CombineInstance>(); groups.Add(mat,parts); }
                parts.Add(new CombineInstance{mesh=filter.sharedMesh,transform=root.worldToLocalMatrix*filter.transform.localToWorldMatrix}); renderer.enabled=false;
            }
            foreach(var group in groups) { var mesh=new Mesh{name="Combined hive geometry",indexFormat=IndexFormat.UInt32}; mesh.CombineMeshes(group.Value.ToArray()); meshes.Add(mesh); var node=Root("Combined surface",root,Vector3.zero); node.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh; var renderer=node.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial=group.Key; renderer.shadowCastingMode=ShadowCastingMode.On; }
        }
        private void Window(Transform root,Vector3 p,float size)
        {
            var frame=Shape("Honeycomb frame",PrimitiveType.Cylinder,p,new Vector3(size,.12f,size),Bark,root); frame.localRotation=Quaternion.Euler(90,0,0);
            var glass=Shape("Amber glass",PrimitiveType.Cylinder,p+Vector3.back*.08f,new Vector3(size*.75f,.1f,size*.75f),Amber,root); glass.localRotation=Quaternion.Euler(90,0,0);
        }
        private void Shield(Transform root,Vector3 p,float size,string color)
        {
            var shield=Shape("Hex shield",PrimitiveType.Cylinder,p,new Vector3(size,.15f,size*1.25f),color,root); shield.localRotation=Quaternion.Euler(90,0,0);
            var seal=Shape("Shield seal",PrimitiveType.Cylinder,p+Vector3.back*.1f,new Vector3(size*.4f,.08f,size*.5f),Wax,root); seal.localRotation=Quaternion.Euler(90,0,0);
        }
        private void Banner(Transform root,Vector3 p,string color)
        {
            Beam(root,p,p+Vector3.up*2.4f,.065f,Bark); Box("Leaf banner",p+new Vector3(.35f,1.95f,0),new Vector3(.72f,.75f,.08f),color,root); Shape("Banner seal",PrimitiveType.Sphere,p+new Vector3(.35f,1.95f,-.06f),new Vector3(.26f,.26f,.04f),Wax,root);
        }
        private void Lantern(Transform root,Vector3 p)
        {
            Beam(root,p,p+Vector3.up*.7f,.065f,Bark); Shape("Amber lantern",PrimitiveType.Sphere,p+Vector3.up*.8f,new Vector3(.4f,.55f,.4f),Amber,root); Roof(root,p+Vector3.up*1.15f,new Vector3(.35f,.25f,.35f),Green,6);
        }
        private void Flower(Transform root,Vector3 p,float height,string color)
        {
            Beam(root,p,p+new Vector3(.15f,height,0),.11f+height*.02f,Green); Leaf(root,p+Vector3.up*height*.4f,new Vector3(height*.32f,.12f,height*.6f),"#4E7A4E",-40);
            var top=p+new Vector3(.15f,height,0); var radius=height*.28f;
            for(var i=0;i<6;i++) { var a=i*Mathf.PI/3; Shape("Flower petal",PrimitiveType.Sphere,top+new Vector3(Mathf.Cos(a)*radius*.6f,0,Mathf.Sin(a)*radius*.6f),new Vector3(radius,radius*.24f,radius),color,root); }
            Shape("Pollen heart",PrimitiveType.Sphere,top+Vector3.up*.12f,new Vector3(radius*.85f,radius*.45f,radius*.85f),Amber,root);
        }
        private void Fern(Transform root,Vector3 p,float size) { for(var i=0;i<5;i++) Leaf(root,p+Vector3.up*(.2f+i*.17f),new Vector3(size*.4f,.1f,size),i%2==0?"#44744D":"#638552",i*73); }
        private void Path(Vector3 from,Vector3 to,float width,Transform root) { var center=(from+to)*.5f; center.y=.11f; var road=Box("Wax path",center,new Vector3(width,.06f,Vector3.Distance(from,to)),"#C9B77D",root); road.localRotation=Quaternion.LookRotation(to-from); }
    }
}
