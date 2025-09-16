using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe strategy based on John Ehlers' Sinewave2 indicator.
/// Combines higher timeframe trend filtering with lower timeframe entry signals.
/// </summary>
public class ExpSinewave2X2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _alphaHigh;
	private readonly StrategyParam<decimal> _alphaLow;
	private readonly StrategyParam<int> _signalBarHigh;
	private readonly StrategyParam<int> _signalBarLow;
	private readonly StrategyParam<bool> _enableBuyOpen;
	private readonly StrategyParam<bool> _enableSellOpen;
	private readonly StrategyParam<bool> _enableBuyCloseTrend;
	private readonly StrategyParam<bool> _enableSellCloseTrend;
	private readonly StrategyParam<bool> _enableBuyCloseLower;
	private readonly StrategyParam<bool> _enableSellCloseLower;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<DataType> _lowerCandleType;

	private Sinewave2Indicator _higherIndicator = null!;
	private Sinewave2Indicator _lowerIndicator = null!;

	private readonly List<(decimal Lead, decimal Sine)> _higherValues = new();
	private readonly List<(decimal Lead, decimal Sine)> _lowerValues = new();

	private int _trendDirection;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpSinewave2X2Strategy"/> class.
	/// </summary>
	public ExpSinewave2X2Strategy()
	{
		_alphaHigh = Param(nameof(AlphaHigh), 0.07m)
		.SetGreaterThanZero()
		.SetDisplay("Higher Alpha", "Alpha for higher timeframe Sinewave2", "Higher TF")
		.SetCanOptimize(true);

		_alphaLow = Param(nameof(AlphaLow), 0.07m)
		.SetGreaterThanZero()
		.SetDisplay("Lower Alpha", "Alpha for lower timeframe Sinewave2", "Lower TF")
		.SetCanOptimize(true);

		_signalBarHigh = Param(nameof(SignalBarHigh), 1)
		.SetGreaterOrEqualZero()
		.SetDisplay("Higher Signal Bar", "Bar shift used to evaluate higher timeframe trend", "Higher TF");

		_signalBarLow = Param(nameof(SignalBarLow), 1)
		.SetGreaterOrEqualZero()
		.SetDisplay("Lower Signal Bar", "Bar shift used to read lower timeframe signals", "Lower TF");

		_enableBuyOpen = Param(nameof(EnableBuyOpen), true)
		.SetDisplay("Enable Long Entries", "Allow long entries on lower timeframe signals", "Entries");

		_enableSellOpen = Param(nameof(EnableSellOpen), true)
		.SetDisplay("Enable Short Entries", "Allow short entries on lower timeframe signals", "Entries");

		_enableBuyCloseTrend = Param(nameof(EnableBuyCloseTrend), true)
		.SetDisplay("Close Longs On Down Trend", "Force long exit when higher timeframe turns bearish", "Exits");

		_enableSellCloseTrend = Param(nameof(EnableSellCloseTrend), true)
		.SetDisplay("Close Shorts On Up Trend", "Force short exit when higher timeframe turns bullish", "Exits");

		_enableBuyCloseLower = Param(nameof(EnableBuyCloseLower), true)
		.SetDisplay("Close Longs On Lower TF", "Close longs when lower timeframe line crosses down", "Exits");

		_enableSellCloseLower = Param(nameof(EnableSellCloseLower), true)
		.SetDisplay("Close Shorts On Lower TF", "Close shorts when lower timeframe line crosses up", "Exits");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss Points", "Protective stop in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit Points", "Target profit in price steps", "Risk");

		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromHours(6).TimeFrame())
		.SetDisplay("Higher TF", "Candle type for the higher timeframe filter", "General");

		_lowerCandleType = Param(nameof(LowerCandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Lower TF", "Candle type for the lower timeframe entries", "General");
	}

	/// <summary>
	/// Alpha parameter for the higher timeframe Sinewave2 indicator.
	/// </summary>
	public decimal AlphaHigh
	{
		get => _alphaHigh.Value;
		set => _alphaHigh.Value = value;
	}

	/// <summary>
	/// Alpha parameter for the lower timeframe Sinewave2 indicator.
	/// </summary>
	public decimal AlphaLow
	{
		get => _alphaLow.Value;
		set => _alphaLow.Value = value;
	}

	/// <summary>
	/// Bar shift used to define the higher timeframe trend.
	/// </summary>
	public int SignalBarHigh
	{
		get => _signalBarHigh.Value;
		set => _signalBarHigh.Value = value;
	}

	/// <summary>
	/// Bar shift used for lower timeframe entry logic.
	/// </summary>
	public int SignalBarLow
	{
		get => _signalBarLow.Value;
		set => _signalBarLow.Value = value;
	}

	/// <summary>
	/// Enables long entries when lower timeframe confirms.
	/// </summary>
	public bool EnableBuyOpen
	{
		get => _enableBuyOpen.Value;
		set => _enableBuyOpen.Value = value;
	}

	/// <summary>
	/// Enables short entries when lower timeframe confirms.
	/// </summary>
	public bool EnableSellOpen
	{
		get => _enableSellOpen.Value;
		set => _enableSellOpen.Value = value;
	}

	/// <summary>
	/// Closes long positions if the higher timeframe turns bearish.
	/// </summary>
	public bool EnableBuyCloseTrend
	{
		get => _enableBuyCloseTrend.Value;
		set => _enableBuyCloseTrend.Value = value;
	}

	/// <summary>
	/// Closes short positions if the higher timeframe turns bullish.
	/// </summary>
	public bool EnableSellCloseTrend
	{
		get => _enableSellCloseTrend.Value;
		set => _enableSellCloseTrend.Value = value;
	}

	/// <summary>
	/// Closes long positions based on the lower timeframe crossover.
	/// </summary>
	public bool EnableBuyCloseLower
	{
		get => _enableBuyCloseLower.Value;
		set => _enableBuyCloseLower.Value = value;
	}

	/// <summary>
	/// Closes short positions based on the lower timeframe crossover.
	/// </summary>
	public bool EnableSellCloseLower
	{
		get => _enableSellCloseLower.Value;
		set => _enableSellCloseLower.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type.
	/// </summary>
	public DataType HigherCandleType
	{
		get => _higherCandleType.Value;
		set => _higherCandleType.Value = value;
	}

	/// <summary>
	/// Lower timeframe candle type.
	/// </summary>
	public DataType LowerCandleType
	{
		get => _lowerCandleType.Value;
		set => _lowerCandleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security is null)
		yield break;

		yield return (Security, HigherCandleType);

		if (!HigherCandleType.Equals(LowerCandleType))
		yield return (Security, LowerCandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_trendDirection = 0;
		_higherValues.Clear();
		_lowerValues.Clear();
		ResetRisk();

		_higherIndicator = new Sinewave2Indicator { Alpha = AlphaHigh };
		_lowerIndicator = new Sinewave2Indicator { Alpha = AlphaLow };

		// Subscribe to the higher timeframe candle stream and bind the indicator pipeline.
		var higherSubscription = SubscribeCandles(HigherCandleType);
		higherSubscription.Bind(_higherIndicator, ProcessHigherCandle);

		if (HigherCandleType.Equals(LowerCandleType))
		{
			higherSubscription.Bind(_lowerIndicator, ProcessLowerCandle).Start();
		}
		else
		{
			higherSubscription.Start();

			SubscribeCandles(LowerCandleType)
			.Bind(_lowerIndicator, ProcessLowerCandle)
			.Start();
		}
	}

	private void ProcessHigherCandle(ICandleMessage candle, decimal sine, decimal lead)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_higherIndicator.IsFormed)
		return;

		// Store the latest higher timeframe indicator values for delayed access.
		_higherValues.Add((lead, sine));
		TrimHistory(_higherValues, SignalBarHigh + 5);

		var index = _higherValues.Count - 1 - SignalBarHigh;
		if (index < 0)
		return;

		var value = _higherValues[index];
		if (value.Lead > value.Sine)
		_trendDirection = 1;
		else if (value.Lead < value.Sine)
		_trendDirection = -1;
		else
		_trendDirection = 0;
	}

	private void ProcessLowerCandle(ICandleMessage candle, decimal sine, decimal lead)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_lowerIndicator.IsFormed)
		return;

		// Keep a rolling buffer with lower timeframe values for crossover checks.
		_lowerValues.Add((lead, sine));
		TrimHistory(_lowerValues, SignalBarLow + 6);

		if (CheckRiskExit(candle))
		return;

		var shift = SignalBarLow;
		var currentIndex = _lowerValues.Count - 1 - shift;
		if (currentIndex < 0)
		return;

		var previousIndex = currentIndex - 1;
		if (previousIndex < 0)
		return;

		var current = _lowerValues[currentIndex];
		var previous = _lowerValues[previousIndex];

		var buyClose = EnableBuyCloseLower && previous.Lead < previous.Sine;
		var sellClose = EnableSellCloseLower && previous.Lead > previous.Sine;
		var buyOpen = false;
		var sellOpen = false;

		if (_trendDirection < 0)
		{
			if (EnableBuyCloseTrend)
			buyClose = true;

			if (EnableSellOpen && current.Lead >= current.Sine && previous.Lead < previous.Sine)
			sellOpen = true;
		}
		else if (_trendDirection > 0)
		{
			if (EnableSellCloseTrend)
			sellClose = true;

			if (EnableBuyOpen && current.Lead <= current.Sine && previous.Lead > previous.Sine)
			buyOpen = true;
		}

		// Exit logic reacts first to avoid keeping invalid positions when the trend changes.
		if (buyClose && Position > 0)
		{
			SellMarket(Position);
			ResetRisk();
		}

		if (sellClose && Position < 0)
		{
			BuyMarket(-Position);
			ResetRisk();
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (buyOpen && Position <= 0)
		{
			var volume = Position < 0 ? Volume + Math.Abs(Position) : Volume;
			BuyMarket(volume);
			SetRiskLevels(candle.ClosePrice, true);
		}
		else if (sellOpen && Position >= 0)
		{
			var volume = Position > 0 ? Volume + Position : Volume;
			SellMarket(volume);
			SetRiskLevels(candle.ClosePrice, false);
		}
	}

	private bool CheckRiskExit(ICandleMessage candle)
	{
		// Handle protective stop and target exits using intrabar extremes.
		if (Position > 0)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetRisk();
				return true;
			}

			if (_takePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				ResetRisk();
				return true;
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(-Position);
				ResetRisk();
				return true;
			}

			if (_takePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(-Position);
				ResetRisk();
				return true;
			}
		}

		return false;
	}

	private void SetRiskLevels(decimal entryPrice, bool isLong)
	{
		_entryPrice = entryPrice;

		// Translate point-based risk settings into absolute price offsets.
		var step = Security?.PriceStep ?? 1m;

		_stopPrice = StopLossPoints > 0m
		? (isLong ? entryPrice - StopLossPoints * step : entryPrice + StopLossPoints * step)
		: null;

		_takePrice = TakeProfitPoints > 0m
		? (isLong ? entryPrice + TakeProfitPoints * step : entryPrice - TakeProfitPoints * step)
		: null;
	}

	private void ResetRisk()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}

	private static void TrimHistory(List<(decimal Lead, decimal Sine)> storage, int maxLength)
	{
		var limit = Math.Max(maxLength, 4);
		if (storage.Count > limit)
		storage.RemoveRange(0, storage.Count - limit);
	}
}

