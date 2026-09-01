using System;

namespace net.satania_shopping.tabakosystem
{
    public static class OnHiearchyChangedHook
    {
        public static event Action OnHierarchyChanged;

        public static void InvokeHierarchyChanged()
        {
            OnHierarchyChanged?.Invoke();
        }
    }
}