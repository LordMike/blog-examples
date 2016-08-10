using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace deserialize_different_types
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string json = File.ReadAllText("Test.json");
            List<Vehicle> testObject = JsonConvert.DeserializeObject<List<Vehicle>>(json);

            foreach (Vehicle vehicle in testObject)
            {
                Console.WriteLine($"Vehicle type: {vehicle.Type}, object type: {vehicle.GetType().FullName}");
                Console.WriteLine($"  Wheels: {vehicle.Wheels}");

                Car asCar = vehicle as Car;
                if (asCar != null)
                {
                    Console.WriteLine($"  Has trunk: {asCar.HasTrunk}");
                }

                Bicycle asBicycle = vehicle as Bicycle;
                if (asBicycle != null)
                {
                    Console.WriteLine($"  Persons: {asBicycle.Persons}");
                }

                Console.WriteLine();
            }
        }
    }

    public class VehicleConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken jObject = JToken.ReadFrom(reader);
            VehicleType type = jObject["type"].ToObject<VehicleType>();

            Vehicle result;
            switch (type)
            {
                case VehicleType.Car:
                    result = new Car();
                    break;
                case VehicleType.Bicycle:
                    result = new Bicycle();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            serializer.Populate(jObject.CreateReader(), result);

            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // We cannot directly serialize "value" here, as that would call our own converter once more
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            // Not needed, as we register our converter directly on Vehicle
            throw new NotImplementedException();
        }
    }

    public enum VehicleType
    {
        Unknown,
        Car,
        Bicycle
    }

    [JsonConverter(typeof(VehicleConverter))]
    public abstract class Vehicle
    {
        public VehicleType Type { get; set; }

        public int Wheels { get; set; }
    }

    public class Car : Vehicle
    {
        public Car()
        {
            Type = VehicleType.Car;
        }

        [JsonProperty("trunk")]
        public bool HasTrunk { get; set; }
    }

    public class Bicycle : Vehicle
    {
        public Bicycle()
        {
            Type = VehicleType.Bicycle;
        }

        public int Persons { get; set; }
    }
}
