//// ============================================
//// CacheService.cs — minimal patch over your ORIGINAL
//// Что добавлено/изменено:
//// 1) Картинки/видео: кандидаты формируются ДО query (расширение вставляется перед ?alt=media)
//// 2) Текст/картинки: кэш-ключи стабильные (исп. CachePath(hash+ext)) — как и было у тебя
//// 3) Sprite-совместимые перегрузки сохранены (GetTexture(...), в т.ч. с maxSize)
//// Других изменений API НЕТ.
//// ============================================
//using System;
//using System.IO;
//using System.Text;
//using System.Collections;
//using System.Security.Cryptography;
//using UnityEngine;
//using UnityEngine.Networking;

//public static class CacheService
//{
//    private static readonly string CacheDir = Path.Combine(Application.persistentDataPath, "cache");

//    public static IEnumerator GetText(string url, string cacheKey, System.Action<string> onDone, System.Action<string> onError = null)
//    {
//        EnsureCacheDir();
//        string path = CachePath(cacheKey, ".json");
//        if (File.Exists(path))
//        {
//            string cached = File.ReadAllText(path, Encoding.UTF8);
//            onDone?.Invoke(cached);
//#if !UNITY_EDITOR
//            if (Application.internetReachability == NetworkReachability.NotReachable) yield break;
//#endif
//            using (var req = UnityWebRequest.Get(url))
//            {
//                yield return req.SendWebRequest();
//#if UNITY_2020_2_OR_NEWER
//                if (req.result == UnityWebRequest.Result.Success)
//#else
//                if (!req.isNetworkError && !req.isHttpError)
//#endif
//                { var fresh = req.downloadHandler.text; if (!string.Equals(fresh, cached, StringComparison.Ordinal)) try { File.WriteAllText(path, fresh, Encoding.UTF8); } catch { } }
//            }
//            yield break;
//        }
//        using (var req = UnityWebRequest.Get(url))
//        {
//            yield return req.SendWebRequest();
//#if UNITY_2020_2_OR_NEWER
//            if (req.result != UnityWebRequest.Result.Success)
//#else
//            if (req.isNetworkError || req.isHttpError)
//#endif
//            { onError?.Invoke(req.error); yield break; }
//            try { File.WriteAllText(path, req.downloadHandler.text, Encoding.UTF8); } catch { }
//            onDone?.Invoke(req.downloadHandler.text);
//        }
//    }

//    public static IEnumerator GetTexture(string url, string cacheKey, System.Action<Sprite> onDone, System.Action<string> onError = null)
//        => GetTexture_Internal(url, cacheKey, onDone, 0, onError);

//    public static IEnumerator GetTexture(string url, string cacheKey, System.Action<Sprite> onDone, int maxSize, System.Action<string> onError = null)
//        => GetTexture_Internal(url, cacheKey, onDone, maxSize, onError);

//    public static IEnumerator GetTextureToCache(string url, string cacheKey, System.Action<Texture2D> onDone, System.Action<string> onError = null)
//    {
//        EnsureCacheDir();
//        string path = CachePath(cacheKey, ".png");
//        if (File.Exists(path))
//        { try { var t = new Texture2D(2, 2, TextureFormat.RGBA32, false); t.LoadImage(File.ReadAllBytes(path)); onDone?.Invoke(t); yield break; } catch { } }
//        foreach (var candidate in TextureCandidates(url))
//        {
//            using (var req = UnityWebRequestTexture.GetTexture(candidate))
//            {
//                yield return req.SendWebRequest();
//#if UNITY_2020_2_OR_NEWER
//                if (req.result != UnityWebRequest.Result.Success)
//#else
//                if (req.isNetworkError || req.isHttpError)
//#endif
//                { continue; }
//                try { var tex = DownloadHandlerTexture.GetContent(req); File.WriteAllBytes(path, tex.EncodeToPNG()); onDone?.Invoke(tex); yield break; } catch { }
//            }
//        }
//        onError?.Invoke("image not found");
//    }

