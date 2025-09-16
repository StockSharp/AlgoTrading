using System;
using System.Collections.Generic;
using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Crossing of two iMA strategy with optional third filter and trailing protection.
/// </summary>
public class CrossingOfTwoIMaV2Strategy : Strategy
{
	private readonly StrategyParam<int> _firstPeriod;
	private readonly StrategyParam<int> _firstShift;
	private readonly StrategyParam<MaMethod> _firstMethod;
	private readonly StrategyParam<AppliedPriceType> _firstPrice;
	private readonly StrategyParam<int> _secondPeriod;
	private readonly StrategyParam<int> _secondShift;
	private readonly StrategyParam<MaMethod> _secondMethod;
	private readonly StrategyParam<AppliedPriceType> _secondPrice;
	private readonly StrategyParam<bool> _useFilter;
	private readonly StrategyParam<int> _thirdPeriod;
	private readonly StrategyParam<int> _thirdShift;
	private readonly StrategyParam<MaMethod> _thirdMethod;
	private readonly StrategyParam<AppliedPriceType> _thirdPrice;
	private readonly StrategyParam<bool> _useRiskPercent;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _pipValue;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator _firstMa;
	private IIndicator _secondMa;
	private IIndicator _thirdMa;

	private decimal?[] _firstSeries = Array.Empty<decimal?>();
	private decimal?[] _secondSeries = Array.Empty<decimal?>();
	private decimal?[] _thirdSeries = Array.Empty<decimal?>();

	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakeProfit;
	private decimal? _shortTakeProfit;
	private decimal? _longTrail;
	private decimal? _shortTrail;
	private decimal? _bestLongPrice;
	private decimal? _bestShortPrice;
	private int _barsSinceLastEntry;

	/// <summary>
	/// Period of the first moving average.
	/// </summary>
	public int FirstPeriod
	{
		get => _firstPeriod.Value;
		set => _firstPeriod.Value = value;
	}

	/// <summary>
	/// Shift of the first moving average in bars.
	/// </summary>
	public int FirstShift
	{
		get => _firstShift.Value;
		set => _firstShift.Value = value;
	}

	/// <summary>
	/// Smoothing method for the first moving average.
	/// </summary>
	public MaMethod FirstMethod
	{
		get => _firstMethod.Value;
		set => _firstMethod.Value = value;
	}

	/// <summary>
	/// Price source for the first moving average.
	/// </summary>
	public AppliedPriceType FirstAppliedPrice
	{
		get => _firstPrice.Value;
		set => _firstPrice.Value = value;
	}

	/// <summary>
	/// Period of the second moving average.
	/// </summary>
	public int SecondPeriod
	{
		get => _secondPeriod.Value;
		set => _secondPeriod.Value = value;
	}

	/// <summary>
	/// Shift of the second moving average in bars.
	/// </summary>
	public int SecondShift
	{
		get => _secondShift.Value;
		set => _secondShift.Value = value;
	}

	/// <summary>
	/// Smoothing method for the second moving average.
	/// </summary>
	public MaMethod SecondMethod
	{
		get => _secondMethod.Value;
		set => _secondMethod.Value = value;
	}

	/// <summary>
	/// Price source for the second moving average.
	/// </summary>
	public AppliedPriceType SecondAppliedPrice
	{
		get => _secondPrice.Value;
		set => _secondPrice.Value = value;
	}

	/// <summary>
	/// Enable the third moving average filter.
	/// </summary>
	public bool UseFilter
	{
		get => _useFilter.Value;
		set => _useFilter.Value = value;
	}

	/// <summary>
	/// Period of the third moving average filter.
	/// </summary>
	public int ThirdPeriod
	{
		get => _thirdPeriod.Value;
		set => _thirdPeriod.Value = value;
	}

	/// <summary>
	/// Shift of the third moving average filter in bars.
	/// </summary>
	public int ThirdShift
	{
		get => _thirdShift.Value;
		set => _thirdShift.Value = value;
	}

	/// <summary>
	/// Smoothing method for the third moving average filter.
	/// </summary>
	public MaMethod ThirdMethod
	{
		get => _thirdMethod.Value;
		set => _thirdMethod.Value = value;
	}

