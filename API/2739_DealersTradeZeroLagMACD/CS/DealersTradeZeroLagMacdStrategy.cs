using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid strategy based on zero lag MACD slope with adaptive spacing and money management.
/// </summary>
public class DealersTradeZeroLagMacdStrategy : Strategy
{
	private sealed class PositionEntry
	{
		public PositionEntry(Sides side, decimal volume)
		{
			Side = side;
			Volume = volume;
		}

		public Sides Side { get; }
		public decimal Volume { get; set; }
		public decimal EntryPrice { get; set; }
		public decimal? StopLoss { get; set; }
		public decimal? TakeProfit { get; set; }
		public decimal TrailingDistance { get; set; }
		public decimal TrailingStep { get; set; }
		public decimal? TrailingStop { get; set; }
		public decimal PendingCloseVolume { get; set; }
	}

	private sealed class PendingEntry
	{
		public PendingEntry(Sides side, decimal volume)
		{
			Side = side;
			Volume = volume;
		}

		public Sides Side { get; }
		public decimal Volume { get; }
		public decimal StopLossDistance { get; set; }
		public decimal TakeProfitDistance { get; set; }
		public decimal TrailingDistance { get; set; }
		public decimal TrailingStep { get; set; }
		public decimal FilledVolume { get; set; }
		public PositionEntry? Entry { get; set; }
	}

	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<int> _intervalPips;
	private readonly StrategyParam<decimal> _intervalCoefficient;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<decimal> _takeProfitCoefficient;
	private readonly StrategyParam<decimal> _secureProfit;
	private readonly StrategyParam<bool> _accountProtection;
	private readonly StrategyParam<int> _positionsForProtection;
	private readonly StrategyParam<bool> _reverseCondition;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _lotMultiplier;
	private readonly StrategyParam<decimal> _minimumBalance;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<PositionEntry> _longEntries = new();
	private readonly List<PositionEntry> _shortEntries = new();

	private ZeroLagExponentialMovingAverage _fastZlema = null!;
	private ZeroLagExponentialMovingAverage _slowZlema = null!;
	private ZeroLagExponentialMovingAverage _signalZlema = null!;

	private PendingEntry? _pendingBuyEntry;
	private PendingEntry? _pendingSellEntry;

	private decimal _pipSize;
	private decimal _lastLongEntryPrice;
	private decimal _lastShortEntryPrice;
	private decimal _previousMacd;
	private bool _hasPreviousMacd;

