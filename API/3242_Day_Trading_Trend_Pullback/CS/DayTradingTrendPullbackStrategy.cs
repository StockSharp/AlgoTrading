using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Day Trading Trend Pullback strategy: uses EMA20/EMA100 for trend and
/// enters on pullbacks to EMA20 in the direction of EMA100 trend.
/// </summary>
public class DayTradingTrendPullbackStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _shortEmaPeriod;
	private readonly StrategyParam<int> _longEmaPeriod;

	private decimal _prevClose;
	private bool _hasPrev;

	/// <summary>
	/// Constructor.
	/// </summary>
	public DayTradingTrendPullbackStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_shortEmaPeriod = Param(nameof(ShortEmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA", "Short EMA for pullback detection", "Indicators");

		_longEmaPeriod = Param(nameof(LongEmaPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA", "Long EMA for trend direction", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int ShortEmaPeriod
	{
		get => _shortEmaPeriod.Value;
		set => _shortEmaPeriod.Value = value;
	}

	public int LongEmaPeriod
	{
		get => _longEmaPeriod.Value;
		set => _longEmaPeriod.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var ema20 = new EMA { Length = ShortEmaPeriod };
		var ema100 = new EMA { Length = LongEmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema20, ema100, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema20);
			DrawIndicator(area, ema100);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema20, decimal ema100)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var bullishTrend = ema20 > ema100;
		var bearishTrend = ema20 < ema100;

		if (_hasPrev)
		{
			// Pullback buy: bullish trend, previous close was below ema20, now above
			if (bullishTrend && _prevClose < ema20 && close > ema20 && Position <= 0)
			{
				BuyMarket();
			}
			// Pullback sell: bearish trend, previous close was above ema20, now below
			else if (bearishTrend && _prevClose > ema20 && close < ema20 && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevClose = close;
		_hasPrev = true;
	}
}
