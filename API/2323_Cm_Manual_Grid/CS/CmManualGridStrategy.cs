using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid strategy that places buy/sell orders at regular intervals
/// based on price distance from a reference level (SMA).
/// Buys below SMA, sells above SMA with grid step spacing.
/// </summary>
public class CmManualGridStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<decimal> _gridStep;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastBuyPrice;
	private decimal _lastSellPrice;

	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }
	public decimal GridStep { get => _gridStep.Value; set => _gridStep.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public CmManualGridStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "Moving average period for center", "Indicators");

		_gridStep = Param(nameof(GridStep), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Grid Step", "Price distance between grid levels", "Grid");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_lastBuyPrice = 0;
		_lastSellPrice = 0;

		var sma = new SimpleMovingAverage { Length = SmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;
		var step = GridStep;

		// Buy when price is below SMA and spaced from last buy
		if (price < smaValue - step)
		{
			if (_lastBuyPrice == 0 || price <= _lastBuyPrice - step)
			{
				BuyMarket();
				_lastBuyPrice = price;
			}
		}

		// Sell when price is above SMA and spaced from last sell
		if (price > smaValue + step)
		{
			if (_lastSellPrice == 0 || price >= _lastSellPrice + step)
			{
				SellMarket();
				_lastSellPrice = price;
			}
		}

		// Reset grid when price returns to center
		if (price > smaValue && _lastBuyPrice != 0)
			_lastBuyPrice = 0;
		if (price < smaValue && _lastSellPrice != 0)
			_lastSellPrice = 0;
	}
}
