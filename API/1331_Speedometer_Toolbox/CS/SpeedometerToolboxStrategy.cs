using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Draws a speedometer-style gauge based on RSI value.
/// Visualization only.
/// </summary>
public class SpeedometerToolboxStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _radius;
	private readonly StrategyParam<DataType> _candleType;

	private bool _gaugeDrawn;
	private IChartArea? _area;

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Radius in bars for gauge drawing.
	/// </summary>
	public int Radius
	{
		get => _radius.Value;
		set => _radius.Value = value;
	}

	/// <summary>
	/// Candle type used to get time information.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="SpeedometerToolboxStrategy"/>.
	/// </summary>
	public SpeedometerToolboxStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Period of RSI", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_radius = Param(nameof(Radius), 20)
			.SetGreaterThanZero()
			.SetDisplay("Radius", "Radius in bars", "Drawing");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for timing", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		_area = CreateChartArea();
		if (_area != null)
			DrawCandles(_area, subscription);
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var radius = Radius;
		var xAxis = new DateTimeOffset[radius * 2];
		var step = (TimeSpan)CandleType.Arg;

		for (var i = 0; i < xAxis.Length; i++)
			xAxis[i] = candle.OpenTime + TimeSpan.FromTicks(step.Ticks * (i - radius + 1));

		const decimal yAxis = 50m;
		const decimal yScale = 50m;

		var left = xAxis[0];
		var right = xAxis[^1];
		var center = xAxis[radius - 1];

		if (!_gaugeDrawn)
		{
			DrawBaseLine(left, right, yAxis);

			for (var s = 1; s <= 5; s++)
				DrawSector(s, 5, 20, radius, xAxis, yAxis, yScale, 2);

			for (var t = 1; t <= 4; t++)
				DrawTick(t, 5, 8m, radius, xAxis, yAxis, yScale, 3);

			for (var t = 1; t < 10; t += 2)
				DrawTick(t, 10, 6m, radius, xAxis, yAxis, yScale, 1);

			DrawSectorLabel(1, 10, radius, xAxis, yAxis, yScale, "Strong Buy");
			DrawSectorLabel(3, 10, radius, xAxis, yAxis, yScale, "Buy");
			DrawSectorLabel(5, 10, radius, xAxis, yAxis, yScale, "Neutral");
			DrawSectorLabel(7, 10, radius, xAxis, yAxis, yScale, "Sell");
			DrawSectorLabel(9, 10, radius, xAxis, yAxis, yScale, "Strong Sell");

			_gaugeDrawn = true;
		}

		DrawNeedle(rsi, center, radius, xAxis, yAxis, yScale, 1);
		DrawTitleLabel(center, yAxis, yScale, $"RSI ({RsiLength}): {rsi:F2}");
	}

	private void DrawBaseLine(DateTimeOffset left, DateTimeOffset right, decimal y)
	{
		DrawLine(left, y, right, y);
	}

	private void DrawSector(int sectorNum, int totalSectors, int lineLimit, int radius, DateTimeOffset[] xAxis, decimal yAxis, decimal yScale, int width)
	{
		var segmentsPerSector = lineLimit / totalSectors;
		var totalSegments = segmentsPerSector * totalSectors;
		var radiansPerSegment = Math.PI / totalSegments;
		var radiansPerSector = Math.PI / totalSectors;
		var start = radiansPerSector * (sectorNum - 1);

		for (var i = 0; i < segmentsPerSector; i++)
		{
			var angle1 = start + radiansPerSegment * i;
			var angle2 = start + radiansPerSegment * (i + 1);

			var index1 = (int)Math.Round(Math.Cos(angle1) * (radius - 1) + radius - 1);
			var index2 = (int)Math.Round(Math.Cos(angle2) * (radius - 1) + radius - 1);

			var x1 = xAxis[index1];
			var x2 = xAxis[index2];

			var y1 = yAxis + (decimal)Math.Sin(angle1) * yScale;
			var y2 = yAxis + (decimal)Math.Sin(angle2) * yScale;

			DrawLine(x1, y1, x2, y2, thickness: width);
		}
	}

	private void DrawNeedle(decimal val, DateTimeOffset center, int radius, DateTimeOffset[] xAxis, decimal yAxis, decimal yScale, int width)
	{
		var angle = Math.PI / 100 * (double)val;
		var index = (int)Math.Round(Math.Cos(angle) * (radius - 1) + radius - 1);
		var x1 = xAxis[index];
		var y1 = yAxis + (decimal)Math.Sin(angle) * yScale;
		DrawLine(x1, y1, center, yAxis, thickness: width);
	}

	private void DrawTick(int num, int divisions, decimal radiusPerc, int radius, DateTimeOffset[] xAxis, decimal yAxis, decimal yScale, int width)
	{
		var pos = Math.PI / divisions * num;
		var index1 = (int)Math.Round(Math.Cos(pos) * (radius - 1) + radius - 1);
		var index2 = (int)Math.Round(Math.Cos(pos) * (radius - 1) * (double)(1 - radiusPerc / 100) + radius - 1);

		var x1 = xAxis[index1];
		var x2 = xAxis[index2];

		var y1 = yAxis + (decimal)Math.Sin(pos) * yScale;
		var y2 = yAxis + (decimal)Math.Sin(pos) * yScale * (1 - radiusPerc / 100);

		DrawLine(x1, y1, x2, y2, thickness: width);
	}

	private void DrawSectorLabel(int num, int divisions, int radius, DateTimeOffset[] xAxis, decimal yAxis, decimal yScale, string text)
	{
		if (_area == null)
			return;

		var pos = Math.PI / divisions * num;
		var index = (int)Math.Round(Math.Cos(pos) * (radius - 1) + radius - 1);
		var x = xAxis[index];
		var y = yAxis + (decimal)Math.Sin(pos) * yScale;
		DrawText(_area, x, y, text);
	}

	private void DrawTitleLabel(DateTimeOffset center, decimal yAxis, decimal yScale, string text)
	{
		if (_area == null)
			return;

		var y = yAxis - 0.25m * yScale;
		DrawText(_area, center, y, text);
	}
}
