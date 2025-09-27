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
/// Gold RSI Divergence strategy.
/// Looks for price/RSI divergence to scalp gold.
/// </summary>
public class GoldRsiDivergenceStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackLeft;
	private readonly StrategyParam<int> _lookbackRight;
	private readonly StrategyParam<int> _rangeLower;
	private readonly StrategyParam<int> _rangeUpper;
	private readonly StrategyParam<decimal> _pipValue;

	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal[] _rsiBuffer = Array.Empty<decimal>();
	private decimal[] _lowBuffer = Array.Empty<decimal>();
	private decimal[] _highBuffer = Array.Empty<decimal>();
	private int _bufferCount;
	private int _barIndex;

	private RelativeStrengthIndex _rsi;

	private decimal? _lastRsiLow;
	private decimal? _lastPriceLow;
	private int _lastPivotLowIndex = -1;

	private decimal? _lastRsiHigh;
	private decimal? _lastPriceHigh;
	private int _lastPivotHighIndex = -1;

	/// <summary>
	/// RSI calculation length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Stop loss in pips (1 pip = 0.1 for gold).
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit in pips (1 pip = 0.1 for gold).
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Pivot lookback bars to the left.
	/// </summary>
	public int LookbackLeft
	{
		get => _lookbackLeft.Value;
		set => _lookbackLeft.Value = value;
	}

	/// <summary>
	/// Pivot lookback bars to the right.
	/// </summary>
	public int LookbackRight
	{
		get => _lookbackRight.Value;
		set => _lookbackRight.Value = value;
	}

	/// <summary>
	/// Minimum number of bars between pivots.
	/// </summary>
	public int RangeLower
	{
		get => _rangeLower.Value;
		set => _rangeLower.Value = value;
	}

	/// <summary>
	/// Maximum number of bars between pivots.
	/// </summary>
	public int RangeUpper
	{
		get => _rangeUpper.Value;
		set => _rangeUpper.Value = value;
	}

	/// <summary>
	/// Pip value for converting pip-based risk to price.
	/// </summary>
	public decimal PipValue
	{
		get => _pipValue.Value;
		set => _pipValue.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public GoldRsiDivergenceStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 60)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation length", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(30, 100, 5);

		_stopLossPips = Param(nameof(StopLossPips), 11m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Stop loss in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5m, 20m, 1m);

		_lookbackLeft = Param(nameof(LookbackLeft), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Left", "Bars to the left of pivot", "Divergence")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_lookbackRight = Param(nameof(LookbackRight), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Right", "Bars to the right of pivot", "Divergence")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_rangeLower = Param(nameof(RangeLower), 5)
			.SetGreaterThanZero()
			.SetDisplay("Range Lower", "Minimum bars between pivots", "Divergence")
			.SetCanOptimize(true)
			.SetOptimize(3, 15, 1);

		_rangeUpper = Param(nameof(RangeUpper), 60)
			.SetGreaterThanZero()
			.SetDisplay("Range Upper", "Maximum bars between pivots", "Divergence")
			.SetCanOptimize(true)
			.SetOptimize(30, 120, 5);

		_pipValue = Param(nameof(PipValue), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Pip Value", "Pip value in price units", "Risk");

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

		InitializeBuffers();
		_barIndex = 0;
		_lastRsiLow = null;
		_lastPriceLow = null;
		_lastPivotLowIndex = -1;
		_lastRsiHigh = null;
		_lastPriceHigh = null;
		_lastPivotHighIndex = -1;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitializeBuffers();
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			new Unit(StopLossPips * PipValue, UnitTypes.Absolute),
			new Unit(TakeProfitPips * PipValue, UnitTypes.Absolute));
	}

	private void InitializeBuffers()
	{
		var length = Math.Max(1, LookbackLeft + LookbackRight + 1);

		_rsiBuffer = new decimal[length];
		_lowBuffer = new decimal[length];
		_highBuffer = new decimal[length];
		_bufferCount = 0;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_barIndex++;

		var rsiValue = _rsi.Process(candle).ToDecimal();

		AddToBuffer(rsiValue, candle.LowPrice, candle.HighPrice);

		if (_bufferCount < _rsiBuffer.Length)
			return;

		var pivotIndex = LookbackRight;
		var candidateRsi = _rsiBuffer[pivotIndex];
		var candidateLow = _lowBuffer[pivotIndex];
		var candidateHigh = _highBuffer[pivotIndex];
		var candidateBar = _barIndex - LookbackRight;

		var isPivotLow = IsPivotLow(candidateRsi);
		var isPivotHigh = IsPivotHigh(candidateRsi);

		if (isPivotLow)
		{
			var inRange = _lastPivotLowIndex >= 0 &&
				candidateBar - _lastPivotLowIndex >= RangeLower &&
				candidateBar - _lastPivotLowIndex <= RangeUpper;

			var bullishDiv = inRange &&
				_lastRsiLow is decimal prevRsiLow &&
				_lastPriceLow is decimal prevPriceLow &&
				candidateRsi > prevRsiLow &&
				candidateLow < prevPriceLow;

			if (bullishDiv && rsiValue < 40m && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));

			_lastRsiLow = candidateRsi;
			_lastPriceLow = candidateLow;
			_lastPivotLowIndex = candidateBar;
		}

		if (isPivotHigh)
		{
			var inRange = _lastPivotHighIndex >= 0 &&
				candidateBar - _lastPivotHighIndex >= RangeLower &&
				candidateBar - _lastPivotHighIndex <= RangeUpper;

			var bearishDiv = inRange &&
				_lastRsiHigh is decimal prevRsiHigh &&
				_lastPriceHigh is decimal prevPriceHigh &&
				candidateRsi < prevRsiHigh &&
				candidateHigh > prevPriceHigh;

			if (bearishDiv && rsiValue > 60m && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));

			_lastRsiHigh = candidateRsi;
			_lastPriceHigh = candidateHigh;
			_lastPivotHighIndex = candidateBar;
		}
	}

	private void AddToBuffer(decimal rsi, decimal low, decimal high)
	{
		if (_bufferCount < _rsiBuffer.Length)
		{
			_rsiBuffer[_bufferCount] = rsi;
			_lowBuffer[_bufferCount] = low;
			_highBuffer[_bufferCount] = high;
			_bufferCount++;
		}
		else
		{
			Array.Copy(_rsiBuffer, 1, _rsiBuffer, 0, _rsiBuffer.Length - 1);
			Array.Copy(_lowBuffer, 1, _lowBuffer, 0, _lowBuffer.Length - 1);
			Array.Copy(_highBuffer, 1, _highBuffer, 0, _highBuffer.Length - 1);
			_rsiBuffer[^1] = rsi;
			_lowBuffer[^1] = low;
			_highBuffer[^1] = high;
		}
	}

	private bool IsPivotLow(decimal value)
	{
		for (var i = 0; i < _rsiBuffer.Length; i++)
		{
			if (i == LookbackRight)
				continue;

			if (_rsiBuffer[i] <= value)
				return false;
		}

		return true;
	}

	private bool IsPivotHigh(decimal value)
	{
		for (var i = 0; i < _rsiBuffer.Length; i++)
		{
			if (i == LookbackRight)
				continue;

			if (_rsiBuffer[i] >= value)
				return false;
		}

		return true;
	}
}
