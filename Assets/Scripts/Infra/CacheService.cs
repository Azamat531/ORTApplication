//using System;
//using System.IO;
//using System.Text;
//using System.Collections;
//using System.Collections.Generic;
//using System.Security.Cryptography;
//using UnityEngine;
//using UnityEngine.Networking;

//public static class CacheService
//{
//    private static readonly string CacheDir = Path.Combine(Application.persistentDataPath, "cache");
//    private static readonly string TempDir = Path.Combine(Application.persistentDataPath, "cache_tmp");

//    // ========= ТЕКСТ (JSON) С ПРОВЕРКОЙ ВЕРСИИ =========
//    public static IEnumerator GetText(string url, string cacheKey, Action<string> onDone, Action<string> onError = null)
//    {
//        EnsureDirs();
//        string encPath = CachePathEnc(cacheKey, ".json");

//        string cachedJson = null;

//        // 1) Если есть кэш → сразу отдаём
//        if (File.Exists(encPath))
//        {
//            try
//            {
//                string tmp = DecryptToTemp(encPath, ".json");
//                cachedJson = File.ReadAllText(tmp, Encoding.UTF8);
//                onDone?.Invoke(cachedJson);
//            }
//            catch (Exception e) { Debug.LogWarning("[CacheService] Ошибка чтения кэша: " + e.Message); }
//        }

//        // 2) Онлайн-проверка версии
//        using (var req = UnityWebRequest.Get(url))
//        {
//            yield return req.SendWebRequest();
//            if (!RequestSucceeded(req))
//            {
//                if (cachedJson == null) onError?.Invoke(req.error);
//                yield break;
//            }

//            string freshJson = req.downloadHandler.text;

//            bool needUpdate = false;
//            try
//            {
//                int cachedVer = ExtractVersion(cachedJson);
//                int freshVer = ExtractVersion(freshJson);
//                if (freshVer > cachedVer) needUpdate = true;
//            }
//            catch
//            {
//                // если нет поля version → сравниваем текст
//                if (cachedJson != freshJson) needUpdate = true;
//            }

//            if (needUpdate)
//            {
//                try
//                {
//                    var bytes = Encoding.UTF8.GetBytes(freshJson);
//                    File.WriteAllBytes(encPath, EncryptBytes(bytes));
//                    string tmp = DecryptToTemp(encPath, ".json");
//                    string updated = File.ReadAllText(tmp, Encoding.UTF8);
//                    onDone?.Invoke(updated);
//                    Debug.Log("[CacheService] Обновил JSON по версии: " + cacheKey);
//                }
//                catch (Exception e) { onError?.Invoke(e.Message); }
//            }
//        }
//    }

//    private static int ExtractVersion(string json)
//    {
//        if (string.IsNullOrEmpty(json)) return 0;
//        if (json.Contains("\"version\""))
//        {
//            int i = json.IndexOf("\"version\"");
//            if (i >= 0)
//            {
//                int c = json.IndexOf(':', i);
//                if (c > 0)
//                {
//                    string sub = json.Substring(c + 1);
//                    string num = "";
//                    foreach (char ch in sub)
//                    {
//                        if (char.IsDigit(ch)) num += ch;
//                        else if (num.Length > 0) break;
//                    }
//                    if (int.TryParse(num, out int v)) return v;
//                }
//            }
//        }
//        return 0;
//    }

//    // ========= КАРТИНКИ =========
//    public static IEnumerator GetTexture(string urlNoExt, string cacheKey, Action<Texture2D> onDone, Action<string> onError = null)
//        => GetTexture_Internal(urlNoExt, cacheKey, onDone, 0, onError);

//    public static IEnumerator GetTexture(string urlNoExt, string cacheKey, Action<Sprite> onDone, int maxSize, Action<string> onError = null)
//    {
//        Texture2D tex = null;
//        yield return GetTexture_Internal(urlNoExt, cacheKey, t => tex = t, maxSize, onError);
//        if (tex != null)
//        {
//            var sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
//            onDone?.Invoke(sp);
//        }
//    }

