using Newtonsoft.Json;
using System.Reflection;

namespace Baobab.SharedKernel.Domain.Primitives.Factory;

public static class EventFactory
{
    public static IDomainEvent CreateEventTypeUsingReflection(string assembly, string typeName, string jsonContent)
    {
        // Load the assembly containing the domain event type
        Assembly domainEventAssembly = Assembly.Load(assembly);
        Type eventType = domainEventAssembly.GetType(typeName)!;

        if (eventType == null || !typeof(IDomainEvent).IsAssignableFrom(eventType))
        {
            throw new InvalidOperationException($"Invalid domain event type: {typeName}");
        }

        // Deserialize the JSON content to the obtained domain event type
        IDomainEvent domainEvent = (IDomainEvent)JsonConvert.DeserializeObject(jsonContent, eventType)!;

        return domainEvent;
    }
}
