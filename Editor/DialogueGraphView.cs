using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

using DialogueEditor.Runtime;

namespace DialogueEditor.Editor {
    public class DialogueGraphView : GraphView {

        public DialogueStartNode EntryPointNode;

        private DialogueEditorSearchWindow searchWindow;

        public List<ExposedProperty> exposedProperties = new List<ExposedProperty>();
        
        private Vector2 lastLocalMousePosition;

        /// <summary>
        /// Construcs a new dialogue graph view.
        /// </summary>
        public DialogueGraphView(EditorWindow editorWindow) {
            styleSheets.Add(Resources.Load<StyleSheet>("DialogueGraph"));
            styleSheets.Add(Resources.Load<StyleSheet>("Node"));

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            GridBackground gridBackground = new GridBackground();
            Insert(index: 0, gridBackground);
            gridBackground.StretchToParentSize();

            AddElement(EntryPointNode = GenerateEntryPointNode());

            CreateAndIntitializeSearchWindow(editorWindow);

            this.viewDataKey = "DialogueGraphView";

            this.canPasteSerializedData = (string data) => GraphSaveUtility.IsJsonValid(data);
            this.unserializeAndPaste = (string operationName, string data) => {
                switch (operationName) {
                    case "Paste": {
                        try {
                            GraphSaveUtility.AddElementsToGraph(this, data, offset: lastLocalMousePosition, createCopies: true);
                        } catch (Newtonsoft.Json.JsonReaderException) { 
                            // NOTE: Invalid json in copy buffer. Do nothing.
                        }
                    } break;
                    
                    default: {
                        throw new System.NotImplementedException($"Unimplemented 'unserializeAndPaste' operation: {operationName}!");
                    }
                }
            };
            this.serializeGraphElements = (IEnumerable<GraphElement>elements) => {
                string serializedData = GraphSaveUtility.SerializeGraphElements(elements);
                return serializedData;
            };

            this.RegisterCallback<PointerDownEvent>((PointerDownEvent evt) => {
                lastLocalMousePosition = evt.localPosition;
            }, TrickleDown.TrickleDown);
        }

        /// <summary>
        /// Creates and initializes the search window
        /// </summary>
        /// <param name="editorWindow"></param>
        private void CreateAndIntitializeSearchWindow(EditorWindow editorWindow) {
            searchWindow = ScriptableObject.CreateInstance<DialogueEditorSearchWindow>();
            searchWindow.Init(this, editorWindow);
            nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindow);
        }

        /// <summary>
        /// Adds an exposed property to the blackboard.
        /// </summary>
        /// <param name="blackboard"></param>
        /// <param name="exposedProperty"></param>
        public void AddPropertyToBlackBoard(Blackboard blackboard, ExposedProperty exposedProperty) {
            string localPropertyName = exposedProperty.propertyName;
            string localPropertyValue = exposedProperty.propertyValue;
            int index = 1;
            while (exposedProperties.Any(x => x.propertyName == localPropertyName)) {
                localPropertyName = $"{Regex.Replace(localPropertyName, @"\(\d+\)" , "")}({index++})";
            }

            ExposedProperty newExposedProperty = new ExposedProperty {
                propertyName = localPropertyName,
                propertyValue = localPropertyValue,
            };
            exposedProperties.Add(newExposedProperty);

            VisualElement container = new VisualElement();
            BlackboardField blackboardField = new BlackboardField {
                text = newExposedProperty.propertyName,
                typeText = "string",
            };
            container.Add(blackboardField);

            TextField propertyValueTextField = new TextField(label: "Value:") {
                value = newExposedProperty.propertyValue,
            };
            propertyValueTextField.RegisterValueChangedCallback(callback: onValueChangedEvent => {
                int changedPropertyIndex = exposedProperties.FindIndex(x => x.propertyName == newExposedProperty.propertyName);
                exposedProperties[changedPropertyIndex].propertyValue = onValueChangedEvent.newValue;
            });
            BlackboardRow blackboardValueRow = new BlackboardRow(item: blackboardField, propertyView: propertyValueTextField);
            container.Add(blackboardValueRow);

            blackboard.Add(container);
        }

        /// <summary>
        /// Generates the starting point for the dialogue.
        /// </summary>
        /// <returns></returns>
        public DialogueStartNode GenerateEntryPointNode() {
            DialogueStartNode entryPointNode = new DialogueStartNode {
                title = "START",
                GUID = System.Guid.NewGuid().ToString(),
            };
            entryPointNode.SetPosition(new Rect(x: 100, y: 200, width: 100, height: 150));

            Port outputPort = NodeCreator<Node, NodeData>.GeneratePort(entryPointNode, Direction.Output, Port.Capacity.Single);
            outputPort.portName = "Next";
            entryPointNode.outputContainer.Add(outputPort);

            entryPointNode.capabilities &= ~Capabilities.Movable; 
            entryPointNode.capabilities &= ~Capabilities.Deletable;

            entryPointNode.RefreshExpandedState();
            entryPointNode.RefreshPorts();
            
            return entryPointNode;
        }

        /// <summary>
        /// Adds the node to the graph.
        /// </summary>
        /// <param name="nodeName"></param>
        internal void AddNode(Node node) {
            AddElement(node);
        }

        /// <summary>
        /// Returns all compatible ports for the given start port.
        /// </summary>
        /// <param name="startPort"></param>
        /// <param name="nodeAdapter"></param>
        /// <returns></returns>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {
            List<Port> compatiblePorts = new List<Port>();
            ports.ForEach(port => {
                Port portView = port;
                if (startPort != port && startPort.node != port.node && startPort.portType == port.portType) {
                    compatiblePorts.Add(port);
                }
            });
            return compatiblePorts;
        }
    }
}