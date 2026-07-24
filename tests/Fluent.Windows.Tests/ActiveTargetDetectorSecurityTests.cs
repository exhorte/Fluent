using Fluent.Core.Interaction;
using Fluent.Windows.ActiveTarget;

namespace Fluent.Windows.Tests;

public sealed class ActiveTargetDetectorSecurityTests
{
    [Theory]
    [InlineData(true, TargetSecurityStatus.Password)]
    [InlineData(false, TargetSecurityStatus.VerifiedNonPassword)]
    [InlineData(null, TargetSecurityStatus.Unknown)]
    public void ClassifySecurityStatus_preserves_unknown_as_fail_closed_state(
        bool? isPassword,
        TargetSecurityStatus expected)
    {
        TargetSecurityStatus actual = ActiveTargetDetector.ClassifySecurityStatus(isPassword);

        Assert.Equal(expected, actual);
    }
}
