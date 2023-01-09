using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using DialogueEditor;
using DialogueEditor.Runtime;
using System;

public class DialogueUIController : MonoBehaviour {

    public TextAsset dialogue;
    [Space]
    public RectTransform textContent;
    public GameObject dialogueElementPrefab;
    public GameObject questionElementPrefab;
    public GameObject dialogueEndElementPrefab;

    private void OnEnable() {
        DialogueTreeWalker.NodeHandlers.Add(typeof(DialogueNodeData), HandleDialogueNode);
        DialogueTreeWalker.NodeHandlers.Add(typeof(QuestionNodeData), HandleQuestionNode);
        DialogueTreeWalker.NodeHandlers.Add(typeof(UpdateQuestNodeData), HandleUpdateQuestNode);
        DialogueTreeWalker.NodeHandlers.Add(typeof(QuestConditionNodeData), HandleQuestConditionNode);
        DialogueTreeWalker.NodeHandlers.Add(typeof(HasItemNodeData), HandleHasItemNode);
    }

    // Start is called before the first frame update
    void Start() {
        for (int childIndex = textContent.childCount - 1; childIndex >= 0; --childIndex) {
            Destroy(textContent.GetChild(childIndex).gameObject);
        }

        StartCoroutine(StartConversation());
    }

    // Update is called once per frame
    void Update() {
        
    }

    /// <summary>
    /// NOTE: Starts playing the conversation tree.
    /// </summary>
    /// <param name="dialogueTree"></param>
    /// <returns></returns>
    private IEnumerator StartConversation() {
        DialogueContainer dialogueTree = DialogueTreeLoader.Load(dialogue);
        yield return StartCoroutine(DialogueTreeWalker.StartDialogueTree(dialogueTree));

        // NOTE: Show user that the dialogue has ended.
        GameObject dialogueElement = Instantiate(dialogueEndElementPrefab, textContent);
    }

    /// <summary>
    /// Handles the logic for the dialogue node
    /// </summary>
    /// <param name="node"></param>
    /// <param name="nextNodes"></param>
    /// <returns></returns>
    public IEnumerator<NodeData> HandleDialogueNode(NodeData node, IEnumerable<(NodeData, string)> nodeLinks) {
        DialogueNodeData dialogueNodeData = (DialogueNodeData)node;

        Character[] allCharacters = Resources.LoadAll<Character>("");
        Character character = allCharacters.Where(c => c.Guid == dialogueNodeData.characterGuid).FirstOrDefault();

        GameObject dialogueElement = Instantiate(dialogueElementPrefab, textContent);
        dialogueElement.transform.Find("Text Box").GetComponentInChildren<Text>().text = dialogueNodeData.DialogueText;

        if (character && !string.IsNullOrWhiteSpace(character.name)) {
            dialogueElement.transform.Find("Speaker Text").GetComponentInChildren<Text>().text = character.name;
        } else {
            // NOTE: Just delete the text element when we don't use it to show the name.
            Destroy(dialogueElement.transform.Find("Speaker Text").gameObject);
        }

        // NOTE: Wait for a few seconds.
        float timer = 2.5f;
        while (timer > 0) {
            yield return null;
            timer -= Time.deltaTime;
        }

        yield return nodeLinks.Select(nodeLink => nodeLink.Item1).FirstOrDefault();
    }

    /// <summary>
    /// Handles the logic for the question node
    /// </summary>
    /// <param name="node"></param>
    /// <param name="nextNodes"></param>
    /// <returns></returns>
    public IEnumerator<NodeData> HandleQuestionNode(NodeData node, IEnumerable<(NodeData, string)> nodeLinks) {
        QuestionNodeData questionNodeData = (QuestionNodeData)node;
        GameObject questionElement = Instantiate(questionElementPrefab, textContent);
        Transform questionButton = questionElement.transform.Find("Question Button");

        int buttonPressed = -1;
        for (int questionIndex = 0; questionIndex < questionNodeData.Questions.Length; ++questionIndex) {
            if (questionIndex == 0) {
                int buttonIndex = questionIndex;

                questionButton.GetComponentInChildren<Text>().text = questionNodeData.Questions[questionIndex];
                questionButton.GetComponentInChildren<Button>().onClick.AddListener(() => {
                    buttonPressed = buttonIndex;
                });
            } else {
                int buttonIndex = questionIndex;

                GameObject newQuestionButton = Instantiate(questionButton.gameObject, questionElement.transform);
                newQuestionButton.GetComponentInChildren<Text>().text = questionNodeData.Questions[questionIndex];
                newQuestionButton.GetComponentInChildren<Button>().onClick.AddListener(() => {
                    buttonPressed = buttonIndex;
                });
            }
        }

        while (buttonPressed < 0) {
            yield return null;
        }
        string selectedButtonName = questionNodeData.Questions[buttonPressed];
        NodeData nextNode = nodeLinks
            .Where(nodeLink => nodeLink.Item2 == selectedButtonName)
            .Select(nodeLink => nodeLink.Item1)
            .ElementAtOrDefault(buttonPressed);
        yield return nextNode;
    }

