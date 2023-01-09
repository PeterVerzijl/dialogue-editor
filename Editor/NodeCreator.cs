using System;

using UnityEngine;

using UnityEditor.Experimental.GraphView;

namespace DialogueEditor.Editor {

    /// <summary>
    /// An abstract interface-like for node creators.
    /// </summary>
    /// <typeparam name="T1">The Node type</typeparam>
    /// <typeparam name="T2">The NodeData type</typeparam>
    public abstract class NodeCreator<T1, T2> where T1 : Node where T2 : NodeData {

        public Type typeParameterType = typeof(T1);

        public readonly Vector2 defaultNodeSize = new Vector2(x: 150, y: 200);

        /// <summary>
        /// Creates a node with the given nama at a position.
        /// </summary>
        /// <param name="nodeName"></param>
        /// <param name="position"></param>
        public abstract T1 CreateNode(string nodeName, T2 nodeData, Vector2 position = default);

        /// <summary>
        /// Generates a dialogue port. 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="direction"></param>
        /// <param name="capacity"></param>
        /// <returns></returns>
        public static Port GeneratePort(Node node, Direction direction, Port.Capacity capacity = Port.Capacity.Single) {
            return node.InstantiatePort(Orientation.Horizontal, direction, capacity, typeof(float));
        }
    }
}