using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

using Newtonsoft.Json;

using System.IO;

namespace DialogueEditor.Editor {
    public class GraphSaveUtility {
    
        /// <summary>
        /// Serializes the nodes in the graph view to a json file.
        /// </summary>
        /// <param name="dialogueGraphView"></param>
        /// <param name="filePath"></param>
        public static void SerializeGraph(DialogueGraphView dialogueGraphView, string filePath) {
            DialogueContainer newDialogueContainer = new DialogueContainer();

            bool saveNodesSuccess = SaveNodes(dialogueGraphView, newDialogueContainer);
            if (saveNodesSuccess) {
                string serializedDialogueContainer = JsonConvert.SerializeObject(newDialogueContainer, Formatting.Indented, new JsonSerializerSettings {
                    TypeNameHandling = TypeNameHandling.All,
                    Converters = new List<JsonConverter> { new Vector2Serializer(), },
                });
                File.WriteAllText(filePath, serializedDialogueContainer);
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Saves the node and edge data to the given dialogue container data structure.
        /// </summary>
        /// <param name="dialogueGraphView"></param>
        /// <param name="newDialogueContainer"></param>
        /// <returns></returns>
        public static bool SaveNodes(DialogueGraphView dialogueGraphView, DialogueContainer newDialogueContainer) {
            List<Edge> edges = dialogueGraphView.edges.ToList();
            List<Node> dialogueNodes = dialogueGraphView.nodes.ToList().Cast<Node>().ToList();

            if (!edges.Any()) return false;
            SerializeEdges(newDialogueContainer, edges);
            SerializeNodes(newDialogueContainer, dialogueNodes);

            return true;
        }

        /// <summary>
        /// Serializes the nodes into the dialogue container.
        /// </summary>
        /// <param name="dialogueContainer"></param>
        /// <param name="edges"></param>
        private static void SerializeNodes(DialogueContainer dialogueContainer, List<Node> dialogueNodes) {
            // NOTE: Serialize the nodes themselves
            dialogueContainer.dialogueNodes = new NodeData[dialogueNodes.Count];
            for (int nodeIndex = 0; nodeIndex < dialogueNodes.Count; nodeIndex++) {
                Node node = dialogueNodes[nodeIndex];
                dialogueContainer.dialogueNodes[nodeIndex] = node.GetNodeData();
            }
        }

        /// <summary>
        /// Serializes the edges into the dialogue container.
        /// </summary>
        /// <param name="dialogueContainer"></param>
        /// <param name="edges"></param>
        private static void SerializeEdges(DialogueContainer dialogueContainer, List<Edge> edges) {
            // NOTE: Serialize the edges
            Edge[] connectedEdges = edges.Where(edge => edge.input.node != null).ToArray();
            dialogueContainer.nodeLinks = new NodeLinkData[connectedEdges.Length];
            for (int edgeIndex = 0; edgeIndex < connectedEdges.Length; edgeIndex++) {
                try {
                    Node fromNode = connectedEdges[edgeIndex].output.node as Node;
                    Node toNode = connectedEdges[edgeIndex].input.node as Node;

                    string fromNodeGUID = fromNode != null ? fromNode.GUID : string.Empty;
                    string fromNodePortName = connectedEdges[edgeIndex] != null ? connectedEdges[edgeIndex].output.portName : string.Empty;
                    string toNodeGUID = toNode != null ? toNode.GUID : string.Empty;

                    dialogueContainer.nodeLinks[edgeIndex] = new NodeLinkData {
                        FromNodeGuid = fromNodeGUID,
                        FromNodePortName = fromNodePortName,
                        ToNodeGuid = toNodeGUID,
                    };
                } catch (Exception e) {
                    Debug.LogError(e);
                }
            }
        }

        /// <summary>
        /// Serializes the given elements to JSON.
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        public static string SerializeGraphElements(IEnumerable<GraphElement> elements) {
            DialogueContainer newDialogueContainer = new DialogueContainer();

            List<Edge> edges = new List<Edge>();
            List<Node> nodes = new List<Node>();
            foreach (GraphElement graphElement in elements) {
                if (graphElement is Edge edge) {
                    edges.Add(edge);
                } else if (graphElement is Node node) {
                    nodes.Add(node);
                }
            }
            SerializeEdges(newDialogueContainer, edges);
            SerializeNodes(newDialogueContainer, nodes);

            string serializedDialogueContainer = JsonConvert.SerializeObject(newDialogueContainer, Formatting.Indented, new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.All,
                Converters = new List<JsonConverter> { new Vector2Serializer(), },
            });
            return serializedDialogueContainer;
        }

        /// <summary>
        /// Adds the serialized nodes to the graph view.
        /// NOTE: Used for copy-paste operations.
        /// </summary>
        /// <param name="graphView"></param>
        /// <param name="json"></param>
        public static void AddElementsToGraph(DialogueGraphView graphView, string json) {
            AddElementsToGraph(graphView, json, Vector2.zero);
        }

        /// <summary>
        /// Checks if the json is valid.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static bool IsJsonValid(string json) {
            try {
                DialogueContainer dialogueContainer = JsonConvert.DeserializeObject<DialogueContainer>(json, new JsonSerializerSettings {
                    TypeNameHandling = TypeNameHandling.Auto,
                    Converters = new List<JsonConverter> { new Vector2Serializer() },
                });
                return true;
            } catch (JsonReaderException) {
                return false;
            }
        }
        
