using System;


public interface IObtainable
{
    event Action EvObtained;

    void StartRooting();

    void StopRooting();

    bool IsNullCoroutine();
}