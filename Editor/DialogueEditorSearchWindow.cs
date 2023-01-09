using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace DialogueEditor.Editor {
    public class DialogueEditorSearchWindow : ScriptableObject, ISearchWindowProvider {

        private DialogueGraphView graphView;
        private EditorWindow window;
        private Texture2D indentationIcon;

        public void Init(DialogueGraphView graphView, EditorWindow window) {
            this.graphView = graphView;
            this.window = window;

            indentationIcon = new Texture2D(width: 1, height: 1);
            indentationIcon.SetPixel(x: 0, y: 0, Color.clear);
            indentationIcon.Apply();
        }

        /// <summary>
        /// Populates the search tree with nodes 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context) {
            List<SearchTreeEntry> searchTree = new List<SearchTreeEntry> { 
                new SearchTreeGroupEntry(new GUIContent("Create Node"), level: 0),
            };
            HashSet<string> folderNames = new HashSet<string>();

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass && !type.IsInterface)
                .Where(type => type.BaseType != null)
                .Where(type => type.BaseType.IsGenericType)
                .Where(type => type.BaseType.GetGenericTypeDefinition() == typeof(NodeCreator<,>));
            foreach (Type type in types) {
                NodeEditorSearchWindowEntryAttribute attribute = type.GetCustomAttribute<NodeEditorSearchWindowEntryAttribute>();
                if (!folderNames.Contains(attribute.SearchWindowPath)) {
                    folderNames.Add(attribute.SearchWindowPath);
                    searchTree.Add(new SearchTreeGroupEntry(new GUIContent(attribute.SearchWindowPath), level: 1));
                }
                ConstructorInfo constructor = type.GetConstructor(new Type[] { });
                object typeInstance = Activator.CreateInstance(type);
                string typeString = type.GetField("typeParameterType").GetValue(typeInstance).ToString().Split('.').Last();
                searchTree.Add(new SearchTreeEntry(new GUIContent(typeString, indentationIcon)) {
                    level = 2,
                    userData = typeInstance,
                });
            }

            //searchTree.Add(new SearchTreeGroupEntry(new GUIContent("Create Comment"), level: 1));
            searchTree.Add(new SearchTreeEntry(new GUIContent("New Comment", indentationIcon)) {
                level = 1,
                userData = "New Comment",
            });

            return searchTree;
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context) {
            // NOTE: The mouse position is in screen space, but we want the mouse positoin in window space.
            Vector2 mouseWorldPosition = window.rootVisualElement.ChangeCoordinatesTo(window.rootVisualElement.parent, 
                                                                                      context.screenMousePosition - window.position.position);
            Vector2 mouseLocalPosition = graphView.contentViewContainer.WorldToLocal(mouseWorldPosition);

            Type nodeCreatorType = searchTreeEntry.userData.GetType();
            Type typerParameterType = nodeCreatorType.GetField("typeParameterType").GetValue(searchTreeEntry.userData) as Type;
            string typeString = typerParameterType.ToString().Split('.').Last();
            Type nodeDataType = (nodeCreatorType.GetCustomAttribute(typeof(NodeCreatorAttribute), true) as NodeCreatorAttribute).type;
            dynamic newNodeData = Activator.CreateInstance(nodeDataType);
            dynamic creator = searchTreeEntry.userData;
            Node node = creator.CreateNode(typeString, newNodeData, mouseLocalPosition);

            graphView.AddNode(node);
            return true;
        }
    }
}
