using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EnemyHpUIManager : SingletonBase<EnemyHpUIManager>
{
    public override bool IsPersistent => false;

    [SerializeField]
    private GameObject _hpBarPrefab; // ?꾨━??吏곸젒 ?좊떦

    private List<GameObject> _hpBars = new List<GameObject>();
    private HashSet<GameObject> _activeEnemies = new HashSet<GameObject>();

    [SerializeField]
    private GameObject _damagedTextPrefab;
    [SerializeField]
    private int _poolSize = 15;

    [SerializeField]
    private int _textPoolSize = 30;
    private Queue<GameObject> _textPool = new Queue<GameObject>();
    private List<DamagedTextData> _activeTexts = new List<DamagedTextData>();

    struct DamagedTextData
    {
        public GameObject Obj;
        public TextMeshProUGUI TextMesh;
        public float StartTime;
        public Vector3 StartPos;
    }

    protected override void Awake()
    {
        Priority = 60;
        base.Awake();
    }

    private void OnEnable()
    {
        UIEvents.OnEnemyListUpdated += UpdateEnemyList;
        UIEvents.OnEnemyDamageTextRequested += ShowDamageText;
    }

    private void OnDisable()
    {
        UIEvents.OnEnemyListUpdated -= UpdateEnemyList;
        UIEvents.OnEnemyDamageTextRequested -= ShowDamageText;
    }

    void Start()
    {
        for (int i = 0; i < _poolSize; i++)
        {
            var hpBar = Instantiate(_hpBarPrefab, this.transform);
            _hpBars.Add(hpBar);
            hpBar.SetActive(false);
        }

        for (int i = 0; i < _textPoolSize; i++)
        {
            var damagePrefab = Instantiate(_damagedTextPrefab, transform);
            damagePrefab.SetActive(false);
            _textPool.Enqueue(damagePrefab);
        }
    }

    private void Update()
    {
        float duration = 1.0f;
        for (int i = _activeTexts.Count - 1; i >= 0; i--)
        {
            var item = _activeTexts[i];
            float elapsed = Time.time - item.StartTime;
            float percent = elapsed / duration;

            if (percent >= 1.0f)
            {
                item.Obj.SetActive(false);
                _textPool.Enqueue(item.Obj);
                _activeTexts.RemoveAt(i);
                continue;
            }

            item.Obj.transform.position = item.StartPos + new Vector3(0, percent * 1.5f, 0);

            Color c = item.TextMesh.color;
            c.a = 1.0f - percent;
            item.TextMesh.color = c;

            if (Camera.main != null)
                item.Obj.transform.rotation = Camera.main.transform.rotation;
        }
    }

    public void UpdateEnemyList(List<GameObject> enemies)
    {
        foreach (var enemy in enemies)
        {
            if (_activeEnemies.Contains(enemy))
                continue;

            var hpBar = AssignHpBar();
            if (hpBar == null) continue;

            var enemyComp = enemy.GetComponent<Enemy>();

            enemyComp.OnEnemyDied -= OnEnemyDied;
            enemyComp.OnEnemyDied += OnEnemyDied;

            _activeEnemies.Add(enemy);
            hpBar.SetActive(true);
            hpBar.GetComponent<EnemyHpUI>().Initialize(enemy);
        }
    }

    public void ShowDamageText(Vector3 worldPos, int damage)
    {
        if (_textPool.Count == 0) return;

        GameObject obj = _textPool.Dequeue();
        obj.transform.position = worldPos + Vector3.left * 1.7f;
        obj.SetActive(true);

        var tmpro = obj.GetComponent<TextMeshProUGUI>();
        tmpro.text = damage.ToString();

        _activeTexts.Add(new DamagedTextData
        {
            Obj = obj,
            TextMesh = tmpro,
            StartTime = Time.time,
            StartPos = obj.transform.position
        });
    }

    private GameObject AssignHpBar()
    {
        foreach (var hpBar in _hpBars)
        {
            if (!hpBar.activeSelf)
                return hpBar;
        }

        GameObject newBar = Instantiate(_hpBarPrefab, transform);
        _hpBars.Add(newBar);
        return newBar;
    }

    private void OnEnemyDied(Enemy enemy)
    {
        _activeEnemies.Remove(enemy.gameObject);

        foreach (var bar in _hpBars)
        {
            var ui = bar.GetComponent<EnemyHpUI>();
            if (ui.targetEnemy == enemy.gameObject)
            {
                ui.targetEnemy = null;
                bar.SetActive(false);
                break;
            }
        }
    }
}