    /// <summary>
    /// Handles the logic for the update quest node
    /// </summary>
    /// <param name="node"></param>
    /// <param name="nextNodes"></param>
    /// <returns></returns>
    public IEnumerator<NodeData> HandleUpdateQuestNode(NodeData node, IEnumerable<(NodeData, string)> nodeLinks) {
        UpdateQuestNodeData updateQuestNodeData = (UpdateQuestNodeData)node;

        Quest[] allQuests = Resources.LoadAll<Quest>("");
        Quest quest = allQuests.Where(q => q.Guid == updateQuestNodeData.questGuid).FirstOrDefault();

        if (quest) {
            Debug.Log($"[Quest] State updated: {quest.state} -> {updateQuestNodeData.newState}!");
            Debug.Log($"[Quest] Result updated: {quest.result} -> {updateQuestNodeData.newResult}!");
            quest.state = updateQuestNodeData.newState;
            quest.result = updateQuestNodeData.newResult;
        } else {
            Debug.LogError($"[Quest] Fatal Errro: Unknown quest with guid {updateQuestNodeData.questGuid}!");
        }

        Debug.Log($"[Quest] Quest '{updateQuestNodeData.questGuid}' updated to state: '{updateQuestNodeData.newState}' and result: '{updateQuestNodeData.newResult}'!");

        yield return nodeLinks.Select(nodeLink => nodeLink.Item1).FirstOrDefault();
    }

    /// <summary>
    /// Handles the logic for the update quest node
    /// </summary>
    /// <param name="node"></param>
    /// <param name="nextNodes"></param>
    /// <returns></returns>
    public IEnumerator<NodeData> HandleQuestConditionNode(NodeData node, IEnumerable<(NodeData, string)> nodeLinks) {
        QuestConditionNodeData questConditionNodeData = (QuestConditionNodeData)node;

        Quest[] allQuests = Resources.LoadAll<Quest>("");
        Quest quest = allQuests.Where(q => q.Guid == questConditionNodeData.questGuid).FirstOrDefault();

        Quest.State questState = (int)Quest.State.UnDiscovered;
        if (quest) {
            questState = quest.state;
            Debug.Log($"[Quest] Quest state: {quest.state}");
        } else {
            Debug.LogError($"[Quest] Fatal Errro: Unknown quest with guid {questConditionNodeData.questGuid}!");
        }

        yield return nodeLinks
            .Where(nodeLink => nodeLink.Item2 == questState.ToString())
            .Select(nodeLink => nodeLink.Item1).FirstOrDefault();
    }

    /// <summary>
    /// Handles the logic for the update quest node
    /// </summary>
    /// <param name="node"></param>
    /// <param name="nextNodes"></param>
    /// <returns></returns>
    public IEnumerator<NodeData> HandleHasItemNode(NodeData node, IEnumerable<(NodeData, string)> nodeLinks) {
        HasItemNodeData hasItemNodeData = (HasItemNodeData)node;

        Debug.Log($"[Item] Has item '{hasItemNodeData.itemId}'!");
        bool hasItem = InventoryBase.Instance.inventory.Any(item => item.itemID == hasItemNodeData.itemId);
        string portName = hasItem ? "True" : "False";

        NodeData nextNode = nodeLinks
            .Where(nodeLink => nodeLink.Item2 == portName)
            .Select(nodeLink => nodeLink.Item1)
            .FirstOrDefault();
        yield return nextNode;
    }
}