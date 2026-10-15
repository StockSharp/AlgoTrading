using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dynamic Support and Resistance Pivot strategy.
/// Support is the last pivot low and resistance the last pivot high, a pivot being a candle whose low (high) is beyond those of the
/// PivotLength candles on each side. A close crossing above support goes long and a close crossing below resistance goes short,
/// reversing an opposite position, when the close is within SupportResistanceDistance percent of that level. Percent stop loss and
/// take profit manage the position.
/// </summary>
public class DynamicSupportAndResistancePivotStrategy : Strategy
{
	private readonly StrategyParam<int> _pivotLength;
	private readonly StrategyParam<decimal> _supportResistanceDistance;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = [];
	private readonly List<decimal> _lows = [];
	private decimal? _support;
	private decimal? _resistance;
	private decimal? _prevClose;

	/// <summary>
	/// Candles on each side of a pivot.
	/// </summary>
	public int PivotLength
	{
		get => _pivotLength.Value;
		set => _pivotLength.Value = value;
	}

	/// <summary>
	/// Maximum distance of the close from the level, in percent.
	/// </summary>
	public decimal SupportResistanceDistance
	{
		get => _supportResistanceDistance.Value;
		set => _supportResistanceDistance.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take profit percentage from entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
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
	public DynamicSupportAndResistancePivotStrategy()
	{
		_pivotLength = Param(nameof(PivotLength), 2)
			.SetGreaterThanZero()
			.SetDisplay("Pivot Length", "Candles on each side of a pivot", "Pivots");

		_supportResistanceDistance = Param(nameof(SupportResistanceDistance), 0.4m)
			.SetNotNegative()
			.SetDisplay("S/R Distance %", "Maximum distance of the close from the level, in percent", "Pivots");

		_stopLossPercent = Param(nameof(StopLossPercent), 10.0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 26.0m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit percentage from entry price", "Risk");

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
		ResetState();
	}

	private void ResetState()
	{
		_highs.Clear();
		_lows.Clear();
		_support = null;
		_resistance = null;
		_prevClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var take = TakeProfitPercent > 0 ? new Unit(TakeProfitPercent, UnitTypes.Percent) : new Unit();
		var stop = StopLossPercent > 0 ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit();
		StartProtection(take, stop, useMarketOrders: true);

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

		var length = PivotLength;
		var size = length * 2 + 1;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		if (_highs.Count > size)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (_highs.Count == size)
		{
			var centerHigh = _highs[length];
			var centerLow = _lows[length];
			var isPivotHigh = true;
			var isPivotLow = true;

			for (var i = 0; i < size; i++)
			{
				if (i == length)
					continue;

				if (_highs[i] >= centerHigh)
					isPivotHigh = false;

				if (_lows[i] <= centerLow)
					isPivotLow = false;
			}

			if (isPivotHigh)
				_resistance = centerHigh;

			if (isPivotLow)
				_support = centerLow;
		}

		var close = candle.ClosePrice;
		var prevClose = _prevClose;
		_prevClose = close;

		if (prevClose is not decimal pc)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var maxDistance = SupportResistanceDistance / 100m;

		var longSignal = _support is decimal support && support > 0
			&& pc <= support && close > support
			&& Math.Abs(close - support) / support <= maxDistance;

		var shortSignal = _resistance is decimal resistance && resistance > 0
			&& pc >= resistance && close < resistance
			&& Math.Abs(close - resistance) / resistance <= maxDistance;

		if (longSignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
