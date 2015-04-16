using System;
using System.Security;
using Newtonsoft.Json;
using Xunit;

namespace IronFrame.Messaging
{
    public class SecureStringJsonConverterTest
    {
        [Fact]
        public void SerializesSecureString()
        {
            var obj = new TypeWithSecureString { Secret = "SECRET".ToSecureString() };

            var json = JsonConvert.SerializeObject(obj);

            Assert.Equal(@"{""Secret"":""SECRET""}", json);
        }

        [Fact]
        public void SerializesNullValue()
        {
            var obj = new TypeWithSecureString { Secret = null };

            var json = JsonConvert.SerializeObject(obj);

            Assert.Equal(@"{""Secret"":null}", json);
        }

        [Fact]
        public void DeserializesSecureString()
        {
            var json = @"{""Secret"":""SECRET""}";

            var obj = JsonConvert.DeserializeObject<TypeWithSecureString>(json);

            Assert.Equal("SECRET", obj.Secret.ToUnsecureString());
        }

        [Fact]
        public void DeserializesNullValue()
        {
            var json = @"{""Secret"":null}";

            var obj = JsonConvert.DeserializeObject<TypeWithSecureString>(json);

            Assert.Null(obj.Secret);
        }
    }

    public class TypeWithSecureString
    {
        [JsonConverter(typeof(SecureStringJsonConverter))]
        public SecureString Secret { get; set; }
    }
}