	/// <summary>
	/// Base order volume. Set to zero to enable risk-based sizing.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Risk percent used when <see cref="BaseVolume"/> is zero.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously open entries.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Initial spacing between entries in pips.
	/// </summary>
	public int IntervalPips
	{
		get => _intervalPips.Value;
		set => _intervalPips.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the spacing after each additional entry.
	/// </summary>
	public decimal IntervalCoefficient
	{
		get => _intervalCoefficient.Value;
		set => _intervalCoefficient.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum price advance before the trailing stop starts to follow the price.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the take profit distance for each additional entry.
	/// </summary>
	public decimal TakeProfitCoefficient
	{
		get => _takeProfitCoefficient.Value;
		set => _takeProfitCoefficient.Value = value;
	}

	/// <summary>
	/// Target profit used when account protection is enabled.
	/// </summary>
	public decimal SecureProfit
	{
		get => _secureProfit.Value;
		set => _secureProfit.Value = value;
	}

	/// <summary>
	/// Enables closing the most profitable position once cumulative profit reaches <see cref="SecureProfit"/>.
	/// </summary>
	public bool AccountProtection
	{
		get => _accountProtection.Value;
		set => _accountProtection.Value = value;
	}

	/// <summary>
	/// Minimum number of entries required before account protection can trigger.
	/// </summary>
	public int PositionsForProtection
	{
		get => _positionsForProtection.Value;
		set => _positionsForProtection.Value = value;
	}

	/// <summary>
	/// Reverses the MACD slope interpretation when set to true.
	/// </summary>
	public bool ReverseCondition
	{
		get => _reverseCondition.Value;
		set => _reverseCondition.Value = value;
	}

	/// <summary>
	/// Fast length of the zero lag EMA.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow length of the zero lag EMA.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Signal length used for smoothing MACD line.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Maximum allowed volume for a single entry.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the base volume when stacking positions.
	/// </summary>
	public decimal LotMultiplier
	{
		get => _lotMultiplier.Value;
		set => _lotMultiplier.Value = value;
	}

	/// <summary>
	/// Minimum portfolio balance required to keep trading.
	/// </summary>
	public decimal MinimumBalance
	{
		get => _minimumBalance.Value;
		set => _minimumBalance.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DealersTradeZeroLagMacdStrategy"/> class.
	/// </summary>
	public DealersTradeZeroLagMacdStrategy()
	{
		_baseVolume = Param(nameof(BaseVolume), 0.1m)
		.SetDisplay("Base Volume", "Initial order volume", "Trading")
		.SetCanOptimize(true);

		_riskPercent = Param(nameof(RiskPercent), 5m)
		.SetDisplay("Risk Percent", "Risk per trade when base volume is zero", "Trading")
		.SetCanOptimize(true);

		_maxPositions = Param(nameof(MaxPositions), 5)
		.SetDisplay("Max Positions", "Maximum simultaneous entries", "Risk")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_intervalPips = Param(nameof(IntervalPips), 15)
		.SetDisplay("Interval (pips)", "Base spacing between entries", "Grid")
		.SetGreaterThanOrEqualZero()
		.SetCanOptimize(true);

		_intervalCoefficient = Param(nameof(IntervalCoefficient), 1.2m)
		.SetDisplay("Interval Coefficient", "Spacing multiplier for additional entries", "Grid")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 0)
		.SetDisplay("Stop Loss (pips)", "Distance to protective stop", "Risk")
		.SetGreaterThanOrEqualZero();

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
		.SetDisplay("Take Profit (pips)", "Base take profit distance", "Risk")
		.SetGreaterThanOrEqualZero()
		.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 0)
		.SetDisplay("Trailing Stop (pips)", "Trailing distance", "Risk")
		.SetGreaterThanOrEqualZero();

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
		.SetDisplay("Trailing Step (pips)", "Extra move required to tighten trail", "Risk")
		.SetGreaterThanOrEqualZero();

		_takeProfitCoefficient = Param(nameof(TakeProfitCoefficient), 1.2m)
		.SetDisplay("TP Coefficient", "Take profit multiplier per entry", "Risk")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_secureProfit = Param(nameof(SecureProfit), 300m)
		.SetDisplay("Secure Profit", "Cumulative profit to trigger protection", "Risk")
		.SetGreaterThanOrEqualZero();

		_accountProtection = Param(nameof(AccountProtection), true)
		.SetDisplay("Account Protection", "Enable profit locking", "Risk");

		_positionsForProtection = Param(nameof(PositionsForProtection), 3)
		.SetDisplay("Positions For Protection", "Entries required for protection", "Risk")
		.SetGreaterThanOrEqualZero();

		_reverseCondition = Param(nameof(ReverseCondition), false)
		.SetDisplay("Reverse Condition", "Invert MACD slope logic", "General");

		_fastLength = Param(nameof(FastLength), 14)
		.SetDisplay("Fast Length", "Fast ZLEMA length", "Indicators")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_slowLength = Param(nameof(SlowLength), 26)
		.SetDisplay("Slow Length", "Slow ZLEMA length", "Indicators")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_signalLength = Param(nameof(SignalLength), 9)
		.SetDisplay("Signal Length", "Signal smoothing length", "Indicators")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_maxVolume = Param(nameof(MaxVolume), 5m)
		.SetDisplay("Max Volume", "Maximum volume per entry", "Trading")
		.SetGreaterThanZero();

		_lotMultiplier = Param(nameof(LotMultiplier), 1.6m)
		.SetDisplay("Lot Multiplier", "Multiplier applied to each new entry", "Trading")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_minimumBalance = Param(nameof(MinimumBalance), 1000m)
		.SetDisplay("Minimum Balance", "Stop trading below this balance", "Risk")
		.SetGreaterThanOrEqualZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for calculations", "General");
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

		_longEntries.Clear();
		_shortEntries.Clear();
		_pendingBuyEntry = null;
		_pendingSellEntry = null;
		_lastLongEntryPrice = 0m;
		_lastShortEntryPrice = 0m;
		_previousMacd = 0m;
		_hasPreviousMacd = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		_fastZlema = new ZeroLagExponentialMovingAverage { Length = FastLength };
		_slowZlema = new ZeroLagExponentialMovingAverage { Length = SlowLength };
		_signalZlema = new ZeroLagExponentialMovingAverage { Length = SignalLength };

		var decimals = Security?.Decimals ?? 0;
		var step = Security?.PriceStep ?? 0.0001m;
		var factor = decimals == 3 || decimals == 5 ? 10m : 1m;
		_pipSize = step * factor;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_fastZlema, _slowZlema, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
			{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastZlema);
			DrawIndicator(area, _slowZlema);
			DrawOwnTrades(area);
		}

		base.OnStarted(time);
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var balance = Portfolio?.CurrentValue;
		if (balance.HasValue && balance.Value < MinimumBalance)
			{
			Stop();
			return;
		}

		var macd = fast - slow;
		_signalZlema.Process(macd, candle.CloseTime, true);

		if (!_fastZlema.IsFormed || !_slowZlema.IsFormed || !_signalZlema.IsFormed)
			{
			_previousMacd = macd;
			_hasPreviousMacd = true;
			return;
		}

		if (!_hasPreviousMacd)
			{
			_previousMacd = macd;
			_hasPreviousMacd = true;
			return;
		}

		var direction = 3;

		if (macd > _previousMacd && macd != 0m && _previousMacd != 0m)
			direction = 2;
		else if (macd < _previousMacd && macd != 0m && _previousMacd != 0m)
			direction = 1;

		if (ReverseCondition)
			{
			if (direction == 1)
				direction = 2;
			else if (direction == 2)
				direction = 1;
		}

		_previousMacd = macd;

		var openPositions = _longEntries.Count + _shortEntries.Count;
		var continueOpening = openPositions <= MaxPositions;

		if (direction != 3 && openPositions > MaxPositions)
			{
			CloseMinimumProfit(candle.ClosePrice);
			return;
		}

		var closedThisBar = ManagePositions(candle);
		if (closedThisBar)
			return;

		var totalProfit = GetTotalProfit(candle.ClosePrice);
		if (AccountProtection && openPositions > PositionsForProtection && totalProfit >= SecureProfit)
			{
			CloseMaximumProfit(candle.ClosePrice);
			return;
		}

		if (!continueOpening)
			return;

		if (direction == 2)
			TryOpenLong(candle, openPositions);
		else if (direction == 1)
			TryOpenShort(candle, openPositions);
	}

