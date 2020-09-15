using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class LeftRight : MonoBehaviour
{
    public GameObject other;
    public float angle = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var org = this.transform.position;
        var dir = this.transform.forward;

        var target = math.normalize(new float3(other.transform.position) - new float3(org));
        this.angle = math.dot(target, dir);


    }
}
