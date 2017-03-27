using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Stylet;
using SyncTrayzor.Syncthing;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages.Tray
{
    public class NetworkGraphViewModel : Screen, IDisposable
    {
        private static readonly DateTime epoch = DateTime.UtcNow; // Some arbitrary value in the past
        private static readonly TimeSpan window = TimeSpan.FromMinutes(15);

        private const double minYValue = 1024 * 100; // 100 KBit/s

        private readonly ISyncthingManager syncthingManager;

        private readonly LinearAxis yAxis;
        private readonly LinearAxis xAxis;

        private readonly LineSeries inboundSeries;
        private readonly LineSeries outboundSeries;

        public PlotModel OxyPlotModel { get; } = new PlotModel();
        public bool ShowGraph { get; private set; }

        public string MaxYValue { get; private set; }

        public NetworkGraphViewModel(ISyncthingManager syncthingManager)
        {
            this.syncthingManager = syncthingManager;

            this.OxyPlotModel.PlotAreaBorderColor = OxyColors.LightGray;

            this.xAxis = new LinearAxis()
            {
                Position = AxisPosition.Bottom,
                IsZoomEnabled = false,
                IsPanEnabled = false,
                IsAxisVisible = false,
                MajorGridlineColor = OxyColors.Gray,
                MajorGridlineStyle = LineStyle.Dash,
            };
            this.OxyPlotModel.Axes.Add(this.xAxis);

            this.yAxis = new LinearAxis()
            {
                Position = AxisPosition.Right,
                IsZoomEnabled = false,
                IsPanEnabled = false,
                IsAxisVisible = false,
                AbsoluteMinimum = -1, // Leave a little bit of room for the line to draw
            };
            this.OxyPlotModel.Axes.Add(this.yAxis);

            this.inboundSeries = new LineSeries()
            {
                Color = OxyColors.Red,
            };
            this.OxyPlotModel.Series.Add(this.inboundSeries);

            this.outboundSeries = new LineSeries()
            {
                Color = OxyColors.Green,
            };
            this.OxyPlotModel.Series.Add(this.outboundSeries);

            this.ResetToEmptyGraph();

            this.Update(this.syncthingManager.TotalConnectionStats);
            this.syncthingManager.TotalConnectionStatsChanged += this.TotalConnectionStatsChanged;
            this.syncthingManager.StateChanged += this.SyncthingStateChanged;
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            this.OxyPlotModel.InvalidatePlot(true);
        }

        private void SyncthingStateChanged(object sender, SyncthingStateChangedEventArgs e)
        {
            if (e.OldState == SyncthingState.Running)
            {
                this.ResetToEmptyGraph();
            }

            this.ShowGraph = e.NewState == SyncthingState.Running;
        }

        private void ResetToEmptyGraph()
        {
            var now = DateTime.UtcNow;
            var earliest = (now - window - epoch).TotalSeconds;
            var latest = (now - epoch).TotalSeconds;

            // Put points on the far left, so we get a line from them
            this.inboundSeries.Points.Clear();
            this.inboundSeries.Points.Add(new DataPoint(earliest, 0));
            this.inboundSeries.Points.Add(new DataPoint(latest, 0));

            this.outboundSeries.Points.Clear();
            this.outboundSeries.Points.Add(new DataPoint(earliest, 0));
            this.outboundSeries.Points.Add(new DataPoint(latest, 0));

            this.xAxis.Minimum = earliest;
            this.xAxis.Maximum = latest;

            this.yAxis.Maximum = minYValue;
            this.MaxYValue = FormatUtils.BytesToHuman(minYValue) + "/s";

            if (this.IsActive)
                this.OxyPlotModel.InvalidatePlot(true);
        }

        private void TotalConnectionStatsChanged(object sender, ConnectionStatsChangedEventArgs e)
        {
            this.Update(e.TotalConnectionStats);
        }

        private void Update(SyncthingConnectionStats stats)
        {
            var now = DateTime.UtcNow;
            double earliest = (now - window - epoch).TotalSeconds;

            this.Update(earliest, this.inboundSeries, stats.InBytesPerSecond);
            this.Update(earliest, this.outboundSeries, stats.OutBytesPerSecond);

            this.xAxis.Minimum = earliest;
            this.xAxis.Maximum = (now - epoch).TotalSeconds;

            // This increases the value to the nearest 1024 boundary
            double maxValue = this.inboundSeries.Points.Concat(this.outboundSeries.Points).Max(x => x.Y);
            double roundedMax;
            if (maxValue > minYValue)
            {
                double factor = Math.Pow(1024, (int)Math.Log(maxValue, 1024));
                roundedMax = Math.Ceiling(maxValue / factor) * factor;
            }
            else
            {
                roundedMax = minYValue;
            }

            // Give the graph a little bit of headroom, otherwise the line gets chopped
            this.yAxis.Maximum = roundedMax * 1.05;
            this.MaxYValue = FormatUtils.BytesToHuman(roundedMax) + "/s";

            if (this.IsActive)
                this.OxyPlotModel.InvalidatePlot(true);
        }

        private void Update(double earliest, LineSeries series, double bytesPerSecond)
        {
            // Keep one data point below 'earliest'

            int i = 0;
            for (; i < series.Points.Count && series.Points[i].X < earliest; i++) { }
            i--;
            if (i > 0)
            {
                series.Points.RemoveRange(0, i);
            }

            series.Points.Add(new DataPoint((DateTime.UtcNow - epoch).TotalSeconds, bytesPerSecond));
        }

        public void Dispose()
        {
            this.syncthingManager.TotalConnectionStatsChanged -= this.TotalConnectionStatsChanged;
            this.syncthingManager.StateChanged -= this.SyncthingStateChanged;
        }
    }
}
