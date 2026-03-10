using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MateosTimeOfDayAnalysisLeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private DateTime _entryDate;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }
	public int EndHour { get => _endHour.Value; set => _endHour.Value = value; }

	public MateosTimeOfDayAnalysisLeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame());
		_startHour = Param(nameof(StartHour), 9);
		_endHour = Param(nameof(EndHour), 16);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryDate = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_entryDate = default;

		var dummyEma1 = new ExponentialMovingAverage { Length = 10 };
		var dummyEma2 = new ExponentialMovingAverage { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(dummyEma1, dummyEma2, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal d1, decimal d2)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hour = candle.ServerTime.Hour;
		var date = candle.ServerTime.Date;

		if (hour >= StartHour && hour < EndHour)
		{
			if (date.DayOfWeek == DayOfWeek.Monday && Position <= 0 && _entryDate != date)
			{
				BuyMarket();
				_entryDate = date;
			}
		}
		else if (hour >= EndHour || hour < StartHour)
		{
			if (Position > 0)
			{
				SellMarket();
				_entryDate = default;
			}
		}
	}
}
