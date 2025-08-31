using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WorldPainter2D
{
    [ExecuteInEditMode]
    public class MeshColorPicker : MonoBehaviour
    {
        public Color color;

        // Update is called once per frame
        public void SetColor()
        {
            var mesh = GetComponent<MeshFilter>().sharedMesh;
            if(mesh != null)
                mesh.SetColors(Enumerable.Repeat(color, mesh.vertexCount).ToArray());
        }
    }
}