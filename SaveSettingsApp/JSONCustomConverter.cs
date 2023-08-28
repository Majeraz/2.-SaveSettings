using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_Asistent_v1._1._2._CS._3._Converters;

public class JSONCustomConverter : JsonConverter {
    public override bool CanConvert(Type objectType) {
        return true;
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
        JToken token = JToken.Load(reader);
        if (token.Type == JTokenType.Object) {
            JObject jObject = (JObject)token;
            JToken typeToken = jObject["type"];

            if (typeToken != null && typeToken.Type == JTokenType.String) {
                string typeName = (string)typeToken;
                Type type = Type.GetType(typeName);

                if (type != null)
                    return jObject.ToObject(type);
            }
        }

        return token.ToObject(objectType);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
        throw new NotImplementedException();
    }
}
