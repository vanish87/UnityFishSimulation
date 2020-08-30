using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityTools.Common;

public class SLTest : MonoBehaviour
{
    [Serializable]

    public class Base
    {
        public int a = 3;
    }
    [Serializable]
    public class Sub : Base
    {
        public int b = 1;
    }

    // Start is called before the first frame update
    void Start()
    {
        Base sub = new Sub() { b = 123 };

        FileTool.Write("test", sub);

        var laoded = FileTool.Read<Base>("test");

        Debug.Log((laoded as Sub).b);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
