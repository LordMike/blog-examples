using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace ConsoleApplication
{
    public class Program
    {
        private static readonly List<ErrorEventArgs> Errors = new List<ErrorEventArgs>();

        public static void Main()
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error,
                ContractResolver = new FailingContractResolver(),
                Error = Error
            };

            JsonSerializer serializer = JsonSerializer.Create(settings);

            MyObject testObject;
            using (StreamReader sr = File.OpenText("Test.json"))
            using (JsonTextReader tr = new JsonTextReader(sr))
                testObject = serializer.Deserialize<MyObject>(tr);

            Console.WriteLine($"There were {Errors.Count} errors");

            foreach (ErrorEventArgs error in Errors)
            {
                Console.WriteLine($"Message : {error.ErrorContext.Error.Message}");
                Console.WriteLine($"  Path  : {error.ErrorContext.Path}");
                Console.WriteLine($"  Member: {error.ErrorContext.Member}");

                Console.WriteLine();
            }
        }

        private static void Error(object sender, ErrorEventArgs errorEventArgs)
        {
            Errors.Add(errorEventArgs);
            errorEventArgs.ErrorContext.Handled = true;
        }
    }

    public class MyObject
    {
        public string Name { get; set; }

        public bool OtherProperty { get; set; }
    }

    public class FailingContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty res = base.CreateProperty(member, memberSerialization);

            if (!res.Ignored)
                // If we haven't explicitly stated that a field is not needed, we require it for compliance
                res.Required = Required.AllowNull;

            return res;
        }
    }
}
