using UnityEngine;

/// <summary>
/// TEMPLATE for your CassettePlayerController. Save insert/play/track/time.
/// Hook these methods to your controller's API.
/// </summary>
public class CassetteSave_Template : MonoBehaviour, ISaveable
{
    [System.Serializable] class Data { public bool inserted; public bool playing; public int track; public float time; }

    public string CaptureState()
    {
        return JsonUtility.ToJson(new Data
        {
            inserted = GetInserted(),
            playing = GetPlaying(),
            track = GetTrack(),
            time = GetTime()
        });
    }

    public void RestoreState(string json)
    {
        var d = JsonUtility.FromJson<Data>(json);
        SetInserted(d.inserted);
        SetTrack(d.track);
        Seek(d.time);
        if (d.playing) Play(); else Stop();
    }

    // ---- Adapt these to your CassettePlayerController ----
    private bool GetInserted() { /* TODO */ return false; }
    private bool GetPlaying() { /* TODO */ return false; }
    private int GetTrack() { /* TODO */ return 0; }
    private float GetTime() { /* TODO */ return 0f; }
    private void SetInserted(bool v) { /* TODO */ }
    private void SetTrack(int i) { /* TODO */ }
    private void Seek(float t) { /* TODO */ }
    private void Play() { /* TODO */ }
    private void Stop() { /* TODO */ }
}