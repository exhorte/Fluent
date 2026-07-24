using Fluent.Core.Interaction;

namespace Fluent.Windows.ActiveTarget;

public interface IActiveTargetDetector
{
    TargetSnapshot? CaptureActiveTarget();
}