//    public static IEnumerator GetTextureToCache(string urlNoExt, string cacheKey, Action<bool> onDone, int maxSide = 0, Action<string> onError = null)
//    {
//        EnsureDirs();
//        string encPath = CachePathEnc(cacheKey, ".png");
//        if (File.Exists(encPath)) { onDone?.Invoke(true); yield break; }

//        foreach (var cand in TextureCandidates(urlNoExt))
//        {
//            using (var req = UnityWebRequestTexture.GetTexture(cand, false))
//            {
//                yield return req.SendWebRequest();
//                if (!RequestSucceeded(req)) continue;

//                try
//                {
//                    var tex = DownloadHandlerTexture.GetContent(req);
//                    if (maxSide > 0) tex = ResizeIfNeeded(tex, maxSide);

//                    var png = tex.EncodeToPNG();
//                    File.WriteAllBytes(encPath, EncryptBytes(png));
//                    onDone?.Invoke(true);
//                    yield break;
//                }
//                catch (Exception e)
//                {
//                    onError?.Invoke(e.Message);
//                    onDone?.Invoke(false);
//                    yield break;
//                }
//            }
//        }
//        onDone?.Invoke(false);
//        onError?.Invoke("Texture not found with tried extensions");
//    }

//    private static IEnumerator GetTexture_Internal(string urlNoExt, string cacheKey, Action<Texture2D> onDone, int maxSize, Action<string> onError)
//    {
//        EnsureDirs();
//        string encPath = CachePathEnc(cacheKey, ".png");

//        if (File.Exists(encPath))
//        {
//            try
//            {
//                string tmp = DecryptToTemp(encPath, ".png");
//                var bytes = File.ReadAllBytes(tmp);
//                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
//                tex.LoadImage(bytes, true);
//                if (maxSize > 0) tex = ResizeIfNeeded(tex, maxSize);
//                onDone?.Invoke(tex);
//            }
//            catch (Exception e) { onError?.Invoke(e.Message); }
//            yield break;
//        }

//        foreach (var cand in TextureCandidates(urlNoExt))
//        {
//            using (var req = UnityWebRequestTexture.GetTexture(cand, false))
//            {
//                yield return req.SendWebRequest();
//                if (!RequestSucceeded(req)) continue;

//                try
//                {
//                    var tex = DownloadHandlerTexture.GetContent(req);
//                    if (maxSize > 0) tex = ResizeIfNeeded(tex, maxSize);

//                    var png = tex.EncodeToPNG();
//                    File.WriteAllBytes(encPath, EncryptBytes(png));
//                    onDone?.Invoke(tex);
//                    yield break;
//                }
//                catch (Exception e) { onError?.Invoke(e.Message); yield break; }
//            }
//        }

//        onError?.Invoke("Texture not found with tried extensions");
//    }

//    // ========= ФАЙЛЫ (видео и т.п.) =========
//    public static IEnumerator GetFile(
//        string url,
//        string cacheKey,
//        Action<string> onDone,
//        string forcedExt = null,
//        Action<string> onError = null,
//        Action<float> onProgress = null
//    )
//    {
//        EnsureDirs();
//        string ext = forcedExt ?? GuessExt(url, ".bin");
//        string encPath = CachePathEnc(cacheKey, ext);

//        if (File.Exists(encPath))
//        {
//            string tmp = GetOrMakePlainTemp(encPath, ext);
//            onProgress?.Invoke(1f);
//            onDone?.Invoke(tmp);
//            yield break;
//        }

//        string tmpDownload = Path.Combine(TempDir, Guid.NewGuid().ToString("N") + NormalizeExt(ext));
//        using (var req = UnityWebRequest.Get(MaybeWithExt(url, ext)))
//        {
//            req.downloadHandler = new DownloadHandlerFile(tmpDownload) { removeFileOnAbort = true };
//            var op = req.SendWebRequest();
//            while (!op.isDone)
//            {
//                onProgress?.Invoke(Mathf.Clamp01(req.downloadProgress));
//                yield return null;
//            }
//            if (!RequestSucceeded(req))
//            {
//                try { if (File.Exists(tmpDownload)) File.Delete(tmpDownload); } catch { }
//                onError?.Invoke(req.error);
//                yield break;
//            }
//        }

