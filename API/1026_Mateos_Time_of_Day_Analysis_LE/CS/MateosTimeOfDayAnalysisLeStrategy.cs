using System;
using System.Linq;
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

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }
	public int EndHour { get => _endHour.Value; set => _endHour.Value = value; }

	public MateosTimeOfDayAnalysisLeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
		_startHour = Param(nameof(StartHour), 9);
		_endHour = Param(nameof(EndHour), 16);
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hour = candle.ServerTime.Hour;

		if (hour >= StartHour && hour < EndHour)
		{
			if (Position <= 0)
				BuyMarket();
		}
		else if (hour >= EndHour)
		{
			if (Position > 0)
				SellMarket();
		}
	}
}
