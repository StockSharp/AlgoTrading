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
/// Conversion of the Exp_ColorSchaffJJRSXTrendCycle_MMRec_Duplex MetaTrader strategy.
/// Combines dual Schaff-style oscillators with a loss-aware position sizing overlay.
/// </summary>
public class ColorSchaffJjrsxMmrecDuplexStrategy : Strategy
{
	public enum AppliedPrices
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted
	}

	private readonly StrategyParam<decimal> _factor;

	private readonly StrategyParam<DataType> _longCandleType;
	private readonly StrategyParam<int> _longTotalTrigger;
	private readonly StrategyParam<int> _longLossTrigger;
	private readonly StrategyParam<decimal> _longSmallMm;
	private readonly StrategyParam<decimal> _longMm;
	private readonly StrategyParam<bool> _longEnableOpen;
	private readonly StrategyParam<bool> _longEnableClose;
	private readonly StrategyParam<int> _longFast;
	private readonly StrategyParam<int> _longSlow;
	private readonly StrategyParam<int> _longSmooth;
	private readonly StrategyParam<int> _longCycle;
	private readonly StrategyParam<int> _longSignalBar;
	private readonly StrategyParam<AppliedPrices> _longPriceType;

	private readonly StrategyParam<DataType> _shortCandleType;
	private readonly StrategyParam<int> _shortTotalTrigger;
	private readonly StrategyParam<int> _shortLossTrigger;
	private readonly StrategyParam<decimal> _shortSmallMm;
	private readonly StrategyParam<decimal> _shortMm;
	private readonly StrategyParam<bool> _shortEnableOpen;
	private readonly StrategyParam<bool> _shortEnableClose;
	private readonly StrategyParam<int> _shortFast;
	private readonly StrategyParam<int> _shortSlow;
	private readonly StrategyParam<int> _shortSmooth;
	private readonly StrategyParam<int> _shortCycle;
	private readonly StrategyParam<int> _shortSignalBar;
	private readonly StrategyParam<AppliedPrices> _shortPriceType;

	private ColorSchaffJjrsxTrendCycleIndicator _longIndicator;
	private ColorSchaffJjrsxTrendCycleIndicator _shortIndicator;

	private readonly List<decimal> _longHistory = new();
	private readonly List<decimal> _shortHistory = new();

	private readonly Queue<bool> _longRecentLosses = new();
	private readonly Queue<bool> _shortRecentLosses = new();

	private int _longLossCount;
	private int _shortLossCount;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="ColorSchaffJjrsxMmrecDuplexStrategy"/> class.
	/// </summary>
	public ColorSchaffJjrsxMmrecDuplexStrategy()
	{
		_factor = Param(nameof(Factor), 0.5m)
			.SetDisplay("Smoothing Factor", "Multiplier used for trend filtering", "General")
			.SetRange(0.01m, 5m)
			.SetCanOptimize(true);

		_longCandleType = Param(nameof(LongCandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Long Candle", "Time-frame used for the long indicator", "Long")
			.SetCanOptimize(false);

		_longTotalTrigger = Param(nameof(LongTotalTrigger), 5)
			.SetDisplay("Long Total Trigger", "Number of recent long trades to inspect", "Long")
			.SetRange(1, 20)
			.SetCanOptimize(true);

		_longLossTrigger = Param(nameof(LongLossTrigger), 3)
			.SetDisplay("Long Loss Trigger", "Losses required to scale long position size down", "Long")
			.SetRange(1, 10)
			.SetCanOptimize(true);

		_longSmallMm = Param(nameof(LongSmallMm), 0.01m)
			.SetDisplay("Long Reduced Multiplier", "Multiplier applied after repeated long losses", "Long")
			.SetRange(0.001m, 1m)
			.SetCanOptimize(true);

		_longMm = Param(nameof(LongMm), 0.1m)
			.SetDisplay("Long Base Multiplier", "Default long position size multiplier", "Long")
			.SetRange(0.001m, 2m)
			.SetCanOptimize(true);

		_longEnableOpen = Param(nameof(LongEnableOpen), true)
			.SetDisplay("Long Entries", "Enable opening long positions", "Long");

		_longEnableClose = Param(nameof(LongEnableClose), true)
			.SetDisplay("Long Exits", "Enable closing long positions", "Long");

		_longFast = Param(nameof(LongFastLength), 23)
			.SetDisplay("Long Fast Length", "Fast RSX approximation period", "Long")
			.SetRange(5, 100)
			.SetCanOptimize(true);

		_longSlow = Param(nameof(LongSlowLength), 50)
			.SetDisplay("Long Slow Length", "Slow RSX approximation period", "Long")
			.SetRange(5, 150)
			.SetCanOptimize(true);

		_longSmooth = Param(nameof(LongSmooth), 8)
			.SetDisplay("Long Smoothing", "Exponential smoothing applied to RSX values", "Long")
			.SetRange(1, 50)
			.SetCanOptimize(true);

		_longCycle = Param(nameof(LongCycleLength), 10)
			.SetDisplay("Long Cycle", "Window used for Schaff normalization", "Long")
			.SetRange(3, 50)
			.SetCanOptimize(true);

		_longSignalBar = Param(nameof(LongSignalBar), 1)
			.SetDisplay("Long Signal Bar", "Shift used when evaluating long signals", "Long")
			.SetRange(0, 5)
			.SetCanOptimize(true);

		_longPriceType = Param(nameof(LongAppliedPrice), AppliedPrices.Close)
			.SetDisplay("Long Applied Price", "Price source for the long indicator", "Long");

		_shortCandleType = Param(nameof(ShortCandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Short Candle", "Time-frame used for the short indicator", "Short")
			.SetCanOptimize(false);

		_shortTotalTrigger = Param(nameof(ShortTotalTrigger), 5)
			.SetDisplay("Short Total Trigger", "Number of recent short trades to inspect", "Short")
			.SetRange(1, 20)
			.SetCanOptimize(true);

		_shortLossTrigger = Param(nameof(ShortLossTrigger), 3)
			.SetDisplay("Short Loss Trigger", "Losses required to scale short position size down", "Short")
			.SetRange(1, 10)
			.SetCanOptimize(true);

		_shortSmallMm = Param(nameof(ShortSmallMm), 0.01m)
			.SetDisplay("Short Reduced Multiplier", "Multiplier applied after repeated short losses", "Short")
			.SetRange(0.001m, 1m)
			.SetCanOptimize(true);

		_shortMm = Param(nameof(ShortMm), 0.1m)
			.SetDisplay("Short Base Multiplier", "Default short position size multiplier", "Short")
			.SetRange(0.001m, 2m)
			.SetCanOptimize(true);

		_shortEnableOpen = Param(nameof(ShortEnableOpen), true)
			.SetDisplay("Short Entries", "Enable opening short positions", "Short");

		_shortEnableClose = Param(nameof(ShortEnableClose), true)
			.SetDisplay("Short Exits", "Enable closing short positions", "Short");

		_shortFast = Param(nameof(ShortFastLength), 23)
			.SetDisplay("Short Fast Length", "Fast RSX approximation period", "Short")
			.SetRange(5, 100)
			.SetCanOptimize(true);

		_shortSlow = Param(nameof(ShortSlowLength), 50)
			.SetDisplay("Short Slow Length", "Slow RSX approximation period", "Short")
			.SetRange(5, 150)
			.SetCanOptimize(true);

		_shortSmooth = Param(nameof(ShortSmooth), 8)
			.SetDisplay("Short Smoothing", "Exponential smoothing applied to RSX values", "Short")
			.SetRange(1, 50)
			.SetCanOptimize(true);

		_shortCycle = Param(nameof(ShortCycleLength), 10)
			.SetDisplay("Short Cycle", "Window used for Schaff normalization", "Short")
			.SetRange(3, 50)
			.SetCanOptimize(true);

		_shortSignalBar = Param(nameof(ShortSignalBar), 1)
			.SetDisplay("Short Signal Bar", "Shift used when evaluating short signals", "Short")
			.SetRange(0, 5)
			.SetCanOptimize(true);

		_shortPriceType = Param(nameof(ShortAppliedPrice), AppliedPrices.Close)
			.SetDisplay("Short Applied Price", "Price source for the short indicator", "Short");
	}

	/// <summary>
	/// Multiplier applied to smooth the duplex indicators.
	/// </summary>
	public decimal Factor
	{
		get => _factor.Value;
		set => _factor.Value = value;
	}

	/// <summary>
	/// Time-frame used for the long indicator.
	/// </summary>
	public DataType LongCandleType
	{
		get => _longCandleType.Value;
		set => _longCandleType.Value = value;
	}

	/// <summary>
	/// Number of recent long trades to inspect when evaluating the position size multiplier.
	/// </summary>
	public int LongTotalTrigger
	{
		get => _longTotalTrigger.Value;
		set => _longTotalTrigger.Value = value;
	}

	/// <summary>
	/// Losses required to switch the long multiplier to the reduced value.
	/// </summary>
	public int LongLossTrigger
	{
		get => _longLossTrigger.Value;
		set => _longLossTrigger.Value = value;
	}

	/// <summary>
	/// Long multiplier used after repeated losses.
	/// </summary>
	public decimal LongSmallMm
	{
		get => _longSmallMm.Value;
		set => _longSmallMm.Value = value;
	}

	/// <summary>
	/// Default long position size multiplier.
	/// </summary>
	public decimal LongMm
	{
		get => _longMm.Value;
		set => _longMm.Value = value;
	}

	/// <summary>
	/// Enables opening long positions.
	/// </summary>
	public bool LongEnableOpen
	{
		get => _longEnableOpen.Value;
		set => _longEnableOpen.Value = value;
	}

	/// <summary>
	/// Enables closing long positions.
	/// </summary>
	public bool LongEnableClose
	{
		get => _longEnableClose.Value;
		set => _longEnableClose.Value = value;
	}

	/// <summary>
	/// Fast RSX approximation period for the long side.
	/// </summary>
	public int LongFastLength
	{
		get => _longFast.Value;
		set => _longFast.Value = value;
	}

	/// <summary>
	/// Slow RSX approximation period for the long side.
	/// </summary>
	public int LongSlowLength
	{
		get => _longSlow.Value;
		set => _longSlow.Value = value;
	}

	/// <summary>
	/// Exponential smoothing applied to long RSX values.
	/// </summary>
	public int LongSmooth
	{
		get => _longSmooth.Value;
		set => _longSmooth.Value = value;
	}

	/// <summary>
	/// Window used for Schaff normalization on the long side.
	/// </summary>
	public int LongCycleLength
	{
		get => _longCycle.Value;
		set => _longCycle.Value = value;
	}

	/// <summary>
	/// Shift used when evaluating long signals.
	/// </summary>
	public int LongSignalBar
	{
		get => _longSignalBar.Value;
		set => _longSignalBar.Value = value;
	}

	/// <summary>
	/// Price source used by the long indicator.
	/// </summary>
	public AppliedPrices LongAppliedPrice
	{
		get => _longPriceType.Value;
		set => _longPriceType.Value = value;
	}

	/// <summary>
	/// Time-frame used for the short indicator.
	/// </summary>
	public DataType ShortCandleType
	{
		get => _shortCandleType.Value;
		set => _shortCandleType.Value = value;
	}

	/// <summary>
	/// Number of recent short trades to inspect when evaluating the position size multiplier.
	/// </summary>
	public int ShortTotalTrigger
	{
		get => _shortTotalTrigger.Value;
		set => _shortTotalTrigger.Value = value;
	}

	/// <summary>
	/// Losses required to switch the short multiplier to the reduced value.
	/// </summary>
	public int ShortLossTrigger
	{
		get => _shortLossTrigger.Value;
		set => _shortLossTrigger.Value = value;
	}

	/// <summary>
	/// Short multiplier used after repeated losses.
	/// </summary>
	public decimal ShortSmallMm
	{
		get => _shortSmallMm.Value;
		set => _shortSmallMm.Value = value;
	}

	/// <summary>
	/// Default short position size multiplier.
	/// </summary>
	public decimal ShortMm
	{
		get => _shortMm.Value;
		set => _shortMm.Value = value;
	}

	/// <summary>
	/// Enables opening short positions.
	/// </summary>
	public bool ShortEnableOpen
	{
		get => _shortEnableOpen.Value;
		set => _shortEnableOpen.Value = value;
	}

	/// <summary>
	/// Enables closing short positions.
	/// </summary>
	public bool ShortEnableClose
	{
		get => _shortEnableClose.Value;
		set => _shortEnableClose.Value = value;
	}

	/// <summary>
	/// Fast RSX approximation period for the short side.
	/// </summary>
	public int ShortFastLength
	{
		get => _shortFast.Value;
		set => _shortFast.Value = value;
	}

	/// <summary>
	/// Slow RSX approximation period for the short side.
	/// </summary>
	public int ShortSlowLength
	{
		get => _shortSlow.Value;
		set => _shortSlow.Value = value;
	}

	/// <summary>
	/// Exponential smoothing applied to short RSX values.
	/// </summary>
	public int ShortSmooth
	{
		get => _shortSmooth.Value;
		set => _shortSmooth.Value = value;
	}

	/// <summary>
	/// Window used for Schaff normalization on the short side.
	/// </summary>
	public int ShortCycleLength
	{
		get => _shortCycle.Value;
		set => _shortCycle.Value = value;
	}

	/// <summary>
	/// Shift used when evaluating short signals.
	/// </summary>
	public int ShortSignalBar
	{
		get => _shortSignalBar.Value;
		set => _shortSignalBar.Value = value;
	}

	/// <summary>
	/// Price source used by the short indicator.
	/// </summary>
	public AppliedPrices ShortAppliedPrice
	{
		get => _shortPriceType.Value;
		set => _shortPriceType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
		(Security, LongCandleType),
		(Security, ShortCandleType)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longIndicator = null;
		_shortIndicator = null;

		_longHistory.Clear();
		_shortHistory.Clear();

		_longRecentLosses.Clear();
		_shortRecentLosses.Clear();

		_longLossCount = 0;
		_shortLossCount = 0;

		_longEntryPrice = null;
		_shortEntryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_longIndicator = new ColorSchaffJjrsxTrendCycleIndicator
		{
			FastLength = LongFastLength,
			SlowLength = LongSlowLength,
			SmoothLength = LongSmooth,
			CycleLength = LongCycleLength,
			AppliedPrice = LongAppliedPrice,
			SmoothingFactor = Factor
		};

		_shortIndicator = new ColorSchaffJjrsxTrendCycleIndicator
		{
			FastLength = ShortFastLength,
			SlowLength = ShortSlowLength,
			SmoothLength = ShortSmooth,
			CycleLength = ShortCycleLength,
			AppliedPrice = ShortAppliedPrice,
			SmoothingFactor = Factor
		};

		var longSubscription = SubscribeCandles(LongCandleType);
		longSubscription
			.Bind(_longIndicator, ProcessLongCandle)
			.Start();

		var shortSubscription = SubscribeCandles(ShortCandleType);
		shortSubscription
			.Bind(_shortIndicator, ProcessShortCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, longSubscription);
			DrawIndicator(area, _longIndicator!);
			DrawIndicator(area, _shortIndicator!);
			DrawOwnTrades(area);
		}
	}

	private void ProcessLongCandle(ICandleMessage candle, decimal indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_longIndicator is null || !_longIndicator.IsFormed)
			return;

		StoreLongValue(indicatorValue);

		if (_longHistory.Count < LongSignalBar + 2)
		return;

		var current = _longHistory[^ (LongSignalBar + 1)];
		var previous = _longHistory[^ (LongSignalBar + 2)];

		var shouldOpen = LongEnableOpen && previous > 0m && current <= 0m;
		var shouldClose = LongEnableClose && previous < 0m;

		if (shouldClose && Position > 0m)
		{
			SellMarket(Position);
			FinalizeLongTrade(candle.ClosePrice);
		}

		if (shouldOpen && Position <= 0m)
		{
			if (Position < 0m)
			FinalizeShortTrade(candle.ClosePrice);

			var volume = CalculateLongVolume();
			var totalVolume = volume + Math.Max(0m, -Position);

			if (totalVolume > 0m)
			{
				BuyMarket(totalVolume);
				_longEntryPrice = candle.ClosePrice;
			}
		}
	}

	private void ProcessShortCandle(ICandleMessage candle, decimal indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_shortIndicator is null || !_shortIndicator.IsFormed)
			return;

		StoreShortValue(indicatorValue);

		if (_shortHistory.Count < ShortSignalBar + 2)
		return;

		var current = _shortHistory[^ (ShortSignalBar + 1)];
		var previous = _shortHistory[^ (ShortSignalBar + 2)];

		var shouldOpen = ShortEnableOpen && previous < 0m && current >= 0m;
		var shouldClose = ShortEnableClose && previous > 0m;

		if (shouldClose && Position < 0m)
		{
			BuyMarket(-Position);
			FinalizeShortTrade(candle.ClosePrice);
		}

		if (shouldOpen && Position >= 0m)
		{
			if (Position > 0m)
			FinalizeLongTrade(candle.ClosePrice);

			var volume = CalculateShortVolume();
			var totalVolume = volume + Math.Max(0m, Position);

			if (totalVolume > 0m)
			{
				SellMarket(totalVolume);
				_shortEntryPrice = candle.ClosePrice;
			}
		}
	}

	private void StoreLongValue(decimal value)
	{
		_longHistory.Add(value);
		TrimHistory(_longHistory, LongSignalBar + 2);
	}

	private void StoreShortValue(decimal value)
	{
		_shortHistory.Add(value);
		TrimHistory(_shortHistory, ShortSignalBar + 2);
	}

	private static void TrimHistory(List<decimal> history, int minSize)
	{
		var limit = Math.Max(minSize, 10);
		while (history.Count > limit)
		history.RemoveAt(0);
	}

	private decimal CalculateLongVolume()
	{
		var mm = LongMm;

		if (LongLossTrigger > 0 && _longLossCount >= LongLossTrigger)
		mm = LongSmallMm;

		var baseVolume = Volume * mm;
		return Math.Max(0m, baseVolume);
	}

	private decimal CalculateShortVolume()
	{
		var mm = ShortMm;

		if (ShortLossTrigger > 0 && _shortLossCount >= ShortLossTrigger)
		mm = ShortSmallMm;

		var baseVolume = Volume * mm;
		return Math.Max(0m, baseVolume);
	}

	private void FinalizeLongTrade(decimal exitPrice)
	{
		if (_longEntryPrice is not decimal entry)
		{
			_longEntryPrice = null;
			return;
		}

		var isLoss = exitPrice < entry;
		RecordLongResult(isLoss);
		_longEntryPrice = null;
	}

	private void FinalizeShortTrade(decimal exitPrice)
	{
		if (_shortEntryPrice is not decimal entry)
		{
			_shortEntryPrice = null;
			return;
		}

		var isLoss = exitPrice > entry;
		RecordShortResult(isLoss);
		_shortEntryPrice = null;
	}

	private void RecordLongResult(bool isLoss)
	{
		if (LongTotalTrigger <= 0)
		{
			_longLossCount = isLoss ? 1 : 0;
			_longRecentLosses.Clear();
			if (isLoss)
			_longRecentLosses.Enqueue(true);
			return;
		}

		_longRecentLosses.Enqueue(isLoss);
		if (isLoss)
		_longLossCount++;

		while (_longRecentLosses.Count > LongTotalTrigger)
		{
			if (_longRecentLosses.Dequeue())
			_longLossCount--;
		}
	}

	private void RecordShortResult(bool isLoss)
	{
		if (ShortTotalTrigger <= 0)
		{
			_shortLossCount = isLoss ? 1 : 0;
			_shortRecentLosses.Clear();
			if (isLoss)
			_shortRecentLosses.Enqueue(true);
			return;
		}

		_shortRecentLosses.Enqueue(isLoss);
		if (isLoss)
		_shortLossCount++;

		while (_shortRecentLosses.Count > ShortTotalTrigger)
		{
			if (_shortRecentLosses.Dequeue())
			_shortLossCount--;
		}
	}
}

