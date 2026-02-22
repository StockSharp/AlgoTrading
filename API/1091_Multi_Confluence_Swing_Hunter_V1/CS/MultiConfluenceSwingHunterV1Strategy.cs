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
/// Multi-confluence swing hunter strategy using RSI, MACD, and price action scoring.
/// </summary>
public class MultiConfluenceSwingHunterV1Strategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _minEntryScore;
	private readonly StrategyParam<int> _minExitScore;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<DataType> _candleType;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
	public int MinEntryScore { get => _minEntryScore.Value; set => _minEntryScore.Value = value; }
	public int MinExitScore { get => _minExitScore.Value; set => _minExitScore.Value = value; }
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MultiConfluenceSwingHunterV1Strategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation length", "Indicators");
		_smaLength = Param(nameof(SmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "SMA period", "Indicators");
		_minEntryScore = Param(nameof(MinEntryScore), 2)
			.SetDisplay("Min Entry Score", "Minimum entry score", "Entry");
		_minExitScore = Param(nameof(MinExitScore), 2)
			.SetDisplay("Min Exit Score", "Minimum exit score", "Exit");
		_rsiOversold = Param(nameof(RsiOversold), 40m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "RSI");
		_rsiOverbought = Param(nameof(RsiOverbought), 60m)
			.SetDisplay("RSI Overbought", "RSI overbought level", "RSI");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var sma = new SMA { Length = SmaLength };

		var prevRsi = 0m;
		var prevClose = 0m;
		var isInitialized = false;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(rsi, sma, (candle, rsiVal, smaVal) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (!isInitialized)
				{
					prevRsi = rsiVal;
					prevClose = candle.ClosePrice;
					isInitialized = true;
					return;
				}

				var entryScore = 0;
				if (rsiVal < RsiOversold) entryScore += 2;
				if (rsiVal > prevRsi) entryScore += 1;
				if (candle.ClosePrice > smaVal) entryScore += 1;
				if (candle.ClosePrice > candle.OpenPrice) entryScore += 1;

				var exitScore = 0;
				if (rsiVal > RsiOverbought) exitScore += 2;
				if (rsiVal < prevRsi) exitScore += 1;
				if (candle.ClosePrice < smaVal) exitScore += 1;
				if (candle.ClosePrice < candle.OpenPrice) exitScore += 1;

				if (entryScore >= MinEntryScore && Position <= 0)
					BuyMarket();
				else if (exitScore >= MinExitScore && Position > 0)
					SellMarket();

				prevRsi = rsiVal;
				prevClose = candle.ClosePrice;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}
}
