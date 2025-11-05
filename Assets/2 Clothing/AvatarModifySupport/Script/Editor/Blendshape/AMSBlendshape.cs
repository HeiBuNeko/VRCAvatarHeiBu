using System.Collections.Generic;

namespace com.ams.avatarmodifysupport
{
    internal sealed class AMSBlendshape
    {
        int _index;
        string _name;
        float _max = 0;

        internal int Index => _index;
        internal string DisplayName => _name;
        internal float Max => _max;
        internal AMSBlendshape parent;
        internal HashSet<AMSBlendshape> childs = new HashSet<AMSBlendshape>();

        public AMSBlendshape(int index, string name, float max)
        {
            _index = index;
            _name = name;
            _max = max;
        }
    }
}