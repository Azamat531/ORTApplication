// ============================================
// File: Assets/Scripts/Infra/VideoPlaybackHelper.cs
// Purpose: КЭШ-ТОЛЬКО воспроизведение (без авто-скачивания/стриминга)
//  - PlayVideoFromCacheOnly: играет только если файл уже в кэше
//  - PlayVideoCached: совместимый старый режим (оставлен на месте)
// ============================================
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

public static class VideoPlaybackHelper
{
    private static string GuessExt(string url, string def)
    {
        try { var pure = url.Split('?')[0]; var ext = Path.GetExtension(pure); return string.IsNullOrEmpty(ext) ? def : ext; }
        catch { return def; }
    }

    private static string AsFileUrl(string localPath)
    {
        if (string.IsNullOrEmpty(localPath)) return localPath;
        return localPath.StartsWith("file://") ? localPath : ("file://" + localPath);
    }

    /// <summary>
    /// Играет ТОЛЬКО из локального кэша. Если файла нет — ничего не качает, не стримит.
    /// </summary>
    public static IEnumerator PlayVideoFromCacheOnly(VideoPlayer vp, string videoUrl, System.Action<string> onMissing = null)
    {
        if (vp == null || string.IsNullOrEmpty(videoUrl)) yield break;
        string ext = GuessExt(videoUrl, ".mp4");
        string cachedPath = CacheService.GetCachedPath("video:" + videoUrl, ext);
        if (string.IsNullOrEmpty(cachedPath))
        {
            onMissing?.Invoke("not_cached");
            yield break;
        }

        vp.skipOnDrop = true;
        vp.source = VideoSource.Url;
        vp.url = AsFileUrl(cachedPath);
        vp.Prepare();
        while (!vp.isPrepared) yield return null;
        vp.Play();
    }

    /// <summary>
    /// Старый режим: если файла нет — скачивает, иначе играет из кэша (оставлено для совместимости).
    /// </summary>
    public static IEnumerator PlayVideoCached(VideoPlayer vp, string videoUrl)
    {
        if (vp == null || string.IsNullOrEmpty(videoUrl)) yield break;
        string ext = GuessExt(videoUrl, ".mp4");
        string finalPath = CacheService.GetCachedPath("video:" + videoUrl, ext);
        if (string.IsNullOrEmpty(finalPath))
        {
            bool ok = false;
            yield return CacheService.GetFile(
                videoUrl, "video:" + videoUrl,
                path => { ok = true; finalPath = path; },
                forcedExt: ext,
                onError: _ => { ok = false; finalPath = null; }
            );
            if (!ok) finalPath = null;
        }

        vp.skipOnDrop = true;
        vp.source = VideoSource.Url;
        vp.url = string.IsNullOrEmpty(finalPath) ? videoUrl : AsFileUrl(finalPath);
        vp.Prepare();
        while (!vp.isPrepared) yield return null;
        vp.Play();
    }
}
