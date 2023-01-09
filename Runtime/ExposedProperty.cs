using System;

namespace DialogueEditor.Runtime {
    
    [Serializable]
    public class ExposedProperty {
        public string propertyName = "new string";
        public string propertyValue = "new value";

        public ExposedProperty() {
            propertyName = "new string";
            propertyValue = "new value";
        }
    }
}