//        try
//        {
//            byte[] data = File.ReadAllBytes(tmpDownload);
//            File.WriteAllBytes(encPath, EncryptBytes(data));
//            onProgress?.Invoke(1f);
//            onDone?.Invoke(tmpDownload);
//        }
//        catch (Exception e)
//        {
//            try { if (File.Exists(tmpDownload)) File.Delete(tmpDownload); } catch { }
//            onError?.Invoke(e.Message);
//        }
//    }

//    public static string GetCachedPath(string cacheKey, string ext)
//    {
//        string p = CachePathEnc(cacheKey, ext);
//        return File.Exists(p) ? p : null;
//    }

//    public static string GetOrMakePlainTemp(string encPath, string ext, bool overwriteIfExists = false)
//    {
//        EnsureDirs();
//        try
//        {
//            string name = Path.GetFileNameWithoutExtension(encPath);
//            string tmp = Path.Combine(TempDir, name + NormalizeExt(ext));
//            if (File.Exists(tmp) && !overwriteIfExists) return tmp;

//            byte[] enc = File.ReadAllBytes(encPath);
//            byte[] plain = DecryptBytes(enc);
//            if (File.Exists(tmp) && overwriteIfExists) File.Delete(tmp);
//            File.WriteAllBytes(tmp, plain);
//            return tmp;
//        }
//        catch { return null; }
//    }

//    public static int CleanupTemp(int maxAgeHours = 24)
//    {
//        try
//        {
//            EnsureDirs();
//            var now = DateTime.UtcNow;
//            int removed = 0;
//            var di = new DirectoryInfo(TempDir);
//            if (!di.Exists) return 0;
//            foreach (var fi in di.GetFiles())
//            {
//                var age = now - fi.LastWriteTimeUtc;
//                if (age.TotalHours > maxAgeHours) { try { fi.Delete(); removed++; } catch { } }
//            }
//            return removed;
//        }
//        catch { return 0; }
//    }

//    public static int ClearAllEncrypted()
//    {
//        try
//        {
//            EnsureDirs();
//            int removed = 0;
//            var di = new DirectoryInfo(CacheDir);
//            if (!di.Exists) return 0;
//            foreach (var fi in di.GetFiles())
//            {
//                try { fi.Delete(); removed++; } catch { }
//            }
//            return removed;
//        }
//        catch { return 0; }
//    }

//    public static bool DeleteCached(string cacheKey, string ext)
//    {
//        try
//        {
//            EnsureDirs();
//            string path = CachePathEnc(cacheKey, ext);
//            if (File.Exists(path)) { File.Delete(path); return true; }
//        }
//        catch { }
//        return false;
//    }

//    public static void LogPersistentPath()
//    {
//        Debug.Log("[CacheService] persistentDataPath = " + Application.persistentDataPath +
//                  "\ncache     = " + CacheDir +
//                  "\ncache_tmp = " + TempDir);
//    }

//    private static void EnsureDirs()
//    {
//        try { if (!Directory.Exists(CacheDir)) Directory.CreateDirectory(CacheDir); } catch { }
//        try { if (!Directory.Exists(TempDir)) Directory.CreateDirectory(TempDir); } catch { }
//    }

//    private static string CachePathEnc(string cacheKey, string ext)
//    {
//        string md5 = MD5Hex(cacheKey);
//        return Path.Combine(CacheDir, md5 + NormalizeExt(ext) + ".enc");
//    }

//    private static string NormalizeExt(string ext) => string.IsNullOrEmpty(ext) ? "" : (ext.StartsWith(".") ? ext : "." + ext);

//    private static string GuessExt(string url, string fallback)
//    {
//        try
//        {
//            int q = url.IndexOf('?');
//            string baseUrl = (q >= 0) ? url.Substring(0, q) : url;
//            string e = Path.GetExtension(baseUrl);
//            return string.IsNullOrEmpty(e) ? fallback : e;
//        }
//        catch { return fallback; }
//    }