/// <summary>
/// Simplified approximation of the ColorSchaffJJRSXTrendCycle oscillator.
/// Combines RSI-style momentum with Schaff Trend Cycle normalization.
/// </summary>
public class ColorSchaffJjrsxTrendCycleIndicator : BaseIndicator<decimal>
{
	private readonly SimpleRsi _fastRsi = new();
	private readonly SimpleRsi _slowRsi = new();
	private readonly ExponentialMovingAverage _fastSmooth = new();
	private readonly ExponentialMovingAverage _slowSmooth = new();

	private readonly Queue<decimal> _macdValues = new();
	private readonly Queue<decimal> _stValues = new();

	private decimal? _prevSt;
	private decimal? _prevStc;
	private bool _stInitialized;
	private bool _stcInitialized;

	/// <summary>
	/// Gets or sets the fast RSX approximation period.
	/// </summary>
	public int FastLength
	{
		get => _fastRsi.Length;
		set => _fastRsi.Length = Math.Max(1, value);
	}

	/// <summary>
	/// Gets or sets the slow RSX approximation period.
	/// </summary>
	public int SlowLength
	{
		get => _slowRsi.Length;
		set => _slowRsi.Length = Math.Max(1, value);
	}

	/// <summary>
	/// Gets or sets the exponential smoothing length applied to RSX values.
	/// </summary>
	public int SmoothLength
	{
		get => _fastSmooth.Length;
		set
		{
			_fastSmooth.Length = Math.Max(1, value);
			_slowSmooth.Length = Math.Max(1, value);
		}
	}

