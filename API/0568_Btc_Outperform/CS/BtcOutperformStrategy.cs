using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BTC outperform strategy.
/// Compares weekly and quarterly closes.
/// Goes long when weekly price is above quarterly price, otherwise short.
/// </summary>
public class BtcOutperformStrategy : Strategy
{
	private readonly StrategyParam<DataType> _weeklyCandleType;
	private readonly StrategyParam<DataType> _quarterlyCandleType;

	private decimal? _weeklyPrice;
	private decimal? _quarterlyPrice;

	public DataType WeeklyCandleType { get => _weeklyCandleType.Value; set => _weeklyCandleType.Value = value; }
	public DataType QuarterlyCandleType { get => _quarterlyCandleType.Value; set => _quarterlyCandleType.Value = value; }

	public BtcOutperformStrategy()
	{
		_weeklyCandleType = Param(nameof(WeeklyCandleType), TimeSpan.FromDays(7).TimeFrame())
			.SetDisplay("Weekly Candle", "Weekly timeframe", "General");

		_quarterlyCandleType = Param(nameof(QuarterlyCandleType), TimeSpan.FromDays(90).TimeFrame())
			.SetDisplay("Quarterly Candle", "Quarterly timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, WeeklyCandleType), (Security, QuarterlyCandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var weeklySubscription = SubscribeCandles(WeeklyCandleType);
		weeklySubscription
			.Bind(ProcessWeekly)
			.Start();

		var quarterlySubscription = SubscribeCandles(QuarterlyCandleType);
		quarterlySubscription
			.Bind(ProcessQuarterly)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, weeklySubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessWeekly(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_weeklyPrice = candle.ClosePrice;
		TryTrade();
	}

	private void ProcessQuarterly(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_quarterlyPrice = candle.ClosePrice;
		TryTrade();
	}

	private void TryTrade()
	{
		if (_weeklyPrice is not decimal weekly || _quarterlyPrice is not decimal quarterly)
			return;

		if (weekly > quarterly && Position <= 0)
		{
			BuyMarket();
		}
		else if (quarterly > weekly && Position >= 0)
		{
			SellMarket();
		}
	}
}
