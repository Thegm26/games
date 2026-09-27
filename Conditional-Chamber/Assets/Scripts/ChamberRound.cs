using System;
using System.Collections.Generic;

namespace ChamberLogic
{
    public enum Shell { Blank, Live }

    public sealed class ChamberRound
    {
        private readonly List<Shell> shells = new List<Shell>();

        public int RemainingLive { get; private set; }
        public int RemainingBlank { get; private set; }
        public int RemainingTotal => RemainingLive + RemainingBlank;
        public float LiveChance => RemainingTotal == 0 ? 0f : (float)RemainingLive / RemainingTotal;

        public ChamberRound(int live, int blank, int seed)
        {
            if (live < 0 || blank < 0 || live + blank == 0)
                throw new ArgumentException("A round needs at least one shell.");

            RemainingLive = live;
            RemainingBlank = blank;
            for (var i = 0; i < live; i++) shells.Add(Shell.Live);
            for (var i = 0; i < blank; i++) shells.Add(Shell.Blank);

            var random = new Random(seed);
            for (var i = shells.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                var swap = shells[i];
                shells[i] = shells[j];
                shells[j] = swap;
            }
        }

        public Shell Fire()
        {
            if (shells.Count == 0) throw new InvalidOperationException("The chamber is empty.");
            var shell = shells[0];
            shells.RemoveAt(0);
            if (shell == Shell.Live) RemainingLive--; else RemainingBlank--;
            return shell;
        }
    }
}
