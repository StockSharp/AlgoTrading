using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive FIR/ARMA crossover strategy converted from MetaTrader.
/// Computes FIR filter with windowed sinc, then ARMA forecast.
/// Buys when ARMA turns up and sells when it turns down.
/// </summary>
public class ExpAfirmaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _periods;
	private readonly StrategyParam<int> _taps;

	private readonly List<decimal> _prices = new();
	private decimal[] _weights = Array.Empty<decimal>();
	private decimal _weightSum;
	private int _effectiveTaps;
	private int _halfWindow;
	private decimal _sx2, _sx3, _sx4, _sx5, _sx6, _denom;
	private decimal? _prevFir;
	private decimal? _prevArma;
	private decimal? _prevPrevArma;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Periods { get => _periods.Value; set => _periods.Value = value; }
	public int Taps { get => _taps.Value; set => _taps.Value = value; }

	public ExpAfirmaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_periods = Param(nameof(Periods), 4)
			.SetGreaterThanZero()
			.SetDisplay("Bandwidth", "FIR bandwidth reciprocal", "Indicator");

		_taps = Param(nameof(Taps), 21)
			.SetGreaterThanZero()
			.SetDisplay("Taps", "FIR coefficient count (odd)", "Indicator");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prices.Clear();
		_weights = Array.Empty<decimal>();
		_weightSum = 0;
		_effectiveTaps = 0;
		_halfWindow = 0;
		_prevFir = null;
		_prevArma = null;
		_prevPrevArma = null;

		// We need a dummy indicator for Bind — use an SMA
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

		if (_weights.Length == 0)
			RebuildCoefficients();

		_prices.Add(candle.ClosePrice);
		while (_prices.Count > _effectiveTaps)
			_prices.RemoveAt(0);

		if (_prices.Count < _effectiveTaps)
			return;

		var fir = ComputeFir();

		if (_prevFir == null)
		{
			_prevFir = fir;
			return;
		}

		var arma = ComputeArma(fir, _prevFir.Value);
		_prevFir = fir;

		if (_prevArma == null)
		{
			_prevArma = arma;
			return;
		}

		if (_prevPrevArma == null)
		{
			_prevPrevArma = _prevArma;
			_prevArma = arma;
			return;
		}

		var curr = arma;
		var prev = _prevArma.Value;
		var prior = _prevPrevArma.Value;

		_prevPrevArma = _prevArma;
		_prevArma = arma;

		// Long: ARMA turning up (prev < prior and curr > prev)
		if (curr > prev && prev < prior && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Short: ARMA turning down (prev > prior and curr < prev)
		else if (curr < prev && prev > prior && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
	}

	private void RebuildCoefficients()
	{
		var taps = Math.Max(3, Taps);
		if (taps % 2 == 0) taps++;
		_effectiveTaps = taps;
		_halfWindow = (taps - 1) / 2;

		_weights = new decimal[taps];
		_weightSum = 0;
		var middle = taps / 2.0;
		var periods = Math.Max(1, Periods);

		for (var k = 0; k < taps; k++)
		{
			// Blackman window
			double weight = 0.42 - 0.50 * Math.Cos(2.0 * Math.PI * k / taps)
				+ 0.08 * Math.Cos(4.0 * Math.PI * k / taps);

			if (Math.Abs(k - middle) > double.Epsilon)
			{
				var num = Math.Sin(Math.PI * (k - middle) / periods);
				var den = Math.PI * (k - middle) / periods;
				if (Math.Abs(den) > double.Epsilon)
					weight *= num / den;
			}

			_weights[k] = (decimal)weight;
			_weightSum += (decimal)weight;
		}

		if (_weightSum == 0) _weightSum = 1;

		var n = (decimal)_halfWindow;
		_sx2 = (2m * n + 1m) / 3m;
		_sx3 = n * (n + 1m) / 2m;
		_sx4 = _sx2 * (3m * n * n + 3m * n - 1m) / 5m;
		_sx5 = _sx3 * (2m * n * n + 2m * n - 1m) / 3m;
		_sx6 = _sx2 * (3m * n * n * n * (n + 2m) - 3m * n + 1m) / 7m;
		_denom = _sx5 == 0 ? 1m : (_sx6 * _sx4 / _sx5 - _sx5);
	}

	private decimal ComputeFir()
	{
		var sum = 0m;
		var count = _prices.Count;
		for (var i = 0; i < _weights.Length && i < count; i++)
			sum += _prices[count - 1 - i] * _weights[i];
		return sum / _weightSum;
	}

	private decimal ComputeArma(decimal fir, decimal previousFir)
	{
		var n = _halfWindow;
		if (n <= 0) return fir;

		var nDec = (decimal)n;
		var sx2y = 0m;
		var sx3y = 0m;

		for (var i = 0; i <= n; i++)
		{
			var lag = n - i;
			if (_prices.Count - 1 - lag < 0) continue;
			var price = _prices[_prices.Count - 1 - lag];
			var iDec = (decimal)i;
			sx2y += iDec * iDec * price;
			sx3y += iDec * iDec * iDec * price;
		}

		sx2y = 2m * sx2y / nDec / (nDec + 1m);
		sx3y = 2m * sx3y / nDec / (nDec + 1m);

		var a0 = fir;
		var a1 = fir - previousFir;
		var p = sx2y - a0 * _sx2 - a1 * _sx3;
		var q = sx3y - a0 * _sx3 - a1 * _sx4;

		if (_sx5 == 0 || Math.Abs(_denom) < 1e-12m)
			return fir;

		var a2 = (p * _sx6 / _sx5 - q) / _denom;
		var a3 = (q * _sx4 / _sx5 - p) / _denom;
		var k = nDec;
		return a0 + k * a1 + k * k * a2 + k * k * k * a3;
	}
}
