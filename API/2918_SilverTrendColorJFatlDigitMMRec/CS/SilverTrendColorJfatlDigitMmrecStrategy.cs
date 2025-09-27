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
/// Port of the "Exp_SilverTrend_ColorJFatl_Digit_MMRec" expert advisor.
/// Combines SilverTrend candle coloring with a smoothed FATL line to manage two independent virtual blocks.
/// </summary>
public class SilverTrendColorJfatlDigitMmrecStrategy : Strategy
{
	private readonly StrategyParam<DataType> _silverCandleType;
	private readonly StrategyParam<int> _silverSsp;
	private readonly StrategyParam<int> _silverRisk;
	private readonly StrategyParam<int> _silverSignalBar;
	private readonly StrategyParam<bool> _silverAllowLong;
	private readonly StrategyParam<bool> _silverAllowShort;
	private readonly StrategyParam<bool> _silverCloseLong;
	private readonly StrategyParam<bool> _silverCloseShort;
	private readonly StrategyParam<decimal> _silverVolume;
	private readonly StrategyParam<int> _silverStopLossPoints;
	private readonly StrategyParam<int> _silverTakeProfitPoints;

	private readonly StrategyParam<DataType> _colorCandleType;
	private readonly StrategyParam<int> _colorLength;
	private readonly StrategyParam<int> _colorPhase;
	private readonly StrategyParam<AppliedPrice> _colorPriceType;
	private readonly StrategyParam<int> _colorDigit;
	private readonly StrategyParam<int> _colorSignalBar;
	private readonly StrategyParam<bool> _colorAllowLong;
	private readonly StrategyParam<bool> _colorAllowShort;
	private readonly StrategyParam<bool> _colorCloseLong;
	private readonly StrategyParam<bool> _colorCloseShort;
	private readonly StrategyParam<decimal> _colorVolume;
	private readonly StrategyParam<int> _colorStopLossPoints;
	private readonly StrategyParam<int> _colorTakeProfitPoints;

	private SilverTrendCalculator _silverCalculator = null!;
	private ColorJfatlCalculator _colorCalculator = null!;

	private readonly List<int> _silverColors = new();
	private readonly List<int> _colorColors = new();

	private decimal _silverPosition;
	private decimal _colorPosition;
	private decimal _silverEntryPrice;
	private decimal _colorEntryPrice;

