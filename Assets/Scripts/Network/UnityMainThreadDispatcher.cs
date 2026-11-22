using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> executionQueue = new Queue<Action>();
    private static UnityMainThreadDispatcher instance;

    public static bool Exists => instance != null;

    public static UnityMainThreadDispatcher Instance()
    {
        if (instance == null)
        {
            throw new InvalidOperationException("메인 스레드 전달기가 준비되지 않았습니다.");
        }

        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
    }

    private void Update()
    {
        for (int i = 0; i < 512; ++i)
        {
            Action action;

            lock (executionQueue)
            {
                if (executionQueue.Count == 0)
                {
                    break;
                }

                action = executionQueue.Dequeue();
            }

            action();
        }
    }

    public void Enqueue(Action action)
    {
        EnqueueWork(action);
    }

    public static void EnqueueWork(Action action)
    {
        if (action == null)
        {
            return;
        }

        lock (executionQueue)
        {
            executionQueue.Enqueue(action);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;

            lock (executionQueue)
            {
                executionQueue.Clear();
            }
        }
    }
}