//    private static string MaybeWithExt(string url, string ext)
//    {
//        if (url.StartsWith("file:")) return url;
//        int q = url.IndexOf('?');
//        string baseUrl = (q >= 0) ? url.Substring(0, q) : url;
//        string query = (q >= 0) ? url.Substring(q) : "";
//        string e = Path.GetExtension(baseUrl);
//        if (string.IsNullOrEmpty(e)) return baseUrl + NormalizeExt(ext) + query;
//        return url;
//    }

//    private static IEnumerable<string> TextureCandidates(string urlNoExt)
//    {
//        string[] exts = { "", ".png", ".jpg", ".jpeg", ".webp" };
//        foreach (var e in exts)
//        {
//            int q = urlNoExt.IndexOf('?');
//            string baseUrl = (q >= 0) ? urlNoExt.Substring(0, q) : urlNoExt;
//            string query = (q >= 0) ? urlNoExt.Substring(q) : "";
//            yield return string.IsNullOrEmpty(e) ? urlNoExt : (baseUrl + e + query);
//        }
//    }

//    private static Texture2D ResizeIfNeeded(Texture2D src, int maxSide)
//    {
//        if (src == null || maxSide <= 0) return src;
//        int w = src.width, h = src.height;
//        int m = Mathf.Max(w, h);
//        if (m <= maxSide) return src;

//        float k = (float)maxSide / m;
//        int nw = Mathf.Max(1, Mathf.RoundToInt(w * k));
//        int nh = Mathf.Max(1, Mathf.RoundToInt(h * k));

//        var rt = RenderTexture.GetTemporary(nw, nh, 0, RenderTextureFormat.ARGB32);
//        Graphics.Blit(src, rt);
//        var prev = RenderTexture.active;
//        RenderTexture.active = rt;

//        var dst = new Texture2D(nw, nh, TextureFormat.RGBA32, false);
//        dst.ReadPixels(new Rect(0, 0, nw, nh), 0, 0);
//        dst.Apply(false, true);

//        RenderTexture.active = prev;
//        RenderTexture.ReleaseTemporary(rt);

//        UnityEngine.Object.Destroy(src);
//        return dst;
//    }

//    private static string MD5Hex(string s)
//    {
//        using var md5 = MD5.Create();
//        var b = Encoding.UTF8.GetBytes(s);
//        var h = md5.ComputeHash(b);
//        var sb = new StringBuilder(h.Length * 2);
//        foreach (var x in h) sb.Append(x.ToString("x2"));
//        return sb.ToString();
//    }

//    private static readonly byte[] Key = new byte[16] { 0x23, 0x55, 0xA1, 0x0F, 0x9C, 0xDE, 0x01, 0x77, 0x33, 0x10, 0xB2, 0x5A, 0xC8, 0x19, 0x4D, 0xEE };
//    private static readonly byte[] IV = new byte[16] { 0x10, 0x22, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90, 0xA0, 0xB0, 0xC0, 0xD0, 0xE0, 0xF0, 0x00 };

//    private static byte[] EncryptBytes(byte[] plain)
//    {
//        using var aes = Aes.Create();
//        aes.Key = Key; aes.IV = IV; aes.Padding = PaddingMode.PKCS7; aes.Mode = CipherMode.CBC;
//        using var ms = new MemoryStream();
//        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
//            cs.Write(plain, 0, plain.Length);
//        return ms.ToArray();
//    }

//    private static byte[] DecryptBytes(byte[] enc)
//    {
//        using var aes = Aes.Create();
//        aes.Key = Key; aes.IV = IV; aes.Padding = PaddingMode.PKCS7; aes.Mode = CipherMode.CBC;
//        using var ms = new MemoryStream(enc);
//        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
//        using var outMs = new MemoryStream();
//        cs.CopyTo(outMs);
//        return outMs.ToArray();
//    }