/// <summary>
/// Indicator producing Sinewave2 sine and lead-sine values.
/// </summary>
public sealed class Sinewave2Indicator : BaseIndicator<decimal>
{
	private const int BufferLength = 100;
	private const int MinFormed = 7;

	private readonly CyclePeriodIndicator _cyclePeriod = new();

	private readonly double[] _price = new double[BufferLength];
	private readonly double[] _smooth = new double[BufferLength];
	private readonly double[] _cycle = new double[BufferLength];

	private int _index;
	private int _totalBars;
	private double _alphaCache = double.NaN;
	private double _k0;
	private double _k1;
	private double _k2;
	private double _k3;
	private double _rad2Deg;
	private double _deg2Rad;
	private double _lastLead;

	/// <summary>
	/// Alpha parameter controlling smoothing speed.
	/// </summary>
	public decimal Alpha { get; set; } = 0.07m;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
		return new Sinewave2Value(this, input, 0m, 0m);

		UpdateCoefficients();

		var periodValue = _cyclePeriod.Process(input);
		if (!_cyclePeriod.IsFormed)
		{
			IsFormed = false;
			return new Sinewave2Value(this, input, 0m, 0m);
		}

		_index--;
		if (_index < 0)
		_index = BufferLength - 1;

		var price = ((double)candle.HighPrice + (double)candle.LowPrice) / 2.0;
		_price[_index] = price;

