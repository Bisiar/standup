// .NET 10 Global XML Namespaces
// These registrations allow XAML files to use types from these namespaces
// without explicit xmlns: declarations by using the global namespace:
// http://schemas.microsoft.com/dotnet/maui/global

// Standup.Maui namespaces
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "Standup.Maui")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "Standup.Maui.Views")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "Standup.Maui.Views.Components")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "Standup.Maui.ViewModels")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "Standup.Maui.Converters")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "Standup.Maui.Models")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "Standup.Maui.Services")]

// Standup.Domain namespaces (from referenced assembly)
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "Standup.Domain.Entities")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "Standup.Domain.Enums")]

// Standup.Application namespaces (from referenced assembly)
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "Standup.Application.ViewModels")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/dotnet/maui/global", "Standup.Application.Models")]
