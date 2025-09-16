using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pending stop order breakout strategy converted from the Ambush MQL5 expert.
/// Places symmetric buy stop and sell stop orders around the market and trails them.
/// </summary>
public class AmbushStrategy : Strategy
{
	private readonly StrategyParam<decimal> _indentationPoints;
	private readonly StrategyParam<decimal> _maxSpreadPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<TimeSpan> _pause;
	private readonly StrategyParam<decimal> _equityTakeProfit;
	private readonly StrategyParam<decimal> _equityStopLoss;

	private decimal _bestBid;
	private decimal _bestAsk;
	private DateTimeOffset _lastTrailTime;
	private Order _buyStopOrder;
	private Order _sellStopOrder;

	/// <summary>
	/// Distance from the market price to the pending stop orders, in points.
	/// </summary>
	public decimal IndentationPoints
	{
		get => _indentationPoints.Value;
		set => _indentationPoints.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread, in points.
	/// </summary>
	public decimal MaxSpreadPoints
	{
		get => _maxSpreadPoints.Value;
		set => _maxSpreadPoints.Value = value;
	}

	/// <summary>
	/// Trailing distance for pending orders, in points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Trailing step added to the base trailing distance, in points.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Pause between trailing recalculations.
	/// </summary>
	public TimeSpan Pause
	{
		get => _pause.Value;
		set => _pause.Value = value;
	}

	/// <summary>
	/// Target equity profit that triggers position flattening.
	/// </summary>
	public decimal EquityTakeProfit
	{
		get => _equityTakeProfit.Value;
		set => _equityTakeProfit.Value = value;
	}

	/// <summary>
	/// Maximum equity drawdown allowed before flattening positions.
	/// </summary>
	public decimal EquityStopLoss
	{
		get => _equityStopLoss.Value;
		set => _equityStopLoss.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AmbushStrategy"/> class.
	/// </summary>
	public AmbushStrategy()
	{
		_indentationPoints = Param(nameof(IndentationPoints), 10m)
			.SetGreaterThanOrEqual(0m)
			.SetDisplay("Indentation (points)", "Distance from price for pending stops", "Orders");

		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 5m)
			.SetGreaterThanOrEqual(0m)
			.SetDisplay("Max Spread (points)", "Maximum allowed spread", "Orders");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 10m)
			.SetGreaterThanOrEqual(0m)
			.SetDisplay("Trailing Stop (points)", "Base trailing distance", "Orders");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 1m)
			.SetGreaterThanOrEqual(0m)
			.SetDisplay("Trailing Step (points)", "Additional trailing offset", "Orders");

		_pause = Param(nameof(Pause), TimeSpan.FromSeconds(1))
			.SetDisplay("Pause", "Pause between trailing recalculations", "Orders");

		_equityTakeProfit = Param(nameof(EquityTakeProfit), 15m)
			.SetGreaterThanOrEqual(0m)
			.SetDisplay("Equity Take Profit", "Flatten positions once this profit is reached", "Risk");

		_equityStopLoss = Param(nameof(EquityStopLoss), 5m)
			.SetGreaterThanOrEqual(0m)
			.SetDisplay("Equity Stop Loss", "Flatten positions after this loss", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bestBid = 0m;
		_bestAsk = 0m;
		_lastTrailTime = DateTimeOffset.MinValue;
		_buyStopOrder = null;
		_sellStopOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_bestBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_bestAsk = (decimal)ask;

		if (_bestBid == 0m || _bestAsk == 0m)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		EnforceEquityTargets();
		ManageStopOrders();
		UpdateTrailingOrders();
	}

	private void EnforceEquityTargets()
	{
		var totalProfit = CalculateEquityPnL();

		if (EquityTakeProfit > 0m && totalProfit >= EquityTakeProfit)
		{
			FlattenPosition();
			return;
		}

		if (EquityStopLoss > 0m && totalProfit <= -EquityStopLoss)
			FlattenPosition();
	}

