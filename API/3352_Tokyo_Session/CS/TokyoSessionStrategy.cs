namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Tokyo Session strategy: breakout from session range.
/// Tracks high/low during first hours, then buys breakout above high, sells below low.
/// </summary>
public class TokyoSessionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;

	private decimal _sessionHigh;
	private decimal _sessionLow;
	private int _candleCount;
	private bool _rangeSet;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public TokyoSessionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for volatility filter", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_candleCount = 0;
		_rangeSet = false;
		_sessionHigh = decimal.MinValue;
		_sessionLow = decimal.MaxValue;

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_candleCount++;

		// Build range from first 12 candles (1 hour of 5min candles)
		if (_candleCount <= 12)
		{
			if (candle.HighPrice > _sessionHigh)
				_sessionHigh = candle.HighPrice;
			if (candle.LowPrice < _sessionLow)
				_sessionLow = candle.LowPrice;

			if (_candleCount == 12)
				_rangeSet = true;

			return;
		}

		if (!_rangeSet)
			return;

		// Reset range every 288 candles (24 hours of 5min candles)
		if (_candleCount % 288 == 0)
		{
			_sessionHigh = candle.HighPrice;
			_sessionLow = candle.LowPrice;
			_rangeSet = false;
			_candleCount = 0;
			return;
		}

		var close = candle.ClosePrice;

		// Breakout above session high
		if (close > _sessionHigh && Position <= 0)
			BuyMarket();
		// Breakout below session low
		else if (close < _sessionLow && Position >= 0)
			SellMarket();
	}
}
