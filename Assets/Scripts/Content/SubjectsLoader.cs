using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class SubjectsLoader : MonoBehaviour
{
    public string subjectsUrl; // сюда в инспекторе вставим прямую ссылку на subjects.json
    public SubjectsJson Data { get; private set; }

    public IEnumerator LoadSubjects()
    {
        using var req = UnityWebRequest.Get(subjectsUrl);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Data = JsonUtility.FromJson<SubjectsJson>(req.downloadHandler.text);
            Debug.Log("Загружено " + Data.subjects.Count + " предметов.");
        }
        else
        {
            Debug.LogError("Ошибка загрузки subjects.json: " + req.error);
        }
    }
}
