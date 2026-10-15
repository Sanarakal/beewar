using UnityEngine;
namespace PlagueGame.Kingdom
{
    public sealed partial class KingdomWorldView
    {
        private Transform Bee(Transform parent,Vector3 p,float scale,bool armed,bool ruta)
        {
            var root=Root(ruta?"Ruta the pathfinder":"Sting guard",parent,p); root.localScale=Vector3.one*scale;
            var body=Root("Bee body",root,Vector3.zero);
            Shape("Abdomen",PrimitiveType.Sphere,new Vector3(0,.8f,.27f),new Vector3(.9f,.9f,.8f),Amber,body);
            for(var i=0;i<2;i++) Shape("Abdomen stripe",PrimitiveType.Cylinder,new Vector3(0,.62f+i*.3f,.28f),new Vector3(.87f-i*.05f,.14f,.77f),Dark,body);
            Shape("Thorax coat",PrimitiveType.Sphere,new Vector3(0,1.24f,0),new Vector3(.76f,.75f,.65f),ruta?Green:Bark,body);
            Shape("Fluffy collar",PrimitiveType.Sphere,new Vector3(0,1.53f,-.02f),new Vector3(.83f,.25f,.69f),"#E4C884",body);
            Shape("Head",PrimitiveType.Sphere,new Vector3(0,1.83f,-.09f),new Vector3(.91f,.85f,.78f),Amber,body);
            Shape("Face",PrimitiveType.Sphere,new Vector3(0,1.79f,-.4f),new Vector3(.72f,.57f,.23f),"#F5D899",body);
            Beam(body,new Vector3(-.07f,1.68f,-.51f),new Vector3(.07f,1.68f,-.51f),.018f,Bark);
            for(var s=-1;s<=1;s+=2)
            {
                Shape("Eye",PrimitiveType.Sphere,new Vector3(s*.23f,1.9f,-.48f),new Vector3(.16f,.23f,.1f),Dark,body);
                Shape("Eye light",PrimitiveType.Sphere,new Vector3(s*.22f,1.96f,-.526f),Vector3.one*.05f,Wax,body);
                Shape("Cheek",PrimitiveType.Sphere,new Vector3(s*.29f,1.74f,-.49f),new Vector3(.14f,.075f,.025f),"#D39163",body);
                Beam(body,new Vector3(s*.22f,2.1f,-.05f),new Vector3(s*.38f,2.52f,-.08f),.045f,Dark);
                Shape("Antenna tip",PrimitiveType.Sphere,new Vector3(s*.38f,2.52f,-.08f),Vector3.one*.12f,Dark,body);
                Beam(body,new Vector3(s*.22f,.55f,0),new Vector3(s*.3f,.13f,-.08f),.115f,Dark);
                Shape("Boot",PrimitiveType.Sphere,new Vector3(s*.3f,.12f,-.17f),new Vector3(.28f,.2f,.4f),Bark,body);
                Beam(body,new Vector3(s*.36f,1.3f,0),new Vector3(s*.63f,.95f,-.18f),.12f,ruta?Green:Bark);
                Shape("Hand",PrimitiveType.Sphere,new Vector3(s*.63f,.95f,-.18f),Vector3.one*.22f,Amber,body);
                var hinge=Root("Wing hinge",root,new Vector3(s*.32f,1.4f,.34f)); wings.Add(hinge);
                Leaf(hinge,new Vector3(s*.5f,.2f,.08f),new Vector3(.55f,.09f,1.15f),"#C6E6C5",s*-48);
                Leaf(hinge,new Vector3(s*.4f,-.16f,.13f),new Vector3(.37f,.08f,.75f),"#9DBFA6",s*-74);
                Beam(hinge,Vector3.zero,new Vector3(s*.8f,.25f,.5f),.025f,Wax);
                Beam(hinge,new Vector3(s*.24f,.1f,.2f),new Vector3(s*.67f,.25f,-.2f),.014f,Wax); Batch(hinge);
            }
            if(ruta)
            {
                var cape=Shape("Leaf cloak",PrimitiveType.Sphere,new Vector3(0,1.1f,.53f),new Vector3(.9f,1.1f,.13f),"#477E61",body); cape.localRotation=Quaternion.Euler(-18,0,0);
                Beam(body,new Vector3(-.28f,1.44f,-.3f),new Vector3(.24f,.95f,-.34f),.035f,"#BC9B60");
                Shape("Coat clasp",PrimitiveType.Sphere,new Vector3(0,1.45f,-.34f),new Vector3(.17f,.17f,.07f),Amber,body);
                Shape("Amber compass",PrimitiveType.Cylinder,new Vector3(.65f,1.02f,-.27f),new Vector3(.34f,.1f,.34f),Amber,body);
                Beam(body,new Vector3(.65f,1.09f,-.27f),new Vector3(.73f,1.09f,-.32f),.035f,Dark);
            }
            if(armed)
            {
                Beam(body,new Vector3(-.64f,.15f,-.2f),new Vector3(-.64f,2.1f,-.2f),.05f,Bark);
                Roof(body,new Vector3(-.64f,2.25f,-.2f),new Vector3(.16f,.38f,.16f),Amber,6);
                if(!ruta) Shield(body,new Vector3(.66f,.95f,-.35f),.55f,Green);
            }
            Batch(body); return root;
        }
        private void Beetle(Transform root)
        {
            Shape("Weevil shell",PrimitiveType.Sphere,new Vector3(0,1.05f,.2f),new Vector3(1.9f,1.4f,2.2f),"#5C6870",root);
            Shape("Shell seam",PrimitiveType.Sphere,new Vector3(0,1.55f,.2f),new Vector3(.12f,.3f,1.9f),Blight,root);
            Shape("Head",PrimitiveType.Sphere,new Vector3(0,.9f,-1),new Vector3(1.2f,.9f,1),Bark,root);
            Beam(root,new Vector3(0,.9f,-1.3f),new Vector3(0,.65f,-2),.16f,Dark); Legs(root,3,.85f,1.35f,Bark);
            for(var s=-1;s<=1;s+=2) Shape("Pollen eye",PrimitiveType.Sphere,new Vector3(s*.38f,1.1f,-1.38f),Vector3.one*.22f,Amber,root);
            for(var i=0;i<3;i++) Shape("Pollen crust",PrimitiveType.Sphere,new Vector3(-.5f+i*.5f,1.55f,.65f),new Vector3(.45f,.5f,.4f),Blight,root);
        }
        private void Hornet(Transform root)
        {
            Shape("Hornet abdomen",PrimitiveType.Sphere,new Vector3(0,1.3f,.4f),new Vector3(1.25f,1,2),Dark,root);
            Shape("Hornet thorax",PrimitiveType.Sphere,new Vector3(0,1.5f,-.55f),new Vector3(.9f,1,1),"#A88A56",root);
            Shape("Hornet head",PrimitiveType.Sphere,new Vector3(0,1.6f,-1.2f),new Vector3(1.1f,.8f,.8f),Amber,root); Legs(root,3,1,1.55f,Dark);
            for(var s=-1;s<=1;s+=2) { Leaf(root,new Vector3(s*1,1.8f,.3f),new Vector3(.7f,.12f,2),"#9D86AE",s*45); Shape("Blight eye",PrimitiveType.Sphere,new Vector3(s*.36f,1.7f,-1.5f),Vector3.one*.26f,Blight,root); }
            Roof(root,new Vector3(0,1.3f,1.6f),new Vector3(.3f,.8f,.3f),Blight,4);
        }
        private void Spider(Transform root)
        {
            Shape("Spider abdomen",PrimitiveType.Sphere,new Vector3(0,1.4f,.5f),new Vector3(2.6f,1.7f,2.7f),"#45444E",root);
            Shape("Spider head",PrimitiveType.Sphere,new Vector3(0,1,-1),new Vector3(1.7f,1.1f,1.3f),"#65556E",root); Legs(root,4,1.5f,2.6f,"#45444E");
            for(var i=0;i<4;i++) Shape("Spider eyes",PrimitiveType.Sphere,new Vector3(-.55f+i*.36f,1.2f,-1.55f),Vector3.one*.22f,Amber,root);
            for(var s=-1;s<=1;s+=2) Beam(root,new Vector3(s*.3f,.85f,-1.5f),new Vector3(s*.4f,.35f,-1.7f),.13f,Wax);
        }
        private void Legs(Transform root,int count,float bodyWidth,float reach,string color)
        {
            for(var s=-1;s<=1;s+=2) for(var i=0;i<count;i++) { var z=-.7f+i*.6f; var knee=new Vector3(s*reach,1.1f,z+(i-1)*.25f); Beam(root,new Vector3(s*bodyWidth*.5f,.9f,z),knee,.12f,color); Beam(root,knee,new Vector3(s*(reach+.2f),.12f,z+(i-1)*.5f),.11f,color); }
        }
    }
}
