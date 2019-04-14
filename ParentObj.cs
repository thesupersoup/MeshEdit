using UnityEngine;

namespace MeshEdit
{
    /// <summary>
    /// Attached to the Parent GameObject, represents the original object during editing
    /// </summary>
    [ExecuteAlways]
    public class ParentObj : MonoBehaviour
    {
        private bool _showMesh = false;
        private int _index = -1;
        private Mesh _mesh = null;
        private GameObject[] _vertObjs = null;
        private VertexObj[] _verts = null;  

        public int Index
        {
            get { return _index; }
            set { _index = value; }
        }

        public void Init(int nIndex, Mesh nMesh)
        {
            _index = nIndex;
            _mesh = nMesh;
        }

        public void AssignVerts(GameObject[] nVerts)
        {
            _vertObjs = nVerts;
            _verts = new VertexObj[_vertObjs.Length];

            for(int i = 0; i < _verts.Length; i++)
            {
                _verts[i] = _vertObjs[i].GetComponent<VertexObj>();
            }
        }

        private void Refresh()
        {
            if (_mesh != null && _verts != null)
            {
                Vector3[] _vertVects = _mesh.vertices;

                for (int i = 0; i < _verts.Length; i++)
                {
                    if (_verts[i].Dirty)
                    {
                        _vertVects[i] = _verts[i].transform.position - _verts[i].Offset;
                        _verts[i].Reset();
                    }
                }

                _mesh.vertices = _vertVects;   // Update mesh
            }
        }

        void OnRenderObject()
        {
            if(MeshEdit.ShowMesh)
            {
                Refresh();
            }
        }
    }
}
