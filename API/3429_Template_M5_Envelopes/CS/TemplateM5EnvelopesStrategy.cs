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
/// Envelope breakout strategy converted from the MetaTrader 4 expert "Template M5 Envelopes".
/// Places stop entries when price stretches beyond the linear weighted moving average envelope.
/// </summary>
public class TemplateM5EnvelopesStrategy : Strategy
{
	private readonly StrategyParam<decimal> _maxSpreadPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _entryOffsetPoints;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<int> _envelopePeriod;
	private readonly StrategyParam<decimal> _envelopeDeviation;
	private readonly StrategyParam<decimal> _distancePoints;
	private readonly StrategyParam<decimal> _slippagePoints;
	private readonly StrategyParam<DataType> _candleType;

	private readonly LinearWeightedMovingAverage _envelopeMa = new();

	private decimal _point;
	private decimal _bestBid;
	private decimal _bestAsk;

	private decimal? _previousUpper;
	private decimal? _previousLower;
	private decimal? _previousHigh;
	private decimal? _previousLow;

	private decimal? _currentUpper;
	private decimal? _currentLower;
	private decimal? _currentHigh;
	private decimal? _currentLow;

	private Order _entryOrder;
	private Order _stopOrder;
	private Order _takeProfitOrder;

	private decimal _lastPosition;
	private decimal _entryPrice;

	private decimal? _pendingReprice;
	private decimal? _pendingStopPrice;
	private decimal? _pendingTakePrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="TemplateM5EnvelopesStrategy"/> class.
	/// </summary>
	public TemplateM5EnvelopesStrategy()
	{
		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 10m)
		.SetDisplay("Max Spread", "Maximum allowed spread in points", "Risk")
		.SetNotNegative();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
		.SetDisplay("Take Profit", "Take-profit distance in points", "Risk")
		.SetNotNegative();

		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
		.SetDisplay("Stop Loss", "Stop-loss distance in points", "Risk")
		.SetNotNegative();

		_entryOffsetPoints = Param(nameof(EntryOffsetPoints), 30m)
		.SetDisplay("Entry Offset", "Offset from bid/ask used for stop entries (points)", "Entries")
		.SetGreaterThanZero();

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
		.SetDisplay("Enable Trailing", "Enable trailing stop updates", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 30m)
		.SetDisplay("Trailing Distance", "Trailing stop distance in points", "Risk")
		.SetNotNegative();

		_fixedVolume = Param(nameof(FixedVolume), 0.01m)
		.SetDisplay("Volume", "Fixed trading volume", "Money Management")
		.SetGreaterThanZero();

		_envelopePeriod = Param(nameof(EnvelopePeriod), 3)
		.SetDisplay("Envelope Period", "Length of the LWMA basis", "Indicator")
		.SetGreaterThanZero();

		_envelopeDeviation = Param(nameof(EnvelopeDeviation), 0.07m)
		.SetDisplay("Envelope Deviation", "Envelope deviation percentage", "Indicator")
		.SetNotNegative();

		_distancePoints = Param(nameof(DistancePoints), 140m)
		.SetDisplay("Distance Filter", "Minimal distance between price and envelope (points)", "Entries")
		.SetNotNegative();

