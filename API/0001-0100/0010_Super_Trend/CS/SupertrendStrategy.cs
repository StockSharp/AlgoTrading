using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Supertrend indicator.
/// It turns long when the Supertrend line flips below price and short when it flips above, reversing on every flip.
/// </summary>
public class SupertrendStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	// Trend of the previous candle; unknown until the indicator forms.
	private bool? _prevIsUpTrend;

	/// <summary>
	/// Period for Supertrend calculation.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Multiplier for Supertrend calculation.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
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
	/// Initialize the Supertrend strategy.
	/// </summary>
	public SupertrendStrategy()
	{
		_period = Param(nameof(Period), 10)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Period for Supertrend calculation", "Indicators")

			.SetOptimize(7, 21, 2);

		_multiplier = Param(nameof(Multiplier), 3.0m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Multiplier for Supertrend calculation", "Indicators")

			.SetOptimize(2.0m, 4.0m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevIsUpTrend = null;

	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Create custom supertrend indicator
		// Since StockSharp doesn't have a built-in Supertrend indicator,
		// we'll use ATR to calculate the basic components
		var supertrend = new SuperTrend { Length = Period, Multiplier = Multiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(supertrend, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue supertrendValue)
	{
		if (candle.State != CandleStates.Finished || !supertrendValue.IsFormed || supertrendValue is not SuperTrendIndicatorValue value)
			return;

		var isUpTrend = value.IsUpTrend;
		var wasUpTrend = _prevIsUpTrend;
		_prevIsUpTrend = isUpTrend;

		if (wasUpTrend is not bool wasUp || wasUp == isUpTrend)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var line = value.Value;

		if (isUpTrend && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			LogInfo($"Buy signal: Price ({candle.ClosePrice}) crossed above Supertrend ({line})");
		}
		else if (!isUpTrend && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			LogInfo($"Sell signal: Price ({candle.ClosePrice}) crossed below Supertrend ({line})");
		}
	}
}
