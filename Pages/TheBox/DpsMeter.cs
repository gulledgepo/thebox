using System;
using System.Collections.Generic;

namespace PaxstonProject.Pages.TheBox
{
    public class DpsMeter
    {
        public double WindowSeconds { get; set; } = 2.0;
        public double EmaTauSeconds { get; set; } = 0.6;

        public Func<double> PassiveDpsProvider { get; }
        public Func<double> ClickValueProvider { get; }

        private readonly Queue<DateTime> _clicks = new();
        private DateTime _now = DateTime.UtcNow;
        private double _emaClickDps = 0.0;

        public DpsMeter(Func<double> passiveDpsProvider, Func<double> clickValueProvider)
        {
            PassiveDpsProvider = passiveDpsProvider ?? throw new ArgumentNullException(nameof(passiveDpsProvider));
            ClickValueProvider = clickValueProvider ?? throw new ArgumentNullException(nameof(clickValueProvider));
        }

        public void OnTick()
        {
            _now = DateTime.UtcNow;
            PruneClicks();

            double clickValue = ClickValueProvider();
            double instantClickDps = GetRecentClickDps(clickValue);

            double dt = WindowSeconds > 0 ? WindowSeconds : 0.1;
            double alpha = 1 - Math.Exp(-dt / EmaTauSeconds);
            _emaClickDps = _emaClickDps + alpha * (instantClickDps - _emaClickDps);
        }

        public void RegisterClick()
        {
            _clicks.Enqueue(DateTime.UtcNow);
        }

        private void PruneClicks()
        {
            var cutoff = _now - TimeSpan.FromSeconds(WindowSeconds);
            while (_clicks.Count > 0 && _clicks.Peek() < cutoff)
                _clicks.Dequeue();
        }

        public double GetRecentClickDps(double clickValue)
        {
            if (WindowSeconds <= 0 || _clicks.Count == 0)
                return 0;
            double clicksPerSec = _clicks.Count / WindowSeconds;
            return clicksPerSec * Math.Max(0.0, clickValue);
        }

        public double GetRecentClickDpsEma(double clickValue)
        {
            return _emaClickDps;
        }

        public double GetPassiveDps()
        {
            return PassiveDpsProvider();
        }

        public double GetTotalDps(double clickValue)
        {
            return GetPassiveDps() + GetRecentClickDps(clickValue);
        }

        public double GetTotalDpsEma(double clickValue)
        {
            return GetPassiveDps() + GetRecentClickDpsEma(clickValue);
        }

        public static string FormatDps(double dps)
        {
            return $"{dps:0.00}";
        }
    }
}
