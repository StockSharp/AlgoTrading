using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gold RSI Divergence strategy.
/// RSI pivots are confirmed LookbackLeft bars before and LookbackRight bars after them. A pivot low whose RSI is higher than the
/// previous pivot low while price made a lower low, within RangeLower..RangeUpper bars of it and with RSI below 40, buys; the mirrored
/// bearish divergence with RSI above 60 sells. Positions are closed by a fixed stop loss and take profit in pips.
/// </summary>
public class GoldRsiDivergenceStrategy : Strategy
{
	private const decimal _longRsiLimit = 40m;
	private const decimal _shortRsiLimit = 60m;

	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _lookbackLeft;
	private readonly StrategyParam<int> _lookbackRight;
	private readonly StrategyParam<int> _rangeLower;
	private readonly StrategyParam<int> _rangeUpper;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<(decimal rsi, decimal low, decimal high)> _window = [];
	private int _barIndex;
	private (decimal rsi, decimal low, int bar)? _lastPivotLow;
	private (decimal rsi, decimal high, int bar)? _lastPivotHigh;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Bars to the left of a pivot.
	/// </summary>
	public int LookbackLeft
	{
		get => _lookbackLeft.Value;
		set => _lookbackLeft.Value = value;
	}

	/// <summary>
	/// Bars to the right of a pivot.
	/// </summary>
	public int LookbackRight
	{
		get => _lookbackRight.Value;
		set => _lookbackRight.Value = value;
	}

	/// <summary>
	/// Minimum bars between two pivots.
	/// </summary>
	public int RangeLower
	{
		get => _rangeLower.Value;
		set => _rangeLower.Value = value;
	}

	/// <summary>
	/// Maximum bars between two pivots.
	/// </summary>
	public int RangeUpper
	{
		get => _rangeUpper.Value;
		set => _rangeUpper.Value = value;
	}

	/// <summary>
	/// Stop loss in pips (price steps).
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit in pips (price steps).
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
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
	public GoldRsiDivergenceStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 60)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_lookbackLeft = Param(nameof(LookbackLeft), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Left", "Bars to the left of a pivot", "Divergence");

		_lookbackRight = Param(nameof(LookbackRight), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Right", "Bars to the right of a pivot", "Divergence");

		_rangeLower = Param(nameof(RangeLower), 5)
			.SetNotNegative()
			.SetDisplay("Range Lower", "Minimum bars between two pivots", "Divergence");

		_rangeUpper = Param(nameof(RangeUpper), 60)
			.SetGreaterThanZero()
			.SetDisplay("Range Upper", "Maximum bars between two pivots", "Divergence");

		_stopLossPips = Param(nameof(StopLossPips), 11m)
			.SetNotNegative()
			.SetDisplay("Stop Loss Pips", "Stop loss in pips (price steps)", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 33m)
			.SetNotNegative()
			.SetDisplay("Take Profit Pips", "Take profit in pips (price steps)", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
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

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, ProcessCandle)
			.Start();

		var step = Security.PriceStep ?? 1m;
		StartProtection(new Unit(TakeProfitPips * step, UnitTypes.Absolute), new Unit(StopLossPips * step, UnitTypes.Absolute), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, rsi);
		}
	}

	private void ResetState()
	{
		_window.Clear();
		_barIndex = 0;
		_lastPivotLow = null;
		_lastPivotHigh = null;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!rsiValue.IsFormed)
			return;

		var rsi = rsiValue.ToDecimal();
		_barIndex++;

		_window.Add((rsi, candle.LowPrice, candle.HighPrice));
		var size = LookbackLeft + LookbackRight + 1;
		if (_window.Count > size)
			_window.RemoveAt(0);

		if (_window.Count < size)
			return;

		var pivot = _window[LookbackLeft];
		var pivotBar = _barIndex - LookbackRight;
		var isPivotLow = true;
		var isPivotHigh = true;

		for (var i = 0; i < size; i++)
		{
			if (i == LookbackLeft)
				continue;

			if (_window[i].rsi <= pivot.rsi)
				isPivotLow = false;

			if (_window[i].rsi >= pivot.rsi)
				isPivotHigh = false;
		}

		var canTrade = IsFormedAndOnlineAndAllowTrading();

		if (isPivotLow)
		{
			var bullish = _lastPivotLow is { } prev
				&& InRange(pivotBar - prev.bar)
				&& pivot.rsi > prev.rsi
				&& pivot.low < prev.low;

			if (canTrade && bullish && rsi < _longRsiLimit && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));

			_lastPivotLow = (pivot.rsi, pivot.low, pivotBar);
		}

		if (isPivotHigh)
		{
			var bearish = _lastPivotHigh is { } prev
				&& InRange(pivotBar - prev.bar)
				&& pivot.rsi < prev.rsi
				&& pivot.high > prev.high;

			if (canTrade && bearish && rsi > _shortRsiLimit && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));

			_lastPivotHigh = (pivot.rsi, pivot.high, pivotBar);
		}
	}

	private bool InRange(int bars) => bars >= RangeLower && bars <= RangeUpper;
}