	/// <summary>
	/// Gets or sets the normalization window used inside the Schaff calculation.
	/// </summary>
	public int CycleLength { get; set; } = 10;

	/// <summary>
	/// Gets or sets the price source used in calculations.
	/// </summary>
	public AppliedPrice AppliedPrice { get; set; } = AppliedPrice.Close;

	/// <summary>
	/// Gets or sets the exponential smoothing factor applied between stages.
	/// </summary>
	public decimal SmoothingFactor { get; set; } = 0.5m;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
		return new DecimalIndicatorValue(this, default, input.Time);

		var price = SelectPrice(candle);

		var fast = _fastRsi.Process(price);
		var slow = _slowRsi.Process(price);

		if (!fast.IsReady || !slow.IsReady)
		return new DecimalIndicatorValue(this, default, input.Time);

		var fastSmooth = _fastSmooth.Process(new DecimalIndicatorValue(_fastSmooth, fast.Value, input.Time)).ToDecimal();
		var slowSmooth = _slowSmooth.Process(new DecimalIndicatorValue(_slowSmooth, slow.Value, input.Time)).ToDecimal();

		var macd = (fastSmooth - 50m) - (slowSmooth - 50m);

		_macdValues.Enqueue(macd);
		while (_macdValues.Count > Math.Max(2, CycleLength))
		_macdValues.Dequeue();

