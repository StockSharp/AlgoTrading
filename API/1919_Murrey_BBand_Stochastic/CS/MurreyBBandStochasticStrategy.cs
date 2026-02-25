
using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Murrey Math reversal strategy filtered by Bollinger Bands and Stochastic oscillator.
/// Buys near Murrey support when stochastic oversold; sells near Murrey resistance when stochastic overbought.
/// </summary>
public class MurreyBBandStochasticStrategy : Strategy
{
	private readonly StrategyParam<int> _frame;
	private readonly StrategyParam<decimal> _entryMarginPct;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _bbDeviation;
	private readonly StrategyParam<int> _stochK;
	private readonly StrategyParam<int> _stochD;
	private readonly StrategyParam<decimal> _stochOversold;
	private readonly StrategyParam<decimal> _stochOverbought;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;

	public int Frame { get => _frame.Value; set => _frame.Value = value; }
	public decimal EntryMarginPct { get => _entryMarginPct.Value; set => _entryMarginPct.Value = value; }
	public int BbPeriod { get => _bbPeriod.Value; set => _bbPeriod.Value = value; }
	public decimal BbDeviation { get => _bbDeviation.Value; set => _bbDeviation.Value = value; }
	public int StochK { get => _stochK.Value; set => _stochK.Value = value; }
	public int StochD { get => _stochD.Value; set => _stochD.Value = value; }
	public decimal StochOversold { get => _stochOversold.Value; set => _stochOversold.Value = value; }
	public decimal StochOverbought { get => _stochOverbought.Value; set => _stochOverbought.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MurreyBBandStochasticStrategy()
	{
		_frame = Param(nameof(Frame), 64)
			.SetGreaterThanZero()
			.SetDisplay("Frame", "Murrey frame size", "General");

		_entryMarginPct = Param(nameof(EntryMarginPct), 2m)
			.SetDisplay("Entry Margin %", "Percentage distance from Murrey line for entry", "General");

		_bbPeriod = Param(nameof(BbPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Bollinger Bands period", "Indicators");

		_bbDeviation = Param(nameof(BbDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Bollinger Bands deviation", "Indicators");

		_stochK = Param(nameof(StochK), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "%K length", "Indicators");

		_stochD = Param(nameof(StochD), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "%D length", "Indicators");

		_stochOversold = Param(nameof(StochOversold), 30m)
			.SetDisplay("Stochastic Oversold", "Level for long setups", "Indicators");

		_stochOverbought = Param(nameof(StochOverbought), 70m)
			.SetDisplay("Stochastic Overbought", "Level for short setups", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highest = new Highest { Length = Frame };
		_lowest = new Lowest { Length = Frame };
		var bollinger = new BollingerBands { Length = BbPeriod, Width = BbDeviation };
		var stochastic = new StochasticOscillator
		{
			K = { Length = StochK },
			D = { Length = StochD },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_highest, _lowest, bollinger, stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue highValue, IIndicatorValue lowValue, IIndicatorValue bbValue, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!highValue.IsFormed || !lowValue.IsFormed || !bbValue.IsFormed || !stochValue.IsFormed)
			return;

		var nHigh = highValue.ToDecimal();
		var nLow = lowValue.ToDecimal();
		var range = nHigh - nLow;
		if (range <= 0m)
			return;

		// Murrey Math level calculation
		decimal fractal;
		if (nHigh <= 250000m && nHigh > 25000m)
			fractal = 100000m;
		else if (nHigh <= 25000m && nHigh > 2500m)
			fractal = 10000m;
		else if (nHigh <= 2500m && nHigh > 250m)
			fractal = 1000m;
		else if (nHigh <= 250m && nHigh > 25m)
			fractal = 100m;
		else if (nHigh <= 25m && nHigh > 6.25m)
			fractal = 12.5m;
		else if (nHigh <= 6.25m && nHigh > 3.125m)
			fractal = 6.25m;
		else if (nHigh <= 3.125m && nHigh > 1.5625m)
			fractal = 3.125m;
		else if (nHigh <= 1.5625m && nHigh > 0.390625m)
			fractal = 1.5625m;
		else if (nHigh > 250000m)
			fractal = 1000000m;
		else
			fractal = 0.1953125m;

		var logVal = Math.Log((double)(fractal / range), 2);
		if (double.IsNaN(logVal) || double.IsInfinity(logVal))
			return;

		var sum = (decimal)Math.Floor(logVal);
		var octave = fractal * (decimal)Math.Pow(0.5, (double)sum);
		if (octave <= 0)
			return;

		var minimum = Math.Floor(nLow / octave) * octave;
		var maximum = minimum + 2m * octave;
		if (maximum > nHigh)
			maximum = minimum + octave;

		var diff = maximum - minimum;
		if (diff <= 0)
			return;

		var level0 = minimum;
		var level1 = minimum + diff / 8m;
		var level4 = minimum + diff / 2m;
		var level7 = minimum + diff * 7m / 8m;
		var level8 = maximum;

		var close = candle.ClosePrice;
		var entryMargin = close * EntryMarginPct / 100m;

		// Bollinger filter
		var bb = (BollingerBandsValue)bbValue;
		if (bb.LowBand is not decimal lower || bb.UpBand is not decimal upper)
			return;

		// Stochastic filter
		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal kValue)
			return;

		// Buy: price near Murrey support (level0-level1), stochastic oversold, price below upper band
		if (Position <= 0 && kValue < StochOversold && close <= level1 + entryMargin && close < upper)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Sell: price near Murrey resistance (level7-level8), stochastic overbought, price above lower band
		else if (Position >= 0 && kValue > StochOverbought && close >= level7 - entryMargin && close > lower)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
		// Exit long at level4 (midpoint) or above level8
		else if (Position > 0 && (close >= level8 || close >= level4))
		{
			SellMarket();
		}
		// Exit short at level4 (midpoint) or below level0
		else if (Position < 0 && (close <= level0 || close <= level4))
		{
			BuyMarket();
		}
	}
}
