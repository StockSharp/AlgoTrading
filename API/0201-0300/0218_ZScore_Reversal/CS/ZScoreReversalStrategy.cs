using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ZScore Reversal strategy.
/// The Z-Score is the distance of the close from the LookbackPeriod simple moving average in standard deviations over the same candles.
/// A Z-Score below minus ZScoreThreshold goes long and one above ZScoreThreshold goes short, reversing an opposite position.
/// A long closes once the Z-Score crosses above zero and a short once it crosses below, and a percent stop limits the loss.
/// </summary>
public class ZScoreReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _zScoreThreshold;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Period of the moving average and the standard deviation.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Z-Score distance from zero that opens a position.
	/// </summary>
	public decimal ZScoreThreshold
	{
		get => _zScoreThreshold.Value;
		set => _zScoreThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	public ZScoreReversalStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Period of the moving average and the standard deviation", "Indicators");

		_zScoreThreshold = Param(nameof(ZScoreThreshold), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Z-Score Threshold", "Z-Score distance from zero that opens a position", "Indicators");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(10).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		var ma = new SimpleMovingAverage { Length = LookbackPeriod };
		var stdev = new StandardDeviation { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ma, stdev, ProcessCandle)
			.Start();

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

		// The stop has to see prices between candles, not only at their close.
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, stdev);
			}
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue, IIndicatorValue stdDevValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!maValue.IsFormed || !stdDevValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var deviation = stdDevValue.GetValue<decimal>();

		if (deviation == 0)
			return;

		var zScore = (candle.ClosePrice - maValue.GetValue<decimal>()) / deviation;

		if (zScore < -ZScoreThreshold && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (zScore > ZScoreThreshold && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && zScore > 0)
			SellMarket(Position);
		else if (Position < 0 && zScore < 0)
			BuyMarket(-Position);
	}
}
