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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mirrors the MetaTrader expert that continually repositions the protective stop-loss.
/// </summary>
public class DynamicStopLossStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<DataType> _candleType;

	private Order _stopOrder;

	/// <summary>
	/// Initializes a new instance of <see cref="DynamicStopLossStrategy"/>.
	/// </summary>
	public DynamicStopLossStrategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 800m)
			.SetRange(0m, 5000m)
			.SetDisplay("Stop Loss Points", "Distance between the market price and the protective stop expressed in instrument points.", "Protection")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used to detect bar completions.", "General");
	}

	/// <summary>
	/// Distance between price and stop measured in MetaTrader points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Candle type that triggers stop recalculation on close.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_stopOrder = null;
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
		// Process only completed candles to mirror the bar-based MetaTrader logic.
		if (candle.State != CandleStates.Finished)
			return;

		if (StopLossPoints <= 0m)
		{
			CancelStop();
			return;
		}

		var volume = Math.Abs(Position);

		// No open position means no stop needs to be tracked.
		if (volume <= 0m)
		{
			CancelStop();
			return;
		}

		var point = GetPointSize();

		if (point <= 0m)
			point = 1m;

		var distance = StopLossPoints * point;

		if (distance <= 0m)
			return;

		var closePrice = candle.ClosePrice;

		if (Position > 0m)
		{
			var newStop = Math.Max(0m, closePrice - distance);
			UpdateStop(Sides.Sell, volume, newStop);
		}
		else if (Position < 0m)
		{
			var newStop = Math.Max(0m, closePrice + distance);
			UpdateStop(Sides.Buy, volume, newStop);
		}
	}

	private void UpdateStop(Sides side, decimal volume, decimal price)
	{
		var normalized = NormalizePrice(price);

		if (_stopOrder != null)
		{
			// Skip re-registration when the existing stop already matches the desired settings.
			if (_stopOrder.State == OrderStates.Active && _stopOrder.Direction == side && _stopOrder.Price == normalized && _stopOrder.Volume == volume)
				return;

			if (_stopOrder.State == OrderStates.Active)
				CancelOrder(_stopOrder);

			_stopOrder = null;
		}

		_stopOrder = side == Sides.Sell
			? SellStop(volume, normalized)
			: BuyStop(volume, normalized);
	}

	private void CancelStop()
	{
		if (_stopOrder != null)
		{
			if (_stopOrder.State == OrderStates.Active)
				CancelOrder(_stopOrder);

			_stopOrder = null;
		}
	}

	private decimal GetPointSize()
	{
		var security = Security;

		if (security?.PriceStep is decimal priceStep && priceStep > 0m)
			return priceStep;

		if (security?.Step is decimal step && step > 0m)
			return step;

		return 1m;
	}

	private decimal NormalizePrice(decimal price)
	{
		var security = Security;
		return security?.ShrinkPrice(price) ?? price;
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		// Remove stale stop orders once the position is flat.
		if (Position == 0m)
			CancelStop();
	}
}

