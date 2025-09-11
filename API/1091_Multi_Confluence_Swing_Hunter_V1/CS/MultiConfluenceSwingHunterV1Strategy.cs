using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-confluence swing hunter strategy using RSI, MACD, and price action scoring.
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

	private bool _isFirst = true;
	private decimal _prevRsi;
	private decimal _prevMacd;
	private decimal _prevHist;

	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int MinEntryScore { get => _minEntryScore.Value; set => _minEntryScore.Value = value; }
	public int MinExitScore { get => _minExitScore.Value; set => _minExitScore.Value = value; }
	public decimal MinLowerWickPercent { get => _minLowerWickPercent.Value; set => _minLowerWickPercent.Value = value; }
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public decimal RsiExtremeOversold { get => _rsiExtremeOversold.Value; set => _rsiExtremeOversold.Value = value; }
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public decimal RsiExtremeOverbought { get => _rsiExtremeOverbought.Value; set => _rsiExtremeOverbought.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MultiConfluenceSwingHunterV1Strategy()
	{
		_macdFast = Param(nameof(MacdFast), 3)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "MACD fast length", "Indicators");
		_macdSlow = Param(nameof(MacdSlow), 10)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow length", "Indicators");
		_macdSignal = Param(nameof(MacdSignal), 3)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "MACD signal length", "Indicators");
		_rsiLength = Param(nameof(RsiLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation length", "Indicators");
		_minEntryScore = Param(nameof(MinEntryScore), 13)
			.SetDisplay("Min Entry Score", "Minimum entry score", "Entry");
		_minExitScore = Param(nameof(MinExitScore), 13)
			.SetDisplay("Min Exit Score", "Minimum exit score", "Exit");
		_minLowerWickPercent = Param(nameof(MinLowerWickPercent), 50m)
			.SetDisplay("Min Lower Wick %", "Minimum lower wick percent", "Price Action");
		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "RSI");
		_rsiExtremeOversold = Param(nameof(RsiExtremeOversold), 25m)
			.SetDisplay("RSI Extreme Oversold", "RSI extreme oversold level", "RSI");
		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "RSI overbought level", "RSI");
		_rsiExtremeOverbought = Param(nameof(RsiExtremeOverbought), 75m)
			.SetDisplay("RSI Extreme Overbought", "RSI extreme overbought level", "RSI");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_isFirst = true;
		_prevRsi = 0m;
		_prevMacd = 0m;
		_prevHist = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdFast,
			LongPeriod = MacdSlow,
			SignalPeriod = MacdSignal
		};
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		SubscribeCandles(CandleType)
			.Bind(macd, rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal macd, decimal signal, decimal hist, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_isFirst)
		{
			_prevRsi = rsi;
			_prevMacd = macd;
			_prevHist = hist;
			_isFirst = false;
			return;
		}

		var bodySize = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var lowerWick = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;
		var upperWick = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
		var totalRange = candle.HighPrice - candle.LowPrice;
		var lowerWickPercent = totalRange > 0 ? (lowerWick / totalRange) * 100m : 0m;
		var upperWickPercent = totalRange > 0 ? (upperWick / totalRange) * 100m : 0m;
		var bodyPercent = totalRange > 0 ? (bodySize / totalRange) * 100m : 0m;

		var entryScore = 0;
		if (rsi < RsiOversold) entryScore += 2;
		if (rsi < RsiExtremeOversold) entryScore += 2;
		if (rsi > _prevRsi) entryScore += 1;
		if (macd < 0) entryScore += 1;
		if (macd > _prevMacd) entryScore += 2;
		if (hist > _prevHist) entryScore += 2;
		if (lowerWickPercent > MinLowerWickPercent) entryScore += 2;
		if (bodyPercent < 30m) entryScore += 1;
		if (candle.ClosePrice > candle.OpenPrice) entryScore += 1;

		var exitScore = 0;
		if (rsi > RsiOverbought) exitScore += 2;
		if (rsi > RsiExtremeOverbought) exitScore += 2;
		if (rsi < _prevRsi) exitScore += 1;
		if (macd > 0) exitScore += 1;
		if (macd < _prevMacd) exitScore += 2;
		if (hist < _prevHist) exitScore += 2;
		if (upperWickPercent > MinLowerWickPercent) exitScore += 2;
		if (candle.ClosePrice < candle.OpenPrice) exitScore += 1;

		if (entryScore >= MinEntryScore && Position <= 0)
			BuyMarket();
		else if (exitScore >= MinExitScore && Position > 0)
			SellMarket();

		_prevRsi = rsi;
		_prevMacd = macd;
		_prevHist = hist;
	}
}
