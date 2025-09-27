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
/// Avalanche grid strategy translated from the MetaTrader Avalanche v1.2 EA.
/// Focuses on stacking positions toward the equilibrium reference price (ERP).
/// </summary>
public class AvalancheStrategy : Strategy
{
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _towardMultiplier;
	private readonly StrategyParam<decimal> _towardInterestMultiplier;
	private readonly StrategyParam<decimal> _stopLossToward;
	private readonly StrategyParam<decimal> _takeProfitToward;
	private readonly StrategyParam<decimal> _intervalToward;
	private readonly StrategyParam<decimal> _stackBufferToward;
	private readonly StrategyParam<int> _erpPeriod;
	private readonly StrategyParam<decimal> _erpChangeBuffer;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _erpCandleType;
	private readonly StrategyParam<bool> _openStartingOrders;

	private readonly List<GridEntry> _entries = new();
	private readonly Dictionary<string, Sides> _interestMap = new(StringComparer.OrdinalIgnoreCase)
	{
		["AUDUSD"] = Sides.Buy,
		["AUDNZD"] = Sides.Sell,
		["CHFJPY"] = Sides.Buy,
		["EURAUD"] = Sides.Sell,
		["EURCAD"] = Sides.Sell,
		["EURCHF"] = Sides.Buy,
		["EURGBP"] = Sides.Sell,
		["EURJPY"] = Sides.Buy,
		["EURUSD"] = Sides.Sell,
		["GBPCHF"] = Sides.Buy,
		["GBPJPY"] = Sides.Buy,
		["GBPUSD"] = Sides.Buy,
		["USDCAD"] = Sides.Buy,
		["USDCHF"] = Sides.Buy,
		["USDJPY"] = Sides.Buy,
	};

	private SMA _erpSma;
	private decimal? _currentErp;
	private decimal _pipSize;
	private ErpPositions _erpPosition;
	private Sides? _interestSide;
	private Sides? _gridDirection;
	private Sides? _pendingEntrySide;
	private decimal _lastEntryPrice;
	private decimal _adverseReferencePrice;
	private bool _startingOrdersOpened;
	private bool _isClosingAll;

	private enum ErpPositions
	{
		None,
		Above,
		Below,
	}

	private struct GridEntry
	{
		public decimal Volume;
		public decimal EntryPrice;
		public decimal? TakeProfit;
		public decimal? StopLoss;
		public bool PendingClose;
	}

