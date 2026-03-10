using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on moving linear regression slope.
/// </summary>
public class MovingRegressionStrategy : Strategy
{
	private readonly StrategyParam<int> _degree;
	private readonly StrategyParam<int> _window;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _slopeThresholdPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _prices = new();
	private int _barIndex;
	private int _lastSignalBar = -1000000;

	/// <summary>
	/// Degree-like sensitivity multiplier.
	/// </summary>
	public int Degree
	{
		get => _degree.Value;
		set => _degree.Value = value;
	}

	/// <summary>
	/// Regression window length.
	/// </summary>
	public int Window
	{
		get => _window.Value;
		set => _window.Value = value;
	}

	/// <summary>
	/// Minimum finished candles between entries.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Minimum absolute slope in percent of close price.
	/// </summary>
	public decimal SlopeThresholdPercent
	{
		get => _slopeThresholdPercent.Value;
		set => _slopeThresholdPercent.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MovingRegressionStrategy"/> class.
	/// </summary>
	public MovingRegressionStrategy()
	{
		_degree = Param(nameof(Degree), 2)
			.SetRange(0, 5)
			.SetDisplay("Degree", "Sensitivity multiplier", "General");

		_window = Param(nameof(Window), 20)
			.SetRange(10, 200)
			.SetDisplay("Window", "Regression window length", "General");

		_cooldownBars = Param(nameof(CooldownBars), 8)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Finished candles between entries", "General");

		_slopeThresholdPercent = Param(nameof(SlopeThresholdPercent), 0.005m)
			.SetGreaterThanZero()
			.SetDisplay("Slope Threshold %", "Min slope absolute value in percent", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prices.Clear();
		_barIndex = 0;
		_lastSignalBar = -1000000;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var dummyEma1 = new ExponentialMovingAverage { Length = 10 };
		var dummyEma2 = new ExponentialMovingAverage { Length = 20 };
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(dummyEma1, dummyEma2, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal d1, decimal d2)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		_prices.Add(candle.ClosePrice);
		if (_prices.Count > Window)
			_prices.RemoveAt(0);

		if (_prices.Count < Window)
			return;

		var slope = CalculateSlope(_prices);
		var slopePercent = candle.ClosePrice != 0m
			? slope / candle.ClosePrice * 100m
			: 0m;
		var threshold = SlopeThresholdPercent * (1m + Degree * 0.05m);
		var canSignal = _barIndex - _lastSignalBar >= CooldownBars;

		if (canSignal && slopePercent > threshold && Position <= 0)
		{
			BuyMarket();
			_lastSignalBar = _barIndex;
		}
		else if (canSignal && slopePercent < -threshold && Position >= 0)
		{
			SellMarket();
			_lastSignalBar = _barIndex;
		}
	}

	private static decimal CalculateSlope(IReadOnlyList<decimal> prices)
	{
		var n = prices.Count;
		if (n < 2)
			return 0m;

		double sumX = 0d;
		double sumY = 0d;
		double sumXX = 0d;
		double sumXY = 0d;

		for (var i = 0; i < n; i++)
		{
			var x = (double)i;
			var y = (double)prices[i];

			sumX += x;
			sumY += y;
			sumXX += x * x;
			sumXY += x * y;
		}

		var denominator = n * sumXX - sumX * sumX;
		if (denominator == 0d)
			return 0m;

		var slope = (n * sumXY - sumX * sumY) / denominator;
		return (decimal)slope;
	}
}
