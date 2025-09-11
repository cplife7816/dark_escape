using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Centralized save & load of checkpoints. Finds every SaveableEntity + ISaveable,
/// and stores per-component JSON entries.
/// </summary>
public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }
    public static event System.Action BeforeLoad;
    public static event System.Action AfterLoad;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---------- Data Container ----------
    [Serializable]
    public class Snapshot
    {
        public List<Entry> entries = new List<Entry>();

        [Serializable]
        public class Entry
        {
            public string entityId;     // SaveableEntity.Id
            public string componentPath; // path hint: ComponentTypeName#index
            public string json;         // component's own state (ISaveable.CaptureState())
        }
    }

    // ---------- Anchored Slots in Memory ----------
    public static class CheckpointStore
    {
        // key -> snapshot json
        public static Dictionary<string, string> Slots = new Dictionary<string, string>();
        public static void Save(string key, string json) => Slots[key] = json;
        public static bool TryGet(string key, out string json) => Slots.TryGetValue(key, out json);
        public static bool Has(string key) => Slots.ContainsKey(key);
    }

    // ---------- Public API ----------
    public void SaveCheckpoint() => SaveCheckpoint("_Last");

    public void SaveCheckpoint(string key)
    {
        var json = BuildSnapshotJson();
        CheckpointStore.Save(key, json);
        Debug.Log($"[SaveSystem] Saved checkpoint '{key}'. bytes={json.Length}");
    }

    public void LoadCheckpoint() => LoadCheckpoint("_Last");

    public void LoadCheckpoint(string key = "_Last")
    {
        if (!CheckpointStore.TryGet(key, out var json) || string.IsNullOrEmpty(json))
        {
            Debug.LogWarning($"[SAVE] LoadCheckpoint('{key}') → NOT FOUND");
            return;
        }

        Debug.Log($"[SAVE1] LoadCheckpoint('{key}') START (jsonLen={json.Length})");
        try { Debug.Log("[SAVE1] → BeforeLoad.Invoke()"); BeforeLoad?.Invoke(); }
        catch (System.Exception ex) { Debug.LogError($"[SAVE] BeforeLoad ERROR: {ex}"); }

        ApplySnapshotJson(json); // 내부에 Applied/Missed/Total 요약 로그가 있다면 그대로 둠

        try { Debug.Log("[SAVE1] → AfterLoad.Invoke()"); AfterLoad?.Invoke(); }
        catch (System.Exception ex) { Debug.LogError($"[SAVE] AfterLoad ERROR: {ex}"); }

        Debug.Log($"[SAVE1] LoadCheckpoint('{key}') END");
    }

    // ---------- Core Build/Apply ----------
    public string BuildSnapshotJson()
    {
        var snapshot = new Snapshot();
        // Find all ISaveable on objects that also have SaveableEntity
        var allBehaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
                                     .Where(b => b is ISaveable && b.GetComponent<SaveableEntity>() != null)
                                     .ToArray();

        foreach (var b in allBehaviours)
        {
            var entity = b.GetComponent<SaveableEntity>();
            var saveable = (ISaveable)b;

            // Derive a stable-ish path using component type and index on the entity
            var components = entity.GetComponents<MonoBehaviour>()
                                   .Where(c => c is ISaveable)
                                   .ToList();
            int index = components.IndexOf(b);
            string path = $"{b.GetType().Name}#{index}";

            string json = string.Empty;
            try { json = saveable.CaptureState(); }
            catch (Exception ex) { Debug.LogError($"[SaveSystem] Capture failed on {entity.name}/{path}: {ex}"); }

            snapshot.entries.Add(new Snapshot.Entry
            {
                entityId = entity.Id,
                componentPath = path,
                json = json ?? string.Empty
            });
        }

        return JsonUtility.ToJson(snapshot);
    }

    public void ApplySnapshotJson(string json)
    {
        var snapshot = JsonUtility.FromJson<Snapshot>(json);
        if (snapshot == null || snapshot.entries == null) return;

        // Build lookup of entityId -> (list of ISaveable on it)
        var allEntities = Resources.FindObjectsOfTypeAll<SaveableEntity>();
        var entityLookup = allEntities.ToDictionary(e => e.Id, e => e);

        int applied = 0, missed = 0;

        foreach (var e in snapshot.entries)
        {
            if (!entityLookup.TryGetValue(e.entityId, out var entity))
            {
                missed++;
                continue;
            }

            // Match by type name and index among ISaveable components on that entity
            var parts = e.componentPath.Split('#');
            string typeName = parts.Length > 0 ? parts[0] : "";
            int index = (parts.Length > 1 && int.TryParse(parts[1], out var i)) ? i : -1;

            var comps = entity.GetComponents<MonoBehaviour>().Where(c => c is ISaveable).ToList();
            var target = comps.Where(c => c.GetType().Name == typeName).Skip(index >= 0 ? index : 0).FirstOrDefault();

            if (target is ISaveable saveable)
            {
                try { saveable.RestoreState(e.json); applied++; }
                catch (Exception ex) { Debug.LogError($"[SaveSystem] Restore failed on {entity.name}/{e.componentPath}: {ex}"); }
            }
            else
            {
                missed++;
            }
        }

        Debug.Log($"[SaveSystem] Apply finished. Applied: {applied}, Missed: {missed}, Total: {snapshot.entries.Count}");
    }
}