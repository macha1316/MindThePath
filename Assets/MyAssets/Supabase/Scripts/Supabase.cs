using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(VideoPlayer))]
public class Supabase : MonoBehaviour
{
    [Header("Supabase Credentials")]
    [Tooltip("e.g. https://xxxx.supabase.co")]
    [SerializeField] private string projectUrl;
    [Tooltip("Supabase anon/public API key (DO NOT use service key)")]
    [SerializeField] private string apiKey;

    [Header("Storage Object")]
    [Tooltip("Bucket name in Supabase Storage")]
    [SerializeField] private string bucket;
    [Tooltip("File path inside the bucket, e.g. media/audio/sample.mp3")]
    [SerializeField] private bool isPublicBucket = true;
    [Tooltip("Optional: If set, this URL is used directly (headers still applied).")]
    [SerializeField] private string directFileUrl = "";

    [Header("Playback")]
    [SerializeField] private bool loop = false;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;
    [Tooltip("Prefer download to file over direct streaming (useful for private buckets)")]
    [SerializeField] private bool preferDownloadForPlayback = false;

    [Header("Stage-based Naming (optional)")]
    [Tooltip("If true, builds object path like <folder>/stage<index+1>.mov using the current StageBuilder stage")]
    [SerializeField] private bool useStageBasedNaming = false;
    [Tooltip("Optional folder inside bucket, e.g. 'movies'")]
    [SerializeField] private string stageFolder = "";
    [Tooltip("Filename prefix before the number")]
    [SerializeField] private string stageFilePrefix = "stage";
    [Tooltip("Filename extension including dot, e.g. '.mov' or '.mp4'")]
    [SerializeField] private string stageFileExtension = ".mov";
    [Tooltip("When true, uses 1-based numbers (stage1, stage2). When false, uses 0-based (stage0, stage1)")]
    [SerializeField] private bool oneBasedIndex = true;

    [Header("Video Output (optional)")]
    [SerializeField] private VideoRenderMode videoRenderMode = VideoRenderMode.RenderTexture;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private RenderTexture targetTexture;
    [SerializeField] private VideoAspectRatio aspectRatio = VideoAspectRatio.FitInside;

