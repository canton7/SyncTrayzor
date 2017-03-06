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
    public class NetworkGraphViewModel : Screen
    {
        private const double maxWindowSizeSeconds = 10;

        private readonly ISyncthingManager syncthingManager;

        private readonly LinearAxis inboundYAxis;
        private readonly LinearAxis outboundYAxis;
        private readonly LinearAxis xAxis;

        private readonly AreaSeries inboundSeries;
        private readonly AreaSeries outboundSeries;

        private DateTime startedAt;

        public PlotModel OxyPlotModel { get; } = new PlotModel();

        public NetworkGraphViewModel(ISyncthingManager syncthingManager)
        {
            this.syncthingManager = syncthingManager;

            this.xAxis = new LinearAxis()
            {
                Title = "Time (secs)",
                Position = AxisPosition.Bottom,
                MajorGridlineStyle = LineStyle.Automatic,
                IsZoomEnabled = false,
                IsPanEnabled = false,
            };
            this.OxyPlotModel.Axes.Add(this.xAxis);

            this.inboundYAxis = new LinearAxis()
            {
                Title = "Inbound",
                Key = "Inbound",
                Position = AxisPosition.Right,
                IsZoomEnabled = false,
                IsPanEnabled = false,
            };
            this.OxyPlotModel.Axes.Add(this.inboundYAxis);

            this.outboundYAxis = new LinearAxis()
            {
                Title = "Outbound",
                Key = "Outbound",
                Position = AxisPosition.Right,
                IsZoomEnabled = false,
                IsPanEnabled = false,
            };
            this.OxyPlotModel.Axes.Add(this.outboundYAxis);

            this.inboundSeries = new AreaSeries()
            {
                YAxisKey = this.inboundYAxis.Key,
                ConstantY2 = 0.0,
            };
            this.OxyPlotModel.Series.Add(this.inboundSeries);

            this.outboundSeries = new AreaSeries()
            {
                YAxisKey = this.outboundYAxis.Key,
                ConstantY2 = 0.0,
            };
            this.OxyPlotModel.Series.Add(this.outboundSeries);

            this.inboundSeries.Points.Add(new DataPoint(0, 10));
            this.inboundSeries.Points.Add(new DataPoint(2, 20));
            this.inboundSeries.Points.Add(new DataPoint(3, 30));

            this.outboundSeries.Points.Add(new DataPoint(0, 5));
            this.outboundSeries.Points.Add(new DataPoint(2, 25));
            this.outboundSeries.Points.Add(new DataPoint(3, 30));

            this.Reset();
        }

        private void Reset()
        {
            this.xAxis.Minimum = 0;
            this.xAxis.Maximum = maxWindowSizeSeconds;
        }

        private void Update()
        {
            var now = (DateTime.Now - this.startedAt).TotalSeconds;

            this.xAxis.Minimum = now - maxWindowSizeSeconds;
            this.xAxis.Maximum = now;

            this.OxyPlotModel.InvalidatePlot(true);
        }
    }
}
