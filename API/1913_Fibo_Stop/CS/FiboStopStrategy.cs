using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that uses Fibonacci retracement levels for entry and trailing stop.
/// Enters on pullback to Fibonacci level and trails stop along levels.
/// </summary>
public class FiboStopStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _entryFiboLevel;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _highestHigh;
	private decimal _lowestLow;
	private int _barCount;
	private int _barsSinceTrade;
	private bool _rangeSet;
	private decimal _entryPrice;

	public int LookbackPeriod { get => _lookbackPeriod.Value; set => _lookbackPeriod.Value = value; }
	public decimal EntryFiboLevel { get => _entryFiboLevel.Value; set => _entryFiboLevel.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FiboStopStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 120)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Bars to calculate high/low range", "General");

		_entryFiboLevel = Param(nameof(EntryFiboLevel), 0.382m)
			.SetDisplay("Entry Fibo", "Fibonacci level for entry (0.236, 0.382, 0.5, 0.618)", "Fibonacci");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 20)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Bars between new entries", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
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
		_highestHigh = decimal.MinValue;
		_lowestLow = decimal.MaxValue;
		_barCount = 0;
		_barsSinceTrade = CooldownBars;
		_rangeSet = false;
		_entryPrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highestHigh = decimal.MinValue;
		_lowestLow = decimal.MaxValue;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent),
			takeProfit: null
		);

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

		_barCount++;
		_barsSinceTrade++;

		if (candle.HighPrice > _highestHigh)
			_highestHigh = candle.HighPrice;
		if (candle.LowPrice < _lowestLow)
			_lowestLow = candle.LowPrice;

		if (_barCount < LookbackPeriod)
			return;

		if (!_rangeSet)
		{
			_rangeSet = true;
			return;
		}

		var range = _highestHigh - _lowestLow;
		if (range <= 0)
			return;

		// Calculate Fibonacci retracement levels from high
		var fiboLevel = _highestHigh - range * EntryFiboLevel;
		var fibo618 = _highestHigh - range * 0.618m;

		if (Position == 0)
		{
			if (_barsSinceTrade < CooldownBars)
				return;

			if (candle.ClosePrice <= fiboLevel && candle.ClosePrice > fibo618)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_barsSinceTrade = 0;
			}
			else if (candle.ClosePrice >= _lowestLow + range * (1m - EntryFiboLevel) && candle.ClosePrice < _lowestLow + range * 0.382m)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_barsSinceTrade = 0;
			}
		}
		else if (Position > 0)
		{
			// Close long if price breaks below 61.8% retracement
			if (candle.ClosePrice < fibo618)
			{
				SellMarket();
			}
		}
		else if (Position < 0)
		{
			// Close short if price breaks above 38.2% retracement from bottom
			if (candle.ClosePrice > _lowestLow + range * 0.618m)
			{
				BuyMarket();
			}
		}

		// Update rolling high/low
		if (_barCount > LookbackPeriod * 2)
		{
			_highestHigh = candle.HighPrice;
			_lowestLow = candle.LowPrice;
			_barCount = LookbackPeriod;
		}
	}
}
