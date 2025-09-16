using System;
using System.Collections.Generic;
using System.Timers;

using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

	/// <summary>
	/// Synchronizes chart settings across multiple charts.
	/// Copies scale, time frame, and vertical lines from the master chart to others.
	/// </summary>
	public class SyncChartsStrategy : Strategy
	{
	private readonly Timer _timer;
	private readonly StrategyParam<bool> _syncVerticalLines;

	private readonly List<ChartState> _charts = new();

	/// <summary>
	/// Enable synchronization of vertical lines.
	/// </summary>
	public bool SyncVerticalLines
	{
	get => _syncVerticalLines.Value;
	set => _syncVerticalLines.Value = value;
}

	/// <summary>
	/// Initializes <see cref="SyncChartsStrategy"/>.
	/// </summary>
	public SyncChartsStrategy()
	{
	_syncVerticalLines = Param(nameof(SyncVerticalLines), true)
	.SetDisplay("Sync VLines", "Synchronize vertical lines across charts", "General");

	_timer = new Timer(200) { AutoReset = true };
	_timer.Elapsed += OnTimer;
}

	/// <summary>
	/// Adds a chart to the synchronization list.
	/// The first added chart becomes the master chart.
	/// </summary>
	/// <param name="chart">Chart state to track.</param>
	public void AddChart(ChartState chart)
	{
	_charts.Add(chart);
}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);
	_timer.Start();
}

	/// <inheritdoc />
	protected override void OnStopped(DateTimeOffset time)
	{
	base.OnStopped(time);
	_timer.Stop();
}

	private void OnTimer(object? sender, ElapsedEventArgs e)
	{
	if (_charts.Count == 0)
	return;

	var master = _charts[0];

	for (var i = 1; i < _charts.Count; i++)
	{
	var chart = _charts[i];
	chart.Scale = master.Scale;
	chart.TimeFrame = master.TimeFrame;
	chart.Mode = master.Mode;
	chart.FirstVisibleBar = master.FirstVisibleBar;
	chart.WidthInBars = master.WidthInBars;

	if (SyncVerticalLines)
	{
	chart.VerticalLines.Clear();
	foreach (var line in master.VerticalLines)
	chart.VerticalLines.Add(line.Clone());
}
}
}

	/// <summary>
	/// Vertical line parameters.
	/// </summary>
	public class VerticalLineState
	{
	/// <summary>Line identifier.</summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>Time value for the line.</summary>
	public DateTime Time { get; set; } = DateTime.MinValue;

	/// <summary>Line color in ARGB format.</summary>
	public int Color { get; set; } = unchecked((int)0xFF0000FF);

	/// <summary>Line width.</summary>
	public int Width { get; set; } = 1;

	/// <summary>Line style identifier.</summary>
	public int Style { get; set; } = 0;

	/// <summary>Selection flag.</summary>
	public bool IsSelected { get; set; } = false;

	/// <summary>Creates a copy of the line.</summary>
	/// <returns>Cloned line.</returns>
	public VerticalLineState Clone() => (VerticalLineState)MemberwiseClone();
}

	/// <summary>
	/// Represents chart settings that can be synchronized.
	/// </summary>
	public class ChartState
	{
	/// <summary>Chart identifier.</summary>
	public long ChartId { get; set; } = 0;

	/// <summary>Index of the first visible bar.</summary>
	public int FirstVisibleBar { get; set; } = 0;

	/// <summary>Number of visible bars on the screen.</summary>
	public int WidthInBars { get; set; } = 0;

	/// <summary>Time frame of the chart.</summary>
	public TimeSpan TimeFrame { get; set; } = TimeSpan.Zero;

	/// <summary>Chart scaling factor.</summary>
	public int Scale { get; set; } = 0;

	/// <summary>Chart display mode.</summary>
	public int Mode { get; set; } = 0;

	/// <summary>Collection of vertical lines.</summary>
	public List<VerticalLineState> VerticalLines { get; } = new();
}
}
