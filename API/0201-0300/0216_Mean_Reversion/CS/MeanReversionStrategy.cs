using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean Reversion strategy.
/// The bands lie DeviationMultiplier standard deviations around the MovingAveragePeriod simple moving average, both measured over the same
/// candles. A close below the lower band goes long and a close above the upper band goes short, reversing an opposite position.
/// A long closes once price closes above the average and a short once it closes below, and a percent stop limits the loss.
/// </summary>
public class MeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _movingAveragePeriod;
	private readonly StrategyParam<decimal> _deviationMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Period of the moving average and the standard deviation.
	/// </summary>
	public int MovingAveragePeriod
	{
		get => _movingAveragePeriod.Value;
		set => _movingAveragePeriod.Value = value;
	}

	/// <summary>
	/// Standard deviations between the average and a band.
	/// </summary>
	public decimal DeviationMultiplier
	{
		get => _deviationMultiplier.Value;
		set => _deviationMultiplier.Value = value;
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
	public MeanReversionStrategy()
	{
		_movingAveragePeriod = Param(nameof(MovingAveragePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period of the moving average and the standard deviation", "Indicators");

		_deviationMultiplier = Param(nameof(DeviationMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation Multiplier", "Standard deviations between the average and a band", "Indicators");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		var ma = new SimpleMovingAverage { Length = MovingAveragePeriod };
		var stdev = new StandardDeviation { Length = MovingAveragePeriod };

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

		var ma = maValue.GetValue<decimal>();
		var deviation = stdDevValue.GetValue<decimal>() * DeviationMultiplier;
		var upper = ma + deviation;
		var lower = ma - deviation;
		var close = candle.ClosePrice;

		if (close < lower && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close > upper && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && close > ma)
			SellMarket(Position);
		else if (Position < 0 && close < ma)
			BuyMarket(-Position);
	}
}
