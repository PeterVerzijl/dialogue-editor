using System;

namespace DialogueEditor.Editor {
    public class NodeEditorSearchWindowEntryAttribute : Attribute {
        
        private string searchWindowPath;
        public string SearchWindowPath => searchWindowPath;
        public NodeEditorSearchWindowEntryAttribute(string searchWindowPath) {
            this.searchWindowPath = searchWindowPath;
        }
    }
}