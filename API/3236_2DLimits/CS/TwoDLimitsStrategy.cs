using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 2DLimits strategy: trades daily range breakouts when last two candles align in trend.
/// Buys when price breaks above prior high with upward bias, sells when below prior low with downward bias.
/// </summary>
public class TwoDLimitsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lookbackBars;

	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _olderHigh;
	private decimal _olderLow;
	private bool _hasPrev;
	private bool _hasOlder;

	/// <summary>
	/// Constructor.
	/// </summary>
	public TwoDLimitsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_lookbackBars = Param(nameof(LookbackBars), 12)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Bars", "Number of bars per range period", "Signals");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int LookbackBars
	{
		get => _lookbackBars.Value;
		set => _lookbackBars.Value = value;
	}

	private decimal _periodHigh;
	private decimal _periodLow;
	private int _barCount;

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;
		_hasOlder = false;
		_barCount = 0;
		_periodHigh = decimal.MinValue;
		_periodLow = decimal.MaxValue;

		var sma = new SMA { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Track period high/low
		if (candle.HighPrice > _periodHigh) _periodHigh = candle.HighPrice;
		if (candle.LowPrice < _periodLow) _periodLow = candle.LowPrice;
		_barCount++;

		if (_barCount >= LookbackBars)
		{
			// Period complete, shift
			_hasOlder = _hasPrev;
			_olderHigh = _prevHigh;
			_olderLow = _prevLow;
			_prevHigh = _periodHigh;
			_prevLow = _periodLow;
			_hasPrev = true;

			_periodHigh = decimal.MinValue;
			_periodLow = decimal.MaxValue;
			_barCount = 0;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_hasPrev || !_hasOlder)
			return;

		// Two-period bias: upward = higher highs and higher lows
		var hasLongBias = _prevHigh > _olderHigh && _prevLow > _olderLow;
		var hasShortBias = _prevHigh < _olderHigh && _prevLow < _olderLow;

		// Breakout entry
		if (hasLongBias && candle.ClosePrice > _prevHigh && Position <= 0)
		{
			BuyMarket();
		}
		else if (hasShortBias && candle.ClosePrice < _prevLow && Position >= 0)
		{
			SellMarket();
		}
		// Mean reversion exit: price returns to middle
		else if (Position > 0)
		{
			var mid = (_prevHigh + _prevLow) / 2m;
			if (candle.ClosePrice < mid)
				SellMarket();
		}
		else if (Position < 0)
		{
			var mid = (_prevHigh + _prevLow) / 2m;
			if (candle.ClosePrice > mid)
				BuyMarket();
		}
	}
}