	private void TryOpenLong(ICandleMessage candle, int openPositions)
	{
		var interval = GetIntervalDistance(openPositions);
		var canOpen = _longEntries.Count == 0 || _lastLongEntryPrice - candle.ClosePrice >= interval;

		if (!canOpen)
			return;

		var stopDistance = StopLossPips > 0 ? StopLossPips * _pipSize : 0m;
		var takeDistance = TakeProfitPips > 0 ? TakeProfitPips * _pipSize : 0m;
		if (takeDistance > 0m)
			{
			var tpMultiplier = Pow(TakeProfitCoefficient, openPositions + 1);
			takeDistance *= tpMultiplier;
		}

		var trailingDistance = TrailingStopPips > 0 ? TrailingStopPips * _pipSize : 0m;
		var trailingStep = TrailingStepPips > 0 ? TrailingStepPips * _pipSize : 0m;

		var lotMultiplier = openPositions == 0 ? 1m : Pow(LotMultiplier, openPositions + 1);
		var volume = CalculateEntryVolume(stopDistance, lotMultiplier);

		if (volume <= 0m)
			return;

		var pending = new PendingEntry(Sides.Buy, volume)
		{
			StopLossDistance = stopDistance,
			TakeProfitDistance = takeDistance,
			TrailingDistance = trailingDistance,
			TrailingStep = trailingStep
		};

		_pendingBuyEntry = pending;
		BuyMarket(volume);
	}

