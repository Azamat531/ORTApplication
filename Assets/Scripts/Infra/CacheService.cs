// ============================================
// File: Assets/Scripts/Infra/CacheService.cs
// Update (Aug 16, 2025): CACHE-FIRST для JSON/изображений
//   - Если файл есть в кэше: СНАЧАЛА возвращаем кэш немедленно,
//     затем, если есть интернет, тихо проверяем/обновляем кэш в фоне
//     (UI не перерисовываем в этот раз).
//   - Если файла нет: грузим из сети как раньше и кладём в кэш.
// Видео (GetFile) оставлено без изменений: если есть — используем кэш,
// если нет — качаем.
// ============================================
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;

public static class CacheService
{
    private static readonly string CacheRoot = Path.Combine(Application.persistentDataPath, "cache");

    // ---- TEXT (JSON) ----
    public static IEnumerator GetText(string url, string cacheKey, Action<string> onDone, Action<string> onError = null)
    {
        EnsureCacheDir();
        string path = CachePath(cacheKey, ".json");

        // 1) Если есть кэш — отдаём СРАЗУ
        if (File.Exists(path))
        {
            string cached = null;
            try { cached = File.ReadAllText(path, Encoding.UTF8); }
            catch { /* файл мог быть битым — тогда пойдём в сеть */ }

            if (!string.IsNullOrEmpty(cached))
            {
                onDone?.Invoke(cached);

                // 1a) Тихая проверка обновлений (если есть интернет)
                if (IsOnline())
                {
                    using (var req = UnityWebRequest.Get(url))
                    {
                        yield return req.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
                        if (req.result == UnityWebRequest.Result.Success)
#else
                        if (!req.isNetworkError && !req.isHttpError)
#endif
                        {
                            var fresh = req.downloadHandler.text;
                            if (!string.Equals(fresh, cached, StringComparison.Ordinal))
                            {
                                try { File.WriteAllText(path, fresh, Encoding.UTF8); } catch { }
                            }
                        }
                    }
                }
                yield break; // уже отдали данные потребителю
            }
        }

        // 2) Кэша нет (или не удалось прочитать) — стандартная загрузка
        using (var req2 = UnityWebRequest.Get(url))
        {
            yield return req2.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
            if (req2.result == UnityWebRequest.Result.Success)
#else
            if (!req2.isNetworkError && !req2.isHttpError)
#endif
            {
                string txt = req2.downloadHandler.text;
                try { File.WriteAllText(path, txt, Encoding.UTF8); } catch { }
                onDone?.Invoke(txt);
            }
            else
            {
                if (File.Exists(path))
                {
                    // попытка аварийно вернуть старый кэш (маловероятно, что сюда попадём)
                    try { onDone?.Invoke(File.ReadAllText(path, Encoding.UTF8)); }
                    catch { onError?.Invoke($"GetText error: {req2.error}"); }
                }
                else onError?.Invoke($"GetText error: {req2.error}");
            }
        }
    }

    // ---- IMAGE (Sprite) ----
    public static IEnumerator GetTexture(string url, string cacheKey, Action<Sprite> onDone, int maxSide = 2048, Action<string> onError = null)
    {
        EnsureCacheDir();
        string path = CachePath(cacheKey, ".png");

        // 1) Есть кэш ? отдадим СРАЗУ
        if (File.Exists(path))
        {
            var sp = LoadSprite(path);
            if (sp != null) onDone?.Invoke(sp);
            else onError?.Invoke("Cached image decode failed");

            // 1a) Тихая актуализация в фоне (если есть интернет)
            if (IsOnline())
            {
                using (var req = UnityWebRequestTexture.GetTexture(url, false)) // readable
                {
                    yield return req.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
                    if (req.result == UnityWebRequest.Result.Success)
#else
                    if (!req.isNetworkError && !req.isHttpError)
#endif
                    {
                        var tex = DownloadHandlerTexture.GetContent(req);
                        if (tex != null)
                        {
                            tex = ResizeIfNeeded(tex, maxSide);
                            try { File.WriteAllBytes(path, tex.EncodeToPNG()); } catch { }
                            UnityEngine.Object.Destroy(tex);
                        }
                    }
                }
            }
            yield break; // UI уже получил картинку
        }

        // 2) Кэша нет ? стандартная загрузка
        using (var req2 = UnityWebRequestTexture.GetTexture(url, false))
        {
            yield return req2.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
            if (req2.result == UnityWebRequest.Result.Success)
#else
            if (!req2.isNetworkError && !req2.isHttpError)
#endif
            {
                var tex = DownloadHandlerTexture.GetContent(req2);
                if (tex != null)
                {
                    tex = ResizeIfNeeded(tex, maxSide);
                    try { File.WriteAllBytes(path, tex.EncodeToPNG()); } catch { }
                    var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    onDone?.Invoke(sprite);
                }
                else
                {
                    if (File.Exists(path)) onDone?.Invoke(LoadSprite(path)); else onError?.Invoke("Texture decode failed");
                }
            }
            else
            {
                if (File.Exists(path)) onDone?.Invoke(LoadSprite(path)); else onError?.Invoke($"GetTexture error: {req2.error}");
            }
        }
    }

