using System;
using Colossal.UI.Binding;
using Game.UI;

namespace InterchangeBuilder.LaneConnections;

internal sealed class RoadSelectionUISystem : UISystemBase
{
    private const string Group = "InterchangeBuilder";

    private ValueBinding<string>? _modeBinding;
    private ValueBinding<string>? _sourceNameBinding;
    private ValueBinding<string>? _statusBinding;
    private ValueBinding<bool>? _canConfirmBinding;
    private int _lastRevision = -1;

    protected override void OnCreate()
    {
        base.OnCreate();
        RoadSelectionUiSnapshot snapshot = RoadSelectionController.GetUiSnapshot();
        AddBinding(_modeBinding = new ValueBinding<string>(
            Group,
            "roadSelectionMode",
            snapshot.Mode));
        AddBinding(_sourceNameBinding = new ValueBinding<string>(
            Group,
            "roadSelectionSourceName",
            snapshot.SourceName));
        AddBinding(_statusBinding = new ValueBinding<string>(
            Group,
            "roadSelectionStatus",
            snapshot.Status));
        AddBinding(_canConfirmBinding = new ValueBinding<bool>(
            Group,
            "roadSelectionCanConfirm",
            snapshot.CanConfirm));
        AddBinding(new TriggerBinding(Group, "confirmRoadSelection", ConfirmSelection));
        AddBinding(new TriggerBinding(Group, "followStartRoad", FollowStart));
        _lastRevision = snapshot.Revision;
        UpgradeLog.Info("Road selection lock UI bindings registered.");
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();
        RoadSelectionUiSnapshot snapshot = RoadSelectionController.GetUiSnapshot();
        if (snapshot.Revision == _lastRevision)
        {
            return;
        }

        _lastRevision = snapshot.Revision;
        _modeBinding?.Update(snapshot.Mode);
        _sourceNameBinding?.Update(snapshot.SourceName);
        _statusBinding?.Update(snapshot.Status);
        _canConfirmBinding?.Update(snapshot.CanConfirm);
    }

    private static void ConfirmSelection()
    {
        if (!RoadSelectionController.ConfirmSelection())
        {
            UpgradeLog.Warn("Road lock confirmation was ignored because no pending road is available.");
        }
    }

    private static void FollowStart()
    {
        RoadSelectionController.FollowStart();
    }
}