	private void TryOpenShort(ICandleMessage candle, int openPositions)
	{
		var interval = GetIntervalDistance(openPositions);
		var canOpen = _shortEntries.Count == 0 || candle.ClosePrice - _lastShortEntryPrice >= interval;

		if (!canOpen)
			return;

		var stopDistance = StopLossPips > 0 ? StopLossPips * _pipSize : 0m;
		var takeDistance = TakeProfitPips > 0 ? TakeProfitPips * _pipSize : 0m;
		if (takeDistance > 0m)
			{
			var tpMultiplier = Pow(TakeProfitCoefficient, openPositions + 1);
			takeDistance *= tpMultiplier;
		}

		var trailingDistance = TrailingStopPips > 0 ? TrailingStopPips * _pipSize : 0m;
		var trailingStep = TrailingStepPips > 0 ? TrailingStepPips * _pipSize : 0m;

		var lotMultiplier = openPositions == 0 ? 1m : Pow(LotMultiplier, openPositions + 1);
		var volume = CalculateEntryVolume(stopDistance, lotMultiplier);

		if (volume <= 0m)
			return;

		var pending = new PendingEntry(Sides.Sell, volume)
		{
			StopLossDistance = stopDistance,
			TakeProfitDistance = takeDistance,
			TrailingDistance = trailingDistance,
			TrailingStep = trailingStep
		};

		_pendingSellEntry = pending;
		SellMarket(volume);
	}

	private bool ManagePositions(ICandleMessage candle)
	{
		var closed = false;

		if (ManageEntries(_longEntries, candle, true))
			closed = true;

		if (ManageEntries(_shortEntries, candle, false))
			closed = true;

		return closed;
	}

	private bool ManageEntries(List<PositionEntry> entries, ICandleMessage candle, bool isLong)
	{
		var closed = false;

		foreach (var entry in entries)
			{
			if (entry.PendingCloseVolume > 0m)
				continue;

			if (isLong)
				{
				if (entry.StopLoss.HasValue && candle.LowPrice <= entry.StopLoss.Value)
					{
					SendCloseOrder(entry);
					closed = true;
					continue;
				}

				if (entry.TakeProfit.HasValue && candle.HighPrice >= entry.TakeProfit.Value)
					{
					SendCloseOrder(entry);
					closed = true;
					continue;
				}

				if (entry.TrailingDistance > 0m)
					{
					var profit = candle.ClosePrice - entry.EntryPrice;
					if (profit > entry.TrailingDistance + entry.TrailingStep)
						{
						var newStop = candle.ClosePrice - entry.TrailingDistance;
						if (!entry.TrailingStop.HasValue || entry.TrailingStop.Value < newStop)
							entry.TrailingStop = newStop;
					}

					if (entry.TrailingStop.HasValue && candle.LowPrice <= entry.TrailingStop.Value)
						{
						SendCloseOrder(entry);
						closed = true;
					}
				}
			}
			else
				{
				if (entry.StopLoss.HasValue && candle.HighPrice >= entry.StopLoss.Value)
					{
					SendCloseOrder(entry);
					closed = true;
					continue;
				}

				if (entry.TakeProfit.HasValue && candle.LowPrice <= entry.TakeProfit.Value)
					{
					SendCloseOrder(entry);
					closed = true;
					continue;
				}

				if (entry.TrailingDistance > 0m)
					{
					var profit = entry.EntryPrice - candle.ClosePrice;
					if (profit > entry.TrailingDistance + entry.TrailingStep)
						{
						var newStop = candle.ClosePrice + entry.TrailingDistance;
						if (!entry.TrailingStop.HasValue || entry.TrailingStop.Value > newStop)
							entry.TrailingStop = newStop;
					}

					if (entry.TrailingStop.HasValue && candle.HighPrice >= entry.TrailingStop.Value)
						{
						SendCloseOrder(entry);
						closed = true;
					}
				}
			}
		}

		return closed;
	}

