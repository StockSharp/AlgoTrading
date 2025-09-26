using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Risk management helper that closes positions shortly before the protective stop would be hit.
/// Ported from the Dual StopLoss.mq4 expert by RayanTech.
/// </summary>
public class DualStoplossStrategy : Strategy
{
	private static readonly Level1Fields? StopLevelField = TryGetField("StopLevel")
		?? TryGetField("MinStopPrice")
		?? TryGetField("StopPrice")
		?? TryGetField("StopDistance");

	private readonly StrategyParam<decimal> _whenToClosePoints;

	private decimal _pointValue;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal? _stopLevelDistance;

	/// <summary>
	/// Distance from the stop loss level (in MetaTrader points) that should trigger an early exit.
	/// </summary>
	public decimal WhenToClosePoints
	{
		get => _whenToClosePoints.Value;
		set => _whenToClosePoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DualStoplossStrategy"/> class.
	/// </summary>
	public DualStoplossStrategy()
	{
		_whenToClosePoints = Param(nameof(WhenToClosePoints), 10m)
			.SetDisplay("Close distance (points)", "Distance from stop loss to trigger early exit (MetaTrader points).", "Risk Management")
			.SetCanOptimize(true)
			.SetMinValue(0m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, DataType.Level1);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pointValue = 0m;
		_bestBid = null;
		_bestAsk = null;
		_stopLevelDistance = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_pointValue = CalculatePointValue();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		TryCloseBeforeStop();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		TryCloseBeforeStop();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		TryCloseBeforeStop();
	}

	/// <inheritdoc />
	protected override void OnOrderRegistered(Order order)
	{
		base.OnOrderRegistered(order);

		TryCloseBeforeStop();
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		TryCloseBeforeStop();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid)
			_bestBid = bid;

		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask)
			_bestAsk = ask;

		if (StopLevelField is Level1Fields stopField && message.Changes.TryGetValue(stopField, out var stopValue))
		{
			var value = ToDecimal(stopValue);
			if (value is decimal stop && stop >= 0m)
				_stopLevelDistance = stop;
		}

		TryCloseBeforeStop();
	}

	private void TryCloseBeforeStop()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0m)
		{
			var stopPrice = GetNearestStopPrice(Sides.Sell);
			if (stopPrice is null)
				return;

			if (_bestBid is not decimal bid)
				return;

			var triggerDistance = GetTriggerDistance();
			var distance = bid - stopPrice.Value;

			// Close the long position before the protective stop fires.
			if (distance <= triggerDistance)
				ClosePosition();
		}
		else if (Position < 0m)
		{
			var stopPrice = GetNearestStopPrice(Sides.Buy);
			if (stopPrice is null)
				return;

			if (_bestAsk is not decimal ask)
				return;

			var triggerDistance = GetTriggerDistance();
			var distance = stopPrice.Value - ask;

			// Close the short position before the protective stop fires.
			if (distance <= triggerDistance)
				ClosePosition();
		}
	}

	private decimal GetTriggerDistance()
	{
		var pointDistance = WhenToClosePoints * _pointValue;
		var stopLevel = Math.Max(_stopLevelDistance ?? 0m, 0m);
		return pointDistance + stopLevel;
	}

	private decimal? GetNearestStopPrice(Sides side)
	{
		decimal? result = null;

		foreach (var order in Orders)
		{
			if (order.Security != Security)
				continue;

			if (order.State != OrderStates.Active)
				continue;

			if (order.Side != side)
				continue;

			if (order.Type != OrderTypes.Stop && order.Type != OrderTypes.StopLimit)
				continue;

			var price = order.StopPrice ?? order.Price;
			if (price <= 0m)
				continue;

			if (result is null)
			{
				result = price;
			}
			else if (side == Sides.Sell)
			{
				if (price > result.Value)
					result = price;
			}
			else if (price < result.Value)
			{
				result = price;
			}
		}

		return result;
	}

	private decimal CalculatePointValue()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		{
			var decimals = security.Decimals;
			if (decimals != null && decimals.Value > 0)
				step = (decimal)Math.Pow(10, -decimals.Value);
		}

		if (step <= 0m)
			step = 0.0001m;

		var multiplier = 1m;
		var digits = security.Decimals;
		if (digits != null && (digits.Value == 3 || digits.Value == 5))
			multiplier = 10m;

		return step * multiplier;
	}

	private static Level1Fields? TryGetField(string name)
	{
		return Enum.TryParse(name, out Level1Fields field) ? field : null;
	}

	private static decimal? ToDecimal(object value)
	{
		return value switch
		{
			decimal dec => dec,
			double dbl => (decimal)dbl,
			float fl => (decimal)fl,
			long l => l,
			int i => i,
			short s => s,
			byte b => b,
			null => null,
			IConvertible convertible => Convert.ToDecimal(convertible),
			_ => null,
		};
	}
}
