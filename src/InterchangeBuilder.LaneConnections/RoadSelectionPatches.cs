using System;
using Game.Prefabs;
using Game.Tools;
using Unity.Entities;

namespace InterchangeBuilder.LaneConnections;

internal static class PanelRoadSelectionPatch
{
    public static void Prefix(object __instance, int __0, out bool __state)
    {
        __state = RoadSelectionController.BeginExplicitSelection(
            __instance,
            __0,
            "InterchangeBuilder compatibility selector");
    }

    public static void Postfix(object __instance, bool __state)
    {
        RoadSelectionController.EndExplicitSelection(__instance, __state);
    }

    public static Exception? Finalizer(object __instance, Exception? __exception, bool __state)
    {
        if (__exception != null)
        {
            RoadSelectionController.EndExplicitSelection(__instance, __state);
        }

        return __exception;
    }
}

internal static class CycleRoadSelectionPatch
{
    public static void Prefix(object __instance, out bool __state)
    {
        __state = RoadSelectionController.BeginExplicitSelection(
            __instance,
            -1,
            "InterchangeBuilder compatibility cycle control");
    }

    public static void Postfix(object __instance, bool __state)
    {
        RoadSelectionController.EndExplicitSelection(__instance, __state);
    }

    public static Exception? Finalizer(object __instance, Exception? __exception, bool __state)
    {
        if (__exception != null)
        {
            RoadSelectionController.EndExplicitSelection(__instance, __state);
        }

        return __exception;
    }
}

internal static class ExternalRoadSelectionPatch
{
    public static void Prefix(object __instance, out bool __state)
    {
        __state = RoadSelectionController.BeginExplicitSelection(
            __instance,
            -1,
            "game road panel");
    }

    public static void Postfix(object __instance, bool __state)
    {
        RoadSelectionController.EndExplicitSelection(__instance, __state);
    }

    public static Exception? Finalizer(object __instance, Exception? __exception, bool __state)
    {
        if (__exception != null)
        {
            RoadSelectionController.EndExplicitSelection(__instance, __state);
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

internal static class ModePreparationPatch
{
    public static void Postfix(object __instance)
    {
        RoadSelectionController.BeginModeFromVanilla(__instance);
    }
}

internal static class RouteRestartPatch
{
    public static void Postfix(object __instance)
    {
        RoadSelectionController.BeginNewRoute(__instance);
    }
}

internal static class RoadBuildSelectionPatch
{
    public static bool Prefix(object __instance) => RoadSelectionController.AllowBuild(__instance);
}

internal static class RoadSelectionWorldPatch
{
    public static void Postfix()
    {
        RoadSelectionController.ResetForWorld();
    }
}

internal static class VanillaPrefabActivationPatch
{
    public static bool Prefix(
        ToolSystem __instance,
        PrefabBase __0,
        ref bool __result)
    {
        if (!RoadSelectionController.TryKeepInterchangeBuilderActive(__instance, __0))
        {
            return true;
        }

        __result = true;
        return false;
    }
}