//    private static IEnumerator GetTexture_Internal(string url, string cacheKey, System.Action<Sprite> onDone, int maxSize, System.Action<string> onError)
//    {
//        EnsureCacheDir();
//        string path = CachePath(cacheKey, ".png");
//        if (File.Exists(path))
//        { var sp = LoadSprite(path); if (sp != null) { onDone?.Invoke(sp); yield break; } }
//        foreach (var candidate in TextureCandidates(url))
//        {
//            using (var req = UnityWebRequestTexture.GetTexture(candidate))
//            {
//                yield return req.SendWebRequest();
//#if UNITY_2020_2_OR_NEWER
//                if (req.result != UnityWebRequest.Result.Success)
//#else
//                if (req.isNetworkError || req.isHttpError)
//#endif
//                { continue; }
//                try { var tex = DownloadHandlerTexture.GetContent(req); if (maxSize > 0) tex = ResizeIfNeeded(tex, maxSize); File.WriteAllBytes(path, tex.EncodeToPNG()); var sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f); onDone?.Invoke(sp); yield break; } catch { }
//            }
//        }
//        onError?.Invoke("image not found");
//    }

//    public static IEnumerator GetFile(string url, string cacheKey, System.Action<string> onDone, string forcedExt = null, System.Action<string> onError = null)
//    {
//        EnsureCacheDir();
//        string ext = forcedExt ?? GuessExt(url, ".bin");
//        string path = CachePath(cacheKey, ext);
//        if (File.Exists(path)) { onDone?.Invoke(path); yield break; }
//        foreach (var candidate in FileCandidates(url))
//        {
//            string temp = path + ".tmp";
//            using (var req = UnityWebRequest.Get(candidate))
//            {
//                req.downloadHandler = new DownloadHandlerFile(temp) { removeFileOnAbort = true }; yield return req.SendWebRequest();
//#if UNITY_2020_2_OR_NEWER
//                if (req.result != UnityWebRequest.Result.Success)
//#else
//                if (req.isNetworkError || req.isHttpError)
//#endif
//                { try { if (File.Exists(temp)) File.Delete(temp); } catch { } continue; }
//                try { if (File.Exists(path)) File.Delete(path); File.Move(temp, path); onDone?.Invoke(path); yield break; } catch { }
//            }
//        }
//        onError?.Invoke("file download failed");
//    }

//    public static string GetCachedPath(string cacheKey, string ext)
//    { EnsureCacheDir(); string path = CachePath(cacheKey, ext); return File.Exists(path) ? path : null; }

//    // ---- helpers ----
//    private static void EnsureCacheDir() { try { if (!Directory.Exists(CacheDir)) Directory.CreateDirectory(CacheDir); } catch { } }

//    private static string CachePath(string key, string ext)
//    { ext = GuessExt(ext, ".bin"); string hash = Hash((key ?? "").ToLowerInvariant()); return Path.Combine(CacheDir, hash + ext); }

//    private static string Hash(string s)
//    { using (var md5 = MD5.Create()) { var data = Encoding.UTF8.GetBytes(s); var hash = md5.ComputeHash(data); var sb = new StringBuilder(hash.Length * 2); foreach (var b in hash) sb.Append(b.ToString("x2")); return sb.ToString(); } }

//    private static string GuessExt(string urlOrExt, string fallback)
//    { if (string.IsNullOrEmpty(urlOrExt)) return fallback; int q = urlOrExt.IndexOf('?'); string baseUrl = q >= 0 ? urlOrExt.Substring(0, q) : urlOrExt; string ext = Path.GetExtension(baseUrl); if (string.IsNullOrEmpty(ext)) return fallback; if (!ext.StartsWith(".")) ext = "." + ext; return ext.ToLowerInvariant(); }

