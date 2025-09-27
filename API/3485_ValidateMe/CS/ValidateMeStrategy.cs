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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that validates stop and take distances before sending a single market order.
/// The direction is configured by parameter and every trade is protected by fixed offsets in pips.
/// </summary>
public class ValidateMeStrategy : Strategy
{
	private readonly StrategyParam<int> _orderTakePips;
	private readonly StrategyParam<int> _orderStopPips;
	private readonly StrategyParam<decimal> _lots;
	private readonly StrategyParam<TradeDirection> _direction;

	private decimal _pipSize;

	/// <summary>
	/// Requested take-profit distance expressed in pips.
	/// </summary>
	public int OrderTakePips
	{
		get => _orderTakePips.Value;
		set => _orderTakePips.Value = value;
	}

	/// <summary>
	/// Requested stop-loss distance expressed in pips.
	/// </summary>
	public int OrderStopPips
	{
		get => _orderStopPips.Value;
		set => _orderStopPips.Value = value;
	}

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal Lots
	{
		get => _lots.Value;
		set
		{
			_lots.Value = value;
			Volume = value;
		}
	}

	/// <summary>
	/// Trade direction to execute when no open position exists.
	/// </summary>
	public TradeDirection Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
	}

	/// <summary>
	/// Available trade directions.
	/// </summary>
	public enum TradeDirection
	{
		Buy,
		Sell,
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ValidateMeStrategy"/> class.
	/// </summary>
	public ValidateMeStrategy()
	{
		_orderTakePips = Param(nameof(OrderTakePips), 50)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Take-profit offset expressed in pips", "Risk Management");

		_orderStopPips = Param(nameof(OrderStopPips), 50)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Stop-loss offset expressed in pips", "Risk Management");

		_lots = Param(nameof(Lots), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Lots", "Order volume in lots", "General");

		_direction = Param(nameof(Direction), TradeDirection.Buy)
			.SetDisplay("Direction", "Trade direction when signals align", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, DataType.Ticks);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = Lots;
		_pipSize = CalculatePipSize();

		var takeProfitOffset = OrderTakePips * _pipSize;
		var stopLossOffset = OrderStopPips * _pipSize;

		StartProtection(new Unit(takeProfitOffset, UnitTypes.Absolute), new Unit(stopLossOffset, UnitTypes.Absolute));

		SubscribeTicks()
			.Bind(ProcessTrade)
			.Start();
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;

		if (priceStep <= 0m)
			return 1m;

		var decimals = Security?.Decimals;

		if (decimals == 5 || decimals == 3)
			return priceStep * 10m;

		return priceStep;
	}

	private void ProcessTrade(ITickTradeMessage trade)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0m)
			return;

		if (HasActiveOrders())
			return;

		var volume = Lots;

		if (Direction == TradeDirection.Buy)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}
	}

	private bool HasActiveOrders()
	{
		foreach (var order in Orders)
		{
			if (order.State.IsActive())
				return true;
		}

		return false;
	}
}

