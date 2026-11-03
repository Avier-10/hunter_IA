public enum NavigationProfile
{
    Hunter,
    Wolf
}

public static class NavigationProfileExtensions
{
    // Wolves never enter safe-zone nodes, so edges touching them are never traversed either.
    public static bool CanUse(this NavigationProfile profile, Node node)
    {
        if (node == null || node.IsBlocked) return false;
        return profile == NavigationProfile.Hunter || !node.IsSafeZone;
    }
}
