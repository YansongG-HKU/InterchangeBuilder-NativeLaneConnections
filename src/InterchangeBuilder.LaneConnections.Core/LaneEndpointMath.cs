namespace InterchangeBuilder.LaneConnections;

/// <summary>Converts CS2 composition-local lane data into endpoint-arm data.</summary>
public static class LaneEndpointMath
{
    /// <param name="localPosition">
    /// CS2 composition X, positive to the road prefab's local right.
    /// </param>
    /// <param name="atStart">Whether the lane is viewed at its route start.</param>
    /// <param name="expressInOpposingArmAxis">
    /// True for the newly built road, whose arm points opposite the target arm
    /// at a continuous connection; false for the existing target road.
    /// </param>
    public static float ToConnectionAxis(
        float localPosition,
        bool atStart,
        bool expressInOpposingArmAxis)
    {
        float position = atStart ? -localPosition : localPosition;
        return expressInOpposingArmAxis ? -position : position;
    }

    /// <summary>
    /// Returns +1 for flow away from the shared node along the lane's own arm,
    /// -1 for flow toward it, and 0 for a two-way lane.
    /// </summary>
    public static int ToArmFlow(bool inverted, bool twoWay, bool atStart)
    {
        if (twoWay)
        {
            return 0;
        }

        int routeFlow = inverted ? -1 : 1;
        return atStart ? routeFlow : -routeFlow;
    }
}
