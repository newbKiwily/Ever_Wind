
using UnityEngine;

public class PlayerStatManager : MonoBehaviour
{
    public struct Stat
    {
        public float Speed;
        public int AttackPower;
        public int DefencePower;
        public float Hp;
        public float MaxHp;
    }
    private Stat _stat = new Stat();

    public void Init()
    {
        _stat = SingletonManager.Instance.GetSingleton<DataCenter>().LoadStat;
    }

    public Stat GetStat()
    { return _stat; }

    public float CalculateDamaged(float dmg)
    {
        float result = 0;
        result = dmg * (100.0f / (100.0f + _stat.DefencePower));
        return result;
    }

    public float CalculateFinalDamage(float ratio)
    {
        float result = 0;
        result = _stat.AttackPower + (_stat.AttackPower * (ratio / 100.0f));
        return result;
    }

    public float GetHp()
    {
        return _stat.Hp;
    }

    public void SetHp(float hp)
    {
        this._stat.Hp = hp;
    }

    public float GetMaxHp()
    {
        return _stat.MaxHp;
    }

    public void SetMaxHp(float maxHp)
    {
        this._stat.MaxHp = maxHp;
    }

    public void SetAttackPower(int power)
    {
        this._stat.AttackPower = power;
    }

    public int GetAttackPower()
    {
        return _stat.AttackPower;
    }

    public int GetDefencePower()
    {
        return _stat.DefencePower;
    }

    public void SetDefencePower(int power)
    {
        this._stat.DefencePower = power;
    }

    public float GetSpeed()
    {
        return _stat.Speed;
    }

    public void SetSpeed(float speed)
    {
        this._stat.Speed = speed;
    }
}
