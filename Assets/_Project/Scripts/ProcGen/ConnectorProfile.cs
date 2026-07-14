namespace Chris.PachiRogue.ProcGen
{
    /// <summary>Shape of a chunk's top/bottom edge, used to guarantee passable seams.</summary>
    public enum ConnectorProfile
    {
        Open = 0,
        Narrow = 1
    }

    public static class ConnectorRules
    {
        /// <summary>
        /// A seam is passable unless a Narrow exit feeds directly into a
        /// Narrow entry (double choke — the "impassable seam" rule).
        /// </summary>
        public static bool AreCompatible(ConnectorProfile bottomOfUpper, ConnectorProfile topOfLower)
        {
            return !(bottomOfUpper == ConnectorProfile.Narrow && topOfLower == ConnectorProfile.Narrow);
        }
    }
}
