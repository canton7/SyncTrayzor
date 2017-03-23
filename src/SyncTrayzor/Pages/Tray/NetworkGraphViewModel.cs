using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Stylet;
using SyncTrayzor.Syncthing;
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
        private static readonly TimeSpan window = TimeSpan.FromMinutes(5);

        private readonly ISyncthingManager syncthingManager;

        private readonly LinearAxis yAxis;
        private readonly LinearAxis xAxis;

        private readonly LineSeries inboundSeries;
        private readonly LineSeries outboundSeries;

        public PlotModel OxyPlotModel { get; } = new PlotModel();
        public bool ShowGraph { get; private set; }

        public NetworkGraphViewModel(ISyncthingManager syncthingManager)
        {
            this.syncthingManager = syncthingManager;

            this.OxyPlotModel.PlotAreaBorderColor = OxyColors.Transparent;

            this.xAxis = new LinearAxis()
            {
                Position = AxisPosition.Bottom,
                IsZoomEnabled = false,
                IsPanEnabled = false,
                IsAxisVisible = false,
            };
            this.OxyPlotModel.Axes.Add(this.xAxis);

            this.yAxis = new LinearAxis()
            {
                Position = AxisPosition.Right,
                IsZoomEnabled = false,
                IsPanEnabled = false,
                IsAxisVisible = false,
                Minimum = 0,
                MinimumRange = 500 * 1024, // Half a meg
            };
            this.OxyPlotModel.Axes.Add(this.yAxis);

            this.inboundSeries = new LineSeries();
            this.OxyPlotModel.Series.Add(this.inboundSeries);

            this.outboundSeries = new LineSeries();
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
