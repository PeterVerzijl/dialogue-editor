using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DialogueEditor {

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class NodeCreatorAttribute : Attribute {
        public Type type;

        public NodeCreatorAttribute(Type type) {
            this.type = type;
        }
    }
}
