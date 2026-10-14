using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-Confluence Swing Hunter V1 strategy.
/// Every finished candle gets a bullish and a bearish score built from RSI levels and turns, MACD position, histogram
/// direction and signal crosses, and candle structure (wick size, candle colour, close change and swing extremes).
/// A long opens when the bullish score reaches MinEntryScore and closes when the bearish score reaches MinExitScore.
/// Long only, no stops.
/// </summary>
public class MultiConfluenceSwingHunterV1Strategy : Strategy
{
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _minEntryScore;
	private readonly StrategyParam<int> _minExitScore;
	private readonly StrategyParam<decimal> _minLowerWickPercent;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiExtremeOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiExtremeOverbought;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevRsi;
	private decimal? _prevMacd;
	private decimal? _prevSignal;
	private decimal? _prevHigh;
	private decimal? _prevLow;
	private decimal? _prevClose;

	/// <summary>
	/// MACD fast EMA length.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// MACD slow EMA length.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD signal line length.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Bullish score required to enter.
	/// </summary>
	public int MinEntryScore
	{
		get => _minEntryScore.Value;
		set => _minEntryScore.Value = value;
	}

	/// <summary>
	/// Bearish score required to exit.
	/// </summary>
	public int MinExitScore
	{
		get => _minExitScore.Value;
		set => _minExitScore.Value = value;
	}

	/// <summary>
	/// Minimum wick size in percent of the candle range that counts as a rejection.
	/// </summary>
	public decimal MinLowerWickPercent
	{
		get => _minLowerWickPercent.Value;
		set => _minLowerWickPercent.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// RSI extreme oversold level.
	/// </summary>
	public decimal RsiExtremeOversold
	{
		get => _rsiExtremeOversold.Value;
		set => _rsiExtremeOversold.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// RSI extreme overbought level.
	/// </summary>
	public decimal RsiExtremeOverbought
	{
		get => _rsiExtremeOverbought.Value;
		set => _rsiExtremeOverbought.Value = value;
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
	public MultiConfluenceSwingHunterV1Strategy()
	{
		_macdFast = Param(nameof(MacdFast), 3)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "MACD fast EMA length", "MACD");

		_macdSlow = Param(nameof(MacdSlow), 10)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow EMA length", "MACD");

		_macdSignal = Param(nameof(MacdSignal), 3)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "MACD signal line length", "MACD");

		_rsiLength = Param(nameof(RsiLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI length", "RSI");

		_minEntryScore = Param(nameof(MinEntryScore), 13)
			.SetGreaterThanZero()
			.SetDisplay("Min Entry Score", "Bullish score required to enter", "Scoring");

		_minExitScore = Param(nameof(MinExitScore), 13)
			.SetGreaterThanZero()
			.SetDisplay("Min Exit Score", "Bearish score required to exit", "Scoring");

		_minLowerWickPercent = Param(nameof(MinLowerWickPercent), 50m)
			.SetRange(0m, 100m)
			.SetDisplay("Min Wick %", "Minimum wick size in percent of the candle range", "Price Action");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "RSI");

		_rsiExtremeOversold = Param(nameof(RsiExtremeOversold), 25m)
			.SetDisplay("RSI Extreme Oversold", "RSI extreme oversold level", "RSI");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "RSI overbought level", "RSI");

		_rsiExtremeOverbought = Param(nameof(RsiExtremeOverbought), 75m)
			.SetDisplay("RSI Extreme Overbought", "RSI extreme overbought level", "RSI");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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

	private void ResetState()
	{
		_prevRsi = null;
		_prevMacd = null;
		_prevSignal = null;
		_prevHigh = null;
		_prevLow = null;
		_prevClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, rsi);
				DrawIndicator(oscillators, macd);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!rsiValue.IsFormed || !macdValue.IsFormed || macdValue is not IMovingAverageConvergenceDivergenceSignalValue { Macd: decimal macd, Signal: decimal signal })
			return;

		var rsi = rsiValue.ToDecimal();

		var prevRsi = _prevRsi;
		var prevMacd = _prevMacd;
		var prevSignal = _prevSignal;
		var prevHigh = _prevHigh;
		var prevLow = _prevLow;
		var prevClose = _prevClose;

		_prevRsi = rsi;
		_prevMacd = macd;
		_prevSignal = signal;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevClose = candle.ClosePrice;

		if (prevRsi is not decimal pRsi || prevMacd is not decimal pMacd || prevSignal is not decimal pSignal ||
			prevHigh is not decimal pHigh || prevLow is not decimal pLow || prevClose is not decimal pClose)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var open = candle.OpenPrice;
		var close = candle.ClosePrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var range = high - low;
		var lowerWickPercent = range > 0 ? (Math.Min(open, close) - low) / range * 100m : 0m;
		var upperWickPercent = range > 0 ? (high - Math.Max(open, close)) / range * 100m : 0m;
		var hist = macd - signal;
		var prevHist = pMacd - pSignal;

		var entryScore = 0;
		if (rsi < RsiExtremeOversold)
			entryScore += 3;
		else if (rsi < RsiOversold)
			entryScore += 2;
		if (pRsi < RsiOversold && rsi > pRsi)
			entryScore += 3;
		if (macd < 0)
			entryScore += 1;
		if (hist > prevHist)
			entryScore += 2;
		if (pMacd <= pSignal && macd > signal)
			entryScore += 3;
		if (lowerWickPercent >= MinLowerWickPercent)
			entryScore += 3;
		if (close > open)
			entryScore += 2;
		if (close > pClose)
			entryScore += 1;
		if (low < pLow)
			entryScore += 2;

		var exitScore = 0;
		if (rsi > RsiExtremeOverbought)
			exitScore += 3;
		else if (rsi > RsiOverbought)
			exitScore += 2;
		if (pRsi > RsiOverbought && rsi < pRsi)
			exitScore += 3;
		if (macd > 0)
			exitScore += 1;
		if (hist < prevHist)
			exitScore += 2;
		if (pMacd >= pSignal && macd < signal)
			exitScore += 3;
		if (upperWickPercent >= MinLowerWickPercent)
			exitScore += 3;
		if (close < open)
			exitScore += 2;
		if (close < pClose)
			exitScore += 1;
		if (high > pHigh)
			exitScore += 2;

		if (Position <= 0 && entryScore >= MinEntryScore)
			BuyMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && exitScore >= MinExitScore)
			SellMarket(Position);
	}
}
