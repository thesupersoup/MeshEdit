using UnityEngine;
using UnityEditor;

// Thanks to Neil Carter (NCarter) for the initial script!
// http://wiki.unity3d.com/index.php?title=CameraFacingBillboard

namespace MeshEdit
{
    /// <summary>
    /// Attach this to any GameObject with a sprite that should always face the active camera
    /// </summary>
    [ExecuteAlways]
    public class CameraFacingSprite : MonoBehaviour
    {
        [SerializeField]
        private bool _play = false;   // Are we in play mode (true) or edit mode (false)
        [SerializeField]
        private Camera faceCam = null;

        void Awake()
        {
            UpdateState();
            RotateToFace(_play);
        }

        void OnRenderObject()
        {
            RotateToFace(_play);
        }

        void UpdateState()
        {
            _play = Application.isPlaying;

            if (_play)
            {
                faceCam = Camera.main;  // In-game Main camera
            }
            else
            { 
                faceCam = SceneView.lastActiveSceneView.camera;     // Editor camera
            }
        }

        void RotateToFace(bool ingame)
        {
            if (faceCam != null)
            {
                transform.LookAt(transform.position + faceCam.transform.rotation * Vector3.forward, faceCam.transform.rotation * Vector3.up);
            }
            else
            {
                UpdateState();
            }
        }
    }
}