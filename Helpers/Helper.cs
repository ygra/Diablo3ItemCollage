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
            if (start > end && step > 0 ||
                start < end && step < 0 ||
                step == 0)
                throw new ArgumentException("Impossible range");

            int steps = (end - start) / step;
            int i, s;
            for (i = start, s = 0; s <= steps; i += step, s++)
            {
                yield return i;
            }
        }
    }
}