	/// <summary>
	/// Applied price modes supported by the ColorJFatl module.
	/// </summary>
	public enum AppliedPrice
	{
		PriceClose = 1,
		PriceOpen,
		PriceHigh,
		PriceLow,
		PriceMedian,
		PriceTypical,
		PriceWeighted,
		PriceSimple,
		PriceQuarter,
		PriceTrendFollow0,
		PriceTrendFollow1,
		PriceDeMark
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SilverTrendColorJfatlDigitMmrecStrategy"/> class.
	/// </summary>
	public SilverTrendColorJfatlDigitMmrecStrategy()
	{
		Volume = 1m;

		_silverCandleType = Param(nameof(SilverCandleType), TimeSpan.FromHours(6).TimeFrame())
		.SetDisplay("Silver Candle Type", "Timeframe used by the SilverTrend block", "SilverTrend");

		_silverSsp = Param(nameof(SilverSsp), 9)
		.SetGreaterThanZero()
		.SetDisplay("SSP", "Depth for SilverTrend range calculations", "SilverTrend");

		_silverRisk = Param(nameof(SilverRisk), 3)
		.SetGreaterThanZero()
		.SetDisplay("Risk", "Risk input that shifts the SilverTrend channel", "SilverTrend");

		_silverSignalBar = Param(nameof(SilverSignalBar), 1)
		.SetNotNegative()
		.SetDisplay("Signal Bar", "Bar shift used for SilverTrend signals", "SilverTrend");

		_silverAllowLong = Param(nameof(SilverAllowLong), true)
		.SetDisplay("Allow Silver Long", "Enable long entries for SilverTrend", "SilverTrend");

		_silverAllowShort = Param(nameof(SilverAllowShort), true)
		.SetDisplay("Allow Silver Short", "Enable short entries for SilverTrend", "SilverTrend");

		_silverCloseLong = Param(nameof(SilverCloseLong), true)
		.SetDisplay("Close Silver Long", "Allow SilverTrend to close long positions", "SilverTrend");

		_silverCloseShort = Param(nameof(SilverCloseShort), true)
		.SetDisplay("Close Silver Short", "Allow SilverTrend to close short positions", "SilverTrend");

		_silverVolume = Param(nameof(SilverVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Silver Volume", "Order volume used by the SilverTrend block", "SilverTrend");

		_silverStopLossPoints = Param(nameof(SilverStopLossPoints), 1000)
		.SetNotNegative()
		.SetDisplay("Silver SL", "Stop-loss distance in points for SilverTrend", "SilverTrend");

		_silverTakeProfitPoints = Param(nameof(SilverTakeProfitPoints), 2500)
		.SetNotNegative()
		.SetDisplay("Silver TP", "Take-profit distance in points for SilverTrend", "SilverTrend");

		_colorCandleType = Param(nameof(ColorCandleType), TimeSpan.FromHours(6).TimeFrame())
		.SetDisplay("Color Candle Type", "Timeframe used by the ColorJFatl block", "ColorJFatl");

		_colorLength = Param(nameof(ColorLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("JMA Length", "Smoothing length for the FATL line", "ColorJFatl");

		_colorPhase = Param(nameof(ColorPhase), -100)
		.SetDisplay("JMA Phase", "Phase shift parameter for the FATL smoother", "ColorJFatl");

		_colorPriceType = Param(nameof(ColorPriceType), AppliedPrice.PriceClose)
		.SetDisplay("Applied Price", "Source price used for FATL", "ColorJFatl");

		_colorDigit = Param(nameof(ColorDigit), 2)
		.SetNotNegative()
		.SetDisplay("Digits", "Number of decimal digits for FATL rounding", "ColorJFatl");

		_colorSignalBar = Param(nameof(ColorSignalBar), 1)
		.SetNotNegative()
		.SetDisplay("Color Signal Bar", "Bar shift used for ColorJFatl signals", "ColorJFatl");

		_colorAllowLong = Param(nameof(ColorAllowLong), true)
		.SetDisplay("Allow Color Long", "Enable long entries for ColorJFatl", "ColorJFatl");

		_colorAllowShort = Param(nameof(ColorAllowShort), true)
		.SetDisplay("Allow Color Short", "Enable short entries for ColorJFatl", "ColorJFatl");

		_colorCloseLong = Param(nameof(ColorCloseLong), true)
		.SetDisplay("Close Color Long", "Allow ColorJFatl to close long positions", "ColorJFatl");

		_colorCloseShort = Param(nameof(ColorCloseShort), true)
		.SetDisplay("Close Color Short", "Allow ColorJFatl to close short positions", "ColorJFatl");

		_colorVolume = Param(nameof(ColorVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Color Volume", "Order volume used by the ColorJFatl block", "ColorJFatl");

		_colorStopLossPoints = Param(nameof(ColorStopLossPoints), 1000)
		.SetNotNegative()
		.SetDisplay("Color SL", "Stop-loss distance in points for ColorJFatl", "ColorJFatl");

		_colorTakeProfitPoints = Param(nameof(ColorTakeProfitPoints), 2500)
		.SetNotNegative()
		.SetDisplay("Color TP", "Take-profit distance in points for ColorJFatl", "ColorJFatl");
	}

	/// <summary>
	/// Timeframe used to calculate SilverTrend signals.
	/// </summary>
	public DataType SilverCandleType
	{
		get => _silverCandleType.Value;
		set => _silverCandleType.Value = value;
	}

	/// <summary>
	/// Period of the SilverTrend channel.
	/// </summary>
	public int SilverSsp
	{
		get => _silverSsp.Value;
		set => _silverSsp.Value = value;
	}

	/// <summary>
	/// Risk coefficient for SilverTrend.
	/// </summary>
	public int SilverRisk
	{
		get => _silverRisk.Value;
		set => _silverRisk.Value = value;
	}

	/// <summary>
	/// Bar shift used when reading SilverTrend colors.
	/// </summary>
	public int SilverSignalBar
	{
		get => _silverSignalBar.Value;
		set => _silverSignalBar.Value = value;
	}

	/// <summary>
	/// Enables SilverTrend long entries.
	/// </summary>
	public bool SilverAllowLong
	{
		get => _silverAllowLong.Value;
		set => _silverAllowLong.Value = value;
	}

	/// <summary>
	/// Enables SilverTrend short entries.
	/// </summary>
	public bool SilverAllowShort
	{
		get => _silverAllowShort.Value;
		set => _silverAllowShort.Value = value;
	}

	/// <summary>
	/// Enables SilverTrend long exits.
	/// </summary>
	public bool SilverCloseLong
	{
		get => _silverCloseLong.Value;
		set => _silverCloseLong.Value = value;
	}

	/// <summary>
	/// Enables SilverTrend short exits.
	/// </summary>
	public bool SilverCloseShort
	{
		get => _silverCloseShort.Value;
		set => _silverCloseShort.Value = value;
	}

	/// <summary>
	/// Order volume for the SilverTrend block.
	/// </summary>
	public decimal SilverVolume
	{
		get => _silverVolume.Value;
		set => _silverVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in points for SilverTrend.
	/// </summary>
	public int SilverStopLossPoints
	{
		get => _silverStopLossPoints.Value;
		set => _silverStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in points for SilverTrend.
	/// </summary>
	public int SilverTakeProfitPoints
	{
		get => _silverTakeProfitPoints.Value;
		set => _silverTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Timeframe used to calculate ColorJFatl signals.
	/// </summary>
	public DataType ColorCandleType
	{
		get => _colorCandleType.Value;
		set => _colorCandleType.Value = value;
	}

	/// <summary>
	/// Length of the exponential smoothing that emulates JMA.
	/// </summary>
	public int ColorLength
	{
		get => _colorLength.Value;
		set => _colorLength.Value = value;
	}

	/// <summary>
	/// Phase parameter from the original indicator (kept for compatibility).
	/// </summary>
	public int ColorPhase
	{
		get => _colorPhase.Value;
		set => _colorPhase.Value = value;
	}

	/// <summary>
	/// Price source used when constructing the FATL series.
	/// </summary>
	public AppliedPrice ColorPriceType
	{
		get => _colorPriceType.Value;
		set => _colorPriceType.Value = value;
	}

	/// <summary>
	/// Rounding precision for the FATL series.
	/// </summary>
	public int ColorDigit
	{
		get => _colorDigit.Value;
		set => _colorDigit.Value = value;
	}

	/// <summary>
	/// Bar shift used when reading ColorJFatl colors.
	/// </summary>
	public int ColorSignalBar
	{
		get => _colorSignalBar.Value;
		set => _colorSignalBar.Value = value;
	}

	/// <summary>
	/// Enables ColorJFatl long entries.
	/// </summary>
	public bool ColorAllowLong
	{
		get => _colorAllowLong.Value;
		set => _colorAllowLong.Value = value;
	}

	/// <summary>
	/// Enables ColorJFatl short entries.
	/// </summary>
	public bool ColorAllowShort
	{
		get => _colorAllowShort.Value;
		set => _colorAllowShort.Value = value;
	}

	/// <summary>
	/// Enables ColorJFatl long exits.
	/// </summary>
	public bool ColorCloseLong
	{
		get => _colorCloseLong.Value;
		set => _colorCloseLong.Value = value;
	}

	/// <summary>
	/// Enables ColorJFatl short exits.
	/// </summary>
	public bool ColorCloseShort
	{
		get => _colorCloseShort.Value;
		set => _colorCloseShort.Value = value;
	}

	/// <summary>
	/// Order volume for the ColorJFatl block.
	/// </summary>
	public decimal ColorVolume
	{
		get => _colorVolume.Value;
		set => _colorVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in points for ColorJFatl.
	/// </summary>
	public int ColorStopLossPoints
	{
		get => _colorStopLossPoints.Value;
		set => _colorStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in points for ColorJFatl.
	/// </summary>
	public int ColorTakeProfitPoints
	{
		get => _colorTakeProfitPoints.Value;
		set => _colorTakeProfitPoints.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, SilverCandleType), (Security, ColorCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_silverColors.Clear();
		_colorColors.Clear();
		_silverPosition = 0m;
		_colorPosition = 0m;
		_silverEntryPrice = 0m;
		_colorEntryPrice = 0m;

		_silverCalculator?.Reset();
		_colorCalculator?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_silverCalculator = new SilverTrendCalculator(SilverSsp, SilverRisk);
		_colorCalculator = new ColorJfatlCalculator(ColorLength, ColorPhase, ColorPriceType, ColorDigit);

		var silverSubscription = SubscribeCandles(SilverCandleType);
		silverSubscription.Bind(ProcessSilverCandle).Start();

		var colorSubscription = SubscribeCandles(ColorCandleType);
		colorSubscription.Bind(ProcessColorCandle).Start();

		var chartArea = CreateChartArea();
		if (chartArea != null)
		{
			DrawCandles(chartArea, silverSubscription);
			DrawOwnTrades(chartArea);
		}
	}

	private void ProcessSilverCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var color = _silverCalculator.Process(candle);
		if (color is null)
		return;

		UpdateColorHistory(_silverColors, color.Value, SilverSignalBar + 3);

		if (!TryGetColors(_silverColors, SilverSignalBar, out var currentColor, out var previousColor))
		return;

		var buySignal = currentColor < 2 && previousColor > 1;
		var sellSignal = currentColor > 2 && previousColor < 3;

		if (buySignal)
		{
			if (SilverCloseShort && _silverPosition < 0m)
			SetSilverPosition(0m, candle.ClosePrice);

			if (SilverAllowLong)
			SetSilverPosition(SilverVolume, candle.ClosePrice);
		}
		else if (sellSignal)
		{
			if (SilverCloseLong && _silverPosition > 0m)
			SetSilverPosition(0m, candle.ClosePrice);

			if (SilverAllowShort)
			SetSilverPosition(-SilverVolume, candle.ClosePrice);
		}

		CheckSilverRisk(candle);
	}

	private void ProcessColorCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var result = _colorCalculator.Process(candle);
		if (result.Color is null)
		return;

		UpdateColorHistory(_colorColors, result.Color.Value, ColorSignalBar + 3);

		if (!TryGetColors(_colorColors, ColorSignalBar, out var currentColor, out var previousColor))
		return;

		var buySignal = currentColor == 2 && previousColor < 2;
		var sellSignal = currentColor == 0 && previousColor > 0;

		if (buySignal)
		{
			if (ColorCloseShort && _colorPosition < 0m)
			SetColorPosition(0m, candle.ClosePrice);

			if (ColorAllowLong)
			SetColorPosition(ColorVolume, candle.ClosePrice);
		}
		else if (sellSignal)
		{
			if (ColorCloseLong && _colorPosition > 0m)
			SetColorPosition(0m, candle.ClosePrice);

			if (ColorAllowShort)
			SetColorPosition(-ColorVolume, candle.ClosePrice);
		}

		CheckColorRisk(candle);
	}

	private void SetSilverPosition(decimal target, decimal price)
	{
		if (_silverPosition == target)
		return;

		var delta = target - _silverPosition;
		if (delta > 0m)
		BuyMarket(delta);
		else if (delta < 0m)
		SellMarket(-delta);

		_silverPosition = target;
		_silverEntryPrice = target != 0m ? price : 0m;
	}

	private void SetColorPosition(decimal target, decimal price)
	{
		if (_colorPosition == target)
		return;

		var delta = target - _colorPosition;
		if (delta > 0m)
		BuyMarket(delta);
		else if (delta < 0m)
		SellMarket(-delta);

		_colorPosition = target;
		_colorEntryPrice = target != 0m ? price : 0m;
	}

	private void CheckSilverRisk(ICandleMessage candle)
	{
		if (_silverPosition == 0m)
		return;

		var step = Security?.PriceStep;
		if (step is null || step.Value <= 0m)
		return;

		var stepValue = step.Value;

		if (_silverPosition > 0m)
		{
			if (SilverTakeProfitPoints > 0)
			{
				var targetPrice = _silverEntryPrice + stepValue * SilverTakeProfitPoints;
				if (candle.ClosePrice >= targetPrice)
				{
					SetSilverPosition(0m, candle.ClosePrice);
					return;
				}
			}

			if (SilverStopLossPoints > 0)
			{
				var stopPrice = _silverEntryPrice - stepValue * SilverStopLossPoints;
				if (candle.ClosePrice <= stopPrice)
				SetSilverPosition(0m, candle.ClosePrice);
			}
		}
		else if (_silverPosition < 0m)
		{
			if (SilverTakeProfitPoints > 0)
			{
				var targetPrice = _silverEntryPrice - stepValue * SilverTakeProfitPoints;
				if (candle.ClosePrice <= targetPrice)
				{
					SetSilverPosition(0m, candle.ClosePrice);
					return;
				}
			}

			if (SilverStopLossPoints > 0)
			{
				var stopPrice = _silverEntryPrice + stepValue * SilverStopLossPoints;
				if (candle.ClosePrice >= stopPrice)
				SetSilverPosition(0m, candle.ClosePrice);
			}
		}
	}

	private void CheckColorRisk(ICandleMessage candle)
	{
		if (_colorPosition == 0m)
		return;

		var step = Security?.PriceStep;
		if (step is null || step.Value <= 0m)
		return;

		var stepValue = step.Value;

		if (_colorPosition > 0m)
		{
			if (ColorTakeProfitPoints > 0)
			{
				var targetPrice = _colorEntryPrice + stepValue * ColorTakeProfitPoints;
				if (candle.ClosePrice >= targetPrice)
				{
					SetColorPosition(0m, candle.ClosePrice);
					return;
				}
			}

			if (ColorStopLossPoints > 0)
			{
				var stopPrice = _colorEntryPrice - stepValue * ColorStopLossPoints;
				if (candle.ClosePrice <= stopPrice)
				SetColorPosition(0m, candle.ClosePrice);
			}
		}
		else if (_colorPosition < 0m)
		{
			if (ColorTakeProfitPoints > 0)
			{
				var targetPrice = _colorEntryPrice - stepValue * ColorTakeProfitPoints;
				if (candle.ClosePrice <= targetPrice)
				{
					SetColorPosition(0m, candle.ClosePrice);
					return;
				}
			}

			if (ColorStopLossPoints > 0)
			{
				var stopPrice = _colorEntryPrice + stepValue * ColorStopLossPoints;
				if (candle.ClosePrice >= stopPrice)
				SetColorPosition(0m, candle.ClosePrice);
			}
		}
	}

	private static void UpdateColorHistory(List<int> history, int color, int maxSize)
	{
		history.Insert(0, color);

		while (history.Count > Math.Max(4, maxSize))
		history.RemoveAt(history.Count - 1);
	}

	private static bool TryGetColors(List<int> history, int signalBar, out int current, out int previous)
	{
		current = 0;
		previous = 0;

		var index = signalBar;
		var prevIndex = signalBar + 1;

		if (history.Count <= prevIndex)
		return false;

		current = history[index];
		previous = history[prevIndex];
		return true;
	}

	private sealed class SilverTrendCalculator
	{
		private readonly int _length;
		private readonly decimal _k;
		private readonly Queue<decimal> _highs = new();
		private readonly Queue<decimal> _lows = new();
		private int _prevTrend;

		public SilverTrendCalculator(int length, int risk)
		{
			_length = Math.Max(1, length);
			_k = (33m - risk) / 100m;
		}

		public int? Process(ICandleMessage candle)
		{
			_highs.Enqueue(candle.HighPrice);
			_lows.Enqueue(candle.LowPrice);

			while (_highs.Count > _length)
			_highs.Dequeue();

			while (_lows.Count > _length)
			_lows.Dequeue();

			if (_highs.Count < _length || _lows.Count < _length)
			return null;

			decimal ssMax = decimal.MinValue;
			foreach (var value in _highs)
			{
				if (value > ssMax)
				ssMax = value;
			}

			decimal ssMin = decimal.MaxValue;
			foreach (var value in _lows)
			{
				if (value < ssMin)
				ssMin = value;
			}

			var range = ssMax - ssMin;
			var smin = ssMin + range * _k;
			var smax = ssMax - range * _k;

			var trend = _prevTrend;
			var close = candle.ClosePrice;

			if (close < smin)
			trend = -1;
			else if (close > smax)
			trend = 1;

			var color = 2;

			if (trend > 0)
			color = candle.OpenPrice <= candle.ClosePrice ? 0 : 1;
			else if (trend < 0)
			color = candle.OpenPrice >= candle.ClosePrice ? 4 : 3;

			_prevTrend = trend;
			return color;
		}

		public void Reset()
		{
			_highs.Clear();
			_lows.Clear();
			_prevTrend = 0;
		}
	}

	private sealed class ColorJfatlCalculator
	{
		private static readonly decimal[] Coefficients =
		{
			+0.4360409450m, +0.3658689069m, +0.2460452079m, +0.1104506886m,
			-0.0054034585m, -0.0760367731m, -0.0933058722m, -0.0670110374m,
			-0.0190795053m, +0.0259609206m, +0.0502044896m, +0.0477818607m,
			+0.0249252327m, -0.0047706151m, -0.0272432537m, -0.0338917071m,
			-0.0244141482m, -0.0055774838m, +0.0128149838m, +0.0226522218m,
			+0.0208778257m, +0.0100299086m, -0.0036771622m, -0.0136744850m,
			-0.0160483392m, -0.0108597376m, -0.0016060704m, +0.0069480557m,
			+0.0110573605m, +0.0095711419m, +0.0040444064m, -0.0023824623m,
			-0.0067093714m, -0.0072003400m, -0.0047717710m, +0.0005541115m,
			+0.0007860160m, +0.0130129076m, +0.0040364019m
		};

		private readonly int _length;
		private readonly AppliedPrice _priceMode;
		private readonly int _digit;
		private readonly List<decimal> _prices = new();
		private readonly ExponentialMovingAverage _smoother;
		private decimal? _prevValue;
		private int _prevColor = 1;

		public ColorJfatlCalculator(int length, int phase, AppliedPrice priceMode, int digit)
		{
			_length = Math.Max(1, length);
			_priceMode = priceMode;
			_digit = Math.Max(0, digit);
			_smoother = new ExponentialMovingAverage { Length = _length };
		}

		public (decimal? Line, int? Color) Process(ICandleMessage candle)
		{
			var price = GetPrice(candle, _priceMode);
			_prices.Insert(0, price);

			while (_prices.Count > Coefficients.Length)
			_prices.RemoveAt(_prices.Count - 1);

			if (_prices.Count < Coefficients.Length)
			return (null, null);

			decimal fatl = 0m;
			for (var i = 0; i < Coefficients.Length; i++)
			fatl += Coefficients[i] * _prices[i];

			var smoothed = _smoother.Process(fatl, candle.OpenTime, true).ToDecimal();

			var factor = (decimal)Math.Pow(10, _digit);
			smoothed = Math.Round(smoothed * factor) / factor;

			int color;
			if (_prevValue is null)
			{
				color = _prevColor;
			}
			else
			{
				var diff = smoothed - _prevValue.Value;
				if (diff > 0m)
				color = 2;
				else if (diff < 0m)
				color = 0;
				else
				color = _prevColor;
			}

			_prevValue = smoothed;
			_prevColor = color;

			return (smoothed, color);
		}

		public void Reset()
		{
			_prices.Clear();
			_prevValue = null;
			_prevColor = 1;
		}

		private static decimal GetPrice(ICandleMessage candle, AppliedPrice mode)
		{
			var open = candle.OpenPrice;
			var close = candle.ClosePrice;
			var high = candle.HighPrice;
			var low = candle.LowPrice;

			return mode switch
			{
				AppliedPrice.PriceClose => close,
				AppliedPrice.PriceOpen => open,
				AppliedPrice.PriceHigh => high,
				AppliedPrice.PriceLow => low,
				AppliedPrice.PriceMedian => (high + low) / 2m,
				AppliedPrice.PriceTypical => (close + high + low) / 3m,
				AppliedPrice.PriceWeighted => (2m * close + high + low) / 4m,
				AppliedPrice.PriceSimple => (open + close) / 2m,
				AppliedPrice.PriceQuarter => (open + close + high + low) / 4m,
				AppliedPrice.PriceTrendFollow0 => close > open ? high : close < open ? low : close,
				AppliedPrice.PriceTrendFollow1 => close > open ? (high + close) / 2m : close < open ? (low + close) / 2m : close,
				AppliedPrice.PriceDeMark =>
				CalculateDeMarkPrice(open, high, low, close),
				_ => close
			};
		}

		private static decimal CalculateDeMarkPrice(decimal open, decimal high, decimal low, decimal close)
		{
			var res = high + low + close;

			if (close < open)
			res = (res + low) / 2m;
			else if (close > open)
			res = (res + high) / 2m;
			else
			res = (res + close) / 2m;

			return ((res - low) + (res - high)) / 2m;
		}
	}
}