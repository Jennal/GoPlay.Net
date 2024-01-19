namespace GoPlayProj.Utils
{
    public static class EqualsUtils
    {
        public static bool Equals(Dictionary<int, int> a, Dictionary<int, int> b)
        {
            if (a.Count != b.Count) return false;
            foreach (var kv in a)
            {
                if (!b.ContainsKey(kv.Key)) return false;
                if (b[kv.Key] != kv.Value) return false;
            }

            return true;
        }
    }
}