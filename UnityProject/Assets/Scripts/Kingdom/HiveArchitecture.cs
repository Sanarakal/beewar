using UnityEngine;
namespace PlagueGame.Kingdom
{
    public sealed partial class KingdomWorldView
    {
        private void BuildVillage()
        {
            var terrain=Root("Root terrace scenery",village,Vector3.zero);
            Shape("Bark terrace",PrimitiveType.Cylinder,new Vector3(0,-.6f,0),new Vector3(28,1.1f,24),Bark,terrain);
            Shape("Moss surface",PrimitiveType.Cylinder,new Vector3(0,-.1f,0),new Vector3(27.4f,.35f,23.4f),"#71905A",terrain);
            for(var i=0;i<7;i++) { var a=i*.8f; var x=Mathf.Cos(a)*11; var z=Mathf.Sin(a)*9.5f; Beam(terrain,new Vector3(x,-.5f,z),new Vector3(x*1.45f,.4f,z*1.5f),.6f,"#77553A"); }
            Path(new Vector3(0,0,-10),new Vector3(0,0,7),1.5f,terrain); Path(new Vector3(-7,0,-.8f),new Vector3(7,0,-.8f),1.2f,terrain);
            for(var i=0;i<17;i++) Shape("Wax battlement",PrimitiveType.Cylinder,new Vector3(-9.3f+i*1.15f,.6f,8.8f),new Vector3(.85f,1.15f,.8f),Wax,terrain);
            for(var s=-1;s<=1;s+=2)
            {
                Beam(terrain,new Vector3(s*9.3f,.4f,-7.7f),new Vector3(s*9.3f,.4f,8.8f),.4f,Wax);
                Flower(terrain,new Vector3(s*10.4f,0,5.5f),5.2f,s==1?"#D9B7D4":"#D7BA64"); Flower(terrain,new Vector3(s*11,0,-4.5f),3.8f,"#EAD9A4");
                for(var i=0;i<4;i++) Fern(terrain,new Vector3(s*(9.5f+i%2),0,-7+i*3.7f),1.4f);
                Banner(terrain,new Vector3(s*1.9f,0,-8.7f),Green); Shape("Gate pier",PrimitiveType.Cylinder,new Vector3(s*2,.65f,-8.7f),new Vector3(.85f,1.3f,.85f),Wax,terrain); Lantern(terrain,new Vector3(s*1.9f,1.6f,-8.7f));
            }
            Batch(terrain);
            Path(new Vector3(5.1f,0,1.5f),sites[(int)BuildingKind.Academy],.9f,village);
            for(var i=0;i<buildings.Length;i++)
            {
                var root=Root(KingdomSimulation.BuildingName((BuildingKind)i),village,sites[i]); buildings[i]=root;
                var hit=root.gameObject.AddComponent<BoxCollider>(); hit.center=Vector3.up*1.6f; hit.size=new Vector3(i==0?5.6f:3.8f,3.5f,3.6f); root.gameObject.AddComponent<KingdomTarget>().index=i;
                var model=Root("Base architecture",root,Vector3.zero); Shape("Hexagon foundation",PrimitiveType.Cylinder,Vector3.up*.15f,new Vector3(i==0?6.7f:4.8f,.3f,i==0?5.9f:4.4f),"#C1AD76",model);
                if(i==0) Castle(model); else if(i==1) Garden(model); else if(i==2) Workshop(model); else if(i==3) Warehouse(model); else if(i==4) Barracks(model); else if(i==5) Camp(model); else Academy(model); Batch(model);
                for(var j=0;j<2;j++)
                {
                    var addition=Root("Growth tier "+(j+2),root,Vector3.zero); growth[i,j]=addition;
                    if(i==0) { var y=j==0?3.9f:5.1f; Shape("Crown tower",PrimitiveType.Cylinder,new Vector3(0,y,.5f),new Vector3(j==0?1.8f:1,1.3f,j==0?1.8f:1),j==0?Wax:Amber,addition); Roof(addition,new Vector3(0,y+1,.5f),new Vector3(1.6f-j*.6f,1.15f,1.6f-j*.6f),j==0?Green:Amber,6); }
                    else Banner(addition,new Vector3(j==0?-1.6f:1.6f,.1f,.7f),j==0?Green:Amber);
                    Batch(addition);
                }
            }
            for(var i=0;i<4;i++) citizens.Add(Bee(village,Vector3.zero,i==0?.95f:.65f,i!=3,i==0));
        }
        private void BuildMap()
        {
            var terrain=Root("Garden geography",wilderness,Vector3.zero);
            Shape("Garden soil",PrimitiveType.Cylinder,new Vector3(0,-.45f,1),new Vector3(43,.8f,38),"#526B4D",terrain);
            Shape("Meadow",PrimitiveType.Cylinder,new Vector3(-5,-.04f,-3),new Vector3(24,.2f,20),"#76905B",terrain);
            var mapHome=Root("Your hive",terrain,home); mapHome.localScale=Vector3.one*.7f; Castle(mapHome);
            for(var i=0;i<3;i++)
            {
                Path(home,targets[i],.65f,terrain); Shape("Danger clearing",PrimitiveType.Cylinder,targets[i],new Vector3(5.8f,.12f,5.4f),i==0?"#AAAD72":"#665A71",terrain);
                var target=Root("Selectable enemy "+i,wilderness,targets[i]); var hit=target.gameObject.AddComponent<BoxCollider>(); hit.center=Vector3.up*1.2f; hit.size=new Vector3(4,3.4f,4); var marker=target.gameObject.AddComponent<KingdomTarget>(); marker.monster=true; marker.index=i;
                monsters[i]=Root("Wild creature "+i,wilderness,targets[i]); if(i==0) Beetle(monsters[i]); else if(i==1) Hornet(monsters[i]); else Spider(monsters[i]); Batch(monsters[i]);
            }
            Flower(terrain,new Vector3(-13,0,6),6,"#E9D19B"); Flower(terrain,new Vector3(12,0,-5),5,"#CEABBE"); Flower(terrain,new Vector3(-1,0,12),7,"#B2A0C4");
            for(var i=0;i<11;i++) Fern(terrain,new Vector3(-14+i*2.8f,0,i%2==0?-12:14),1.6f+i%3*.4f);
            for(var i=0;i<7;i++) Shape("Black pollen crystal",PrimitiveType.Sphere,new Vector3(1+i*1.4f,.3f,9+i%2*2.3f),new Vector3(.6f,.9f,.6f),Blight,terrain);
            Beam(terrain,new Vector3(-14,-.1f,11),new Vector3(13,1.1f,15),1.4f,Bark);
            var ruin=Root("Sealed root gate",terrain,new Vector3(8,0,12));
            for(var s=-1;s<=1;s+=2) Shape("Ancient wax pillar",PrimitiveType.Cylinder,new Vector3(s*2,1.7f,0),new Vector3(.8f,3.4f,.8f),"#BFAE92",ruin);
            Beam(ruin,new Vector3(-2,3.4f,0),new Vector3(2,3.4f,0),.55f,Amber); Batch(terrain);
        }
        private void BuildHeroStage()
        {
            var stage=Root("Hero garden",heroStage,Vector3.zero);
            Shape("Bark dais",PrimitiveType.Cylinder,new Vector3(0,-.2f,0),new Vector3(6.4f,.6f,5.1f),Bark,stage); Shape("Wax dais",PrimitiveType.Cylinder,new Vector3(0,.13f,0),new Vector3(5.7f,.17f,4.6f),Wax,stage); Ring(stage,new Vector3(0,.25f,0),2.2f,Amber);
            Flower(stage,new Vector3(-2.7f,-.2f,1.6f),3.4f,"#E6C675"); Fern(stage,new Vector3(2.7f,0,1.5f),2.2f); Banner(stage,new Vector3(2.1f,0,.7f),Green); Batch(stage);
            hero=Bee(heroStage,new Vector3(0,.4f,0),2.15f,true,true);
        }
        private void Castle(Transform root)
        {
            Shape("Wax keep",PrimitiveType.Cylinder,new Vector3(0,1.8f,.45f),new Vector3(3.7f,3.3f,3.3f),Wax,root); Shape("Honey cornice",PrimitiveType.Cylinder,new Vector3(0,3.25f,.45f),new Vector3(3.95f,.25f,3.55f),Amber,root); Roof(root,new Vector3(0,3.85f,.45f),new Vector3(2.5f,1.25f,2.35f),Green,6);
            for(var s=-1;s<=1;s+=2) { Shape("Gate tower",PrimitiveType.Cylinder,new Vector3(s*2,1.2f,-.8f),new Vector3(1.45f,2.25f,1.4f),Wax,root); Shape("Tower trim",PrimitiveType.Cylinder,new Vector3(s*2,2.35f,-.8f),new Vector3(1.6f,.2f,1.5f),Amber,root); Roof(root,new Vector3(s*2,2.9f,-.8f),new Vector3(1.15f,1.2f,1.1f),Green,6); Window(root,new Vector3(s*2,1.4f,-1.52f),.5f); }
            Box("Gate wall",new Vector3(0,.8f,-1.3f),new Vector3(3,1.4f,.4f),Wax,root); Box("Bark door",new Vector3(0,.85f,-1.55f),new Vector3(1.1f,1.6f,.12f),Bark,root); Window(root,new Vector3(0,2.6f,-1.22f),1.05f); Banner(root,new Vector3(-.8f,3.5f,.4f),Amber);
            for(var i=0;i<4;i++) Shape("Gate honeycomb",PrimitiveType.Cylinder,new Vector3(-1.35f+i*.9f,1.65f,-1.3f),new Vector3(.45f,.45f,.45f),Amber,root);
        }
        private void WaxHouse(Transform root,string roof)
        {
            Shape("Wax walls",PrimitiveType.Cylinder,new Vector3(0,1.05f,.1f),new Vector3(3.2f,1.8f,2.9f),Wax,root); Shape("Trim",PrimitiveType.Cylinder,new Vector3(0,1.9f,.1f),new Vector3(3.4f,.18f,3.05f),Amber,root); Roof(root,new Vector3(0,2.45f,.1f),new Vector3(2.3f,1.1f,2.1f),roof,6); Box("Door",new Vector3(.3f,.72f,-1.37f),new Vector3(.7f,1.2f,.12f),Bark,root); Window(root,new Vector3(-.65f,1.2f,-1.35f),.65f);
        }
        private void Garden(Transform root)
        {
            Shape("Nectar basin",PrimitiveType.Cylinder,new Vector3(0,.4f,0),new Vector3(3.4f,.5f,2.9f),Bark,root); Shape("Nectar",PrimitiveType.Cylinder,new Vector3(0,.66f,0),new Vector3(3.05f,.08f,2.6f),Amber,root);
            for(var i=0;i<3;i++) Flower(root,new Vector3(-1.1f+i*1.1f,.2f,.7f),1.65f+i%2*.6f,i==1?"#D6A5AE":"#F1DB9D");
            for(var i=0;i<3;i++) Shape("Nectar jar",PrimitiveType.Sphere,new Vector3(-.8f+i*.8f,.6f,-1.3f),new Vector3(.6f,.85f,.6f),Wax,root); Lantern(root,new Vector3(1.6f,.6f,-1));
        }
        private void Workshop(Transform root)
        {
            WaxHouse(root,"#A06F43"); Shape("Bark chimney",PrimitiveType.Cylinder,new Vector3(.9f,2.4f,.6f),new Vector3(.65f,1.8f,.65f),Bark,root); Box("Workbench",new Vector3(0,.65f,-1.7f),new Vector3(2.6f,.18f,.6f),Bark,root);
            for(var i=0;i<4;i++) Shape("Wax ingot",PrimitiveType.Cylinder,new Vector3(-1+i*.65f,.85f,-1.7f),new Vector3(.5f,.3f,.45f),i%2==0?Wax:Amber,root); Lantern(root,new Vector3(-1.6f,.7f,.4f));
        }
        private void Warehouse(Transform root)
        {
            WaxHouse(root,Green);
            for(var i=0;i<4;i++) { var p=new Vector3(-1.1f+i*.72f,.5f,-1.7f); Shape("Honey storage cell",PrimitiveType.Cylinder,p,new Vector3(.58f,.8f,.58f),"#CFA15F",root); Shape("Sealed lid",PrimitiveType.Cylinder,p+Vector3.up*.4f,new Vector3(.62f,.1f,.62f),Wax,root); }
        }
        private void Barracks(Transform root)
        {
            WaxHouse(root,"#46685B"); Banner(root,new Vector3(1.5f,0,-1.4f),"#B9474C");
            for(var i=0;i<3;i++) { var p=new Vector3(-1.1f+i*.8f,.8f,-1.8f); Beam(root,p-Vector3.up*.6f,p+Vector3.up*.6f,.065f,Bark); Shield(root,p,.6f,Amber); }
        }
        private void Camp(Transform root)
        {
            for(var s=-1;s<=1;s+=2) Beam(root,new Vector3(s*1.2f,.2f,.7f),new Vector3(s*1.2f,2.3f,.7f),.12f,Bark);
            Leaf(root,new Vector3(0,2.1f,.3f),new Vector3(2,.4f,2.4f),Green,0); Box("Training platform",new Vector3(0,.4f,0),new Vector3(2.7f,.18f,2.2f),Bark,root);
            for(var i=0;i<2;i++) { Beam(root,new Vector3(-.7f+i*1.4f,.2f,-1.4f),new Vector3(-.7f+i*1.4f,1.8f,-1.4f),.06f,Bark); Shape("Training target",PrimitiveType.Sphere,new Vector3(-.7f+i*1.4f,1.6f,-1.4f),new Vector3(.6f,.6f,.25f),Amber,root); }
        }
        private void Academy(Transform root)
        {
            WaxHouse(root,"#487B78");
            Shape("Observatory tower",PrimitiveType.Cylinder,new Vector3(.7f,2.7f,.7f),new Vector3(1.4f,2.2f,1.4f),Wax,root);
            Roof(root,new Vector3(.7f,4,.7f),new Vector3(1.3f,1.1f,1.3f),"#487B78",6);
            Shape("Amber star",PrimitiveType.Sphere,new Vector3(.7f,4.7f,.7f),new Vector3(.48f,.7f,.48f),Amber,root);
            Window(root,new Vector3(.7f,3.1f,-.03f),.72f);
            Box("Reading desk",new Vector3(-.45f,.65f,-1.65f),new Vector3(1.7f,.2f,.8f),Bark,root);
            var book=Box("Open wax manuscript",new Vector3(-.45f,.83f,-1.65f),new Vector3(1.15f,.08f,.7f),Wax,root);
            book.localRotation=Quaternion.Euler(12,0,0);
            Beam(root,new Vector3(-.45f,.89f,-1.97f),new Vector3(-.45f,1.02f,-1.34f),.025f,Amber);
            for(var i=0;i<3;i++) Box("Wax tablet",new Vector3(-1.7f,.42f+i*.15f,.3f),new Vector3(.6f,.12f,.85f),i==1?Amber:Wax,root);
            Lantern(root,new Vector3(1.65f,1,-1.35f)); Banner(root,new Vector3(-1.5f,0,-.8f),"#487B78");
        }
    }
}
