using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ItemCollage
{
    public static class Helper
    {
        public static IEnumerable<int> Range(int start, int end, int step = 1)
        {
            int i;
            for (i = start; i <= end; i += step)
            {
                yield return i;
            }
        }
    }
}
