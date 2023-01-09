using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor.Experimental.GraphView;

namespace DialogueEditor.Editor {
    public class QuestionNode : Node {
        public List<string> Questions = new List<string>();

        public override NodeData GetNodeData() => new QuestionNodeData {
            Guid = this.GUID,
            position = this.GetPosition().position,
            Questions = this.Questions.ToArray(),
        };
    }

    [NodeEditorSearchWindowEntry("Dialogue Nodes")]
    [NodeCreator(typeof(QuestionNodeData))]
    public class QuestionNodeCreator : NodeCreator<QuestionNode, QuestionNodeData> {

        /// <summary>
        /// Creates a new question node
        /// </summary>
        /// <param name="nodeName"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override QuestionNode CreateNode(string nodeName, QuestionNodeData nodeData, Vector2 position = default) {
            List<string> questions = nodeData.Questions != null ? nodeData.Questions.ToList() : new List<string>();
            QuestionNode result = new QuestionNode {
                GUID = System.Guid.NewGuid().ToString(),
                title = nodeName,
                Questions = questions,
            };

            Button addChoiceButton = new Button(clickEvent: () => AddChoicePort(result, addNewQuestion: true));
            addChoiceButton.text = "New Choice";
            result.outputContainer.Add(addChoiceButton);

            Port inputPort = GeneratePort(result, Direction.Input, Port.Capacity.Multi);
            inputPort.name = "Input";
            result.inputContainer.Add(inputPort);

            for (int questionIndex = 0; questionIndex < result.Questions.Count; ++questionIndex) {
                AddChoicePort(result, result.Questions[questionIndex], addNewQuestion: false);
            }

            result.RefreshExpandedState();
            result.RefreshPorts();

            result.SetPosition(new Rect(position, defaultNodeSize));

            return result;
        }

        /// <summary>
        /// Adds a choice to the given question node.
        /// </summary>
        /// <param name="questionNode"></param>
        /// <param name="addNewQuestion">Wether to add a question to the nodes questions list.</param>
        public void AddChoicePort(QuestionNode questionNode, string choice = "", bool addNewQuestion = false) {
            Port outputPort = GeneratePort(questionNode, Direction.Output, Port.Capacity.Single);

            // NOTE: Remove the label
            Label automaticLabel = outputPort.contentContainer.Q<Label>(name: "type");
            outputPort.contentContainer.Remove(automaticLabel);

            int outputPortCount = questionNode.outputContainer.Query("connector").ToList().Count;

            string portName = string.IsNullOrEmpty(choice) ? $"Choice {outputPortCount}" : choice;

            if (addNewQuestion) { questionNode.Questions.Add(portName); }

            TextField textField = new TextField {
                name = string.Empty,
                value = portName,
                multiline = true,
            };
            textField.RegisterValueChangedCallback(callback: onValueChangedEvent => {
                int choiceIndex = questionNode.outputContainer.IndexOf(outputPort) - 1;
                outputPort.portName = onValueChangedEvent.newValue;
                questionNode.Questions[choiceIndex] = onValueChangedEvent.newValue;
            });
            outputPort.contentContainer.Add(child: new Label(text: " "));
            outputPort.contentContainer.Add(textField);

            Button deleteButton = new Button(clickEvent: () => RemovePort(questionNode, outputPort)) {
                text = "x",
            };
            outputPort.contentContainer.Add(deleteButton);

            outputPort.portName = portName;
            questionNode.outputContainer.Add(outputPort);
            questionNode.RefreshPorts();
            questionNode.RefreshExpandedState();
        }

        /// <summary>
        /// Remove a port from the question node.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="port"></param>
        public void RemovePort(QuestionNode node, Port port) {
            node.Questions.Remove(port.portName);

            node.outputContainer.Remove(port);
            node.RefreshPorts();
            node.RefreshExpandedState();
        }
    }
}
