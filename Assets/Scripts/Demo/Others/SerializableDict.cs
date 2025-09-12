// Step 1: Create this script - "SerializableDict.cs" 
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SerializableDict<TKey, TValue> : ISerializationCallbackReceiver
{
    [SerializeField] private List<TKey> keys = new List<TKey>();
    [SerializeField] private List<TValue> values = new List<TValue>();

    private Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();

        foreach (var kvp in dictionary)
        {
            keys.Add(kvp.Key);
            values.Add(kvp.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        dictionary = new Dictionary<TKey, TValue>();

        for (int i = 0; i < keys.Count && i < values.Count; i++)
        {
            dictionary[keys[i]] = values[i];
        }
    }

    // Dictionary interface
    public TValue this[TKey key]
    {
        get { return dictionary[key]; }
        set { dictionary[key] = value; }
    }

    public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);
    public bool TryGetValue(TKey key, out TValue value) => dictionary.TryGetValue(key, out value);
    public void Add(TKey key, TValue value) => dictionary.Add(key, value);
    public bool Remove(TKey key) => dictionary.Remove(key);
    public void Clear() => dictionary.Clear();
    public int Count => dictionary.Count;
    public Dictionary<TKey, TValue>.KeyCollection Keys => dictionary.Keys;
    public Dictionary<TKey, TValue>.ValueCollection Values => dictionary.Values;
    public Dictionary<TKey, TValue>.Enumerator GetEnumerator() => dictionary.GetEnumerator();

    // Implicit conversion
    public static implicit operator Dictionary<TKey, TValue>(SerializableDict<TKey, TValue> serializableDict)
    {
        return serializableDict.dictionary;
    }
}

// Specific types for common use cases
[System.Serializable]
public class StringTransformDict : SerializableDict<string, Transform> { }

[System.Serializable]
public class StringGameObjectDict : SerializableDict<string, GameObject> { }