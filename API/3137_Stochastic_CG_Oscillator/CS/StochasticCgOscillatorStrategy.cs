using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Stochastic Center of Gravity oscillator.
/// Computes CG oscillator inline and trades on crossovers with its trigger line.
/// </summary>
public class StochasticCgOscillatorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;

	private readonly List<decimal> _medianPrices = new();
	private readonly List<decimal> _cgValues = new();
	private readonly decimal[] _normalizedBuffer = new decimal[4];
	private int _normalizedCount;
	private decimal? _prevOscillator;
	private decimal? _prevMain;
	private decimal? _prevTrigger;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Length { get => _length.Value; set => _length.Value = value; }

	public StochasticCgOscillatorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_length = Param(nameof(Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("Length", "CG oscillator lookback", "Indicator");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_medianPrices.Clear();
		_cgValues.Clear();
		Array.Clear(_normalizedBuffer, 0, _normalizedBuffer.Length);
		_normalizedCount = 0;
		_prevOscillator = null;
		_prevMain = null;
		_prevTrigger = null;

		var sma = new SimpleMovingAverage { Length = 2 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var len = Length;
		var price = (candle.HighPrice + candle.LowPrice) / 2m;
		_medianPrices.Add(price);
		while (_medianPrices.Count > len)
			_medianPrices.RemoveAt(0);

		if (_medianPrices.Count < len)
			return;

		// Compute Center of Gravity
		decimal num = 0, denom = 0;
		var weight = 1;
		for (var i = _medianPrices.Count - 1; i >= 0; i--)
		{
			num += weight * _medianPrices[i];
			denom += _medianPrices[i];
			weight++;
		}

		var cg = denom != 0 ? -num / denom + (len + 1m) / 2m : 0m;

		_cgValues.Add(cg);
		while (_cgValues.Count > len)
			_cgValues.RemoveAt(0);

		// Stochastic normalization
		var high = cg;
		var low = cg;
		foreach (var v in _cgValues)
		{
			if (v > high) high = v;
			if (v < low) low = v;
		}

		var normalized = high != low ? (cg - low) / (high - low) : 0m;

		// Shift buffer
		var limit = Math.Min(_normalizedCount, 3);
		for (var shift = limit; shift > 0; shift--)
			_normalizedBuffer[shift] = _normalizedBuffer[shift - 1];
		_normalizedBuffer[0] = normalized;
		if (_normalizedCount < 4) _normalizedCount++;

		if (_normalizedCount < 4)
			return;

		// Smoothed oscillator
		var smoothed = (4m * _normalizedBuffer[0] + 3m * _normalizedBuffer[1]
			+ 2m * _normalizedBuffer[2] + _normalizedBuffer[3]) / 10m;
		var oscillator = 2m * (smoothed - 0.5m);
		var triggerSrc = _prevOscillator ?? oscillator;
		var trigger = 0.96m * (triggerSrc + 0.02m);
		_prevOscillator = oscillator;

		if (_prevMain == null || _prevTrigger == null)
		{
			_prevMain = oscillator;
			_prevTrigger = trigger;
			return;
		}

		var prevAbove = _prevMain.Value > _prevTrigger.Value;
		var prevBelow = _prevMain.Value < _prevTrigger.Value;
		var currAbove = oscillator > trigger;
		var currBelow = oscillator < trigger;

		_prevMain = oscillator;
		_prevTrigger = trigger;

		// Crossover signals
		var buySignal = prevAbove && currBelow; // main crosses below trigger
		var sellSignal = prevBelow && currAbove; // main crosses above trigger

		if (buySignal && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (sellSignal && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
	}
}
