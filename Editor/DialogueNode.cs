using System.Linq;

using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;

namespace DialogueEditor.Editor {

    public class DialogueNode : Node {
        public string Dialogue;
        public Character character;

        public override NodeData GetNodeData() => new DialogueNodeData { 
            Guid = this.GUID,
            position = this.GetPosition().position,
            DialogueText = this.Dialogue,
            characterGuid = this.character?.Guid,
        };
    }

    [NodeEditorSearchWindowEntry("Dialogue Nodes")]
    [NodeCreator(typeof(DialogueNodeData))]
    public class DialogueNodeCreator : NodeCreator<DialogueNode, DialogueNodeData> {

        /// <summary>
        /// Creates a new dialogue node.
        /// </summary>
        /// <param name="nodeName"></param>
        /// <param name="nodeData"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public override DialogueNode CreateNode(string nodeName, DialogueNodeData nodeData, Vector2 position = default) {
            Character[] characters = Resources.LoadAll<Character>("");
            Character character = characters.Where(character2 => character2.Guid == nodeData.characterGuid).FirstOrDefault();
            DialogueNode result = new DialogueNode {
                GUID = System.Guid.NewGuid().ToString(),
                title = nodeName,
                Dialogue = nodeData.DialogueText,
                character = character,
            };

            Port inputPort = GeneratePort(result, Direction.Input, Port.Capacity.Multi);
            inputPort.portName = "Input";
            Port outputPort = GeneratePort(result, Direction.Output, Port.Capacity.Single);
            outputPort.portName = "Output";

            result.inputContainer.Add(inputPort);
            result.outputContainer.Add(outputPort);

            VisualElement visualElement = new VisualElement {
                name = "node-contents",
            };
            ObjectField characterField = new ObjectField("Character");
            characterField.allowSceneObjects = false;
            characterField.objectType = typeof(Character);
            characterField.RegisterValueChangedCallback(callback: onValueChangedEvent => { result.character = onValueChangedEvent.newValue as Character; });
            characterField.SetValueWithoutNotify(character);
            visualElement.Add(characterField);

            TextField textField = new TextField(string.Empty);
            textField.multiline = true;
            textField.RegisterValueChangedCallback(onTextChanged => result.Dialogue = onTextChanged.newValue);
            textField.SetValueWithoutNotify(nodeData.DialogueText);
            visualElement.Add(textField);

            result.Q<VisualElement>("node-border").Add(visualElement);

            result.RefreshExpandedState();
            result.RefreshPorts();

            result.SetPosition(new Rect(position, defaultNodeSize));

            return result;
        }
    }
}
