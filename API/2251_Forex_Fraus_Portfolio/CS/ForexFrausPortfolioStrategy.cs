using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams %R based strategy with trailing stop logic.
/// </summary>
public class ForexFrausPortfolioStrategy : Strategy
{
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<decimal> _buyThreshold;
	private readonly StrategyParam<decimal> _sellThreshold;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _stopHour;
	private readonly StrategyParam<DataType> _candleType;

	private bool _okBuy;
	private bool _okSell;

	public int WprPeriod { get => _wprPeriod.Value; set => _wprPeriod.Value = value; }
	public decimal BuyThreshold { get => _buyThreshold.Value; set => _buyThreshold.Value = value; }
	public decimal SellThreshold { get => _sellThreshold.Value; set => _sellThreshold.Value = value; }
	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }
	public int StopHour { get => _stopHour.Value; set => _stopHour.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ForexFrausPortfolioStrategy()
	{
		_wprPeriod = Param(nameof(WprPeriod), 60)
			.SetDisplay("WPR Period", "Williams %R calculation period", "Parameters")
			.SetOptimize(20, 200, 20);

		_buyThreshold = Param(nameof(BuyThreshold), -90m)
			.SetDisplay("Buy Threshold", "Trigger level for long entry", "Parameters");

		_sellThreshold = Param(nameof(SellThreshold), -10m)
			.SetDisplay("Sell Threshold", "Trigger level for short entry", "Parameters");

		_startHour = Param(nameof(StartHour), 0)
			.SetDisplay("Start Hour", "Trading start hour", "Time");

		_stopHour = Param(nameof(StopHour), 24)
			.SetDisplay("Stop Hour", "Trading stop hour", "Time");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_okBuy = false;
		_okSell = false;

		var wpr = new WilliamsR { Length = WprPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(wpr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, wpr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wprValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var hour = candle.OpenTime.Hour;
		var inTime = StartHour <= StopHour
			? hour >= StartHour && hour < StopHour
			: hour >= StartHour || hour < StopHour;

		if (!inTime)
		{
			if (Position > 0)
				SellMarket();
			else if (Position < 0)
				BuyMarket();
			return;
		}

		// WPR dips below buy threshold => arm buy signal
		if (wprValue < BuyThreshold)
			_okBuy = true;

		// WPR crosses back above buy threshold while armed => buy
		if (wprValue > BuyThreshold && _okBuy)
		{
			_okBuy = false;
			if (Position <= 0)
				BuyMarket();
			return;
		}

		// WPR rises above sell threshold => arm sell signal
		if (wprValue > SellThreshold)
			_okSell = true;

		// WPR crosses back below sell threshold while armed => sell
		if (wprValue < SellThreshold && _okSell)
		{
			_okSell = false;
			if (Position >= 0)
				SellMarket();
		}
	}
}
