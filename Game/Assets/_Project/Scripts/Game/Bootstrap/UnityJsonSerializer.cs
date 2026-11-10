using Arsenal.Core.Save;
using UnityEngine;

namespace Arsenal.Game.Bootstrap
{
    /// <summary>Sérialisation JSON via JsonUtility (rapide, sans dépendance, compatible IL2CPP/iOS).</summary>
    public class UnityJsonSerializer : IJsonSerializer
    {
        private readonly bool _pretty;
        public UnityJsonSerializer(bool pretty = false) { _pretty = pretty; }
        public string ToJson(object value) => JsonUtility.ToJson(value, _pretty);
        public T FromJson<T>(string json) => JsonUtility.FromJson<T>(json);
    }
}
