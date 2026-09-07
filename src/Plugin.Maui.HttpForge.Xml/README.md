# Plugin.Maui.HttpForge.Xml

Optional XXE-safe XML serializer for [Plugin.Maui.HttpForge](https://www.nuget.org/packages/Plugin.Maui.HttpForge). The core package does not take an XML dependency.

```csharp
settings.ContentSerializer = new XmlContentSerializer();
```

DTD processing is prohibited and `XmlResolver` is null.
