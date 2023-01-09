namespace DialogueEditor.Editor {
    public abstract class Node : UnityEditor.Experimental.GraphView.Node {
        public string GUID;

        public abstract NodeData GetNodeData();
    }
}
