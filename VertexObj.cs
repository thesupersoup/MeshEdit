using UnityEngine;

namespace MeshEdit
{
    /// <summary>
    /// Represents a vertex during editing; can be set dirty to update mesh
    /// </summary>
    [ExecuteAlways]
    public class VertexObj : MonoBehaviour
    {
        [SerializeField]
        private Sprite _sprBase = null;
        [SerializeField]
        private Sprite _sprSel = null;
        [SerializeField]
        private SpriteRenderer _sprRender = null;
        private bool _init = false;
        private bool _dirty = false;
        private int _index = -1;
        private Vector3 _prevPos = Vector3.zero;
        private Vector3 _offset = Vector3.zero;   // Subtract from position to get actual pos

        public bool Dirty
        {
            get { return _dirty; }
        }

        public int Index
        {
            get { return _index; }
        }

        public Vector3 Offset
        {
            get { return _offset; }
        }

        public void OnEnable()
        {
            _prevPos = this.transform.position;
            if (_sprRender == null)
            {
                _sprRender = GetComponent<SpriteRenderer>();
            }
        }

        public void Select(bool isSelected)
        {
            if (_sprRender != null)
            {
                if (isSelected)
                {
                    _sprRender.sprite = _sprSel;
                }
                else
                {
                    _sprRender.sprite = _sprBase;
                }
            }
        } 

        public void Init(Vector3 nOffset, int nInd)
        {
            _init = true;
            _offset = nOffset;
            _index = nInd;
            _prevPos = this.transform.position - _offset;
        }

        public void Reset()
        {
            _dirty = false;
        }

        void Update()
        {
            if (_init)
            {
                if (this.transform.position - _offset != _prevPos) 
                {
                    _dirty = true;
                    _prevPos = this.transform.position - _offset;
                }
            }
        }
    }
}
