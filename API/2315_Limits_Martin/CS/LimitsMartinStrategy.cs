namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class LimitsMartinStrategy : Strategy
{
	// Strategy parameters
	private readonly StrategyParam<int> _step;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<bool> _useMartingale;
	private readonly StrategyParam<int> _lossLimit;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<bool> _useMegaLot;
	private readonly StrategyParam<DataType> _candleType;

	// Internal fields
	private decimal _currentVolume;
	private int _lossCount;
	private decimal _entryPrice;

	public int Step { get => _step.Value; set => _step.Value = value; }
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public int TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public bool UseMartingale { get => _useMartingale.Value; set => _useMartingale.Value = value; }
	public int LossLimit { get => _lossLimit.Value; set => _lossLimit.Value = value; }
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }
	public bool UseMegaLot { get => _useMegaLot.Value; set => _useMegaLot.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LimitsMartinStrategy()
	{
		// Parameter initialization
		_step = Param(nameof(Step), 200)
			.SetDisplay("Step", "Distance to place limit orders in pips", "Parameters")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 30)
			.SetDisplay("Stop Loss", "Stop loss size in pips", "Risk")
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 60)
			.SetDisplay("Take Profit", "Take profit size in pips", "Risk")
			.SetCanOptimize(true);

		_useMartingale = Param(nameof(UseMartingale), true)
			.SetDisplay("Use Martingale", "Increase volume after loss", "Parameters");

		_lossLimit = Param(nameof(LossLimit), 10)
			.SetDisplay("Loss Limit", "Maximum consecutive losses for martingale", "Parameters")
			.SetCanOptimize(true);

		_volume = Param(nameof(Volume), 0.01m)
			.SetDisplay("Volume", "Base order volume", "Parameters");

		_useMegaLot = Param(nameof(UseMegaLot), true)
			.SetDisplay("Use MegaLot", "Double volume to recover loss", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for processing", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_currentVolume = default;
		_lossCount = default;
		_entryPrice = default;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(); // enable position protection
		_currentVolume = Volume;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Do(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// avoid overlapping orders
		if (Orders.Any(o => o.State == OrderStates.Active))
			return;

		var stepPrice = Step * Security.PriceStep;
		var price = candle.ClosePrice;

		if (Position == 0)
		{
			var buyPrice = price - stepPrice;
			var sellPrice = price + stepPrice;
			BuyLimit(buyPrice, _currentVolume); // place buy limit below price
			SellLimit(sellPrice, _currentVolume); // place sell limit above price
		}
		else if (Position > 0)
		{
			var stop = _entryPrice - StopLoss * Security.PriceStep;
			var take = _entryPrice + TakeProfit * Security.PriceStep;
			if (candle.LowPrice <= stop || candle.HighPrice >= take)
				ClosePosition(); // exit on stop or target
		}
		else if (Position < 0)
		{
			var stop = _entryPrice + StopLoss * Security.PriceStep;
			var take = _entryPrice - TakeProfit * Security.PriceStep;
			if (candle.HighPrice >= stop || candle.LowPrice <= take)
				ClosePosition();
		}
	}

	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (Position != 0)
		{
			_entryPrice = trade.Order.Price; // remember entry price
			return;
		}

		var pnl = trade.Order.Direction == Sides.Buy ? trade.Order.Price - _entryPrice : _entryPrice - trade.Order.Price;
		AdjustVolume(pnl); // adapt volume after trade
		_entryPrice = 0;
	}

	private void AdjustVolume(decimal pnl)
	{
		// martingale adjustment
		if (pnl < 0 && UseMartingale && _lossCount < LossLimit)
		{
			_lossCount++;
			_currentVolume = UseMegaLot ? _currentVolume * 2m : _currentVolume + Volume;
		}
		else
		{
			_lossCount = 0;
			_currentVolume = Volume;
		}
	}
}