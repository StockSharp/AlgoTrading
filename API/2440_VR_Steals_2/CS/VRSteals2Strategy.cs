using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Opens a single long position and manages fixed take profit, stop loss and breakeven.
/// </summary>
public class VRSteals2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _breakeven;
	private readonly StrategyParam<decimal> _breakevenOffset;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private bool _breakevenActivated;

	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal Breakeven { get => _breakeven.Value; set => _breakeven.Value = value; }
	public decimal BreakevenOffset { get => _breakevenOffset.Value; set => _breakevenOffset.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VRSteals2Strategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 50m).SetDisplay("Take Profit", "Distance to take profit in steps", "General").SetCanOptimize(true);
		_stopLoss = Param(nameof(StopLoss), 50m).SetDisplay("Stop Loss", "Distance to stop loss in steps", "General").SetCanOptimize(true);
		_breakeven = Param(nameof(Breakeven), 20m).SetDisplay("Breakeven", "Distance to activate breakeven in steps", "General").SetCanOptimize(true);
		_breakevenOffset = Param(nameof(BreakevenOffset), 9m).SetDisplay("Breakeven Offset", "Offset applied when breakeven is triggered", "General").SetCanOptimize(true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type", "Type of candles to process", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryPrice = 0m;
		_stopPrice = 0m;
		_breakevenActivated = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var sub = SubscribeCandles(CandleType);
		sub.Bind(ProcessCandle).Start();

		BuyMarket();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position <= 0)
			return;

		var price = candle.ClosePrice;
		var step = Security.PriceStep ?? 1m;

		if (!_breakevenActivated && Breakeven > 0m && price >= _entryPrice + Breakeven * step)
		{
			_stopPrice = _entryPrice + BreakevenOffset * step;
			_breakevenActivated = true;
		}

		if (TakeProfit > 0m && price >= _entryPrice + TakeProfit * step)
		{
			SellMarket(Position);
			return;
		}

		var stop = _breakevenActivated
			? _stopPrice
			: (StopLoss > 0m ? _entryPrice - StopLoss * step : decimal.MinValue);

		if (price <= stop)
			SellMarket(Position);
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (Position > 0 && _entryPrice == 0m)
			_entryPrice = trade.Trade.Price;
		else if (Position == 0)
		{
			_entryPrice = 0m;
			_breakevenActivated = false;
			_stopPrice = 0m;
		}
	}
}
