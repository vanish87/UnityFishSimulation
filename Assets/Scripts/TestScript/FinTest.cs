using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class FinTest : MonoBehaviour
{
    public float3 worldUp = new float3(0, 1, 0);
    public float left = math.PI / 2;
    public float right = math.PI / 2;
    
    public float angle = math.PI/2;
    
    public float3 leftUpNormal;
    public float3 projection;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        var left = new float3(this.transform.forward);
        var normal = new float3(this.transform.up);
        left = math.normalize(left);
        normal = math.normalize(normal);

        this.leftUpNormal = math.normalize(math.cross(this.worldUp, left));
        this.projection = normal - (math.dot(normal, leftUpNormal) * leftUpNormal);
        this.projection = math.normalize(projection);
        var angleWithWorld = math.acos(math.dot(left, this.worldUp));

        this.left =  angleWithWorld;
        this.right = math.PI - angleWithWorld;

        this.angle = angleWithWorld;
    }


    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(float3.zero, this.leftUpNormal * 10);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(float3.zero, this.projection * 10);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(float3.zero, this.worldUp * 10);
    }
}