		if (_macdValues.Count < Math.Max(2, CycleLength))
		return new DecimalIndicatorValue(this, default, input.Time);

		var llv = _macdValues.Min();
		var hhv = _macdValues.Max();

		decimal st;
		if (hhv != llv)
		st = (macd - llv) / (hhv - llv) * 100m;
		else
		st = _prevSt ?? macd;

		if (_stInitialized && _prevSt is decimal prevSt)
		st = SmoothingFactor * (st - prevSt) + prevSt;
		else
		_stInitialized = true;

		_prevSt = st;

		_stValues.Enqueue(st);
		while (_stValues.Count > Math.Max(2, CycleLength))
		_stValues.Dequeue();

		if (_stValues.Count < Math.Max(2, CycleLength))
		return new DecimalIndicatorValue(this, default, input.Time);

		var stLlv = _stValues.Min();
		var stHhv = _stValues.Max();

		decimal stc;
		if (stHhv != stLlv)
		stc = (st - stLlv) / (stHhv - stLlv) * 200m - 100m;
		else
		stc = _prevStc ?? st;

		if (_stcInitialized && _prevStc is decimal prevStc)
		stc = SmoothingFactor * (stc - prevStc) + prevStc;
		else
		_stcInitialized = true;

		_prevStc = stc;
		IsFormed = true;

