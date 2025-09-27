
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
/// Duplex version of the Skyscraper Fix strategy with separate long and short indicator settings.
/// </summary>
public class ExpSkyscraperFixDuplexStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<bool> _enableLongEntries;
	private readonly StrategyParam<bool> _enableLongExits;
	private readonly StrategyParam<DataType> _longCandleType;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<decimal> _longKv;
	private readonly StrategyParam<decimal> _longPercentage;
	private readonly StrategyParam<SkyscraperCalculationModes> _longMode;
	private readonly StrategyParam<int> _longSignalBar;
	private readonly StrategyParam<bool> _enableShortEntries;
	private readonly StrategyParam<bool> _enableShortExits;
	private readonly StrategyParam<DataType> _shortCandleType;
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<decimal> _shortKv;
	private readonly StrategyParam<decimal> _shortPercentage;
	private readonly StrategyParam<SkyscraperCalculationModes> _shortMode;
	private readonly StrategyParam<int> _shortSignalBar;

	private SkyscraperFixIndicator _longIndicator;
	private SkyscraperFixIndicator _shortIndicator;

	private readonly Queue<SkyscraperSignal> _longSignals = new();
	private readonly Queue<SkyscraperSignal> _shortSignals = new();

	private readonly record struct SkyscraperSignal(decimal? Upper, decimal? Lower, decimal? Buy, decimal? Sell);

	/// <summary>
	/// Trade volume for new market entries.
	/// </summary>
	public decimal TradeVolume { get => _tradeVolume.Value; set => _tradeVolume.Value = value; }

	/// <summary>
	/// Enable long side entries.
	/// </summary>
	public bool EnableLongEntries { get => _enableLongEntries.Value; set => _enableLongEntries.Value = value; }

	/// <summary>
	/// Enable long side exits.
	/// </summary>
	public bool EnableLongExits { get => _enableLongExits.Value; set => _enableLongExits.Value = value; }

	/// <summary>
	/// Candle type for the long side indicator.
	/// </summary>
	public DataType LongCandleType { get => _longCandleType.Value; set => _longCandleType.Value = value; }

	/// <summary>
	/// ATR window length for the long indicator.
	/// </summary>
	public int LongLength { get => _longLength.Value; set => _longLength.Value = value; }

	/// <summary>
	/// Sensitivity multiplier for the long indicator.
	/// </summary>
	public decimal LongKv { get => _longKv.Value; set => _longKv.Value = value; }

	/// <summary>
	/// Percentage offset applied to the long trailing line.
	/// </summary>
	public decimal LongPercentage { get => _longPercentage.Value; set => _longPercentage.Value = value; }

	/// <summary>
	/// Calculation mode for the long indicator.
	/// </summary>
	public SkyscraperCalculationModes LongMode { get => _longMode.Value; set => _longMode.Value = value; }

	/// <summary>
	/// Number of closed candles to delay long signals.
	/// </summary>
	public int LongSignalBar { get => _longSignalBar.Value; set => _longSignalBar.Value = value; }

	/// <summary>
	/// Enable short side entries.
	/// </summary>
	public bool EnableShortEntries { get => _enableShortEntries.Value; set => _enableShortEntries.Value = value; }

	/// <summary>
	/// Enable short side exits.
	/// </summary>
	public bool EnableShortExits { get => _enableShortExits.Value; set => _enableShortExits.Value = value; }

	/// <summary>
	/// Candle type for the short side indicator.
	/// </summary>
	public DataType ShortCandleType { get => _shortCandleType.Value; set => _shortCandleType.Value = value; }

	/// <summary>
	/// ATR window length for the short indicator.
	/// </summary>
	public int ShortLength { get => _shortLength.Value; set => _shortLength.Value = value; }

	/// <summary>
	/// Sensitivity multiplier for the short indicator.
	/// </summary>
	public decimal ShortKv { get => _shortKv.Value; set => _shortKv.Value = value; }

	/// <summary>
	/// Percentage offset applied to the short trailing line.
	/// </summary>
	public decimal ShortPercentage { get => _shortPercentage.Value; set => _shortPercentage.Value = value; }

	/// <summary>
	/// Calculation mode for the short indicator.
	/// </summary>
	public SkyscraperCalculationModes ShortMode { get => _shortMode.Value; set => _shortMode.Value = value; }

	/// <summary>
	/// Number of closed candles to delay short signals.
	/// </summary>
	public int ShortSignalBar { get => _shortSignalBar.Value; set => _shortSignalBar.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpSkyscraperFixDuplexStrategy"/> class.
	/// </summary>
	public ExpSkyscraperFixDuplexStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume for entries", "Trading");

		_enableLongEntries = Param(nameof(EnableLongEntries), true)
			.SetDisplay("Enable Long Entries", "Allow opening long positions", "Long");

		_enableLongExits = Param(nameof(EnableLongExits), true)
			.SetDisplay("Enable Long Exits", "Allow closing long positions", "Long");

		_longCandleType = Param(nameof(LongCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Long Candle Type", "Candle type for long analysis", "Long");

		_longLength = Param(nameof(LongLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Long Length", "ATR window for long side", "Long");

		_longKv = Param(nameof(LongKv), 0.9m)
			.SetGreaterThanZero()
			.SetDisplay("Long Kv", "Sensitivity multiplier for long side", "Long");

		_longPercentage = Param(nameof(LongPercentage), 0m)
			.SetDisplay("Long Percentage", "Offset percentage for long trailing line", "Long");

		_longMode = Param(nameof(LongMode), SkyscraperCalculationModes.HighLow)
			.SetDisplay("Long Mode", "Price source for the long indicator", "Long");

		_longSignalBar = Param(nameof(LongSignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Long Signal Bar", "Delay long signals by closed candles", "Long");

		_enableShortEntries = Param(nameof(EnableShortEntries), true)
			.SetDisplay("Enable Short Entries", "Allow opening short positions", "Short");

		_enableShortExits = Param(nameof(EnableShortExits), true)
			.SetDisplay("Enable Short Exits", "Allow closing short positions", "Short");

		_shortCandleType = Param(nameof(ShortCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Short Candle Type", "Candle type for short analysis", "Short");

		_shortLength = Param(nameof(ShortLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Short Length", "ATR window for short side", "Short");

		_shortKv = Param(nameof(ShortKv), 0.9m)
			.SetGreaterThanZero()
			.SetDisplay("Short Kv", "Sensitivity multiplier for short side", "Short");

		_shortPercentage = Param(nameof(ShortPercentage), 0m)
			.SetDisplay("Short Percentage", "Offset percentage for short trailing line", "Short");

		_shortMode = Param(nameof(ShortMode), SkyscraperCalculationModes.HighLow)
			.SetDisplay("Short Mode", "Price source for the short indicator", "Short");

		_shortSignalBar = Param(nameof(ShortSignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Short Signal Bar", "Delay short signals by closed candles", "Short");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security is null)
			yield break;

		yield return (Security, LongCandleType);

		if (!Equals(LongCandleType, ShortCandleType))
			yield return (Security, ShortCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_longSignals.Clear();
		_shortSignals.Clear();
		_longIndicator = null;
		_shortIndicator = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();
		Volume = TradeVolume;

		var priceStep = Security?.PriceStep ?? 0m;

		_longIndicator = new SkyscraperFixIndicator
		{
			Length = LongLength,
			Kv = LongKv,
			Percentage = LongPercentage,
			Mode = LongMode,
			PriceStep = priceStep
		};

		_shortIndicator = new SkyscraperFixIndicator
		{
			Length = ShortLength,
			Kv = ShortKv,
			Percentage = ShortPercentage,
			Mode = ShortMode,
			PriceStep = priceStep
		};

		var longSubscription = SubscribeCandles(LongCandleType);
		longSubscription
			.BindEx(_longIndicator, ProcessLong)
			.Start();

		var shortSubscription = SubscribeCandles(ShortCandleType);
		shortSubscription
			.BindEx(_shortIndicator, ProcessShort)
			.Start();
	}

	private void ProcessLong(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (indicatorValue is not SkyscraperFixValue value || !value.HasValue)
			return;

		var signal = new SkyscraperSignal(value.Upper, value.Lower, value.Buy, value.Sell);
		_longSignals.Enqueue(signal);

		if (_longSignals.Count > LongSignalBar)
		{
			var target = _longSignals.Dequeue();
			HandleLongSignal(target);
		}
	}

	private void ProcessShort(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (indicatorValue is not SkyscraperFixValue value || !value.HasValue)
			return;

		var signal = new SkyscraperSignal(value.Upper, value.Lower, value.Buy, value.Sell);
		_shortSignals.Enqueue(signal);

		if (_shortSignals.Count > ShortSignalBar)
		{
			var target = _shortSignals.Dequeue();
			HandleShortSignal(target);
		}
	}

	private void HandleLongSignal(SkyscraperSignal signal)
	{
		if (EnableLongExits && signal.Lower.HasValue && Position > 0 && IsFormedAndOnlineAndAllowTrading())
			SellMarket(Position);

		if (EnableLongEntries && signal.Buy.HasValue && IsFormedAndOnlineAndAllowTrading())
		{
			if (Position < 0)
				BuyMarket(-Position);

			if (Position <= 0)
				BuyMarket();
		}
	}

	private void HandleShortSignal(SkyscraperSignal signal)
	{
		if (EnableShortExits && signal.Upper.HasValue && Position < 0 && IsFormedAndOnlineAndAllowTrading())
			BuyMarket(-Position);

		if (EnableShortEntries && signal.Sell.HasValue && IsFormedAndOnlineAndAllowTrading())
		{
			if (Position > 0)
				SellMarket(Position);

			if (Position >= 0)
				SellMarket();
		}
	}
}

/// <summary>
/// Calculation mode for the Skyscraper Fix indicator.
/// </summary>
public enum SkyscraperCalculationModes
{
	/// <summary>Use the bar high and low.</summary>
	HighLow,

	/// <summary>Use the bar close.</summary>
	Close
}

/// <summary>
/// Skyscraper Fix indicator translated from the original MQL version.
/// </summary>
public class SkyscraperFixIndicator : BaseIndicator<decimal>
{
	private readonly AverageTrueRange _atr = new();
	private readonly Highest _atrHighest = new();
	private readonly Lowest _atrLowest = new();

	private decimal? _previousMin;
	private decimal? _previousMax;
	private decimal? _previousLine;
	private decimal? _previousUpper;
	private decimal? _previousLower;
	private int _previousTrend;
	private bool _initialized;

	/// <summary>
	/// ATR averaging period.
	/// </summary>
	public int AtrPeriod { get; set; } = 15;

	/// <summary>
	/// ATR lookback length used for trailing step calculation.
	/// </summary>
	public int Length { get; set; } = 10;

	/// <summary>
	/// Sensitivity multiplier applied to the ATR step.
	/// </summary>
	public decimal Kv { get; set; } = 0.9m;

	/// <summary>
	/// Percentage offset applied to the midline.
	/// </summary>
	public decimal Percentage { get; set; }

	/// <summary>
	/// Price mode used for envelope construction.
	/// </summary>
	public SkyscraperCalculationModes Mode { get; set; } = SkyscraperCalculationModes.HighLow;

	/// <summary>
	/// Instrument price step.
	/// </summary>
	public decimal PriceStep { get; set; }

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
			return new SkyscraperFixValue(this, input, false, null, null, null, null, null, null);

		if (PriceStep <= 0m || Length <= 0 || AtrPeriod <= 0)
			return new SkyscraperFixValue(this, input, false, null, null, null, null, null, null);

		if (_atr.Length != AtrPeriod)
			_atr.Length = AtrPeriod;

		if (_atrHighest.Length != Length)
			_atrHighest.Length = Length;

		if (_atrLowest.Length != Length)
			_atrLowest.Length = Length;

		var atrValue = _atr.Process(input);
		if (!_atr.IsFormed)
			return new SkyscraperFixValue(this, input, false, null, null, null, null, null, null);

		var atr = atrValue.ToDecimal();

		var highest = _atrHighest.Process(new DecimalIndicatorValue(_atrHighest, atr, input.Time));
		var lowest = _atrLowest.Process(new DecimalIndicatorValue(_atrLowest, atr, input.Time));

		if (!_atrHighest.IsFormed || !_atrLowest.IsFormed)
			return new SkyscraperFixValue(this, input, false, null, null, null, null, null, null);

		var atrMax = highest.ToDecimal();
		var atrMin = lowest.ToDecimal();

		var stepDecimal = 0.5m * Kv * (atrMax + atrMin) / PriceStep;
		if (stepDecimal <= 0m)
			return new SkyscraperFixValue(this, input, false, null, null, null, null, null, null);

		var step = Math.Max(1, (int)Math.Floor(stepDecimal));
		var stepValue = step * PriceStep;
		var doubleStep = stepValue * 2m;

		decimal smax0;
		decimal smin0;
		switch (Mode)
		{
			case SkyscraperCalculationModes.HighLow:
				smax0 = candle.LowPrice + doubleStep;
				smin0 = candle.HighPrice - doubleStep;
				break;
			case SkyscraperCalculationModes.Close:
				smax0 = candle.ClosePrice + doubleStep;
				smin0 = candle.ClosePrice - doubleStep;
				break;
			default:
				smax0 = candle.LowPrice + doubleStep;
				smin0 = candle.HighPrice - doubleStep;
				break;
		}

		if (!_initialized)
		{
			_previousMin = smin0;
			_previousMax = smax0;
			_previousLine = candle.ClosePrice;
			_previousUpper = null;
			_previousLower = null;
			_previousTrend = 0;
			_initialized = true;
			return new SkyscraperFixValue(this, input, false, null, null, null, null, null, null);
		}

		var prevMin = _previousMin ?? smin0;
		var prevMax = _previousMax ?? smax0;
		var prevLine = _previousLine ?? candle.ClosePrice;
		var trend = _previousTrend;

		if (candle.ClosePrice > prevMax)
			trend = 1;

		if (candle.ClosePrice < prevMin)
			trend = -1;

		decimal? upper = null;
		decimal? lower = null;
		var middle = prevLine;
		var percentOffset = Percentage / 100m * stepValue;

		if (trend > 0)
		{
			smin0 = Math.Max(smin0, prevMin);
			upper = smin0;
			var line = smin0 + stepValue;
			middle = Math.Max(line - percentOffset, prevLine);
		}
		else
		{
			smax0 = Math.Min(smax0, prevMax);
			lower = smax0;
			var line = smax0 - stepValue;
			middle = Math.Min(line + percentOffset, prevLine);
		}

		var buy = upper.HasValue && _previousLower.HasValue ? upper : null;
		var sell = lower.HasValue && _previousUpper.HasValue ? lower : null;

		_previousTrend = trend;
		_previousMin = smin0;
		_previousMax = smax0;
		_previousLine = middle;
		_previousUpper = upper;
		_previousLower = lower;

		return new SkyscraperFixValue(this, input, true, upper, lower, buy, sell, middle, trend);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();
		_atr.Reset();
		_atrHighest.Reset();
		_atrLowest.Reset();
		_previousMin = null;
		_previousMax = null;
		_previousLine = null;
		_previousUpper = null;
		_previousLower = null;
		_previousTrend = 0;
		_initialized = false;
	}
}

/// <summary>
/// Indicator value for <see cref="SkyscraperFixIndicator"/>.
/// </summary>
public class SkyscraperFixValue : ComplexIndicatorValue
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SkyscraperFixValue"/> class.
	/// </summary>
	public SkyscraperFixValue(IIndicator indicator, IIndicatorValue input, bool hasValue, decimal? upper, decimal? lower, decimal? buy, decimal? sell, decimal? middle, int? trend)
		: base(indicator, input,
			(nameof(Upper), upper),
			(nameof(Lower), lower),
			(nameof(Buy), buy),
			(nameof(Sell), sell),
			(nameof(Middle), middle),
			(nameof(Trend), trend))
	{
		HasValue = hasValue;
	}

	/// <summary>
	/// True when the indicator has produced a valid output.
	/// </summary>
	public bool HasValue { get; }

	/// <summary>
	/// Upper trailing level.
	/// </summary>
	public decimal? Upper => (decimal?)GetValue(nameof(Upper));

	/// <summary>
	/// Lower trailing level.
	/// </summary>
	public decimal? Lower => (decimal?)GetValue(nameof(Lower));

	/// <summary>
	/// Long entry trigger level.
	/// </summary>
	public decimal? Buy => (decimal?)GetValue(nameof(Buy));

	/// <summary>
	/// Short entry trigger level.
	/// </summary>
	public decimal? Sell => (decimal?)GetValue(nameof(Sell));

	/// <summary>
	/// Midline used for visualization.
	/// </summary>
	public decimal? Middle => (decimal?)GetValue(nameof(Middle));

	/// <summary>
	/// Current trend direction.
	/// </summary>
	public int? Trend => (int?)GetValue(nameof(Trend));
}

