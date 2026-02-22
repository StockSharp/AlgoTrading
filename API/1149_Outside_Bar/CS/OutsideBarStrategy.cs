using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OutsideBarStrategy : Strategy
{
	private readonly StrategyParam<decimal> _entryPct;
	private readonly StrategyParam<decimal> _tpPct;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _prevCandle;
	private decimal _stopLoss;
	private decimal _takeProfit;

	public decimal EntryPct { get => _entryPct.Value; set => _entryPct.Value = value; }
	public decimal TpPct { get => _tpPct.Value; set => _tpPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OutsideBarStrategy()
	{
		_entryPct = Param(nameof(EntryPct), 0.5m);
		_tpPct = Param(nameof(TpPct), 1.0m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevCandle = null;
		_stopLoss = 0;
		_takeProfit = 0;

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

		// Exit management
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopLoss || candle.HighPrice >= _takeProfit)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopLoss || candle.LowPrice <= _takeProfit)
				BuyMarket(Math.Abs(Position));
		}

		if (_prevCandle != null && Position == 0)
		{
			var isOutsideBar = candle.HighPrice > _prevCandle.HighPrice && candle.LowPrice < _prevCandle.LowPrice;

			if (isOutsideBar)
			{
				var isBullish = candle.ClosePrice > candle.OpenPrice;
				var isBearish = candle.ClosePrice < candle.OpenPrice;
				var barSize = candle.HighPrice - candle.LowPrice;

				if (isBullish)
				{
					BuyMarket(Volume);
					_stopLoss = candle.LowPrice;
					_takeProfit = candle.HighPrice + barSize * TpPct;
				}
				else if (isBearish)
				{
					SellMarket(Volume);
					_stopLoss = candle.HighPrice;
					_takeProfit = candle.LowPrice - barSize * TpPct;
				}
			}
		}

		_prevCandle = candle;
	}
}
