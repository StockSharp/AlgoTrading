namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// Strategy that reproduces the MetaTrader HistTraining expert advisor.
/// It listens to manual triggers that mimic the global variables used in MQL.
/// </summary>
public class HistTrainingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _buyTrigger;
	private readonly StrategyParam<bool> _sellTrigger;
	private readonly StrategyParam<bool> _closeTrigger;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Initializes a new instance of the <see cref="HistTrainingStrategy"/>.
	/// </summary>
	public HistTrainingStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order volume", "Default trade size for market orders.", "Trading");

		_buyTrigger = Param(nameof(BuyTrigger), false)
			.SetDisplay("Buy trigger", "Set to true to request a new long position when the strategy is flat.", "Manual signals")
			.SetCanOptimize(false);

		_sellTrigger = Param(nameof(SellTrigger), false)
			.SetDisplay("Sell trigger", "Set to true to request a new short position when the strategy is flat.", "Manual signals")
			.SetCanOptimize(false);

		_closeTrigger = Param(nameof(CloseTrigger), false)
			.SetDisplay("Close trigger", "Set to true to close the current position regardless of its direction.", "Manual signals")
			.SetCanOptimize(false);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle series used only as a heartbeat for polling the manual triggers.", "Data")
			.SetCanOptimize(false);
	}

	/// <summary>
	/// Order volume used for market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set
		{
			_orderVolume.Value = value;
			Volume = value; // Keep the base strategy volume aligned with the configured trade size
		}
	}

	/// <summary>
	/// Manual flag that opens a long position when enabled.
	/// </summary>
	public bool BuyTrigger
	{
		get => _buyTrigger.Value;
		set => _buyTrigger.Value = value;
	}

	/// <summary>
	/// Manual flag that opens a short position when enabled.
	/// </summary>
	public bool SellTrigger
	{
		get => _sellTrigger.Value;
		set => _sellTrigger.Value = value;
	}

	/// <summary>
	/// Manual flag that forces the strategy to flatten its exposure.
	/// </summary>
	public bool CloseTrigger
	{
		get => _closeTrigger.Value;
		set => _closeTrigger.Value = value;
	}

	/// <summary>
	/// Candle type that drives the polling loop for manual triggers.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume; // Ensure helper shortcuts use the requested trade size

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(); // Activate position protection once, mirroring the recommended pattern
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return; // Operate only on completed candles, the same as the original expert

		if (!IsFormedAndOnlineAndAllowTrading())
			return; // Do not submit orders if the strategy is not fully operational

		TryExecuteManualSignals();
	}

	private void TryExecuteManualSignals()
	{
		var volume = OrderVolume;
		if (volume <= 0m)
			return; // Volume parameter validation should avoid this situation, but keep a defensive check

		if (BuyTrigger && Position == 0m)
		{
			BuyMarket(volume); // Recreate the OrderSend call that buys when global flag 97 equals 1
			BuyTrigger = false; // Reset the flag after the corresponding order is dispatched
		}

		if (SellTrigger && Position == 0m)
		{
			SellMarket(volume); // Recreate the OrderSend call that sells when global flag 98 equals 1
			SellTrigger = false; // Reset the flag after submitting the sell order
		}

		if (CloseTrigger && Position != 0m)
		{
			ClosePosition(); // Match the OrderClose logic driven by global flag 99
			CloseTrigger = false; // Reset the close request once the position has been flattened
		}
	}
}