		var idx1 = (_index + 1) % BufferLength;
		var idx2 = (_index + 2) % BufferLength;
		var idx3 = (_index + 3) % BufferLength;

		_smooth[_index] = (_price[_index] + 2.0 * _price[idx1] + 2.0 * _price[idx2] + _price[idx3]) / 6.0;

		if (_totalBars > 6)
		_cycle[_index] = _k0 * (_smooth[_index] - _k1 * _smooth[idx1] + _smooth[idx2]) + _k2 * _cycle[idx1] - _k3 * _cycle[idx2];
		else
		_cycle[_index] = (_price[_index] - 2.0 * _price[idx1] + _price[idx2]) / 4.0;

		_totalBars++;
		var samples = Math.Min(_totalBars, BufferLength);

		var dcPeriod = (int)Math.Floor(periodValue.ToDecimal());
		dcPeriod = Math.Max(1, Math.Min(dcPeriod, samples));

		double real = 0.0;
		double imag = 0.0;

		for (var i = 0; i < dcPeriod; i++)
		{
			var idx = (_index + i) % BufferLength;
			var arg = _deg2Rad * 360.0 * i / dcPeriod;
			real += Math.Sin(arg) * _cycle[idx];
			imag += Math.Cos(arg) * _cycle[idx];
		}

