using System;
using System.Security;
using Newtonsoft.Json;

namespace IronFoundry.Warden.Shared.Messaging
{
    public class SecureStringJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SecureString);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
                return null;

            var value = reader.Value.ToString();
            return value.ToSecureString();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else
            {
                SecureString secureValue = (SecureString)value;
                writer.WriteValue(secureValue.ToUnsecureString());
            }
        }
    }
}
