using Newtonsoft.Json.Linq;

namespace DSLink.Serializer
{
    /// <summary>
    /// Interface for serialization
    /// </summary>
    public abstract class BaseSerializer
    {
        /// <summary>
        /// Serialize the specified data.
        /// </summary>
        /// <param name="data">Data to serialize in serialization object form.</param>
        /// <returns>Serialized data</returns>
        public abstract dynamic Serialize(JObject data);

        /// <summary>
        /// Deserialize the specified data.
        /// </summary>
        /// <param name="data">Data in serialized form.</param>
        /// <returns>Deserialized data in serialization object form.</returns>
        public abstract JObject Deserialize(dynamic data);
    }
}
