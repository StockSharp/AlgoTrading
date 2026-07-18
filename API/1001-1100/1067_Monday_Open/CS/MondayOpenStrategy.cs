using System;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Opens a long trade at the start of the week and exits on Tuesday.
/// </summary>
public class MondayOpenStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private bool _tradeOpened;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MondayOpenStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_tradeOpened = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var day = candle.OpenTime.DayOfWeek;

		if (day == DayOfWeek.Monday && !_tradeOpened && Position <= 0)
		{
			BuyMarket();
			_tradeOpened = true;
		}
		else if (day == DayOfWeek.Tuesday && _tradeOpened && Position > 0)
		{
			SellMarket();
			_tradeOpened = false;
		}
		else if (day != DayOfWeek.Monday && day != DayOfWeek.Tuesday)
		{
			_tradeOpened = false;
		}
	}
}
