// This project sets <GenerateAssemblyInfo>false</GenerateAssemblyInfo>, which suppresses the
// SupportedOSPlatform attribute the SDK would otherwise auto-emit for the net8.0-windows TFM.
// Without it, the platform-compatibility analyzer (CA1416) treats the assembly as cross-platform
// and warns on every Windows-only API (WinForms, etc.). Asserting it here restores the intended
// behavior. Kept out of the shared projitems on purpose: MEPUtils-2022/2024 target .NET Framework
// 4.8, where SupportedOSPlatformAttribute does not exist.
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows7.0")]