		double dcPhase;
		if (Math.Abs(imag) > 0.001)
		dcPhase = _rad2Deg * Math.Atan(real / imag);
		else
		dcPhase = real >= 0.0 ? 90.0 : -90.0;

		dcPhase += 90.0;
		if (imag < 0.0)
		dcPhase += 180.0;
		if (dcPhase > 315.0)
		dcPhase -= 360.0;

		var sine = Math.Sin(dcPhase * _deg2Rad);
		var lead = Math.Sin((dcPhase + 45.0) * _deg2Rad);

		if (Math.Abs(dcPhase - 180.0) < 1e-6 && _lastLead > 0.0)
		lead = Math.Sin(45.0 * _deg2Rad);
		if (Math.Abs(dcPhase) < 1e-6 && _lastLead < 0.0)
		lead = Math.Sin(225.0 * _deg2Rad);

		_lastLead = lead;
		IsFormed = _totalBars >= MinFormed && _cyclePeriod.IsFormed;

		return new Sinewave2Value(this, input, (decimal)sine, (decimal)lead);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_cyclePeriod.Reset();
		Array.Clear(_price, 0, _price.Length);
		Array.Clear(_smooth, 0, _smooth.Length);
		Array.Clear(_cycle, 0, _cycle.Length);
		_index = 0;
		_totalBars = 0;
		_lastLead = 0.0;
		_alphaCache = double.NaN;
	}

	private void UpdateCoefficients()
	{
		// Recalculate smoothing factors only when the alpha parameter changes.
		var alpha = (double)Alpha;
		if (Math.Abs(alpha - _alphaCache) < double.Epsilon)
		return;

		_alphaCache = alpha;

		_k0 = Math.Pow(1.0 - 0.5 * alpha, 2.0);
		_k1 = 2.0;
		_k2 = _k1 * (1.0 - alpha);
		_k3 = Math.Pow(1.0 - alpha, 2.0);
		_rad2Deg = 45.0 / Math.Atan(1.0);
		_deg2Rad = 1.0 / _rad2Deg;

		_cyclePeriod.Alpha = Alpha;
	}
}

/// <summary>
/// Complex indicator value exposing Sinewave2 sine and lead components.
/// </summary>
public sealed class Sinewave2Value : ComplexIndicatorValue
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Sinewave2Value"/> class.
	/// </summary>
	public Sinewave2Value(IIndicator indicator, IIndicatorValue input, decimal sine, decimal lead)
	: base(indicator, input, (nameof(Sine), sine), (nameof(Lead), lead))
	{
	}

	/// <summary>
	/// Sine component.
	/// </summary>
	public decimal Sine => (decimal)GetValue(nameof(Sine));

	/// <summary>
	/// Lead sine component.
	/// </summary>
	public decimal Lead => (decimal)GetValue(nameof(Lead));
}

/// <summary>
/// Adaptive cycle period indicator used inside the Sinewave2 calculation.
/// </summary>
public sealed class CyclePeriodIndicator : BaseIndicator<decimal>
{
	private const int BufferLength = 7;
	private const int MedianLength = 5;
	private const int MinFormed = MedianLength + 16;

	private readonly double[] _price = new double[BufferLength];
	private readonly double[] _smooth = new double[BufferLength];
	private readonly double[] _cycle = new double[BufferLength];
	private readonly double[] _q1 = new double[BufferLength];
	private readonly double[] _i1 = new double[BufferLength];
	private readonly double[] _deltaPhase = new double[MedianLength];
	private readonly double[] _medianBuffer = new double[MedianLength];

