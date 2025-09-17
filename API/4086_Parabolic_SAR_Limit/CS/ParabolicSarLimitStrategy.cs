using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that places pending limit orders at the Parabolic SAR level and keeps them aligned each new candle.
/// </summary>
public class ParabolicSarLimitStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaximum;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _minOrderDistancePoints;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private Order? _buyLimitOrder;
	private Order? _sellLimitOrder;

	private decimal? _pendingLongStop;
	private decimal? _pendingLongTake;
	private decimal? _pendingShortStop;
	private decimal? _pendingShortTake;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _activeLongStop;
	private decimal? _activeLongTake;
	private decimal? _activeShortStop;
	private decimal? _activeShortTake;
	/// <summary>
	/// Acceleration factor for the Parabolic SAR indicator.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor for the Parabolic SAR indicator.
	/// </summary>
	public decimal SarMaximum
	{
		get => _sarMaximum.Value;
		set => _sarMaximum.Value = value;
	}

	/// <summary>
	/// Stop loss distance in points relative to the order price.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in points relative to the order price.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Minimal distance in points between current market price and the pending order.
	/// </summary>
	public decimal MinOrderDistancePoints
	{
		get => _minOrderDistancePoints.Value;
		set => _minOrderDistancePoints.Value = value;
	}

	/// <summary>
	/// Volume used when submitting pending orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for generating trading signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters with defaults based on the original MQL script.
	/// </summary>
	public ParabolicSarLimitStrategy()
	{
		_sarStep = Param(nameof(SarStep), 0.009m)
			.SetDisplay("SAR Step", "Acceleration factor for Parabolic SAR", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.001m, 0.03m, 0.001m);

		_sarMaximum = Param(nameof(SarMaximum), 0.2m)
			.SetDisplay("SAR Maximum", "Maximum acceleration factor for Parabolic SAR", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.5m, 0.05m);

		_stopLossPoints = Param(nameof(StopLossPoints), 500m)
			.SetDisplay("Stop Loss (points)", "Stop loss distance in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100m, 1500m, 100m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 500m)
			.SetDisplay("Take Profit (points)", "Take profit distance in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100m, 1500m, 100m);

		_minOrderDistancePoints = Param(nameof(MinOrderDistancePoints), 0m)
			.SetDisplay("Min Order Distance (points)", "Minimal distance between price and pending order", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0m, 200m, 10m);

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetDisplay("Order Volume", "Volume for pending limit orders", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 2m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for Parabolic SAR analysis", "General");
	}
	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_buyLimitOrder = null;
		_sellLimitOrder = null;

		_pendingLongStop = null;
		_pendingLongTake = null;
		_pendingShortStop = null;
		_pendingShortTake = null;

		ResetLongProtection();
		ResetShortProtection();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var parabolicSar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationMax = SarMaximum
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(parabolicSar, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, parabolicSar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Enforce virtual stop-loss and take-profit boundaries before processing new signals.
		ApplyProtection(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var minDistance = PointsToPrice(MinOrderDistancePoints);
		var stopLossOffset = PointsToPrice(StopLossPoints);
		var takeProfitOffset = PointsToPrice(TakeProfitPoints);

		var bestBid = GetBestBidPrice(candle);
		var bestAsk = GetBestAskPrice(candle);
		var alignedSar = AlignPrice(sarValue);

		if (alignedSar <= 0m)
			return;

		// Place or update the buy limit order when SAR sits below the candle low and price is far enough.
		if (alignedSar < candle.LowPrice && bestBid > alignedSar + minDistance)
		{
			EnsureBuyLimit(alignedSar, stopLossOffset, takeProfitOffset);
		}

		// Place or update the sell limit order when SAR rises above the candle high and ask is sufficiently lower.
		if (alignedSar > candle.HighPrice && bestAsk < alignedSar - minDistance)
		{
			EnsureSellLimit(alignedSar, stopLossOffset, takeProfitOffset);
		}
	}

	private void EnsureBuyLimit(decimal price, decimal stopLossOffset, decimal takeProfitOffset)
	{
		var volume = AlignVolume(OrderVolume);
		if (volume <= 0m)
			return;

		// Replace the pending order only when the SAR level changed enough.
		if (_buyLimitOrder != null && _buyLimitOrder.State == OrderStates.Active)
		{
			if (ArePricesEqual(_buyLimitOrder.Price, price))
			{
				_pendingLongStop = stopLossOffset > 0m ? AlignPrice(price - stopLossOffset) : null;
				_pendingLongTake = takeProfitOffset > 0m ? AlignPrice(price + takeProfitOffset) : null;
				return;
			}

			CancelOrder(_buyLimitOrder);
			_buyLimitOrder = null;
		}

		_pendingLongStop = stopLossOffset > 0m ? AlignPrice(price - stopLossOffset) : null;
		_pendingLongTake = takeProfitOffset > 0m ? AlignPrice(price + takeProfitOffset) : null;
		_buyLimitOrder = BuyLimit(volume, price);
	}

	private void EnsureSellLimit(decimal price, decimal stopLossOffset, decimal takeProfitOffset)
	{
		var volume = AlignVolume(OrderVolume);
		if (volume <= 0m)
			return;

		// Replace the pending order only when the SAR level changed enough.
		if (_sellLimitOrder != null && _sellLimitOrder.State == OrderStates.Active)
		{
			if (ArePricesEqual(_sellLimitOrder.Price, price))
			{
				_pendingShortStop = stopLossOffset > 0m ? AlignPrice(price + stopLossOffset) : null;
				_pendingShortTake = takeProfitOffset > 0m ? AlignPrice(price - takeProfitOffset) : null;
				return;
			}

			CancelOrder(_sellLimitOrder);
			_sellLimitOrder = null;
		}

		_pendingShortStop = stopLossOffset > 0m ? AlignPrice(price + stopLossOffset) : null;
		_pendingShortTake = takeProfitOffset > 0m ? AlignPrice(price - takeProfitOffset) : null;
		_sellLimitOrder = SellLimit(volume, price);
	}

	private void ApplyProtection(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_activeLongStop is decimal stop && candle.LowPrice <= stop)
			{
				ClosePosition();
				ResetLongProtection();
				return;
			}

			if (_activeLongTake is decimal take && candle.HighPrice >= take)
			{
				ClosePosition();
				ResetLongProtection();
				return;
			}
		}
		else if (Position < 0)
		{
			if (_activeShortStop is decimal stop && candle.HighPrice >= stop)
			{
				ClosePosition();
				ResetShortProtection();
				return;
			}

			if (_activeShortTake is decimal take && candle.LowPrice <= take)
			{
				ClosePosition();
				ResetShortProtection();
				return;
			}
		}
		else
		{
			ResetLongProtection();
			ResetShortProtection();
		}
	}
	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (_buyLimitOrder != null && order == _buyLimitOrder)
		{
			// Store the fill price and activate protection when the buy limit is executed.
			if (order.State is OrderStates.Done)
			{
				_longEntryPrice = order.AveragePrice ?? order.Price;
				_activeLongStop = _pendingLongStop;
				_activeLongTake = _pendingLongTake;
				_pendingLongStop = null;
				_pendingLongTake = null;
				_buyLimitOrder = null;
			}
			else if (order.State is OrderStates.Failed or OrderStates.Canceled)
			{
				_pendingLongStop = null;
				_pendingLongTake = null;
				_buyLimitOrder = null;
			}
		}

		if (_sellLimitOrder != null && order == _sellLimitOrder)
		{
			// Store the fill price and activate protection when the sell limit is executed.
			if (order.State is OrderStates.Done)
			{
				_shortEntryPrice = order.AveragePrice ?? order.Price;
				_activeShortStop = _pendingShortStop;
				_activeShortTake = _pendingShortTake;
				_pendingShortStop = null;
				_pendingShortTake = null;
				_sellLimitOrder = null;
			}
			else if (order.State is OrderStates.Failed or OrderStates.Canceled)
			{
				_pendingShortStop = null;
				_pendingShortTake = null;
				_sellLimitOrder = null;
			}
		}
	}

	private decimal PointsToPrice(decimal points)
	{
		if (points <= 0m)
			return 0m;

		return points * GetPriceStep();
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep;
		return step is null or <= 0m ? 1m : step.Value;
	}

	private decimal AlignPrice(decimal price)
	{
		var step = GetPriceStep();
		return Math.Round(price / step, MidpointRounding.AwayFromZero) * step;
	}

	private decimal AlignVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var step = Security?.VolumeStep;
		if (step is null or <= 0m)
			return volume;

		return Math.Round(volume / step.Value, MidpointRounding.AwayFromZero) * step.Value;
	}

	private static bool ArePricesEqual(decimal left, decimal right)
	{
		return Math.Abs(left - right) < 1e-6m;
	}

	private decimal GetBestBidPrice(ICandleMessage candle)
	{
		var bid = Security?.BestBid?.Price ?? Security?.BestBidPrice;
		return bid ?? candle.ClosePrice;
	}

	private decimal GetBestAskPrice(ICandleMessage candle)
	{
		var ask = Security?.BestAsk?.Price ?? Security?.BestAskPrice;
		return ask ?? candle.ClosePrice;
	}

	private void ResetLongProtection()
	{
		_longEntryPrice = null;
		_activeLongStop = null;
		_activeLongTake = null;
	}

	private void ResetShortProtection()
	{
		_shortEntryPrice = null;
		_activeShortStop = null;
		_activeShortTake = null;
	}
}
