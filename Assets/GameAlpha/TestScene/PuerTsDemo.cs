using UnityEngine;
using Puerts;

public class PuerTsDemo : MonoBehaviour
{
    // 1. Hello World
    private void Start()
    {
        var env = new ScriptEnv(new BackendV8());
        env.Eval(@"console.log('hello world');");
    }
}