		return new DecimalIndicatorValue(this, stc, input.Time);
	}

	/// <inheritdoc />
	protected override void OnReset()
	{
		base.OnReset();

		_fastRsi.Reset();
		_slowRsi.Reset();
		_fastSmooth.Reset();
		_slowSmooth.Reset();

		_macdValues.Clear();
		_stValues.Clear();

		_prevSt = null;
		_prevStc = null;
		_stInitialized = false;
		_stcInitialized = false;
	}

	private decimal SelectPrice(ICandleMessage candle)
	{
		return AppliedPrice switch
		{
			AppliedPrice.Close => candle.ClosePrice,
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrice.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
			AppliedPrice.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPrice.TrendFollow0 => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
			AppliedPrice.TrendFollow1 => (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m,
			AppliedPrice.DeMark => (candle.ClosePrice + candle.HighPrice + candle.LowPrice + candle.LowPrice) / 4m,
			_ => candle.ClosePrice
		};
	}
}

/// <summary>
/// Lightweight RSI implementation used to approximate JJRSX behaviour.
/// </summary>
public class SimpleRsi
{
	private decimal? _previousPrice;
	private decimal _avgGain;
	private decimal _avgLoss;
	private int _counter;

	/// <summary>
	/// Gets or sets the RSI period.
	/// </summary>
	public int Length { get; set; } = 14;

	/// <summary>
	/// Processes the next price sample.
	/// </summary>
	public (bool IsReady, decimal Value) Process(decimal price)
	{
		if (_previousPrice is null)
		{
			_previousPrice = price;
			return (false, 50m);
		}

		var change = price - _previousPrice.Value;
		_previousPrice = price;

		var gain = change > 0m ? change : 0m;
		var loss = change < 0m ? -change : 0m;

		if (_counter < Length)
		{
			_avgGain += gain;
			_avgLoss += loss;
			_counter++;

			if (_counter == Length)
			{
				_avgGain /= Length;
				_avgLoss /= Length;
			}

			return (false, 50m);
		}

		_avgGain = ((_avgGain * (Length - 1)) + gain) / Length;
		_avgLoss = ((_avgLoss * (Length - 1)) + loss) / Length;

		if (_avgLoss == 0m)
		return (true, 100m);

		var rs = _avgGain / _avgLoss;
		var rsi = 100m - 100m / (1m + rs);
		return (true, rsi);
	}

	/// <summary>
	/// Resets the internal state.
	/// </summary>
	public void Reset()
	{
		_previousPrice = null;
		_avgGain = 0m;
		_avgLoss = 0m;
		_counter = 0;
	}
}

