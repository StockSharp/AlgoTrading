using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.MatchingEngine;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum LbsMoneyMode
{
	FixedLot,
	RiskPercent,
}

/// <summary>
/// Previous-candle breakout strategy with paired conditional stops and Level1 trailing.
/// </summary>
public class LbsStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<LbsMoneyMode> _moneyMode;
	private readonly StrategyParam<decimal> _volumeOrRisk;
	private readonly StrategyParam<int> _hour1;
	private readonly StrategyParam<int> _hour2;
	private readonly StrategyParam<int> _hour3;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _bid;
	private decimal? _ask;
	private Order _buyPending;
	private Order _sellPending;
	private Order _protectiveStop;
	private decimal _entryPrice;

	public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }
	public int TrailingStopPips { get => _trailingStopPips.Value; set => _trailingStopPips.Value = value; }
	public int TrailingStepPips { get => _trailingStepPips.Value; set => _trailingStepPips.Value = value; }
	public LbsMoneyMode MoneyMode { get => _moneyMode.Value; set => _moneyMode.Value = value; }
	public decimal VolumeOrRisk { get => _volumeOrRisk.Value; set => _volumeOrRisk.Value = value; }
	public int Hour1 { get => _hour1.Value; set => _hour1.Value = value; }
	public int Hour2 { get => _hour2.Value; set => _hour2.Value = value; }
	public int Hour3 { get => _hour3.Value; set => _hour3.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LbsStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 50).SetNotNegative();
		_trailingStopPips = Param(nameof(TrailingStopPips), 5).SetNotNegative();
		_trailingStepPips = Param(nameof(TrailingStepPips), 15).SetNotNegative();
		_moneyMode = Param(nameof(MoneyMode), LbsMoneyMode.FixedLot);
		_volumeOrRisk = Param(nameof(VolumeOrRisk), 1m).SetGreaterThanZero();
		_hour1 = Param(nameof(Hour1), 10).SetRange(0, 23);
		_hour2 = Param(nameof(Hour2), 11).SetRange(0, 23);
		_hour3 = Param(nameof(Hour3), 12).SetRange(0, 23);
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_bid = null;
		_ask = null;
		_buyPending = null;
		_sellPending = null;
		_protectiveStop = null;
		_entryPrice = 0m;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		SubscribeLevel1().Bind(ProcessLevel1).Start();
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || Position != 0m)
			return;

		var hour = candle.CloseTime.Hour;
		if (!IsTradingHour(hour) || _bid is not decimal bid || _ask is not decimal ask)
			return;

		CancelEntryStops();

		var point = GetPoint();
		var (buyPrice, sellPrice) = CalculateBreakoutLevels(
			candle.HighPrice, candle.LowPrice, bid, ask, point);

		var volume = CalculateVolume(point);
		if (volume <= 0m)
			return;

		_buyPending = CreateStopOrder(Sides.Buy, buyPrice, volume, "LBS buy breakout");
		_sellPending = CreateStopOrder(Sides.Sell, sellPrice, volume, "LBS sell breakout");

		RegisterOrder(_buyPending);
		RegisterOrder(_sellPending);
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid && bid > 0m)
			_bid = bid;
		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask && ask > 0m)
			_ask = ask;

		if (Position == 0m || _protectiveStop is null || TrailingStopPips <= 0 || TrailingStepPips <= 0)
			return;

		var point = GetPoint();
		var trail = TrailingStopPips * point;
		var step = TrailingStepPips * point;

		if (Position > 0m && _bid is decimal longPrice)
		{
			if (longPrice - _entryPrice < trail + step)
				return;

			var candidate = longPrice - trail;
			var current = GetActivation(_protectiveStop);
			if (candidate >= current + step)
				ReplaceProtectiveStop(Sides.Sell, candidate);
		}
		else if (Position < 0m && _ask is decimal shortPrice)
		{
			if (_entryPrice - shortPrice < trail + step)
				return;

			var candidate = shortPrice + trail;
			var current = GetActivation(_protectiveStop);
			if (candidate <= current - step)
				ReplaceProtectiveStop(Sides.Buy, candidate);
		}
	}

	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order is null || trade.Trade is null)
			return;

		if (IsSameOrder(trade.Order, _buyPending) || IsSameOrder(trade.Order, _sellPending))
		{
			var filledSide = trade.Order.Side;
			_entryPrice = trade.Trade.Price;

			if (filledSide == Sides.Buy)
				CancelIfActive(_sellPending);
			else
				CancelIfActive(_buyPending);

			_buyPending = null;
			_sellPending = null;

			if (StopLossPips > 0 && Position != 0m)
			{
				var point = GetPoint();
				var stop = filledSide == Sides.Buy
					? _entryPrice - StopLossPips * point
					: _entryPrice + StopLossPips * point;

				ReplaceProtectiveStop(filledSide.Invert(), stop);
			}

			return;
		}

		if (IsSameOrder(trade.Order, _protectiveStop) && Position == 0m)
		{
			_protectiveStop = null;
			_entryPrice = 0m;
		}
	}

	private bool IsTradingHour(int hour)
		=> (Hour1 != 0 && hour == Hour1) ||
		   (Hour2 != 0 && hour == Hour2) ||
		   (Hour3 != 0 && hour == Hour3);

	private decimal CalculateVolume(decimal point)
	{
		if (MoneyMode == LbsMoneyMode.FixedLot)
			return NormalizeVolume(VolumeOrRisk);

		if (StopLossPips <= 0)
			return 0m;

		var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (balance <= 0m)
			return 0m;

		var riskMoney = balance * VolumeOrRisk / 100m;
		var stepPrice = Security?.StepPrice ?? 0m;
		var lossPerUnit = stepPrice > 0m
			? StopLossPips * stepPrice
			: StopLossPips * point * (Security?.Multiplier ?? 1m);

		return lossPerUnit > 0m ? NormalizeVolume(riskMoney / lossPerUnit) : 0m;
	}

	private Order CreateStopOrder(Sides side, decimal activationPrice, decimal volume, string comment)
		=> new()
		{
			Security = Security,
			Portfolio = Portfolio,
			Type = OrderTypes.Conditional,
			Condition = new StopOrderCondition { ActivationPrice = Security.ShrinkPrice(activationPrice) },
			Side = side,
			Volume = volume,
			Comment = comment,
		};

	private void ReplaceProtectiveStop(Sides side, decimal activationPrice)
	{
		var old = _protectiveStop;
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		var replacement = CreateStopOrder(side, activationPrice, volume, "LBS protective stop");

		if (old is { State: OrderStates.Active })
			ReRegisterOrder(old, replacement);
		else
			RegisterOrder(replacement);

		_protectiveStop = replacement;
	}

	private void CancelEntryStops()
	{
		CancelIfActive(_buyPending);
		CancelIfActive(_sellPending);
		_buyPending = null;
		_sellPending = null;
	}

	private void CancelIfActive(Order order)
	{
		if (order is { State: OrderStates.Active })
			CancelOrder(order);
	}

	private static bool IsSameOrder(Order left, Order right)
		=> left is not null && right is not null &&
		   (ReferenceEquals(left, right) || left.TransactionId == right.TransactionId);

	private static decimal GetActivation(Order order)
		=> order?.Condition is StopOrderCondition stop ? stop.ActivationPrice ?? 0m : 0m;

	private decimal NormalizeVolume(decimal volume)
	{
		if (Security?.MaxVolume is decimal max && max > 0m)
			volume = Math.Min(volume, max);
		if (Security?.MinVolume is decimal min && min > 0m)
			volume = Math.Max(volume, min);
		if (Security?.VolumeStep is decimal step && step > 0m)
			volume = Math.Floor(volume / step) * step;

		return volume;
	}

	private decimal GetPoint()
	{
		var point = Security?.PriceStep ?? 0m;
		return point > 0m ? point : 0.0001m;
	}

	internal static (decimal buy, decimal sell) CalculateBreakoutLevels(
		decimal candleHigh,
		decimal candleLow,
		decimal bid,
		decimal ask,
		decimal priceStep)
	{
		var spread = Math.Max(0m, ask - bid);
		var buffer = Math.Max(3m * spread, 10m * priceStep);
		return (Math.Max(candleHigh, ask + buffer), Math.Min(candleLow, bid - buffer));
	}
}