//    private static string DecryptToTemp(string encPath, string ext)
//    {
//        string name = Path.GetFileNameWithoutExtension(encPath);
//        string tmp = Path.Combine(TempDir, name + NormalizeExt(ext));
//        try
//        {
//            if (File.Exists(tmp)) return tmp;
//            var enc = File.ReadAllBytes(encPath);
//            var plain = DecryptBytes(enc);
//            File.WriteAllBytes(tmp, plain);
//            return tmp;
//        }
//        catch { return null; }
//    }

//    private static bool RequestSucceeded(UnityWebRequest req)
//    {
//#if UNITY_2020_2_OR_NEWER
//        return req.result == UnityWebRequest.Result.Success;
//#else
//        return !req.isNetworkError && !req.isHttpError;
//#endif
//    }
//}


using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;

public static class CacheService
{
    private static readonly string CacheDir = Path.Combine(Application.persistentDataPath, "cache");
    private static readonly string TempDir = Path.Combine(Application.persistentDataPath, "cache_tmp");

    // ========= ТЕКСТ (JSON) С ПРОВЕРКОЙ ВЕРСИИ =========
    public static IEnumerator GetText(string url, string cacheKey, Action<string> onDone, Action<string> onError = null)
    {
        EnsureDirs();
        string encPath = CachePathEnc(cacheKey, ".json");

        string cachedJson = null;

        // 1) Если есть кэш → сразу отдаём
        if (File.Exists(encPath))
        {
            try
            {
                string tmp = DecryptToTemp(encPath, ".json");
                cachedJson = File.ReadAllText(tmp, Encoding.UTF8);
                onDone?.Invoke(cachedJson);
            }
            catch (Exception e) { Debug.LogWarning("[CacheService] Ошибка чтения кэша: " + e.Message); }
        }

        // 2) Онлайн-проверка версии
        using (var req = UnityWebRequest.Get(url + (url.Contains("?") ? "&" : "?") + "ts=" + DateTimeOffset.UtcNow.ToUnixTimeSeconds()))
        {
            req.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
            req.SetRequestHeader("Pragma", "no-cache");
            req.SetRequestHeader("Expires", "0");

            yield return req.SendWebRequest();
            if (!RequestSucceeded(req))
            {
                if (cachedJson == null) onError?.Invoke(req.error);
                yield break;
            }

            string freshJson = req.downloadHandler.text;

            bool needUpdate = false;
            try
            {
                int cachedVer = ExtractVersion(cachedJson);
                int freshVer = ExtractVersion(freshJson);
                if (freshVer > cachedVer) needUpdate = true;
            }
            catch
            {
                // если нет поля version → сравниваем текст
                if (cachedJson != freshJson) needUpdate = true;
            }

            if (needUpdate)
            {
                try
                {
                    var bytes = Encoding.UTF8.GetBytes(freshJson);
                    File.WriteAllBytes(encPath, EncryptBytes(bytes));
                    string tmp = DecryptToTemp(encPath, ".json");
                    string updated = File.ReadAllText(tmp, Encoding.UTF8);
                    onDone?.Invoke(updated);
                    Debug.Log("[CacheService] Обновил JSON по версии: " + cacheKey);
                }
                catch (Exception e) { onError?.Invoke(e.Message); }
            }
        }
    }

    private static int ExtractVersion(string json)
    {
        if (string.IsNullOrEmpty(json)) return 0;
        if (json.Contains("\"version\""))
        {
            int i = json.IndexOf("\"version\"");
            if (i >= 0)
            {
                int c = json.IndexOf(':', i);
                if (c > 0)
                {
                    string sub = json.Substring(c + 1);
                    string num = "";
                    foreach (char ch in sub)
                    {
                        if (char.IsDigit(ch)) num += ch;
                        else if (num.Length > 0) break;
                    }
                    if (int.TryParse(num, out int v)) return v;
                }
            }
        }
        return 0;
    }

    // ========= КАРТИНКИ =========
    public static IEnumerator GetTexture(string urlNoExt, string cacheKey, Action<Texture2D> onDone, Action<string> onError = null)
        => GetTexture_Internal(urlNoExt, cacheKey, onDone, 0, onError);

