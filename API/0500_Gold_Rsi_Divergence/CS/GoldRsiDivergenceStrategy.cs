using System;
using System.Collections.Generic;

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
	private const int _lookbackLeft = 5;
	private const int _lookbackRight = 5;
	private const int _rangeLower = 5;
	private const int _rangeUpper = 60;
	private const decimal _pipValue = 0.1m;

	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal[] _rsiBuffer = new decimal[_lookbackLeft + _lookbackRight + 1];
	private readonly decimal[] _lowBuffer = new decimal[_lookbackLeft + _lookbackRight + 1];
	private readonly decimal[] _highBuffer = new decimal[_lookbackLeft + _lookbackRight + 1];
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

		_takeProfitPips = Param(nameof(TakeProfitPips), 33m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Take profit in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 60m, 5m);

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

		_bufferCount = 0;
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

		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			new Unit(StopLossPips * _pipValue, UnitTypes.Absolute),
			new Unit(TakeProfitPips * _pipValue, UnitTypes.Absolute));
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

		var pivotIndex = _lookbackRight;
		var candidateRsi = _rsiBuffer[pivotIndex];
		var candidateLow = _lowBuffer[pivotIndex];
		var candidateHigh = _highBuffer[pivotIndex];
		var candidateBar = _barIndex - _lookbackRight;

		var isPivotLow = IsPivotLow(candidateRsi);
		var isPivotHigh = IsPivotHigh(candidateRsi);

		if (isPivotLow)
		{
			var inRange = _lastPivotLowIndex >= 0 &&
				candidateBar - _lastPivotLowIndex >= _rangeLower &&
				candidateBar - _lastPivotLowIndex <= _rangeUpper;

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
				candidateBar - _lastPivotHighIndex >= _rangeLower &&
				candidateBar - _lastPivotHighIndex <= _rangeUpper;

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
			if (i == _lookbackRight)
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
			if (i == _lookbackRight)
				continue;

			if (_rsiBuffer[i] >= value)
				return false;
		}

		return true;
	}
}
