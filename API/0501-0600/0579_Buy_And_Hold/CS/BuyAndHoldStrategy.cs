namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Buys once at the start date and holds the long until the end date.
/// </summary>
public class BuyAndHoldStrategy : Strategy
{
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	private readonly StrategyParam<DataType> _candleType;
	private bool _entrySubmitted;
	private bool _exitSubmitted;

	public DateTimeOffset StartDate { get => _startDate.Value; set => _startDate.Value = value; }
	public DateTimeOffset EndDate { get => _endDate.Value; set => _endDate.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BuyAndHoldStrategy()
	{
		_startDate = Param(nameof(StartDate), new DateTimeOffset(2018, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Date", "Buy once on or after this date", "General");
		_endDate = Param(nameof(EndDate), new DateTimeOffset(2069, 12, 31, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("End Date", "Close the long on or after this date", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_entrySubmitted = _exitSubmitted = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.OpenTime >= EndDate.UtcDateTime)
		{
			if (!_exitSubmitted && Position > 0)
			{
				SellMarket(Position);
				_exitSubmitted = true;
			}
		}
		else if (!_entrySubmitted && candle.OpenTime >= StartDate.UtcDateTime && Position == 0)
		{
			BuyMarket();
			// Remember the submitted entry even while its fill is still pending.
			_entrySubmitted = true;
		}
	}
}
