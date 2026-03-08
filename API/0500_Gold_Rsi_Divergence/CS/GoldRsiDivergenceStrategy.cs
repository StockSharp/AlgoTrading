using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI Divergence strategy.
/// Looks for price/RSI divergence for entries.
/// </summary>
public class GoldRsiDivergenceStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackLeft;
	private readonly StrategyParam<int> _lookbackRight;
	private readonly StrategyParam<int> _rangeLower;
	private readonly StrategyParam<int> _rangeUpper;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal[] _rsiBuffer = Array.Empty<decimal>();
	private decimal[] _lowBuffer = Array.Empty<decimal>();
	private decimal[] _highBuffer = Array.Empty<decimal>();
	private int _bufferCount;
	private int _barIndex;

	private decimal? _lastRsiLow;
	private decimal? _lastPriceLow;
	private int _lastPivotLowIndex = -1;

	private decimal? _lastRsiHigh;
	private decimal? _lastPriceHigh;
	private int _lastPivotHighIndex = -1;
	private int _cooldownRemaining;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int LookbackLeft { get => _lookbackLeft.Value; set => _lookbackLeft.Value = value; }
	public int LookbackRight { get => _lookbackRight.Value; set => _lookbackRight.Value = value; }
	public int RangeLower { get => _rangeLower.Value; set => _rangeLower.Value = value; }
	public int RangeUpper { get => _rangeUpper.Value; set => _rangeUpper.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public GoldRsiDivergenceStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation length", "RSI");

		_lookbackLeft = Param(nameof(LookbackLeft), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Left", "Bars to the left of pivot", "Divergence");

		_lookbackRight = Param(nameof(LookbackRight), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Right", "Bars to the right of pivot", "Divergence");

		_rangeLower = Param(nameof(RangeLower), 5)
			.SetGreaterThanZero()
			.SetDisplay("Range Lower", "Minimum bars between pivots", "Divergence");

		_rangeUpper = Param(nameof(RangeUpper), 60)
			.SetGreaterThanZero()
			.SetDisplay("Range Upper", "Maximum bars between pivots", "Divergence");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

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
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		InitializeBuffers();

		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void InitializeBuffers()
	{
		var length = Math.Max(1, LookbackLeft + LookbackRight + 1);
		_rsiBuffer = new decimal[length];
		_lowBuffer = new decimal[length];
		_highBuffer = new decimal[length];
		_bufferCount = 0;
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_barIndex++;

		AddToBuffer(rsiValue, candle.LowPrice, candle.HighPrice);

		if (_bufferCount < _rsiBuffer.Length)
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			CheckPivots(rsiValue, candle);
			return;
		}

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
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));
				BuyMarket(Volume);
				_cooldownRemaining = CooldownBars;
			}

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
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				SellMarket(Volume);
				_cooldownRemaining = CooldownBars;
			}

			_lastRsiHigh = candidateRsi;
			_lastPriceHigh = candidateHigh;
			_lastPivotHighIndex = candidateBar;
		}
	}

	private void CheckPivots(decimal rsiValue, ICandleMessage candle)
	{
		// Still track pivots during cooldown
		var pivotIndex = LookbackRight;
		var candidateRsi = _rsiBuffer[pivotIndex];
		var candidateBar = _barIndex - LookbackRight;

		if (IsPivotLow(candidateRsi))
		{
			_lastRsiLow = candidateRsi;
			_lastPriceLow = _lowBuffer[pivotIndex];
			_lastPivotLowIndex = candidateBar;
		}

		if (IsPivotHigh(candidateRsi))
		{
			_lastRsiHigh = candidateRsi;
			_lastPriceHigh = _highBuffer[pivotIndex];
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
