using HarmonyLib;
using System.Reflection;

namespace HarmonyPatcher
{
    class Patch
    {
        private static bool m_bIsPatched = false;
        private static Harmony m_hMyInstance = null;
        public static bool IsPatched()
        {
            return m_bIsPatched;
        }
        internal static void Apply()
        {
            if (m_hMyInstance == null)
            {
                m_hMyInstance = new Harmony(ModConstants.ModConstants.modGUID);
                if (!m_bIsPatched)
                {
                    m_hMyInstance.PatchAll(Assembly.GetExecutingAssembly());
                    m_bIsPatched = true;
                }
            }
        }
        internal static void Remove()
        {
            if (m_hMyInstance != null)
            {
                m_hMyInstance.UnpatchSelf();
            }
            m_bIsPatched = false;
        }
    }
}
