using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EnemyHpUI : MonoBehaviour
{
    public GameObject hpFilledArea;
    public GameObject targetEnemy;
    private Enemy enemy;
    public TextMeshProUGUI enemyName;

    private Transform mainCameraTransform;

    public void Initialize(GameObject target)
    {
        targetEnemy = target;
        enemyName.text = targetEnemy.name;
        enemy = targetEnemy.GetComponent<Enemy>();

        if (Camera.main != null)
            mainCameraTransform = Camera.main.transform;
    }

    void Update()
    {
        if (targetEnemy == null || enemy == null) return;

        Vector3 basePos = targetEnemy.transform.position + Vector3.up * 3.5f;
        transform.position = basePos;

        transform.LookAt(transform.position + mainCameraTransform.rotation * Vector3.forward,
                         mainCameraTransform.rotation * Vector3.up);

        float currHp = enemy.GetHp();
        float maxHp = 100f; // 기본값. 만약 Enemy 클래스에 MaxHp가 있다면 그 값을 가져오세요.

        float ratio = Mathf.Clamp01(currHp / maxHp);
        Vector3 scale = hpFilledArea.transform.localScale;
        scale.x = ratio;
        hpFilledArea.transform.localScale = scale;
    }
}