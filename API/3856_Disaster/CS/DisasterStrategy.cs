using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader "disaster" expert advisor.
/// Places stop orders around a long-period SMA and mirrors the adaptive take-profit sizing.
/// Automatically rebuilds pending entries as the average drifts and recreates protective orders after fills.
/// </summary>
public class DisasterStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _triggerDistancePips;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _sma = null!;

	private decimal _pipSize;
	private decimal _priceStep;

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _stopLossOrder;
	private Order _takeProfitOrder;

	private decimal? _lastBid;
	private decimal? _lastAsk;

	private bool _lastBuyWasLoss;
	private bool _lastSellWasLoss;

	private decimal _entryPrice;
	private Sides? _entrySide;

	/// <summary>
	/// Initializes a new instance of the <see cref="DisasterStrategy"/> class.
	/// </summary>
	public DisasterStrategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume for stop entries", "Trading");

		_maPeriod = Param(nameof(MaPeriod), 590)
		.SetRange(1, 5000)
		.SetDisplay("SMA Period", "Length of the baseline moving average", "Signal");

		_stopLossPips = Param(nameof(StopLossPips), 30m)
		.SetRange(0m, 1000m)
		.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 70m)
		.SetRange(0m, 1000m)
		.SetDisplay("Take Profit (pips)", "Base take-profit distance in pips", "Risk");

		_triggerDistancePips = Param(nameof(TriggerDistancePips), 20m)
		.SetRange(0m, 1000m)
		.SetDisplay("Trigger Distance (pips)", "Minimal gap between price and SMA to arm stops", "Signal");

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1)))
		.SetDisplay("Candle Type", "Primary candle series used for the SMA", "Data");
	}

	/// <summary>
	/// Trading volume used for new entry orders.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Moving average length applied to minute candles.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips before adaptive reductions.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Minimal displacement between the current price and the SMA.
	/// </summary>
	public decimal TriggerDistancePips
	{
		get => _triggerDistancePips.Value;
		set => _triggerDistancePips.Value = value;
	}

	/// <summary>
	/// Candle series used to feed the moving average.
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

		_sma = null!;
		_pipSize = 0m;
		_priceStep = 0m;

		_buyStopOrder = null;
		_sellStopOrder = null;
		_stopLossOrder = null;
		_takeProfitOrder = null;

		_lastBid = null;
		_lastAsk = null;

		_lastBuyWasLoss = false;
		_lastSellWasLoss = false;

		_entryPrice = 0m;
		_entrySide = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_sma = new SMA
		{
			Length = MaPeriod
		};

		_pipSize = CalculatePipSize();
		_priceStep = CalculatePriceStep();

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_sma, ProcessCandle)
		.Start();
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (ReferenceEquals(order, _buyStopOrder))
		{
			HandleEntryOrderUpdate(order, Sides.Buy);
		}
		else if (ReferenceEquals(order, _sellStopOrder))
		{
			HandleEntryOrderUpdate(order, Sides.Sell);
		}
		else if (ReferenceEquals(order, _stopLossOrder) || ReferenceEquals(order, _takeProfitOrder))
		{
			HandleProtectionOrderUpdate(order);
		}
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		CancelPendingOrders();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue) && bidValue is decimal bid)
		_lastBid = bid;

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue) && askValue is decimal ask)
		_lastAsk = ask;
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_sma.IsFormed)
		return;

		var bid = _lastBid ?? candle.ClosePrice;
		var ask = _lastAsk ?? candle.ClosePrice;

		UpdatePendingOrders(smaValue, bid, ask);

		if (Position != 0m)
		return;

		PlaceNewEntriesIfNeeded(smaValue, bid, ask);
	}

	private void UpdatePendingOrders(decimal smaValue, decimal bid, decimal ask)
	{
		if (_buyStopOrder != null)
		{
			UpdateEntryOrder(_buyStopOrder, smaValue, bid, ask, isBuy: true);
		}

		if (_sellStopOrder != null)
		{
			UpdateEntryOrder(_sellStopOrder, smaValue, bid, ask, isBuy: false);
		}
	}

	private void PlaceNewEntriesIfNeeded(decimal smaValue, decimal bid, decimal ask)
	{
		var volume = AlignVolume(Volume);
		if (volume <= 0m)
		return;

		var triggerDistance = TriggerDistancePips * _pipSize;
		if (triggerDistance <= 0m)
		return;

		var spread = Math.Abs(ask - bid);

		if (_sellStopOrder == null && bid - smaValue > triggerDistance)
		{
			var activationPrice = NormalizePrice(smaValue);
			if (activationPrice > 0m)
			{
				_sellStopOrder = SellStop(volume, activationPrice);
			}
		}

		if (_buyStopOrder == null && smaValue - ask > triggerDistance)
		{
			var activationPrice = NormalizePrice(smaValue + spread);
			if (activationPrice > 0m)
			{
				_buyStopOrder = BuyStop(volume, activationPrice);
			}
		}
	}

	private void UpdateEntryOrder(Order order, decimal smaValue, decimal bid, decimal ask, bool isBuy)
	{
		if (order.State == OrderStates.Done || order.State == OrderStates.Failed || order.State == OrderStates.Canceled)
		{
			if (isBuy)
			_buyStopOrder = null;
			else
			_sellStopOrder = null;
			return;
		}

		var spread = Math.Abs(ask - bid);
		var desiredPrice = isBuy ? NormalizePrice(smaValue + spread) : NormalizePrice(smaValue);

		if (desiredPrice <= 0m)
		return;

		if (order.Price != desiredPrice)
		ReRegisterOrder(order, desiredPrice, order.Volume ?? Volume);
	}

	private void HandleEntryOrderUpdate(Order order, Sides side)
	{
		if (order.State == OrderStates.Done)
		{
			var fillPrice = order.AveragePrice ?? order.Price ?? 0m;
			if (fillPrice <= 0m)
			return;

			if (side == Sides.Buy)
			_buyStopOrder = null;
			else
			_sellStopOrder = null;

			_entrySide = side;
			_entryPrice = fillPrice;

			CreateProtectionOrders(side, Math.Abs(order.Volume ?? Volume), fillPrice);
		}
		else if (order.State is OrderStates.Failed or OrderStates.Canceled)
		{
			if (side == Sides.Buy)
			_buyStopOrder = null;
			else
			_sellStopOrder = null;
		}
	}

	private void CreateProtectionOrders(Sides side, decimal volume, decimal entryPrice)
	{
		CancelProtectionOrders();

		var stopDistance = StopLossPips * _pipSize;
		var takeDistance = GetAdaptiveTakeProfitDistance(side) * _pipSize;

		if (stopDistance <= 0m && takeDistance <= 0m)
		return;

		var isLong = side == Sides.Buy;

		if (stopDistance > 0m)
		{
			var stopPrice = isLong ? entryPrice - stopDistance : entryPrice + stopDistance;
			stopPrice = NormalizePrice(stopPrice);
			if (stopPrice > 0m)
			_stopLossOrder = isLong ? SellStop(volume, stopPrice) : BuyStop(volume, stopPrice);
		}

		if (takeDistance > 0m)
		{
			var takePrice = isLong ? entryPrice + takeDistance : entryPrice - takeDistance;
			takePrice = NormalizePrice(takePrice);
			if (takePrice > 0m)
			_takeProfitOrder = isLong ? SellLimit(volume, takePrice) : BuyLimit(volume, takePrice);
		}
	}

	private void HandleProtectionOrderUpdate(Order order)
	{
		if (order.State != OrderStates.Done)
		{
			if (order.State is OrderStates.Failed or OrderStates.Canceled)
			{
				if (ReferenceEquals(order, _stopLossOrder))
				_stopLossOrder = null;
				else if (ReferenceEquals(order, _takeProfitOrder))
				_takeProfitOrder = null;
			}
			return;
		}

		if (ReferenceEquals(order, _stopLossOrder))
		{
			if (_entrySide == Sides.Buy)
			_lastBuyWasLoss = true;
			else if (_entrySide == Sides.Sell)
			_lastSellWasLoss = true;
		}
		else if (ReferenceEquals(order, _takeProfitOrder))
		{
			if (_entrySide == Sides.Buy)
			_lastBuyWasLoss = false;
			else if (_entrySide == Sides.Sell)
			_lastSellWasLoss = false;
		}

		_stopLossOrder = null;
		_takeProfitOrder = null;
		_entrySide = null;
		_entryPrice = 0m;
	}

	private decimal GetAdaptiveTakeProfitDistance(Sides side)
	{
		var baseDistance = TakeProfitPips;
		if (baseDistance <= 0m)
		return 0m;

		return side == Sides.Buy && _lastBuyWasLoss
		? baseDistance * 0.5m
		: side == Sides.Sell && _lastSellWasLoss
		? baseDistance * 0.5m
		: baseDistance;
	}

	private void CancelPendingOrders()
	{
		if (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active)
		CancelOrder(_buyStopOrder);
		_buyStopOrder = null;

		if (_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active)
		CancelOrder(_sellStopOrder);
		_sellStopOrder = null;

		CancelProtectionOrders();
	}

	private void CancelProtectionOrders()
	{
		if (_stopLossOrder != null && _stopLossOrder.State == OrderStates.Active)
		CancelOrder(_stopLossOrder);

		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
		CancelOrder(_takeProfitOrder);

		_stopLossOrder = null;
		_takeProfitOrder = null;
		_entrySide = null;
		_entryPrice = 0m;
	}

	private decimal AlignVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
		return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var multiplier = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = multiplier * step;
		}

		var minVolume = security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
		volume = minVolume;

		var maxVolume = security.MaxVolume ?? 0m;
		if (maxVolume > 0m && volume > maxVolume)
		volume = maxVolume;

		return volume;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
		return 0.0001m;

		var step = security.PriceStep ?? security.MinPriceStep ?? 0m;
		if (step <= 0m)
		{
			var decimals = security.Decimals;
			if (decimals != null && decimals.Value > 0)
			{
				step = (decimal)Math.Pow(10, -decimals.Value);
			}
		}

		if (step <= 0m)
		step = 0.0001m;

		var digits = security.Decimals;
		if (digits != null && (digits.Value == 3 || digits.Value == 5))
		step *= 10m;

		return step;
	}

	private decimal CalculatePriceStep()
	{
		var security = Security;
		if (security == null)
		return 0m;

		var step = security.PriceStep ?? security.MinPriceStep ?? 0m;
		if (step <= 0m)
		{
			var decimals = security.Decimals;
			if (decimals != null && decimals.Value > 0)
			step = (decimal)Math.Pow(10, -decimals.Value);
		}

		return step;
	}

	private decimal NormalizePrice(decimal price)
	{
		if (_priceStep <= 0m)
		return price;

		var steps = Math.Round(price / _priceStep, MidpointRounding.AwayFromZero);
		return steps * _priceStep;
	}
}
