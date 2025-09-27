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

public enum ManualTradingOrderType
{
	MarketExecution,
	PendingLimit,
	PendingStop
}

public enum ManualPriceMode
{
	Market,
	Manual
}

/// <summary>
/// Manual trading strategy that mirrors the Manual Trading Lightweight Utility expert advisor.
/// </summary>
public class ManualTradingLightweightUtilityStrategy : Strategy
{
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<bool> _lotControl;
	private readonly StrategyParam<decimal> _lotVolumeStep;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _limitOrderPoints;
	private readonly StrategyParam<decimal> _stopOrderPoints;
	private readonly StrategyParam<decimal> _priceStepPoints;
	private readonly StrategyParam<string> _orderComment;

	private readonly StrategyParam<decimal> _sellVolume;
	private readonly StrategyParam<decimal> _buyVolume;
	private readonly StrategyParam<ManualTradingOrderType> _sellOrderType;
	private readonly StrategyParam<ManualTradingOrderType> _buyOrderType;
	private readonly StrategyParam<ManualPriceMode> _sellPriceMode;
	private readonly StrategyParam<ManualPriceMode> _buyPriceMode;
	private readonly StrategyParam<decimal> _sellManualPrice;
	private readonly StrategyParam<decimal> _buyManualPrice;
	private readonly StrategyParam<bool> _sendSellOrder;
	private readonly StrategyParam<bool> _sendBuyOrder;

	private decimal? _lastBid;
	private decimal? _lastAsk;
	private decimal? _lastTrade;
	private bool _protectionTriggered;

