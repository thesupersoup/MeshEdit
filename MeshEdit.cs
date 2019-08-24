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
		const float VERSION = 0.2f;						// MeshEdit Version
		const string KEY_VERSION = "version",			// EditorPref Key strings
						KEY_EDIT_OPEN = "editOnOpen",
						KEY_MOD_COL = "modCol",
						KEY_SHOW_MESH = "showMesh",
						KEY_SAVE_CLOSE = "saveOnClose";
		const float TITLE_HEIGHT = 32.0f,				// Px
					VERT_SELECT_TOLERANCE = 0.05f;		// Radius of the spherecast 

		private static MeshEdit _i = null;

		public static MeshEdit Instance => _i;


		///--------------------------------------------------------------------------
		/// MeshEdit variables
		///--------------------------------------------------------------------------

		private static bool _dev = false;			// Dev mode
		private static bool _editing = false;		// Is editing in progress
		private static bool _editOnOpen;			// Edit selection on open
		private static bool _modCol;				// Modify collision
		private static bool _showMesh;				// Show mesh during editing
		private static bool _saveOnClose;					// Save if the window is closed (true), or discard changes
		private static GameObject _vertPrefab = null;		// Prefab for the vertex object during editing
		private GameObject[] _selected = null,
								_prevSelected = null,
								_stored = null,				// Will store objects in this array during editing
								_parentObjs = null;			// Parent GameObjects for vertex objects
		private GameObject[][] _vertObjs = null;			// Stores GameObjects which represent vertices during editing
		private ParentObj[] _parentScripts = null;

		// Mesh info
		private MeshFilter[] _filters = null,		// Selected MeshFilters for editing
						_parentObjFilters = null;	// MeshFilters of Parent GameObjects for vertex objects during editing
		private MeshRenderer[] _renderers = null;	// MeshRenderers for objects selected for editing
		private Vector3[][] _vertVects = null;		// Vertices for meshes being edited stored as Vector3s
		private int[][] _tris = null;				// Tris for meshes being edited stored as groups of ints

		private int _selectedNum = 0,
					_prevNum = 0;
		private string _names = "";

		private static GUIStyle _styleTitle = new GUIStyle(),
								_styleInfo = new GUIStyle(),
								_styleButton = new GUIStyle(),
								_styleVersion = new GUIStyle();


		///--------------------------------------------------------------------------
		/// MeshEdit properties
		///--------------------------------------------------------------------------

		public GameObject[] Selected => _selected;
		public static bool Editing => _editing;
		public static bool ModifyCollision => _modCol;
		public static bool ShowMesh => _showMesh;
		public static bool SaveOnClose => _saveOnClose;


		///--------------------------------------------------------------------------
		/// MeshEdit methods
		///--------------------------------------------------------------------------

		[MenuItem("Window/MeshEdit %#e")]
		static void Init()
		{
			if (_i == null)
			{
				UpdateGUIStyles();
				_i = CreateInstance<MeshEdit>();				// Assign the static instance
				_i.titleContent = new GUIContent("MeshEdit");	// Set title, else we get the namespace too
				TryGetEditorPrefs();							// Get MeshEdit preferences; set defaults if not found
				_i.ShowUtility();								// Make it an undockable window
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

		/// <summary>
		/// Look for Vertex and Parent Prefabs in expected location
		/// </summary>
		static void FindVertPrefab()
		{
			_vertPrefab = Resources.Load("Prefabs/VertexPrefab") as GameObject;
		}

		/// <summary>
		/// Check for EditorPrefs and set to defaults if not found
		/// </summary>
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

		static void SaveEditorPrefs()
		{
			EditorPrefs.SetFloat(KEY_VERSION, VERSION);
			EditorPrefs.SetBool(KEY_EDIT_OPEN, _editOnOpen);
			EditorPrefs.SetBool(KEY_MOD_COL, _modCol);
			EditorPrefs.SetBool(KEY_SHOW_MESH, _showMesh);
			EditorPrefs.SetBool(KEY_SAVE_CLOSE, _saveOnClose);
		}

		static void SetDefaultPrefs()
		{
			_editOnOpen = true;     // Default true
			_modCol = true;         // Default true
			_showMesh = true;       // Default true
			_saveOnClose = false;   // Default false
		}

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
			EditorGUI.BeginDisabledGroup(_editing);		// Prevent changes while editing
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

		/// <summary>
		/// Display the extension title and author info 
		/// </summary>
		void DisplayTitle ()
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

		/// <summary>
		/// Check the currently selected GameObject(s), and return true if the Editor Window 
		/// should be repainted (something changed), or false when it doesn't need to be 
		/// repainted to save overhead
		/// </summary>
		private bool CheckSelected()
		{
			bool update = false;
			_selected = Selection.gameObjects;
			_prevNum = _selectedNum;

			_selectedNum = _selected.Length;

			if (!_editing)
			{
				if ( _selected == null || _selectedNum <= 0 )
					_names = "No GameObject(s) currently selected...";
				else
				{
					_names = "";
					for ( int i = 0; i < _selectedNum; i++ )
					{
						_names += _selected[i].name;
						if ( i < _selectedNum - 1 )   // More names to add
							_names += ", ";
					}
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

		/// <summary>
		/// Store selected GameObjects with Meshes, then "dissolve" them for 
		/// vertex manipulation
		/// </summary>
		void DissolveObjects()
		{
			// No selected GameObjects
			if(_selected == null || _selected.Length == 0)
			{
				MeshLog( "No object(s) selected..." );
				return;
			}

			List<GameObject> objsWithMesh = new List<GameObject>();
			List<MeshFilter> tempFilters = new List<MeshFilter>();
			List<MeshRenderer> tempRenderers = new List<MeshRenderer>();

			for (int i = 0; i < _selected.Length; i++)  // Check for Meshes
			{
				MeshFilter objFilter = _selected[i].GetComponent<MeshFilter>();
				MeshRenderer objRenderer = _selected[i].GetComponent<MeshRenderer>();

				if (objFilter != null && objRenderer != null)
				{
					objsWithMesh.Add(_selected[i]);
					tempFilters.Add(objFilter);
					tempRenderers.Add( objRenderer );
					_selected[i].SetActive(false);  // Disable to remove from view nondestructively
				}
			}

			// No editable GameObjects found
			if(tempFilters.Count == 0)
			{
				MeshLog( "No meshes detected..." );
				return;
			}

			_stored = objsWithMesh.ToArray();		// Store the GameObjects with meshes to be restored later

			Selection.SetActiveObjectWithContext(null, null);	// Deselect any selected objects
			_selected = null;						// Nothing should be selected now, null selected array

			_filters = tempFilters.ToArray();		// Set array with gathered MeshFilters
			_renderers = tempRenderers.ToArray();	// Set array with gathered MeshRenderers

			// Number of meshes being edited
			int arrLen = _filters.Length;

			// Set up vert array, tri array, and vert Obj array
			_vertVects = new Vector3[arrLen][];
			_tris = new int[arrLen][];
			_vertObjs = new GameObject[arrLen][];

			// Set up parent arrays
			_parentObjs = new GameObject[arrLen];
			_parentScripts = new ParentObj[arrLen];

			if (_showMesh)
				_parentObjFilters = new MeshFilter[arrLen];
			else
				_parentObjFilters = null;

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

				Mesh objMesh = Instantiate( _filters[i].sharedMesh ) as Mesh;

				_vertVects[i] = objMesh.vertices;
				_tris[i] = objMesh.triangles;
				_parentObjs[i] = new GameObject(_stored[i].name + ":Editing...");
				_parentObjs[i].transform.position = _stored[i].transform.position;

				// Add ParentObj script to parent object
				_parentScripts[i] = _parentObjs[i].AddComponent<ParentObj>();

				if (_showMesh)
				{
					// Set details of edit mesh
					MeshFilter editFilter = _parentObjs[i].AddComponent<MeshFilter>();
					MeshRenderer editRender = _parentObjs[i].AddComponent<MeshRenderer>();

					editFilter.sharedMesh = objMesh;
					editRender.materials = _renderers[i].sharedMaterials;

					_parentObjFilters[i] = editFilter;
					_parentScripts[i].Init(i, editFilter);
				}
			}

			for ( int k = 0; k < _vertVects.Length; k++ )
			{
				_vertObjs[k] = new GameObject[_vertVects[k].Length];

				for ( int v = 0; v < _vertObjs[k].Length; v++ )
				{
					VertexObj vertScript = null;
					GameObject tempObj;

					if ( _vertPrefab != null )
					{
						tempObj = Instantiate( _vertPrefab );
						vertScript = tempObj.GetComponent<VertexObj>();
						tempObj.name = "Vert[" + k + "][" + v + "]";

						if ( vertScript == null )
						{
							vertScript = tempObj.AddComponent<VertexObj>();
						}
					}
					else
					{
						tempObj = new GameObject( "Vert[" + k + "][" + v + "]" );
						vertScript = tempObj.AddComponent<VertexObj>();
						SpriteRenderer sprRender = tempObj.AddComponent<SpriteRenderer>();
						Sprite spr = Resources.Load( "Sprites/HotspotDia" ) as Sprite;
						sprRender.sprite = spr;
					}

					tempObj.transform.parent = _parentObjs[k].transform;
					tempObj.transform.position = _stored[k].transform.position + _vertVects[k][v];  // Offset based on obj world position
					_vertObjs[k][v] = tempObj;

					vertScript.Init( _stored[k].transform.position, k );
				}

				_parentScripts[k].AssignVerts( _vertObjs[k] );
			}

			_editing = true;    // Now entering Edit mode
			Repaint();
		}

		void RestoreObjects()
		{
			if (_stored != null && _stored.Length > 0)
			{
				for (int i = 0; i < _stored.Length; i++)
				{
					MeshFilter objFilter = _stored[i].GetComponent<MeshFilter>();

					if (objFilter != null)  // It better not be null...
					{
						Mesh objMesh = Instantiate( objFilter.sharedMesh ) as Mesh;
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

							if (_parentObjs[i] != null)				// Safe to destroy
								DestroyImmediate(_parentObjs[i]);	// Clean up parent obj

							objMesh.vertices = verts;
							objMesh.name = _stored[i].name;
							objFilter.sharedMesh = objMesh;

							if ( _modCol )
							{
								MeshCollider col = _stored[i].AddComponent<MeshCollider>();
								col.sharedMesh = objMesh;
							}

							_stored[i].SetActive( true ); // Make visible again with new verts
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
			}
			else
			{
				MeshLog("No stored objects, can't restore...");
			}


			_editing = false;
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

			_filters = null;
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
					RestoreObjects();
				else
					Reset();
			}
			else
				Reset();

			SaveEditorPrefs();   // Save MeshEdit preferences
			_i = null;          // Free up the static instance
		}
	}
}
