namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// Port of the "Elite eFibo Trader" averaging expert advisor.
/// Recreates the Fibonacci volume progression with optional MA and RSI filters.
/// </summary>
public class EliteEfiboTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _levelCount;

	private readonly StrategyParam<bool> _useMaLogic;
	private readonly StrategyParam<int> _maSlowPeriod;
	private readonly StrategyParam<int> _maFastPeriod;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<bool> _useRsiFilter;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiHigh;
	private readonly StrategyParam<decimal> _rsiLow;
	private readonly StrategyParam<bool> _manualOpenBuy;
	private readonly StrategyParam<bool> _manualOpenSell;
	private readonly StrategyParam<bool> _tradeAgainAfterProfit;
	private readonly StrategyParam<int> _levelDistancePips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<decimal> _moneyTakeProfit;
	private readonly StrategyParam<DataType> _candleType;
	private StrategyParam<decimal>[] _levelVolumeParams;

	private SimpleMovingAverage _slowMa;
	private SimpleMovingAverage _fastMa;
	private RelativeStrengthIndex _rsi;

	private readonly List<LevelState> _levels = new();
	private readonly Dictionary<Order, LevelState> _entryOrderMap = new();
	private readonly Dictionary<Order, LevelState> _exitOrderMap = new();

	private bool _allowTrading = true;
	private bool _maOpenBuy;
	private bool _maOpenSell;
	private decimal? _previousSlow;
	private decimal? _previousFast;
	private DateTimeOffset? _lastSignalTime;
	private decimal _pipSize;
	private decimal _priceStep;
	private decimal _stepPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="EliteEfiboTraderStrategy"/> class.
	/// </summary>
	public EliteEfiboTraderStrategy()
	{
		_useMaLogic = Param(nameof(UseMaLogic), true)
		.SetDisplay("Use MA Logic", "Enable the moving average crossover logic that overrides manual direction switches.", "Filters");

		_maSlowPeriod = Param(nameof(MaSlowPeriod), 65)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA", "Slow simple moving average period.", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(10, 150, 5);

		_maFastPeriod = Param(nameof(MaFastPeriod), 15)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA", "Fast simple moving average period.", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(5, 60, 5);

		_trailingStopPips = Param(nameof(TrailingStopPips), 15)
		.SetNotNegative()
		.SetDisplay("Trailing Stop", "Trailing stop distance in pips applied after an adverse MA crossover.", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5, 60, 5);

		_useRsiFilter = Param(nameof(UseRsiFilter), false)
		.SetDisplay("Use RSI Filter", "Require RSI confirmation before opening a new basket.", "Filters");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Period used for the RSI confirmation filter.", "Filters")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 5);

		_rsiHigh = Param(nameof(RsiHigh), 70m)
		.SetDisplay("RSI High", "RSI level that permits long baskets when the filter is active.", "Filters");

		_rsiLow = Param(nameof(RsiLow), 30m)
		.SetDisplay("RSI Low", "RSI level that permits short baskets when the filter is active.", "Filters");

		_manualOpenBuy = Param(nameof(ManualOpenBuy), false)
		.SetDisplay("Manual Buy", "Allow opening buy ladders when MA logic is disabled.", "Execution");

		_manualOpenSell = Param(nameof(ManualOpenSell), true)
		.SetDisplay("Manual Sell", "Allow opening sell ladders when MA logic is disabled.", "Execution");

		_tradeAgainAfterProfit = Param(nameof(TradeAgainAfterProfit), true)
		.SetDisplay("Trade After Profit", "Resume trading after the money take-profit is reached.", "Risk");

		_levelCount = Param(nameof(LevelCount), 14)
		.SetGreaterThanZero()
		.SetDisplay("Level Count", "Number of Fibonacci ladder levels managed by the strategy.", "Position Sizing");

		_levelDistancePips = Param(nameof(LevelDistancePips), 20)
		.SetNotNegative()
		.SetDisplay("Level Distance", "Distance between consecutive pending levels in pips.", "Execution")
		.SetCanOptimize(true)
		.SetOptimize(5, 80, 5);

		_stopLossPips = Param(nameof(StopLossPips), 10)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Initial stop-loss distance in pips for each level.", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5, 80, 5);

		_moneyTakeProfit = Param(nameof(MoneyTakeProfit), 2000m)
		.SetNotNegative()
		.SetDisplay("Money Take Profit", "Cash profit target for the active basket.", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(200m, 5000m, 200m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Trading timeframe used for indicator calculations.", "General");

		ConfigureLevelVolumeParams(LevelCount);
	}

	private void ConfigureLevelVolumeParams(int count)
	{
		count = Math.Max(1, count);

		var defaults = BuildLevelVolumeDefaults(count);
		var newParams = new StrategyParam<decimal>[count];

		for (var i = 0; i < count; i++)
		{
			var index = i + 1;
			var defaultVolume = defaults[i];

			if (_levelVolumeParams != null && i < _levelVolumeParams.Length)
				defaultVolume = _levelVolumeParams[i].Value;

			newParams[i] = Param($"Level{index}Volume", defaultVolume)
				.SetNotNegative()
				.SetDisplay($"Level {index} Volume", $"Volume used for ladder level {index}.", "Position Sizing");
		}

		_levelVolumeParams = newParams;
	}

	private static decimal[] BuildLevelVolumeDefaults(int count)
	{
		if (count <= 0)
			return Array.Empty<decimal>();

		if (count == 1)
			return new[] { 1m };

		var values = new decimal[count];
		values[0] = 1m;
		values[1] = 1m;

		for (var i = 2; i < count; i++)
			values[i] = values[i - 1] + values[i - 2];

		return values;
	}

	/// <summary>
	/// Use the moving average crossover logic instead of manual switches.
	/// </summary>
	public bool UseMaLogic
	{
		get => _useMaLogic.Value;
		set => _useMaLogic.Value = value;
	}

	/// <summary>
	/// Slow simple moving average period.
	/// </summary>
	public int MaSlowPeriod
	{
		get => _maSlowPeriod.Value;
		set
		{
			_maSlowPeriod.Value = value;
			if (_slowMa != null)
			_slowMa.Length = value;
		}
	}

	/// <summary>
	/// Fast simple moving average period.
	/// </summary>
	public int MaFastPeriod
	{
		get => _maFastPeriod.Value;
		set
		{
			_maFastPeriod.Value = value;
			if (_fastMa != null)
			_fastMa.Length = value;
		}
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Enable the RSI confirmation filter.
	/// </summary>
	public bool UseRsiFilter
	{
		get => _useRsiFilter.Value;
		set => _useRsiFilter.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set
		{
			_rsiPeriod.Value = value;
			if (_rsi != null)
			_rsi.Length = value;
		}
	}

	/// <summary>
	/// RSI threshold that allows long baskets.
	/// </summary>
	public decimal RsiHigh
	{
		get => _rsiHigh.Value;
		set => _rsiHigh.Value = value;
	}

	/// <summary>
	/// RSI threshold that allows short baskets.
	/// </summary>
	public decimal RsiLow
	{
		get => _rsiLow.Value;
		set => _rsiLow.Value = value;
	}

	/// <summary>
	/// Manual switch that allows buy ladders when MA logic is disabled.
	/// </summary>
	public bool ManualOpenBuy
	{
		get => _manualOpenBuy.Value;
		set => _manualOpenBuy.Value = value;
	}

	/// <summary>
	/// Manual switch that allows sell ladders when MA logic is disabled.
	/// </summary>
	public bool ManualOpenSell
	{
		get => _manualOpenSell.Value;
		set => _manualOpenSell.Value = value;
	}

	/// <summary>
	/// Continue trading after the money take-profit fires.
	/// </summary>
	public bool TradeAgainAfterProfit
	{
		get => _tradeAgainAfterProfit.Value;
		set => _tradeAgainAfterProfit.Value = value;
	}

	/// <summary>
	/// Number of Fibonacci ladder levels managed by the strategy.
	/// </summary>
	public int LevelCount
	{
		get => _levelCount.Value;
		set
		{
			_levelCount.Value = value;
			ConfigureLevelVolumeParams(value);
		}
	}

	/// <summary>
	/// Distance between ladder levels measured in pips.
	/// </summary>
	public int LevelDistancePips
	{
		get => _levelDistancePips.Value;
		set => _levelDistancePips.Value = value;
	}

	/// <summary>
	/// Initial stop-loss distance for each level measured in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Basket take-profit expressed in account currency.
	/// </summary>
	public decimal MoneyTakeProfit
	{
		get => _moneyTakeProfit.Value;
		set => _moneyTakeProfit.Value = value;
	}

	/// <summary>
	/// Candle type used for indicators and trade timing.
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

		_levels.Clear();
		_entryOrderMap.Clear();
		_exitOrderMap.Clear();
		_allowTrading = true;
		_maOpenBuy = false;
		_maOpenSell = false;
		_previousSlow = null;
		_previousFast = null;
		_lastSignalTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ConfigureLevelVolumeParams(_levelCount.Value);

		_pipSize = CalculatePipSize();
		_priceStep = Security?.PriceStep ?? 0m;
		_stepPrice = Security?.StepPrice ?? 0m;

		_slowMa = new SimpleMovingAverage { Length = MaSlowPeriod };
		_fastMa = new SimpleMovingAverage { Length = MaFastPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_slowMa, _fastMa, _rsi, ProcessCandle)
		.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		if (order == null)
		return;

		if (_entryOrderMap.TryGetValue(order, out var entryLevel) && IsFinalState(order))
		{
			_entryOrderMap.Remove(order);
			entryLevel.EntryOrder = null;
		}

		if (_exitOrderMap.TryGetValue(order, out var exitLevel) && IsFinalState(order))
		{
			_exitOrderMap.Remove(order);
			exitLevel.ExitOrder = null;
			if (exitLevel.OpenVolume <= 0m)
			exitLevel.StopPrice = null;
		}

		CleanupLevels();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order is not Order order)
		return;

		var tradeInfo = trade.Trade;
		var tradeVolume = tradeInfo?.Volume ?? 0m;
		if (tradeVolume <= 0m)
		return;

		if (_entryOrderMap.TryGetValue(order, out var entryLevel))
		{
			var tradePrice = tradeInfo?.Price ?? order.Price ?? 0m;
			if (tradePrice > 0m)
			{
				var executed = entryLevel.ExecutedVolume;
				var newExecuted = executed + tradeVolume;
				if (executed <= 0m)
				{
					entryLevel.EntryPrice = tradePrice;
				}
				else if (entryLevel.EntryPrice is decimal avg)
				{
					entryLevel.EntryPrice = (avg * executed + tradePrice * tradeVolume) / newExecuted;
				}
				entryLevel.ExecutedVolume = newExecuted;
			}

			entryLevel.OpenVolume += tradeVolume;

			UpdateInitialStop(entryLevel);
		}
		else if (_exitOrderMap.TryGetValue(order, out var exitLevel))
		{
			exitLevel.OpenVolume -= tradeVolume;
			if (exitLevel.OpenVolume < 0m)
			exitLevel.OpenVolume = 0m;
		}

		CleanupLevels();
	}

	private void ProcessCandle(ICandleMessage candle, decimal slowValue, decimal fastValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateTrailingStops(candle);
		CheckStopHits(candle);
		CleanupLevels();

		if (!HasOpenVolume() && HasPendingOrders())
		CancelAllPendingOrders();

		if (TradeAgainAfterProfit)
		_allowTrading = true;

		if (MoneyTakeProfit > 0m && HasOpenVolume())
		{
			var profit = CalculateOpenProfit(candle.ClosePrice);
			if (profit >= MoneyTakeProfit)
			{
				LogInfo($"Money take profit reached: {profit:F2}");
				CloseAllPositions();
				CancelAllPendingOrders();
				if (!TradeAgainAfterProfit)
				_allowTrading = false;
			}
		}

		if (_slowMa == null || _fastMa == null || _rsi == null)
		return;

		if (!_slowMa.IsFormed || !_fastMa.IsFormed)
		{
			_previousSlow = slowValue;
			_previousFast = fastValue;
			return;
		}

		if (UseRsiFilter && !_rsi.IsFormed)
		{
			_previousSlow = slowValue;
			_previousFast = fastValue;
			return;
		}

		if (UseMaLogic)
		HandleMovingAverageLogic(candle, slowValue, fastValue);
		else
		{
			_previousSlow = slowValue;
			_previousFast = fastValue;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_allowTrading)
		return;

		if (HasActiveExposure())
		return;

		var openBuy = UseMaLogic ? _maOpenBuy && !_maOpenSell : ManualOpenBuy && !ManualOpenSell;
		var openSell = UseMaLogic ? _maOpenSell && !_maOpenBuy : ManualOpenSell && !ManualOpenBuy;

		if (UseRsiFilter)
		{
			if (openBuy && !(rsiValue > RsiHigh))
			openBuy = false;

			if (openSell && !(rsiValue < RsiLow))
			openSell = false;
		}

		if (openBuy)
		{
			TryOpenLadder(Sides.Buy, candle.ClosePrice);
		}
		else if (openSell)
		{
			TryOpenLadder(Sides.Sell, candle.ClosePrice);
		}
	}

	private void HandleMovingAverageLogic(ICandleMessage candle, decimal slowValue, decimal fastValue)
	{
		if (_previousSlow is decimal prevSlow && _previousFast is decimal prevFast)
		{
			if (slowValue > fastValue && prevSlow <= prevFast && _lastSignalTime != candle.OpenTime)
			{
				_maOpenSell = true;
				_maOpenBuy = false;
				_lastSignalTime = candle.OpenTime;
			}
			else if (slowValue < fastValue && prevSlow >= prevFast && _lastSignalTime != candle.OpenTime)
			{
				_maOpenBuy = true;
				_maOpenSell = false;
				_lastSignalTime = candle.OpenTime;
			}
		}

		if (HasOpenSide(Sides.Buy) && slowValue > fastValue)
		{
			var profit = CalculateOpenProfit(candle.ClosePrice);
			if (profit > 0m)
			{
				LogInfo("Moving average crossover triggered a long basket exit.");
				CloseAllPositions();
				CancelAllPendingOrders();
			}
			else
			{
				ApplyTrailing(Sides.Buy, candle.ClosePrice);
			}
		}

		if (HasOpenSide(Sides.Sell) && slowValue < fastValue)
		{
			var profit = CalculateOpenProfit(candle.ClosePrice);
			if (profit > 0m)
			{
				LogInfo("Moving average crossover triggered a short basket exit.");
				CloseAllPositions();
				CancelAllPendingOrders();
			}
			else
			{
				ApplyTrailing(Sides.Sell, candle.ClosePrice);
			}
		}

		_previousSlow = slowValue;
		_previousFast = fastValue;
	}

	private void TryOpenLadder(Sides side, decimal referencePrice)
	{
		var levelDistance = LevelDistancePips * _pipSize;
		var stopOffset = StopLossPips * _pipSize;

		for (var i = 0; i < _levelVolumeParams.Length; i++)
		{
			var volume = _levelVolumeParams[i].Value;
			if (volume <= 0m)
			continue;

			volume = NormalizeVolume(volume);
			if (volume <= 0m)
			continue;

			var levelIndex = i + 1;
			Order order;

			if (i == 0)
			{
				order = side == Sides.Buy ? BuyMarket(volume) : SellMarket(volume);
			}
			else
			{
				decimal price;
				if (side == Sides.Buy)
				price = referencePrice + levelDistance * i;
				else
				price = referencePrice - levelDistance * i;

				price = NormalizePrice(price);
				if (price <= 0m)
				continue;

				order = side == Sides.Buy ? BuyStop(volume, price) : SellStop(volume, price);
			}

			if (order == null)
			continue;

			var level = new LevelState(levelIndex, side, volume, stopOffset)
			{
				EntryOrder = order
			};

			_levels.Add(level);
			_entryOrderMap[order] = level;
		}
	}

	private void CloseAllPositions()
	{
		foreach (var level in _levels)
		{
			if (level.OpenVolume <= 0m)
			continue;

			if (level.ExitOrder != null && !IsFinalState(level.ExitOrder))
			continue;

			Order order;
			if (level.Side == Sides.Buy)
			order = SellMarket(level.OpenVolume);
			else
			order = BuyMarket(level.OpenVolume);

			if (order != null)
			{
				level.ExitOrder = order;
				_exitOrderMap[order] = level;
			}
		}
	}

	private void CancelAllPendingOrders()
	{
		foreach (var level in _levels)
		{
			var order = level.EntryOrder;
			if (order == null || IsFinalState(order))
			continue;

			CancelOrder(order);
		}
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
		ApplyTrailing(Sides.Buy, candle.ClosePrice);
		ApplyTrailing(Sides.Sell, candle.ClosePrice);
	}

	private void ApplyTrailing(Sides side, decimal closePrice)
	{
		if (TrailingStopPips <= 0 || _pipSize <= 0m)
		return;

		var offset = TrailingStopPips * _pipSize;
		if (offset <= 0m)
		return;

		foreach (var level in _levels)
		{
			if (level.Side != side || level.OpenVolume <= 0m)
			continue;

			if (side == Sides.Buy)
			{
				var newStop = NormalizePrice(closePrice - offset);
				if (level.StopPrice is not decimal current || newStop > current)
				level.StopPrice = newStop;
			}
			else
			{
				var newStop = NormalizePrice(closePrice + offset);
				if (level.StopPrice is not decimal current || newStop < current)
				level.StopPrice = newStop;
			}
		}
	}

	private void UpdateInitialStop(LevelState level)
	{
		if (level.StopOffset <= 0m || level.EntryPrice is not decimal price)
		return;

		var stop = level.Side == Sides.Buy
		? NormalizePrice(price - level.StopOffset)
		: NormalizePrice(price + level.StopOffset);

		if (level.StopPrice is not decimal current)
		{
			level.StopPrice = stop;
		}
		else if (level.Side == Sides.Buy && stop > current)
		{
			level.StopPrice = stop;
		}
		else if (level.Side == Sides.Sell && stop < current)
		{
			level.StopPrice = stop;
		}
	}

	private void CheckStopHits(ICandleMessage candle)
	{
		foreach (var level in _levels)
		{
			if (level.OpenVolume <= 0m || level.StopPrice is not decimal stop)
			continue;

			if (level.ExitOrder != null && !IsFinalState(level.ExitOrder))
			continue;

			if (level.Side == Sides.Buy)
			{
				if (candle.LowPrice <= stop)
				RequestExit(level);
			}
			else
			{
				if (candle.HighPrice >= stop)
				RequestExit(level);
			}
		}
	}

	private void RequestExit(LevelState level)
	{
		if (level.OpenVolume <= 0m)
		return;

		if (level.ExitOrder != null && !IsFinalState(level.ExitOrder))
		return;

		Order order = level.Side == Sides.Buy
		? SellMarket(level.OpenVolume)
		: BuyMarket(level.OpenVolume);

		if (order != null)
		{
			level.ExitOrder = order;
			_exitOrderMap[order] = level;
		}
	}

	private bool HasOpenSide(Sides side)
	{
		foreach (var level in _levels)
		{
			if (level.Side == side && level.OpenVolume > 0m)
			return true;
		}

		return false;
	}

	private bool HasOpenVolume()
	{
		foreach (var level in _levels)
		{
			if (level.OpenVolume > 0m)
			return true;
		}

		return false;
	}

	private bool HasPendingOrders()
	{
		foreach (var level in _levels)
		{
			var order = level.EntryOrder;
			if (order != null && !IsFinalState(order))
			return true;
		}

		return false;
	}

	private bool HasActiveExposure()
	{
		if (HasOpenVolume())
		return true;

		foreach (var level in _levels)
		{
			if (level.EntryOrder != null && !IsFinalState(level.EntryOrder))
			return true;

			if (level.ExitOrder != null && !IsFinalState(level.ExitOrder))
			return true;
		}

		return false;
	}

	private void CleanupLevels()
	{
		for (var i = _levels.Count - 1; i >= 0; i--)
		{
			var level = _levels[i];

			if (level.OpenVolume > 0m)
			continue;

			var hasEntry = level.EntryOrder != null && !IsFinalState(level.EntryOrder);
			var hasExit = level.ExitOrder != null && !IsFinalState(level.ExitOrder);

			if (!hasEntry && !hasExit)
			{
				_levels.RemoveAt(i);
			}
		}
	}

	private decimal CalculateOpenProfit(decimal currentPrice)
	{
		if (_priceStep <= 0m || _stepPrice <= 0m)
		return 0m;

		decimal profit = 0m;

		foreach (var level in _levels)
		{
			if (level.OpenVolume <= 0m || level.EntryPrice is not decimal entry)
			continue;

			var difference = level.Side == Sides.Buy ? currentPrice - entry : entry - currentPrice;
			var steps = difference / _priceStep;
			profit += steps * _stepPrice * level.OpenVolume;
		}

		return profit;
	}

	private decimal NormalizePrice(decimal price)
	{
		if (_priceStep <= 0m)
		return price;

		return Math.Round(price / _priceStep, MidpointRounding.AwayFromZero) * _priceStep;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
		return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		var minVolume = security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
		volume = minVolume.Value;

		var maxVolume = security.MaxVolume ?? 0m;
		if (maxVolume > 0m && volume > maxVolume)
		volume = maxVolume.Value;

		return volume;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
		return 0.0001m;

		var step = security.PriceStep ?? 0.0001m;
		var multiplier = security.Decimals is 3 or 5 ? 10m : 1m;
		var pip = step * multiplier;
		return pip > 0m ? pip : 0.0001m;
	}

	private sealed class LevelState
	{
		public LevelState(int index, Sides side, decimal plannedVolume, decimal stopOffset)
		{
			Index = index;
			Side = side;
			PlannedVolume = plannedVolume;
			StopOffset = stopOffset;
		}

		public int Index { get; }
		public Sides Side { get; }
		public decimal PlannedVolume { get; }
		public decimal StopOffset { get; }
		public Order EntryOrder { get; set; }
		public Order ExitOrder { get; set; }
		public decimal ExecutedVolume { get; set; }
		public decimal OpenVolume { get; set; }
		public decimal? EntryPrice { get; set; }
		public decimal? StopPrice { get; set; }
	}
}
