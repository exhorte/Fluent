using Fluent.Windows;
using Fluent.Windows.Hotkeys;

namespace Fluent.Windows.Tests;

public sealed class WindowsAssemblyBoundaryTests
{
    [Fact]
    public void Windows_project_depends_on_core_boundary()
    {
        Assert.Equal("Fluent.Core", FluentWindowsAssembly.CoreAssemblyName);
    }

    [Fact]
    public void Global_hotkey_uses_stable_phase_01_identifier()
    {
        Assert.Equal(0x4E59, GlobalHotKey.DefaultId);
    }
}
