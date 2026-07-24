using System.Reflection;
using System.Runtime.InteropServices;
using Fluent.Windows.Input;

namespace Fluent.Windows.Tests;

public sealed class KeyboardInputSenderTests
{
    [Fact]
    public void Native_input_layout_matches_Windows_INPUT_size()
    {
        Type? inputType = typeof(KeyboardInputSender).GetNestedType("Input", BindingFlags.NonPublic);
        int expectedSize = Environment.Is64BitProcess ? 40 : 28;

        Assert.NotNull(inputType);
        Assert.Equal(expectedSize, Marshal.SizeOf(inputType));
    }
}
