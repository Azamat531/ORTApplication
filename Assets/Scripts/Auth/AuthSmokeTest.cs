//using UnityEngine;

//public class AuthSmokeTest : MonoBehaviour
//{
//    private async void Start()
//    {
//        string uname = "test" + Random.Range(10000, 99999);
//        var reg = await AuthManager.Instance.Register(uname, "Test User", "secret123");
//        Debug.Log($"REGISTER uname={uname} ok={reg.ok} err={reg.err}");
//        var si = await AuthManager.Instance.SignIn(uname, "secret123");
//        Debug.Log($"SIGNIN ok={si.ok} err={si.err}");
//    }
//}

// ===============================
// AuthSmokeTest.cs — editor-only now
// ===============================
#if UNITY_EDITOR
using UnityEngine;

public class AuthSmokeTest : MonoBehaviour
{
    private async void Start()
    {
        string uname = "test" + Random.Range(10000, 99999);
        var reg = await AuthManager.Instance.Register(uname, "Test User", "secret123");
        Debug.Log($"REGISTER uname={uname} ok={reg.ok} err={reg.err}");

        var si = await AuthManager.Instance.SignIn(uname, "secret123");
        Debug.Log($"SIGNIN ok={si.ok} err={si.err}");
    }
}
#endif