		_slippagePoints = Param(nameof(SlippagePoints), 15m)
		.SetDisplay("Slippage", "Additional distance tolerated before repricing (points)", "Entries")
		.SetNotNegative();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for envelope calculations", "Indicator");
	}

	/// <summary>
	/// Maximum allowed spread in points.
	/// </summary>
	public decimal MaxSpreadPoints
	{
		get => _maxSpreadPoints.Value;
		set => _maxSpreadPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Offset from the current bid/ask used when placing stop entries.
	/// </summary>
	public decimal EntryOffsetPoints
	{
		get => _entryOffsetPoints.Value;
		set => _entryOffsetPoints.Value = value;
	}

	/// <summary>
	/// Enables or disables trailing stop adjustments.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Fixed trading volume used for entries.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Length of the linear weighted moving average used as envelope basis.
	/// </summary>
	public int EnvelopePeriod
	{
		get => _envelopePeriod.Value;
		set => _envelopePeriod.Value = value;
	}

	/// <summary>
	/// Envelope deviation expressed in percent.
	/// </summary>
	public decimal EnvelopeDeviation
	{
		get => _envelopeDeviation.Value;
		set => _envelopeDeviation.Value = value;
	}

	/// <summary>
	/// Minimal distance between price and envelope bands required to arm a signal.
	/// </summary>
	public decimal DistancePoints
	{
		get => _distancePoints.Value;
		set => _distancePoints.Value = value;
	}

	/// <summary>
	/// Additional distance tolerated before repricing pending entries.
	/// </summary>
	public decimal SlippagePoints
	{
		get => _slippagePoints.Value;
		set => _slippagePoints.Value = value;
	}

	/// <summary>
	/// Candle type used to calculate the envelope bands.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_point = 0m;
		_bestBid = 0m;
		_bestAsk = 0m;
		_previousUpper = null;
		_previousLower = null;
		_previousHigh = null;
		_previousLow = null;
		_currentUpper = null;
		_currentLower = null;
		_currentHigh = null;
		_currentLow = null;
		_entryOrder = null;
		_stopOrder = null;
		_takeProfitOrder = null;
		_lastPosition = 0m;
		_entryPrice = 0m;
		_pendingReprice = null;
		_pendingStopPrice = null;
		_pendingTakePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = FixedVolume;

		_point = CalculatePointValue();
		_envelopeMa.Length = EnvelopePeriod;
		_envelopeMa.CandlePrice = CandlePrice.Median;

		SubscribeCandles(CandleType)
		.Bind(_envelopeMa, ProcessCandle)
		.Start();

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();
	}

	private decimal CalculatePointValue()
	{
		var security = Security;
		if (security == null)
		return 0.0001m;

		var step = security.PriceStep ?? 0m;
		if (step > 0m)
		return step;

		var decimals = security.Decimals;
		if (decimals != null && decimals.Value > 0)
		return (decimal)Math.Pow(10, -decimals.Value);

		return 0.0001m;
	}

	private void ProcessCandle(ICandleMessage candle, decimal basis)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var deviationFactor = EnvelopeDeviation / 100m;
		var upper = basis * (1m + deviationFactor);
		var lower = basis * (1m - deviationFactor);

		_previousUpper = _currentUpper;
		_previousLower = _currentLower;
		_previousHigh = _currentHigh;
		_previousLow = _currentLow;

		_currentUpper = upper;
		_currentLower = lower;
		_currentHigh = candle.HighPrice;
		_currentLow = candle.LowPrice;
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
		_bestBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
		_bestAsk = (decimal)ask;

		if (_bestBid <= 0m || _bestAsk <= 0m)
		return;

		CleanupOrders();

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!HasEnvelopeData())
		return;

		UpdateEntryOrder();
		UpdateTrailingStop();
	}

	private void CleanupOrders()
	{
		if (_entryOrder != null && _entryOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		_entryOrder = null;

		if (_stopOrder != null && _stopOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		_stopOrder = null;

		if (_takeProfitOrder != null && _takeProfitOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		_takeProfitOrder = null;
	}

	private bool HasEnvelopeData()
	{
		return _previousUpper.HasValue && _previousLower.HasValue && _previousHigh.HasValue && _previousLow.HasValue;
	}

	private void UpdateEntryOrder()
	{
		if (_point <= 0m)
		return;

		var spread = _bestAsk - _bestBid;
		var maxSpread = MaxSpreadPoints <= 0m ? decimal.MaxValue : MaxSpreadPoints * _point;

		if (spread > maxSpread)
		{
			CancelEntryOrder();
			return;
		}

		var entryOffset = EntryOffsetPoints * _point;
		HandleEntryReprice(entryOffset);

		if (Position != 0m || _entryOrder != null)
		return;

		var distance = DistancePoints * _point;

		var prevUpper = _previousUpper!.Value;
		var prevLower = _previousLower!.Value;
		var prevHigh = _previousHigh!.Value;
		var prevLow = _previousLow!.Value;

		var buyCondition = prevLower - prevLow > distance && prevLower - _bestBid > distance;
		var sellCondition = prevHigh - prevUpper > distance && _bestBid - prevUpper > distance;

		if (!buyCondition && !sellCondition)
		return;

		var volume = Volume;

		if (buyCondition)
		{
			var price = NormalizePrice(_bestAsk + entryOffset);
			var stopPrice = StopLossPoints > 0m ? NormalizePrice(price - StopLossPoints * _point) : (decimal?)null;
			var takePrice = TakeProfitPoints > 0m ? NormalizePrice(price + TakeProfitPoints * _point) : (decimal?)null;

			_entryOrder = BuyStop(volume, price, stopLoss: stopPrice, takeProfit: takePrice);
			_entryPrice = price;
		}
		else if (sellCondition)
		{
			var price = NormalizePrice(_bestBid - entryOffset);
			var stopPrice = StopLossPoints > 0m ? NormalizePrice(price + StopLossPoints * _point) : (decimal?)null;
			var takePrice = TakeProfitPoints > 0m ? NormalizePrice(price - TakeProfitPoints * _point) : (decimal?)null;

			_entryOrder = SellStop(volume, price, stopLoss: stopPrice, takeProfit: takePrice);
			_entryPrice = price;
		}
	}

	private void HandleEntryReprice(decimal entryOffset)
	{
		if (_entryOrder == null || _entryOrder.State != OrderStates.Active)
		return;

		var currentPrice = _entryOrder.Price ?? 0m;
		if (currentPrice <= 0m)
		return;

		var tolerance = (EntryOffsetPoints + SlippagePoints) * _point;

		if (_entryOrder.Side == Sides.Buy)
		{
			var desiredPrice = NormalizePrice(_bestAsk + entryOffset);
			if (desiredPrice > 0m && currentPrice - _bestAsk > tolerance && desiredPrice != currentPrice)
			{
				var stopPrice = StopLossPoints > 0m ? NormalizePrice(desiredPrice - StopLossPoints * _point) : (decimal?)null;
				var takePrice = TakeProfitPoints > 0m ? NormalizePrice(desiredPrice + TakeProfitPoints * _point) : (decimal?)null;

				RequestReprice(desiredPrice, stopPrice, takePrice);
			}
		}
		else if (_entryOrder.Side == Sides.Sell)
		{
			var desiredPrice = NormalizePrice(_bestBid - entryOffset);
			if (desiredPrice > 0m && _bestBid - currentPrice > tolerance && desiredPrice != currentPrice)
			{
				var stopPrice = StopLossPoints > 0m ? NormalizePrice(desiredPrice + StopLossPoints * _point) : (decimal?)null;
				var takePrice = TakeProfitPoints > 0m ? NormalizePrice(desiredPrice - TakeProfitPoints * _point) : (decimal?)null;

				RequestReprice(desiredPrice, stopPrice, takePrice);
			}
		}
	}

	private void UpdateTrailingStop()
	{
		if (!UseTrailingStop || TrailingStopPoints <= 0m || Position == 0m)
		return;

		var trailingDistance = TrailingStopPoints * _point;
		if (trailingDistance <= 0m)
		return;

		if (Position > 0m)
		{
			var desiredStop = NormalizePrice(_bestBid - trailingDistance);
			if (desiredStop > 0m && (_stopOrder == null || desiredStop > (_stopOrder.Price ?? 0m)))
			RecreateStopOrder(true, desiredStop);
		}
		else if (Position < 0m)
		{
			var desiredStop = NormalizePrice(_bestAsk + trailingDistance);
			if (desiredStop > 0m && (_stopOrder == null || desiredStop < (_stopOrder.Price ?? 0m)))
			RecreateStopOrder(false, desiredStop);
		}
	}

	private void CancelEntryOrder()
	{
		if (_entryOrder != null && _entryOrder.State == OrderStates.Active)
		CancelOrder(_entryOrder);
	}

	private void RecreateStopOrder(bool isLong, decimal stopPrice)
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
		{
			ReRegisterOrder(_stopOrder, stopPrice, Math.Abs(Position));
			return;
		}

		var volume = Math.Abs(Position);
		_stopOrder = isLong ? SellStop(volume, stopPrice) : BuyStop(volume, stopPrice);
	}

	private decimal NormalizePrice(decimal price)
	{
		return Security?.ShrinkPrice(price) ?? price;
	}

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		if (order == _entryOrder)
		{
			if (order.State is OrderStates.Canceled && _pendingReprice.HasValue)
			{
				PlaceRepricedEntry(order.Side, order.Volume ?? Volume);
			}
			else if (order.State is OrderStates.Done or OrderStates.Canceled or OrderStates.Failed)
			{
				_entryOrder = null;
				_pendingReprice = null;
				_pendingStopPrice = null;
				_pendingTakePrice = null;
			}
		}
		else if (order == _stopOrder && order.State is OrderStates.Done or OrderStates.Canceled or OrderStates.Failed)
		{
			_stopOrder = null;
		}
		else if (order == _takeProfitOrder && order.State is OrderStates.Done or OrderStates.Canceled or OrderStates.Failed)
		{
			_takeProfitOrder = null;
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var currentPosition = Position;

		if (_lastPosition == 0m && currentPosition != 0m)
		{
			_entryPrice = trade.Trade.Price;
			SetupProtectionOrders(currentPosition > 0m);
		}
		else if (_lastPosition != 0m && currentPosition == 0m)
		{
			CancelProtectionOrders();
		}

		_lastPosition = currentPosition;
	}

	private void SetupProtectionOrders(bool isLong)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return;

		var stopDistance = StopLossPoints * _point;
		var takeDistance = TakeProfitPoints * _point;

		if (stopDistance > 0m)
		{
			var stopPrice = NormalizePrice(isLong ? _entryPrice - stopDistance : _entryPrice + stopDistance);
			_stopOrder = isLong ? SellStop(volume, stopPrice) : BuyStop(volume, stopPrice);
		}

		if (takeDistance > 0m)
		{
			var takePrice = NormalizePrice(isLong ? _entryPrice + takeDistance : _entryPrice - takeDistance);
			_takeProfitOrder = isLong ? SellLimit(volume, takePrice) : BuyLimit(volume, takePrice);
		}
	}

	private void CancelProtectionOrders()
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
		CancelOrder(_stopOrder);

		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
		CancelOrder(_takeProfitOrder);
	}

	private void PlaceRepricedEntry(Sides side, decimal volume)
	{
		if (!_pendingReprice.HasValue)
		return;

		var price = _pendingReprice.Value;
		var stopPrice = _pendingStopPrice;
		var takePrice = _pendingTakePrice;

		_entryOrder = side == Sides.Buy
		? BuyStop(volume, price, stopLoss: stopPrice, takeProfit: takePrice)
		: SellStop(volume, price, stopLoss: stopPrice, takeProfit: takePrice);

		_pendingReprice = null;
		_pendingStopPrice = null;
		_pendingTakePrice = null;
		_entryPrice = price;
	}

	private void RequestReprice(decimal price, decimal? stopPrice, decimal? takePrice)
	{
		if (_entryOrder == null || _entryOrder.State != OrderStates.Active)
		return;

		if (_pendingReprice.HasValue)
		return;

		_pendingReprice = price;
		_pendingStopPrice = stopPrice;
		_pendingTakePrice = takePrice;

		CancelOrder(_entryOrder);
	}
}

