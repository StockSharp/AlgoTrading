using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OvernightEffectHighVolatilityCryptoStrategy : Strategy
{
	private readonly StrategyParam<int> _entryHour;
	private readonly StrategyParam<int> _exitHour;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private bool _inTrade;

	public int EntryHour { get => _entryHour.Value; set => _entryHour.Value = value; }
	public int ExitHour { get => _exitHour.Value; set => _exitHour.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OvernightEffectHighVolatilityCryptoStrategy()
	{
		_entryHour = Param(nameof(EntryHour), 20);
		_exitHour = Param(nameof(ExitHour), 8);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevClose = 0;
		_inTrade = false;

		var sma = new SimpleMovingAverage { Length = 20 };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_prevClose = candle.ClosePrice;
		var hour = candle.OpenTime.Hour;

		if (hour == EntryHour && !_inTrade && Position == 0)
		{
			BuyMarket(Volume);
			_inTrade = true;
		}
		else if (hour == ExitHour && _inTrade && Position > 0)
		{
			SellMarket(Math.Abs(Position));
			_inTrade = false;
		}
	}
}
