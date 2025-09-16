using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Combined strategy that merges BykovTrend and ColorX2MA entries with simplified money management.
/// Tracks trend color changes from BykovTrend (WPR based) and slope direction of a double-smoothed MA.
/// </summary>
public class BykovTrendColorX2MaMmRecStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableBykovTrendBuy;
	private readonly StrategyParam<bool> _enableBykovTrendSell;
	private readonly StrategyParam<bool> _allowBykovTrendCloseBuy;
	private readonly StrategyParam<bool> _allowBykovTrendCloseSell;
	private readonly StrategyParam<int> _bykovTrendRisk;
	private readonly StrategyParam<int> _bykovTrendWprLength;
	private readonly StrategyParam<int> _bykovTrendSignalBar;
	private readonly StrategyParam<DataType> _bykovTrendCandleType;

	private readonly StrategyParam<bool> _enableColorX2MaBuy;
	private readonly StrategyParam<bool> _enableColorX2MaSell;
	private readonly StrategyParam<bool> _allowColorX2MaCloseBuy;
	private readonly StrategyParam<bool> _allowColorX2MaCloseSell;
	private readonly StrategyParam<ColorX2MaSmoothingMethod> _colorX2MaMethod1;
	private readonly StrategyParam<int> _colorX2MaLength1;
	private readonly StrategyParam<int> _colorX2MaPhase1;
	private readonly StrategyParam<ColorX2MaSmoothingMethod> _colorX2MaMethod2;
	private readonly StrategyParam<int> _colorX2MaLength2;
	private readonly StrategyParam<int> _colorX2MaPhase2;
	private readonly StrategyParam<PriceType> _colorX2MaPriceType;
	private readonly StrategyParam<int> _colorX2MaSignalBar;
	private readonly StrategyParam<DataType> _colorX2MaCandleType;

	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private WilliamsR _bykovTrendWpr = null!;
	private LengthIndicator<decimal> _colorX2MaStage1 = null!;
	private LengthIndicator<decimal> _colorX2MaStage2 = null!;

	private readonly List<int> _bykovTrendColors = new();
	private readonly List<int> _colorX2MaStates = new();

	private int _bykovTrendTrend;
	private decimal? _prevColorX2MaValue;

	/// <summary>
	/// Enable long entries from the BykovTrend module.
	/// </summary>
	public bool EnableBykovTrendBuy
	{
		get => _enableBykovTrendBuy.Value;
		set => _enableBykovTrendBuy.Value = value;
	}

	/// <summary>
	/// Enable short entries from the BykovTrend module.
	/// </summary>
	public bool EnableBykovTrendSell
	{
		get => _enableBykovTrendSell.Value;
		set => _enableBykovTrendSell.Value = value;
	}

	/// <summary>
	/// Allow BykovTrend to close existing long positions when a bearish color appears.
	/// </summary>
	public bool AllowBykovTrendCloseBuy
	{
		get => _allowBykovTrendCloseBuy.Value;
		set => _allowBykovTrendCloseBuy.Value = value;
	}

	/// <summary>
	/// Allow BykovTrend to close existing short positions when a bullish color appears.
	/// </summary>
	public bool AllowBykovTrendCloseSell
	{
		get => _allowBykovTrendCloseSell.Value;
		set => _allowBykovTrendCloseSell.Value = value;
	}

	/// <summary>
	/// Sensitivity setting for BykovTrend (mapped to WPR thresholds).
	/// </summary>
	public int BykovTrendRisk
	{
		get => _bykovTrendRisk.Value;
		set => _bykovTrendRisk.Value = value;
	}

	/// <summary>
	/// Williams %R length used by BykovTrend.
	/// </summary>
	public int BykovTrendWprLength
	{
		get => _bykovTrendWprLength.Value;
		set => _bykovTrendWprLength.Value = value;
	}

	/// <summary>
	/// Bar offset for evaluating BykovTrend colors.
	/// </summary>
	public int BykovTrendSignalBar
	{
		get => _bykovTrendSignalBar.Value;
		set => _bykovTrendSignalBar.Value = value;
	}

	/// <summary>
	/// Candle type used to calculate BykovTrend signals.
	/// </summary>
	public DataType BykovTrendCandleType
	{
		get => _bykovTrendCandleType.Value;
		set => _bykovTrendCandleType.Value = value;
	}

	/// <summary>
	/// Enable long entries from the ColorX2MA module.
	/// </summary>
	public bool EnableColorX2MaBuy
	{
		get => _enableColorX2MaBuy.Value;
		set => _enableColorX2MaBuy.Value = value;
	}

	/// <summary>
	/// Enable short entries from the ColorX2MA module.
	/// </summary>
	public bool EnableColorX2MaSell
	{
		get => _enableColorX2MaSell.Value;
		set => _enableColorX2MaSell.Value = value;
	}

	/// <summary>
	/// Allow ColorX2MA to close long positions on bearish slope changes.
	/// </summary>
	public bool AllowColorX2MaCloseBuy
	{
		get => _allowColorX2MaCloseBuy.Value;
		set => _allowColorX2MaCloseBuy.Value = value;
	}

	/// <summary>
	/// Allow ColorX2MA to close short positions on bullish slope changes.
	/// </summary>
	public bool AllowColorX2MaCloseSell
	{
		get => _allowColorX2MaCloseSell.Value;
		set => _allowColorX2MaCloseSell.Value = value;
	}

	/// <summary>
	/// First smoothing method for ColorX2MA.
	/// </summary>
	public ColorX2MaSmoothingMethod ColorX2MaMethod1
	{
		get => _colorX2MaMethod1.Value;
		set => _colorX2MaMethod1.Value = value;
	}

	/// <summary>
	/// Period for the first smoothing stage.
	/// </summary>
	public int ColorX2MaLength1
	{
		get => _colorX2MaLength1.Value;
		set => _colorX2MaLength1.Value = value;
	}

	/// <summary>
	/// Placeholder for the first stage phase (retained for parity with the MQL input).
	/// </summary>
	public int ColorX2MaPhase1
	{
		get => _colorX2MaPhase1.Value;
		set => _colorX2MaPhase1.Value = value;
	}

	/// <summary>
	/// Second smoothing method for ColorX2MA.
	/// </summary>
	public ColorX2MaSmoothingMethod ColorX2MaMethod2
	{
		get => _colorX2MaMethod2.Value;
		set => _colorX2MaMethod2.Value = value;
	}

	/// <summary>
	/// Period for the second smoothing stage.
	/// </summary>
	public int ColorX2MaLength2
	{
		get => _colorX2MaLength2.Value;
		set => _colorX2MaLength2.Value = value;
	}

	/// <summary>
	/// Placeholder for the second stage phase (kept for documentation completeness).
	/// </summary>
	public int ColorX2MaPhase2
	{
		get => _colorX2MaPhase2.Value;
		set => _colorX2MaPhase2.Value = value;
	}

	/// <summary>
	/// Price source used for ColorX2MA calculations.
	/// </summary>
	public PriceType ColorX2MaPriceType
	{
		get => _colorX2MaPriceType.Value;
		set => _colorX2MaPriceType.Value = value;
	}

	/// <summary>
	/// Bar offset for evaluating ColorX2MA slope colors.
	/// </summary>
	public int ColorX2MaSignalBar
	{
		get => _colorX2MaSignalBar.Value;
		set => _colorX2MaSignalBar.Value = value;
	}

	/// <summary>
	/// Candle type used for ColorX2MA smoothing.
	/// </summary>
	public DataType ColorX2MaCandleType
	{
		get => _colorX2MaCandleType.Value;
		set => _colorX2MaCandleType.Value = value;
	}

	/// <summary>
	/// Optional stop loss percentage applied through the built-in protection block.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Optional take profit percentage applied through the built-in protection block.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters with defaults mirroring the MQL inputs.
	/// </summary>
	public BykovTrendColorX2MaMmRecStrategy()
	{
		_enableBykovTrendBuy = Param(nameof(EnableBykovTrendBuy), true)
		.SetDisplay("BykovTrend Buy", "Allow BykovTrend module to open long trades", "BykovTrend");

		_enableBykovTrendSell = Param(nameof(EnableBykovTrendSell), true)
		.SetDisplay("BykovTrend Sell", "Allow BykovTrend module to open short trades", "BykovTrend");

		_allowBykovTrendCloseBuy = Param(nameof(AllowBykovTrendCloseBuy), true)
		.SetDisplay("BykovTrend Close Long", "Close longs when BykovTrend turns bearish", "BykovTrend");

		_allowBykovTrendCloseSell = Param(nameof(AllowBykovTrendCloseSell), true)
		.SetDisplay("BykovTrend Close Short", "Close shorts when BykovTrend turns bullish", "BykovTrend");

		_bykovTrendRisk = Param(nameof(BykovTrendRisk), 3)
		.SetDisplay("BykovTrend Risk", "Sensitivity value for Williams %R thresholds", "BykovTrend");

		_bykovTrendWprLength = Param(nameof(BykovTrendWprLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("BykovTrend WPR Length", "Williams %R length", "BykovTrend");

		_bykovTrendSignalBar = Param(nameof(BykovTrendSignalBar), 1)
		.SetDisplay("BykovTrend Signal Bar", "Bar offset for color checks", "BykovTrend");

		_bykovTrendCandleType = Param(nameof(BykovTrendCandleType), TimeSpan.FromHours(2).TimeFrame())
		.SetDisplay("BykovTrend Timeframe", "Candle type for BykovTrend", "BykovTrend");

		_enableColorX2MaBuy = Param(nameof(EnableColorX2MaBuy), true)
		.SetDisplay("ColorX2MA Buy", "Allow ColorX2MA module to open long trades", "ColorX2MA");

		_enableColorX2MaSell = Param(nameof(EnableColorX2MaSell), true)
		.SetDisplay("ColorX2MA Sell", "Allow ColorX2MA module to open short trades", "ColorX2MA");

		_allowColorX2MaCloseBuy = Param(nameof(AllowColorX2MaCloseBuy), true)
		.SetDisplay("ColorX2MA Close Long", "Close longs when slope turns bearish", "ColorX2MA");

		_allowColorX2MaCloseSell = Param(nameof(AllowColorX2MaCloseSell), true)
		.SetDisplay("ColorX2MA Close Short", "Close shorts when slope turns bullish", "ColorX2MA");

		_colorX2MaMethod1 = Param(nameof(ColorX2MaMethod1), ColorX2MaSmoothingMethod.Simple)
		.SetDisplay("First MA", "Smoothing method for the first stage", "ColorX2MA");

		_colorX2MaLength1 = Param(nameof(ColorX2MaLength1), 12)
		.SetGreaterThanZero()
		.SetDisplay("First Length", "Length of the first smoothing", "ColorX2MA");

		_colorX2MaPhase1 = Param(nameof(ColorX2MaPhase1), 15)
		.SetDisplay("First Phase", "Placeholder for historical compatibility", "ColorX2MA");

		_colorX2MaMethod2 = Param(nameof(ColorX2MaMethod2), ColorX2MaSmoothingMethod.Jurik)
		.SetDisplay("Second MA", "Smoothing method for the second stage", "ColorX2MA");

		_colorX2MaLength2 = Param(nameof(ColorX2MaLength2), 5)
		.SetGreaterThanZero()
		.SetDisplay("Second Length", "Length of the second smoothing", "ColorX2MA");

		_colorX2MaPhase2 = Param(nameof(ColorX2MaPhase2), 15)
		.SetDisplay("Second Phase", "Placeholder for historical compatibility", "ColorX2MA");

		_colorX2MaPriceType = Param(nameof(ColorX2MaPriceType), PriceType.Close)
		.SetDisplay("Price Source", "Price type used for smoothing", "ColorX2MA");

		_colorX2MaSignalBar = Param(nameof(ColorX2MaSignalBar), 1)
		.SetDisplay("ColorX2MA Signal Bar", "Bar offset for slope checks", "ColorX2MA");

		_colorX2MaCandleType = Param(nameof(ColorX2MaCandleType), TimeSpan.FromHours(2).TimeFrame())
		.SetDisplay("ColorX2MA Timeframe", "Candle type for ColorX2MA", "ColorX2MA");

		_stopLossPercent = Param(nameof(StopLossPercent), 0m)
		.SetDisplay("Stop Loss %", "Optional stop loss percentage", "Risk Management");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0m)
		.SetDisplay("Take Profit %", "Optional take profit percentage", "Risk Management");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var seen = new HashSet<DataType>();

		foreach (var type in new[] { BykovTrendCandleType, ColorX2MaCandleType })
		{
			if (seen.Add(type))
			yield return (Security, type);
		}
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Prepare BykovTrend Williams %R indicator.
		_bykovTrendWpr = new WilliamsR { Length = BykovTrendWprLength };

		var bykovSubscription = SubscribeCandles(BykovTrendCandleType);
		bykovSubscription
		.Bind(_bykovTrendWpr, ProcessBykovTrend)
		.Start();

		// Prepare double smoothing for ColorX2MA slope detection.
		_colorX2MaStage1 = CreateMovingAverage(ColorX2MaMethod1, ColorX2MaLength1);
		_colorX2MaStage2 = CreateMovingAverage(ColorX2MaMethod2, ColorX2MaLength2);

		var colorSubscription = SubscribeCandles(ColorX2MaCandleType);
		colorSubscription
		.Bind(ProcessColorX2Ma)
		.Start();

		if (TakeProfitPercent > 0m || StopLossPercent > 0m)
		{
			StartProtection(
			takeProfit: TakeProfitPercent > 0m ? new Unit(TakeProfitPercent, UnitTypes.Percent) : null,
			stopLoss: StopLossPercent > 0m ? new Unit(StopLossPercent, UnitTypes.Percent) : null);
		}
	}

	private void ProcessBykovTrend(ICandleMessage candle, decimal wprValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_bykovTrendWpr.IsFormed)
		return;

		// Calculate color based on the Williams %R thresholds.
		var k = 33m - BykovTrendRisk;
		var trend = _bykovTrendTrend;

		if (wprValue < -100m + k)
		trend = -1;
		if (wprValue > -k)
		trend = 1;

		var color = 2;

		if (trend > 0)
		{
			color = candle.OpenPrice <= candle.ClosePrice ? 0 : 1;
		}
		else if (trend < 0)
		{
			color = candle.OpenPrice >= candle.ClosePrice ? 4 : 3;
		}

		_bykovTrendTrend = trend;

		PushWithLimit(_bykovTrendColors, color, BykovTrendSignalBar + 3);

		HandleBykovTrendSignals();
	}

	private void ProcessColorX2Ma(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var price = SelectPrice(candle, ColorX2MaPriceType);

		var stage1Value = _colorX2MaStage1.Process(new DecimalIndicatorValue(_colorX2MaStage1, price, candle.OpenTime));
		if (stage1Value is not DecimalIndicatorValue { IsFinal: true, Value: var firstStage })
		return;

		var stage2Value = _colorX2MaStage2.Process(new DecimalIndicatorValue(_colorX2MaStage2, firstStage, candle.OpenTime));
		if (stage2Value is not DecimalIndicatorValue { IsFinal: true, Value: var smoothed })
		return;

		var state = 0;
		if (_prevColorX2MaValue is decimal prev)
		{
			state = smoothed > prev ? 1 : smoothed < prev ? 2 : 0;
		}

		_prevColorX2MaValue = smoothed;

		PushWithLimit(_colorX2MaStates, state, ColorX2MaSignalBar + 3);

		HandleColorX2MaSignals();
	}

	private void HandleBykovTrendSignals()
	{
		var index = Math.Max(BykovTrendSignalBar, 0);
		if (_bykovTrendColors.Count <= index)
		return;

		var current = _bykovTrendColors[index];
		var previous = _bykovTrendColors.Count > index + 1 ? _bykovTrendColors[index + 1] : current;

		var closeShort = AllowBykovTrendCloseSell && current < 2;
		var closeLong = AllowBykovTrendCloseBuy && current > 2;

		var openLong = EnableBykovTrendBuy && current < 2 && previous > 1;
		var openShort = EnableBykovTrendSell && current > 2 && previous < 3;

		ExecuteOrders(closeLong, closeShort, openLong, openShort);
	}

	private void HandleColorX2MaSignals()
	{
		var index = Math.Max(ColorX2MaSignalBar, 0);
		if (_colorX2MaStates.Count <= index)
		return;

		var current = _colorX2MaStates[index];
		var previous = _colorX2MaStates.Count > index + 1 ? _colorX2MaStates[index + 1] : current;

		var closeShort = AllowColorX2MaCloseSell && current == 1;
		var closeLong = AllowColorX2MaCloseBuy && current == 2;

		var openLong = EnableColorX2MaBuy && current == 1 && previous != 1;
		var openShort = EnableColorX2MaSell && current == 2 && previous != 2;

		ExecuteOrders(closeLong, closeShort, openLong, openShort);
	}

	private void ExecuteOrders(bool closeLong, bool closeShort, bool openLong, bool openShort)
	{
		// Close shorts first to mirror the MQL script behaviour.
		if (closeShort && Position < 0)
		BuyMarket();

		// Close longs next.
		if (closeLong && Position > 0)
		SellMarket();

		// Enter long if permitted after handling closures.
		if (openLong && Position <= 0)
		BuyMarket();

		// Enter short if permitted after handling closures.
		if (openShort && Position >= 0)
		SellMarket();
	}

	private static void PushWithLimit(List<int> target, int value, int maxCount)
	{
		target.Insert(0, value);
		if (target.Count > maxCount)
		target.RemoveAt(target.Count - 1);
	}

	private static decimal SelectPrice(ICandleMessage candle, PriceType type)
	{
		return type switch
		{
			PriceType.Close => candle.ClosePrice,
			PriceType.Open => candle.OpenPrice,
			PriceType.High => candle.HighPrice,
			PriceType.Low => candle.LowPrice,
			PriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			PriceType.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
			PriceType.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			PriceType.Simpl => (candle.OpenPrice + candle.ClosePrice) / 2m,
			PriceType.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			PriceType.TrendFollow0 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice : candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
			PriceType.TrendFollow1 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m : candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
			PriceType.Demark =>
			DemarkPrice(candle),
			_ => candle.ClosePrice,
		};
	}

	private static decimal DemarkPrice(ICandleMessage candle)
	{
		var sum = candle.HighPrice + candle.LowPrice + candle.ClosePrice;

		if (candle.ClosePrice < candle.OpenPrice)
		sum = (sum + candle.LowPrice) / 2m;
		else if (candle.ClosePrice > candle.OpenPrice)
		sum = (sum + candle.HighPrice) / 2m;
		else
		sum = (sum + candle.ClosePrice) / 2m;

		return ((sum - candle.LowPrice) + (sum - candle.HighPrice)) / 2m;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(ColorX2MaSmoothingMethod method, int length)
	{
		return method switch
		{
			ColorX2MaSmoothingMethod.Simple => new SimpleMovingAverage { Length = length },
			ColorX2MaSmoothingMethod.Exponential => new ExponentialMovingAverage { Length = length },
			ColorX2MaSmoothingMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			ColorX2MaSmoothingMethod.Weighted => new WeightedMovingAverage { Length = length },
			ColorX2MaSmoothingMethod.Jurik => new JurikMovingAverage { Length = length },
			ColorX2MaSmoothingMethod.Hull => new HullMovingAverage { Length = length },
			ColorX2MaSmoothingMethod.VolumeWeighted => new VolumeWeightedMovingAverage { Length = length },
			ColorX2MaSmoothingMethod.DoubleExponential => new DoubleExponentialMovingAverage { Length = length },
			ColorX2MaSmoothingMethod.TripleExponential => new TripleExponentialMovingAverage { Length = length },
			ColorX2MaSmoothingMethod.ZeroLagExponential => new ZeroLagExponentialMovingAverage { Length = length },
			ColorX2MaSmoothingMethod.KaufmanAdaptive => new KaufmanAdaptiveMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	/// <summary>
	/// Supported price sources replicating the MQL applied price enumeration.
	/// </summary>
	public enum PriceType
	{
		Close = 1,
		Open = 2,
		High = 3,
		Low = 4,
		Median = 5,
		Typical = 6,
		Weighted = 7,
		Simpl = 8,
		Quarter = 9,
		TrendFollow0 = 10,
		TrendFollow1 = 11,
		Demark = 12,
	}

	/// <summary>
	/// Available smoothing choices for the double MA pipeline.
	/// </summary>
	public enum ColorX2MaSmoothingMethod
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted,
		Jurik,
		Hull,
		VolumeWeighted,
		DoubleExponential,
		TripleExponential,
		ZeroLagExponential,
		KaufmanAdaptive,
	}
}
