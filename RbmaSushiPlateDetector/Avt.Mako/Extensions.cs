using System;

namespace Avt.Mako
{
    public static class Extensions
    {
        public static void SafeInvoke<T>(this EventHandler<T> evt, object sender, T e) where T : EventArgs
        {
            if (evt != null)
                evt(sender, e);
        }
    }
}
