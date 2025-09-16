namespace StockSharp.Samples.Strategies;

using System;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;

/// <summary>
/// Simplified Visual Trader order manager.
/// Sends a single market order and attaches stop-loss and take-profit protection.
/// </summary>
public class VisualTraderSimulatorEditionStrategy : Strategy
{
	public enum TradeDirection
	{
		Buy,
		Sell
	}

	private readonly StrategyParam<TradeDirection> _tradeDirection;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	public VisualTraderSimulatorEditionStrategy()
	{
		_tradeDirection = Param(nameof(Direction), TradeDirection.Buy)
			.SetDisplay("Trade Direction", "Initial trade direction", "General");

		_takeProfit = Param(nameof(TakeProfit), 0m)
			.SetDisplay("Take Profit", "Take profit in absolute price", "Protection")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetDisplay("Stop Loss", "Stop loss in absolute price", "Protection")
			.SetCanOptimize(true);

		Volume = 1;
	}

	/// <summary>
	/// Direction of the initial trade.
	/// </summary>
	public TradeDirection Direction
	{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
	}

	/// <summary>
	/// Take profit value in absolute price. Zero disables take profit.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss value in absolute price. Zero disables stop loss.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(
			takeProfit: TakeProfit > 0 ? new Unit(TakeProfit, UnitTypes.Absolute) : default,
			stopLoss: StopLoss > 0 ? new Unit(StopLoss, UnitTypes.Absolute) : default);

		if (Direction == TradeDirection.Buy)
			BuyMarket();
		else
			SellMarket();
	}
}
