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
/// Conversion of the MetaTrader "HistoryTrain" helper into a high level StockSharp strategy.
/// It automates the horizontal-line workflow by managing breakout, pullback and market entries
/// around three configurable price levels while respecting optional stop-loss and take-profit bands.
/// </summary>
public class HistTrainingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _upperLevel;
	private readonly StrategyParam<decimal> _entryLevel;
	private readonly StrategyParam<decimal> _lowerLevel;
	private readonly StrategyParam<bool> _enableLongSide;
	private readonly StrategyParam<bool> _enableShortSide;
	private readonly StrategyParam<bool> _useBreakoutOrders;
	private readonly StrategyParam<bool> _usePullbackOrders;
	private readonly StrategyParam<bool> _enableMarketBuy;
	private readonly StrategyParam<bool> _enableMarketSell;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _buyLimitOrder;
	private Order _sellLimitOrder;

	private decimal? _previousClose;

	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	/// <summary>
	/// Price level acting as take profit for long trades and stop loss for short trades.
	/// </summary>
	public decimal UpperLevel
	{
		get => _upperLevel.Value;
		set => _upperLevel.Value = value;
	}

	/// <summary>
	/// Central entry level used to anchor stop/limit orders and market triggers.
	/// </summary>
	public decimal EntryLevel
	{
		get => _entryLevel.Value;
		set => _entryLevel.Value = value;
	}

	/// <summary>
	/// Price level acting as stop loss for long trades and take profit for short trades.
	/// </summary>
	public decimal LowerLevel
	{
		get => _lowerLevel.Value;
		set => _lowerLevel.Value = value;
	}

	/// <summary>
	/// Enables submission of long side orders.
	/// </summary>
	public bool EnableLongSide
	{
		get => _enableLongSide.Value;
		set => _enableLongSide.Value = value;
	}

	/// <summary>
	/// Enables submission of short side orders.
	/// </summary>
	public bool EnableShortSide
	{
		get => _enableShortSide.Value;
		set => _enableShortSide.Value = value;
	}

	/// <summary>
	/// When true the strategy prepares stop orders to trade breakouts through the entry level.
	/// </summary>
	public bool UseBreakoutOrders
	{
		get => _useBreakoutOrders.Value;
		set => _useBreakoutOrders.Value = value;
	}

	/// <summary>
	/// When true the strategy prepares limit orders to trade pullbacks towards the entry level.
	/// </summary>
	public bool UsePullbackOrders
	{
		get => _usePullbackOrders.Value;
		set => _usePullbackOrders.Value = value;
	}

	/// <summary>
	/// Automatically opens a market buy when price closes above the entry level while flat.
	/// </summary>
	public bool EnableMarketBuy
	{
		get => _enableMarketBuy.Value;
		set => _enableMarketBuy.Value = value;
	}

	/// <summary>
	/// Automatically opens a market sell when price closes below the entry level while flat.
	/// </summary>
	public bool EnableMarketSell
	{
		get => _enableMarketSell.Value;
		set => _enableMarketSell.Value = value;
	}

	/// <summary>
	/// Base order volume used for every new position.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Candle type driving the price monitoring process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes default parameters for the HistTraining strategy.
	/// </summary>
	public HistTrainingStrategy()
	{
		_upperLevel = Param(nameof(UpperLevel), 0m)
			.SetDisplay("Upper level", "Price level acting as take profit for longs and stop loss for shorts.", "Levels");

		_entryLevel = Param(nameof(EntryLevel), 0m)
			.SetDisplay("Entry level", "Central level used to anchor orders and triggers.", "Levels");

		_lowerLevel = Param(nameof(LowerLevel), 0m)
			.SetDisplay("Lower level", "Price level acting as stop loss for longs and take profit for shorts.", "Levels");

		_enableLongSide = Param(nameof(EnableLongSide), true)
			.SetDisplay("Enable long side", "Allow the strategy to submit long side trades.", "Trading");

		_enableShortSide = Param(nameof(EnableShortSide), true)
			.SetDisplay("Enable short side", "Allow the strategy to submit short side trades.", "Trading");

		_useBreakoutOrders = Param(nameof(UseBreakoutOrders), true)
			.SetDisplay("Use breakout orders", "Submit stop orders when price is on the wrong side of the entry level.", "Trading");

		_usePullbackOrders = Param(nameof(UsePullbackOrders), true)
			.SetDisplay("Use pullback orders", "Submit limit orders when price is on the favorable side of the entry level.", "Trading");

		_enableMarketBuy = Param(nameof(EnableMarketBuy), false)
			.SetDisplay("Market buy trigger", "Enter a market buy when a candle closes above the entry level.", "Market triggers");

		_enableMarketSell = Param(nameof(EnableMarketSell), false)
			.SetDisplay("Market sell trigger", "Enter a market sell when a candle closes below the entry level.", "Market triggers");

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetDisplay("Order volume", "Base volume used for pending and market orders.", "Risk management")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Time-frame used for level interactions.", "General");
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

		_buyStopOrder = null;
		_sellStopOrder = null;
		_buyLimitOrder = null;
		_sellLimitOrder = null;

		_previousClose = null;

		ResetProtectionLevels();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = AdjustVolume(OrderVolume);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		if (Position == 0)
		{
			ResetProtectionLevels();
			ManagePendingOrders(candle.ClosePrice);
			TryEnterMarket(candle);
		}
		else
		{
			CancelAllPendingOrders();
			UpdateProtectionLevels();
			ManageOpenPosition(candle);
		}

		_previousClose = candle.ClosePrice;
	}

	private void ManagePendingOrders(decimal closePrice)
	{
		if (Volume <= 0m)
		{
			CancelAllPendingOrders();
			return;
		}

		var entry = EntryLevel;
		var canLong = EnableLongSide && entry > 0m;
		var canShort = EnableShortSide && entry > 0m;

		NormalizeOrderReference(ref _buyStopOrder);
		NormalizeOrderReference(ref _sellStopOrder);
		NormalizeOrderReference(ref _buyLimitOrder);
		NormalizeOrderReference(ref _sellLimitOrder);

		EnsureOrder(
			ref _buyStopOrder,
			UseBreakoutOrders && canLong && closePrice < entry,
			entry,
			price =>
			{
				var normalized = NormalizePrice(price);
				var volume = Volume;
				LogInfo($"Submitting buy stop at {normalized:F5} for volume {volume:F3}.");
				return BuyStop(volume, normalized);
			});

		EnsureOrder(
			ref _sellStopOrder,
			UseBreakoutOrders && canShort && closePrice > entry,
			entry,
			price =>
			{
				var normalized = NormalizePrice(price);
				var volume = Volume;
				LogInfo($"Submitting sell stop at {normalized:F5} for volume {volume:F3}.");
				return SellStop(volume, normalized);
			});

		EnsureOrder(
			ref _buyLimitOrder,
			UsePullbackOrders && canLong && closePrice > entry,
			entry,
			price =>
			{
				var normalized = NormalizePrice(price);
				var volume = Volume;
				LogInfo($"Submitting buy limit at {normalized:F5} for volume {volume:F3}.");
				return BuyLimit(volume, normalized);
			});

		EnsureOrder(
			ref _sellLimitOrder,
			UsePullbackOrders && canShort && closePrice < entry,
			entry,
			price =>
			{
				var normalized = NormalizePrice(price);
				var volume = Volume;
				LogInfo($"Submitting sell limit at {normalized:F5} for volume {volume:F3}.");
				return SellLimit(volume, normalized);
			});
	}

	private void TryEnterMarket(ICandleMessage candle)
	{
		if (!EnableLongSide && !EnableShortSide)
			return;

		if (EntryLevel <= 0m)
			return;

		if (_previousClose == null)
			return;

		var previous = _previousClose.Value;
		var current = candle.ClosePrice;

		if (EnableMarketBuy && EnableLongSide && previous <= EntryLevel && current > EntryLevel)
		{
			var volume = Volume + Math.Max(0m, -Position);
			if (volume > 0m)
			{
				BuyMarket(volume);
				LogInfo($"Market buy activated at {current:F5}, entry level {EntryLevel:F5}.");
				CancelAllPendingOrders();
				_longStopPrice = LowerLevel > 0m ? NormalizePrice(LowerLevel) : null;
				_longTakePrice = UpperLevel > 0m ? NormalizePrice(UpperLevel) : null;
			}
		}

		if (EnableMarketSell && EnableShortSide && previous >= EntryLevel && current < EntryLevel)
		{
			var volume = Volume + Math.Max(0m, Position);
			if (volume > 0m)
			{
				SellMarket(volume);
				LogInfo($"Market sell activated at {current:F5}, entry level {EntryLevel:F5}.");
				CancelAllPendingOrders();
				_shortStopPrice = UpperLevel > 0m ? NormalizePrice(UpperLevel) : null;
				_shortTakePrice = LowerLevel > 0m ? NormalizePrice(LowerLevel) : null;
			}
		}
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		if (Position > 0)
		{
			if (_longStopPrice != null && candle.LowPrice <= _longStopPrice.Value)
			{
				SellMarket(volume);
				LogInfo($"Long stop loss triggered at {_longStopPrice.Value:F5}.");
				ResetProtectionLevels();
				return;
			}

			if (_longTakePrice != null && candle.HighPrice >= _longTakePrice.Value)
			{
				SellMarket(volume);
				LogInfo($"Long take profit triggered at {_longTakePrice.Value:F5}.");
				ResetProtectionLevels();
			}
		}
		else if (Position < 0)
		{
			if (_shortStopPrice != null && candle.HighPrice >= _shortStopPrice.Value)
			{
				BuyMarket(volume);
				LogInfo($"Short stop loss triggered at {_shortStopPrice.Value:F5}.");
				ResetProtectionLevels();
				return;
			}

			if (_shortTakePrice != null && candle.LowPrice <= _shortTakePrice.Value)
			{
				BuyMarket(volume);
				LogInfo($"Short take profit triggered at {_shortTakePrice.Value:F5}.");
				ResetProtectionLevels();
			}
		}
	}

	private void UpdateProtectionLevels()
	{
		if (Position > 0)
		{
			_longStopPrice ??= LowerLevel > 0m ? NormalizePrice(LowerLevel) : null;
			_longTakePrice ??= UpperLevel > 0m ? NormalizePrice(UpperLevel) : null;
		}
		else if (Position < 0)
		{
			_shortStopPrice ??= UpperLevel > 0m ? NormalizePrice(UpperLevel) : null;
			_shortTakePrice ??= LowerLevel > 0m ? NormalizePrice(LowerLevel) : null;
		}
	}

	private void ResetProtectionLevels()
	{
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	private void EnsureOrder(ref Order order, bool shouldExist, decimal price, Func<decimal, Order> register)
	{
		if (!shouldExist)
		{
			CancelTrackedOrder(ref order);
			return;
		}

		var normalized = NormalizePrice(price);

		if (order != null)
		{
			if (IsOrderActive(order) && order.Price == normalized)
				return;

			if (!IsOrderActive(order))
				order = null;
			else
				CancelTrackedOrder(ref order);
		}

		if (Volume <= 0m)
			return;

		order = register(normalized);
	}

	private void CancelAllPendingOrders()
	{
		CancelTrackedOrder(ref _buyStopOrder);
		CancelTrackedOrder(ref _sellStopOrder);
		CancelTrackedOrder(ref _buyLimitOrder);
		CancelTrackedOrder(ref _sellLimitOrder);
	}

	private void CancelTrackedOrder(ref Order order)
	{
		if (order == null)
			return;

		if (IsOrderActive(order))
			base.CancelOrder(order);

		order = null;
	}

	private void NormalizeOrderReference(ref Order order)
	{
		if (order != null && !IsOrderActive(order))
			order = null;
	}

	private static bool IsOrderActive(Order order)
	{
		return order.State is OrderStates.None or OrderStates.Pending or OrderStates.Active;
	}

	private decimal NormalizePrice(decimal price)
	{
		return Security?.ShrinkPrice(price) ?? price;
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (Security is null)
			return volume;

		var step = Security.VolumeStep ?? 0m;
		if (step > 0m)
			volume = step * Math.Floor(volume / step);

		var minVolume = Security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			return 0m;

		var maxVolume = Security.MaxVolume;
		if (maxVolume != null && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}
}
