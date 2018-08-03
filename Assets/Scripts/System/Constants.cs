namespace Assets.Scripts.System
{
    public static class Constants
    {
        // The ratio of speed in mph over Unity world space units per second. e.g. 1.0 means 1 mph = 1.0 world unit per second
        // Current value is estimated based on perceivable speed of drone cars in training mission.
        public const float SpeedUnitRatio = 1.0f;

        // The distance between an entity and its target path node to satisfy the HasArrived action.
        // Current value is estimated based on stopping position of player's car in training mission.
        public const float PathMinDistanceTreshold = 33.5f;
    }
}
