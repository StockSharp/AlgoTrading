using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Opening range breakout with pivot point trailing stop.
/// Uses a rolling high/low channel for breakout entry and pivot-based trailing stop for exits.
/// </summary>
public class LongOnlyOpeningRangeBreakoutWithPivotPointsStrategy : Strategy
{
	public enum SlTypes
	{
		Percentage,
		PreviousLow
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rangeBars;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<SlTypes> _initialSlType;
	private readonly StrategyParam<int> _pivotLength;

	private Highest _highest;
	private Lowest _lowest;
	private decimal _entryPrice;
	private decimal _sl0;
	private decimal _trailStop;
	private int _cooldown;
	private bool _prevReady;
	private decimal _prevHighest;
	private decimal _prevLowest;

	// Pivot levels
	private decimal _r1;
	private decimal _r2;
	private decimal _s1;
	private decimal _s2;

	// For pivot calc
	private decimal _pivotHigh;
	private decimal _pivotLow;
	private decimal _pivotClose;
	private int _pivotBarCount;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RangeBars { get => _rangeBars.Value; set => _rangeBars.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public SlTypes InitialSlType { get => _initialSlType.Value; set => _initialSlType.Value = value; }
	public int PivotLength { get => _pivotLength.Value; set => _pivotLength.Value = value; }

	public LongOnlyOpeningRangeBreakoutWithPivotPointsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Working candle type", "General");

		_rangeBars = Param(nameof(RangeBars), 20)
			.SetGreaterThanZero()
			.SetDisplay("Range Bars", "Lookback bars for channel", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 5m)
			.SetDisplay("Stop Loss %", "Initial stop loss percent", "Risk");

		_initialSlType = Param(nameof(InitialSlType), SlTypes.Percentage)
			.SetDisplay("Initial SL Type", "Initial stop loss type", "Risk");

		_pivotLength = Param(nameof(PivotLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Pivot Length", "Bars for pivot calculation", "Indicators");
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

		_entryPrice = default;
		_sl0 = default;
		_trailStop = default;
		_cooldown = default;
		_prevReady = false;
		_prevHighest = default;
		_prevLowest = default;
		_r1 = default;
		_r2 = default;
		_s1 = default;
		_s2 = default;
		_pivotHigh = default;
		_pivotLow = default;
		_pivotClose = default;
		_pivotBarCount = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highest = new Highest { Length = RangeBars };
		_lowest = new Lowest { Length = RangeBars };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_highest, _lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		// Update pivot levels every PivotLength bars
		_pivotBarCount++;
		_pivotHigh = _pivotHigh == 0 ? candle.HighPrice : Math.Max(_pivotHigh, candle.HighPrice);
		_pivotLow = _pivotLow == 0 ? candle.LowPrice : Math.Min(_pivotLow, candle.LowPrice);
		_pivotClose = candle.ClosePrice;

		if (_pivotBarCount >= PivotLength)
		{
			var pivot = (_pivotHigh + _pivotLow + _pivotClose) / 3m;

			_r1 = pivot + pivot - _pivotLow;
			_r2 = pivot + (_pivotHigh - _pivotLow);
			_s1 = pivot + pivot - _pivotHigh;
			_s2 = pivot - (_pivotHigh - _pivotLow);

			_pivotHigh = 0;
			_pivotLow = 0;
			_pivotBarCount = 0;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevHighest = highestValue;
			_prevLowest = lowestValue;
			_prevReady = true;
			return;
		}

		// Long breakout entry
		if (_prevReady && Position <= 0 && candle.ClosePrice > _prevHighest && _r1 > 0)
		{
			if (Position < 0)
				BuyMarket();

			_entryPrice = candle.ClosePrice;
			_sl0 = _entryPrice * (1m - StopLossPercent / 100m);
			_trailStop = 0m;
			BuyMarket();
			_cooldown = 40;
		}
		// Short breakdown entry
		else if (_prevReady && Position >= 0 && candle.ClosePrice < _prevLowest && _s1 > 0)
		{
			if (Position > 0)
				SellMarket();

			_entryPrice = candle.ClosePrice;
			_sl0 = _entryPrice * (1m + StopLossPercent / 100m);
			_trailStop = 0m;
			SellMarket();
			_cooldown = 40;
		}

		// Trailing stop for long position
		if (Position > 0 && _r1 > 0)
		{
			if (candle.HighPrice > _r2)
				_trailStop = Math.Max(_trailStop, _r1);
			else if (candle.HighPrice > _r1)
				_trailStop = Math.Max(_trailStop, highestValue);

			var sl = Math.Max(_sl0, _trailStop);

			if (candle.LowPrice <= sl)
			{
				SellMarket();
				_cooldown = 40;
			}
		}

		// Trailing stop for short position
		if (Position < 0 && _s1 > 0)
		{
			if (candle.LowPrice < _s2)
				_trailStop = _trailStop == 0 ? _s1 : Math.Min(_trailStop, _s1);
			else if (candle.LowPrice < _s1)
				_trailStop = _trailStop == 0 ? lowestValue : Math.Min(_trailStop, lowestValue);

			var sl = _trailStop > 0 ? Math.Min(_sl0, _trailStop) : _sl0;

			if (candle.HighPrice >= sl)
			{
				BuyMarket();
				_cooldown = 40;
			}
		}

		_prevHighest = highestValue;
		_prevLowest = lowestValue;
		_prevReady = true;
	}
}
