using UnityEngine;

namespace ArcSquad
{
    public sealed class ArcEffects : MonoBehaviour
    {
        float dyingAt;
        LineRenderer line;
        public static void DrawShot(Vector3 a,Vector3 b,bool missile)
        {
            var obj=new GameObject(missile?"ROCKET TRAIL":"REPULSOR TRACER");
            var fx=obj.AddComponent<ArcEffects>();
            fx.dyingAt=Time.time+(missile?.25f:.11f);
            fx.line=obj.AddComponent<LineRenderer>();
            fx.line.positionCount=2;
            fx.line.SetPosition(0,a);
            fx.line.SetPosition(1,b);
            fx.line.startWidth=missile?.55f:.24f;
            fx.line.endWidth=.04f;
            fx.line.numCapVertices=4;
            var mat=new Material(Shader.Find("Sprites/Default"));
            mat.color=missile?new Color(1f,.65f,.2f):new Color(.2f,1f,1f);
            fx.line.material=mat;
            fx.line.startColor=mat.color;fx.line.endColor=mat.color;
        }
        void Update()
        {
            if (line) line.widthMultiplier=Mathf.Max(0,(dyingAt-Time.time)*5f);
            if (Time.time>dyingAt) Destroy(gameObject);
        }
    }
}
