using System.Collections.Generic;
using UnityEngine;

namespace NightShift
{
    internal readonly struct FloorFootprint
    {
        internal FloorFootprint(Vector2 center, Vector2 size)
        {
            Center = center;
            Size = size;
        }

        internal Vector2 Center { get; }
        internal Vector2 Size { get; }
        internal float Area => Size.x * Size.y;
    }

    internal enum GameMode
    {
        Casual,
        Standard
    }

    internal enum PlacementFailure
    {
        None,
        NoOverlap,
        TooLittleSupport,
        Unbalanced
    }

    internal readonly struct SupportJoint
    {
        internal SupportJoint(int lowerIndex, Vector2 contactMin, Vector2 contactMax, Vector2 loadCenter)
        {
            LowerIndex = lowerIndex;
            ContactMin = contactMin;
            ContactMax = contactMax;
            LoadCenter = loadCenter;

            float inset = StackRules.SupportEdgeInset;
            float marginX = Mathf.Min(loadCenter.x - (contactMin.x + inset), (contactMax.x - inset) - loadCenter.x);
            float marginY = Mathf.Min(loadCenter.y - (contactMin.y + inset), (contactMax.y - inset) - loadCenter.y);
            Margin = Mathf.Min(marginX, marginY);
        }

        internal int LowerIndex { get; }
        internal Vector2 ContactMin { get; }
        internal Vector2 ContactMax { get; }
        internal Vector2 LoadCenter { get; }
        internal float Margin { get; }
        internal bool HasContact => ContactMax.x > ContactMin.x && ContactMax.y > ContactMin.y;
    }

    internal readonly struct PlacementResult
    {
        internal PlacementResult(FloorFootprint footprint, PlacementFailure failure, bool snapped = false, SupportJoint? joint = null)
        {
            Footprint = footprint;
            Failure = failure;
            IsPerfect = snapped && failure == PlacementFailure.None;
            Joint = joint;
        }

        internal FloorFootprint Footprint { get; }
        internal bool IsStable => Failure == PlacementFailure.None;
        internal bool IsPerfect { get; }
        internal PlacementFailure Failure { get; }
        internal SupportJoint? Joint { get; }
    }

    internal static class StackRules
    {
        internal const float PerfectTolerance = 0.16f;
        internal const float SupportEdgeInset = 0.08f;
        private const float MinimumSupportFraction = 0.30f;

        internal static PlacementResult Evaluate(
            IReadOnlyList<FloorFootprint> settledBottomToTop, FloorFootprint incoming, GameMode mode = GameMode.Standard)
        {
            if (settledBottomToTop.Count == 0)
            {
                return new PlacementResult(incoming, PlacementFailure.None);
            }

            int topIndex = settledBottomToTop.Count - 1;
            Vector2 topCenter = settledBottomToTop[topIndex].Center;
            Vector2 offset = incoming.Center - topCenter;
            bool snapped = Mathf.Abs(offset.x) <= PerfectTolerance && Mathf.Abs(offset.y) <= PerfectTolerance;
            FloorFootprint placement = snapped ? new FloorFootprint(topCenter, incoming.Size) : incoming;

            FloorFootprint upper = placement;
            float loadArea = placement.Area;
            Vector2 weightedCenter = placement.Center * loadArea;
            SupportJoint? weakestJoint = null;
            int bottomIndex = mode == GameMode.Casual ? topIndex : 0;

            for (int index = topIndex; index >= bottomIndex; index--)
            {
                FloorFootprint lower = settledBottomToTop[index];
                Vector2 upperHalfSize = upper.Size * 0.5f;
                Vector2 lowerHalfSize = lower.Size * 0.5f;
                Vector2 contactMin = Vector2.Max(upper.Center - upperHalfSize, lower.Center - lowerHalfSize);
                Vector2 contactMax = Vector2.Min(upper.Center + upperHalfSize, lower.Center + lowerHalfSize);
                Vector2 contactSize = contactMax - contactMin;

                // Floors have equal thickness and density, so footprint area is proportional to mass.
                Vector2 loadCenter = weightedCenter / loadArea;
                SupportJoint joint = new SupportJoint(index, contactMin, contactMax, loadCenter);
                if (!joint.HasContact)
                {
                    return new PlacementResult(placement, PlacementFailure.NoOverlap, joint: joint);
                }

                if (contactSize.x * contactSize.y < upper.Area * MinimumSupportFraction)
                {
                    return new PlacementResult(placement, PlacementFailure.TooLittleSupport, joint: joint);
                }

                if (joint.Margin < 0f)
                {
                    return new PlacementResult(placement, PlacementFailure.Unbalanced, joint: joint);
                }

                if (!weakestJoint.HasValue || joint.Margin < weakestJoint.Value.Margin)
                {
                    weakestJoint = joint;
                }

                loadArea += lower.Area;
                weightedCenter += lower.Center * lower.Area;
                upper = lower;
            }

            return new PlacementResult(placement, PlacementFailure.None, snapped, weakestJoint);
        }
    }
}