	private void SendCloseOrder(PositionEntry entry)
	{
		if (entry.PendingCloseVolume > 0m)
			return;

		entry.PendingCloseVolume = entry.Volume;

		if (entry.Side == Sides.Buy)
			SellMarket(entry.Volume);
		else
			BuyMarket(entry.Volume);
	}

	private void CloseMaximumProfit(decimal price)
	{
		PositionEntry? best = null;
		var bestProfit = decimal.MinValue;

		foreach (var entry in _longEntries)
			{
			var profit = GetEntryProfit(entry, price);
			if (profit > bestProfit)
				{
				bestProfit = profit;
				best = entry;
			}
		}

		foreach (var entry in _shortEntries)
			{
			var profit = GetEntryProfit(entry, price);
			if (profit > bestProfit)
				{
				bestProfit = profit;
				best = entry;
			}
		}

		if (best != null)
			SendCloseOrder(best);
	}

	private void CloseMinimumProfit(decimal price)
	{
		PositionEntry? worst = null;
		var worstProfit = decimal.MaxValue;

		foreach (var entry in _longEntries)
			{
			var profit = GetEntryProfit(entry, price);
			if (profit < worstProfit)
				{
				worstProfit = profit;
				worst = entry;
			}
		}

		foreach (var entry in _shortEntries)
			{
			var profit = GetEntryProfit(entry, price);
			if (profit < worstProfit)
				{
				worstProfit = profit;
				worst = entry;
			}
		}

		if (worst != null)
			SendCloseOrder(worst);
	}

	private decimal GetTotalProfit(decimal price)
	{
		var total = 0m;

		foreach (var entry in _longEntries)
			total += GetEntryProfit(entry, price);

		foreach (var entry in _shortEntries)
			total += GetEntryProfit(entry, price);

		return total;
	}

	private decimal GetEntryProfit(PositionEntry entry, decimal price)
	{
		var priceStep = Security?.PriceStep ?? 1m;
		var stepPrice = Security?.StepPrice ?? priceStep;
		if (priceStep == 0m)
			priceStep = 1m;

		var diff = entry.Side == Sides.Buy ? price - entry.EntryPrice : entry.EntryPrice - price;
		var steps = diff / priceStep;
		return steps * stepPrice * entry.Volume;
	}

	private decimal CalculateEntryVolume(decimal stopDistance, decimal multiplier)
	{
		var volume = BaseVolume > 0m ? BaseVolume : CalculateRiskVolume(stopDistance);
		if (volume <= 0m)
			return 0m;

		volume *= multiplier;

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
			volume = Math.Floor(volume / step) * step;

		var min = Security?.MinVolume ?? 0m;
		if (min > 0m && volume < min)
			return 0m;

		var max = Security?.MaxVolume;
		if (max.HasValue && volume > max.Value)
			volume = max.Value;

		if (volume > MaxVolume)
			return 0m;

		return volume;
	}

	private decimal CalculateRiskVolume(decimal stopDistance)
	{
		if (stopDistance <= 0m)
			return 0m;

		var portfolioValue = Portfolio?.CurrentValue;
		if (!portfolioValue.HasValue || portfolioValue.Value <= 0m)
			return 0m;

		var priceStep = Security?.PriceStep ?? 1m;
		var stepPrice = Security?.StepPrice ?? priceStep;
		if (priceStep == 0m || stepPrice == 0m)
			return 0m;

		var steps = stopDistance / priceStep;
		if (steps <= 0m)
			return 0m;

		var lossPerUnit = steps * stepPrice;
		if (lossPerUnit <= 0m)
			return 0m;

		var riskAmount = portfolioValue.Value * (RiskPercent / 100m);
		return riskAmount / lossPerUnit;
	}

