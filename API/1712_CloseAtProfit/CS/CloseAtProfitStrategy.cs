using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that closes all positions when profit target or loss limit is reached.
/// </summary>
public class CloseAtProfitStrategy : Strategy
{
	private readonly StrategyParam<bool> _useProfitToClose;
	private readonly StrategyParam<decimal> _profitToClose;
	private readonly StrategyParam<bool> _useLossToClose;
	private readonly StrategyParam<decimal> _lossToClose;
	private readonly StrategyParam<bool> _closePendingOrders;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Enable closing positions by reaching profit target.
	/// </summary>
	public bool UseProfitToClose
	{
		get => _useProfitToClose.Value;
		set => _useProfitToClose.Value = value;
	}

	/// <summary>
	/// Profit value that triggers position closing.
	/// </summary>
	public decimal ProfitToClose
	{
		get => _profitToClose.Value;
		set => _profitToClose.Value = value;
	}

	/// <summary>
	/// Enable closing positions by reaching loss limit.
	/// </summary>
	public bool UseLossToClose
	{
		get => _useLossToClose.Value;
		set => _useLossToClose.Value = value;
	}

	/// <summary>
	/// Loss value (absolute) that triggers position closing.
	/// </summary>
	public decimal LossToClose
	{
		get => _lossToClose.Value;
		set => _lossToClose.Value = value;
	}

	/// <summary>
	/// Cancel all active orders when closing.
	/// </summary>
	public bool ClosePendingOrders
	{
		get => _closePendingOrders.Value;
		set => _closePendingOrders.Value = value;
	}

	/// <summary>
	/// Candle type used to trigger periodic checks.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public CloseAtProfitStrategy()
	{
		_useProfitToClose = Param(nameof(UseProfitToClose), true)
			.SetDisplay("Use Profit Target", "Enable closing by profit value", "General");

		_profitToClose = Param(nameof(ProfitToClose), 20m)
			.SetDisplay("Profit Target", "Close when realized PnL reaches this value", "General");

		_useLossToClose = Param(nameof(UseLossToClose), false)
			.SetDisplay("Use Loss Limit", "Enable closing by drawdown", "General");

		_lossToClose = Param(nameof(LossToClose), 100m)
			.SetDisplay("Loss Limit", "Close when realized PnL falls below negative value", "General");

		_closePendingOrders = Param(nameof(ClosePendingOrders), true)
			.SetDisplay("Cancel Pending", "Cancel active orders when closing", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to drive checks", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
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

		var pnl = PnL;

		if (UseProfitToClose && pnl >= ProfitToClose)
		{
			LogInfo($"Profit target reached {pnl:F2} >= {ProfitToClose:F2}. Closing positions.");
			CloseAll();
		}
		else if (UseLossToClose && pnl <= -LossToClose)
		{
			LogInfo($"Loss limit reached {pnl:F2} <= {-LossToClose:F2}. Closing positions.");
			CloseAll();
		}
	}

	private void CloseAll()
	{
		if (Position > 0)
			SellMarket(Math.Abs(Position));
		else if (Position < 0)
			BuyMarket(Math.Abs(Position));

		if (ClosePendingOrders)
			CancelActiveOrders();
	}
}