	private void ManageStopOrders()
	{
		CleanupOrder(ref _buyStopOrder);
		CleanupOrder(ref _sellStopOrder);

		var spread = _bestAsk - _bestBid;
		if (spread <= 0m)
			return;

		var step = Security.PriceStep ?? 1m;
		var maxSpread = MaxSpreadPoints <= 0m ? decimal.MaxValue : MaxSpreadPoints * step;

		if (spread > maxSpread)
			return;

		var indentation = Math.Max(GetPriceOffset(IndentationPoints), spread * 3m);

		if (!IsOrderActive(_buyStopOrder))
		{
			var price = NormalizePrice(_bestAsk + indentation);
			_buyStopOrder = BuyStop(Volume, price);
		}

		if (!IsOrderActive(_sellStopOrder))
		{
			var price = NormalizePrice(_bestBid - indentation);
			_sellStopOrder = SellStop(Volume, price);
		}
	}

	private void UpdateTrailingOrders()
	{
		if (TrailingStopPoints <= 0m)
			return;

		if (!IsOrderActive(_buyStopOrder) && !IsOrderActive(_sellStopOrder))
			return;

		var now = CurrentTime;
		if (_lastTrailTime != DateTimeOffset.MinValue && Pause > TimeSpan.Zero && now - _lastTrailTime < Pause)
			return;

		_lastTrailTime = now;

		var spread = _bestAsk - _bestBid;
		if (spread <= 0m)
			return;

		var trailingBase = GetPriceOffset(TrailingStopPoints) + GetPriceOffset(TrailingStepPoints);
		var trailingDistance = Math.Max(trailingBase, spread * 3m);

		if (IsOrderActive(_buyStopOrder))
		{
			var newPrice = NormalizePrice(_bestAsk + trailingDistance);
			if (NeedReRegister(_buyStopOrder, newPrice))
			{
				var volume = _buyStopOrder.Volume ?? Volume;
				CancelOrder(_buyStopOrder);
				_buyStopOrder = BuyStop(volume, newPrice);
			}
		}

		if (IsOrderActive(_sellStopOrder))
		{
			var newPrice = NormalizePrice(_bestBid - trailingDistance);
			if (NeedReRegister(_sellStopOrder, newPrice))
			{
				var volume = _sellStopOrder.Volume ?? Volume;
				CancelOrder(_sellStopOrder);
				_sellStopOrder = SellStop(volume, newPrice);
			}
		}
	}

	private void FlattenPosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);
	}

	private decimal CalculateEquityPnL()
	{
		var result = PnL;

		if (Position == 0)
			return result;

		if (_bestBid == 0m || _bestAsk == 0m)
			return result;

		var exitPrice = Position > 0 ? _bestBid : _bestAsk;
		var entryPrice = PositionPrice;
		result += (exitPrice - entryPrice) * Position;

		return result;
	}

	private static void CleanupOrder(ref Order order)
	{
		if (order == null)
			return;

		switch (order.State)
		{
			case OrderStates.Done:
			case OrderStates.Failed:
			case OrderStates.Canceled:
				order = null;
				break;
		}
	}

	private bool NeedReRegister(Order order, decimal newPrice)
	{
		if (order.Price is not decimal existingPrice)
			return true;

		var diff = Math.Abs(existingPrice - newPrice);
		if (diff == 0m)
			return false;

		var step = Security.PriceStep;
		if (step is null || step == 0m)
			return true;

		return diff >= step / 2m;
	}

	private decimal GetPriceOffset(decimal points)
	{
		if (points <= 0m)
			return 0m;

		var step = Security.PriceStep ?? 1m;
		return points * step;
	}

	private decimal NormalizePrice(decimal price)
	{
		var step = Security.PriceStep;
		if (step is { } s && s > 0m)
		{
			var steps = Math.Round(price / s, MidpointRounding.AwayFromZero);
			return steps * s;
		}

		return price;
	}
}
