using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 80-20 strategy - trades when price closes near extremes of the candle.
/// </summary>
public class EightyTwentyStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _rangePercent;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fraction of candle range defining trigger zone.
	/// </summary>
	public decimal RangePercent
	{
		get => _rangePercent.Value;
		set => _rangePercent.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public EightyTwentyStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_rangePercent = Param(nameof(RangePercent), 0.2m)
			.SetRange(0.05m, 0.5m)
			.SetDisplay("Range Percent", "Fraction of candle range for trigger zone", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.3m, 0.05m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var range = candle.HighPrice - candle.LowPrice;
		var offset = RangePercent * range;

		var triggerGreen = candle.ClosePrice >= candle.HighPrice - offset &&
			candle.OpenPrice <= candle.LowPrice + offset;

		var triggerRed = candle.OpenPrice >= candle.HighPrice - offset &&
			candle.ClosePrice <= candle.LowPrice + offset;

		if (triggerGreen && Position <= 0)
		{
			CancelActiveOrders();
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (triggerRed && Position >= 0)
		{
			CancelActiveOrders();
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}