    private AudioSource audioSource;
    private VideoPlayer videoPlayer;
    private UnityWebRequest currentRequest;
    private string lastTempVideoPath;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = loop;
        audioSource.volume = volume;

        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = loop;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, audioSource);
        ApplyVideoOutput();
    }

    // MP4/MOV 再生
    public void PlayVideo()
    {
        StopAudio();
        StartCoroutine(PlayVideoFromSupabase());
    }

    public void PlayAudio()
    {
        if (currentRequest != null)
        {
            // Cancel any ongoing request
            currentRequest.Abort();
            currentRequest = null;
        }
        StartCoroutine(PlayAudioFromSupabase());
    }

    public void StopAudio()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    public void StopVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }
        // cleanup temp file if any
        if (!string.IsNullOrEmpty(lastTempVideoPath) && File.Exists(lastTempVideoPath))
        {
            try { File.Delete(lastTempVideoPath); } catch { /* ignore */ }
            lastTempVideoPath = null;
        }
    }

    private IEnumerator PlayAudioFromSupabase()
    {
        string pathOverride = ResolveStagePathOrObjectPath();
        string url = BuildFileUrlWithPath(pathOverride);
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogError("Supabase: URL is empty. Check settings.");
            yield break;
        }

        using (var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            currentRequest = req;
            // Headers for Supabase auth
            if (!string.IsNullOrEmpty(apiKey))
            {
                req.SetRequestHeader("apikey", apiKey);
                req.SetRequestHeader("Authorization", "Bearer " + apiKey);
            }
            req.SetRequestHeader("Accept", "audio/mpeg");

            yield return req.SendWebRequest();

            currentRequest = null;

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Supabase: Failed to fetch audio. {req.error}\nURL: {url}");
                yield break;
            }

            var clip = DownloadHandlerAudioClip.GetContent(req);
            if (clip == null)
            {
                Debug.LogError("Supabase: AudioClip decode failed.");
                yield break;
            }

            audioSource.clip = clip;
            audioSource.loop = loop;
            audioSource.volume = volume;
            audioSource.Play();
        }
    }

    private string BuildFileUrlWithPath(string path)
    {
        if (!string.IsNullOrEmpty(directFileUrl)) return directFileUrl;
        // Allow full URL in path as well
        if (!string.IsNullOrEmpty(path) && (path.StartsWith("http://") || path.StartsWith("https://")))
        {
            return path;
        }
        if (string.IsNullOrEmpty(projectUrl) || string.IsNullOrEmpty(bucket) || string.IsNullOrEmpty(path))
        {
            return null;
        }

        string baseUrl = SanitizeProjectUrl(projectUrl);
        string prefix = isPublicBucket ? "/storage/v1/object/public/" : "/storage/v1/object/";
        string trimmedBase = baseUrl.TrimEnd('/');
        string trimmedPath = path.TrimStart('/');
        return $"{trimmedBase}{prefix}{bucket}/{trimmedPath}";
    }

    private string ResolveStagePathOrObjectPath()
    {
        int index = 0;
        var sb = StageBuilder.Instance;
        if (sb != null) index = sb.stageNumber;
        int display = oneBasedIndex ? index + 1 : index;
        string fileName = $"{stageFilePrefix}{display}{stageFileExtension}";
        if (!string.IsNullOrEmpty(stageFolder))
        {
            return $"{stageFolder.TrimEnd('/')}/{fileName}";
        }
        return fileName;
    }

    private string SanitizeProjectUrl(string url)
    {
        try
        {
            var u = new Uri(url);
            string host = u.Host;
            if (host.Contains(".storage.supabase."))
            {
                host = host.Replace(".storage.supabase.", ".supabase.");
            }
            var builder = new UriBuilder(u.Scheme, host);
            return builder.Uri.ToString().TrimEnd('/');
        }
        catch
        {
            return url;
        }
    }

    private IEnumerator PlayVideoFromSupabase()
    {
        string pathOverride = ResolveStagePathOrObjectPath();
        string url = BuildFileUrlWithPath(pathOverride);
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogError("Supabase: URL is empty. Check settings.");
            yield break;
        }

        bool canStream = isPublicBucket || (!string.IsNullOrEmpty(directFileUrl) && (directFileUrl.StartsWith("http://") || directFileUrl.StartsWith("https://")));
        bool shouldDownload = (!canStream) || preferDownloadForPlayback;

        if (shouldDownload)
        {
            string ext = Path.GetExtension(url);
            if (string.IsNullOrEmpty(ext)) ext = ".mp4";
            string localPath = Path.Combine(Application.temporaryCachePath, $"supabase_video_{Guid.NewGuid()}{ext}");
            using (var req = UnityWebRequest.Get(url))
            {
                currentRequest = req;
                if (!string.IsNullOrEmpty(apiKey))
                {
                    req.SetRequestHeader("apikey", apiKey);
                    req.SetRequestHeader("Authorization", "Bearer " + apiKey);
                }
                req.downloadHandler = new DownloadHandlerFile(localPath, true);
                yield return req.SendWebRequest();
                currentRequest = null;

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Supabase: Failed to fetch video. {req.error}\\nURL: {url}");
                    yield break;
                }

                lastTempVideoPath = localPath;
                yield return PrepareAndPlay("file://" + localPath);
            }
        }
        else
        {
            yield return PrepareAndPlay(url);
        }
    }

    private IEnumerator PrepareAndPlay(string videoUrl)
    {
        ApplyVideoOutput();
        videoPlayer.Stop();
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = videoUrl;
        videoPlayer.isLooping = loop;
        audioSource.volume = volume;

        bool isError = false;
        string errorMsg = null;
        VideoPlayer.EventHandler onPrepared = null;
        VideoPlayer.ErrorEventHandler onError = null;
        onPrepared = (vp) => { };
        onError = (vp, msg) => { isError = true; errorMsg = msg; };
        videoPlayer.errorReceived += onError;

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared && !isError)
        {
            yield return null;
        }
        videoPlayer.errorReceived -= onError;
        if (isError)
        {
            Debug.LogError($"Supabase: Video prepare failed: {errorMsg}\\nURL: {videoUrl}");
            yield break;
        }
        videoPlayer.Play();
    }

    private void ApplyVideoOutput()
    {
        if (videoPlayer == null) return;
        videoPlayer.renderMode = videoRenderMode;
        videoPlayer.aspectRatio = aspectRatio;
        switch (videoRenderMode)
        {
            case VideoRenderMode.CameraFarPlane:
            case VideoRenderMode.CameraNearPlane:
                if (targetCamera == null) targetCamera = Camera.main;
                videoPlayer.targetCamera = targetCamera;
                break;
            case VideoRenderMode.RenderTexture:
                videoPlayer.targetTexture = targetTexture;
                break;
            case VideoRenderMode.MaterialOverride:
                // Use inspector to wire renderer/material
                break;
        }
    }
}