	private int _index1;
	private int _index2;
	private int _barsProcessed;
	private double _alphaCache = double.NaN;
	private double _k0;
	private double _k1;
	private double _k2;
	private double _k3;
	private double _f0;
	private double _f1;
	private double _f2;
	private double _f3;
	private double _instPeriod = 1.0;
	private double _cyclePeriod = 1.0;

	/// <summary>
	/// Alpha smoothing factor.
	/// </summary>
	public decimal Alpha { get; set; } = 0.07m;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
		return new DecimalIndicatorValue(this, 0m, input.Time);

		UpdateCoefficients();

		_index1--;
		if (_index1 < 0)
		_index1 = BufferLength - 1;

		_index2--;
		if (_index2 < 0)
		_index2 = MedianLength - 1;

		var bar0 = _index1;
		var bar1 = (bar0 + 1) % BufferLength;
		var bar2 = (bar0 + 2) % BufferLength;
		var bar3 = (bar0 + 3) % BufferLength;
		var bar4 = (bar0 + 4) % BufferLength;
		var bar6 = (bar0 + 6) % BufferLength;

		var price = ((double)candle.HighPrice + (double)candle.LowPrice) / 2.0;
		_price[bar0] = price;
		_smooth[bar0] = (price + 2.0 * _price[bar1] + 2.0 * _price[bar2] + _price[bar3]) / 6.0;

		if (_barsProcessed < 6)
		_cycle[bar0] = (price - 2.0 * _price[bar1] + _price[bar2]) / 4.0;
		else
		_cycle[bar0] = _k0 * (_smooth[bar0] - _k1 * _smooth[bar1] + _smooth[bar2]) + _k2 * _cycle[bar1] - _k3 * _cycle[bar2];

		_q1[bar0] = (_f0 * _cycle[bar0] + _f1 * _cycle[bar2] - _f1 * _cycle[bar4] - _f0 * _cycle[bar6]) * (_f2 + _f3 * _instPeriod);
		_i1[bar0] = _cycle[bar3];

		var qCurrent = _q1[bar0];
		var qPrev = _q1[bar1];
		if (Math.Abs(qCurrent) > double.Epsilon && Math.Abs(qPrev) > double.Epsilon)
		{
			var delta = (_i1[bar0] / qCurrent - _i1[bar1] / qPrev) /
			(1.0 + _i1[bar0] * _i1[bar1] / (qCurrent * qPrev));
			_deltaPhase[_index2] = delta;
		}

		var clamped = Math.Max(0.1, Math.Min(1.1, _deltaPhase[_index2]));
		_deltaPhase[_index2] = clamped;

		Array.Copy(_deltaPhase, _medianBuffer, MedianLength);
		Array.Sort(_medianBuffer);
		var median = _medianBuffer[MedianLength / 2];
		var dc = Math.Abs(median) < double.Epsilon ? 15.0 : 6.28318 / median + 0.5;

		_instPeriod = 0.67 * _instPeriod + 0.33 * dc;
		_cyclePeriod = 0.85 * _cyclePeriod + 0.15 * _instPeriod;

		_barsProcessed++;
		IsFormed = _barsProcessed >= MinFormed;

		return new DecimalIndicatorValue(this, (decimal)_cyclePeriod, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		Array.Clear(_price, 0, _price.Length);
		Array.Clear(_smooth, 0, _smooth.Length);
		Array.Clear(_cycle, 0, _cycle.Length);
		Array.Clear(_q1, 0, _q1.Length);
		Array.Clear(_i1, 0, _i1.Length);
		Array.Clear(_deltaPhase, 0, _deltaPhase.Length);
		Array.Clear(_medianBuffer, 0, _medianBuffer.Length);
		_index1 = 0;
		_index2 = 0;
		_barsProcessed = 0;
		_alphaCache = double.NaN;
		_instPeriod = 1.0;
		_cyclePeriod = 1.0;
	}

	private void UpdateCoefficients()
	{
		// Recalculate smoothing factors only when the alpha parameter changes.
		var alpha = (double)Alpha;
		if (Math.Abs(alpha - _alphaCache) < double.Epsilon)
		return;

		_alphaCache = alpha;

		_k0 = Math.Pow(1.0 - 0.5 * alpha, 2.0);
		_k1 = 2.0;
		_k2 = _k1 * (1.0 - alpha);
		_k3 = Math.Pow(1.0 - alpha, 2.0);
		_f0 = 0.0962;
		_f1 = 0.5769;
		_f2 = 0.5;
		_f3 = 0.08;
	}
}
