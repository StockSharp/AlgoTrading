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
/// Breakout strategy converted from the MQL Expert NEWS robot.
/// Detects breakouts above/below a reference price range and enters positions.
/// Uses stop loss and take profit for position management.
/// </summary>
public class ExpertNewsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _entryOffset;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private decimal _entryPrice;

	/// <summary>
	/// Entry offset from the high/low range.
	/// </summary>
	public decimal EntryOffset
	{
		get => _entryOffset.Value;
		set => _entryOffset.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Number of bars to determine high/low range.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ExpertNewsStrategy()
	{
		_entryOffset = Param(nameof(EntryOffset), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Offset", "Offset from range high/low for entry", "Parameters");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Bars for range calculation", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_highs.Clear();
		_lows.Clear();
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			new Unit(TakeProfit, UnitTypes.Absolute),
			new Unit(StopLoss, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		if (_highs.Count > LookbackPeriod + 1)
			_highs.RemoveAt(0);
		if (_lows.Count > LookbackPeriod + 1)
			_lows.RemoveAt(0);

		if (_highs.Count <= LookbackPeriod)
			return;

		// Compute range from prior bars (excluding current)
		var rangeHigh = decimal.MinValue;
		var rangeLow = decimal.MaxValue;
		for (int i = 0; i < _highs.Count - 1; i++)
		{
			if (_highs[i] > rangeHigh) rangeHigh = _highs[i];
			if (_lows[i] < rangeLow) rangeLow = _lows[i];
		}

		var close = candle.ClosePrice;
		var breakoutUp = close > rangeHigh + EntryOffset;
		var breakoutDown = close < rangeLow - EntryOffset;

		if (breakoutUp && Position <= 0)
		{
			BuyMarket();
			_entryPrice = close;
		}
		else if (breakoutDown && Position >= 0)
		{
			SellMarket();
			_entryPrice = close;
		}
	}
}
