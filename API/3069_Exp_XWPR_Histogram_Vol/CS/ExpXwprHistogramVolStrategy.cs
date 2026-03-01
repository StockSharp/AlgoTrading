using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from the MetaTrader expert Exp_XWPR_Histogram_Vol.
/// Computes a volume-weighted Williams %R histogram inline and trades
/// on colour transitions.
/// </summary>
public class ExpXwprHistogramVolStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<decimal> _highLevel2;
	private readonly StrategyParam<decimal> _highLevel1;
	private readonly StrategyParam<decimal> _lowLevel1;
	private readonly StrategyParam<decimal> _lowLevel2;

	// rolling state
	private WilliamsR _wpr;
	private SimpleMovingAverage _histSma;
	private SimpleMovingAverage _volSma;
	private int? _prevColor;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int WprPeriod { get => _wprPeriod.Value; set => _wprPeriod.Value = value; }
	public int SmoothingLength { get => _smoothingLength.Value; set => _smoothingLength.Value = value; }
	public decimal HighLevel2 { get => _highLevel2.Value; set => _highLevel2.Value = value; }
	public decimal HighLevel1 { get => _highLevel1.Value; set => _highLevel1.Value = value; }
	public decimal LowLevel1 { get => _lowLevel1.Value; set => _lowLevel1.Value = value; }
	public decimal LowLevel2 { get => _lowLevel2.Value; set => _lowLevel2.Value = value; }

	public ExpXwprHistogramVolStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_wprPeriod = Param(nameof(WprPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("WPR Period", "Williams %R lookback", "Indicator");

		_smoothingLength = Param(nameof(SmoothingLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing", "Smoothing length", "Indicator");

		_highLevel2 = Param(nameof(HighLevel2), 17m)
			.SetDisplay("High Level 2", "Strong bullish zone", "Indicator");

		_highLevel1 = Param(nameof(HighLevel1), 5m)
			.SetDisplay("High Level 1", "Mild bullish zone", "Indicator");

		_lowLevel1 = Param(nameof(LowLevel1), -5m)
			.SetDisplay("Low Level 1", "Mild bearish zone", "Indicator");

		_lowLevel2 = Param(nameof(LowLevel2), -17m)
			.SetDisplay("Low Level 2", "Strong bearish zone", "Indicator");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevColor = null;

		_wpr = new WilliamsR { Length = WprPeriod };
		_histSma = new SimpleMovingAverage { Length = SmoothingLength };
		_volSma = new SimpleMovingAverage { Length = SmoothingLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_wpr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _wpr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wprVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// volume from candle
		var volume = candle.TotalVolume;
		if (volume <= 0)
			volume = 1;

		// histogram raw = (wpr + 50) * volume
		var histRaw = (wprVal + 50m) * volume;

		// smooth histogram and volume
		var histSmoothed = _histSma.Process(new DecimalIndicatorValue(_histSma, histRaw, candle.OpenTime) { IsFinal = true });
		var volSmoothed = _volSma.Process(new DecimalIndicatorValue(_volSma, volume, candle.OpenTime) { IsFinal = true });

		if (!histSmoothed.IsFinal || !histSmoothed.IsFormed)
			return;
		if (!volSmoothed.IsFinal || !volSmoothed.IsFormed)
			return;

		var hist = histSmoothed.ToDecimal();
		var baseline = volSmoothed.ToDecimal();

		if (baseline == 0)
			return;

		// determine color zone (0=strong bull, 1=mild bull, 2=neutral, 3=mild bear, 4=strong bear)
		var maxLevel = HighLevel2 * baseline;
		var upperLevel = HighLevel1 * baseline;
		var lowerLevel = LowLevel1 * baseline;
		var minLevel = LowLevel2 * baseline;

		int color;
		if (hist > maxLevel)
			color = 0; // strong bullish
		else if (hist > upperLevel)
			color = 1; // mild bullish
		else if (hist < minLevel)
			color = 4; // strong bearish
		else if (hist < lowerLevel)
			color = 3; // mild bearish
		else
			color = 2; // neutral

		if (_prevColor == null)
		{
			_prevColor = color;
			return;
		}

		var older = _prevColor.Value;
		_prevColor = color;

		// Trading logic: color transitions
		// Bullish transitions: from mild bull (1) to stronger (0), or from neutral+ to any bull
		// Bearish transitions: from mild bear (3) to stronger (4), or from neutral- to any bear

		if (older == 1 && color == 0 && Position <= 0)
		{
			// strong bullish signal
			if (Position < 0) BuyMarket(); // close short
			BuyMarket(); // open long
		}
		else if (older == 2 && color <= 1 && Position <= 0)
		{
			// neutral to bullish
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (older == 3 && color == 4 && Position >= 0)
		{
			// strong bearish signal
			if (Position > 0) SellMarket(); // close long
			SellMarket(); // open short
		}
		else if (older == 2 && color >= 3 && Position >= 0)
		{
			// neutral to bearish
			if (Position > 0) SellMarket();
			SellMarket();
		}
	}
}
