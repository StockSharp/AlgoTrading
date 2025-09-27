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

using Ecng.ComponentModel;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Color Schaff JCCX Trend Cycle MMRec Duplex strategy.
/// Combines two Schaff trend cycle pipelines to trade in both directions with adaptive position sizing.
/// </summary>
public class ColorSchaffJccxTrendCycleMmrecDuplexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _longCandleType;
	private readonly StrategyParam<int> _longFastLength;
	private readonly StrategyParam<int> _longSlowLength;
	private readonly StrategyParam<int> _longSmoothLength;
	private readonly StrategyParam<int> _longPhase;
	private readonly StrategyParam<int> _longCycle;
	private readonly StrategyParam<int> _longSignalBar;
	private readonly StrategyParam<AppliedPrices> _longAppliedPrice;
	private readonly StrategyParam<bool> _longAllowOpen;
	private readonly StrategyParam<bool> _longAllowClose;
	private readonly StrategyParam<int> _longTotalTrigger;
	private readonly StrategyParam<int> _longLossTrigger;
	private readonly StrategyParam<decimal> _longSmallVolume;
	private readonly StrategyParam<decimal> _longNormalVolume;
	private readonly StrategyParam<decimal> _longStopLoss;
	private readonly StrategyParam<decimal> _longTakeProfit;

	private readonly StrategyParam<DataType> _shortCandleType;
	private readonly StrategyParam<int> _shortFastLength;
	private readonly StrategyParam<int> _shortSlowLength;
	private readonly StrategyParam<int> _shortSmoothLength;
	private readonly StrategyParam<int> _shortPhase;
	private readonly StrategyParam<int> _shortCycle;
	private readonly StrategyParam<int> _shortSignalBar;
	private readonly StrategyParam<AppliedPrices> _shortAppliedPrice;
	private readonly StrategyParam<bool> _shortAllowOpen;
	private readonly StrategyParam<bool> _shortAllowClose;
	private readonly StrategyParam<int> _shortTotalTrigger;
	private readonly StrategyParam<int> _shortLossTrigger;
	private readonly StrategyParam<decimal> _shortSmallVolume;
	private readonly StrategyParam<decimal> _shortNormalVolume;
	private readonly StrategyParam<decimal> _shortStopLoss;
	private readonly StrategyParam<decimal> _shortTakeProfit;

	private SchaffTrendCycleCalculator _longCalculator = null!;
	private SchaffTrendCycleCalculator _shortCalculator = null!;

	private readonly List<decimal> _longValues = new();
	private readonly List<decimal> _shortValues = new();
	private readonly Queue<bool> _longResults = new();
	private readonly Queue<bool> _shortResults = new();

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal _longEntryVolume;
	private decimal _shortEntryVolume;

	/// <summary>
	/// Long side candle type.
	/// </summary>
	public DataType LongCandleType
	{
		get => _longCandleType.Value;
		set => _longCandleType.Value = value;
	}

	/// <summary>
	/// Fast Jurik length for the long Schaff trend cycle.
	/// </summary>
	public int LongFastLength
	{
		get => _longFastLength.Value;
		set => _longFastLength.Value = value;
	}

	/// <summary>
	/// Slow Jurik length for the long Schaff trend cycle.
	/// </summary>
	public int LongSlowLength
	{
		get => _longSlowLength.Value;
		set => _longSlowLength.Value = value;
	}

	/// <summary>
	/// Jurik smoothing length applied inside the long JCCX approximation.
	/// </summary>
	public int LongSmoothLength
	{
		get => _longSmoothLength.Value;
		set => _longSmoothLength.Value = value;
	}

	/// <summary>
	/// Phase control used to adjust the internal smoothing factor for the long side.
	/// </summary>
	public int LongPhase
	{
		get => _longPhase.Value;
		set => _longPhase.Value = value;
	}

	/// <summary>
	/// Cycle length used for stochastic transforms in the long Schaff trend cycle.
	/// </summary>
	public int LongCycle
	{
		get => _longCycle.Value;
		set => _longCycle.Value = value;
	}

	/// <summary>
	/// Number of closed candles to wait before evaluating long signals.
	/// </summary>
	public int LongSignalBar
	{
		get => _longSignalBar.Value;
		set => _longSignalBar.Value = value;
	}

	/// <summary>
	/// Price source used for the long calculations.
	/// </summary>
	public AppliedPrices LongAppliedPrice
	{
		get => _longAppliedPrice.Value;
		set => _longAppliedPrice.Value = value;
	}

	/// <summary>
	/// Enables long entries.
	/// </summary>
	public bool LongAllowOpen
	{
		get => _longAllowOpen.Value;
		set => _longAllowOpen.Value = value;
	}

	/// <summary>
	/// Enables long exits based on indicator signals.
	/// </summary>
	public bool LongAllowClose
	{
		get => _longAllowClose.Value;
		set => _longAllowClose.Value = value;
	}

	/// <summary>
	/// Maximum number of recent long trades considered for money management.
	/// </summary>
	public int LongTotalTrigger
	{
		get => _longTotalTrigger.Value;
		set => _longTotalTrigger.Value = value;
	}

	/// <summary>
	/// Number of losing long trades that switches to the reduced position size.
	/// </summary>
	public int LongLossTrigger
	{
		get => _longLossTrigger.Value;
		set => _longLossTrigger.Value = value;
	}

	/// <summary>
	/// Reduced long position size used after a loss streak.
	/// </summary>
	public decimal LongSmallVolume
	{
		get => _longSmallVolume.Value;
		set => _longSmallVolume.Value = value;
	}

	/// <summary>
	/// Default long position size.
	/// </summary>
	public decimal LongNormalVolume
	{
		get => _longNormalVolume.Value;
		set => _longNormalVolume.Value = value;
	}

	/// <summary>
	/// Long stop-loss in price steps.
	/// </summary>
	public decimal LongStopLoss
	{
		get => _longStopLoss.Value;
		set => _longStopLoss.Value = value;
	}

	/// <summary>
	/// Long take-profit in price steps.
	/// </summary>
	public decimal LongTakeProfit
	{
		get => _longTakeProfit.Value;
		set => _longTakeProfit.Value = value;
	}

	/// <summary>
	/// Short side candle type.
	/// </summary>
	public DataType ShortCandleType
	{
		get => _shortCandleType.Value;
		set => _shortCandleType.Value = value;
	}

	/// <summary>
	/// Fast Jurik length for the short Schaff trend cycle.
	/// </summary>
	public int ShortFastLength
	{
		get => _shortFastLength.Value;
		set => _shortFastLength.Value = value;
	}

	/// <summary>
	/// Slow Jurik length for the short Schaff trend cycle.
	/// </summary>
	public int ShortSlowLength
	{
		get => _shortSlowLength.Value;
		set => _shortSlowLength.Value = value;
	}

	/// <summary>
	/// Jurik smoothing length applied inside the short JCCX approximation.
	/// </summary>
	public int ShortSmoothLength
	{
		get => _shortSmoothLength.Value;
		set => _shortSmoothLength.Value = value;
	}

	/// <summary>
	/// Phase control used to adjust the internal smoothing factor for the short side.
	/// </summary>
	public int ShortPhase
	{
		get => _shortPhase.Value;
		set => _shortPhase.Value = value;
	}

	/// <summary>
	/// Cycle length used for stochastic transforms in the short Schaff trend cycle.
	/// </summary>
	public int ShortCycle
	{
		get => _shortCycle.Value;
		set => _shortCycle.Value = value;
	}

	/// <summary>
	/// Number of closed candles to wait before evaluating short signals.
	/// </summary>
	public int ShortSignalBar
	{
		get => _shortSignalBar.Value;
		set => _shortSignalBar.Value = value;
	}

	/// <summary>
	/// Price source used for the short calculations.
	/// </summary>
	public AppliedPrices ShortAppliedPrice
	{
		get => _shortAppliedPrice.Value;
		set => _shortAppliedPrice.Value = value;
	}

	/// <summary>
	/// Enables short entries.
	/// </summary>
	public bool ShortAllowOpen
	{
		get => _shortAllowOpen.Value;
		set => _shortAllowOpen.Value = value;
	}

	/// <summary>
	/// Enables short exits based on indicator signals.
	/// </summary>
	public bool ShortAllowClose
	{
		get => _shortAllowClose.Value;
		set => _shortAllowClose.Value = value;
	}

	/// <summary>
	/// Maximum number of recent short trades considered for money management.
	/// </summary>
	public int ShortTotalTrigger
	{
		get => _shortTotalTrigger.Value;
		set => _shortTotalTrigger.Value = value;
	}

	/// <summary>
	/// Number of losing short trades that switches to the reduced position size.
	/// </summary>
	public int ShortLossTrigger
	{
		get => _shortLossTrigger.Value;
		set => _shortLossTrigger.Value = value;
	}

	/// <summary>
	/// Reduced short position size used after a loss streak.
	/// </summary>
	public decimal ShortSmallVolume
	{
		get => _shortSmallVolume.Value;
		set => _shortSmallVolume.Value = value;
	}

	/// <summary>
	/// Default short position size.
	/// </summary>
	public decimal ShortNormalVolume
	{
		get => _shortNormalVolume.Value;
		set => _shortNormalVolume.Value = value;
	}

	/// <summary>
	/// Short stop-loss in price steps.
	/// </summary>
	public decimal ShortStopLoss
	{
		get => _shortStopLoss.Value;
		set => _shortStopLoss.Value = value;
	}

	/// <summary>
	/// Short take-profit in price steps.
	/// </summary>
	public decimal ShortTakeProfit
	{
		get => _shortTakeProfit.Value;
		set => _shortTakeProfit.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ColorSchaffJccxTrendCycleMmrecDuplexStrategy"/>.
	/// </summary>
	public ColorSchaffJccxTrendCycleMmrecDuplexStrategy()
	{
		_longCandleType = Param(nameof(LongCandleType), TimeSpan.FromHours(8).TimeFrame())
		.SetDisplay("Long Candle Type", "Timeframe used for long calculations", "Long");

		_longFastLength = Param(nameof(LongFastLength), 23)
		.SetGreaterThanZero()
		.SetDisplay("Long Fast Length", "Fast Jurik length", "Long")
		.SetCanOptimize(true)
		.SetOptimize(10, 60, 5);

		_longSlowLength = Param(nameof(LongSlowLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("Long Slow Length", "Slow Jurik length", "Long")
		.SetCanOptimize(true)
		.SetOptimize(20, 120, 5);

		_longSmoothLength = Param(nameof(LongSmoothLength), 8)
		.SetGreaterThanZero()
		.SetDisplay("Long Jurik Smoothing", "Smoothing length for the JCCX approximation", "Long")
		.SetCanOptimize(true)
		.SetOptimize(4, 20, 1);

		_longPhase = Param(nameof(LongPhase), 100)
		.SetDisplay("Long Phase", "Phase control translated into smoothing factor", "Long")
		.SetCanOptimize(true)
		.SetOptimize(0, 200, 10);

		_longCycle = Param(nameof(LongCycle), 10)
		.SetGreaterThanZero()
		.SetDisplay("Long Cycle", "Cycle length for stochastic transforms", "Long")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 1);

		_longSignalBar = Param(nameof(LongSignalBar), 1)
		.SetDisplay("Long Signal Bar", "Delay in bars before evaluating long signals", "Long")
		.SetCanOptimize(true)
		.SetOptimize(0, 3, 1);

		_longAppliedPrice = Param(nameof(LongAppliedPrice), AppliedPrices.Close)
		.SetDisplay("Long Applied Price", "Price source for long logic", "Long");

		_longAllowOpen = Param(nameof(LongAllowOpen), true)
		.SetDisplay("Long Allow Open", "Enable opening long trades", "Long");

		_longAllowClose = Param(nameof(LongAllowClose), true)
		.SetDisplay("Long Allow Close", "Enable closing long trades", "Long");

		_longTotalTrigger = Param(nameof(LongTotalTrigger), 5)
		.SetGreaterThanZero()
		.SetDisplay("Long Total Trigger", "History length for long money management", "Long");

		_longLossTrigger = Param(nameof(LongLossTrigger), 3)
		.SetNotNegative()
		.SetDisplay("Long Loss Trigger", "Losses required to shrink long size", "Long")
		.SetCanOptimize(true)
		.SetOptimize(1, 5, 1);

		_longSmallVolume = Param(nameof(LongSmallVolume), 0.01m)
		.SetDisplay("Long Small Volume", "Reduced long volume", "Long")
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 0.1m, 0.01m);

		_longNormalVolume = Param(nameof(LongNormalVolume), 0.1m)
		.SetDisplay("Long Normal Volume", "Default long volume", "Long")
		.SetCanOptimize(true)
		.SetOptimize(0.05m, 0.5m, 0.05m);

		_longStopLoss = Param(nameof(LongStopLoss), 1000m)
		.SetNotNegative()
		.SetDisplay("Long Stop Loss", "Stop-loss distance in price steps", "Long");

		_longTakeProfit = Param(nameof(LongTakeProfit), 2000m)
		.SetNotNegative()
		.SetDisplay("Long Take Profit", "Take-profit distance in price steps", "Long");

		_shortCandleType = Param(nameof(ShortCandleType), TimeSpan.FromHours(8).TimeFrame())
		.SetDisplay("Short Candle Type", "Timeframe used for short calculations", "Short");

		_shortFastLength = Param(nameof(ShortFastLength), 23)
		.SetGreaterThanZero()
		.SetDisplay("Short Fast Length", "Fast Jurik length", "Short")
		.SetCanOptimize(true)
		.SetOptimize(10, 60, 5);

		_shortSlowLength = Param(nameof(ShortSlowLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("Short Slow Length", "Slow Jurik length", "Short")
		.SetCanOptimize(true)
		.SetOptimize(20, 120, 5);

		_shortSmoothLength = Param(nameof(ShortSmoothLength), 8)
		.SetGreaterThanZero()
		.SetDisplay("Short Jurik Smoothing", "Smoothing length for the JCCX approximation", "Short")
		.SetCanOptimize(true)
		.SetOptimize(4, 20, 1);

		_shortPhase = Param(nameof(ShortPhase), 100)
		.SetDisplay("Short Phase", "Phase control translated into smoothing factor", "Short")
		.SetCanOptimize(true)
		.SetOptimize(0, 200, 10);

		_shortCycle = Param(nameof(ShortCycle), 10)
		.SetGreaterThanZero()
		.SetDisplay("Short Cycle", "Cycle length for stochastic transforms", "Short")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 1);

		_shortSignalBar = Param(nameof(ShortSignalBar), 1)
		.SetDisplay("Short Signal Bar", "Delay in bars before evaluating short signals", "Short")
		.SetCanOptimize(true)
		.SetOptimize(0, 3, 1);

		_shortAppliedPrice = Param(nameof(ShortAppliedPrice), AppliedPrices.Close)
		.SetDisplay("Short Applied Price", "Price source for short logic", "Short");

		_shortAllowOpen = Param(nameof(ShortAllowOpen), true)
		.SetDisplay("Short Allow Open", "Enable opening short trades", "Short");

		_shortAllowClose = Param(nameof(ShortAllowClose), true)
		.SetDisplay("Short Allow Close", "Enable closing short trades", "Short");

		_shortTotalTrigger = Param(nameof(ShortTotalTrigger), 5)
		.SetGreaterThanZero()
		.SetDisplay("Short Total Trigger", "History length for short money management", "Short");

		_shortLossTrigger = Param(nameof(ShortLossTrigger), 3)
		.SetNotNegative()
		.SetDisplay("Short Loss Trigger", "Losses required to shrink short size", "Short")
		.SetCanOptimize(true)
		.SetOptimize(1, 5, 1);

		_shortSmallVolume = Param(nameof(ShortSmallVolume), 0.01m)
		.SetDisplay("Short Small Volume", "Reduced short volume", "Short")
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 0.1m, 0.01m);

		_shortNormalVolume = Param(nameof(ShortNormalVolume), 0.1m)
		.SetDisplay("Short Normal Volume", "Default short volume", "Short")
		.SetCanOptimize(true)
		.SetOptimize(0.05m, 0.5m, 0.05m);

		_shortStopLoss = Param(nameof(ShortStopLoss), 1000m)
		.SetNotNegative()
		.SetDisplay("Short Stop Loss", "Stop-loss distance in price steps", "Short");

		_shortTakeProfit = Param(nameof(ShortTakeProfit), 2000m)
		.SetNotNegative()
		.SetDisplay("Short Take Profit", "Take-profit distance in price steps", "Short");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var securities = new List<(Security, DataType)>();

		if (Security != null)
		{
			securities.Add((Security, LongCandleType));

			if (ShortCandleType != LongCandleType)
			securities.Add((Security, ShortCandleType));
		}

		return securities;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longCalculator?.Reset();
		_shortCalculator?.Reset();
		_longValues.Clear();
		_shortValues.Clear();
		_longResults.Clear();
		_shortResults.Clear();
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longEntryVolume = 0m;
		_shortEntryVolume = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
		throw new InvalidOperationException("Security is not specified.");

		_longCalculator = new SchaffTrendCycleCalculator(
		LongFastLength,
		LongSlowLength,
		LongSmoothLength,
		LongCycle,
		CalculateSmoothingFactor(LongPhase));

		_shortCalculator = new SchaffTrendCycleCalculator(
		ShortFastLength,
		ShortSlowLength,
		ShortSmoothLength,
		ShortCycle,
		CalculateSmoothingFactor(ShortPhase));

		if (LongCandleType == ShortCandleType)
		{
			var subscription = SubscribeCandles(LongCandleType);
			subscription.Bind(ProcessCombinedCandle).Start();

			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawOwnTrades(area);
			}
		}
		else
		{
			var longSubscription = SubscribeCandles(LongCandleType);
			longSubscription.Bind(ProcessLongCandle).Start();

			var shortSubscription = SubscribeCandles(ShortCandleType);
			shortSubscription.Bind(ProcessShortCandle).Start();

			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, longSubscription);
				DrawOwnTrades(area);
			}
		}
	}

	private void ProcessCombinedCandle(ICandleMessage candle)
	{
		ProcessLongCandle(candle);
		ProcessShortCandle(candle);
	}

	private void ProcessLongCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var price = GetAppliedPrice(candle, LongAppliedPrice);
		var value = _longCalculator.Process(price, candle.OpenTime);
		if (value is null)
		return;

		UpdateHistory(_longValues, value.Value, LongSignalBar);

		if (Position > 0 && _longEntryPrice is decimal entryPrice && TryHandleLongRisk(candle, entryPrice))
		return;

		var currentOffset = Math.Max(LongSignalBar - 1, 0);
		var previousOffset = currentOffset + 1;

		var current = GetHistoryValue(_longValues, currentOffset);
		var previous = GetHistoryValue(_longValues, previousOffset);

		if (current is null || previous is null)
		return;

		if (LongAllowClose && current < 0m && Position > 0)
		{
			SellMarket(Position);
			CompleteLongTrade(candle.ClosePrice);
		}

		if (LongAllowOpen && current > 0m && previous <= 0m)
		{
			if (Position < 0)
			{
				BuyMarket(Math.Abs(Position));
				CompleteShortTrade(candle.ClosePrice);
			}

			if (Position <= 0)
			{
				var volume = GetLongTradeVolume();
				if (volume > 0m)
				{
					BuyMarket(volume);
					RegisterLongEntry(candle.ClosePrice, volume);
				}
			}
		}
	}

	private void ProcessShortCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var price = GetAppliedPrice(candle, ShortAppliedPrice);
		var value = _shortCalculator.Process(price, candle.OpenTime);
		if (value is null)
		return;

		UpdateHistory(_shortValues, value.Value, ShortSignalBar);

		if (Position < 0 && _shortEntryPrice is decimal entryPrice && TryHandleShortRisk(candle, entryPrice))
		return;

		var currentOffset = Math.Max(ShortSignalBar - 1, 0);
		var previousOffset = currentOffset + 1;

		var current = GetHistoryValue(_shortValues, currentOffset);
		var previous = GetHistoryValue(_shortValues, previousOffset);

		if (current is null || previous is null)
		return;

		if (ShortAllowClose && current > 0m && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			CompleteShortTrade(candle.ClosePrice);
		}

		if (ShortAllowOpen && current < 0m && previous >= 0m)
		{
			if (Position > 0)
			{
				SellMarket(Position);
				CompleteLongTrade(candle.ClosePrice);
			}

			if (Position >= 0)
			{
				var volume = GetShortTradeVolume();
				if (volume > 0m)
				{
					SellMarket(volume);
					RegisterShortEntry(candle.ClosePrice, volume);
				}
			}
		}
	}

	private bool TryHandleLongRisk(ICandleMessage candle, decimal entryPrice)
	{
		var step = GetPriceStep();
		decimal? exitPrice = null;
		var isLoss = false;

		if (LongStopLoss > 0m)
		{
			var stopDistance = LongStopLoss * step;
			if (stopDistance > 0m && candle.LowPrice <= entryPrice - stopDistance)
			{
				exitPrice = entryPrice - stopDistance;
				isLoss = true;
			}
		}

		if (exitPrice is null && LongTakeProfit > 0m)
		{
			var takeDistance = LongTakeProfit * step;
			if (takeDistance > 0m && candle.HighPrice >= entryPrice + takeDistance)
			{
				exitPrice = entryPrice + takeDistance;
				isLoss = false;
			}
		}

		if (exitPrice is decimal finalPrice)
		{
			SellMarket(Position);
			CompleteLongTrade(finalPrice, isLoss);
			return true;
		}

		return false;
	}

	private bool TryHandleShortRisk(ICandleMessage candle, decimal entryPrice)
	{
		var step = GetPriceStep();
		decimal? exitPrice = null;
		var isLoss = false;

		if (ShortStopLoss > 0m)
		{
			var stopDistance = ShortStopLoss * step;
			if (stopDistance > 0m && candle.HighPrice >= entryPrice + stopDistance)
			{
				exitPrice = entryPrice + stopDistance;
				isLoss = true;
			}
		}

		if (exitPrice is null && ShortTakeProfit > 0m)
		{
			var takeDistance = ShortTakeProfit * step;
			if (takeDistance > 0m && candle.LowPrice <= entryPrice - takeDistance)
			{
				exitPrice = entryPrice - takeDistance;
				isLoss = false;
			}
		}

		if (exitPrice is decimal finalPrice)
		{
			BuyMarket(Math.Abs(Position));
			CompleteShortTrade(finalPrice, isLoss);
			return true;
		}

		return false;
	}

	private void RegisterLongEntry(decimal price, decimal volume)
	{
		_longEntryPrice = price;
		_longEntryVolume = volume;
	}

	private void RegisterShortEntry(decimal price, decimal volume)
	{
		_shortEntryPrice = price;
		_shortEntryVolume = volume;
	}

	private void CompleteLongTrade(decimal exitPrice)
	{
		if (_longEntryPrice is not decimal entryPrice)
		return;

		var isLoss = exitPrice < entryPrice;
		CompleteLongTrade(exitPrice, isLoss);
	}

	private void CompleteLongTrade(decimal exitPrice, bool isLoss)
	{
		RecordResult(_longResults, isLoss, LongTotalTrigger);
		_longEntryPrice = null;
		_longEntryVolume = 0m;
	}

	private void CompleteShortTrade(decimal exitPrice)
	{
		if (_shortEntryPrice is not decimal entryPrice)
		return;

		var isLoss = exitPrice > entryPrice;
		CompleteShortTrade(exitPrice, isLoss);
	}

	private void CompleteShortTrade(decimal exitPrice, bool isLoss)
	{
		RecordResult(_shortResults, isLoss, ShortTotalTrigger);
		_shortEntryPrice = null;
		_shortEntryVolume = 0m;
	}

	private decimal GetLongTradeVolume()
	{
		var losses = _longResults.Count(r => r);
		if (LongLossTrigger > 0 && losses >= LongLossTrigger)
		return LongSmallVolume;

		return LongNormalVolume;
	}

	private decimal GetShortTradeVolume()
	{
		var losses = _shortResults.Count(r => r);
		if (ShortLossTrigger > 0 && losses >= ShortLossTrigger)
		return ShortSmallVolume;

		return ShortNormalVolume;
	}

	private static void RecordResult(Queue<bool> results, bool isLoss, int maxCount)
	{
		results.Enqueue(isLoss);

		var limit = Math.Max(1, maxCount);
		while (results.Count > limit)
		results.Dequeue();
	}

	private static void UpdateHistory(List<decimal> history, decimal value, int signalBar)
	{
		history.Add(value);

		var maxCount = Math.Max(signalBar + 3, 4);
		while (history.Count > maxCount)
		history.RemoveAt(0);
	}

	private static decimal? GetHistoryValue(List<decimal> history, int offset)
	{
		if (offset < 0)
		offset = 0;

		var indexFromEnd = offset + 1;
		if (history.Count < indexFromEnd)
		return null;

		return history[^indexFromEnd];
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep;
		return step is { } value && value > 0m ? value : 1m;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPrices price)
	{
		return price switch
		{
			AppliedPrices.Open => candle.OpenPrice,
			AppliedPrices.High => candle.HighPrice,
			AppliedPrices.Low => candle.LowPrice,
			AppliedPrices.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrices.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrices.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			AppliedPrices.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPrices.Quarter => (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m,
			AppliedPrices.TrendFollow0 => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
			AppliedPrices.TrendFollow1 => (candle.HighPrice + candle.LowPrice + candle.OpenPrice + candle.OpenPrice) / 4m,
			AppliedPrices.DeMark => candle.ClosePrice < candle.OpenPrice
			? (candle.HighPrice + 2m * candle.LowPrice + candle.ClosePrice) / 4m
			: candle.ClosePrice > candle.OpenPrice
			? (2m * candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m
			: (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private static decimal CalculateSmoothingFactor(int phase)
	{
		var factor = 0.5m + (phase - 100m) / 400m;
		if (factor < 0.05m)
		factor = 0.05m;
		if (factor > 0.95m)
		factor = 0.95m;
		return factor;
	}

	private enum AppliedPrices
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted,
		Simple,
		Quarter,
		TrendFollow0,
		TrendFollow1,
		DeMark
	}

	private sealed class SchaffTrendCycleCalculator
	{
		private readonly JccxCalculator _fast;
		private readonly JccxCalculator _slow;
		private readonly Highest _macdHigh;
		private readonly Lowest _macdLow;
		private readonly Highest _stHigh;
		private readonly Lowest _stLow;
		private readonly decimal _smoothingFactor;

		private decimal? _previousSt;
		private decimal? _previousStc;

		public SchaffTrendCycleCalculator(int fastLength, int slowLength, int smoothLength, int cycleLength, decimal smoothingFactor)
		{
			_fast = new JccxCalculator(Math.Max(1, fastLength), Math.Max(1, smoothLength));
			_slow = new JccxCalculator(Math.Max(1, slowLength), Math.Max(1, smoothLength));
			_macdHigh = new Highest { Length = Math.Max(2, cycleLength) };
			_macdLow = new Lowest { Length = Math.Max(2, cycleLength) };
			_stHigh = new Highest { Length = Math.Max(2, cycleLength) };
			_stLow = new Lowest { Length = Math.Max(2, cycleLength) };
			_smoothingFactor = smoothingFactor;
		}

		public decimal? Process(decimal price, DateTimeOffset time)
		{
			var fast = _fast.Process(price, time);
			var slow = _slow.Process(price, time);

			if (fast is null || slow is null)
			return null;

			var macd = fast.Value - slow.Value;

			var macdHighValue = _macdHigh.Process(new DecimalIndicatorValue(_macdHigh, macd, time));
			var macdLowValue = _macdLow.Process(new DecimalIndicatorValue(_macdLow, macd, time));

			if (!_macdHigh.IsFormed || !_macdLow.IsFormed)
			return null;

			var macdHigh = macdHighValue.ToDecimal();
			var macdLow = macdLowValue.ToDecimal();

			decimal st;
			if (macdHigh - macdLow != 0m)
			st = ((macd - macdLow) / (macdHigh - macdLow)) * 100m;
			else if (_previousSt.HasValue)
			st = _previousSt.Value;
			else
			return null;

			if (_previousSt.HasValue)
			st = _smoothingFactor * (st - _previousSt.Value) + _previousSt.Value;

			_previousSt = st;

			var stHighValue = _stHigh.Process(new DecimalIndicatorValue(_stHigh, st, time));
			var stLowValue = _stLow.Process(new DecimalIndicatorValue(_stLow, st, time));

			if (!_stHigh.IsFormed || !_stLow.IsFormed)
			return null;

			var stHigh = stHighValue.ToDecimal();
			var stLow = stLowValue.ToDecimal();

			decimal stc;
			if (stHigh - stLow != 0m)
			stc = ((st - stLow) / (stHigh - stLow)) * 200m - 100m;
			else if (_previousStc.HasValue)
			stc = _previousStc.Value;
			else
			return null;

			if (_previousStc.HasValue)
			stc = _smoothingFactor * (stc - _previousStc.Value) + _previousStc.Value;

			_previousStc = stc;

			return stc;
		}

		public void Reset()
		{
			_fast.Reset();
			_slow.Reset();
			_macdHigh.Reset();
			_macdLow.Reset();
			_stHigh.Reset();
			_stLow.Reset();
			_previousSt = null;
			_previousStc = null;
		}
	}

	private sealed class JccxCalculator
	{
		private readonly JurikMovingAverage _jma;
		private readonly JurikMovingAverage _upSmooth;
		private readonly JurikMovingAverage _downSmooth;

		public JccxCalculator(int jmaLength, int jurikLength)
		{
			_jma = new JurikMovingAverage { Length = jmaLength };
			_upSmooth = new JurikMovingAverage { Length = jurikLength };
			_downSmooth = new JurikMovingAverage { Length = jurikLength };
		}

		public decimal? Process(decimal price, DateTimeOffset time)
		{
			var jmaValue = _jma.Process(new DecimalIndicatorValue(_jma, price, time));
			if (!_jma.IsFormed)
			return null;

			var jma = jmaValue.ToDecimal();
			var up = price - jma;
			var down = Math.Abs(up);

			var upValue = _upSmooth.Process(new DecimalIndicatorValue(_upSmooth, up, time));
			var downValue = _downSmooth.Process(new DecimalIndicatorValue(_downSmooth, down, time));

			if (!_upSmooth.IsFormed || !_downSmooth.IsFormed)
			return null;

			var denominator = downValue.ToDecimal();
			if (denominator == 0m)
			return null;

			var ratio = upValue.ToDecimal() / denominator;

			if (ratio > 1m)
			ratio = 1m;
			else if (ratio < -1m)
			ratio = -1m;

			return ratio;
		}

		public void Reset()
		{
			_jma.Reset();
			_upSmooth.Reset();
			_downSmooth.Reset();
		}
	}
}

