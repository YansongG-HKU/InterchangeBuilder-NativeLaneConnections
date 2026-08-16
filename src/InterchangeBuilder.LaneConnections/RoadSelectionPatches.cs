using System;
using Unity.Entities;

namespace InterchangeBuilder.LaneConnections;

internal static class PanelRoadSelectionPatch
{
    public static void Prefix(object __instance, int __0, out bool __state)
    {
        __state = RoadSelectionController.BeginUserSelection(__instance, __0);
    }

    public static void Postfix(object __instance, bool __state)
    {
        RoadSelectionController.EndUserSelection(__instance, __state);
    }

    public static Exception? Finalizer(object __instance, Exception? __exception, bool __state)
    {
        if (__exception != null)
        {
            RoadSelectionController.EndUserSelection(__instance, __state);
        }

        return __exception;
    }
}

internal static class CycleRoadSelectionPatch
{
    public static void Prefix(object __instance, out bool __state)
    {
        __state = RoadSelectionController.BeginUserSelection(__instance, -1);
    }

    public static void Postfix(object __instance, bool __state)
    {
        RoadSelectionController.EndUserSelection(__instance, __state);
    }

    public static Exception? Finalizer(object __instance, Exception? __exception, bool __state)
    {
        if (__exception != null)
        {
            RoadSelectionController.EndUserSelection(__instance, __state);
        }

        return __exception;
    }
}

internal static class ExternalRoadSelectionPatch
{
    public static void Prefix(object __instance, out bool __state)
    {
        __state = RoadSelectionController.BeginUserSelection(__instance, -1);
    }

    public static void Postfix(object __instance, bool __state)
    {
        RoadSelectionController.EndUserSelection(__instance, __state);
    }

    public static Exception? Finalizer(object __instance, Exception? __exception, bool __state)
    {
        if (__exception != null)
        {
            RoadSelectionController.EndUserSelection(__instance, __state);
        }

        return __exception;
    }
}

internal static class SelectedRoadPrefabPatch
{
    public static void Prefix(object __instance, ref Entity __0)
    {
        RoadSelectionController.FilterSelectedPrefab(__instance, ref __0);
    }
}

internal static class EndpointRoadSourcePatch
{
    public static void Prefix(bool __1, out int __state)
    {
        __state = RoadSelectionController.BeginSourceSelection(__1);
    }

    public static void Postfix(object __instance, int __state)
    {
        RoadSelectionController.EndSourceSelection(__instance, __state);
    }

    public static Exception? Finalizer(object __instance, Exception? __exception, int __state)
    {
        if (__exception != null)
        {
            RoadSelectionController.EndSourceSelection(__instance, __state);
        }

        return __exception;
    }
}

internal static class FreeDrawRoadSourcePatch
{
    public static void Prefix(out int __state)
    {
        __state = RoadSelectionController.BeginSourceSelection(inheritRoadFromNode: true);
    }

    public static void Postfix(object __instance, int __state)
    {
        RoadSelectionController.EndSourceSelection(__instance, __state);
    }

    public static Exception? Finalizer(object __instance, Exception? __exception, int __state)
    {
        if (__exception != null)
        {
            RoadSelectionController.EndSourceSelection(__instance, __state);
        }

        return __exception;
    }
}

internal static class NewRoutePatch
{
    public static void Postfix(object __instance)
    {
        RoadSelectionController.BeginNewRoute(__instance);
    }
}

internal static class RoadBuildConfirmationPatch
{
    public static bool Prefix(object __instance) => RoadSelectionController.AllowBuild(__instance);
}

internal static class RoadSelectionWorldPatch
{
    public static void Postfix(object __instance)
    {
        RoadSelectionController.ResetForWorld(__instance);
    }
}
