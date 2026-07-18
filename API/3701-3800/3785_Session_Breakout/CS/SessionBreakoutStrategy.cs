using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Session breakout strategy that tracks a range during first hours and trades breakouts later.
/// Accumulates high/low during range hours (0-8), then trades breakouts during active hours (8-20).
/// </summary>
public class SessionBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _rangeStartHour;
	private readonly StrategyParam<int> _rangeEndHour;
	private readonly StrategyParam<int> _tradeEndHour;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentDate;
	private decimal? _rangeHigh;
	private decimal? _rangeLow;
	private bool _rangeComplete;
	private bool _tradedToday;

	public int RangeStartHour { get => _rangeStartHour.Value; set => _rangeStartHour.Value = value; }
	public int RangeEndHour { get => _rangeEndHour.Value; set => _rangeEndHour.Value = value; }
	public int TradeEndHour { get => _tradeEndHour.Value; set => _tradeEndHour.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SessionBreakoutStrategy()
	{
		_rangeStartHour = Param(nameof(RangeStartHour), 0)
			.SetDisplay("Range Start", "Hour to start tracking range", "Sessions");

		_rangeEndHour = Param(nameof(RangeEndHour), 8)
			.SetDisplay("Range End", "Hour to stop tracking range", "Sessions");

		_tradeEndHour = Param(nameof(TradeEndHour), 20)
			.SetDisplay("Trade End", "Hour to stop trading", "Sessions");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
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

		_currentDate = default;
		_rangeHigh = null;
		_rangeLow = null;
		_rangeComplete = false;
		_tradedToday = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_currentDate = default;
		_rangeHigh = null;
		_rangeLow = null;
		_rangeComplete = false;
		_tradedToday = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var candleDate = candle.OpenTime.Date;
		var hour = candle.OpenTime.Hour;

		// Reset on new day
		if (candleDate != _currentDate)
		{
			_currentDate = candleDate;
			_rangeHigh = null;
			_rangeLow = null;
			_rangeComplete = false;
			_tradedToday = false;
		}

		// Build range during accumulation hours
		if (hour >= RangeStartHour && hour < RangeEndHour)
		{
			if (_rangeHigh == null || candle.HighPrice > _rangeHigh)
				_rangeHigh = candle.HighPrice;
			if (_rangeLow == null || candle.LowPrice < _rangeLow)
				_rangeLow = candle.LowPrice;
			return;
		}

		// Mark range as complete
		if (!_rangeComplete && hour >= RangeEndHour && _rangeHigh != null && _rangeLow != null)
			_rangeComplete = true;

		if (!_rangeComplete || _rangeHigh == null || _rangeLow == null)
			return;

		// Trade during active hours
		if (hour >= RangeEndHour && hour < TradeEndHour)
		{
			// Breakout above range high - buy
			if (Position <= 0 && candle.ClosePrice > _rangeHigh.Value && !_tradedToday)
			{
				if (Position < 0)
					BuyMarket();
				BuyMarket();
				_tradedToday = true;
			}
			// Breakout below range low - sell
			else if (Position >= 0 && candle.ClosePrice < _rangeLow.Value && !_tradedToday)
			{
				if (Position > 0)
					SellMarket();
				SellMarket();
				_tradedToday = true;
			}
		}

		// Close at end of day
		if (hour >= TradeEndHour && Position != 0)
		{
			if (Position > 0)
				SellMarket();
			else
				BuyMarket();
		}
	}
}