    // ---- FILE (video etc.) ----
    public static IEnumerator GetFile(string url, string cacheKey, Action<string> onDone, string forcedExt = null, Action<string> onError = null)
    {
        EnsureCacheDir();
        string ext = forcedExt ?? GuessExt(url, ".bin");
        string path = CachePath(cacheKey, ext);

        if (File.Exists(path)) { onDone?.Invoke(path); yield break; }

        using (var req = UnityWebRequest.Get(url))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            yield return req.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
            if (req.result == UnityWebRequest.Result.Success)
#else
            if (!req.isNetworkError && !req.isHttpError)
#endif
            {
                var bytes = req.downloadHandler.data;
                string tmp = path + "." + Guid.NewGuid().ToString("N") + ".part";
                try
                {
                    File.WriteAllBytes(tmp, bytes);
                    if (!File.Exists(path)) File.Move(tmp, path); else File.Delete(tmp);
                    onDone?.Invoke(path);
                }
                catch (Exception e)
                {
                    try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                    if (File.Exists(path)) onDone?.Invoke(path); else onError?.Invoke($"GetFile write error: {e.Message}");
                }
            }
            else
            {
                if (File.Exists(path)) onDone?.Invoke(path); else onError?.Invoke($"GetFile error: {req.error}");
            }
        }
    }

    public static bool HasCached(string cacheKey, string ext = null) => File.Exists(CachePath(cacheKey, ext ?? ".bin"));
    public static string GetCachedPath(string cacheKey, string ext = null) { var p = CachePath(cacheKey, ext ?? ".bin"); return File.Exists(p) ? p : null; }

    private static void EnsureCacheDir() { if (!Directory.Exists(CacheRoot)) Directory.CreateDirectory(CacheRoot); }
    private static bool IsOnline() => Application.internetReachability != NetworkReachability.NotReachable;

    private static string CachePath(string key, string ext)
    { if (!ext.StartsWith(".")) ext = "." + ext; string file = Hash(key) + ext.ToLowerInvariant(); return Path.Combine(CacheRoot, file); }

    private static string Hash(string s)
    { using (var sha1 = SHA1.Create()) { var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(s)); var sb = new StringBuilder(bytes.Length * 2); foreach (var b in bytes) sb.Append(b.ToString("x2")); return sb.ToString(); } }

    private static string GuessExt(string url, string def)
    { try { var pure = url.Split('?')[0]; var ext = Path.GetExtension(pure); return string.IsNullOrEmpty(ext) ? def : ext; } catch { return def; } }

    private static Sprite LoadSprite(string path)
    {
        try { var bytes = File.ReadAllBytes(path); var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false); if (!tex.LoadImage(bytes)) return null; return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f); } catch { return null; }
    }

    private static Texture2D ResizeIfNeeded(Texture2D src, int max)
    {
        int w = src.width, h = src.height; int m = Mathf.Max(w, h);
        if (m <= max) return src;
        float k = (float)max / m; int nw = Mathf.RoundToInt(w * k), nh = Mathf.RoundToInt(h * k);
        var rt = new RenderTexture(nw, nh, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(src, rt);
        var prev = RenderTexture.active; RenderTexture.active = rt;
        var tex = new Texture2D(nw, nh, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, nw, nh), 0, 0); tex.Apply();
        RenderTexture.active = prev; rt.Release();
        return tex;
    }
}
