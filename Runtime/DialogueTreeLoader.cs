using System.IO;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using Newtonsoft.Json;

namespace DialogueEditor.Runtime {

    public static class DialogueTreeLoader {

        /// <summary>
        /// Loads the dialogue tree from a dialogue text asset file and returns the first node.
        /// </summary>
        /// <param name="textAsset"></param>
        /// <returns></returns>
        public static DialogueContainer Load(TextAsset textAsset) {
            return Load(textAsset.text);
        }

        /// <summary>
        /// Loads the dialogue tree from a json file path and returns the first node.
        /// </summary>
        /// <param name="filePath">The location of the json file</param>
        /// <returns></returns>
        public static DialogueContainer LoadFile(string filePath) {
            string json = File.ReadAllText(filePath);
            return Load(json);
        }

        /// <summary>
        /// Loads the dialogue tree from json data and returns the first node.
        /// </summary>
        /// <param name="json">The json containing the node data.</param>
        /// <returns></returns>
        public static DialogueContainer Load(string json) {
            DialogueContainer dialogueContainer = JsonConvert.DeserializeObject<DialogueContainer>(json, new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.Auto,
                Converters = new List<JsonConverter> { new Vector2Serializer() },
            });
            return dialogueContainer;
        }
    }
}