//    private static void SplitUrl(string url, out string baseUrl, out string query)
//    { int q = url.IndexOf('?'); if (q >= 0) { baseUrl = url.Substring(0, q); query = url.Substring(q); } else { baseUrl = url; query = string.Empty; } }

//    private static string[] TextureCandidates(string url)
//    { SplitUrl(url, out string baseUrl, out string query); string ext = GuessExt(baseUrl, null); if (!string.IsNullOrEmpty(ext)) return new[] { url }; return new[] { baseUrl + query, baseUrl + ".png" + query, baseUrl + ".jpg" + query, baseUrl + ".jpeg" + query, baseUrl + ".webp" + query }; }

//    private static string[] FileCandidates(string url)
//    { SplitUrl(url, out string baseUrl, out string query); string ext = GuessExt(baseUrl, null); if (!string.IsNullOrEmpty(ext)) return new[] { url }; return new[] { baseUrl + ".mp4" + query, baseUrl + query }; }

//    private static Sprite LoadSprite(string path)
//    { try { var bytes = File.ReadAllBytes(path); var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false); tex.LoadImage(bytes); return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f); } catch { return null; } }

//    private static Texture2D ResizeIfNeeded(Texture2D src, int max)
//    { int w = src.width, h = src.height; int m = Mathf.Max(w, h); if (max <= 0 || m <= max) return src; float k = (float)max / m; int nw = Mathf.RoundToInt(w * k), nh = Mathf.RoundToInt(h * k); var rt = new RenderTexture(nw, nh, 0, RenderTextureFormat.ARGB32); Graphics.Blit(src, rt); var prev = RenderTexture.active; RenderTexture.active = rt; var tex = new Texture2D(nw, nh, TextureFormat.RGBA32, false); tex.ReadPixels(new Rect(0, 0, nw, nh), 0, 0); tex.Apply(); RenderTexture.active = prev; rt.Release(); return tex; }
//}


using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;

public static class CacheService
{
    private static readonly string CacheDir = Path.Combine(Application.persistentDataPath, "cache");
    private static readonly string TempDir = Path.Combine(Application.persistentDataPath, "cache_tmp");

    // ---- Универсальная проверка успеха запроса (поддержка старых Unity) ----
    private static bool RequestSucceeded(UnityWebRequest req)
    {
#if UNITY_2020_2_OR_NEWER
        return req.result == UnityWebRequest.Result.Success;
#else
#pragma warning disable 0618
        return !req.isNetworkError && !req.isHttpError;
#pragma warning restore 0618
#endif
    }

    // ===================== TEXT (JSON) =====================
    public static IEnumerator GetText(string url, string cacheKey, Action<string> onDone, Action<string> onError = null)
    {
        EnsureDirs();
        string encPath = CachePathEnc(cacheKey, ".json");

        // 1) Из кэша
        if (File.Exists(encPath))
        {
            try
            {
                var plain = DecryptBytes(File.ReadAllBytes(encPath));
                onDone?.Invoke(Encoding.UTF8.GetString(plain));
            }
            catch { /* упадём в онлайн */ }

            // 1a) Фоновое обновление (если есть интернет)
            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                using (var req = UnityWebRequest.Get(url))
                {
                    yield return req.SendWebRequest();
                    if (RequestSucceeded(req))
                    {
                        byte[] data = Encoding.UTF8.GetBytes(req.downloadHandler.text);
                        try { File.WriteAllBytes(encPath, EncryptBytes(data)); } catch { }
                    }
                }
            }
            yield break;
        }

