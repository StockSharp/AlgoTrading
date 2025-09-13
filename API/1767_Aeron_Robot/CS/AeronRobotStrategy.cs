using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Aeron robot grid strategy.
/// Opens a series of buy and sell orders at a fixed price distance.
/// Increases volume after each new order to average down.
/// </summary>
public class AeronRobotStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _firstLot;
	private readonly StrategyParam<decimal> _lotsFactor;
	private readonly StrategyParam<decimal> _gap;
	private readonly StrategyParam<decimal> _gapFactor;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<bool> _hedging;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastBuyPrice;
	private decimal _lastSellPrice;
	private int _buyCount;
	private int _sellCount;

	/// <summary>
	/// Profit target in points.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Initial order volume.
	/// </summary>
	public decimal FirstLot { get => _firstLot.Value; set => _firstLot.Value = value; }

	/// <summary>
	/// Multiplier for subsequent orders.
	/// </summary>
	public decimal LotsFactor { get => _lotsFactor.Value; set => _lotsFactor.Value = value; }

	/// <summary>
	/// Distance between grid levels in points.
	/// </summary>
	public decimal Gap { get => _gap.Value; set => _gap.Value = value; }

	/// <summary>
	/// Multiplier for gap after each trade.
	/// </summary>
	public decimal GapFactor { get => _gapFactor.Value; set => _gapFactor.Value = value; }

	/// <summary>
	/// Maximum number of trades per side.
	/// </summary>
	public int MaxTrades { get => _maxTrades.Value; set => _maxTrades.Value = value; }

	/// <summary>
	/// Allow simultaneous long and short positions.
	/// </summary>
	public bool Hedging { get => _hedging.Value; set => _hedging.Value = value; }

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public AeronRobotStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 19m)
			.SetDisplay("Take Profit (points)", "Profit target in points", "General")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss (points)", "Stop loss in points", "General")
			.SetCanOptimize(true);

		_firstLot = Param(nameof(FirstLot), 0.02m)
			.SetDisplay("First Lot", "Initial order volume", "Risk")
			.SetCanOptimize(true);

		_lotsFactor = Param(nameof(LotsFactor), 2.667m)
			.SetDisplay("Lots Factor", "Multiplier for subsequent orders", "Risk")
			.SetCanOptimize(true);

		_gap = Param(nameof(Gap), 150m)
			.SetDisplay("Positions Gap", "Distance between grid levels in points", "Grid")
			.SetCanOptimize(true);

		_gapFactor = Param(nameof(GapFactor), 1m)
			.SetDisplay("Gap Factor", "Multiplier for gap after each trade", "Grid")
			.SetCanOptimize(true);

		_maxTrades = Param(nameof(MaxTrades), 3)
			.SetDisplay("Max Trades", "Maximum number of trades per side", "Grid")
			.SetCanOptimize(true);

		_hedging = Param(nameof(Hedging), true)
			.SetDisplay("Hedging", "Allow simultaneous long and short", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}


	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_lastBuyPrice = 0m;
		_lastSellPrice = 0m;
		_buyCount = 0;
		_sellCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;
		var step = Security.PriceStep ?? 1m;

		CheckPositions(price, step);

		TryOpenBuy(price, step);
		TryOpenSell(price, step);
	}

	private void CheckPositions(decimal price, decimal step)
	{
		if (Position > 0)
		{
			var profit = price - _lastBuyPrice;
			if (profit >= TakeProfit * step || profit <= -StopLoss * step)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			var profit = _lastSellPrice - price;
			if (profit >= TakeProfit * step || profit <= -StopLoss * step)
				BuyMarket(Math.Abs(Position));
		}
	}

	private void TryOpenBuy(decimal price, decimal step)
	{
		if (_buyCount >= MaxTrades)
			return;

		var levelGap = Gap * (decimal)Math.Pow((double)GapFactor, _buyCount) * step;
		if (_buyCount == 0 || price <= _lastBuyPrice - levelGap)
		{
			var volume = FirstLot * (decimal)Math.Pow((double)LotsFactor, _buyCount);
			if (!Hedging && Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(volume);
			_lastBuyPrice = price;
			_buyCount++;
		}
	}

	private void TryOpenSell(decimal price, decimal step)
	{
		if (_sellCount >= MaxTrades)
			return;

		var levelGap = Gap * (decimal)Math.Pow((double)GapFactor, _sellCount) * step;
		if (_sellCount == 0 || price >= _lastSellPrice + levelGap)
		{
			var volume = FirstLot * (decimal)Math.Pow((double)LotsFactor, _sellCount);
			if (!Hedging && Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(volume);
			_lastSellPrice = price;
			_sellCount++;
		}
	}
}