	private decimal GetIntervalDistance(int openPositions)
	{
		var distance = IntervalPips > 0 ? IntervalPips * _pipSize : 0m;
		if (distance <= 0m)
			return 0m;

		if (openPositions > 0)
			{
			var multiplier = Pow(IntervalCoefficient, openPositions);
			distance *= multiplier;
		}

		return distance;
	}

	private static decimal Pow(decimal value, int exponent)
	{
		var result = 1m;
		for (var i = 0; i < exponent; i++)
			result *= value;
		return result;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
			return;

		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;

		if (trade.Order.Side == Sides.Buy)
			{
			if (_pendingBuyEntry != null)
				{
				ProcessPendingEntry(_pendingBuyEntry, volume, price, _longEntries, true);
				if (_pendingBuyEntry.FilledVolume >= _pendingBuyEntry.Volume - 0.0000001m)
					{
					_lastLongEntryPrice = _pendingBuyEntry.Entry?.EntryPrice ?? _lastLongEntryPrice;
					_pendingBuyEntry = null;
				}
			}
			else
				{
				ProcessClose(_shortEntries, volume, false);
			}
		}
		else if (trade.Order.Side == Sides.Sell)
			{
			if (_pendingSellEntry != null)
				{
				ProcessPendingEntry(_pendingSellEntry, volume, price, _shortEntries, false);
				if (_pendingSellEntry.FilledVolume >= _pendingSellEntry.Volume - 0.0000001m)
					{
					_lastShortEntryPrice = _pendingSellEntry.Entry?.EntryPrice ?? _lastShortEntryPrice;
					_pendingSellEntry = null;
				}
			}
			else
				{
				ProcessClose(_longEntries, volume, true);
			}
		}
	}

	private void ProcessPendingEntry(PendingEntry pending, decimal volume, decimal price, List<PositionEntry> entries, bool isLong)
	{
		var entry = pending.Entry;
		if (entry == null)
			{
			entry = new PositionEntry(pending.Side, volume)
			{
				EntryPrice = price,
				TrailingDistance = pending.TrailingDistance,
				TrailingStep = pending.TrailingStep
			};
			entries.Add(entry);
			pending.Entry = entry;
		}
		else
			{
			var totalVolume = entry.Volume + volume;
			entry.EntryPrice = (entry.EntryPrice * entry.Volume + price * volume) / totalVolume;
			entry.Volume = totalVolume;
		}

		pending.FilledVolume += volume;

		if (isLong)
			{
			entry.StopLoss = pending.StopLossDistance > 0m ? entry.EntryPrice - pending.StopLossDistance : null;
			entry.TakeProfit = pending.TakeProfitDistance > 0m ? entry.EntryPrice + pending.TakeProfitDistance : null;
		}
		else
			{
			entry.StopLoss = pending.StopLossDistance > 0m ? entry.EntryPrice + pending.StopLossDistance : null;
			entry.TakeProfit = pending.TakeProfitDistance > 0m ? entry.EntryPrice - pending.TakeProfitDistance : null;
		}

		entry.TrailingStop = null;
	}

	private void ProcessClose(List<PositionEntry> entries, decimal volume, bool closingLong)
	{
		var remaining = volume;

		foreach (var entry in entries)
			{
			if (remaining <= 0m)
				break;

			if (entry.PendingCloseVolume <= 0m)
				continue;

			var closeVolume = Math.Min(entry.PendingCloseVolume, remaining);
			entry.PendingCloseVolume -= closeVolume;
			entry.Volume -= closeVolume;
			remaining -= closeVolume;

			if (entry.PendingCloseVolume <= 0m)
				entry.PendingCloseVolume = 0m;
		}

		for (var i = entries.Count - 1; i >= 0; i--)
			{
			var entry = entries[i];
			if (entry.Volume <= 0m)
				{
				entries.RemoveAt(i);
			}
		}

		if (closingLong)
			_lastLongEntryPrice = _longEntries.Count > 0 ? _longEntries[^1].EntryPrice : 0m;
		else
			_lastShortEntryPrice = _shortEntries.Count > 0 ? _shortEntries[^1].EntryPrice : 0m;
	}
}