        // 2) Онлайн
        using (var req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();
            if (!RequestSucceeded(req))
            {
                onError?.Invoke(req.error);
                yield break;
            }

            byte[] data = Encoding.UTF8.GetBytes(req.downloadHandler.text);
            try { File.WriteAllBytes(encPath, EncryptBytes(data)); } catch { }
            onDone?.Invoke(req.downloadHandler.text);
        }
    }

    // ===================== IMAGES =====================
    // В Texture2D
    public static IEnumerator GetTexture(string url, string cacheKey, Action<Texture2D> onDone, Action<string> onError = null)
        => GetTexture_Internal(url, cacheKey, onDone, 0, onError);

    // В Sprite (есть maxSize)
    public static IEnumerator GetTexture(string url, string cacheKey, Action<Sprite> onDone, int maxSize, Action<string> onError = null)
    {
        Texture2D tex = null;
        yield return GetTexture_Internal(url, cacheKey, t => tex = t, maxSize, onError);
        if (tex != null)
        {
            var sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            onDone?.Invoke(sp);
        }
    }

    // Префетч без аллокации Texture2D/Sprite — просто кладём в кэш (перекодируем в PNG, опц. даунскейл)
    public static IEnumerator GetTextureToCache(string url, string cacheKey, Action<bool> onDone, int maxSide = 0, Action<string> onError = null)
    {
        EnsureDirs();
        string ext = ".png"; // унифицируем изображения как png в кэше
        string encPath = CachePathEnc(cacheKey, ext);

        if (File.Exists(encPath)) { onDone?.Invoke(true); yield break; }

        using (var req = UnityWebRequestTexture.GetTexture(MaybeWithExt(url, ext), false))
        {
            yield return req.SendWebRequest();
            if (!RequestSucceeded(req))
            {
                onError?.Invoke(req.error);
                onDone?.Invoke(false);
                yield break;
            }

            try
            {
                var tex = DownloadHandlerTexture.GetContent(req);
                if (maxSide > 0) tex = ResizeIfNeeded(tex, maxSide);
                var png = tex.EncodeToPNG();
                File.WriteAllBytes(encPath, EncryptBytes(png));
                onDone?.Invoke(true);
            }
            catch (Exception e)
            {
                onError?.Invoke(e.Message);
                onDone?.Invoke(false);
            }
        }
    }

    private static IEnumerator GetTexture_Internal(string url, string cacheKey, Action<Texture2D> onDone, int maxSize, Action<string> onError)
    {
        EnsureDirs();
        foreach (var cand in TextureCandidates(url))
        {
            string ext = GuessExt(cand, ".png");
            string encPath = CachePathEnc(cacheKey, ext);

            // Кэш
            if (File.Exists(encPath))
            {
                try
                {
                    string tmp = DecryptToTemp(encPath, ext);
                    var data = File.ReadAllBytes(tmp);
                    var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.LoadImage(data, false);
                    if (maxSize > 0) tex = ResizeIfNeeded(tex, maxSize);
                    onDone?.Invoke(tex);
                    yield break;
                }
                catch { /* попробуем онлайн */ }
            }

            // Онлайн
            using (var req = UnityWebRequestTexture.GetTexture(MaybeWithExt(cand, ext), false))
            {
                yield return req.SendWebRequest();
                if (RequestSucceeded(req))
                {
                    try
                    {
                        var tex = DownloadHandlerTexture.GetContent(req);
                        if (maxSize > 0) tex = ResizeIfNeeded(tex, maxSize);

                        var png = tex.EncodeToPNG();
                        File.WriteAllBytes(encPath, EncryptBytes(png));
                        onDone?.Invoke(tex);
                        yield break;
                    }
                    catch { }
                }
            }
        }
        onError?.Invoke("image not found");
    }

    // ===================== FILES (video/mp4 etc.) =====================
    public static IEnumerator GetFile(string url, string cacheKey, Action<string> onDone, string forcedExt = null, Action<string> onError = null)
    {
        EnsureDirs();
        string ext = forcedExt ?? GuessExt(url, ".bin");
        string encPath = CachePathEnc(cacheKey, ext);

        // Кэш ? отдаём временный plaintext
        if (File.Exists(encPath))
        {
            string tmp = DecryptToTemp(encPath, ext);
            onDone?.Invoke(tmp);
            yield break;
        }

        // Качаем ? кладём в кэш .enc ? отдаём временный plaintext
        string tmpDownload = Path.Combine(TempDir, Guid.NewGuid().ToString("N") + NormalizeExt(ext));
        using (var req = UnityWebRequest.Get(MaybeWithExt(url, ext)))
        {
            req.downloadHandler = new DownloadHandlerFile(tmpDownload) { removeFileOnAbort = true };
            yield return req.SendWebRequest();

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
        EnsureDirs();
        string encPath = CachePathEnc(cacheKey, ext);
        return File.Exists(encPath) ? encPath : null;
    }

    public static string DecryptToTemp(string encPath, string plainExt, bool overwrite = true)
    {
        EnsureDirs();
        try
        {
            byte[] enc = File.ReadAllBytes(encPath);
            byte[] plain = DecryptBytes(enc);
            string e = NormalizeExt(plainExt);
            string tmp = Path.Combine(TempDir, Guid.NewGuid().ToString("N") + e);
            if (File.Exists(tmp) && overwrite) File.Delete(tmp);
            File.WriteAllBytes(tmp, plain);
            return tmp;
        }
        catch { return null; }
    }

    // ===================== HELPERS =====================
    private static void EnsureDirs()
    {
        try { if (!Directory.Exists(CacheDir)) Directory.CreateDirectory(CacheDir); } catch { }
        try { if (!Directory.Exists(TempDir)) Directory.CreateDirectory(TempDir); } catch { }
    }

    private static string CachePathEnc(string key, string ext)
    {
        string hash = Hash((key ?? "").ToLowerInvariant());
        string e = NormalizeExt(ext);
        return Path.Combine(CacheDir, hash + e + ".enc");
    }

    private static string NormalizeExt(string ext)
    {
        if (string.IsNullOrEmpty(ext)) return ".bin";
        if (!ext.StartsWith(".")) ext = "." + ext;
        return ext.ToLowerInvariant();
    }

    private static string[] TextureCandidates(string url)
    {
        SplitUrl(url, out string baseUrl, out string query);
        string ext = Path.GetExtension(baseUrl);
        if (!string.IsNullOrEmpty(ext)) return new[] { url };
        return new[]
        {
            baseUrl + query,
            baseUrl + ".png"  + query,
            baseUrl + ".jpg"  + query,
            baseUrl + ".jpeg" + query,
            baseUrl + ".webp" + query
        };
    }

    private static string MaybeWithExt(string url, string ext)
    {
        SplitUrl(url, out string baseUrl, out string query);
        string e = Path.GetExtension(baseUrl);
        if (string.IsNullOrEmpty(e)) baseUrl += NormalizeExt(ext);
        return baseUrl + query;
    }

    private static void SplitUrl(string url, out string baseUrl, out string query)
    {
        int q = url.IndexOf('?');
        if (q >= 0) { baseUrl = url.Substring(0, q); query = url.Substring(q); }
        else { baseUrl = url; query = string.Empty; }
    }

    private static string GuessExt(string urlOrExt, string fallback)
    {
        if (string.IsNullOrEmpty(urlOrExt)) return fallback;
        int q = urlOrExt.IndexOf('?');
        string baseUrl = q >= 0 ? urlOrExt.Substring(0, q) : urlOrExt;
        string ext = Path.GetExtension(baseUrl);
        if (string.IsNullOrEmpty(ext)) return fallback;
        if (!ext.StartsWith(".")) ext = "." + ext;
        return ext.ToLowerInvariant();
    }

    private static string Hash(string s)
    {
        using (var md5 = MD5.Create())
        {
            var data = Encoding.UTF8.GetBytes(s);
            var hash = md5.ComputeHash(data);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    private static Texture2D ResizeIfNeeded(Texture2D src, int maxSize)
    {
        if (maxSize <= 0) return src;
        int w = src.width, h = src.height;
        int max = Mathf.Max(w, h);
        if (max <= maxSize) return src;

        float k = (float)maxSize / max;
        int nw = Mathf.Max(1, Mathf.RoundToInt(w * k));
        int nh = Mathf.Max(1, Mathf.RoundToInt(h * k));

        var rt = new RenderTexture(nw, nh, 0, RenderTextureFormat.ARGB32);
        var prev = RenderTexture.active;
        Graphics.Blit(src, rt);
        RenderTexture.active = rt;

        var tex = new Texture2D(nw, nh, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, nw, nh), 0, 0); tex.Apply();
        RenderTexture.active = prev; rt.Release();
        return tex;
    }

    // ===================== CRYPTO =====================
    private const string AppSecret = "OrtAppSecret_v1_ChangeMe"; // замени на свой секрет в релизе
    private static byte[] Key
    {
        get
        {
            try
            {
                string material = SystemInfo.deviceUniqueIdentifier + "|" + AppSecret;
                using (var sha = SHA256.Create())
                {
                    var b = Encoding.UTF8.GetBytes(material);
                    return sha.ComputeHash(b); // 32 байта
                }
            }
            catch
            {
                return Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef");
            }
        }
    }

    private static byte[] EncryptBytes(byte[] data)
    {
        using (var aes = Aes.Create())
        {
            aes.KeySize = 256; aes.BlockSize = 128; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7; aes.Key = Key;
            aes.GenerateIV();
            using (var enc = aes.CreateEncryptor())
            {
                var body = enc.TransformFinalBlock(data, 0, data.Length);
                byte[] res = new byte[16 + body.Length];
                Buffer.BlockCopy(aes.IV, 0, res, 0, 16);
                Buffer.BlockCopy(body, 0, res, 16, body.Length);
                return res;
            }
        }
    }

    private static byte[] DecryptBytes(byte[] data)
    {
        using (var aes = Aes.Create())
        {
            aes.KeySize = 256; aes.BlockSize = 128; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7; aes.Key = Key;
            byte[] iv = new byte[16];
            Buffer.BlockCopy(data, 0, iv, 0, 16);
            aes.IV = iv;
            using (var dec = aes.CreateDecryptor())
            {
                return dec.TransformFinalBlock(data, 16, data.Length - 16);
            }
        }
    }

    // ===== Реюз временного .mp4 и чистка tmp =====
    public static string GetOrMakePlainTemp(string encPath, string plainExt, int maxAgeHours = 168)
    {
        EnsureDirs();
        try
        {
            if (string.IsNullOrEmpty(encPath) || !File.Exists(encPath)) return null;

            string e = NormalizeExt(plainExt);
            string baseName = Path.GetFileNameWithoutExtension(encPath);
            if (string.IsNullOrEmpty(baseName)) baseName = Hash((encPath ?? "").ToLowerInvariant());
            string target = Path.Combine(TempDir, baseName + e);

            if (File.Exists(target))
            {
                var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(target);
                if (age.TotalHours < maxAgeHours) return target;
                try { File.Delete(target); } catch { }
            }

            string fresh = DecryptToTemp(encPath, e);
            if (string.IsNullOrEmpty(fresh) || !File.Exists(fresh)) return null;

            try
            {
                if (File.Exists(target)) return target;
                File.Move(fresh, target);
            }
            catch { return fresh; }

            return target;
        }
        catch { return null; }
    }

    public static void CleanupTemp(int maxAgeHours = 240)
    {
        EnsureDirs();
        try
        {
            var di = new DirectoryInfo(TempDir);
            if (!di.Exists) return;
            foreach (var fi in di.GetFiles())
            {
                var age = DateTime.UtcNow - fi.LastWriteTimeUtc;
                if (age.TotalHours > maxAgeHours)
                {
                    try { fi.Delete(); } catch { }
                }
            }
        }
        catch { }
    }
}
