using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CC Trend Strategy 2 Downtrend Short.
/// Short only. The Fibonacci range spans the lowest low and the highest high of the last FibLength candles, and its 0.236 level lies
/// 23.6% of the range below the high. A short opens when the previous close is below the Fibonacci high and EMA21 is below EMA55.
/// It closes when the close crosses above EMA200 while the trade is not losing, or when the previous close is above the 0.236 level
/// and there is no new short signal.
/// </summary>
public class CCTrend2DowntrendShortStrategy : Strategy
{
	private const int _fastEmaLength = 21;
	private const int _slowEmaLength = 55;
	private const int _trendEmaLength = 200;
	private const decimal _fibLevel = 0.236m;

	private readonly StrategyParam<int> _fibLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private decimal? _prevTrendEma;
	private decimal _entryPrice;

	/// <summary>
	/// Candles of the Fibonacci range.
	/// </summary>
	public int FibLength
	{
		get => _fibLength.Value;
		set => _fibLength.Value = value;
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
	/// Constructor.
	/// </summary>
	public CCTrend2DowntrendShortStrategy()
	{
		_fibLength = Param(nameof(FibLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("Fib Length", "Candles of the Fibonacci range", "Fibonacci");

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
		_prevClose = null;
		_prevTrendEma = null;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;
		_prevTrendEma = null;
		_entryPrice = 0m;

		var fastEma = new ExponentialMovingAverage { Length = _fastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = _slowEmaLength };
		var trendEma = new ExponentialMovingAverage { Length = _trendEmaLength };
		var highest = new Highest { Length = FibLength };
		var lowest = new Lowest { Length = FibLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastEma, slowEma, trendEma, highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawIndicator(area, trendEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue trendValue, IIndicatorValue highestValue, IIndicatorValue lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var prevClose = _prevClose;
		var prevTrendEma = _prevTrendEma;
		_prevClose = close;

		if (!trendValue.IsFormed)
			return;

		var trendEma = trendValue.GetValue<decimal>();
		_prevTrendEma = trendEma;

		if (!fastValue.IsFormed || !slowValue.IsFormed || !highestValue.IsFormed || !lowestValue.IsFormed)
			return;

		if (prevClose is not decimal pc || prevTrendEma is not decimal pt)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var fibHigh = highestValue.GetValue<decimal>();
		var fibLow = lowestValue.GetValue<decimal>();
		var fib236 = fibHigh - (fibHigh - fibLow) * _fibLevel;
		var shortSignal = pc < fibHigh && fastValue.GetValue<decimal>() < slowValue.GetValue<decimal>();

		if (Position < 0)
		{
			var crossAboveTrend = pc <= pt && close > trendEma;

			if ((crossAboveTrend && close <= _entryPrice) || (pc > fib236 && !shortSignal))
				BuyMarket(-Position);

			return;
		}

		if (Position == 0 && shortSignal)
		{
			SellMarket(Volume);
			_entryPrice = close;
		}
	}
}
