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

using System.Reflection;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert advisor 10p3v003 (10point3.mq4).
/// Implements a MACD based trigger with grid-style position scaling, martingale sizing,
/// optional equity based sizing, and trailing/stop-loss management for each entry.
/// </summary>
public class TenPointThreeMacdGridStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _initialStopPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _lotMultiplier;
	private readonly StrategyParam<decimal> _gridStepPips;
	private readonly StrategyParam<int> _ordersToProtect;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<int> _accountType;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<bool> _reverseSignal;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _signalShift;
	private readonly StrategyParam<decimal> _tradingRangePips;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _stopHour;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private readonly List<decimal> _macdMainHistory = new();
	private readonly List<decimal> _macdSignalHistory = new();
	private readonly List<GridEntry> _entries = new();

	private decimal _pipSize;
	private Sides? _gridDirection;
	private decimal _lastEntryPrice;
	private DateTimeOffset? _lastSignalBar;
	private decimal _initialEquity;
	private Sides? _pendingEntrySide;
	private int? _pendingEntryExistingCount;

	private struct GridEntry
	{
		public Sides Side { get; set; }
		public decimal Volume { get; set; }
		public decimal EntryPrice { get; set; }
		public decimal? TakeProfitPrice { get; set; }
		public decimal? StopLossPrice { get; set; }
		public decimal? TrailExtreme { get; set; }
		public bool PendingClose { get; set; }
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TenPointThreeMacdGridStrategy"/>.
	/// </summary>
	public TenPointThreeMacdGridStrategy()
	{
		_takeProfitPips = Param(nameof(TakeProfitPips), 45m)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Profit target for each position leg", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10m, 90m, 5m);

		_initialStopPips = Param(nameof(InitialStopPips), 0m)
		.SetNotNegative()
		.SetDisplay("Initial Stop (pips)", "Base stop distance expanded by remaining grid slots", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 200m, 20m);

		_trailingStopPips = Param(nameof(TrailingStopPips), 45m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance activated after sufficient profit", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 90m, 5m);

		_maxTrades = Param(nameof(MaxTrades), 10)
		.SetGreaterThanZero()
		.SetDisplay("Max Trades", "Maximum number of simultaneous martingale entries", "Money Management");

		_lotMultiplier = Param(nameof(LotMultiplier), 2m)
		.SetGreaterThanOrEqual(1m)
		.SetDisplay("Lot Multiplier", "Scaling factor applied to each additional grid entry", "Money Management")
		.SetCanOptimize(true)
		.SetOptimize(1m, 3m, 0.25m);

		_gridStepPips = Param(nameof(GridStepPips), 30m)
		.SetGreaterThanZero()
		.SetDisplay("Grid Step (pips)", "Distance between consecutive martingale entries", "Money Management")
		.SetCanOptimize(true)
		.SetOptimize(10m, 80m, 5m);

		_ordersToProtect = Param(nameof(OrdersToProtect), 5)
		.SetGreaterThanZero()
		.SetDisplay("Orders To Protect", "Minimum open trades required before profit protection triggers", "Money Management");

		_useMoneyManagement = Param(nameof(UseMoneyManagement), false)
		.SetDisplay("Use Equity Sizing", "Calculate base volume from equity using risk percentage", "Money Management");

		_accountType = Param(nameof(AccountType), 2)
		.SetRange(0, 3)
		.SetDisplay("Account Type", "0=Standard, 1=Normal, 2=Nano (affects risk based sizing)", "Money Management");

		_riskPercent = Param(nameof(RiskPercent), 0.5m)
		.SetNotNegative()
		.SetDisplay("Risk %", "Percentage of equity used when money management is enabled", "Money Management")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 5m, 0.1m);

		_reverseSignal = Param(nameof(ReverseSignal), false)
		.SetDisplay("Reverse Signal", "Invert MACD based trade direction", "General");

		_fastEmaLength = Param(nameof(FastEmaLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA", "Fast EMA length for MACD", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);

		_slowEmaLength = Param(nameof(SlowEmaLength), 26)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA", "Slow EMA length for MACD", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(20, 40, 1);

		_signalLength = Param(nameof(SignalLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("Signal SMA", "Signal line length for MACD", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(5, 15, 1);

		_signalShift = Param(nameof(SignalShift), 1)
		.SetGreaterThanZero()
		.SetDisplay("Signal Shift", "Number of closed bars back used for the MACD signal", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(1, 3, 1);

		_tradingRangePips = Param(nameof(TradingRangePips), 0m)
		.SetNotNegative()
		.SetDisplay("Trading Range (pips)", "MACD signal bounds required before accepting cross", "Indicator");

		_useTimeFilter = Param(nameof(UseTimeFilter), false)
		.SetDisplay("Use Time Filter", "Block new trades within the danger hour window", "Sessions");

		_stopHour = Param(nameof(StopHour), 18)
		.SetRange(0, 23)
		.SetDisplay("Stop Hour", "Lower bound of blocked trading window (exclusive)", "Sessions");

		_startHour = Param(nameof(StartHour), 19)
		.SetRange(0, 23)
		.SetDisplay("Start Hour", "Upper bound of blocked trading window (exclusive)", "Sessions");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle type for MACD calculations", "General");
	}

	/// <summary>
	/// Profit target in pips applied per position leg.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Initial stop distance in pips.
	/// </summary>
	public decimal InitialStopPips
	{
		get => _initialStopPips.Value;
		set => _initialStopPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Maximum number of martingale entries.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Scaling factor applied after each additional grid entry.
	/// </summary>
	public decimal LotMultiplier
	{
		get => _lotMultiplier.Value;
		set => _lotMultiplier.Value = value;
	}

	/// <summary>
	/// Grid spacing in pips between consecutive entries.
	/// </summary>
	public decimal GridStepPips
	{
		get => _gridStepPips.Value;
		set => _gridStepPips.Value = value;
	}

	/// <summary>
	/// Number of open trades required before profit protection applies.
	/// </summary>
	public int OrdersToProtect
	{
		get => _ordersToProtect.Value;
		set => _ordersToProtect.Value = value;
	}

	/// <summary>
	/// Enables equity based position sizing.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Account model used by equity sizing (0: Standard, 1: Normal, 2: Nano).
	/// </summary>
	public int AccountType
	{
		get => _accountType.Value;
		set => _accountType.Value = value;
	}

	/// <summary>
	/// Percentage of equity risked for money management sizing.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Inverts the MACD trade direction.
	/// </summary>
	public bool ReverseSignal
	{
		get => _reverseSignal.Value;
		set => _reverseSignal.Value = value;
	}

	/// <summary>
	/// Fast EMA length for MACD.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length for MACD.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// Signal SMA length for MACD.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Number of closed bars back used for MACD signal evaluation.
	/// </summary>
	public int SignalShift
	{
		get => _signalShift.Value;
		set => _signalShift.Value = value;
	}

	/// <summary>
	/// Required MACD signal range before trading.
	/// </summary>
	public decimal TradingRangePips
	{
		get => _tradingRangePips.Value;
		set => _tradingRangePips.Value = value;
	}

	/// <summary>
	/// Enables the danger hour trading filter.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Lower bound of the blocked trading window (exclusive).
	/// </summary>
	public int StopHour
	{
		get => _stopHour.Value;
		set => _stopHour.Value = value;
	}

	/// <summary>
	/// Upper bound of the blocked trading window (exclusive).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
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

		_macdMainHistory.Clear();
		_macdSignalHistory.Clear();
		_entries.Clear();
		_gridDirection = null;
		_lastEntryPrice = 0m;
		_lastSignalBar = null;
		_pipSize = 0m;
		_initialEquity = 0m;
		_pendingEntrySide = null;
		_pendingEntryExistingCount = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_initialEquity = GetPortfolioValue();

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastEmaLength },
				LongMa = { Length = SlowEmaLength },
			},
			SignalMa = { Length = SignalLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_macd, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_pipSize <= 0m)
		_pipSize = CalculatePipSize();

		ManageActiveEntries(candle);

		if (_entries.Count >= OrdersToProtect && TryTriggerProfitProtection(candle))
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!TryExtractMacd(macdValue, out var macdMain, out var macdSignal))
		return;

		StoreMacdValues(macdMain, macdSignal);

		var shift = Math.Max(1, SignalShift);
		if (_macdMainHistory.Count < shift + 1)
		return;

		var currIndex = _macdMainHistory.Count - shift;
		var prevIndex = currIndex - 1;

		var macdMainCurr = _macdMainHistory[currIndex];
		var macdSignalCurr = _macdSignalHistory[currIndex];
		var macdMainPrev = _macdMainHistory[prevIndex];
		var macdSignalPrev = _macdSignalHistory[prevIndex];

		var tradingRange = TradingRangePips * _pipSize;
		var buyRange = -tradingRange;
		var sellRange = tradingRange;

		Sides? desiredSide = null;

		if (macdMainCurr > macdSignalCurr &&
		macdMainPrev < macdSignalPrev &&
		macdSignalPrev < buyRange &&
		macdMainCurr < 0m &&
		_lastSignalBar != candle.OpenTime)
		{
			desiredSide = Sides.Buy;
		}
		else if (macdMainCurr < macdSignalCurr &&
		macdMainPrev > macdSignalPrev &&
		macdSignalPrev > sellRange &&
		macdMainCurr > 0m &&
		_lastSignalBar != candle.OpenTime)
		{
			desiredSide = Sides.Sell;
		}

		if (desiredSide == null)
		return;

		_lastSignalBar = candle.OpenTime;

		if (ReverseSignal)
		desiredSide = desiredSide == Sides.Buy ? Sides.Sell : Sides.Buy;

		if (_entries.Count == 0 && UseTimeFilter && IsWithinBlockedWindow(candle.OpenTime))
		return;

		if (_entries.Count > 0 && _gridDirection.HasValue && _gridDirection != desiredSide)
		return;

		if (_entries.Count >= MaxTrades)
		return;

		var gridDistance = GridStepPips * _pipSize;
		if (_entries.Count > 0 && gridDistance > 0m)
		{
			if (_gridDirection == Sides.Buy && (_lastEntryPrice - candle.ClosePrice) < gridDistance)
			return;

			if (_gridDirection == Sides.Sell && (candle.ClosePrice - _lastEntryPrice) < gridDistance)
			return;
		}

		var volume = CalculateOrderVolume(_entries.Count);
		if (volume <= 0m)
		return;

		_pendingEntrySide = desiredSide;
		_pendingEntryExistingCount = _entries.Count;

		if (desiredSide == Sides.Buy)
		BuyMarket(volume);
		else
		SellMarket(volume);
	}

	private void ManageActiveEntries(ICandleMessage candle)
	{
		if (_entries.Count == 0)
		return;

		var price = candle.ClosePrice;
		var trailingDistance = TrailingStopPips * _pipSize;
		var activationDistance = trailingDistance + GridStepPips * _pipSize;

		for (var i = _entries.Count - 1; i >= 0; i--)
		{
			var entry = _entries[i];
			if (entry.PendingClose)
			continue;

			if (entry.TakeProfitPrice is decimal takeProfit)
			{
				if ((entry.Side == Sides.Buy && price >= takeProfit) || (entry.Side == Sides.Sell && price <= takeProfit))
				{
					CloseEntry(i);
					continue;
				}
			}

			if (entry.StopLossPrice is decimal stopLoss)
			{
				if ((entry.Side == Sides.Buy && price <= stopLoss) || (entry.Side == Sides.Sell && price >= stopLoss))
				{
					CloseEntry(i);
					continue;
				}
			}

			if (TrailingStopPips > 0m && activationDistance > 0m)
			{
				if (entry.Side == Sides.Buy)
				{
					var profitDistance = price - entry.EntryPrice;
					if (profitDistance >= activationDistance)
					{
						var extreme = entry.TrailExtreme ?? entry.EntryPrice;
						extreme = Math.Max(extreme, price);
						entry.TrailExtreme = extreme;
						_entries[i] = entry;

						if (extreme - price >= trailingDistance)
						CloseEntry(i);
					}
				}
				else
				{
					var profitDistance = entry.EntryPrice - price;
					if (profitDistance >= activationDistance)
					{
						var extreme = entry.TrailExtreme ?? entry.EntryPrice;
						extreme = Math.Min(extreme, price);
						entry.TrailExtreme = extreme;
						_entries[i] = entry;

						if (price - extreme >= trailingDistance)
						CloseEntry(i);
					}
				}
			}
		}
	}

	private bool TryTriggerProfitProtection(ICandleMessage candle)
	{
		var floating = GetFloatingProfit(candle.ClosePrice);
		if (floating <= 0m)
		return false;

		decimal threshold;
		if (UseMoneyManagement)
		{
			threshold = GetPortfolioValue() * (RiskPercent / 100m);
		}
		else
		{
			var baseVolume = CalculateBaseVolume();
			if (baseVolume <= 0m)
			return false;

			threshold = baseVolume * (GetContractSize() / 100m);
		}

		if (threshold <= 0m)
		return false;

		if (floating >= threshold)
		{
			CloseEntry(_entries.Count - 1);
			return true;
		}

		return false;
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

		if (entry.Side == Sides.Buy)
		SellMarket(volume);
		else
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

		if (_gridDirection == null)
		{
			AddEntry(side, volume, price, _pendingEntryExistingCount ?? 0);
			_pendingEntrySide = null;
			_pendingEntryExistingCount = null;
			return;
		}

		if (side == _gridDirection)
		{
			AddEntry(side, volume, price, _pendingEntryExistingCount ?? _entries.Count);
			_pendingEntrySide = null;
			_pendingEntryExistingCount = null;
		}
		else
		{
			ReduceEntries(volume);
		}
	}

	private void AddEntry(Sides side, decimal volume, decimal price, int existingCount)
	{
		if (_pendingEntrySide.HasValue && _pendingEntrySide != side)
		return;

		var normalizedVolume = NormalizeVolume(volume);
		if (normalizedVolume <= 0m)
		return;

		var entry = new GridEntry
		{
			Side = side,
			Volume = normalizedVolume,
			EntryPrice = price,
			TakeProfitPrice = CalculateTakeProfitPrice(side, price),
			StopLossPrice = CalculateInitialStop(side, price, existingCount),
			TrailExtreme = null,
			PendingClose = false,
		};

		_entries.Add(entry);
		_gridDirection = side;
		_lastEntryPrice = price;
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
			{
				_entries.RemoveAt(i);
			}
			else
			{
				_entries[i] = entry;
			}
		}

		if (_entries.Count == 0)
		{
			_gridDirection = null;
			_lastEntryPrice = 0m;
		}
		else
		{
			_lastEntryPrice = _entries[^1].EntryPrice;
		}
	}

	private decimal? CalculateTakeProfitPrice(Sides side, decimal price)
	{
		if (TakeProfitPips <= 0m || _pipSize <= 0m)
		return null;

		var distance = TakeProfitPips * _pipSize;
		return side == Sides.Buy ? price + distance : price - distance;
	}

	private decimal? CalculateInitialStop(Sides side, decimal price, int existingCount)
	{
		if (InitialStopPips <= 0m || _pipSize <= 0m)
		return null;

		var buffer = InitialStopPips + (MaxTrades - existingCount) * GridStepPips;
		if (buffer <= 0m)
		return null;

		var distance = buffer * _pipSize;
		return side == Sides.Buy ? price - distance : price + distance;
	}

	private bool TryExtractMacd(IIndicatorValue macdValue, out decimal macdMain, out decimal macdSignal)
	{
		macdMain = 0m;
		macdSignal = 0m;

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue typed)
		return false;

		if (typed.Macd is not decimal main || typed.Signal is not decimal signal)
		return false;

		macdMain = main;
		macdSignal = signal;
		return true;
	}

	private void StoreMacdValues(decimal macdMain, decimal macdSignal)
	{
		_macdMainHistory.Add(macdMain);
		_macdSignalHistory.Add(macdSignal);

		var maxSize = Math.Max(3, SignalShift + 2);
		if (_macdMainHistory.Count > maxSize)
		{
			var removeCount = _macdMainHistory.Count - maxSize;
			_macdMainHistory.RemoveRange(0, removeCount);
			_macdSignalHistory.RemoveRange(0, removeCount);
		}
	}

	private bool IsWithinBlockedWindow(DateTimeOffset time)
	{
		var hour = time.Hour;
		return hour > StopHour && hour < StartHour;
	}

	private decimal CalculateOrderVolume(int existingOrders)
	{
		var baseVolume = CalculateBaseVolume();
		if (baseVolume <= 0m)
		return 0m;

		var volume = baseVolume;

		for (var i = 0; i < existingOrders; i++)
		{
			var factor = MaxTrades > 12 ? 1.5m : LotMultiplier;
			volume *= factor;
		}

		if (volume > 100m)
		volume = 100m;

		return NormalizeVolume(volume);
	}

	private decimal CalculateBaseVolume()
	{
		decimal volume;

		if (UseMoneyManagement)
		{
			var equity = GetPortfolioValue();
			var riskFraction = RiskPercent / 100m;

			switch (AccountType)
			{
			case 0:
				volume = Math.Ceiling((riskFraction * equity) / 10000m * 10m) / 10m;
				break;
			case 1:
				volume = (riskFraction * equity) / 100000m;
				break;
			case 2:
				volume = (riskFraction * equity) / 1000m;
				break;
			default:
				volume = Math.Ceiling((riskFraction * equity) / 10000m * 10m) / 10m;
				break;
			}
		}
		else
		{
			volume = Volume;
		}

		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var security = Security;
		if (security == null)
		return Math.Round(volume, 2, MidpointRounding.AwayFromZero);

		var maxVolume = security.MaxVolume ?? 0m;
		var minVolume = security.MinVolume ?? 0m;
		var step = security.VolumeStep ?? 0m;

		if (maxVolume > 0m && volume > maxVolume)
		volume = maxVolume;

		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
			volume = steps * step;
		}

		if (minVolume > 0m && volume < minVolume)
		return 0m;

		return volume;
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

	private decimal GetFloatingProfit(decimal price)
	{
		decimal total = 0m;
		foreach (var entry in _entries)
		{
			var diff = entry.Side == Sides.Buy ? price - entry.EntryPrice : entry.EntryPrice - price;
			total += ConvertPriceToMoney(diff, entry.Volume);
		}

		return total;
	}

	private decimal ConvertPriceToMoney(decimal priceDiff, decimal volume)
	{
		var security = Security;
		if (security == null)
		return priceDiff * volume;

		var priceStep = security.PriceStep ?? security.Step ?? 0m;
		var stepPrice = security.StepPrice ?? 0m;

		if (priceStep <= 0m || stepPrice <= 0m)
		return priceDiff * volume;

		return priceDiff / priceStep * stepPrice * volume;
	}

	private decimal GetPortfolioValue()
	{
		var portfolio = Portfolio;
		var value = portfolio?.CurrentValue ?? portfolio?.BeginValue ?? 0m;
		if (value == 0m)
		value = _initialEquity;
		return value;
	}

	private decimal GetContractSize()
	{
		var security = Security;
		if (security == null)
		return 100000m;

		var type = security.GetType();

		decimal TryGetProperty(string name)
		{
			var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
			if (prop == null)
			return 0m;

			var value = prop.GetValue(security);
			return value switch
			{
				decimal dec => dec,
				double dbl => (decimal)dbl,
				int intValue => intValue,
				long longValue => longValue,
				_ => 0m,
			};
		}

		var contractSize = TryGetProperty("ContractSize");
		if (contractSize <= 0m)
		contractSize = TryGetProperty("LotSize");

		return contractSize > 0m ? contractSize : 100000m;
	}
}
