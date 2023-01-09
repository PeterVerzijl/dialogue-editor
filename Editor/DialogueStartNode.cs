namespace DialogueEditor.Editor {
    public class DialogueStartNode : Node {
        public override NodeData GetNodeData() => new DialogueStartNodeData {
            Guid = this.GUID,
            position = this.GetPosition().position,
        };
    }
}