    public static IEnumerator GetTexture(string urlNoExt, string cacheKey, Action<Sprite> onDone, int maxSize, Action<string> onError = null)
    {
        Texture2D tex = null;
        yield return GetTexture_Internal(urlNoExt, cacheKey, t => tex = t, maxSize, onError);
        if (tex != null)
        {
            var sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            onDone?.Invoke(sp);
        }
    }

    public static IEnumerator GetTextureToCache(string urlNoExt, string cacheKey, Action<bool> onDone, int maxSide = 0, Action<string> onError = null)
    {
        EnsureDirs();
        string encPath = CachePathEnc(cacheKey, ".png");
        if (File.Exists(encPath)) { onDone?.Invoke(true); yield break; }

        foreach (var cand in TextureCandidates(urlNoExt))
        {
            using (var req = UnityWebRequestTexture.GetTexture(cand, false))
            {
                yield return req.SendWebRequest();
                if (!RequestSucceeded(req)) continue;

                try
                {
                    var tex = DownloadHandlerTexture.GetContent(req);
                    if (maxSide > 0) tex = ResizeIfNeeded(tex, maxSide);

                    var png = tex.EncodeToPNG();
                    File.WriteAllBytes(encPath, EncryptBytes(png));
                    onDone?.Invoke(true);
                    yield break;
                }
                catch (Exception e)
                {
                    onError?.Invoke(e.Message);
                    onDone?.Invoke(false);
                    yield break;
                }
            }
        }
        onDone?.Invoke(false);
        onError?.Invoke("Texture not found with tried extensions");
    }

    private static IEnumerator GetTexture_Internal(string urlNoExt, string cacheKey, Action<Texture2D> onDone, int maxSize, Action<string> onError)
    {
        EnsureDirs();
        string encPath = CachePathEnc(cacheKey, ".png");

        if (File.Exists(encPath))
        {
            try
            {
                string tmp = DecryptToTemp(encPath, ".png");
                var bytes = File.ReadAllBytes(tmp);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(bytes, true);
                if (maxSize > 0) tex = ResizeIfNeeded(tex, maxSize);
                onDone?.Invoke(tex);
            }
            catch (Exception e) { onError?.Invoke(e.Message); }
            yield break;
        }

        foreach (var cand in TextureCandidates(urlNoExt))
        {
            using (var req = UnityWebRequestTexture.GetTexture(cand, false))
            {
                yield return req.SendWebRequest();
                if (!RequestSucceeded(req)) continue;

                try
                {
                    var tex = DownloadHandlerTexture.GetContent(req);
                    if (maxSize > 0) tex = ResizeIfNeeded(tex, maxSize);

                    var png = tex.EncodeToPNG();
                    File.WriteAllBytes(encPath, EncryptBytes(png));
                    onDone?.Invoke(tex);
                    yield break;
                }
                catch (Exception e) { onError?.Invoke(e.Message); yield break; }
            }
        }

        onError?.Invoke("Texture not found with tried extensions");
    }

    // ========= ФАЙЛЫ (видео и т.п.) =========
    public static IEnumerator GetFile(
        string url,
        string cacheKey,
        Action<string> onDone,
        string forcedExt = null,
        Action<string> onError = null,
        Action<float> onProgress = null
    )
    {
        EnsureDirs();
        string ext = forcedExt ?? GuessExt(url, ".bin");
        string encPath = CachePathEnc(cacheKey, ext);

        if (File.Exists(encPath))
        {
            string tmp = GetOrMakePlainTemp(encPath, ext);
            onProgress?.Invoke(1f);
            onDone?.Invoke(tmp);
            yield break;
        }

        string tmpDownload = Path.Combine(TempDir, Guid.NewGuid().ToString("N") + NormalizeExt(ext));
        using (var req = UnityWebRequest.Get(MaybeWithExt(url, ext)))
        {
            req.downloadHandler = new DownloadHandlerFile(tmpDownload) { removeFileOnAbort = true };
            var op = req.SendWebRequest();
            while (!op.isDone)
            {
                onProgress?.Invoke(Mathf.Clamp01(req.downloadProgress));
                yield return null;
            }
            if (!RequestSucceeded(req))
            {
                try { if (File.Exists(tmpDownload)) File.Delete(tmpDownload); } catch { }
                onError?.Invoke(req.error);
                yield break;
            }
        }

        try
        {
            byte[] data = File.ReadAllBytes(tmpDownload);
            File.WriteAllBytes(encPath, EncryptBytes(data));
            onProgress?.Invoke(1f);
            onDone?.Invoke(tmpDownload);
        }
        catch (Exception e)
        {
            try { if (File.Exists(tmpDownload)) File.Delete(tmpDownload); } catch { }
            onError?.Invoke(e.Message);
        }
    }

