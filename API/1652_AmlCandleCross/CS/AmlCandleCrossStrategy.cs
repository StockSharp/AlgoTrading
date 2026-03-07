using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive Market Level candle cross strategy.
/// Opens position when AML value lies between candle open and close.
/// Reverses position if opposite condition occurs.
/// </summary>
public class AmlCandleCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _fractal;
	private readonly StrategyParam<int> _lag;
	private readonly StrategyParam<DataType> _candleType;

	public int Fractal { get => _fractal.Value; set => _fractal.Value = value; }
	public int Lag { get => _lag.Value; set => _lag.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AmlCandleCrossStrategy()
	{
		_fractal = Param(nameof(Fractal), 20)
			.SetGreaterThanZero()
			.SetDisplay("Fractal", "Fractal window size", "General");
		_lag = Param(nameof(Lag), 10)
			.SetGreaterThanZero()
			.SetDisplay("Lag", "Lag for smoothing", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle Type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var aml = new AdaptiveMarketLevel
		{
			Fractal = Fractal,
			Lag = Lag,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(aml, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal amlValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (amlValue == 0)
			return;

		var open = candle.OpenPrice;
		var close = candle.ClosePrice;

		// Bullish: AML between open and close, bullish candle
		var bullish = close > open && amlValue >= open && amlValue <= close;
		// Bearish: AML between close and open, bearish candle
		var bearish = close < open && amlValue >= close && amlValue <= open;

		if (bullish)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		else if (bearish)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}
	}
}

/// <summary>
/// Adaptive Market Level indicator.
/// </summary>
public class AdaptiveMarketLevel : BaseIndicator
{
	private int _pos;
	private decimal[] _smooth = Array.Empty<decimal>();
	private decimal _lastValue;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();

	public int Fractal { get; set; } = 70;
	public int Lag { get; set; } = 18;

	public override void Reset()
	{
		base.Reset();
		_pos = 0;
		_smooth = new decimal[Lag + 1];
		_lastValue = 0;
		_highs.Clear();
		_lows.Clear();
	}

	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.GetValue<ICandleMessage>();

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		if (_highs.Count < Fractal * 2 || _highs.Count <= Lag)
			return new DecimalIndicatorValue(this, 0m, input.Time);

		IsFormed = true;

		decimal r1 = Range(Fractal, 0) / Fractal;
		decimal r2 = Range(Fractal, Fractal) / Fractal;
		decimal r3 = Range(Fractal * 2, 0) / (Fractal * 2);

		double dim = 0;
		if (r1 + r2 > 0 && r3 > 0)
			dim = (Math.Log((double)(r1 + r2)) - Math.Log((double)r3)) * 1.44269504088896;

		var alpha = (decimal)Math.Exp(-Lag * (dim - 1.0));
		if (alpha > 1m) alpha = 1m;
		if (alpha < 0.01m) alpha = 0.01m;

		var price = (candle.HighPrice + candle.LowPrice + 2m * candle.OpenPrice + 2m * candle.ClosePrice) / 6m;

		var prevPos = (_pos - 1 + _smooth.Length) % _smooth.Length;
		_smooth[_pos] = alpha * price + (1m - alpha) * _smooth[prevPos];

		var lagPos = (_pos - Lag + _smooth.Length) % _smooth.Length;
		var step = 0.01m;
		var current = Math.Abs(_smooth[_pos] - _smooth[lagPos]) >= Lag * Lag * step ? _smooth[_pos] : _lastValue;

		_lastValue = current;
		_pos = (_pos + 1) % _smooth.Length;

		return new DecimalIndicatorValue(this, current, input.Time);
	}

	private decimal Range(int count, int offset)
	{
		var end = _highs.Count - 1 - offset;
		var start = end - count + 1;
		if (start < 0) start = 0;

		var max = decimal.MinValue;
		var min = decimal.MaxValue;

		for (var i = start; i <= end; i++)
		{
			if (_highs[i] > max) max = _highs[i];
			if (_lows[i] < min) min = _lows[i];
		}

		return max - min;
	}
}
