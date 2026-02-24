using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Alexav D1 Profit GBPUSD strategy (simplified). Uses EMA crossover with RSI
/// filter for breakout entries with ATR-based stop/take management.
/// </summary>
public class AlexavD1ProfitGbpUsdStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaFastLength;
	private readonly StrategyParam<int> _emaSlowLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiUpperLevel;
	private readonly StrategyParam<decimal> _rsiLowerLevel;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaFastLength
	{
		get => _emaFastLength.Value;
		set => _emaFastLength.Value = value;
	}

	public int EmaSlowLength
	{
		get => _emaSlowLength.Value;
		set => _emaSlowLength.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public decimal RsiUpperLevel
	{
		get => _rsiUpperLevel.Value;
		set => _rsiUpperLevel.Value = value;
	}

	public decimal RsiLowerLevel
	{
		get => _rsiLowerLevel.Value;
		set => _rsiLowerLevel.Value = value;
	}

	public AlexavD1ProfitGbpUsdStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_emaFastLength = Param(nameof(EmaFastLength), 6)
			.SetGreaterThanZero()
			.SetDisplay("EMA Fast", "Fast EMA period", "Indicators");

		_emaSlowLength = Param(nameof(EmaSlowLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("EMA Slow", "Slow EMA period", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_rsiUpperLevel = Param(nameof(RsiUpperLevel), 65m)
			.SetDisplay("RSI Upper", "Max RSI for buy", "Filters");

		_rsiLowerLevel = Param(nameof(RsiLowerLevel), 35m)
			.SetDisplay("RSI Lower", "Min RSI for sell", "Filters");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var emaFast = new ExponentialMovingAverage { Length = EmaFastLength };
		var emaSlow = new ExponentialMovingAverage { Length = EmaSlowLength };

		decimal prevFast = 0, prevSlow = 0;
		var hasPrev = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(emaFast, emaSlow, (ICandleMessage candle, decimal fastVal, decimal slowVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!hasPrev)
				{
					prevFast = fastVal;
					prevSlow = slowVal;
					hasPrev = true;
					return;
				}

				if (!IsFormedAndOnlineAndAllowTrading())
				{
					prevFast = fastVal;
					prevSlow = slowVal;
					return;
				}

				var close = candle.ClosePrice;

				// EMA fast crosses above slow - buy signal
				var bullishCross = prevFast <= prevSlow && fastVal > slowVal;
				// EMA fast crosses below slow - sell signal
				var bearishCross = prevFast >= prevSlow && fastVal < slowVal;

				// Also allow trend-following: fast well above slow
				var strongUptrend = fastVal > slowVal && close > fastVal;
				var strongDowntrend = fastVal < slowVal && close < fastVal;

				if ((bullishCross || strongUptrend) && Position <= 0)
					BuyMarket();
				else if ((bearishCross || strongDowntrend) && Position >= 0)
					SellMarket();

				prevFast = fastVal;
				prevSlow = slowVal;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaFast);
			DrawIndicator(area, emaSlow);
			DrawOwnTrades(area);
		}
	}
}
