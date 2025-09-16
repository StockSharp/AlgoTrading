using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader 4 expert advisor "RUBBERBANDS_3".
/// Implements a band expansion grid that alternates long and short sequences after retracements.
/// </summary>
public class Rubberbands3Strategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<int> _pipStep;
	private readonly StrategyParam<int> _backStep;
	private readonly StrategyParam<bool> _quiesceNow;
	private readonly StrategyParam<bool> _doNow;
	private readonly StrategyParam<bool> _stopNow;
	private readonly StrategyParam<bool> _closeNow;
	private readonly StrategyParam<bool> _useSessionTakeProfit;
	private readonly StrategyParam<decimal> _sessionTakeProfit;
	private readonly StrategyParam<bool> _useSessionStopLoss;
	private readonly StrategyParam<decimal> _sessionStopLoss;
	private readonly StrategyParam<bool> _useInitialValues;
	private readonly StrategyParam<decimal> _initialMax;
	private readonly StrategyParam<decimal> _initialMin;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<PositionEntry> _longEntries = new();
	private readonly List<PositionEntry> _shortEntries = new();

	private decimal _realizedProfit;
	private decimal _maxPrice;
	private decimal _minPrice;
	private decimal _pipSize;
	private bool _hasBounds;
	private bool _shouldCloseAll;
	private bool _shouldCloseLong;
	private bool _shouldCloseShort;
	private bool _shouldOpenLong;
	private bool _shouldOpenShort;
	private bool _isForward;
	private Sides? _lastSide;
	private Sides? _pendingEntry;

	/// <summary>
	/// Initializes a new instance of the <see cref="Rubberbands3Strategy"/> class.
	/// </summary>
	public Rubberbands3Strategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume submitted with each market order.", "General")
			.SetCanOptimize(true);

		_maxOrders = Param(nameof(MaxOrders), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max Orders", "Maximum simultaneous positions per direction.", "General")
			.SetCanOptimize(true);

		_pipStep = Param(nameof(PipStep), 100)
			.SetGreaterThanZero()
			.SetDisplay("Pip Step", "Distance in points required to add a new trade.", "Bands")
			.SetCanOptimize(true);

		_backStep = Param(nameof(BackStep), 20)
			.SetGreaterOrEqualZero()
			.SetDisplay("Back Step", "Retracement in points that triggers an exit.", "Bands")
			.SetCanOptimize(true);

		_quiesceNow = Param(nameof(QuiesceNow), false)
			.SetDisplay("Quiesce Now", "Pause when no positions are open.", "Control");

		_doNow = Param(nameof(DoNow), false)
			.SetDisplay("Open Immediately", "Request an immediate entry on start.", "Control");

		_stopNow = Param(nameof(StopNow), false)
			.SetDisplay("Stop Now", "Skip all trading activity.", "Control");

		_closeNow = Param(nameof(CloseNow), false)
			.SetDisplay("Close Now", "Close every position as soon as possible.", "Control");

		_useSessionTakeProfit = Param(nameof(UseSessionTakeProfit), true)
			.SetDisplay("Use Session Take-Profit", "Enable cumulative profit target.", "Session");

		_sessionTakeProfit = Param(nameof(SessionTakeProfit), 2000m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Session Take-Profit", "Profit target in account currency per lot.", "Session")
			.SetCanOptimize(true);

		_useSessionStopLoss = Param(nameof(UseSessionStopLoss), true)
			.SetDisplay("Use Session Stop-Loss", "Enable loss cap while reversing.", "Session");

		_sessionStopLoss = Param(nameof(SessionStopLoss), 4000m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Session Stop-Loss", "Loss limit in account currency per lot.", "Session")
			.SetCanOptimize(true);

		_useInitialValues = Param(nameof(UseInitialValues), false)
			.SetDisplay("Use Initial Extremes", "Reuse saved price extremes when restarting.", "Session");

		_initialMax = Param(nameof(InitialMax), 0m)
			.SetDisplay("Initial Max", "Manually supplied upper extreme.", "Session");

		_initialMin = Param(nameof(InitialMin), 0m)
			.SetDisplay("Initial Min", "Manually supplied lower extreme.", "Session");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe processed by the strategy.", "General");
	}

	/// <summary>
	/// Base volume used when submitting market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Maximum number of concurrent orders in a single direction.
	/// </summary>
	public int MaxOrders
	{
		get => _maxOrders.Value;
		set => _maxOrders.Value = value;
	}

	/// <summary>
	/// Required distance in points to expand the position.
	/// </summary>
	public int PipStep
	{
		get => _pipStep.Value;
		set => _pipStep.Value = value;
	}

	/// <summary>
	/// Retracement distance in points that forces the strategy to switch direction.
	/// </summary>
	public int BackStep
	{
		get => _backStep.Value;
		set => _backStep.Value = value;
	}

	/// <summary>
	/// Pause trading when there are no active positions.
	/// </summary>
	public bool QuiesceNow
	{
		get => _quiesceNow.Value;
		set => _quiesceNow.Value = value;
	}

	/// <summary>
	/// Request an immediate entry on start.
	/// </summary>
	public bool DoNow
	{
		get => _doNow.Value;
		set => _doNow.Value = value;
	}

	/// <summary>
	/// Disable all trading activity while keeping the strategy running.
	/// </summary>
	public bool StopNow
	{
		get => _stopNow.Value;
		set => _stopNow.Value = value;
	}

	/// <summary>
	/// Force the strategy to close every open position.
	/// </summary>
	public bool CloseNow
	{
		get => _closeNow.Value;
		set => _closeNow.Value = value;
	}

	/// <summary>
	/// Enable cumulative session profit target.
	/// </summary>
	public bool UseSessionTakeProfit
	{
		get => _useSessionTakeProfit.Value;
		set => _useSessionTakeProfit.Value = value;
	}

	/// <summary>
	/// Session profit target expressed in account currency per lot.
	/// </summary>
	public decimal SessionTakeProfit
	{
		get => _sessionTakeProfit.Value;
		set => _sessionTakeProfit.Value = value;
	}

	/// <summary>
	/// Enable cumulative session stop-loss.
	/// </summary>
	public bool UseSessionStopLoss
	{
		get => _useSessionStopLoss.Value;
		set => _useSessionStopLoss.Value = value;
	}

	/// <summary>
	/// Session loss limit expressed in account currency per lot.
	/// </summary>
	public decimal SessionStopLoss
	{
		get => _sessionStopLoss.Value;
		set => _sessionStopLoss.Value = value;
	}

	/// <summary>
	/// Reuse saved price extremes when restarting the strategy.
	/// </summary>
	public bool UseInitialValues
	{
		get => _useInitialValues.Value;
		set => _useInitialValues.Value = value;
	}

	/// <summary>
	/// Upper extreme reused on restart when <see cref="UseInitialValues"/> is true.
	/// </summary>
	public decimal InitialMax
	{
		get => _initialMax.Value;
		set => _initialMax.Value = value;
	}

	/// <summary>
	/// Lower extreme reused on restart when <see cref="UseInitialValues"/> is true.
	/// </summary>
	public decimal InitialMin
	{
		get => _initialMin.Value;
		set => _initialMin.Value = value;
	}

	/// <summary>
	/// Primary candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longEntries.Clear();
		_shortEntries.Clear();
		_realizedProfit = 0m;
		_maxPrice = 0m;
		_minPrice = 0m;
		_pipSize = 0m;
		_hasBounds = false;
		_shouldCloseAll = false;
		_shouldCloseLong = false;
		_shouldCloseShort = false;
		_shouldOpenLong = false;
		_shouldOpenShort = false;
		_isForward = true;
		_lastSide = null;
		_pendingEntry = null;
	}
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = Security?.PriceStep ?? 0m;
		if (_pipSize <= 0m)
		{
			LogWarning("PriceStep is not defined for the selected security. Falling back to 0.0001.");
			_pipSize = 0.0001m;
		}

		_shouldCloseAll = CloseNow;
		_shouldCloseLong = false;
		_shouldCloseShort = false;
		_shouldOpenLong = DoNow;
		_shouldOpenShort = false;
		_isForward = true;
		_pendingEntry = null;
		_lastSide = null;

		if (UseInitialValues)
		{
			_maxPrice = InitialMax;
			_minPrice = InitialMin;
			_hasBounds = true;
		}
		else
		{
			_hasBounds = false;
		}

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var closePrice = candle.ClosePrice;

		EnsureBounds(closePrice);
		HandlePendingEntry();

		if (StopNow)
			return;

		if (CloseNow)
			_shouldCloseAll = true;

		var openProfit = CalculateOpenProfit(closePrice);
		var totalProfit = _realizedProfit + openProfit;

		if (UseSessionTakeProfit && totalProfit >= SessionTakeProfit * OrderVolume)
			_shouldCloseAll = true;

		if (UseSessionStopLoss && !_isForward && openProfit <= -SessionStopLoss * OrderVolume)
			_shouldCloseAll = true;

		if (ExecuteNextAction())
			return;

		var totalOrders = _longEntries.Count + _shortEntries.Count;

		if (totalOrders == 0)
		{
			if (QuiesceNow)
				return;

			if (!_hasBounds)
			{
				_maxPrice = closePrice;
				_minPrice = closePrice;
				_hasBounds = true;
			}
		}

		if (totalOrders >= MaxOrders)
			return;

		if (!_isForward)
			return;

		var pipDistance = PipStep * _pipSize;
		var backDistance = BackStep * _pipSize;

		if ((_lastSide == null || _lastSide == Sides.Buy) && closePrice >= _maxPrice + pipDistance)
		{
			_maxPrice = closePrice;
			_shouldOpenLong = true;
			_lastSide = Sides.Buy;

			ExecuteNextAction();
			return;
		}

		if (_lastSide == Sides.Buy && backDistance > 0m && closePrice <= _maxPrice - backDistance)
		{
			_shouldCloseLong = true;
			_pendingEntry = Sides.Sell;
			_isForward = false;

			ExecuteNextAction();
			return;
		}

		if ((_lastSide == null || _lastSide == Sides.Sell) && closePrice <= _minPrice - pipDistance)
		{
			_minPrice = closePrice;
			_shouldOpenShort = true;
			_lastSide = Sides.Sell;

			ExecuteNextAction();
			return;
		}

		if (_lastSide == Sides.Sell && backDistance > 0m && closePrice >= _minPrice + backDistance)
		{
			_shouldCloseShort = true;
			_pendingEntry = Sides.Buy;
			_isForward = false;

			ExecuteNextAction();
		}
	}

	private void HandlePendingEntry()
	{
		if (_pendingEntry == null)
			return;

		if (_longEntries.Count > 0 || _shortEntries.Count > 0)
			return;

		if (_pendingEntry == Sides.Buy)
		{
			_shouldOpenLong = true;
			_isForward = true;
			_hasBounds = false;
		}
		else
		{
			_shouldOpenShort = true;
			_isForward = true;
			_hasBounds = false;
		}

		_pendingEntry = null;
	}
	private bool ExecuteNextAction()
	{
		if (_shouldCloseAll)
		{
			if (_longEntries.Count > 0)
			{
				if (ExecuteCloseLong())
					return true;
			}

			if (_shortEntries.Count > 0)
			{
				if (ExecuteCloseShort())
					return true;
			}

			if (_longEntries.Count == 0 && _shortEntries.Count == 0)
			{
				_shouldCloseAll = false;
				_isForward = true;
				_lastSide = null;
				_pendingEntry = null;
				_hasBounds = false;
				_realizedProfit = 0m;
			}
		}

		if (_shouldCloseLong)
		{
			if (_longEntries.Count == 0)
			{
				_shouldCloseLong = false;
			}
			else if (ExecuteCloseLong())
			{
				return true;
			}
		}

		if (_shouldCloseShort)
		{
			if (_shortEntries.Count == 0)
			{
				_shouldCloseShort = false;
			}
			else if (ExecuteCloseShort())
			{
				return true;
			}
		}

		if (_shouldOpenLong)
		{
			if (_shortEntries.Count > 0)
			{
				_shouldCloseShort = true;
				_pendingEntry = Sides.Buy;
				_shouldOpenLong = false;
				return false;
			}

			if (ExecuteOpenLong())
			{
				_shouldOpenLong = false;
				return true;
			}
		}

		if (_shouldOpenShort)
		{
			if (_longEntries.Count > 0)
			{
				_shouldCloseLong = true;
				_pendingEntry = Sides.Sell;
				_shouldOpenShort = false;
				return false;
			}

			if (ExecuteOpenShort())
			{
				_shouldOpenShort = false;
				return true;
			}
		}

		return false;
	}

	private bool ExecuteOpenLong()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return false;

		if (_longEntries.Count >= MaxOrders)
			return false;

		var volume = AdjustVolume(OrderVolume);
		if (volume <= 0m)
			return false;

		return BuyMarket(volume) != null;
	}

	private bool ExecuteOpenShort()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return false;

		if (_shortEntries.Count >= MaxOrders)
			return false;

		var volume = AdjustVolume(OrderVolume);
		if (volume <= 0m)
			return false;

		return SellMarket(volume) != null;
	}

	private bool ExecuteCloseLong()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return false;

		var volume = AdjustVolume(GetTotalVolume(_longEntries));
		if (volume <= 0m)
		{
			_shouldCloseLong = false;
			return false;
		}

		if (SellMarket(volume) != null)
		{
			_shouldCloseLong = false;
			return true;
		}

		return false;
	}

	private bool ExecuteCloseShort()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return false;

		var volume = AdjustVolume(GetTotalVolume(_shortEntries));
		if (volume <= 0m)
		{
			_shouldCloseShort = false;
			return false;
		}

		if (BuyMarket(volume) != null)
		{
			_shouldCloseShort = false;
			return true;
		}

		return false;
	}
	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var security = Security;
		if (security == null)
			return volume;

		if (security.VolumeStep is { } step && step > 0m)
		{
			var steps = Math.Round(volume / step, MidpointRounding.AwayFromZero);
			if (steps < 1m)
				steps = 1m;

			volume = steps * step;
		}

		if (security.MinVolume is { } min && volume < min)
			volume = min;

		if (security.MaxVolume is { } max && volume > max)
			volume = max;

		return volume;
	}

	private static decimal GetTotalVolume(List<PositionEntry> entries)
	{
		var total = 0m;
		foreach (var entry in entries)
			total += entry.Volume;
		return total;
	}

	private decimal CalculateOpenProfit(decimal price)
	{
		var profit = 0m;

		foreach (var entry in _longEntries)
			profit += (price - entry.Price) * entry.Volume;

		foreach (var entry in _shortEntries)
			profit += (entry.Price - price) * entry.Volume;

		return profit;
	}

	private void EnsureBounds(decimal price)
	{
		if (_hasBounds)
			return;

		if (UseInitialValues && _longEntries.Count == 0 && _shortEntries.Count == 0 && _realizedProfit == 0m && _maxPrice == 0m && _minPrice == 0m)
		{
			_maxPrice = InitialMax;
			_minPrice = InitialMin;

			if (_maxPrice == 0m && _minPrice == 0m)
			{
				_maxPrice = price;
				_minPrice = price;
			}
		}
		else
		{
			_maxPrice = price;
			_minPrice = price;
		}

		_hasBounds = true;
	}
	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade.Order;
		var info = trade.Trade;

		if (order == null || info == null)
			return;

		var volume = info.Volume;
		var price = info.Price;

		if (volume <= 0m)
			return;

		if (order.Direction == Sides.Buy)
			ProcessBuyTrade(volume, price);
		else
			ProcessSellTrade(volume, price);

		if (_longEntries.Count == 0 && _shortEntries.Count == 0)
		{
			_hasBounds = false;

			if (_shouldCloseAll)
			{
				_shouldCloseAll = false;
				_isForward = true;
				_lastSide = null;
				_pendingEntry = null;
				_realizedProfit = 0m;
			}
		}
	}

	private void ProcessBuyTrade(decimal volume, decimal price)
	{
		var remaining = volume;

		for (var i = 0; i < _shortEntries.Count && remaining > 0m;)
		{
			var entry = _shortEntries[i];
			var portion = Math.Min(entry.Volume, remaining);
			_realizedProfit += (entry.Price - price) * portion;
			entry.Volume -= portion;
			remaining -= portion;

			if (entry.Volume <= 0m)
			{
				_shortEntries.RemoveAt(i);
				continue;
			}

			_shortEntries[i] = entry;
			break;
		}

		if (remaining > 0m)
			_longEntries.Add(new PositionEntry(price, remaining));
	}

	private void ProcessSellTrade(decimal volume, decimal price)
	{
		var remaining = volume;

		for (var i = 0; i < _longEntries.Count && remaining > 0m;)
		{
			var entry = _longEntries[i];
			var portion = Math.Min(entry.Volume, remaining);
			_realizedProfit += (price - entry.Price) * portion;
			entry.Volume -= portion;
			remaining -= portion;

			if (entry.Volume <= 0m)
			{
				_longEntries.RemoveAt(i);
				continue;
			}

			_longEntries[i] = entry;
			break;
		}

		if (remaining > 0m)
			_shortEntries.Add(new PositionEntry(price, remaining));
	}

	private sealed class PositionEntry
	{
		public PositionEntry(decimal price, decimal volume)
		{
			Price = price;
			Volume = volume;
		}

		public decimal Price { get; }
		public decimal Volume { get; set; }
	}
}
