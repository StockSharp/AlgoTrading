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
/// Automated recreation of the eInTradePanel manual trading tool.
/// </summary>
public class EInTradePanelStrategy : Strategy
{
	/// <summary>
	/// Supported entry types.
	/// </summary>
	public enum EntryMode
	{
		Buy,
		Sell,
		BuyStop,
		SellStop,
		BuyLimit,
		SellLimit,
		BuyStopLimit,
		SellStopLimit,
	}


	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<EntryMode> _entryMode;
	private readonly StrategyParam<int> _baseTicks;
	private readonly StrategyParam<int> _pendingMultiplier;
	private readonly StrategyParam<int> _triggerMultiplier;
	private readonly StrategyParam<int> _stopLossMultiplier;
	private readonly StrategyParam<int> _takeProfitMultiplier;
	private readonly StrategyParam<bool> _useAtrScaling;
	private readonly StrategyParam<decimal> _atrFactor;
	private readonly StrategyParam<int> _expirationMinutes;
	private readonly StrategyParam<int> _minExpirationMinutes;
	private readonly StrategyParam<int> _atrLength;

	private AverageTrueRange _atr = null!;
	private decimal? _lastAtr;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private Order _entryOrder;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private bool _stopLimitWaiting;
	private decimal? _stopLimitTrigger;
	private decimal? _stopLimitEntry;
	private decimal? _pendingStop;
	private decimal? _pendingTake;
	private DateTimeOffset? _expirationTime;

	/// <summary>
	/// Initializes a new instance of <see cref="EInTradePanelStrategy"/>.
	/// </summary>
	public EInTradePanelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Aggregation for ATR and signals", "General");

