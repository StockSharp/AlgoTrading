using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bullish B's RSI Divergence strategy.
/// RSI pivots are bars whose RSI is below (pivot low) or above (pivot high) the PivotLookbackLeft bars before and the
/// PivotLookbackRight bars after them; a pivot is confirmed PivotLookbackRight bars later and is compared with the previous pivot
/// of the same kind when that one lies RangeLower..RangeUpper bars back. Long only: a regular bullish divergence (price lower low,
/// RSI higher low) or a hidden one (price higher low, RSI lower low) opens a long. The long closes on a regular bearish divergence
/// (price higher high, RSI lower high), when RSI crosses above TakeProfitRsiLevel, or on the optional trailing stop that follows
/// the close by StopLoss percent or by AtrMultiplier times ATR(AtrLength).
/// </summary>
public class BullishBsRsiDivergenceStrategy : Strategy
{
	/// <summary>
	/// Trailing stop types.
	/// </summary>
	public enum StopTypes
	{
		/// <summary>
		/// No trailing stop.
		/// </summary>
		None,

		/// <summary>
		/// Stop StopLoss percent below the close.
		/// </summary>
		Percent,

		/// <summary>
		/// Stop AtrMultiplier ATRs below the close.
		/// </summary>
		Atr,
	}

	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _pivotLookbackRight;
	private readonly StrategyParam<int> _pivotLookbackLeft;
	private readonly StrategyParam<decimal> _takeProfitRsiLevel;
	private readonly StrategyParam<int> _rangeUpper;
	private readonly StrategyParam<int> _rangeLower;
	private readonly StrategyParam<StopTypes> _stopType;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<(decimal rsi, decimal low, decimal high)> _window = new();
	private int _barIndex;
	private int? _lastLowPivotBar;
	private decimal _lastLowPivotRsi;
	private decimal _lastLowPivotPrice;
	private int? _lastHighPivotBar;
	private decimal _lastHighPivotRsi;
	private decimal _lastHighPivotPrice;
	private decimal? _prevRsi;
	private decimal? _trailingStop;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Bars after a pivot that confirm it.
	/// </summary>
	public int PivotLookbackRight
	{
		get => _pivotLookbackRight.Value;
		set => _pivotLookbackRight.Value = value;
	}

	/// <summary>
	/// Bars before a pivot that it must beat.
	/// </summary>
	public int PivotLookbackLeft
	{
		get => _pivotLookbackLeft.Value;
		set => _pivotLookbackLeft.Value = value;
	}

	/// <summary>
	/// RSI level whose upward cross closes the long.
	/// </summary>
	public decimal TakeProfitRsiLevel
	{
		get => _takeProfitRsiLevel.Value;
		set => _takeProfitRsiLevel.Value = value;
	}

	/// <summary>
	/// Maximum bars between two compared pivots.
	/// </summary>
	public int RangeUpper
	{
		get => _rangeUpper.Value;
		set => _rangeUpper.Value = value;
	}

	/// <summary>
	/// Minimum bars between two compared pivots.
	/// </summary>
	public int RangeLower
	{
		get => _rangeLower.Value;
		set => _rangeLower.Value = value;
	}

	/// <summary>
	/// Trailing stop type.
	/// </summary>
	public StopTypes StopType
	{
		get => _stopType.Value;
		set => _stopType.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in percent for the percent stop.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// ATR period for the ATR stop.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiple for the ATR stop.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
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
	public BullishBsRsiDivergenceStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "RSI");

		_pivotLookbackRight = Param(nameof(PivotLookbackRight), 3)
			.SetGreaterThanZero()
			.SetDisplay("Pivot Lookback Right", "Bars after a pivot that confirm it", "Pivots");

		_pivotLookbackLeft = Param(nameof(PivotLookbackLeft), 1)
			.SetGreaterThanZero()
			.SetDisplay("Pivot Lookback Left", "Bars before a pivot that it must beat", "Pivots");

		_takeProfitRsiLevel = Param(nameof(TakeProfitRsiLevel), 80m)
			.SetDisplay("Take Profit RSI Level", "RSI level whose upward cross closes the long", "Exit");

