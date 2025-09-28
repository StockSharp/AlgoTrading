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
/// Port of the MetaTrader expert "Franks 4 hour limit orders" to the StockSharp high-level API.
/// The strategy analyses MACD histogram direction together with the Force Index on four-hour candles.
/// When momentum conditions align it submits contrarian limit orders near the previous candle extremes.
/// Filled orders are managed through configurable stop-loss, take-profit, and trailing stop distances.
/// </summary>
public class Franks4HourLimitOrdersStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _entryBufferPips;
	private readonly StrategyParam<decimal> _pipSize;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private ForceIndex _forceIndex = null!;

	private Order _pendingBuyLimit;
	private Order _pendingSellLimit;

	private decimal? _prevOsma;
	private decimal? _prevPrevOsma;
	private decimal? _prevForce;
	private decimal? _prevHigh;
	private decimal? _lastHigh;
	private decimal? _prevLow;
	private decimal? _lastLow;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortTakeProfitPrice;
	private decimal? _plannedLongStop;
	private decimal? _plannedLongTake;
	private decimal? _plannedShortStop;
	private decimal? _plannedShortTake;

	private decimal _pipSizeValue;
	private decimal _pointSizeValue;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal _trailingStopDistance;
	private decimal _entryBufferDistance;

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public Franks4HourLimitOrdersStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetDisplay("Order Volume", "Fixed trade volume for limit orders", "Trading")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 35m)
		.SetDisplay("Stop Loss (pips)", "Stop loss distance expressed in pips", "Risk")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 150m)
		.SetDisplay("Take Profit (pips)", "Take profit distance expressed in pips", "Risk")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 30m)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance expressed in pips", "Risk")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_entryBufferPips = Param(nameof(EntryBufferPips), 16m)
		.SetDisplay("Entry Buffer (pips)", "Minimum distance from current price to the pending order", "Trading")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_pipSize = Param(nameof(PipSize), 0.0001m)
		.SetDisplay("Pip Size", "Price movement corresponding to one pip", "Trading")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for indicator calculations", "General");
	}

	/// <summary>
	/// Volume for new orders when the strategy opens a position.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum distance between the market price and the pending limit order.
	/// </summary>
	public decimal EntryBufferPips
	{
		get => _entryBufferPips.Value;
		set => _entryBufferPips.Value = value;
	}

	/// <summary>
	/// Pip size used for price conversions.
	/// </summary>
	public decimal PipSize
	{
		get => _pipSize.Value;
		set => _pipSize.Value = value;
	}

	/// <summary>
	/// Candle series for signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevOsma = null;
		_prevPrevOsma = null;
		_prevForce = null;
		_prevHigh = null;
		_lastHigh = null;
		_prevLow = null;
		_lastLow = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakeProfitPrice = null;
		_shortTakeProfitPrice = null;
		_plannedLongStop = null;
		_plannedLongTake = null;
		_plannedShortStop = null;
		_plannedShortTake = null;
		_pendingBuyLimit = null;
		_pendingSellLimit = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSizeValue = ResolvePipSize();
		_pointSizeValue = ResolvePointSize();
		_stopLossDistance = StopLossPips * _pipSizeValue;
		_takeProfitDistance = TakeProfitPips * _pipSizeValue;
		_trailingStopDistance = TrailingStopPips * _pipSizeValue;
		_entryBufferDistance = EntryBufferPips * _pipSizeValue;

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 },
			},
			SignalMa = { Length = 9 },
		};

		_forceIndex = new ForceIndex { Length = 24 };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_macd, _forceIndex, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawIndicator(area, _forceIndex);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue forceValue)
	{
		// Skip unfinished candles because the original EA evaluates signals on closed bars.
		if (candle.State != CandleStates.Finished)
		return;

		// Indicators must be fully formed to access previous values reliably.
		if (!_macd.IsFormed || !_forceIndex.IsFormed)
		goto UpdateHistory;

		if (!IsFormedAndOnlineAndAllowTrading())
		goto UpdateHistory;

		// Extract MACD line and signal to rebuild the OsMA histogram from managed indicators.
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signalLine)
		goto UpdateHistory;

		var osmaCurrent = macdLine - signalLine;
		var forceCurrent = forceValue.ToDecimal();

		// Use current best bid/ask as a fallback for pending order anchoring.
		var bid = Security?.BestBid?.Price ?? candle.ClosePrice;
		var ask = Security?.BestAsk?.Price ?? candle.ClosePrice;
		if (ask < bid)
		ask = bid;

		var hasHistory = _prevOsma.HasValue && _prevPrevOsma.HasValue &&
		_prevForce.HasValue && _lastHigh.HasValue && _lastLow.HasValue;

		// The MQL expert requires two historical bars to compute slopes and force direction.
		if (hasHistory)
		{
			var osmaDirection = Compare(_prevOsma!.Value, _prevPrevOsma!.Value);
			var forcePositive = _prevForce!.Value > 0m;
			var forceNegative = _prevForce.Value < 0m;

			ManageOpenPositions(candle);
			HandleStalePendingOrders(osmaDirection);

			if (Position == 0m)
			{
				if (_pendingSellLimit == null && osmaDirection == 1 && forceNegative)
				{
					TryPlaceSellLimit(_lastHigh!.Value, bid);
				}

				if (_pendingBuyLimit == null && osmaDirection == -1 && forcePositive)
				{
					TryPlaceBuyLimit(_lastLow!.Value, ask);
				}
			}
		}

		UpdateHistory:
		_prevPrevOsma = _prevOsma;
		_prevOsma = macdValue.IsFinal ? osmaCurrent : _prevOsma;
		_prevForce = forceValue.IsFinal ? forceCurrent : _prevForce;

		_prevHigh = _lastHigh;
		_prevLow = _lastLow;
		_lastHigh = candle.HighPrice;
		_lastLow = candle.LowPrice;
	}

	private void TryPlaceSellLimit(decimal previousHigh, decimal bid)
	{
		// Recreate the limit price around the previous high with the mandatory buffer.
		var referencePrice = AlignPrice(previousHigh + _pointSizeValue);
		var minDistancePrice = AlignPrice(bid + _entryBufferDistance);
		var targetPrice = Math.Max(referencePrice, minDistancePrice);

		if (targetPrice <= bid)
		return;

		var stopPrice = _stopLossDistance > 0m ? AlignPrice(targetPrice + _stopLossDistance) : (decimal?)null;
		var takePrice = _takeProfitDistance > 0m ? AlignPrice(targetPrice - _takeProfitDistance) : (decimal?)null;

		var volume = GetOrderVolume();
		if (volume <= 0m)
		return;

		CancelOrder(ref _pendingSellLimit);
		_pendingSellLimit = SellLimit(volume, targetPrice);
		_plannedShortStop = stopPrice;
		_plannedShortTake = takePrice;
	}

	private void TryPlaceBuyLimit(decimal previousLow, decimal ask)
	{
		// Mirror logic for the buy limit, anchoring it below the previous low.
		var referencePrice = AlignPrice(previousLow - _pointSizeValue);
		var minDistancePrice = AlignPrice(ask - _entryBufferDistance);
		var targetPrice = Math.Min(referencePrice, minDistancePrice);

		if (targetPrice >= ask)
		return;

		var stopPrice = _stopLossDistance > 0m ? AlignPrice(targetPrice - _stopLossDistance) : (decimal?)null;
		var takePrice = _takeProfitDistance > 0m ? AlignPrice(targetPrice + _takeProfitDistance) : (decimal?)null;

		var volume = GetOrderVolume();
		if (volume <= 0m)
		return;

		CancelOrder(ref _pendingBuyLimit);
		_pendingBuyLimit = BuyLimit(volume, targetPrice);
		_plannedLongStop = stopPrice;
		_plannedLongTake = takePrice;
	}

	private void HandleStalePendingOrders(int osmaDirection)
	{
		// Cancel outdated buy limits once the OsMA slope flips upwards.
		if (_pendingBuyLimit != null && osmaDirection == 1)
		{
			CancelOrder(ref _pendingBuyLimit);
			_plannedLongStop = null;
			_plannedLongTake = null;
		}

		// Cancel outdated sell limits when the histogram starts pointing down again.
		if (_pendingSellLimit != null && osmaDirection == -1)
		{
			CancelOrder(ref _pendingSellLimit);
			_plannedShortStop = null;
			_plannedShortTake = null;
		}
	}

	private void ManageOpenPositions(ICandleMessage candle)
	{
		// Manage open long positions including trailing logic and protective exits.
		if (Position > 0m)
		{
			UpdateLongTrailing(candle);

			if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				return;
			}

			if (_longTakeProfitPrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
			}
		}
		// Symmetric handling for active short positions.
		else if (Position < 0m)
		{
			UpdateShortTrailing(candle);

			if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			if (_shortTakeProfitPrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(Position));
			}
		}
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
		if (_longEntryPrice is not decimal entryPrice || _trailingStopDistance <= 0m)
		return;

		// Trail the stop only forward to mimic the EA trailing behaviour for longs.
		var desiredStop = AlignPrice(candle.ClosePrice - _trailingStopDistance);
		if (_longStopPrice is not decimal currentStop || desiredStop > currentStop)
		_longStopPrice = desiredStop;
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
		if (_shortEntryPrice is not decimal entryPrice || _trailingStopDistance <= 0m)
		return;

		// Trail the stop only downward to protect short entries.
		var desiredStop = AlignPrice(candle.ClosePrice + _trailingStopDistance);
		if (_shortStopPrice is not decimal currentStop || desiredStop < currentStop)
		_shortStopPrice = desiredStop;
	}

	private decimal ResolvePipSize()
	{
		if (PipSize > 0m)
		return PipSize;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return 0.0001m;

		var decimals = Security?.Decimals;
		if (decimals == 3 || decimals == 5)
		return step * 10m;

		return step;
	}

	private decimal ResolvePointSize()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : _pipSizeValue;
	}

	private decimal AlignPrice(decimal price)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return price;

		return Math.Round(price / step, MidpointRounding.AwayFromZero) * step;
	}

	private decimal AlignVolume(decimal volume)
	{
		var step = Security?.VolumeStep ?? 0m;
		if (step <= 0m)
		return volume;

		var steps = Math.Max(1m, Math.Floor(volume / step));
		return steps * step;
	}

	private decimal GetOrderVolume()
	{
		return AlignVolume(OrderVolume);
	}

	private static int Compare(decimal first, decimal second)
	{
		if (first > second)
		return 1;
		if (first < second)
		return -1;
		return 0;
	}

	private void CancelOrder(ref Order orderField)
	{
		var order = orderField;
		if (order == null)
		return;

		if (order.State is OrderStates.Active or OrderStates.Pending)
		CancelOrder(order);

		orderField = null;
	}

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		if (_pendingBuyLimit != null && order == _pendingBuyLimit && order.State is OrderStates.Done or OrderStates.Canceled or OrderStates.Failed)
		{
			if (order.State != OrderStates.Done)
			{
				_plannedLongStop = null;
				_plannedLongTake = null;
			}

			_pendingBuyLimit = null;
		}

		if (_pendingSellLimit != null && order == _pendingSellLimit && order.State is OrderStates.Done or OrderStates.Canceled or OrderStates.Failed)
		{
			if (order.State != OrderStates.Done)
			{
				_plannedShortStop = null;
				_plannedShortTake = null;
			}

			_pendingSellLimit = null;
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
		return;

		// Store execution prices and planned protective levels once a buy limit fills.
		if (_pendingBuyLimit != null && trade.Order == _pendingBuyLimit)
		{
			_pendingBuyLimit = null;
			_longEntryPrice = trade.Trade.Price;
			_longStopPrice = _plannedLongStop;
			_longTakeProfitPrice = _plannedLongTake;
			_plannedLongStop = null;
			_plannedLongTake = null;

			CancelOrder(ref _pendingSellLimit);
		}
		// Equivalent bookkeeping for filled sell limit orders.
		else if (_pendingSellLimit != null && trade.Order == _pendingSellLimit)
		{
			_pendingSellLimit = null;
			_shortEntryPrice = trade.Trade.Price;
			_shortStopPrice = _plannedShortStop;
			_shortTakeProfitPrice = _plannedShortTake;
			_plannedShortStop = null;
			_plannedShortTake = null;

			CancelOrder(ref _pendingBuyLimit);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
		{
			_longEntryPrice = null;
			_shortEntryPrice = null;
			_longStopPrice = null;
			_shortStopPrice = null;
			_longTakeProfitPrice = null;
			_shortTakeProfitPrice = null;
		}
		else if (Position > 0m)
		{
			_shortEntryPrice = null;
			_shortStopPrice = null;
			_shortTakeProfitPrice = null;
		}
		// Symmetric handling for active short positions.
		else if (Position < 0m)
		{
			_longEntryPrice = null;
			_longStopPrice = null;
			_longTakeProfitPrice = null;
		}
	}
}

