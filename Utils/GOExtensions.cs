using UnityEngine;

namespace LEGACY.Utils
{
    public static class GOExtensions
    {
        public static GameObject GetChild(this GameObject go, string childName, bool substrSearch = false, bool recursiveSearch = true)
        {
            if (go.name.Equals(childName) ||
                substrSearch && go.name.Contains(childName)) return go;

            for (int i = 0; i < go.transform.childCount; i++)
            {
                var child = go.transform.GetChild(i).gameObject;

                GameObject ans = null;
                if (recursiveSearch)
                {
                    ans = child.GetChild(childName, substrSearch);
                }
                else if (child.name.Equals(childName) || substrSearch && child.name.Contains(childName))
                {
                    ans = child;
                }

                if (ans != null) return ans;
            }

            return null;
        }

    }
}
