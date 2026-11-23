using UnityEngine;
using UnityEngine.UI;

namespace SaintKnight.Day26
{
    /// <summary>Vertical colour gradient over a uGUI graphic (title logo: pale gold top, bronze bottom).</summary>
    public sealed class SKGradient : BaseMeshEffect
    {
        public Color Top = Color.white, Bottom = Color.gray;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            var v = new UIVertex();
            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                min = Mathf.Min(min, v.position.y);
                max = Mathf.Max(max, v.position.y);
            }
            float span = Mathf.Max(.001f, max - min);
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                v.color = Color.Lerp(Bottom, Top, (v.position.y - min) / span) * (Color)v.color;
                vh.SetUIVertex(v, i);
            }
        }
    }
}
