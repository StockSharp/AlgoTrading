
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
/// Rapid Doji breakout strategy converted from the original MQL5 expert advisor.
/// Places stop entry orders around daily doji candles and manages risk with ATR based
/// stop-loss placement and a fixed trailing stop measured in raw points.
/// </summary>
public class RapidDojiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _trailingDistancePoints;
	private readonly StrategyParam<decimal> _dojiBodyThreshold;

	private AverageTrueRange _atr = null!;
	private Order _longEntryOrder;
	private Order _shortEntryOrder;
	private Order _protectiveStopOrder;

	private decimal? _plannedLongStop;
	private decimal? _plannedShortStop;

	public RapidDojiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Entry Candle", "Timeframe used for identifying doji candles.", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Lookback period for ATR stop calculation.", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.75m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier", "Multiplier applied to ATR for stop loss placement.", "Risk");

		_trailingDistancePoints = Param(nameof(TrailingDistancePoints), 2700m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Distance (points)", "Fixed trailing distance measured in raw points.", "Risk");
	}

	/// <summary>
	/// Candle type used for pattern detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// ATR lookback length.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to ATR when calculating stop losses.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in raw points.
	/// </summary>
	public decimal TrailingDistancePoints
	{
		get => _trailingDistancePoints.Value;
		set => _trailingDistancePoints.Value = value;
	}

	public decimal DojiBodyThreshold
	{
		get => _dojiBodyThreshold.Value;
		set => _dojiBodyThreshold.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longEntryOrder = null;
		_shortEntryOrder = null;
		_protectiveStopOrder = null;
		_plannedLongStop = null;
		_plannedShortStop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateTrailingStop(candle);

		if (!IsDoji(candle))
			return;

		if (atrValue <= 0m)
			return;

		PlaceEntryOrders(candle, atrValue);
	}

	private bool IsDoji(ICandleMessage candle)
	{
		var range = candle.HighPrice - candle.LowPrice;
		if (range <= 0m)
			return false;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		return body <= DojiBodyThreshold * range;
	}

	private void PlaceEntryOrders(ICandleMessage candle, decimal atrValue)
	{
		var volume = Volume;
		if (volume <= 0m)
			return;

		var atrDistance = atrValue * AtrMultiplier;
		var buyPrice = NormalizePrice(candle.HighPrice);
		var sellPrice = NormalizePrice(candle.LowPrice);
		var buyStopLoss = NormalizePrice(candle.LowPrice - atrDistance);
		var sellStopLoss = NormalizePrice(candle.HighPrice + atrDistance);

		CancelEntryOrders();

		_plannedLongStop = null;
		_plannedShortStop = null;

		if (buyPrice > 0m)
		{
			_longEntryOrder = BuyStop(volume, buyPrice);
			_plannedLongStop = buyStopLoss > 0m ? buyStopLoss : null;
		}

		if (sellPrice > 0m)
		{
			_shortEntryOrder = SellStop(volume, sellPrice);
			_plannedShortStop = sellStopLoss > 0m ? sellStopLoss : null;
		}
	}

	private void UpdateTrailingStop(ICandleMessage candle)
	{
		if (Position == 0m)
		{
			CancelOrderIfActive(ref _protectiveStopOrder);
			return;
		}

		var trailingDistance = CalculateTrailingDistance();
		if (trailingDistance <= 0m)
			return;

		var volume = Math.Abs(Position);
		var currentStopPrice = _protectiveStopOrder?.Price;
		var closePrice = candle.ClosePrice;

		if (Position > 0m)
		{
			if (closePrice <= PositionPrice)
				return;

			if (currentStopPrice.HasValue && closePrice - currentStopPrice.Value <= trailingDistance)
				return;

			var newStop = NormalizePrice(closePrice - trailingDistance);
			if (!currentStopPrice.HasValue || newStop > currentStopPrice.Value)
				MoveProtectiveStop(true, volume, newStop);
		}
		else
		{
			if (closePrice >= PositionPrice)
				return;

			if (currentStopPrice.HasValue && currentStopPrice.Value - closePrice <= trailingDistance)
				return;

			var newStop = NormalizePrice(closePrice + trailingDistance);
			if (!currentStopPrice.HasValue || newStop < currentStopPrice.Value)
				MoveProtectiveStop(false, volume, newStop);
		}
	}

	private void MoveProtectiveStop(bool isLong, decimal volume, decimal price)
	{
		if (volume <= 0m || price <= 0m)
			return;

		CancelOrderIfActive(ref _protectiveStopOrder);

		_protectiveStopOrder = isLong
			? SellStop(volume, price)
			: BuyStop(volume, price);
	}

	private void CancelEntryOrders()
	{
		CancelOrderIfActive(ref _longEntryOrder);
		CancelOrderIfActive(ref _shortEntryOrder);
	}

	private void CancelOrderIfActive(ref Order order)
	{
		if (order == null)
			return;

		if (order.State == OrderStates.Active || order.State == OrderStates.Pending)
			CancelOrder(order);

		order = null;
	}

	private decimal CalculateTrailingDistance()
	{
		var step = GetPriceStep();
		return step > 0m ? TrailingDistancePoints * step : 0m;
	}

	private decimal GetPriceStep()
	{
		if (Security?.PriceStep > 0m)
			return Security.PriceStep.Value;

		if (Security?.StepPrice > 0m)
			return Security.StepPrice.Value;

		return 0m;
	}

	private decimal NormalizePrice(decimal price)
	=> Security?.ShrinkPrice(price) ?? price;

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position > 0m)
		{
			CancelOrderIfActive(ref _shortEntryOrder);

			if (_plannedLongStop.HasValue)
				MoveProtectiveStop(true, Math.Abs(Position), NormalizePrice(_plannedLongStop.Value));
		}
		else if (Position < 0m)
		{
			CancelOrderIfActive(ref _longEntryOrder);

			if (_plannedShortStop.HasValue)
				MoveProtectiveStop(false, Math.Abs(Position), NormalizePrice(_plannedShortStop.Value));
		}
		else
		{
			CancelOrderIfActive(ref _protectiveStopOrder);
			_plannedLongStop = null;
			_plannedShortStop = null;
		}
	}

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		if (_longEntryOrder != null && order == _longEntryOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
			_longEntryOrder = null;

		if (_shortEntryOrder != null && order == _shortEntryOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
			_shortEntryOrder = null;

		if (_protectiveStopOrder != null && order == _protectiveStopOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
			_protectiveStopOrder = null;
	}
}

