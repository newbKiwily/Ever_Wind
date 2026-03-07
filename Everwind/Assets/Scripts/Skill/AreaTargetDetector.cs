using System.Collections.Generic;
using UnityEngine;

public class AreaTargetDetector : MonoBehaviour
{
    public HashSet<GameObject> DetectedEnemies = new HashSet<GameObject>();
    public AreaSkill Owner;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            DetectedEnemies.Add(other.gameObject);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            DetectedEnemies.Add(other.gameObject);
        }
    }

    public void Finish()
    {
        Owner.ReceiveTargets(DetectedEnemies);
        Destroy(gameObject);
    }
}