	/// <summary>
	/// Initializes a new instance of the <see cref="ManualTradingLightweightUtilityStrategy"/> class.
	/// </summary>
	public ManualTradingLightweightUtilityStrategy()
	{
		_lotSize = Param(nameof(LotSize), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Size", "Base order volume used when Lot Control is disabled.", "Volume");

		_lotControl = Param(nameof(LotControl), false)
			.SetDisplay("Lot Control", "Enable independent buy/sell volumes.", "Volume")
			.SetCanOptimize(false);

		_lotVolumeStep = Param(nameof(LotVolumeStep), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Volume Step", "Minimum change applied when adjusting volumes without exchange metadata.", "Volume");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 400m)
			.SetRange(0m, 100000m)
			.SetDisplay("Take Profit Points", "Distance in points used for take-profit management.", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 200m)
			.SetRange(0m, 100000m)
			.SetDisplay("Stop Loss Points", "Distance in points used for stop-loss management.", "Risk");

		_limitOrderPoints = Param(nameof(LimitOrderPoints), 50m)
			.SetRange(0m, 100000m)
			.SetDisplay("Limit Order Points", "Offset in points applied when calculating pending limit prices.", "Order Prices");

		_stopOrderPoints = Param(nameof(StopOrderPoints), 50m)
			.SetRange(0m, 100000m)
			.SetDisplay("Stop Order Points", "Offset in points applied when calculating pending stop prices.", "Order Prices");

		_priceStepPoints = Param(nameof(PriceStepPoints), 10m)
			.SetRange(0m, 100000m)
			.SetDisplay("Price Step Points", "Fallback step in points when manual prices need adjustment.", "Order Prices");

		_orderComment = Param(nameof(OrderComment), "Manual Trading")
			.SetDisplay("Order Comment", "Text comment attached to generated orders.", "Misc");

		_sellVolume = Param(nameof(SellVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Sell Volume", "Volume used for sell orders when Lot Control is enabled.", "Sell")
			.SetCanOptimize(false);

		_buyVolume = Param(nameof(BuyVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Buy Volume", "Volume used for buy orders when Lot Control is enabled.", "Buy")
			.SetCanOptimize(false);

		_sellOrderType = Param(nameof(SellOrderType), ManualTradingOrderType.MarketExecution)
			.SetDisplay("Sell Order Type", "Order type used when submitting sell orders.", "Sell");

		_buyOrderType = Param(nameof(BuyOrderType), ManualTradingOrderType.MarketExecution)
			.SetDisplay("Buy Order Type", "Order type used when submitting buy orders.", "Buy");

		_sellPriceMode = Param(nameof(SellPriceMode), ManualPriceMode.Market)
			.SetDisplay("Sell Price Mode", "Select automatic or manual price handling for sell orders.", "Sell")
			.SetCanOptimize(false);

		_buyPriceMode = Param(nameof(BuyPriceMode), ManualPriceMode.Market)
			.SetDisplay("Buy Price Mode", "Select automatic or manual price handling for buy orders.", "Buy")
			.SetCanOptimize(false);

		_sellManualPrice = Param(nameof(SellManualPrice), 0m)
			.SetDisplay("Sell Manual Price", "Manual trigger price for sell pending orders.", "Sell")
			.SetCanOptimize(false);

		_buyManualPrice = Param(nameof(BuyManualPrice), 0m)
			.SetDisplay("Buy Manual Price", "Manual trigger price for buy pending orders.", "Buy")
			.SetCanOptimize(false);

		_sendSellOrder = Param(nameof(SendSellOrder), false)
			.SetDisplay("Send Sell Order", "Set to true to submit a sell order.", "Sell")
			.SetCanOptimize(false);

		_sendBuyOrder = Param(nameof(SendBuyOrder), false)
			.SetDisplay("Send Buy Order", "Set to true to submit a buy order.", "Buy")
			.SetCanOptimize(false);
	}

	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	public bool LotControl
	{
		get => _lotControl.Value;
		set => _lotControl.Value = value;
	}

	public decimal LotVolumeStep
	{
		get => _lotVolumeStep.Value;
		set => _lotVolumeStep.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal LimitOrderPoints
	{
		get => _limitOrderPoints.Value;
		set => _limitOrderPoints.Value = value;
	}

	public decimal StopOrderPoints
	{
		get => _stopOrderPoints.Value;
		set => _stopOrderPoints.Value = value;
	}

	public decimal PriceStepPoints
	{
		get => _priceStepPoints.Value;
		set => _priceStepPoints.Value = value;
	}

	public string OrderComment
	{
		get => _orderComment.Value;
		set => _orderComment.Value = value;
	}

	public decimal SellVolume
	{
		get => _sellVolume.Value;
		set => _sellVolume.Value = value;
	}

	public decimal BuyVolume
	{
		get => _buyVolume.Value;
		set => _buyVolume.Value = value;
	}

	public ManualTradingOrderType SellOrderType
	{
		get => _sellOrderType.Value;
		set => _sellOrderType.Value = value;
	}

	public ManualTradingOrderType BuyOrderType
	{
		get => _buyOrderType.Value;
		set => _buyOrderType.Value = value;
	}

	public ManualPriceMode SellPriceMode
	{
		get => _sellPriceMode.Value;
		set => _sellPriceMode.Value = value;
	}

	public ManualPriceMode BuyPriceMode
	{
		get => _buyPriceMode.Value;
		set => _buyPriceMode.Value = value;
	}

	public decimal SellManualPrice
	{
		get => _sellManualPrice.Value;
		set => _sellManualPrice.Value = value;
	}

	public decimal BuyManualPrice
	{
		get => _buyManualPrice.Value;
		set => _buyManualPrice.Value = value;
	}

	public bool SendSellOrder
	{
		get => _sendSellOrder.Value;
		set => _sendSellOrder.Value = value;
	}

	public bool SendBuyOrder
	{
		get => _sendBuyOrder.Value;
		set => _sendBuyOrder.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastBid = null;
		_lastAsk = null;
		_lastTrade = null;
		_protectionTriggered = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
			throw new InvalidOperationException("Security is not specified.");

		if (Portfolio == null)
			throw new InvalidOperationException("Portfolio is not specified.");

		TimerInterval = TimeSpan.FromMilliseconds(200);

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnTimer()
	{
		base.OnTimer();

		UpdateAutoPrices();
		ProcessManualOrders();
		ManageProtection();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid) && bid is decimal bidPrice)
			_lastBid = bidPrice;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask) && ask is decimal askPrice)
			_lastAsk = askPrice;

		if (level1.Changes.TryGetValue(Level1Fields.LastTradePrice, out var trade) && trade is decimal tradePrice)
			_lastTrade = tradePrice;

		UpdateAutoPrices();
		ProcessManualOrders();
		ManageProtection();
	}

	private void UpdateAutoPrices()
	{
		if (!LotControl)
		{
			SellVolume = LotSize;
			BuyVolume = LotSize;
		}

		UpdateAutoPrice(Sides.Sell, SellOrderType, SellPriceMode, value => SellManualPrice = value);
		UpdateAutoPrice(Sides.Buy, BuyOrderType, BuyPriceMode, value => BuyManualPrice = value);
	}

	private void UpdateAutoPrice(Sides side, ManualTradingOrderType orderType, ManualPriceMode mode, Action<decimal> setter)
	{
		if (mode == ManualPriceMode.Manual)
			return;

		if (orderType == ManualTradingOrderType.MarketExecution)
		{
			setter(0m);
			return;
		}

		var price = ComputeAutoPrice(side, orderType);
		if (price != null)
			setter(price.Value);
	}

	private void ProcessManualOrders()
	{
		var sellRequested = SendSellOrder;
		var buyRequested = SendBuyOrder;

		if (!sellRequested && !buyRequested)
			return;

		if (!IsOnline)
		{
			if (sellRequested)
				SendSellOrder = false;

			if (buyRequested)
				SendBuyOrder = false;

			return;
		}

		if (Security == null || Portfolio == null)
		{
			if (sellRequested)
				SendSellOrder = false;

			if (buyRequested)
				SendBuyOrder = false;

			LogWarning("Security or portfolio is not configured. Manual command ignored.");
			return;
		}

		if (sellRequested)
		{
			var volume = ResolveVolume(isBuy: false);
			SendOrder(Sides.Sell, SellOrderType, SellPriceMode, SellManualPrice, volume);
			SendSellOrder = false;
		}

		if (buyRequested)
		{
			var volume = ResolveVolume(isBuy: true);
			SendOrder(Sides.Buy, BuyOrderType, BuyPriceMode, BuyManualPrice, volume);
			SendBuyOrder = false;
		}
	}

	private void SendOrder(Sides side, ManualTradingOrderType orderType, ManualPriceMode priceMode, decimal manualPrice, decimal volume)
	{
		if (volume <= 0m)
		{
			LogWarning($"{side} order skipped because volume is not positive.");
			return;
		}

		var adjustedVolume = NormalizeVolume(volume);
		if (adjustedVolume <= 0m)
		{
			LogWarning($"{side} order skipped because volume is below instrument limits.");
			return;
		}

		var orderPrice = ResolveOrderPrice(side, orderType, priceMode, manualPrice);

		switch (orderType)
		{
			case ManualTradingOrderType.MarketExecution:
				SubmitMarketOrder(side, adjustedVolume);
				break;
			case ManualTradingOrderType.PendingLimit:
				if (orderPrice == null)
				{
					LogWarning($"{side} limit order skipped because price is unavailable.");
					return;
				}

				SubmitLimitOrder(side, adjustedVolume, orderPrice.Value);
				break;
			case ManualTradingOrderType.PendingStop:
				if (orderPrice == null)
				{
					LogWarning($"{side} stop order skipped because price is unavailable.");
					return;
				}

				SubmitStopOrder(side, adjustedVolume, orderPrice.Value);
				break;
		}
	}

	private void SubmitMarketOrder(Sides side, decimal volume)
	{
		if (side == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);
	}

	private void SubmitLimitOrder(Sides side, decimal volume, decimal price)
	{
		var normalizedPrice = NormalizePrice(price);
		if (normalizedPrice <= 0m)
		{
			LogWarning("Limit order price is invalid.");
			return;
		}

		if (side == Sides.Buy)
			BuyLimit(volume, normalizedPrice);
		else
			SellLimit(volume, normalizedPrice);
	}

	private void SubmitStopOrder(Sides side, decimal volume, decimal price)
	{
		var normalizedPrice = NormalizePrice(price);
		if (normalizedPrice <= 0m)
		{
			LogWarning("Stop order price is invalid.");
			return;
		}

		if (side == Sides.Buy)
			BuyStop(volume, normalizedPrice);
		else
			SellStop(volume, normalizedPrice);
	}

	private decimal? ResolveOrderPrice(Sides side, ManualTradingOrderType orderType, ManualPriceMode mode, decimal manualPrice)
	{
		if (orderType == ManualTradingOrderType.MarketExecution)
			return null;

		if (mode == ManualPriceMode.Manual && manualPrice > 0m)
			return manualPrice;

		if (mode == ManualPriceMode.Manual)
		{
			var fallback = ComputeManualFallbackPrice(side, orderType);
			if (fallback != null)
				return fallback;
		}

		return ComputeAutoPrice(side, orderType);
	}

	private decimal? ComputeManualFallbackPrice(Sides side, ManualTradingOrderType orderType)
	{
		var reference = side == Sides.Sell ? _lastBid ?? _lastTrade : _lastAsk ?? _lastTrade;
		if (reference == null)
			return null;

		var offset = GetPriceOffset(PriceStepPoints);
		if (offset <= 0m)
			offset = GetPriceOffset(orderType == ManualTradingOrderType.PendingLimit ? LimitOrderPoints : StopOrderPoints);

		if (offset <= 0m)
			return reference;

		var direction = GetPriceDirection(side, orderType);
		return reference.Value + direction * offset;
	}

	private decimal? ComputeAutoPrice(Sides side, ManualTradingOrderType orderType)
	{
		var reference = side == Sides.Sell ? _lastBid ?? _lastTrade : _lastAsk ?? _lastTrade;
		if (reference == null)
			return null;

		var offset = orderType == ManualTradingOrderType.PendingLimit
			? GetPriceOffset(LimitOrderPoints)
			: GetPriceOffset(StopOrderPoints);

		var direction = GetPriceDirection(side, orderType);
		return reference.Value + direction * offset;
	}

	private static decimal GetPriceDirection(Sides side, ManualTradingOrderType orderType)
	{
		return (side == Sides.Sell ? 1m : -1m) * (orderType == ManualTradingOrderType.PendingLimit ? 1m : -1m);
	}

	private decimal ResolveVolume(bool isBuy)
	{
		return LotControl ? (isBuy ? BuyVolume : SellVolume) : LotSize;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (Security?.VolumeStep is decimal step && step > 0m)
			volume = step * Math.Round(volume / step, MidpointRounding.ToEven);
		else if (LotVolumeStep > 0m)
			volume = LotVolumeStep * Math.Round(volume / LotVolumeStep, MidpointRounding.ToEven);

		if (volume <= 0m)
			return 0m;

		var minVolume = Security?.MinVolume;
		if (minVolume != null && volume < minVolume.Value)
			volume = minVolume.Value;

		var maxVolume = Security?.MaxVolume;
		if (maxVolume != null && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}

	private decimal NormalizePrice(decimal price)
	{
		if (Security?.PriceStep is decimal step && step > 0m)
		{
			var rounded = Math.Round(price / step, MidpointRounding.ToEven);
			price = step * rounded;
		}

		return price;
	}

	private decimal GetPriceOffset(decimal points)
	{
		if (points <= 0m)
			return 0m;

		var step = Security?.PriceStep ?? 1m;
		return points * step;
	}

	private void ManageProtection()
	{
		if (_protectionTriggered)
			return;

		var position = Position;
		if (position == 0m)
			return;

		var reference = GetReferencePrice();
		if (reference == null)
			return;

		var step = Security?.PriceStep ?? 1m;
		var spread = ComputeSpread();
		var absPosition = Math.Abs(position);

		if (position > 0m)
		{
			if (StopLossPoints > 0m)
			{
				var stopPrice = PositionPrice - StopLossPoints * step - spread;
				if (stopPrice > 0m && reference.Value <= stopPrice)
				{
					SellMarket(absPosition);
					_protectionTriggered = true;
					return;
				}
			}

			if (TakeProfitPoints > 0m)
			{
				var takePrice = PositionPrice + TakeProfitPoints * step;
				if (takePrice > 0m && reference.Value >= takePrice)
				{
					SellMarket(absPosition);
					_protectionTriggered = true;
				}
			}
		}
		else
		{
			if (StopLossPoints > 0m)
			{
				var stopPrice = PositionPrice + StopLossPoints * step + spread;
				if (stopPrice > 0m && reference.Value >= stopPrice)
				{
					BuyMarket(absPosition);
					_protectionTriggered = true;
					return;
				}
			}

			if (TakeProfitPoints > 0m)
			{
				var takePrice = PositionPrice - TakeProfitPoints * step;
				if (takePrice > 0m && reference.Value <= takePrice)
				{
					BuyMarket(absPosition);
					_protectionTriggered = true;
				}
			}
		}
	}

	private decimal? GetReferencePrice()
	{
		if (_lastTrade != null)
			return _lastTrade;

		if (_lastBid != null && _lastAsk != null)
			return (_lastBid.Value + _lastAsk.Value) / 2m;

		return _lastBid ?? _lastAsk;
	}

	private decimal ComputeSpread()
	{
		if (_lastBid != null && _lastAsk != null)
			return Math.Max(0m, _lastAsk.Value - _lastBid.Value);

		return 0m;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
			_protectionTriggered = false;
	}
}