		_volume = Param(nameof(Volume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "Trading");

		_entryMode = Param(nameof(Mode), EntryMode.Buy)
		.SetDisplay("Mode", "Type of entry order", "Trading");

		_baseTicks = Param(nameof(BaseTicks), 10)
		.SetGreaterThanZero()
		.SetDisplay("Base Ticks", "Fallback tick distance when ATR is not available", "Risk");

		_pendingMultiplier = Param(nameof(PendingMultiplier), 14)
		.SetGreaterThanZero()
		.SetDisplay("Pending Multiplier", "Multiplier for pending order distance", "Risk");

		_triggerMultiplier = Param(nameof(TriggerMultiplier), 7)
		.SetGreaterThanZero()
		.SetDisplay("Trigger Multiplier", "Multiplier for stop-limit trigger distance", "Risk");

		_stopLossMultiplier = Param(nameof(StopLossMultiplier), 14)
		.SetDisplay("Stop Multiplier", "Multiplier for stop-loss distance", "Risk");

		_takeProfitMultiplier = Param(nameof(TakeProfitMultiplier), 28)
		.SetDisplay("Take Multiplier", "Multiplier for take-profit distance", "Risk");

		_useAtrScaling = Param(nameof(UseAtrScaling), true)
		.SetDisplay("Use ATR", "Scale distances with ATR", "Risk");

		_atrFactor = Param(nameof(AtrFactor), 0.15m)
		.SetDisplay("ATR Factor", "Fraction of ATR used as spread proxy", "Risk");

		_atrLength = Param(nameof(AtrLength), 55)
			.SetDisplay("ATR Length", "Number of candles used to calculate ATR", "Risk")
			.SetRange(1, 500);

		_expirationMinutes = Param(nameof(ExpirationMinutes), 0)
		.SetDisplay("Expiration", "Minutes before pending orders expire", "Risk");

		_minExpirationMinutes = Param(nameof(MinExpirationMinutes), 11)
		.SetDisplay("Min Expiration", "Minimum pending lifetime in minutes", "Risk");
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Selected entry mode.
	/// </summary>
	public EntryMode Mode
	{
		get => _entryMode.Value;
		set => _entryMode.Value = value;
	}

	/// <summary>
	/// Base number of ticks used when ATR is not ready.
	/// </summary>
	public int BaseTicks
	{
		get => _baseTicks.Value;
		set => _baseTicks.Value = value;
	}

	/// <summary>
	/// Multiplier applied to pending order distances.
	/// </summary>
	public int PendingMultiplier
	{
		get => _pendingMultiplier.Value;
		set => _pendingMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier used to compute stop-limit trigger distance.
	/// </summary>
	public int TriggerMultiplier
	{
		get => _triggerMultiplier.Value;
		set => _triggerMultiplier.Value = value;
	}

	/// <summary>
	/// Stop-loss multiplier in ticks.
	/// </summary>
	public int StopLossMultiplier
	{
		get => _stopLossMultiplier.Value;
		set => _stopLossMultiplier.Value = value;
	}

	/// <summary>
	/// Take-profit multiplier in ticks.
	/// </summary>
	public int TakeProfitMultiplier
	{
		get => _takeProfitMultiplier.Value;
		set => _takeProfitMultiplier.Value = value;
	}

	/// <summary>
	/// Enables ATR driven scaling.
	/// </summary>
	public bool UseAtrScaling
	{
		get => _useAtrScaling.Value;
		set => _useAtrScaling.Value = value;
	}

	/// <summary>
	/// Fraction of ATR treated as synthetic spread.
	/// </summary>
	public decimal AtrFactor
	{
		get => _atrFactor.Value;
		set => _atrFactor.Value = value;
	}

	/// <summary>
	/// Number of candles used to calculate the ATR indicator.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Pending order expiration time in minutes.
	/// </summary>
	public int ExpirationMinutes
	{
		get => _expirationMinutes.Value;
		set => _expirationMinutes.Value = value;
	}

	/// <summary>
	/// Minimum allowed pending lifetime in minutes.
	/// </summary>
	public int MinExpirationMinutes
	{
		get => _minExpirationMinutes.Value;
		set => _minExpirationMinutes.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_atr, ProcessCandle)
		.Start();

		SubscribeOrderBook()
		.Bind(orderBook =>
		{
			_bestBid = orderBook.GetBestBid()?.Price ?? _bestBid;
			_bestAsk = orderBook.GetBestAsk()?.Price ?? _bestAsk;
		})
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (atrValue > 0m)
		_lastAtr = atrValue;

		var (bid, ask) = GetBidAsk(candle);
		var priceStep = Security.PriceStep ?? 1m;
		if (priceStep <= 0m)
		priceStep = 1m;

		var baseTicks = CalculateBaseTicks(priceStep, bid, ask);

		ManagePosition(candle);
		UpdateOrderState();
		ManageExpiration(candle);

		if (Position != 0)
		return;

		if (_entryOrder != null)
		return;

		if (Mode is EntryMode.BuyStopLimit or EntryMode.SellStopLimit)
		{
			if (!_stopLimitWaiting)
			PrepareStopLimit(Mode, baseTicks, priceStep, bid, ask);

			TryTriggerStopLimit(candle);
			return;
		}

		SubmitEntryOrder(Mode, baseTicks, priceStep, bid, ask, candle);
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetProtection();
				return;
			}

			if (_takePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				ResetProtection();
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(-Position);
				ResetProtection();
				return;
			}

			if (_takePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(-Position);
				ResetProtection();
			}
		}
		else
		{
			ResetProtection();
		}
	}

	private void UpdateOrderState()
	{
		if (_entryOrder == null)
		return;

		switch (_entryOrder.State)
		{
		case OrderStates.Done:
		case OrderStates.Failed:
		case OrderStates.Canceled:
			_entryOrder = null;
			_expirationTime = null;
			break;
		}
	}

	private void ManageExpiration(ICandleMessage candle)
	{
		if (_entryOrder == null || _expirationTime == null)
		return;

		if (candle.CloseTime >= _expirationTime.Value && IsOrderAlive(_entryOrder))
		{
			CancelOrder(_entryOrder);
			_expirationTime = null;
		}
	}

	private void PrepareStopLimit(EntryMode mode, int baseTicks, decimal priceStep, decimal bid, decimal ask)
	{
		var levels = CalculateLevels(mode, priceStep, baseTicks, bid, ask);
		if (levels.trigger is null || levels.entry is null)
		return;

		_stopLimitTrigger = levels.trigger;
		_stopLimitEntry = levels.entry;
		_pendingStop = levels.stop;
		_pendingTake = levels.take;
		_stopLimitWaiting = true;
	}

	private void TryTriggerStopLimit(ICandleMessage candle)
	{
		if (!_stopLimitWaiting || _stopLimitTrigger is null || _stopLimitEntry is null)
		return;

		if (Mode == EntryMode.BuyStopLimit)
		{
			if (candle.HighPrice < _stopLimitTrigger.Value)
			return;

			_entryOrder = BuyLimit(_stopLimitEntry.Value, Volume);
		}
		else
		{
			if (candle.LowPrice > _stopLimitTrigger.Value)
			return;

			_entryOrder = SellLimit(_stopLimitEntry.Value, Volume);
		}

		_stopPrice = _pendingStop;
		_takePrice = _pendingTake;
		_stopLimitWaiting = false;
		_pendingStop = null;
		_pendingTake = null;
		_expirationTime = GetExpiration(candle);
	}

	private void SubmitEntryOrder(EntryMode mode, int baseTicks, decimal priceStep, decimal bid, decimal ask, ICandleMessage candle)
	{
		var levels = CalculateLevels(mode, priceStep, baseTicks, bid, ask);

		switch (mode)
		{
		case EntryMode.Buy:
			BuyMarket(Volume);
			_stopPrice = levels.stop;
			_takePrice = levels.take;
			break;
		case EntryMode.Sell:
			SellMarket(Volume);
			_stopPrice = levels.stop;
			_takePrice = levels.take;
			break;
		case EntryMode.BuyStop:
			if (levels.entry is decimal buyStop)
			{
				_entryOrder = BuyStop(Volume, buyStop);
				_stopPrice = levels.stop;
				_takePrice = levels.take;
				_expirationTime = GetExpiration(candle);
			}
			break;
		case EntryMode.SellStop:
			if (levels.entry is decimal sellStop)
			{
				_entryOrder = SellStop(Volume, sellStop);
				_stopPrice = levels.stop;
				_takePrice = levels.take;
				_expirationTime = GetExpiration(candle);
			}
			break;
		case EntryMode.BuyLimit:
			if (levels.entry is decimal buyLimit)
			{
				_entryOrder = BuyLimit(buyLimit, Volume);
				_stopPrice = levels.stop;
				_takePrice = levels.take;
				_expirationTime = GetExpiration(candle);
			}
			break;
		case EntryMode.SellLimit:
			if (levels.entry is decimal sellLimit)
			{
				_entryOrder = SellLimit(sellLimit, Volume);
				_stopPrice = levels.stop;
				_takePrice = levels.take;
				_expirationTime = GetExpiration(candle);
			}
			break;
		}
	}

	private (decimal? trigger, decimal? entry, decimal? stop, decimal? take) CalculateLevels(EntryMode mode, decimal priceStep, int baseTicks, decimal bid, decimal ask)
	{
		var pendingTicks = baseTicks * PendingMultiplier;
		var triggerTicks = baseTicks * TriggerMultiplier;
		var stopTicks = baseTicks * StopLossMultiplier;
		var takeTicks = baseTicks * TakeProfitMultiplier;

		decimal? trigger = null;
		decimal? entry = null;
		decimal? stop = null;
		decimal? take = null;

		decimal reference;

		switch (mode)
		{
		case EntryMode.Buy:
			reference = ask;
			entry = reference;
			if (StopLossMultiplier > 0)
			stop = reference - stopTicks * priceStep;
			if (TakeProfitMultiplier > 0)
			take = reference + takeTicks * priceStep;
			break;
		case EntryMode.Sell:
			reference = bid;
			entry = reference;
			if (StopLossMultiplier > 0)
			stop = reference + stopTicks * priceStep;
			if (TakeProfitMultiplier > 0)
			take = reference - takeTicks * priceStep;
			break;
		case EntryMode.BuyStop:
			reference = ask + pendingTicks * priceStep;
			reference = Math.Max(reference, ask + priceStep);
			entry = reference;
			if (StopLossMultiplier > 0)
			stop = reference - stopTicks * priceStep;
			if (TakeProfitMultiplier > 0)
			take = reference + takeTicks * priceStep;
			break;
		case EntryMode.SellStop:
			reference = bid - pendingTicks * priceStep;
			reference = Math.Min(reference, bid - priceStep);
			entry = reference;
			if (StopLossMultiplier > 0)
			stop = reference + stopTicks * priceStep;
			if (TakeProfitMultiplier > 0)
			take = reference - takeTicks * priceStep;
			break;
		case EntryMode.BuyLimit:
			reference = ask - pendingTicks * priceStep;
			reference = Math.Min(reference, ask - priceStep);
			entry = reference;
			if (StopLossMultiplier > 0)
			stop = reference - stopTicks * priceStep;
			if (TakeProfitMultiplier > 0)
			take = reference + takeTicks * priceStep;
			break;
		case EntryMode.SellLimit:
			reference = bid + pendingTicks * priceStep;
			reference = Math.Max(reference, bid + priceStep);
			entry = reference;
			if (StopLossMultiplier > 0)
			stop = reference + stopTicks * priceStep;
			if (TakeProfitMultiplier > 0)
			take = reference - takeTicks * priceStep;
			break;
		case EntryMode.BuyStopLimit:
			trigger = ask + triggerTicks * priceStep;
			trigger = Math.Max(trigger.Value, ask + priceStep);
			reference = trigger.Value - pendingTicks * priceStep;
			reference = Math.Min(reference, trigger.Value - priceStep);
			entry = reference;
			if (StopLossMultiplier > 0)
			stop = reference - stopTicks * priceStep;
			if (TakeProfitMultiplier > 0)
			take = reference + takeTicks * priceStep;
			break;
		case EntryMode.SellStopLimit:
			trigger = bid - triggerTicks * priceStep;
			trigger = Math.Min(trigger.Value, bid - priceStep);
			reference = trigger.Value + pendingTicks * priceStep;
			reference = Math.Max(reference, trigger.Value + priceStep);
			entry = reference;
			if (StopLossMultiplier > 0)
			stop = reference + stopTicks * priceStep;
			if (TakeProfitMultiplier > 0)
			take = reference - takeTicks * priceStep;
			break;
		}

		return (trigger, entry, stop, take);
	}

	private int CalculateBaseTicks(decimal priceStep, decimal bid, decimal ask)
	{
		var baseTicks = Math.Max(BaseTicks, 1);

		if (UseAtrScaling && _lastAtr is decimal atr && priceStep > 0m)
		{
			var atrTicks = atr / priceStep * AtrFactor;
			var dynamicTicks = (int)Math.Max(1, Math.Round((double)atrTicks));
			baseTicks = Math.Max(baseTicks, dynamicTicks);
		}

		if (priceStep > 0m && bid > 0m && ask > 0m && ask > bid)
		{
			var spreadTicks = (int)Math.Max(1, Math.Round((double)((ask - bid) / priceStep)));
			baseTicks = Math.Max(baseTicks, spreadTicks);
		}

		return baseTicks;
	}

	private (decimal bid, decimal ask) GetBidAsk(ICandleMessage candle)
	{
		var close = candle.ClosePrice;
		var bid = _bestBid ?? close;
		var ask = _bestAsk ?? close;

		if (bid <= 0m)
		bid = close;

		if (ask <= 0m)
		ask = close;

		if (ask < bid)
		{
			bid = close;
			ask = close;
		}

		return (bid, ask);
	}

	private DateTimeOffset? GetExpiration(ICandleMessage candle)
	{
		if (ExpirationMinutes <= 0)
		return null;

		var minMinutes = Math.Max(MinExpirationMinutes, 0);
		var minExpiration = candle.CloseTime + TimeSpan.FromMinutes(minMinutes);
		var requested = candle.CloseTime + TimeSpan.FromMinutes(ExpirationMinutes);

		return requested < minExpiration ? minExpiration : requested;
	}

	private static bool IsOrderAlive(Order order)
	{
		return order.State != OrderStates.Done &&
		order.State != OrderStates.Canceled &&
		order.State != OrderStates.Failed;
	}

	private void ResetProtection()
	{
		if (Position == 0)
		{
			_stopPrice = null;
			_takePrice = null;
		}
	}
}

