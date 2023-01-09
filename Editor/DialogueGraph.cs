using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

using DialogueEditor.Runtime;

namespace DialogueEditor.Editor {
    public class DialogueGraph : EditorWindow {

        private const string LAST_DIRECTORY_PREFS_KEY = "DialogueGraph_LastOpenDirectory";
    
        private DialogueGraphView graphView;

        private const string DEFAULT_FILE_NAME = "New Dialogue";
        string filename = DEFAULT_FILE_NAME;
        string currentFilePath = string.Empty;
        string currentDirectory = string.Empty;

        [MenuItem("Dialogue Editor", menuItem = "Window/Dialogue Editor")]
        public static void OpenDialogueEditor() {
            DialogueGraph window = EditorWindow.GetWindow<DialogueGraph>();
            window.titleContent = new GUIContent("Dialogue Editor");
        }

        /// <summary>
        /// Creates everything nessesary for drawing the graph view.
        /// </summary>
        private void OnEnable() {
            ConstructGraphView();
            GenerateToolbar();
            GenerateMiniMap();
            GenerateBlackboard();

            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        private void OnBeforeAssemblyReload() {
            //if (EditorUtility.DisplayDialog("Save changes?", "Assembly is going to reload, do you want to save your changes?", "Yes", "No")) {
            //    SaveData();
            //}
        }

        private void OnAfterAssemblyReload() {
            // LoadFile();
        }

        /// <summary>
        /// Removes the graph view from the window.
        /// </summary>
        private void OnDisable() {
            rootVisualElement.Remove(graphView);

            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
        }

        /// <summary>
        /// Creates the minimap and adds it to the graph view.
        /// </summary>
        private void GenerateMiniMap() {
            MiniMap miniMap = new MiniMap {
                anchored = true,
            };
            // NOTE: 10 pixels from the right side.
            Vector2 coordinates = graphView.contentViewContainer.WorldToLocal(new Vector2(x: this.maxSize.x - 10, y: 30));
            miniMap.SetPosition(new Rect(x: coordinates.x, y: coordinates.y, width: 200, height: 140));
            graphView.Add(miniMap);
        }

        /// <summary>
        /// Creates the blackboard and adds it to the minimap.
        /// </summary>
        private void GenerateBlackboard() {
            Blackboard blackboard = new Blackboard(graphView);
            blackboard.Add(new BlackboardSection { title = "Exposed Properties" });
            blackboard.addItemRequested = _blackboard => graphView.AddPropertyToBlackBoard(blackboard, new ExposedProperty());
            blackboard.editTextRequested = (_blackboard, visualElement, newPropertyName) => {
                string oldPropertyName = (visualElement as BlackboardField).text;
                if (graphView.exposedProperties.Any(x => x.propertyName == newPropertyName)) {
                    EditorUtility.DisplayDialog("Error", $"The property key '{newPropertyName}' already exists! Choose another name!", "Ok");
                    return;
                }
                int changedPropertyIndex = graphView.exposedProperties.FindIndex(x => x.propertyName == oldPropertyName);
                graphView.exposedProperties[changedPropertyIndex].propertyValue = newPropertyName;
                (visualElement as BlackboardField).text = newPropertyName;
            };
            blackboard.SetPosition(new Rect(x: 10, y: 30, width: 200, height: 300));

            graphView.Add(blackboard);
        }

        /// <summary>
        /// Constructs the dialogue graph view portion of the window.
        /// </summary>
        private void ConstructGraphView() {
            graphView = new DialogueGraphView(editorWindow: this) {
                name = "Dialogue Graph",
            };
            graphView.StretchToParentSize();
            graphView.RegisterCallback<KeyDownEvent>(callback: OnKeyDownEvent);
            rootVisualElement.Add(graphView);
        }

        /// <summary>
        /// Gets called when a key is pressed
        /// </summary>
        /// <param name="onKeyDownEvent"></param>
        private void OnKeyDownEvent(KeyDownEvent onKeyDownEvent) {
            if (onKeyDownEvent.ctrlKey && onKeyDownEvent.keyCode == KeyCode.S) {
                SaveData();
            }
        }

        /// <summary>
        /// Constructs the toolbar portion of the window.
        /// </summary>
        private void GenerateToolbar() {
            Toolbar toolbar = new Toolbar();

            TextField saveFileNameTextField = new TextField(label: "File Name");
            saveFileNameTextField.SetValueWithoutNotify(filename);
            saveFileNameTextField.MarkDirtyRepaint();
            saveFileNameTextField.RegisterValueChangedCallback(onChangedEvent => filename = onChangedEvent.newValue);
            toolbar.Add(saveFileNameTextField);

            Button saveButton = new Button(clickEvent: () => SaveData()) { text = "Save" };
            toolbar.Add(saveButton);
            Button loadButton = new Button(clickEvent: () => OpenFile()) { text = "Open" };
            toolbar.Add(loadButton);

            rootVisualElement.Add(toolbar);
        }

        /// <summary>
        /// Loads the nodes from a JSON text file.
        /// </summary>
        private void OpenFile() {
            currentDirectory = EditorPrefs.GetString(LAST_DIRECTORY_PREFS_KEY);
            string folder = string.IsNullOrWhiteSpace(currentDirectory) ? Application.dataPath : currentDirectory;
            string newPath = EditorUtility.OpenFilePanel("Select Dialogue Graph", folder, "json");
            if (string.IsNullOrWhiteSpace(newPath))
                return;
            currentFilePath = newPath;
            currentDirectory = System.IO.Path.GetDirectoryName(currentFilePath);
            EditorPrefs.SetString(LAST_DIRECTORY_PREFS_KEY, currentDirectory);

            LoadFile();
        }

        private void LoadFile() {
            if (string.IsNullOrWhiteSpace(currentFilePath)) {
                EditorUtility.DisplayDialog("Error", $"File at '{currentFilePath}' is not valid!", "ok");
                return;
            }
            GraphSaveUtility.LoadGraph(graphView, currentFilePath);
        }

        /// <summary>
        /// Writes the nodes as JSON to a text file.
        /// </summary>
        public void SaveData() {
            if (string.IsNullOrWhiteSpace(currentFilePath)) {
                var newFilepath = EditorUtility.SaveFilePanelInProject("Save Node Graph", "new dialogue", "json", "");
                if (string.IsNullOrWhiteSpace(newFilepath)) {
                    EditorUtility.DisplayDialog("Error", $"File path '{currentFilePath}' is not valid!", "ok");
                    return;
                }
                currentFilePath = newFilepath;
            }
            GraphSaveUtility.SerializeGraph(graphView, currentFilePath);
        }
    }
}
