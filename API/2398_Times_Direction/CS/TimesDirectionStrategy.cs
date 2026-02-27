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
/// Time based strategy that opens and closes a position at specified hours of the day.
/// Opens at OpenHour and closes at CloseHour, with SL/TP protection.
/// </summary>
public class TimesDirectionStrategy : Strategy
{
	private readonly StrategyParam<int> _openHour;
	private readonly StrategyParam<int> _closeHour;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	public int OpenHour { get => _openHour.Value; set => _openHour.Value = value; }
	public int CloseHour { get => _closeHour.Value; set => _closeHour.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TimesDirectionStrategy()
	{
		_openHour = Param(nameof(OpenHour), 2)
			.SetDisplay("Open Hour", "Hour of day to open position (UTC)", "General");

		_closeHour = Param(nameof(CloseHour), 14)
			.SetDisplay("Close Hour", "Hour of day to close position (UTC)", "General");

		_stopLoss = Param(nameof(StopLoss), 500m)
			.SetDisplay("Stop Loss", "Stop loss distance in price units", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 1000m)
			.SetDisplay("Take Profit", "Take profit distance in price units", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0m;

		var sub = SubscribeCandles(CandleType);
		sub.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hour = candle.OpenTime.Hour;

		if (Position == 0)
		{
			if (hour == OpenHour)
			{
				_entryPrice = candle.ClosePrice;
				BuyMarket();
			}
		}
		else
		{
			// Close at specified hour
			if (hour == CloseHour)
			{
				SellMarket();
				_entryPrice = 0m;
				return;
			}

			// SL/TP check
			if (_entryPrice != 0m && Position > 0)
			{
				var sl = _entryPrice - StopLoss;
				var tp = _entryPrice + TakeProfit;
				if (candle.LowPrice <= sl || candle.HighPrice >= tp)
				{
					SellMarket();
					_entryPrice = 0m;
				}
			}
		}
	}
}