	/// <summary>
	/// Base order volume before applying multipliers.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to standard toward-ERP entries.
	/// </summary>
	public decimal TowardMultiplier
	{
		get => _towardMultiplier.Value;
		set => _towardMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier used when the instrument pays positive swap in the entry direction.
	/// </summary>
	public decimal TowardInterestMultiplier
	{
		get => _towardInterestMultiplier.Value;
		set => _towardInterestMultiplier.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for toward entries measured in points.
	/// </summary>
	public decimal StopLossToward
	{
		get => _stopLossToward.Value;
		set => _stopLossToward.Value = value;
	}

	/// <summary>
	/// Take-profit distance for toward entries measured in points.
	/// </summary>
	public decimal TakeProfitToward
	{
		get => _takeProfitToward.Value;
		set => _takeProfitToward.Value = value;
	}

	/// <summary>
	/// Distance between consecutive grid additions in the favorable direction.
	/// </summary>
	public decimal IntervalToward
	{
		get => _intervalToward.Value;
		set => _intervalToward.Value = value;
	}

	/// <summary>
	/// Additional buffer added when stacking against adverse price movements.
	/// </summary>
	public decimal StackBufferToward
	{
		get => _stackBufferToward.Value;
		set => _stackBufferToward.Value = value;
	}

	/// <summary>
	/// Number of periods for the ERP simple moving average.
	/// </summary>
	public int ErpPeriod
	{
		get => _erpPeriod.Value;
		set => _erpPeriod.Value = value;
	}

	/// <summary>
	/// Buffer (in points) used before flipping the ERP bias.
	/// </summary>
	public decimal ErpChangeBuffer
	{
		get => _erpChangeBuffer.Value;
		set => _erpChangeBuffer.Value = value;
	}

	/// <summary>
	/// Candle type used for trading decisions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Candle type used to calculate the ERP moving average.
	/// </summary>
	public DataType ErpCandleType
	{
		get => _erpCandleType.Value;
		set => _erpCandleType.Value = value;
	}

	/// <summary>
	/// When enabled the strategy immediately opens the first grid order after a signal.
	/// </summary>
	public bool OpenStartingOrders
	{
		get => _openStartingOrders.Value;
		set => _openStartingOrders.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AvalancheStrategy"/>.
	/// </summary>
	public AvalancheStrategy()
	{
		_baseVolume = Param(nameof(BaseVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Base order volume before multipliers", "Volume")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.5m, 0.05m);

		_towardMultiplier = Param(nameof(TowardMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Toward Multiplier", "Multiplier for standard toward entries", "Volume");

		_towardInterestMultiplier = Param(nameof(TowardInterestMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Toward Interest Multiplier", "Multiplier applied when swap is positive", "Volume");

		_stopLossToward = Param(nameof(StopLossToward), 0m)
			.SetDisplay("Toward Stop Loss", "Stop loss distance in points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0m, 200m, 20m);

		_takeProfitToward = Param(nameof(TakeProfitToward), 10m)
			.SetDisplay("Toward Take Profit", "Take profit distance in points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(5m, 50m, 5m);

		_intervalToward = Param(nameof(IntervalToward), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Toward Interval", "Distance between stacking orders", "Grid");

		_stackBufferToward = Param(nameof(StackBufferToward), 5m)
			.SetDisplay("Toward Stack Buffer", "Additional buffer for adverse stacking", "Grid");

		_erpPeriod = Param(nameof(ErpPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("ERP Period", "SMA period for ERP calculation", "ERP");

		_erpChangeBuffer = Param(nameof(ErpChangeBuffer), 50m)
			.SetDisplay("ERP Buffer", "Buffer in points before switching bias", "ERP");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Trading Candle", "Timeframe for trade decisions", "General");

		_erpCandleType = Param(nameof(ErpCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("ERP Candle", "Timeframe for ERP calculation", "ERP");

		_openStartingOrders = Param(nameof(OpenStartingOrders), true)
			.SetDisplay("Open Starting Orders", "Automatically open the first grid order", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var security = Security;
		if (security == null)
			yield break;

		yield return (security, CandleType);

		if (!Equals(CandleType, ErpCandleType))
			yield return (security, ErpCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entries.Clear();
		_erpSma = null;
		_currentErp = null;
		_pipSize = 0m;
		_erpPosition = ErpPositions.None;
		_interestSide = null;
		_gridDirection = null;
		_pendingEntrySide = null;
		_lastEntryPrice = 0m;
		_adverseReferencePrice = 0m;
		_startingOrdersOpened = false;
		_isClosingAll = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_interestSide = ResolveInterestSide();
		_pipSize = CalculatePipSize();

		_erpSma = new SMA { Length = ErpPeriod };

		var erpSubscription = SubscribeCandles(ErpCandleType);
		erpSubscription
			.Bind(_erpSma, ProcessErpCandle)
			.Start();

		var priceSubscription = SubscribeCandles(CandleType);
		priceSubscription
			.Bind(ProcessMainCandle)
			.Start();

		var chart = CreateChartArea();
		if (chart != null)
		{
			DrawCandles(chart, priceSubscription);
			if (_erpSma != null)
				DrawIndicator(chart, _erpSma, erpSubscription);
			DrawOwnTrades(chart);
		}
	}

	private void ProcessErpCandle(ICandleMessage candle, decimal erpValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_erpSma == null || !_erpSma.IsFormed)
			return;

		_currentErp = erpValue;
	}

	private void ProcessMainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_pipSize <= 0m)
			_pipSize = CalculatePipSize();

		if (_pipSize <= 0m)
			return;

		if (_currentErp is not decimal erp)
			return;

		ManageOpenEntries(candle.ClosePrice);

		var newPosition = DetermineErpPosition(candle.ClosePrice, erp);
		if (newPosition != _erpPosition)
			OnErpPositionChanged(newPosition, candle.ClosePrice);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_isClosingAll)
			return;

		if (_erpPosition == ErpPositions.None)
			return;

		var desiredSide = _erpPosition == ErpPositions.Below ? Sides.Buy : Sides.Sell;

		if (_entries.Count == 0)
		{
			if (OpenStartingOrders && !_startingOrdersOpened && !_pendingEntrySide.HasValue)
				RequestEntry(desiredSide);

			return;
		}

		HandleGridEntries(desiredSide, candle.ClosePrice);
	}

	private ErpPositions DetermineErpPosition(decimal price, decimal erp)
	{
		var buffer = ErpChangeBuffer * _pipSize;

		return _erpPosition switch
		{
			ErpPositions.None => price >= erp ? ErpPositions.Above : ErpPositions.Below,
			ErpPositions.Above => price >= erp - buffer ? ErpPositions.Above : ErpPositions.Below,
			ErpPositions.Below => price >= erp + buffer ? ErpPositions.Above : ErpPositions.Below,
			_ => ErpPositions.None,
		};
	}

	private void OnErpPositionChanged(ErpPositions newPosition, decimal price)
	{
		var previous = _erpPosition;
		_erpPosition = newPosition;

		LogInfo($"ERP position changed from {previous} to {newPosition} at price {price:F5} with ERP {_currentErp:F5}.");

		_pendingEntrySide = null;
		_startingOrdersOpened = false;
		_lastEntryPrice = 0m;
		_adverseReferencePrice = 0m;

		if (_entries.Count > 0)
			CloseAllEntries();
	}

	private void CloseAllEntries()
	{
		if (_entries.Count == 0)
			return;

		_isClosingAll = true;

		for (var i = _entries.Count - 1; i >= 0; i--)
			CloseEntry(i);
	}

	private void HandleGridEntries(Sides desiredSide, decimal price)
	{
		if (_gridDirection != desiredSide)
			return;

		if (_pendingEntrySide.HasValue)
			return;

		var interval = IntervalToward * _pipSize;
		if (interval > 0m)
		{
			if (desiredSide == Sides.Buy)
			{
				if (price - _lastEntryPrice >= interval)
					RequestEntry(desiredSide);
			}
			else
			{
				if (_lastEntryPrice - price >= interval)
					RequestEntry(desiredSide);
			}
		}

		var stackThreshold = (IntervalToward + StackBufferToward) * _pipSize;
		if (stackThreshold <= 0m)
			return;

		if (desiredSide == Sides.Buy)
		{
			if (_adverseReferencePrice - price >= stackThreshold)
				RequestEntry(desiredSide);
		}
		else
		{
			if (price - _adverseReferencePrice >= stackThreshold)
				RequestEntry(desiredSide);
		}
	}

	private void RequestEntry(Sides side)
	{
		if (_pendingEntrySide.HasValue)
			return;

		var volume = GetEntryVolume(side);
		if (volume <= 0m)
			return;

		_pendingEntrySide = side;

		if (side == Sides.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);
	}

	private decimal GetEntryVolume(Sides side)
	{
		var multiplier = TowardMultiplier;
		if (_interestSide.HasValue && _interestSide == side)
			multiplier = TowardInterestMultiplier;

		var volume = BaseVolume * multiplier;
		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 0m;
		if (step <= 0m)
			step = 1m;

		var normalized = Math.Round(volume / step) * step;
		if (normalized <= 0m)
			normalized = step;

		return normalized;
	}

	private decimal? CalculateTakeProfit(Sides side, decimal price)
	{
		if (TakeProfitToward <= 0m || _pipSize <= 0m)
			return null;

		var offset = TakeProfitToward * _pipSize;
		return side == Sides.Buy ? price + offset : price - offset;
	}

	private decimal? CalculateStopLoss(Sides side, decimal price)
	{
		if (StopLossToward <= 0m || _pipSize <= 0m)
			return null;

		var offset = StopLossToward * _pipSize;
		return side == Sides.Buy ? price - offset : price + offset;
	}

	private void ManageOpenEntries(decimal price)
	{
		if (_entries.Count == 0)
			return;

		for (var i = _entries.Count - 1; i >= 0; i--)
		{
			var entry = _entries[i];
			if (entry.PendingClose)
				continue;

			if (entry.TakeProfit is decimal takeProfit)
			{
				if (_gridDirection == Sides.Buy && price >= takeProfit)
					CloseEntry(i);
				else if (_gridDirection == Sides.Sell && price <= takeProfit)
					CloseEntry(i);
			}

			if (entry.StopLoss is decimal stopLoss)
			{
				if (_gridDirection == Sides.Buy && price <= stopLoss)
					CloseEntry(i);
				else if (_gridDirection == Sides.Sell && price >= stopLoss)
					CloseEntry(i);
			}
		}
	}

	private void CloseEntry(int index)
	{
		if (index < 0 || index >= _entries.Count)
			return;

		var entry = _entries[index];
		if (entry.PendingClose || entry.Volume <= 0m)
			return;

		var volume = NormalizeVolume(entry.Volume);
		if (volume <= 0m)
			return;

		entry.PendingClose = true;
		_entries[index] = entry;

		if (_gridDirection == Sides.Buy)
			SellMarket(volume);
		else if (_gridDirection == Sides.Sell)
			BuyMarket(volume);
	}

		/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var order = trade.Order;
		if (order == null)
			return;

		var side = order.Side;
		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;

		if (volume <= 0m)
			return;

		if (side == Sides.Buy || side == Sides.Sell)
		{
			if (_pendingEntrySide.HasValue && side == _pendingEntrySide)
			{
				AddEntry(side, volume, price);
				_pendingEntrySide = null;
				return;
			}

			if (_gridDirection.HasValue && side == _gridDirection)
			{
				AddEntry(side, volume, price);
			}
			else
			{
				ReduceEntries(volume);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnOrderFailed(Order order, OrderFail fail)
	{
		base.OnOrderFailed(order, fail);

		if (order?.Side != null && _pendingEntrySide.HasValue && order.Side == _pendingEntrySide)
			_pendingEntrySide = null;
	}


	private void AddEntry(Sides side, decimal volume, decimal price)
	{
		var normalized = NormalizeVolume(volume);
		if (normalized <= 0m)
			return;

		_entries.Add(new GridEntry
		{
			Volume = normalized,
			EntryPrice = price,
			TakeProfit = CalculateTakeProfit(side, price),
			StopLoss = CalculateStopLoss(side, price),
			PendingClose = false,
		});

		_gridDirection = side;
		_lastEntryPrice = price;
		_adverseReferencePrice = price;
		_startingOrdersOpened = true;
	}

	private void ReduceEntries(decimal volume)
	{
		var remaining = volume;

		for (var i = _entries.Count - 1; i >= 0 && remaining > 0m; i--)
		{
			var entry = _entries[i];
			var reduce = Math.Min(entry.Volume, remaining);
			entry.Volume -= reduce;
			entry.PendingClose = false;
			remaining -= reduce;

			if (entry.Volume <= 0m)
				_entries.RemoveAt(i);
			else
				_entries[i] = entry;
		}

		if (_entries.Count == 0)
		{
			_gridDirection = null;
			_lastEntryPrice = 0m;
			_adverseReferencePrice = 0m;
			_pendingEntrySide = null;
			_startingOrdersOpened = false;
			if (_isClosingAll)
				_isClosingAll = false;
		}
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		var step = security.PriceStep ?? security.Step ?? 0m;
		if (step <= 0m)
			step = 0.0001m;

		var current = step;
		var digits = 0;
		while (current < 1m && digits < 10)
		{
			current *= 10m;
			digits++;
		}

		return digits == 3 || digits == 5 ? step * 10m : step;
	}

	private Sides? ResolveInterestSide()
	{
		var code = Security?.Code;
		if (code != null && _interestMap.TryGetValue(code, out var side))
			return side;

		return null;
	}
}

