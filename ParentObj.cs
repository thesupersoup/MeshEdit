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
        private MeshFilter _filter = null;
		[SerializeField]
        private GameObject[] _vertObjs = null;
		[SerializeField]
        private VertexObj[] _verts = null;  

        public int Index
        {
            get { return _index; }
            set { _index = value; }
        }

        public void Init(int nIndex, MeshFilter nFilter)
        {
            _index = nIndex;
            _filter = nFilter;
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
            if (_filter != null && _verts != null)
            {
                Vector3[] _vertVects = _filter.sharedMesh.vertices;

                for (int i = 0; i < _verts.Length; i++)
                {
                    if (_verts[i].Dirty)
                    {
                        _vertVects[i] = _verts[i].transform.position - _verts[i].Offset;
                        _verts[i].Reset();
                    }
                }

                _filter.sharedMesh.vertices = _vertVects;   // Update mesh
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
