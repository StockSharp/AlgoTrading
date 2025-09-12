using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using a responsive linear regression channel.
/// Adjusts lookback based on timeframe.
/// </summary>
public class ResponsiveLinearRegressionChannelsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<bool> _useSmartLookback;
	private readonly StrategyParam<decimal> _deviationMultiplier;
	private readonly StrategyParam<bool> _useStandardDeviation;

	private readonly Queue<decimal> _closes = new();
	private const int MaxBars = 3000;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Number of bars for fixed lookback.
	/// </summary>
	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }

	/// <summary>
	/// Enable smart lookback based on timeframe.
	/// </summary>
	public bool UseSmartLookback { get => _useSmartLookback.Value; set => _useSmartLookback.Value = value; }

	/// <summary>
	/// Channel width multiplier.
	/// </summary>
	public decimal DeviationMultiplier { get => _deviationMultiplier.Value; set => _deviationMultiplier.Value = value; }

	/// <summary>
	/// Use standard deviation instead of RMSE.
	/// </summary>
	public bool UseStandardDeviation { get => _useStandardDeviation.Value; set => _useStandardDeviation.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ResponsiveLinearRegressionChannelsStrategy"/> class.
	/// </summary>
	public ResponsiveLinearRegressionChannelsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_lookback = Param(nameof(Lookback), 100)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Bars", "Fixed lookback size", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 50);

		_useSmartLookback = Param(nameof(UseSmartLookback), true)
			.SetDisplay("Use Smart Lookback", "Adjust period to timeframe", "Parameters");

		_deviationMultiplier = Param(nameof(DeviationMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation Multiplier", "Channel width", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.5m);

		_useStandardDeviation = Param(nameof(UseStandardDeviation), false)
			.SetDisplay("Use Standard Deviation", "Use standard deviation instead of RMSE", "Parameters");
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
		_closes.Clear();
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
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closes.Enqueue(candle.ClosePrice);
		if (_closes.Count > MaxBars)
			_closes.Dequeue();

		var period = UseSmartLookback ? CalculatePeriod() : Lookback;
		if (period < 2 || _closes.Count < period)
			return;

		var data = _closes.ToArray();
		var start = data.Length - period;

		decimal sumX = 0;
		decimal sumY = 0;
		decimal sumXY = 0;
		decimal sumX2 = 0;

		for (var i = 0; i < period; i++)
		{
			var x = (decimal)i;
			var y = data[start + i];
			sumX += x;
			sumY += y;
			sumXY += x * y;
			sumX2 += x * x;
		}

		var denom = period * sumX2 - sumX * sumX;
		if (denom == 0)
			return;

		var slope = (period * sumXY - sumX * sumY) / denom;
		var intercept = (sumY - slope * sumX) / period;
		var lastX = period - 1;
		var line = intercept + slope * lastX;

		decimal deviation;
		if (UseStandardDeviation)
		{
			var mean = sumY / period;
			decimal sq = 0;
			for (var i = 0; i < period; i++)
			{
				var y = data[start + i];
				var diff = y - mean;
				sq += diff * diff;
			}
			deviation = (decimal)Math.Sqrt((double)(sq / period));
		}
		else
		{
			decimal sq = 0;
			for (var i = 0; i < period; i++)
			{
				var x = (decimal)i;
				var y = data[start + i];
				var fitted = intercept + slope * x;
				var diff = y - fitted;
				sq += diff * diff;
			}
			deviation = (decimal)Math.Sqrt((double)(sq / period));
		}

		var upper = line + deviation * DeviationMultiplier;
		var lower = line - deviation * DeviationMultiplier;

		if (slope > 0 && candle.ClosePrice < lower && Position <= 0)
		{
			BuyMarket();
		}
		else if (slope < 0 && candle.ClosePrice > upper && Position >= 0)
		{
			SellMarket();
		}
		else if (Position > 0 && candle.ClosePrice >= line)
		{
			SellMarket();
		}
		else if (Position < 0 && candle.ClosePrice <= line)
		{
			BuyMarket();
		}
	}

	private int CalculatePeriod()
	{
		var tf = (TimeSpan)CandleType.Arg;

		if (tf < TimeSpan.FromDays(1))
		{
			var minutes = (int)tf.TotalMinutes;
			return minutes switch
			{
				1 => DaysToBars(1),
				3 or 5 => DaysToBars(2),
				10 => DaysToBars(3),
				15 => WeeksToBars(1),
				30 => WeeksToBars(2),
				60 => WeeksToBars(4),
				120 => WeeksToBars(8),
				180 => WeeksToBars(13),
				195 or 240 => WeeksToBars(25),
				360 => WeeksToBars(36),
				_ => DaysToBars(10),
			};
		}

		if (tf == TimeSpan.FromDays(1))
			return WeeksToBars(50);

		if (tf == TimeSpan.FromDays(7))
			return 104;

		if (tf >= TimeSpan.FromDays(28))
			return 60;

		return Lookback;
	}

	private int DaysToBars(int days)
	{
		var tf = (TimeSpan)CandleType.Arg;
		var perDay = (int)(TimeSpan.FromDays(1).Ticks / tf.Ticks);
		return days * perDay;
	}

	private int WeeksToBars(int weeks)
	{
		return DaysToBars(weeks * 7);
	}
}

