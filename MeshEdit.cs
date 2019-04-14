using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace MeshEdit
{
    /// <summary>
    /// Dissolve Unity Meshes and perform per-vertex edits in the Editor 
    /// </summary>
    [ExecuteAlways]
    public class MeshEdit : EditorWindow
    {
        const float VERSION = 0.1f;                     // MeshEdit Version
        const string KEY_VERSION = "version",           // EditorPref Key strings
                     KEY_EDIT_OPEN = "editOnOpen",
                     KEY_MOD_COL = "modCol",
                     KEY_SHOW_MESH = "showMesh",
                     KEY_SAVE_CLOSE = "saveOnClose";
        const float TITLE_HEIGHT = 32.0f,   // Px?
                    MAIN_HEIGHT = 128.0f;

        private static MeshEdit _i = null;

        public static MeshEdit Instance
        {
            get { return _i; }
        }

        private static bool _dev = false;           // Dev mode
        private static bool _editing = false;       // Editing in progress?
        private static bool _editOnOpen;            // Edit selection on open
        private static bool _modCol;                // Modify collision
        private static bool _showMesh;              // Show mesh during editing
        private static bool _saveOnClose;           // Save if the window is closed (true), or discard changes
        private static SceneView _view = null;      // For hooking into and decoupling from the SceneView.onSceneGUIDelegate
        private static GameObject _vertPrefab;      // Prefab for the vertex object during editing
        private GameObject[] _selected = null,
                             _prevSelected = null,
                             _stored = null,        // Will store objects in this array during editing
                             _parentObjs = null;    // Parent GameObjects for vertex objects
        private GameObject[][] _vertObjs = null;    // Stores GameObjects which represent vertices during editing

        // Mesh info
        private Mesh[] _meshes = null,          // Selected meshes for editing
                       _parentObjMeshes = null; // Meshes of Parent GameObjects for vertex objects during editing
        private Vector3[][] _vertVects = null;  // Vertices for meshes being edited stored as Vector3s
        private int[][] _tris = null;           // Tris for meshes being edited stored as groups of ints

        private int _selectedNum = 0,
                    _prevNum = 0;
        private string _names = "";

        private static GUIStyle _styleTitle = new GUIStyle(),
                                _styleInfo = new GUIStyle(),
                                _styleButton = new GUIStyle(),
                                _styleVersion = new GUIStyle();


        // MeshEdit properties
        public GameObject[] Selected
        {
            get { return _selected; }
        }

        public static bool Editing
        {
            get { return _editing; }
        }

        public static bool ModifyCollision
        {
            get { return _modCol; }
        }

        public static bool ShowMesh
        {
            get { return _showMesh; }
        }

        public static bool SaveOnClose
        {
            get { return _saveOnClose; }
        }

        // MeshEdit methods
        [MenuItem("Window/MeshEdit %#e")]
        static void Init()
        {
            if (_i == null)
            {
                UpdateGUIStyles();
                _i = new MeshEdit();                            // Assign the static instance
                _i.titleContent = new GUIContent("MeshEdit");   // Set title, else we get the namespace too
                TryGetEditorPrefs();                            // Get MeshEdit preferences; set defaults if not found
                _i.ShowUtility();                               // Make it an undockable window
                FindVertPrefab();

                if(_editOnOpen)
                {
                    _i.CheckSelected();
                    _i.DissolveObjects();
                }
            }
            else
            {
                _i.Close();
            }
        }

        /*
         * Look for Vertex and Parent Prefabs in expected location
         */
        static void FindVertPrefab()
        {
            _vertPrefab = Resources.Load("Prefabs/VertexPrefab") as GameObject;
        }

        /*
         * Check for EditorPrefs and set to defaults if not found
         */
        static void TryGetEditorPrefs()
        {
            if (EditorPrefs.HasKey(KEY_VERSION))
            {
                float tempVersion = EditorPrefs.GetFloat(KEY_VERSION);
                if (tempVersion <= VERSION)
                {
                    _editOnOpen = EditorPrefs.GetBool(KEY_EDIT_OPEN, true);     // Default true
                    _modCol = EditorPrefs.GetBool(KEY_MOD_COL, true);           // Default true
                    _showMesh = EditorPrefs.GetBool(KEY_SHOW_MESH, true);       // Default true
                    _saveOnClose = EditorPrefs.GetBool(KEY_SAVE_CLOSE, false);  // Default false
                }
                else
                {
                    if (_i != null)
                    {
                        _i.ShowNotification(new GUIContent("MeshEdit preferences are from a newer version, resetting..."));
                    }

                    SetDefaultPrefs();
                }
            }
        }

        /*
         * Save EditorPrefs 
         */
        static void SaveEditorPrefs()
        {
            EditorPrefs.SetFloat(KEY_VERSION, VERSION);
            EditorPrefs.SetBool(KEY_EDIT_OPEN, _editOnOpen);
            EditorPrefs.SetBool(KEY_MOD_COL, _modCol);
            EditorPrefs.SetBool(KEY_SHOW_MESH, _showMesh);
            EditorPrefs.SetBool(KEY_SAVE_CLOSE, _saveOnClose);
        }

        /*
         * Set default EditorPref values
         */ 
        static void SetDefaultPrefs()
        {
            _editOnOpen = true;     // Default true
            _modCol = true;         // Default true
            _showMesh = true;       // Default true
            _saveOnClose = false;   // Default false
        }

        /*
         * Update custom GUIStyles
         */
        static void UpdateGUIStyles()
        {
            _styleTitle.padding = new RectOffset(8, 8, 4, 4);
            _styleTitle.normal.textColor = Color.white;
            _styleTitle.fontSize = 28;
            _styleTitle.fontStyle = FontStyle.Bold;

            _styleInfo.padding = new RectOffset(8, 8, 4, 4);
            _styleInfo.normal.textColor = Color.gray;
            _styleInfo.alignment = TextAnchor.MiddleLeft;

            _styleVersion.normal.textColor = Color.gray;
        }

        void OnSelectionChange()
        {
            if (CheckSelected())
                Repaint();
        }

        void OnGUI()
        {
            UpdateGUIStyles();
            DisplayTitle();

            // Main section vertical layout
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true),
                                            GUILayout.Width(256.0f));
            // Main content
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Selected GameObject(s):", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(_names);
            EditorGUILayout.Space();
            if (!_editing)
            {
                if (GUILayout.Button("Change Mesh(es) to Verts", GUILayout.Width(176.0f), GUILayout.Height(32.0f)))
                {
                    DissolveObjects();
                }
            }
            else
            {
                if (GUILayout.Button("Save Changes", GUILayout.Width(176.0f), GUILayout.Height(32.0f)))
                {
                    RestoreObjects();
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Vertex Prefab:", EditorStyles.boldLabel);
            _vertPrefab = (GameObject)EditorGUILayout.ObjectField(_vertPrefab, typeof(GameObject), false, GUILayout.MaxWidth(256.0f));
            EditorGUILayout.EndVertical();
            EditorGUI.BeginDisabledGroup(_editing);    // Prevent changes while editing
            // Main options
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(256.0f));
            EditorGUILayout.LabelField("Edit on open:", EditorStyles.boldLabel);
            _editOnOpen = EditorGUILayout.Toggle(_editOnOpen);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(256.0f));
            EditorGUILayout.LabelField("Modify collision mesh:", EditorStyles.boldLabel);
            _modCol = EditorGUILayout.Toggle(_modCol);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(256.0f));
            EditorGUILayout.LabelField("Show Mesh during editing:", EditorStyles.boldLabel);
            _showMesh = EditorGUILayout.Toggle(_showMesh);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(256.0f));
            EditorGUILayout.LabelField("Save on window close:", EditorStyles.boldLabel);
            _saveOnClose = EditorGUILayout.Toggle(_saveOnClose);
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
            // End Main section
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            // How to close
            EditorGUILayout.LabelField("Ctrl+Shift+E to close");
            EditorGUILayout.EndHorizontal();

            // Footer
            if (_dev)
            {
                EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(16.0f), GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Update GUIStyles"))
                {
                    UpdateGUIStyles();
                    Repaint();
                }
                if (GUILayout.Button("Reset MeshEdit"))
                {
                    Reset();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        /*
         * Display the extension title and author info 
         */
        void DisplayTitle()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true),
                                            GUILayout.MinHeight(TITLE_HEIGHT),
                                            GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("MeshEdit", _styleTitle);    // MeshEdit title
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true),
                                            GUILayout.MinHeight(TITLE_HEIGHT),
                                            GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("v" + VERSION + " \u00A9 2019 Mitch Gentry", _styleInfo);
            EditorGUILayout.EndVertical();
        }

        /*
         * Check the currently selected GameObject(s), and return true if the
         * Editor Window should be repainted (something changed), or false
         * when it doesn't need to be repainted to save overhead
         */
        private bool CheckSelected()
        {
            bool update = false;
            _selected = Selection.gameObjects;
            _prevNum = _selectedNum;

            if (!_editing)
            {
                if (_selected != null && (_selectedNum = _selected.Length) > 0) // Assign length to _selected Num as it's checked
                {
                    if (_selectedNum == 1)
                    {
                        _names = _selected[0].name;
                    }
                    else
                    {
                        _names = "";
                        for (int i = 0; i < _selectedNum; i++)
                        {
                            _names += _selected[i].name;
                            if (i < _selectedNum - 1)   // More names to add
                                _names += ", ";
                        }
                    }
                }
                else
                {
                    _names = "No GameObject(s) currently selected...";
                }
            }
            else
            {
                _names = "Editing...";

                if (_showMesh)
                {
                    for (int i = 0; i < _prevSelected.Length; i++)
                    {
                        _prevSelected[i].GetComponent<VertexObj>()?.Select(false);
                    }

                    for (int i = 0; i < _selected.Length; i++)
                    {
                        _selected[i].GetComponent<VertexObj>()?.Select(true);
                    }
                }
            }

            // Either there's a different number of objects selected now, or the
            // selected objects have changed
            if (_prevNum != _selectedNum || _prevSelected != _selected)
            {
                update = true;
                _prevSelected = _selected;
            }

            return update;
        }

        /*
         * Store selected GameObjects with Meshes, then "dissolve" them for 
         * vertex manipulation
         */
        void DissolveObjects()
        {
            if (_selected != null && _selected.Length > 0)  // We have some selected GameObjects    
            {
                ParentObj parentScript = null;  // If we end up editing an object, this will store the script
                List<GameObject> objsWithMesh = new List<GameObject>();
                List<Mesh> tempMeshes = new List<Mesh>();

                for (int i = 0; i < _selected.Length; i++)  // Check for Meshes
                {
                    MeshFilter objFilter = _selected[i].GetComponent<MeshFilter>();
                    Mesh objMesh = null;

                    if (objFilter != null)
                    {
                        objMesh = objFilter.mesh;
                        if (objMesh != null)
                        {
                            objsWithMesh.Add(_selected[i]);
                            tempMeshes.Add(objMesh);
                            _selected[i].SetActive(false);  // Disable to remove from view nondestructively
                        }
                    }
                }

                if (tempMeshes.Count > 0)    // We found some editable GameObjects
                {
                    _stored = objsWithMesh.ToArray();   // Store the GameObjects with meshes to be restored later

                    Selection.SetActiveObjectWithContext(null, null);   // Deselect any selected objects
                    _selected = null;       // Nothing should be selected now, null selected array

                    _meshes = tempMeshes.ToArray(); // Set array with gathered Meshes

                    // Set up vert array, tri array, and vert Obj array
                    int arrLen = _meshes.Length;
                    _vertVects = new Vector3[arrLen][];
                    _tris = new int[arrLen][];
                    _parentObjs = new GameObject[arrLen];
                    _vertObjs = new GameObject[arrLen][];

                    if (_showMesh)
                        _parentObjMeshes = new Mesh[arrLen];
                    else
                        _parentObjMeshes = null;

                    for (int i = 0; i < arrLen; i++)
                    {
                        if (_modCol)
                        {
                            // We're going to be changing the collision mesh, clean up old Colliders
                            Collider[] tempCol = _stored[i].GetComponents<Collider>();

                            if (tempCol != null && tempCol.Length > 0)
                            {
                                for (int c = 0; c < tempCol.Length; c++)
                                {
                                    DestroyImmediate(tempCol[c]);
                                }
                            }
                        }

                        _vertVects[i] = _meshes[i].vertices;
                        _tris[i] = _meshes[i].triangles;
                        _parentObjs[i] = new GameObject(_stored[i].name + ":Editing...");
                        _parentObjs[i].transform.position = _stored[i].transform.position;

                        parentScript = _parentObjs[i].AddComponent<ParentObj>();    // Assign this ParentObj to the script var

                        if (_showMesh)
                        {
                            // Set details of edit mesh
                            MeshFilter editFilter = _parentObjs[i].AddComponent<MeshFilter>();
                            MeshRenderer editRender = _parentObjs[i].AddComponent<MeshRenderer>();

                            editFilter.mesh = _stored[i].GetComponent<MeshFilter>()?.mesh;  // Null-check, so we can be safe! - Boots the Monkey
                            editRender.materials = _stored[i].GetComponent<MeshRenderer>()?.materials;

                            parentScript?.Init(i, editFilter.mesh);

                            _parentObjMeshes[i] = editFilter.mesh;
                        }
                    }

                    _editing = true;    // Now entering Edit mode

                    for (int i = 0; i < _vertVects.Length; i++)
                    {
                        _vertObjs[i] = new GameObject[_vertVects[i].Length];

                        for (int v = 0; v < _vertObjs[i].Length; v++)
                        {
                            VertexObj vertScript = null;
                            GameObject tempObj;

                            if (_vertPrefab != null)
                            {
                                tempObj = Instantiate(_vertPrefab);
                                vertScript = tempObj.GetComponent<VertexObj>();
                                tempObj.name = "Vert[" + i + "][" + v + "]";

                                if(vertScript == null)
                                {
                                    vertScript = tempObj.AddComponent<VertexObj>();
                                }
                            }
                            else
                            {
                                tempObj = new GameObject("Vert[" + i + "][" + v + "]");
                                vertScript = tempObj.AddComponent<VertexObj>();
                                SpriteRenderer sprRender = tempObj.AddComponent<SpriteRenderer>();
                                Sprite spr = Resources.Load("Sprites/HotspotDia") as Sprite;
                                sprRender.sprite = spr;
                            }

                            tempObj.transform.parent = _parentObjs[i].transform;
                            tempObj.transform.position = _stored[i].transform.position + _vertVects[i][v];  // Offset based on obj world position
                            _vertObjs[i][v] = tempObj;

                            vertScript?.Init(_stored[i].transform.position, i);
                        }

                        parentScript?.AssignVerts(_vertObjs[i]);
                    }
                }
                else
                {
                    MeshLog("No meshes detected...");
                }
            }
            else
            {
                MeshLog("No object(s) selected...");
            }

            Repaint();
        }

        void RestoreObjects()
        {
            if (_stored != null && _stored.Length > 0)
            {
                for (int i = 0; i < _stored.Length; i++)
                {
                    MeshFilter objFilter = _stored[i].GetComponent<MeshFilter>();
                    Mesh objMesh = null;

                    if (objFilter != null)  // It better not be null...
                    {
                        objMesh = objFilter.mesh;
                        if (objMesh != null) // It better not be null...
                        {
                            Vector3[] verts = new Vector3[_vertObjs[i].Length];
                            for (int v = 0; v < verts.Length; v++)
                            {
                                // Assign vert obj positions as vert coords
                                verts[v] = _vertObjs[i][v].transform.position - _stored[i].transform.position;  // Undo offset
                                if (_vertObjs[i][v] != null)    // Safe destroy
                                    DestroyImmediate(_vertObjs[i][v]);   // Clean up vert obj
                            }

                            if (_parentObjs[i] != null)         // Safe destroy
                                DestroyImmediate(_parentObjs[i]);   // Clean up parent obj

                            if (_modCol)
                            {
                                _stored[i].AddComponent<MeshCollider>();
                            }

                            objMesh.vertices = verts;
                            _stored[i].SetActive(true); // Make visible again with new verts

                            objMesh.name = _stored[i].name;
                        }
                        else
                        {
                            MeshLog("There is no mesh on the stored object.\n" +
                                    "Look at what your careless hands have wrought.\n" +
                                    "Distant screams from deep below reach your ears.\n" +
                                    "The Elder Ones come to feast on the chaos you have unleashed...");
                        }
                    }
                    else
                    {
                        MeshLog("You shall become as dust. Dust such that the worm will pass you by.");
                    }
                }

                _editing = false;   // Exiting edit mode
            }
            else
            {
                MeshLog("No stored objects, can't restore...");
            }

            CheckSelected();
            Repaint();
        }

        void Update()
        {
            if (_vertPrefab == null)
                FindVertPrefab();
        }

        void Reset()
        {
            _editing = false;
            _selected = null;
            _prevSelected = null;

            if (_stored != null && _stored.Length > 0)
            {
                for (int i = 0; i < _stored.Length; i++)
                {
                    _stored[i].SetActive(true); // Reset visibility of GameObjects

                    if (_modCol) // Editing collision
                    {
                        _stored[i].AddComponent<MeshCollider>();
                    }
                }
            }
            _stored = null;

            if (_parentObjs != null && _parentObjs.Length > 0)
            {
                for (int i = 0; i < _parentObjs.Length; i++)
                {
                    if (_parentObjs[i] != null)
                        DestroyImmediate(_parentObjs[i]);
                }
            }
            _parentObjs = null;

            if (_vertObjs != null && _vertObjs.Length > 0)
            {
                for (int i = 0; i < _vertObjs.Length; i++)
                {
                    for (int v = 0; v < _vertObjs[i].Length; v++)
                    {
                        if (_vertObjs[i][v] != null)
                            DestroyImmediate(_vertObjs[i][v]);
                    }
                }
            }
            _vertObjs = null;

            _meshes = null;
            _vertVects = null;
            _tris = null;

            CheckSelected();
            FindVertPrefab();
            Repaint();
        }

        static void MeshLog(string nMsg)
        {
            Debug.Log("MeshEdit: " + nMsg);
        }

        void OnDestroy()
        {
            if (_editing)
            {
                if (_saveOnClose)
                {
                    RestoreObjects();
                }
                else
                {
                    Reset();
                }
            }

            SaveEditorPrefs();   // Save MeshEdit preferences
            _i = null;          // Free up the static instance
        }
    }
}
