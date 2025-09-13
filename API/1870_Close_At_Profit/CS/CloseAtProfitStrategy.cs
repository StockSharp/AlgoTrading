using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Closes all positions when floating profit or loss reaches predefined thresholds.
/// </summary>
public class CloseAtProfitStrategy : Strategy
{
	private readonly StrategyParam<bool> _useProfitToClose;
	private readonly StrategyParam<decimal> _profitToClose;
	private readonly StrategyParam<bool> _useLossToClose;
	private readonly StrategyParam<decimal> _lossToClose;
	private readonly StrategyParam<bool> _allSymbols;
	private readonly StrategyParam<bool> _pendingOrders;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Enable closing by profit.
	/// </summary>
	public bool UseProfitToClose { get => _useProfitToClose.Value; set => _useProfitToClose.Value = value; }

	/// <summary>
	/// Profit target to trigger closing.
	/// </summary>
	public decimal ProfitToClose { get => _profitToClose.Value; set => _profitToClose.Value = value; }

	/// <summary>
	/// Enable closing by loss.
	/// </summary>
	public bool UseLossToClose { get => _useLossToClose.Value; set => _useLossToClose.Value = value; }

	/// <summary>
	/// Loss threshold to trigger closing.
	/// </summary>
	public decimal LossToClose { get => _lossToClose.Value; set => _lossToClose.Value = value; }

	/// <summary>
	/// Sum profit across all securities of the strategy.
	/// </summary>
	public bool AllSymbols { get => _allSymbols.Value; set => _allSymbols.Value = value; }

	/// <summary>
	/// Cancel pending orders when closing.
	/// </summary>
	public bool PendingOrders { get => _pendingOrders.Value; set => _pendingOrders.Value = value; }

	/// <summary>
	/// Candle series used for price tracking.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	private decimal _lastPrice;

	/// <summary>
	/// Initializes a new instance of <see cref="CloseAtProfitStrategy"/>.
	/// </summary>
	public CloseAtProfitStrategy()
	{
		_useProfitToClose = Param(nameof(UseProfitToClose), true)
			.SetDisplay("Use Profit To Close", "Enable profit based closing", "General");

		_profitToClose = Param(nameof(ProfitToClose), 100m)
			.SetDisplay("Profit To Close", "Profit target for closing", "General");

		_useLossToClose = Param(nameof(UseLossToClose), false)
			.SetDisplay("Use Loss To Close", "Enable loss based closing", "General");

		_lossToClose = Param(nameof(LossToClose), 100m)
			.SetDisplay("Loss To Close", "Loss threshold for closing", "General");

		_allSymbols = Param(nameof(AllSymbols), true)
			.SetDisplay("All Symbols", "Track all strategy securities", "General");

		_pendingOrders = Param(nameof(PendingOrders), true)
			.SetDisplay("Pending Orders", "Cancel active orders when closing", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for price tracking", "General");
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

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastPrice = candle.ClosePrice;

		var profit = Position * (_lastPrice - Position.AveragePrice);

		if (UseProfitToClose && profit >= ProfitToClose)
		{
			ClosePositions();
		}
		else if (UseLossToClose && profit <= -LossToClose)
		{
			ClosePositions();
		}
	}

	private void ClosePositions()
	{
		if (PendingOrders)
			CancelActiveOrders();

		if (AllSymbols)
		{
			foreach (var position in Positions)
			{
				var volume = GetPositionValue(position.Security, Portfolio) ?? 0m;
				if (volume > 0)
					SellMarket(volume, position.Security);
				else if (volume < 0)
					BuyMarket(-volume, position.Security);
			}
		}
		else
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(-Position);
		}
	}
}
