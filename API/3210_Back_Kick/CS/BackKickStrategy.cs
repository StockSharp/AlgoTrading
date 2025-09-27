using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hedged breakout strategy that immediately opens both long and short positions
/// on every new candle, mirroring the MetaTrader expert "Back kick.mq5".
/// </summary>
public class BackKickStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _logDiagnostics;
	private readonly StrategyParam<decimal> _volumeTolerance;

	private readonly Dictionary<Order, PendingOrderInfo> _pendingOrders = new();

	private PositionState _longPosition;
	private PositionState _shortPosition;

	private decimal _pipValue;
	private decimal _stopLossOffset;
	private decimal _takeProfitOffset;

	private decimal _bestBid;
	private decimal _bestAsk;
	private bool _hasBestBid;
	private bool _hasBestAsk;
	private bool _shouldOpenPair;
	private bool _isPairOpening;
	private int _pendingEntryOrders;

	/// <summary>
	/// Volume applied to each hedged leg.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips converted to absolute price offsets.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips converted to absolute price offsets.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Candle type that defines the bar close events used to trigger entries.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Enables verbose diagnostic logging for fills and exits.
	/// </summary>
	public bool LogDiagnostics
	{
		get => _logDiagnostics.Value;
		set => _logDiagnostics.Value = value;
	}

	/// <summary>
	/// Minimal volume difference treated as meaningful when reconciling hedged orders.
	/// </summary>
	public decimal VolumeTolerance
	{
		get => _volumeTolerance.Value;
		set => _volumeTolerance.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public BackKickStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Volume of each hedged leg", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 2m, 0.1m);

		_stopLossPips = Param(nameof(StopLossPips), 50)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0, 200, 10);

		_takeProfitPips = Param(nameof(TakeProfitPips), 140)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(50, 400, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for entry triggers", "General");

		_logDiagnostics = Param(nameof(LogDiagnostics), false)
		.SetDisplay("Log Diagnostics", "Write detailed fill information", "Logging");

		_volumeTolerance = Param(nameof(VolumeTolerance), 0.0000001m)
		.SetGreaterThanZero()
		.SetDisplay("Volume Tolerance", "Minimum difference treated as a volume change", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (Security, DataType.Level1);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pendingOrders.Clear();
		_longPosition = null;
		_shortPosition = null;
		_bestBid = 0m;
		_bestAsk = 0m;
		_hasBestBid = false;
		_hasBestAsk = false;
		_shouldOpenPair = false;
		_isPairOpening = false;
		_pendingEntryOrders = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_pipValue = CalculatePipValue();
		_stopLossOffset = StopLossPips > 0 ? StopLossPips * _pipValue : 0m;
		_takeProfitOffset = TakeProfitPips > 0 ? TakeProfitPips * _pipValue : 0m;

		StartProtection();

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription
		.Bind(ProcessCandle)
		.Start();

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		// Only request a new pair when the previous hedge is completely flat.
		if (HasOpenExposure())
		{
			return;
		}

		_shouldOpenPair = true;
		ProcessStrategy();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
		{
			var bid = (decimal)bidValue;
			if (bid > 0m)
			{
				_bestBid = bid;
				_hasBestBid = true;
			}
		}

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
		{
			var ask = (decimal)askValue;
			if (ask > 0m)
			{
				_bestAsk = ask;
				_hasBestAsk = true;
			}
		}

		ProcessStrategy();
	}

	private void ProcessStrategy()
	{
		if (!_hasBestBid || !_hasBestAsk)
		{
			return;
		}

		if (!HasOpenExposure())
		{
			if (_shouldOpenPair)
			{
				TryOpenPair();
			}

			return;
		}

		if (_longPosition != null)
		{
			ManageLong(_longPosition);
		}

		if (_shortPosition != null)
		{
			ManageShort(_shortPosition);
		}
	}

	private bool HasOpenExposure()
	{
		return _isPairOpening
		|| (_longPosition is { IsActive: true })
		|| (_shortPosition is { IsActive: true });
	}

	private void TryOpenPair()
	{
		if (_isPairOpening)
		{
			return;
		}

		if (Security == null || Portfolio == null)
		{
			return;
		}

		var volume = NormalizeVolume(OrderVolume);
		if (volume <= 0m)
		{
			return;
		}

		_shouldOpenPair = false;
		_isPairOpening = true;

		// Create independent position trackers for long and short legs.
		var longPosition = new PositionState { Side = Sides.Buy };
		var longOrder = CreateMarketOrder(Sides.Buy, volume, "BackKick:LongEntry");
		RegisterEntryOrder(longOrder, longPosition);

		var shortPosition = new PositionState { Side = Sides.Sell };
		var shortOrder = CreateMarketOrder(Sides.Sell, volume, "BackKick:ShortEntry");
		RegisterEntryOrder(shortOrder, shortPosition);
	}

	private void ManageLong(PositionState position)
	{
		if (!position.IsActive || position.IsClosing)
		{
			return;
		}

		var price = _bestBid;
		if (price <= 0m)
		{
			return;
		}

		// Stop loss check for the long leg.
		if (position.StopPrice is decimal stop && stop > 0m && price <= stop)
		{
			ClosePosition(position, CloseReason.StopLoss);
			return;
		}

		// Take profit check for the long leg.
		if (position.TakePrice is decimal take && take > 0m && price >= take)
		{
			ClosePosition(position, CloseReason.TakeProfit);
		}
	}

	private void ManageShort(PositionState position)
	{
		if (!position.IsActive || position.IsClosing)
		{
			return;
		}

		var price = _bestAsk;
		if (price <= 0m)
		{
			return;
		}

		// Stop loss check for the short leg.
		if (position.StopPrice is decimal stop && stop > 0m && price >= stop)
		{
			ClosePosition(position, CloseReason.StopLoss);
			return;
		}

		// Take profit check for the short leg.
		if (position.TakePrice is decimal take && take > 0m && price <= take)
		{
			ClosePosition(position, CloseReason.TakeProfit);
		}
	}

	private void RegisterEntryOrder(Order order, PositionState position)
	{
		_pendingOrders[order] = new PendingOrderInfo
		{
			Position = position,
			IsEntry = true,
			RemainingVolume = order.Volume,
			CloseReason = CloseReason.None
		};

		_pendingEntryOrders++;

		RegisterOrder(order);
	}

	private void RegisterExitOrder(Order order, PositionState position, CloseReason reason)
	{
		_pendingOrders[order] = new PendingOrderInfo
		{
			Position = position,
			IsEntry = false,
			RemainingVolume = order.Volume,
			CloseReason = reason
		};

		RegisterOrder(order);
	}

	private Order CreateMarketOrder(Sides side, decimal volume, string comment)
	{
		return new Order
		{
			Security = Security,
			Portfolio = Portfolio,
			Volume = volume,
			Side = side,
			Type = OrderTypes.Market,
			Comment = comment
		};
	}

	private void ClosePosition(PositionState position, CloseReason reason)
	{
		if (Security == null || Portfolio == null)
		{
			return;
		}

		if (position.IsClosing)
		{
			return;
		}

		var volume = NormalizeVolume(position.Volume);
		if (volume <= 0m)
		{
			ReleasePosition(position);
			return;
		}

		var exitSide = position.Side == Sides.Buy ? Sides.Sell : Sides.Buy;
		var order = CreateMarketOrder(exitSide, volume, reason == CloseReason.TakeProfit ? "BackKick:TakeProfit" : "BackKick:StopLoss");

		position.IsClosing = true;
		RegisterExitOrder(order, position, reason);
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
		{
			return;
		}

		if (!_pendingOrders.TryGetValue(trade.Order, out var info))
		{
			return;
		}

		var tradeVolume = trade.Trade.Volume;
		var tradePrice = trade.Trade.Price;

		info.RemainingVolume -= tradeVolume;
		info.FilledVolume += tradeVolume;
		info.WeightedPrice += tradePrice * tradeVolume;

		if (info.RemainingVolume > VolumeTolerance)
		{
			return;
		}

		_pendingOrders.Remove(trade.Order);

		if (info.FilledVolume <= 0m)
		{
			return;
		}

		var averagePrice = info.WeightedPrice / info.FilledVolume;
		var position = info.Position;

		if (info.IsEntry)
		{
			if (position.Side == Sides.Buy)
			{
				_longPosition = position;
			}
			else
			{
				_shortPosition = position;
			}

			position.Volume = info.FilledVolume;
			position.EntryPrice = averagePrice;
			position.IsActive = true;
			position.IsClosing = false;
			position.StopPrice = _stopLossOffset > 0m
			? (position.Side == Sides.Buy ? averagePrice - _stopLossOffset : averagePrice + _stopLossOffset)
			: null;
			position.TakePrice = _takeProfitOffset > 0m
			? (position.Side == Sides.Buy ? averagePrice + _takeProfitOffset : averagePrice - _takeProfitOffset)
			: null;

			if (_pendingEntryOrders > 0)
			{
				_pendingEntryOrders--;
			}

			if (_pendingEntryOrders == 0)
			{
				_isPairOpening = false;
			}

			LogTrade($"{position.Side} entry filled at {averagePrice} with volume {info.FilledVolume}");
		}
		else
		{
			ReleasePosition(position);
			LogTrade($"{position.Side} exit filled at {averagePrice} with volume {info.FilledVolume} ({info.CloseReason})");
		}
	}

	private void ReleasePosition(PositionState position)
	{
		position.IsActive = false;
		position.IsClosing = false;
		position.Volume = 0m;
		position.StopPrice = null;
		position.TakePrice = null;

		if (position.Side == Sides.Buy)
		{
			_longPosition = null;
		}
		else
		{
			_shortPosition = null;
		}
	}

	private decimal CalculatePipValue()
	{
		var security = Security ?? throw new InvalidOperationException("Security is not set.");
		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		{
			throw new InvalidOperationException("Price step is not specified for the security.");
		}

		var decimals = security.Decimals;
		var adjust = decimals == 3 || decimals == 5 ? 10m : 1m;
		return step * adjust;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
		{
			return 0m;
		}

		var security = Security ?? throw new InvalidOperationException("Security is not set.");

		var minVolume = security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume - VolumeTolerance)
		{
			throw new InvalidOperationException($"Order volume {volume} is less than the minimal allowed {minVolume}.");
		}

		var maxVolume = security.MaxVolume ?? 0m;
		if (maxVolume > 0m && volume > maxVolume + VolumeTolerance)
		{
			throw new InvalidOperationException($"Order volume {volume} exceeds the maximal allowed {maxVolume}.");
		}

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var ratio = Math.Round(volume / step);
			var normalized = ratio * step;
			if (Math.Abs(normalized - volume) > VolumeTolerance)
			{
				throw new InvalidOperationException($"Order volume {volume} is not aligned with the volume step {step}. Closest valid volume is {normalized}.");
			}

			volume = normalized;
		}

		return volume > 0m ? volume : 0m;
	}

	private void LogTrade(string message)
	{
		if (LogDiagnostics)
		{
			LogInfo(message);
		}
	}

	private enum CloseReason
	{
		None,
		StopLoss,
		TakeProfit
	}

	private sealed class PositionState
	{
		public required Sides Side { get; init; }
		public decimal Volume { get; set; }
		public decimal EntryPrice { get; set; }
		public decimal? StopPrice { get; set; }
		public decimal? TakePrice { get; set; }
		public bool IsActive { get; set; }
		public bool IsClosing { get; set; }
	}

	private sealed class PendingOrderInfo
	{
		public required PositionState Position { get; init; }
		public bool IsEntry { get; init; }
		public decimal RemainingVolume { get; set; }
		public decimal FilledVolume { get; set; }
		public decimal WeightedPrice { get; set; }
		public CloseReason CloseReason { get; init; }
	}
}
