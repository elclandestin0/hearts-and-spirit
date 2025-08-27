using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class CineAction : ScriptableObject
{
    public abstract IEnumerator Execute(CineContext ctx);
}