    public static string GetCachedPath(string cacheKey, string ext)
    {
        string p = CachePathEnc(cacheKey, ext);
        return File.Exists(p) ? p : null;
    }

    public static string GetOrMakePlainTemp(string encPath, string ext, bool overwriteIfExists = false)
    {
        EnsureDirs();
        try
        {
            string name = Path.GetFileNameWithoutExtension(encPath);
            string tmp = Path.Combine(TempDir, name + NormalizeExt(ext));
            if (File.Exists(tmp) && !overwriteIfExists) return tmp;

            byte[] enc = File.ReadAllBytes(encPath);
            byte[] plain = DecryptBytes(enc);
            if (File.Exists(tmp) && overwriteIfExists) File.Delete(tmp);
            File.WriteAllBytes(tmp, plain);
            return tmp;
        }
        catch { return null; }
    }

    public static int CleanupTemp(int maxAgeHours = 24)
    {
        try
        {
            EnsureDirs();
            var now = DateTime.UtcNow;
            int removed = 0;
            var di = new DirectoryInfo(TempDir);
            if (!di.Exists) return 0;
            foreach (var fi in di.GetFiles())
            {
                var age = now - fi.LastWriteTimeUtc;
                if (age.TotalHours > maxAgeHours) { try { fi.Delete(); removed++; } catch { } }
            }
            return removed;
        }
        catch { return 0; }
    }

    public static int ClearAllEncrypted()
    {
        try
        {
            EnsureDirs();
            int removed = 0;
            var di = new DirectoryInfo(CacheDir);
            if (!di.Exists) return 0;
            foreach (var fi in di.GetFiles())
            {
                try { fi.Delete(); removed++; } catch { }
            }
            return removed;
        }
        catch { return 0; }
    }

    public static bool DeleteCached(string cacheKey, string ext)
    {
        try
        {
            EnsureDirs();
            string path = CachePathEnc(cacheKey, ext);
            if (File.Exists(path)) { File.Delete(path); return true; }
        }
        catch { }
        return false;
    }

    public static void LogPersistentPath()
    {
        Debug.Log("[CacheService] persistentDataPath = " + Application.persistentDataPath +
                  "\ncache     = " + CacheDir +
                  "\ncache_tmp = " + TempDir);
    }

    private static void EnsureDirs()
    {
        try { if (!Directory.Exists(CacheDir)) Directory.CreateDirectory(CacheDir); } catch { }
        try { if (!Directory.Exists(TempDir)) Directory.CreateDirectory(TempDir); } catch { }
    }

    private static string CachePathEnc(string cacheKey, string ext)
    {
        string md5 = MD5Hex(cacheKey);
        return Path.Combine(CacheDir, md5 + NormalizeExt(ext) + ".enc");
    }

    private static string NormalizeExt(string ext) => string.IsNullOrEmpty(ext) ? "" : (ext.StartsWith(".") ? ext : "." + ext);

    private static string GuessExt(string url, string fallback)
    {
        try
        {
            int q = url.IndexOf('?');
            string baseUrl = (q >= 0) ? url.Substring(0, q) : url;
            string e = Path.GetExtension(baseUrl);
            return string.IsNullOrEmpty(e) ? fallback : e;
        }
        catch { return fallback; }
    }

