using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace DialogueEditor.Runtime {

    public static class DialogueTreeWalker {

        public delegate IEnumerator<NodeData> NodeHandler(NodeData currentNode, IEnumerable<(NodeData, string)> nodeLinks);
        public static Dictionary<Type, NodeHandler> NodeHandlers = new Dictionary<Type, NodeHandler> {
            { typeof(DialogueStartNodeData), HandleStartNode },
        };

        /// <summary>
        /// Handles the logic for the start node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="nodeLinks"></param>
        /// <returns></returns>
        public static IEnumerator<NodeData> HandleStartNode(NodeData node, IEnumerable<(NodeData, string)> nodeLinks) { 
            yield return nodeLinks.Select(nodeLink => nodeLink.Item1).FirstOrDefault();
        }

        /// <summary>
        /// Starts the dialogue
        /// </summary>
        /// <param name="dialogueTree"></param>
        /// <returns></returns>
        public static IEnumerator StartDialogueTree(DialogueContainer dialogueTree) {
            NodeData nodeData = dialogueTree.GetStartNode();
            IEnumerator<NodeData> nextNode;
            do {
                Debug.Log($"[Dialogue] Next node: {nodeData.GetType()}!");
                if (NodeHandlers.TryGetValue(nodeData.GetType(), out NodeHandler GetNextNode)) {
                    IEnumerable<(NodeData, string)> nodeLinks = dialogueTree.GetNodeLinks(nodeData);
                    nextNode = GetNextNode(nodeData, nodeLinks);
                    while (nextNode.MoveNext()) {
                        yield return null;
                    }
                    nodeData = nextNode.Current;
                } else {
                    Debug.LogError($"[Dialogue] Can not find node hanlder for node type {nodeData.GetType()}!");
                    yield break;
                }
            } while (nodeData != null);
        }
        
        /// <summary>
        /// Returns the node with the given GUID, if it exists.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static NodeData GetNodeById(this DialogueContainer dialogue, string guid) {
            return dialogue.dialogueNodes
                .Where(node => node.Guid == guid).FirstOrDefault();
        }
        
        /// <summary>
        /// Returns the start node, if it exists.
        /// </summary>
        /// <returns></returns>
        public static NodeData GetStartNode(this DialogueContainer dialogue) {
            return dialogue.dialogueNodes
                .Where(node => node is DialogueStartNodeData).FirstOrDefault();
        }

        /// <summary>
        /// Returns an enumerable list of next nodes, from the given node.
        /// </summary>
        /// <param name="nodeData"></param>
        /// <returns></returns>
        public static IEnumerable<(NodeData, string)> GetNodeLinks(this DialogueContainer dialogue, NodeData nodeData) {
            return dialogue.nodeLinks
                .Where(nodeLink => nodeLink.FromNodeGuid == nodeData.Guid)
                .Select(nodeLink => (dialogue.GetNodeById(nodeLink.ToNodeGuid), nodeLink.FromNodePortName));
        }
    }
}