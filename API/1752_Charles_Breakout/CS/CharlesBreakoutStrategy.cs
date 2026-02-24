using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy based on daily high/low with RSI and EMA trend filter.
/// </summary>
public class CharlesBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _delta;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _rsiPeriod;

	private decimal _dailyHigh;
	private decimal _dailyLow;
	private decimal _prevDayHigh;
	private decimal _prevDayLow;
	private DateTime _currentDate;

	public decimal Delta { get => _delta.Value; set => _delta.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	public CharlesBreakoutStrategy()
	{
		_delta = Param(nameof(Delta), 50m)
			.SetDisplay("Price Offset", "Offset from daily high/low", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_fastPeriod = Param(nameof(FastPeriod), 18)
			.SetDisplay("Fast EMA Period", "Fast EMA length", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 60)
			.SetDisplay("Slow EMA Period", "Slow EMA length", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI length", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new EMA { Length = FastPeriod };
		var slowEma = new EMA { Length = SlowPeriod };
		var rsi = new RSI { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastEma, slowEma, rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastEma, decimal slowEma, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var date = candle.OpenTime.Date;
		if (date != _currentDate)
		{
			_prevDayHigh = _dailyHigh;
			_prevDayLow = _dailyLow;
			_currentDate = date;
			_dailyHigh = candle.HighPrice;
			_dailyLow = candle.LowPrice;
		}
		else
		{
			if (candle.HighPrice > _dailyHigh)
				_dailyHigh = candle.HighPrice;
			if (candle.LowPrice < _dailyLow)
				_dailyLow = candle.LowPrice;
		}

		if (_prevDayHigh == 0)
			return;

		var upperLevel = _prevDayHigh + Delta;
		var lowerLevel = _prevDayLow - Delta;

		var bullish = rsi > 55 && fastEma > slowEma;
		var bearish = rsi < 45 && fastEma < slowEma;

		if (bullish && candle.ClosePrice > upperLevel && Position <= 0)
			BuyMarket();
		else if (bearish && candle.ClosePrice < lowerLevel && Position >= 0)
			SellMarket();
	}
}