    private static string MaybeWithExt(string url, string ext)
    {
        if (url.StartsWith("file:")) return url;
        int q = url.IndexOf('?');
        string baseUrl = (q >= 0) ? url.Substring(0, q) : url;
        string query = (q >= 0) ? url.Substring(q) : "";
        string e = Path.GetExtension(baseUrl);
        if (string.IsNullOrEmpty(e)) return baseUrl + NormalizeExt(ext) + query;
        return url;
    }

    private static IEnumerable<string> TextureCandidates(string urlNoExt)
    {
        string[] exts = { "", ".png", ".jpg", ".jpeg", ".webp" };
        foreach (var e in exts)
        {
            int q = urlNoExt.IndexOf('?');
            string baseUrl = (q >= 0) ? urlNoExt.Substring(0, q) : urlNoExt;
            string query = (q >= 0) ? urlNoExt.Substring(q) : "";
            yield return string.IsNullOrEmpty(e) ? urlNoExt : (baseUrl + e + query);
        }
    }

    private static Texture2D ResizeIfNeeded(Texture2D src, int maxSide)
    {
        if (src == null || maxSide <= 0) return src;
        int w = src.width, h = src.height;
        int m = Mathf.Max(w, h);
        if (m <= maxSide) return src;

        float k = (float)maxSide / m;
        int nw = Mathf.Max(1, Mathf.RoundToInt(w * k));
        int nh = Mathf.Max(1, Mathf.RoundToInt(h * k));

        var rt = RenderTexture.GetTemporary(nw, nh, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(src, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;

        var dst = new Texture2D(nw, nh, TextureFormat.RGBA32, false);
        dst.ReadPixels(new Rect(0, 0, nw, nh), 0, 0);
        dst.Apply(false, true);

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        UnityEngine.Object.Destroy(src);
        return dst;
    }

    private static string MD5Hex(string s)
    {
        using var md5 = MD5.Create();
        var b = Encoding.UTF8.GetBytes(s);
        var h = md5.ComputeHash(b);
        var sb = new StringBuilder(h.Length * 2);
        foreach (var x in h) sb.Append(x.ToString("x2"));
        return sb.ToString();
    }

    private static readonly byte[] Key = new byte[16] { 0x23, 0x55, 0xA1, 0x0F, 0x9C, 0xDE, 0x01, 0x77, 0x33, 0x10, 0xB2, 0x5A, 0xC8, 0x19, 0x4D, 0xEE };
    private static readonly byte[] IV = new byte[16] { 0x10, 0x22, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90, 0xA0, 0xB0, 0xC0, 0xD0, 0xE0, 0xF0, 0x00 };

    private static byte[] EncryptBytes(byte[] plain)
    {
        using var aes = Aes.Create();
        aes.Key = Key; aes.IV = IV; aes.Padding = PaddingMode.PKCS7; aes.Mode = CipherMode.CBC;
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            cs.Write(plain, 0, plain.Length);
        return ms.ToArray();
    }

    private static byte[] DecryptBytes(byte[] enc)
    {
        using var aes = Aes.Create();
        aes.Key = Key; aes.IV = IV; aes.Padding = PaddingMode.PKCS7; aes.Mode = CipherMode.CBC;
        using var ms = new MemoryStream(enc);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var outMs = new MemoryStream();
        cs.CopyTo(outMs);
        return outMs.ToArray();
    }

    private static string DecryptToTemp(string encPath, string ext)
    {
        string name = Path.GetFileNameWithoutExtension(encPath);
        string tmp = Path.Combine(TempDir, name + NormalizeExt(ext));
        try
        {
            if (File.Exists(tmp)) return tmp;
            var enc = File.ReadAllBytes(encPath);
            var plain = DecryptBytes(enc);
            File.WriteAllBytes(tmp, plain);
            return tmp;
        }
        catch { return null; }
    }

    private static bool RequestSucceeded(UnityWebRequest req)
    {
#if UNITY_2020_2_OR_NEWER
        return req.result == UnityWebRequest.Result.Success;
#else
        return !req.isNetworkError && !req.isHttpError;
#endif
    }
}
