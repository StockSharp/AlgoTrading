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
/// Strategy that trades using adaptive Fractal RSI indicator computed inline.
/// </summary>
public class FractalRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fractalPeriod;
	private readonly StrategyParam<int> _normalSpeed;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private readonly List<decimal> _prices = new();
	private decimal? _previousValue;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FractalPeriod { get => _fractalPeriod.Value; set => _fractalPeriod.Value = value; }
	public int NormalSpeed { get => _normalSpeed.Value; set => _normalSpeed.Value = value; }
	public decimal HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }
	public decimal LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	public FractalRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator", "General");

		_fractalPeriod = Param(nameof(FractalPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Fractal Period", "Period for fractal dimension", "Indicator");

		_normalSpeed = Param(nameof(NormalSpeed), 30)
			.SetGreaterThanZero()
			.SetDisplay("Normal Speed", "Base period for RSI", "Indicator");

		_highLevel = Param(nameof(HighLevel), 60m)
			.SetDisplay("High Level", "Upper threshold", "Indicator");

		_lowLevel = Param(nameof(LowLevel), 40m)
			.SetDisplay("Low Level", "Lower threshold", "Indicator");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prices.Clear();
		_previousValue = null;
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

	private decimal? ComputeFractalRsi()
	{
		var period = FractalPeriod;
		if (_prices.Count < period + 1)
			return null;

		var lastIndex = _prices.Count - 1;
		var startIndex = lastIndex - period + 1;

		var priceMax = _prices[startIndex];
		var priceMin = _prices[startIndex];
		for (var i = startIndex; i <= lastIndex; i++)
		{
			if (_prices[i] > priceMax) priceMax = _prices[i];
			if (_prices[i] < priceMin) priceMin = _prices[i];
		}

		double length = 0.0;
		double? priorDiff = null;

		if (priceMax - priceMin > 0m)
		{
			for (var k = 0; k < period; k++)
			{
				var p = (double)((_prices[lastIndex - k] - priceMin) / (priceMax - priceMin));
				if (priorDiff != null)
					length += Math.Sqrt(Math.Pow(p - priorDiff.Value, 2.0) + 1.0 / (period * period));
				priorDiff = p;
			}
		}

		var log2 = Math.Log(2.0);
		double fdi = length > 0.0 ? 1.0 + (Math.Log(length) + log2) / Math.Log(2.0 * (period - 1)) : 0.0;
		double hurst = 2.0 - fdi;
		double trailDim = hurst != 0.0 ? 1.0 / hurst : 0.0;
		var speed = (int)Math.Max(1, Math.Round(NormalSpeed * trailDim / 2.0));

		if (_prices.Count <= speed)
			return null;

		decimal sumUp = 0m;
		decimal sumDown = 0m;
		for (var i = lastIndex - speed + 1; i <= lastIndex; i++)
		{
			var diff = _prices[i] - _prices[i - 1];
			if (diff > 0) sumUp += diff;
			else sumDown -= diff;
		}

		var pos = sumUp / speed;
		var neg = sumDown / speed;

		if (neg > 0) return 100m - (100m / (1m + pos / neg));
		return pos > 0 ? 100m : 50m;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_prices.Add(candle.ClosePrice);
		if (_prices.Count > 500)
			_prices.RemoveAt(0);

		var value = ComputeFractalRsi();
		if (value == null)
			return;

		var prev = _previousValue;
		_previousValue = value;

		if (prev is null)
			return;

		// Direct mode: buy on oversold cross down, sell on overbought cross up
		if (prev > LowLevel && value <= LowLevel && Position <= 0)
			BuyMarket();
		else if (prev < HighLevel && value >= HighLevel && Position >= 0)
			SellMarket();
	}
}
