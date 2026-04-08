using System;
using System.Reflection;
using System.Linq;

// Load the assembly
var assembly = Assembly.LoadFrom("/Volumes/1TB/Repos/avarts/bikeapelago-react/api/bin/Debug/net10.0/Archipelago.MultiClient.Net.dll");

// Let's find ArchipelagoSession
var sessionType = assembly.GetType("Archipelago.MultiClient.Net.ArchipelagoSession");
if (sessionType != null) {
    Console.WriteLine("Methods on ArchipelagoSession:");
    foreach (var method in sessionType.GetMethods().Where(m => m.Name.Contains("Login") || m.Name.Contains("Connect")))
    {
        Console.WriteLine(" - " + method.Name);
    }
}

// Find extension methods
var extensions = assembly.GetTypes()
    .Where(t => t.IsSealed && !t.IsGenericType && !t.IsNested)
    .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public))
    .Where(m => m.GetCustomAttributes(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false).Any() 
             && (m.Name.Contains("Connect") || m.Name.Contains("Login")))
    .ToList();

Console.WriteLine("Extension Methods:");
foreach (var ext in extensions)
{
    var p = string.Join(", ", ext.GetParameters().Select(x => x.ParameterType.Name));
    Console.WriteLine($" - {ext.Name} on {p}");
}
