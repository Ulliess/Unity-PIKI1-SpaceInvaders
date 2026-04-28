using UnityEngine;
using Unity.Netcode;

public class SquareNetworkMovementTest : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        transform.position += new Vector3(Input.GetAxis("Horizontal"),Input.GetAxis("Vertical")) * 5f * Time.deltaTime;

    }
}
