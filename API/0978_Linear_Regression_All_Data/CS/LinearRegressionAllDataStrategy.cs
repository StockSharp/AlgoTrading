using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Linear Regression (All Data) strategy.
/// Calculates linear regression using all available bars and draws the regression line.
/// </summary>
public class LinearRegressionAllDataStrategy : Strategy
{
	private readonly StrategyParam<int> _maxBarsBack;
	private readonly StrategyParam<DataType> _candleType;

	private long _index;
	private decimal _sumX;
	private decimal _sumY;
	private decimal _sumX2;
	private decimal _sumY2;
	private decimal _sumXY;
	private readonly Queue<DateTimeOffset> _times = new();

	/// <summary>
	/// Maximum number of bars used for line drawing.
	/// </summary>
	public int MaxBarsBack
	{
		get => _maxBarsBack.Value;
		set => _maxBarsBack.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="LinearRegressionAllDataStrategy"/>.
	/// </summary>
	public LinearRegressionAllDataStrategy()
	{
		_maxBarsBack = Param(nameof(MaxBarsBack), 5000)
			.SetGreaterThanZero()
			.SetDisplay("Max Bars Back", "Maximum number of bars for drawing", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_index = 0;
		_sumX = 0m;
		_sumY = 0m;
		_sumX2 = 0m;
		_sumY2 = 0m;
		_sumXY = 0m;
		_times.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
			DrawCandles(area, subscription);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_index++;

		var x = (decimal)_index;
		var y = candle.ClosePrice;

		_sumX += x;
		_sumY += y;
		_sumX2 += x * x;
		_sumY2 += y * y;
		_sumXY += x * y;

		_times.Enqueue(candle.OpenTime);
		if (_times.Count > MaxBarsBack)
			_times.Dequeue();

		if (_index < 2)
			return;

		var denom = _index * _sumX2 - _sumX * _sumX;
		if (denom == 0)
			return;

		var slope = (_index * _sumXY - _sumX * _sumY) / denom;
		var intercept = (_sumY - slope * _sumX) / _index;

		var varY = _index * _sumY2 - _sumY * _sumY;
		var varX = denom;

		var rNum = (double)(_index * _sumXY - _sumX * _sumY);
		var rDen = Math.Sqrt((double)(varY * varX));
		if (rDen == 0)
			return;

		var r = rNum / rDen;
		var r2 = r * r;

		var startIndex = _index > MaxBarsBack ? _index - MaxBarsBack : 0;
		var startX = (decimal)startIndex;
		var startY = slope * startX + intercept;
		var endY = slope * (_index - 1) + intercept;
		var startTime = _times.Peek();

		DrawLine(startTime, startY, candle.OpenTime, endY);

		LogInfo($"Slope: {slope}, Intercept: {intercept}, r: {r:F3}, r2: {r2:F3}");
	}
}