	/// <summary>
	/// Price source for the third moving average filter.
	/// </summary>
	public AppliedPriceType ThirdAppliedPrice
	{
		get => _thirdPrice.Value;
		set => _thirdPrice.Value = value;
	}

	/// <summary>
	/// Use risk percentage position sizing instead of fixed volume.
	/// </summary>
	public bool UseRiskPercent
	{
		get => _useRiskPercent.Value;
		set => _useRiskPercent.Value = value;
	}

	/// <summary>
	/// Fixed volume when risk percentage sizing is disabled.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Percentage of equity risked per trade when risk sizing is enabled.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Monetary value of one pip for a single lot.
	/// </summary>
	public decimal PipValue
	{
		get => _pipValue.Value;
		set => _pipValue.Value = value;
	}

	/// <summary>
	/// Stop loss size in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit size in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop size in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum trailing adjustment step in pips.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Candle type used to drive the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="CrossingOfTwoIMaV2Strategy"/>.
	/// </summary>
	public CrossingOfTwoIMaV2Strategy()
	{
		_firstPeriod = Param(nameof(FirstPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("First MA Period", "Period of the first moving average", "First Moving Average")
		.SetCanOptimize();

		_firstShift = Param(nameof(FirstShift), 3)
		.SetGreaterOrEqualZero()
		.SetDisplay("First MA Shift", "Shift (in bars) applied to the first moving average", "First Moving Average")
		.SetCanOptimize();

		_firstMethod = Param(nameof(FirstMethod), MaMethod.Smoothed)
		.SetDisplay("First MA Method", "Smoothing method for the first moving average", "First Moving Average")
		.SetCanOptimize();

		_firstPrice = Param(nameof(FirstAppliedPrice), AppliedPriceType.Close)
		.SetDisplay("First MA Price", "Price source for the first moving average", "First Moving Average");

		_secondPeriod = Param(nameof(SecondPeriod), 8)
		.SetGreaterThanZero()
		.SetDisplay("Second MA Period", "Period of the second moving average", "Second Moving Average")
		.SetCanOptimize();

		_secondShift = Param(nameof(SecondShift), 5)
		.SetGreaterOrEqualZero()
		.SetDisplay("Second MA Shift", "Shift (in bars) applied to the second moving average", "Second Moving Average")
		.SetCanOptimize();

		_secondMethod = Param(nameof(SecondMethod), MaMethod.Smoothed)
		.SetDisplay("Second MA Method", "Smoothing method for the second moving average", "Second Moving Average")
		.SetCanOptimize();

		_secondPrice = Param(nameof(SecondAppliedPrice), AppliedPriceType.Close)
		.SetDisplay("Second MA Price", "Price source for the second moving average", "Second Moving Average");

		_useFilter = Param(nameof(UseFilter), true)
		.SetDisplay("Enable Filter", "Use the third moving average as a directional filter", "Filter");

		_thirdPeriod = Param(nameof(ThirdPeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("Third MA Period", "Period of the third moving average filter", "Filter")
		.SetCanOptimize();

		_thirdShift = Param(nameof(ThirdShift), 8)
		.SetGreaterOrEqualZero()
		.SetDisplay("Third MA Shift", "Shift (in bars) applied to the third moving average filter", "Filter")
		.SetCanOptimize();

		_thirdMethod = Param(nameof(ThirdMethod), MaMethod.Smoothed)
		.SetDisplay("Third MA Method", "Smoothing method for the third moving average filter", "Filter")
		.SetCanOptimize();

		_thirdPrice = Param(nameof(ThirdAppliedPrice), AppliedPriceType.Close)
		.SetDisplay("Third MA Price", "Price source for the third moving average filter", "Filter");

		_useRiskPercent = Param(nameof(UseRiskPercent), true)
		.SetDisplay("Risk Based Sizing", "Use percentage risk position sizing", "Risk");

		_fixedVolume = Param(nameof(FixedVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Fixed Volume", "Trade volume when fixed sizing is enabled", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 5m)
		.SetGreaterThanZero()
		.SetDisplay("Risk Percent", "Percentage of equity risked per trade", "Risk")
		.SetCanOptimize();

		_pipValue = Param(nameof(PipValue), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Pip Value", "Monetary value of one pip for a single lot", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 50)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss", "Stop loss distance in pips", "Protection");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit", "Take profit distance in pips", "Protection");

		_trailingStopPips = Param(nameof(TrailingStopPips), 10)
		.SetGreaterOrEqualZero()
		.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Protection");

		_trailingStepPips = Param(nameof(TrailingStepPips), 4)
		.SetGreaterOrEqualZero()
		.SetDisplay("Trailing Step", "Minimum trailing stop adjustment in pips", "Protection");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle type used for analysis", "General");

		_barsSinceLastEntry = int.MaxValue;
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

		_firstMa = null;
		_secondMa = null;
		_thirdMa = null;
		_firstSeries = Array.Empty<decimal?>();
		_secondSeries = Array.Empty<decimal?>();
		_thirdSeries = Array.Empty<decimal?>();
		ResetTradeState();
		_barsSinceLastEntry = int.MaxValue;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_firstMa = CreateMovingAverage(FirstMethod, FirstPeriod);
		_secondMa = CreateMovingAverage(SecondMethod, SecondPeriod);
		_thirdMa = UseFilter ? CreateMovingAverage(ThirdMethod, ThirdPeriod) : null;

		_firstSeries = new decimal?[FirstShift + 3];
		_secondSeries = new decimal?[SecondShift + 3];
		_thirdSeries = UseFilter ? new decimal?[ThirdShift + 1] : Array.Empty<decimal?>();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (_firstMa != null)
			{
				DrawIndicator(area, _firstMa);
			}

			if (_secondMa != null)
			{
				DrawIndicator(area, _secondMa);
			}

			if (UseFilter && _thirdMa != null)
			{
				DrawIndicator(area, _thirdMa);
			}

			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Ignore unfinished candles to work on closed bars only.
		if (candle.State != CandleStates.Finished)
		return;

		if (_barsSinceLastEntry < int.MaxValue)
		_barsSinceLastEntry++;

		// Manage open positions and exit if protections trigger.
		if (UpdateRiskManagement(candle))
		return;

		var firstInput = GetAppliedPrice(candle, FirstAppliedPrice);
		var secondInput = GetAppliedPrice(candle, SecondAppliedPrice);
		var thirdInput = UseFilter ? GetAppliedPrice(candle, ThirdAppliedPrice) : (decimal?)null;

		// Update indicator series with the latest values.
		var firstValue = _firstMa?.Process(firstInput, candle.OpenTime, true);
		ShiftSeries(_firstSeries, firstValue?.IsFinal == true ? firstValue.ToDecimal() : (decimal?)null);

		var secondValue = _secondMa?.Process(secondInput, candle.OpenTime, true);
		ShiftSeries(_secondSeries, secondValue?.IsFinal == true ? secondValue.ToDecimal() : (decimal?)null);

		if (UseFilter && _thirdMa != null && _thirdSeries.Length > 0 && thirdInput.HasValue)
		{
			var thirdValue = _thirdMa.Process(thirdInput.Value, candle.OpenTime, true);
			ShiftSeries(_thirdSeries, thirdValue.IsFinal ? thirdValue.ToDecimal() : (decimal?)null);
		}

		// Ensure we have enough data for crossover evaluation.
		if (!HasSeriesValue(_firstSeries, FirstShift, 2) || !HasSeriesValue(_secondSeries, SecondShift, 2))
		return;

		var first0 = GetSeriesValue(_firstSeries, FirstShift, 0)!.Value;
		var first1 = GetSeriesValue(_firstSeries, FirstShift, 1)!.Value;
		var second0 = GetSeriesValue(_secondSeries, SecondShift, 0)!.Value;
		var second1 = GetSeriesValue(_secondSeries, SecondShift, 1)!.Value;

		var buySignal = first0 > second0 && first1 < second1;
		var sellSignal = first0 < second0 && first1 > second1;

		if (!buySignal && !sellSignal && HasSeriesValue(_firstSeries, FirstShift, 2) && HasSeriesValue(_secondSeries, SecondShift, 2))
		{
			var first2 = GetSeriesValue(_firstSeries, FirstShift, 2)!.Value;
			var second2 = GetSeriesValue(_secondSeries, SecondShift, 2)!.Value;

			if (first0 > second0 && first2 < second2 && _barsSinceLastEntry > 2)
			{
				buySignal = true;
			}
			else if (first0 < second0 && first2 > second2 && _barsSinceLastEntry > 2)
			{
				sellSignal = true;
			}
		}

		if (UseFilter)
		{
			if (!_thirdSeries.IsNullOrEmpty() && HasSeriesValue(_thirdSeries, ThirdShift, 0))
			{
				var filterValue = GetSeriesValue(_thirdSeries, ThirdShift, 0)!.Value;
				if (buySignal && filterValue >= first0)
				buySignal = false;
				if (sellSignal && filterValue <= first0)
				sellSignal = false;
			}
			else if (buySignal || sellSignal)
			{
				return;
			}
		}

		// Check trading permissions before submitting orders.
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (buySignal && Position <= 0)
		{
			EnterLong(candle);
		}
		else if (sellSignal && Position >= 0)
		{
			EnterShort(candle);
		}
	}

	private void EnterLong(ICandleMessage candle)
	{
		var volume = GetEntryVolume();
		if (volume <= 0m)
		return;

		if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		BuyMarket(volume);
		SetLongProtection(candle.ClosePrice);
		_barsSinceLastEntry = 0;
	}

	private void EnterShort(ICandleMessage candle)
	{
		var volume = GetEntryVolume();
		if (volume <= 0m)
		return;

		if (Position > 0)
		{
			SellMarket(Math.Abs(Position));
		}

		SellMarket(volume);
		SetShortProtection(candle.ClosePrice);
		_barsSinceLastEntry = 0;
	}

	private decimal GetEntryVolume()
	{
		if (!UseRiskPercent)
		return FixedVolume;

		var equity = Portfolio?.CurrentValue ?? Portfolio?.CurrentBalance ?? 0m;
		if (equity <= 0m)
		return FixedVolume;

		var riskAmount = equity * RiskPercent / 100m;
		var riskPips = StopLossPips > 0 ? StopLossPips : TrailingStopPips;
		if (riskPips <= 0)
		return FixedVolume;

		if (PipValue <= 0m)
		return FixedVolume;

		var volume = riskAmount / (riskPips * PipValue);
		return volume > 0m ? volume : FixedVolume;
	}

	private bool UpdateRiskManagement(ICandleMessage candle)
	{
		var point = GetPointValue();
		var trailingStep = TrailingStepPips > 0 ? TrailingStepPips * point : 0m;

		if (Position > 0)
		{
			_bestLongPrice = _bestLongPrice.HasValue ? Math.Max(_bestLongPrice.Value, candle.HighPrice) : candle.HighPrice;

			if (TrailingStopPips > 0 && _bestLongPrice.HasValue)
			{
				var desiredStop = _bestLongPrice.Value - TrailingStopPips * point;
				if (!_longTrail.HasValue || desiredStop - _longTrail.Value >= trailingStep)
				_longTrail = desiredStop;
			}

			var exitStop = CombineLongStops(_longStopPrice, _longTrail);
			if (exitStop.HasValue && candle.LowPrice <= exitStop.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeState();
				return true;
			}

			if (_longTakeProfit.HasValue && candle.HighPrice >= _longTakeProfit.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeState();
				return true;
			}
		}
		else if (Position < 0)
		{
			_bestShortPrice = _bestShortPrice.HasValue ? Math.Min(_bestShortPrice.Value, candle.LowPrice) : candle.LowPrice;

			if (TrailingStopPips > 0 && _bestShortPrice.HasValue)
			{
				var desiredStop = _bestShortPrice.Value + TrailingStopPips * point;
				if (!_shortTrail.HasValue || _shortTrail.Value - desiredStop >= trailingStep)
				_shortTrail = desiredStop;
			}

			var exitStop = CombineShortStops(_shortStopPrice, _shortTrail);
			if (exitStop.HasValue && candle.HighPrice >= exitStop.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeState();
				return true;
			}

			if (_shortTakeProfit.HasValue && candle.LowPrice <= _shortTakeProfit.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeState();
				return true;
			}
		}
		else
		{
			ResetTradeState();
		}

		return false;
	}

	private void SetLongProtection(decimal entryPrice)
	{
		var point = GetPointValue();
		_longStopPrice = StopLossPips > 0 ? entryPrice - StopLossPips * point : null;
		_longTakeProfit = TakeProfitPips > 0 ? entryPrice + TakeProfitPips * point : null;
		_longTrail = TrailingStopPips > 0 ? entryPrice - TrailingStopPips * point : null;
		_bestLongPrice = entryPrice;
		_shortStopPrice = null;
		_shortTakeProfit = null;
		_shortTrail = null;
		_bestShortPrice = null;
	}

	private void SetShortProtection(decimal entryPrice)
	{
		var point = GetPointValue();
		_shortStopPrice = StopLossPips > 0 ? entryPrice + StopLossPips * point : null;
		_shortTakeProfit = TakeProfitPips > 0 ? entryPrice - TakeProfitPips * point : null;
		_shortTrail = TrailingStopPips > 0 ? entryPrice + TrailingStopPips * point : null;
		_bestShortPrice = entryPrice;
		_longStopPrice = null;
		_longTakeProfit = null;
		_longTrail = null;
		_bestLongPrice = null;
	}

	private void ResetTradeState()
	{
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakeProfit = null;
		_shortTakeProfit = null;
		_longTrail = null;
		_shortTrail = null;
		_bestLongPrice = null;
		_bestShortPrice = null;
	}

	private decimal GetPointValue()
	{
		var point = Security?.PriceStep ?? 1m;
		return point > 0m ? point : 1m;
	}

	private static void ShiftSeries(decimal?[] series, decimal? value)
	{
		if (series.Length == 0)
		return;

		for (var i = series.Length - 1; i > 0; i--)
		{
			series[i] = series[i - 1];
		}

		series[0] = value;
	}

	private static bool HasSeriesValue(decimal?[] series, int shift, int depth)
	{
		var index = shift + depth;
		return index < series.Length && series[index].HasValue;
	}

	private static decimal? GetSeriesValue(decimal?[] series, int shift, int depth)
	{
		var index = shift + depth;
		return index < series.Length ? series[index] : null;
	}

	private static decimal? CombineLongStops(decimal? stopLoss, decimal? trailing)
	{
		if (stopLoss.HasValue && trailing.HasValue)
		return Math.Max(stopLoss.Value, trailing.Value);
		return stopLoss ?? trailing;
	}

	private static decimal? CombineShortStops(decimal? stopLoss, decimal? trailing)
	{
		if (stopLoss.HasValue && trailing.HasValue)
		return Math.Min(stopLoss.Value, trailing.Value);
		return stopLoss ?? trailing;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceType priceType)
	{
		return priceType switch
		{
			AppliedPriceType.Open => candle.OpenPrice,
			AppliedPriceType.High => candle.HighPrice,
			AppliedPriceType.Low => candle.LowPrice,
			AppliedPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceType.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceType.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}

	private static IIndicator CreateMovingAverage(MaMethod method, int period)
	{
		return method switch
		{
			MaMethod.Simple => new SimpleMovingAverage { Length = period },
			MaMethod.Exponential => new ExponentialMovingAverage { Length = period },
			MaMethod.Smoothed => new SmoothedMovingAverage { Length = period },
			MaMethod.Weighted => new WeightedMovingAverage { Length = period },
			_ => new SimpleMovingAverage { Length = period }
		};
	}

	/// <summary>
	/// Moving average smoothing methods supported by the strategy.
	/// </summary>
	public enum MaMethod
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted
	}

	/// <summary>
	/// Price sources supported for indicator calculations.
	/// </summary>
	public enum AppliedPriceType
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted
	}
}