		_rangeUpper = Param(nameof(RangeUpper), 60)
			.SetGreaterThanZero()
			.SetDisplay("Range Upper", "Maximum bars between two compared pivots", "Pivots");

		_rangeLower = Param(nameof(RangeLower), 5)
			.SetNotNegative()
			.SetDisplay("Range Lower", "Minimum bars between two compared pivots", "Pivots");

		_stopType = Param(nameof(StopType), StopTypes.None)
			.SetDisplay("Stop Type", "Trailing stop type", "Exit");

		_stopLoss = Param(nameof(StopLoss), 5m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Trailing stop distance for the percent stop", "Exit");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period for the ATR stop", "Exit");

		_atrMultiplier = Param(nameof(AtrMultiplier), 3.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiple for the ATR stop", "Exit");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, atr, ProcessCandle)
			.Start();

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
		_lastLowPivotBar = null;
		_lastHighPivotBar = null;
		_lastLowPivotRsi = 0;
		_lastLowPivotPrice = 0;
		_lastHighPivotRsi = 0;
		_lastHighPivotPrice = 0;
		_prevRsi = null;
		_trailingStop = null;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!rsiValue.IsFormed)
			return;

		var rsi = rsiValue.GetValue<decimal>();
		var prevRsi = _prevRsi;
		_prevRsi = rsi;

		_barIndex++;
		_window.Add((rsi, candle.LowPrice, candle.HighPrice));
		var size = PivotLookbackLeft + PivotLookbackRight + 1;
		while (_window.Count > size)
			_window.RemoveAt(0);

		var bullish = false;
		var bearish = false;

		if (_window.Count == size)
		{
			var center = _window[PivotLookbackLeft];
			var pivotBar = _barIndex - PivotLookbackRight;

			var isLow = true;
			var isHigh = true;
			for (var i = 0; i < size; i++)
			{
				if (i == PivotLookbackLeft)
					continue;

				if (_window[i].rsi <= center.rsi)
					isLow = false;
				if (_window[i].rsi >= center.rsi)
					isHigh = false;
			}

			if (isLow)
			{
				if (_lastLowPivotBar is int prevBar && InRange(prevBar))
				{
					var regular = center.low < _lastLowPivotPrice && center.rsi > _lastLowPivotRsi;
					var hidden = center.low > _lastLowPivotPrice && center.rsi < _lastLowPivotRsi;
					bullish = regular || hidden;
				}

				_lastLowPivotBar = pivotBar;
				_lastLowPivotRsi = center.rsi;
				_lastLowPivotPrice = center.low;
			}

			if (isHigh)
			{
				if (_lastHighPivotBar is int prevBar && InRange(prevBar))
					bearish = center.high > _lastHighPivotPrice && center.rsi < _lastHighPivotRsi;

				_lastHighPivotBar = pivotBar;
				_lastHighPivotRsi = center.rsi;
				_lastHighPivotPrice = center.high;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if (_trailingStop is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				_trailingStop = null;
				return;
			}

			var targetCross = prevRsi is decimal pr && pr <= TakeProfitRsiLevel && rsi > TakeProfitRsiLevel;
			if (bearish || targetCross)
			{
				SellMarket(Position);
				_trailingStop = null;
				return;
			}

			UpdateTrailingStop(candle.ClosePrice, atrValue);
		}
		else if (Position == 0 && bullish)
		{
			BuyMarket(Volume);
			_trailingStop = null;
			UpdateTrailingStop(candle.ClosePrice, atrValue);
		}
	}

	// Pivots are compared only when the previous one lies RangeLower..RangeUpper bars before the bar preceding this one.
	private bool InRange(int prevPivotBar)
	{
		var bars = _barIndex - 1 - prevPivotBar;
		return bars >= RangeLower && bars <= RangeUpper;
	}

	private void UpdateTrailingStop(decimal close, IIndicatorValue atrValue)
	{
		decimal? candidate = StopType switch
		{
			StopTypes.Percent when StopLoss > 0 => close * (1m - StopLoss / 100m),
			StopTypes.Atr when atrValue.IsFormed => close - atrValue.GetValue<decimal>() * AtrMultiplier,
			_ => null,
		};

		if (candidate is not decimal value)
			return;

		_trailingStop = _trailingStop is decimal current ? Math.Max(current, value) : value;
	}
}