        /// <summary>
        /// Adds the serialized nodes to the graph view.
        /// NOTE: Used for copy-paste operations.
        /// </summary>
        /// <param name="graphView"></param>
        /// <param name="json"></param>
        /// <param name="offset"></param>
        public static void AddElementsToGraph(DialogueGraphView graphView, string json, Vector2 offset, bool createCopies = false) {
            DialogueContainer dialogueContainer = JsonConvert.DeserializeObject<DialogueContainer>(json, new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.Auto,
                Converters = new List<JsonConverter> { new Vector2Serializer() },
            });
            if (dialogueContainer != null) {
                Dictionary<string, Node> nodeGuidMap = new Dictionary<string, Node>(dialogueContainer.nodeLinks.Length);

                var nodeCreators =
                    from a in AppDomain.CurrentDomain.GetAssemblies()
                    from t in a.GetTypes()
                    let attributes = t.GetCustomAttributes(typeof(NodeCreatorAttribute), true)
                    where attributes != null && attributes.Length > 0
                    select new { Type = t, Attribute = attributes.Cast<NodeCreatorAttribute>().FirstOrDefault() };

                for (int nodeIndex = 0; nodeIndex < dialogueContainer.dialogueNodes.Length; ++nodeIndex) {
                    try {
                        dynamic nodeData = dialogueContainer.dialogueNodes[nodeIndex];
                        Node node;

                        var tuple = nodeCreators.Where(t => t.Attribute.type == nodeData.GetType()).FirstOrDefault();
                        string nodeGuid;
                        if (tuple != null) {
                            Type nodeCreatorType = tuple.Type;
                            dynamic creator = Activator.CreateInstance(nodeCreatorType);
                            string fullTypeName = tuple.Attribute.type.ToString();
                            string typeName = Regex.Match(fullTypeName, @"\w+\.((?:[A-Z][a-z]+)+)Data").Groups[1].Value;
                            string nodeTitle = Regex.Replace(typeName, @"[A-Z]", m => $" {m.Value}");
                            Node newNode = creator.CreateNode(nodeTitle, nodeData, nodeData.position);

                            nodeGuid = createCopies ? GUID.Generate().ToString() : nodeData.Guid;
                            newNode.GUID = nodeGuid;

                            Rect nodeRect = newNode.GetPosition();
                            nodeRect.position = nodeData.position - offset;
                            newNode.SetPosition(nodeRect);

                            node = newNode;
                        } else {
                            // NOTE: We should never allow copies of the entry point node!
                            if (createCopies) {
                                continue;
                            }
                            graphView.EntryPointNode = graphView.GenerateEntryPointNode();

                            nodeGuid = nodeData.Guid;
                            graphView.EntryPointNode.GUID = nodeGuid;

                            Rect nodeRect = graphView.EntryPointNode.GetPosition();
                            nodeRect.position = nodeData.position + offset;
                            graphView.EntryPointNode.SetPosition(nodeRect);

                            node = graphView.EntryPointNode;
                        }
                        nodeGuidMap.Add(nodeGuid, node);
                        graphView.AddElement(node);
                    } catch (Exception e) {
                        Debug.LogError(e);
                    }
                }

                // Add connections
                for (int nodeConnectionIndex = 0; nodeConnectionIndex < dialogueContainer.nodeLinks.Length; ++nodeConnectionIndex) {
                    NodeLinkData nodeLinkData = dialogueContainer.nodeLinks[nodeConnectionIndex];
                    if (nodeGuidMap.TryGetValue(nodeLinkData.FromNodeGuid, out Node fromNode) &&
                        nodeGuidMap.TryGetValue(nodeLinkData.ToNodeGuid, out Node toNode)) {
                        try {
                            Port fromPort = fromNode.outputContainer.Children().OfType<Port>().FirstOrDefault(x => x.portName == nodeLinkData.FromNodePortName);
                            Port toPort = (Port)toNode.inputContainer[0];

                            Edge edge = new Edge {
                                output = fromPort,
                                input = toPort,
                            };
                            edge?.input.Connect(edge);
                            edge?.output.Connect(edge);
                            graphView.AddElement(edge);
                        } catch (Exception e) {
                            Debug.LogError(e);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Loads the graph from the json file and populates the given graph view.
        /// </summary>
        /// <param name="graphView"></param>
        /// <param name="filePath"></param>
        public static void LoadGraph(DialogueGraphView graphView, string filePath) {
            graphView.nodes.ForEach(node => graphView.RemoveElement(node));
            graphView.edges.ForEach(edge => graphView.RemoveElement(edge));

            string serializedDialogueContainer = File.ReadAllText(filePath);
            AddElementsToGraph(graphView, serializedDialogueContainer);
        }
    }
}
