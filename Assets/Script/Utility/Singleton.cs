using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// 1. 씬이 변경이 되더라도 인스턴스가 파괴되지 않고, 유지를 시켜주는 싱글톤. 
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Inst { get; private set; }

    protected virtual void Awake()
    {
        if (Inst == null)
        {
            Inst = this as T;
            DontDestroyOnLoad(gameObject); // 자신의 객체를 파괴되지 않도록 설정. 
        }
        else
        {
            Destroy(gameObject);
        }
        DoAwake();
    }

    protected virtual void DoAwake() { } // 파생클래스에서 자신의 초기화에 필요한 로직
}

// 2. 씬이 변경이 되면, 인스턴스가 파괴가 되는 싱글톤. 
public class SingletonDestroy<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Inst { get; private set; }


    protected virtual void Awake()
    {
        if (Inst == null)
        {
            Inst = this as T;
        }
        else
        {
            Destroy(gameObject);

        }
        DoAwake();
    }
    protected virtual void DoAwake() { }
}