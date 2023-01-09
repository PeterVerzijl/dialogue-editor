using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "Assets/Create/Character")]
public class Character : ScriptableObject {

    [ReadOnly, SerializeField] private string guid;
    public string Guid => this.guid;

    public new string name;

    public void Reset() {
        guid = System.Guid.NewGuid().ToString();
    }
}
