using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trading Boxing strategy (simplified). Uses CCI momentum for entries.
/// </summary>
public class TradingBoxingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciLength;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int CciLength
	{
		get => _cciLength.Value;
		set => _cciLength.Value = value;
	}

	public TradingBoxingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");

		_cciLength = Param(nameof(CciLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI Length", "CCI period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var cci = new CommodityChannelIndex { Length = CciLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(cci, (ICandleMessage candle, decimal cciValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (cciValue < -100 && Position <= 0)
				{
					BuyMarket();
				}
				else if (cciValue > 100 && Position >= 0)
				{
					SellMarket();
				}
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
}
