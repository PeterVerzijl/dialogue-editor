using UnityEngine;
using Newtonsoft.Json;
using System;

namespace DialogueEditor {
    public class Vector2Serializer : JsonConverter<Vector2> {
        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer) {
            if (reader.TokenType != JsonToken.StartObject) {
                throw new JsonException($"JsonTokenType was of type {reader.TokenType}, only objects are supported");
            }
            reader.Read(); // Start object
            reader.Read(); // First label
            float x = Convert.ToSingle(reader.Value);
            reader.Read(); // Second label
            reader.Read(); // Second label
            float y = Convert.ToSingle(reader.Value);
            reader.Read(); // End of object?
            return new Vector2(x, y);
        }

        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer) {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WriteEndObject();
        }
    }
}
