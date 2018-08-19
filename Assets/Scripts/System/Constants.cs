namespace Assets.Scripts.System
{
    public static class Constants
    {
        // The distance between an entity and its target path node to satisfy the HasArrived action.
        // Current value is estimated based on stopping position of player's car in training mission.
        public const float PathMinDistanceTreshold = 33.5f;
    }
}
