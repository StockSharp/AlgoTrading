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
/// Multi-level breakout scalping strategy converted from the MetaTrader ScalpWiz 9001 expert advisor.
/// Places layered stop orders around the Bollinger Bands envelope and manages exits with trailing logic.
/// </summary>
public class ScalpWiz9001Strategy : Strategy
{
	private readonly StrategyParam<int> _levelCount;

	private enum VolumeModes
	{
		FixedVolume,
		RiskPercent,
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bandsPeriod;
	private readonly StrategyParam<decimal> _bandsDeviation;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _expirationMinutes;
	private readonly StrategyParam<VolumeModes> _volumeMode;
	private readonly StrategyParam<decimal>[] _levelValues;
	private readonly StrategyParam<decimal>[] _levelPips;

	private decimal _pipSize;
	private decimal _tickSize;

	private readonly List<PendingOrderInfo> _pendingOrders = new();

	private decimal? _longStopPrice;
	private decimal? _longTakeProfit;
	private decimal? _shortStopPrice;
	private decimal? _shortTakeProfit;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private bool _longExitRequested;
	private bool _shortExitRequested;

	/// <summary>
	/// Initializes a new instance of the <see cref="ScalpWiz9001Strategy"/> class.
	/// </summary>
	public ScalpWiz9001Strategy()
	{
		const int levelSlots = 4;

		_levelValues = new StrategyParam<decimal>[levelSlots];
		_levelPips = new StrategyParam<decimal>[levelSlots];

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for Bollinger calculations", "General");

		_bandsPeriod = Param(nameof(BandsPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bands Period", "Bollinger Bands period", "General");

		_bandsDeviation = Param(nameof(BandsDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bands Deviation", "Bollinger Bands deviation multiplier", "General");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 15m)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Extra move required before trailing stop adjustment", "Risk");

		_expirationMinutes = Param(nameof(ExpirationMinutes), 15)
			.SetNotNegative()
			.SetDisplay("Expiration (minutes)", "Lifetime of pending stop orders", "Orders");

		_volumeMode = Param(nameof(ManagementMode), VolumeModes.RiskPercent)
			.SetDisplay("Management Mode", "Interpretation of level values (fixed lot or risk percent)", "Money Management");

		_levelCount = Param(nameof(LevelCount), levelSlots)
			.SetRange(1, levelSlots)
			.SetDisplay("Level Count", "Number of active breakout layers", "Money Management");

		_levelValues[0] = Param(nameof(Level0Value), 1m)
			.SetNotNegative()
			.SetDisplay("Level 0 Value", "Volume or risk percent for the first layer", "Money Management");

		_levelValues[1] = Param(nameof(Level1Value), 2m)
			.SetNotNegative()
			.SetDisplay("Level 1 Value", "Volume or risk percent for the second layer", "Money Management");

		_levelValues[2] = Param(nameof(Level2Value), 3m)
			.SetNotNegative()
			.SetDisplay("Level 2 Value", "Volume or risk percent for the third layer", "Money Management");

		_levelValues[3] = Param(nameof(Level3Value), 4m)
			.SetNotNegative()
			.SetDisplay("Level 3 Value", "Volume or risk percent for the farthest layer", "Money Management");

		_levelPips[0] = Param(nameof(Level0Pips), 10m)
			.SetNotNegative()
			.SetDisplay("Level 0 Pips", "Entry offset for the first pending order", "Entries");

		_levelPips[1] = Param(nameof(Level1Pips), 12m)
			.SetNotNegative()
			.SetDisplay("Level 1 Pips", "Entry offset for the second pending order", "Entries");

		_levelPips[2] = Param(nameof(Level2Pips), 15m)
			.SetNotNegative()
			.SetDisplay("Level 2 Pips", "Entry offset for the third pending order", "Entries");

		_levelPips[3] = Param(nameof(Level3Pips), 20m)
			.SetNotNegative()
			.SetDisplay("Level 3 Pips", "Entry offset for the fourth pending order", "Entries");
	}

	/// <summary>
	/// Working candle type used for Bollinger calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Bollinger Bands lookback period.
	/// </summary>
	public int BandsPeriod
	{
		get => _bandsPeriod.Value;
		set => _bandsPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BandsDeviation
	{
		get => _bandsDeviation.Value;
		set => _bandsDeviation.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Extra distance before the trailing stop is moved.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Pending order expiration measured in minutes.
	/// </summary>
	public int ExpirationMinutes
	{
		get => _expirationMinutes.Value;
		set => _expirationMinutes.Value = value;
	}

	/// <summary>
	/// Number of breakout layers actively managed by the strategy.
	/// </summary>
	public int LevelCount
	{
		get => _levelCount.Value;
		set => _levelCount.Value = value;
	}

	/// <summary>
	/// Interprets level values either as fixed volumes or risk percentages.
	/// </summary>
	public VolumeModes ManagementMode
	{
		get => _volumeMode.Value;
		set => _volumeMode.Value = value;
	}

	/// <summary>
	/// Money management value for the first pending order.
	/// </summary>
	public decimal Level0Value
	{
		get => _levelValues[0].Value;
		set => _levelValues[0].Value = value;
	}

	/// <summary>
	/// Money management value for the second pending order.
	/// </summary>
	public decimal Level1Value
	{
		get => _levelValues[1].Value;
		set => _levelValues[1].Value = value;
	}

	/// <summary>
	/// Money management value for the third pending order.
	/// </summary>
	public decimal Level2Value
	{
		get => _levelValues[2].Value;
		set => _levelValues[2].Value = value;
	}

	/// <summary>
	/// Money management value for the fourth pending order.
	/// </summary>
	public decimal Level3Value
	{
		get => _levelValues[3].Value;
		set => _levelValues[3].Value = value;
	}

	/// <summary>
	/// Entry distance for the first pending order in pips.
	/// </summary>
	public decimal Level0Pips
	{
		get => _levelPips[0].Value;
		set => _levelPips[0].Value = value;
	}

	/// <summary>
	/// Entry distance for the second pending order in pips.
	/// </summary>
	public decimal Level1Pips
	{
		get => _levelPips[1].Value;
		set => _levelPips[1].Value = value;
	}

	/// <summary>
	/// Entry distance for the third pending order in pips.
	/// </summary>
	public decimal Level2Pips
	{
		get => _levelPips[2].Value;
		set => _levelPips[2].Value = value;
	}

	/// <summary>
	/// Entry distance for the fourth pending order in pips.
	/// </summary>
	public decimal Level3Pips
	{
		get => _levelPips[3].Value;
		set => _levelPips[3].Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new (Security sec, DataType dt)[]
		{
			(Security, CandleType),
		};
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pipSize = 0m;
		_tickSize = 0m;

		_pendingOrders.Clear();

		_longStopPrice = null;
		_longTakeProfit = null;
		_shortStopPrice = null;
		_shortTakeProfit = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longExitRequested = false;
		_shortExitRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tickSize = Security?.PriceStep ?? 1m;
		if (_tickSize <= 0m)
		{
			_tickSize = 1m;
		}

		var decimals = Security?.Decimals ?? 0;
		_pipSize = _tickSize;
		if (decimals is 3 or 5)
		{
			_pipSize = _tickSize * 10m;
		}

		var bollinger = new BollingerBands
		{
			Length = BandsPeriod,
			Width = BandsDeviation,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		CleanupPendingOrders();
		CancelExpiredPendingOrders(candle.CloseTime);

		ManagePosition(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		if (HasActivePendingOrders())
		{
			return;
		}

		var levelOffsets = GetLevelOffsets();
		if (levelOffsets.Length < LevelCount || levelOffsets[LevelCount - 1] <= 0m)
		{
			return;
		}

		var stopOffset = StopLossPips > 0m ? StopLossPips * _pipSize : 0m;
		var takeOffset = TakeProfitPips > 0m ? TakeProfitPips * _pipSize : 0m;

		var close = candle.ClosePrice;

		if (close - upper >= levelOffsets[LevelCount - 1])
		{
			var bid = GetBidPrice(candle);
			if (bid > 0m)
			{
				PlaceSellStopOrders(bid, levelOffsets, stopOffset, takeOffset, candle.CloseTime);
			}
		}
		else if (lower - close >= levelOffsets[LevelCount - 1])
		{
			var ask = GetAskPrice(candle);
			if (ask > 0m)
			{
				PlaceBuyStopOrders(ask, levelOffsets, stopOffset, takeOffset, candle.CloseTime);
			}
		}
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var entry = _longEntryPrice ?? (PositionPrice != 0m ? PositionPrice : (decimal?)null) ?? candle.ClosePrice;
			_longEntryPrice ??= entry;

			InitializeLongTargets(entry);
			UpdateLongTrailing(candle.ClosePrice, entry);

			if (!_longExitRequested && _longStopPrice is decimal stop && candle.ClosePrice <= stop)
			{
				SellMarket(Position);
				_longExitRequested = true;
				return;
			}

			if (!_longExitRequested && _longTakeProfit is decimal take && candle.ClosePrice >= take)
			{
				SellMarket(Position);
				_longExitRequested = true;
			}
		}
		else if (Position < 0)
		{
			var entry = _shortEntryPrice ?? (PositionPrice != 0m ? PositionPrice : (decimal?)null) ?? candle.ClosePrice;
			_shortEntryPrice ??= entry;

			InitializeShortTargets(entry);
			UpdateShortTrailing(candle.ClosePrice, entry);

			var volume = Math.Abs(Position);
			if (!_shortExitRequested && _shortStopPrice is decimal stop && candle.ClosePrice >= stop)
			{
				BuyMarket(volume);
				_shortExitRequested = true;
				return;
			}

			if (!_shortExitRequested && _shortTakeProfit is decimal take && candle.ClosePrice <= take)
			{
				BuyMarket(volume);
				_shortExitRequested = true;
			}
		}
	}

	private void InitializeLongTargets(decimal entryPrice)
	{
		if (_longStopPrice is null && StopLossPips > 0m)
		{
			var stop = NormalizePrice(entryPrice - StopLossPips * _pipSize);
			_longStopPrice = stop > 0m ? stop : null;
		}

		if (_longTakeProfit is null && TakeProfitPips > 0m)
		{
			var take = NormalizePrice(entryPrice + TakeProfitPips * _pipSize);
			_longTakeProfit = take > 0m ? take : null;
		}
	}

	private void InitializeShortTargets(decimal entryPrice)
	{
		if (_shortStopPrice is null && StopLossPips > 0m)
		{
			var stop = NormalizePrice(entryPrice + StopLossPips * _pipSize);
			_shortStopPrice = stop > 0m ? stop : null;
		}

		if (_shortTakeProfit is null && TakeProfitPips > 0m)
		{
			var take = NormalizePrice(entryPrice - TakeProfitPips * _pipSize);
			_shortTakeProfit = take > 0m ? take : null;
		}
	}

	private void UpdateLongTrailing(decimal price, decimal entry)
	{
		if (TrailingStopPips <= 0m)
		{
			return;
		}

		var trail = TrailingStopPips * _pipSize;
		var step = TrailingStepPips * _pipSize;

		if (price - entry <= trail + step)
		{
			return;
		}

		var newStop = price - trail;
		if (_longStopPrice is null || _longStopPrice < newStop)
		{
			_longStopPrice = NormalizePrice(newStop);
		}
	}

	private void UpdateShortTrailing(decimal price, decimal entry)
	{
		if (TrailingStopPips <= 0m)
		{
			return;
		}

		var trail = TrailingStopPips * _pipSize;
		var step = TrailingStepPips * _pipSize;

		if (entry - price <= trail + step)
		{
			return;
		}

		var newStop = price + trail;
		if (_shortStopPrice is null || _shortStopPrice > newStop)
		{
			_shortStopPrice = NormalizePrice(newStop);
		}
	}

	private void PlaceSellStopOrders(decimal referencePrice, decimal[] levelOffsets, decimal stopOffset, decimal takeOffset, DateTimeOffset candleTime)
	{
		var expiration = ExpirationMinutes > 0 ? candleTime + TimeSpan.FromMinutes(ExpirationMinutes) : (DateTimeOffset?)null;

		for (var i = 0; i < LevelCount; i++)
		{
			var offset = levelOffsets[i];
			if (offset <= 0m)
			{
				continue;
			}

			var entryPrice = NormalizePrice(referencePrice - offset);
			if (entryPrice <= 0m)
			{
				continue;
			}

			var volume = CalculateOrderVolume(i, stopOffset);
			volume = NormalizeVolume(volume);
			if (volume <= 0m)
			{
				continue;
			}

			var order = SellStop(volume, entryPrice);
			if (order == null)
			{
				continue;
			}

			_pendingOrders.Add(new PendingOrderInfo(order, Sides.Sell, stopOffset, takeOffset, expiration));
		}
	}

	private void PlaceBuyStopOrders(decimal referencePrice, decimal[] levelOffsets, decimal stopOffset, decimal takeOffset, DateTimeOffset candleTime)
	{
		var expiration = ExpirationMinutes > 0 ? candleTime + TimeSpan.FromMinutes(ExpirationMinutes) : (DateTimeOffset?)null;

		for (var i = 0; i < LevelCount; i++)
		{
			var offset = levelOffsets[i];
			if (offset <= 0m)
			{
				continue;
			}

			var entryPrice = NormalizePrice(referencePrice + offset);
			if (entryPrice <= 0m)
			{
				continue;
			}

			var volume = CalculateOrderVolume(i, stopOffset);
			volume = NormalizeVolume(volume);
			if (volume <= 0m)
			{
				continue;
			}

			var order = BuyStop(volume, entryPrice);
			if (order == null)
			{
				continue;
			}

			_pendingOrders.Add(new PendingOrderInfo(order, Sides.Buy, stopOffset, takeOffset, expiration));
		}
	}

	private void CancelExpiredPendingOrders(DateTimeOffset currentTime)
	{
		if (ExpirationMinutes <= 0)
		{
			return;
		}

		foreach (var info in _pendingOrders)
		{
			if (info.Expiration is not DateTimeOffset expiration)
			{
				continue;
			}

			var order = info.EntryOrder;
			if (order.State == OrderStates.Active && currentTime >= expiration)
			{
				CancelOrder(order);
			}
		}
	}

	private void CleanupPendingOrders()
	{
		for (var i = _pendingOrders.Count - 1; i >= 0; i--)
		{
			var order = _pendingOrders[i].EntryOrder;
			if (IsFinalState(order))
			{
				_pendingOrders.RemoveAt(i);
			}
		}
	}

	private bool HasActivePendingOrders()
	{
		foreach (var info in _pendingOrders)
		{
			var state = info.EntryOrder.State;
			if (state == OrderStates.Active || state == OrderStates.Pending)
			{
				return true;
			}
		}

		return false;
	}

	private decimal[] GetLevelOffsets()
	{
		var offsets = new decimal[LevelCount];
		for (var i = 0; i < LevelCount; i++)
		{
			var raw = _levelPips[i].Value;
			offsets[i] = raw > 0m ? raw * _pipSize : 0m;
		}

		return offsets;
	}

	private decimal CalculateOrderVolume(int levelIndex, decimal stopOffset)
	{
		var value = _levelValues[levelIndex].Value;
		if (value <= 0m)
		{
			return 0m;
		}

		if (ManagementMode == VolumeModes.FixedVolume)
		{
			return value;
		}

		if (stopOffset <= 0m)
		{
			return 0m;
		}

		var portfolio = Portfolio;
		if (portfolio is null)
		{
			return 0m;
		}

		var equity = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
		if (equity <= 0m)
		{
			return 0m;
		}

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;
		if (priceStep <= 0m || stepPrice <= 0m)
		{
			return 0m;
		}

		var perUnitRisk = stopOffset / priceStep * stepPrice;
		if (perUnitRisk <= 0m)
		{
			return 0m;
		}

		var riskAmount = equity * value / 100m;
		if (riskAmount <= 0m)
		{
			return 0m;
		}

		var rawVolume = riskAmount / perUnitRisk;
		return rawVolume > 0m ? rawVolume : 0m;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
		{
			return 0m;
		}

		var min = Security?.VolumeMin;
		if (min is decimal minVolume && minVolume > 0m && volume < minVolume)
		{
			volume = minVolume;
		}

		var max = Security?.VolumeMax;
		if (max is decimal maxVolume && maxVolume > 0m && volume > maxVolume)
		{
			volume = maxVolume;
		}

		var step = Security?.VolumeStep;
		if (step is decimal stepValue && stepValue > 0m)
		{
			var steps = Math.Max(1m, Math.Floor(volume / stepValue));
			volume = steps * stepValue;
		}

		return volume;
	}

	private decimal NormalizePrice(decimal price)
	{
		if (price <= 0m)
		{
			return price;
		}

		var normalized = Security?.ShrinkPrice(price) ?? price;
		return normalized > 0m ? normalized : price;
	}

	private decimal GetBidPrice(ICandleMessage candle)
	{
		if (Security?.BestBid?.Price is decimal bid && bid > 0m)
		{
			return bid;
		}

		if (Security?.LastPrice is decimal last && last > 0m)
		{
			return last;
		}

		return candle.ClosePrice;
	}

	private decimal GetAskPrice(ICandleMessage candle)
	{
		if (Security?.BestAsk?.Price is decimal ask && ask > 0m)
		{
			return ask;
		}

		if (Security?.LastPrice is decimal last && last > 0m)
		{
			return last;
		}

		return candle.ClosePrice;
	}

	private void CancelAllPendingOrders()
	{
		foreach (var info in _pendingOrders)
		{
			var order = info.EntryOrder;
			if (order.State == OrderStates.Active)
			{
				CancelOrder(order);
			}
		}

		_pendingOrders.Clear();
	}

	private void ResetLongState()
	{
		_longStopPrice = null;
		_longTakeProfit = null;
		_longEntryPrice = null;
		_longExitRequested = false;
	}

	private void ResetShortState()
	{
		_shortStopPrice = null;
		_shortTakeProfit = null;
		_shortEntryPrice = null;
		_shortExitRequested = false;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var order = trade?.Order;
		if (order == null)
		{
			return;
		}

		for (var i = 0; i < _pendingOrders.Count; i++)
		{
			var info = _pendingOrders[i];
			if (info.EntryOrder != order)
			{
				continue;
			}

			_pendingOrders.RemoveAt(i);
			CancelAllPendingOrders();

			var price = trade.Trade?.Price ?? order.Price ?? 0m;
			if (info.Side == Sides.Buy)
			{
				_longEntryPrice = price > 0m ? price : null;
				_longStopPrice = info.StopOffset > 0m ? NormalizePrice(price - info.StopOffset) : null;
				_longTakeProfit = info.TakeOffset > 0m ? NormalizePrice(price + info.TakeOffset) : null;
				_longExitRequested = false;
			}
			else if (info.Side == Sides.Sell)
			{
				_shortEntryPrice = price > 0m ? price : null;
				_shortStopPrice = info.StopOffset > 0m ? NormalizePrice(price + info.StopOffset) : null;
				_shortTakeProfit = info.TakeOffset > 0m ? NormalizePrice(price - info.TakeOffset) : null;
				_shortExitRequested = false;
			}

			break;
		}

		if (_longExitRequested && Position <= 0)
		{
			ResetLongState();
		}

		if (_shortExitRequested && Position >= 0)
		{
			ResetShortState();
		}
	}

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		if (order == null)
		{
			return;
		}

		CleanupPendingOrders();
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position > 0)
		{
			ResetShortState();
		}
		else if (Position < 0)
		{
			ResetLongState();
		}
		else
		{
			ResetLongState();
			ResetShortState();
		}
	}

	private static bool IsFinalState(Order order)
	{
		return order.State == OrderStates.Done
			|| order.State == OrderStates.Failed
			|| order.State == OrderStates.Cancelled;
	}

	private sealed class PendingOrderInfo
	{
		public PendingOrderInfo(Order entryOrder, Sides side, decimal stopOffset, decimal takeOffset, DateTimeOffset? expiration)
		{
			EntryOrder = entryOrder;
			Side = side;
			StopOffset = stopOffset;
			TakeOffset = takeOffset;
			Expiration = expiration;
		}

		public Order EntryOrder { get; }
		public Sides Side { get; }
		public decimal StopOffset { get; }
		public decimal TakeOffset { get; }
		public DateTimeOffset? Expiration { get; }
	}
}

