using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Duplex strategy based on two Color JFATL Digit indicators with independent parameters for long and short trades.
/// The long module opens trades when the indicator turns bullish (color 2) and exits when it turns bearish (color 0).
/// The short module mirrors the logic, entering on bearish turns and exiting on bullish turns.
/// Optional stop loss and take profit offsets in price steps are available for each side individually.
/// </summary>
public class ColorJfatlDigitDuplexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _longCandleType;
	private readonly StrategyParam<DataType> _shortCandleType;
	private readonly StrategyParam<int> _longJmaLength;
	private readonly StrategyParam<int> _longJmaPhase;
	private readonly StrategyParam<AppliedPrice> _longAppliedPrice;
	private readonly StrategyParam<int> _longDigit;
	private readonly StrategyParam<int> _longSignalBar;
	private readonly StrategyParam<int> _longStopLossPoints;
	private readonly StrategyParam<int> _longTakeProfitPoints;
	private readonly StrategyParam<bool> _enableLongOpen;
	private readonly StrategyParam<bool> _enableLongClose;

	private readonly StrategyParam<int> _shortJmaLength;
	private readonly StrategyParam<int> _shortJmaPhase;
	private readonly StrategyParam<AppliedPrice> _shortAppliedPrice;
	private readonly StrategyParam<int> _shortDigit;
	private readonly StrategyParam<int> _shortSignalBar;
	private readonly StrategyParam<int> _shortStopLossPoints;
	private readonly StrategyParam<int> _shortTakeProfitPoints;
	private readonly StrategyParam<bool> _enableShortOpen;
	private readonly StrategyParam<bool> _enableShortClose;

	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	public ColorJfatlDigitDuplexStrategy()
	{
		_longCandleType = Param(nameof(LongCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Long Candle Type", "Timeframe for the long indicator", "General");
		_shortCandleType = Param(nameof(ShortCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Short Candle Type", "Timeframe for the short indicator", "General");

		_longJmaLength = Param(nameof(LongJmaLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("Long JMA Length", "Period of the Jurik moving average for longs", "Indicator");
		_longJmaPhase = Param(nameof(LongJmaPhase), -100)
		.SetDisplay("Long JMA Phase", "Phase adjustment for the Jurik moving average", "Indicator");
		_longAppliedPrice = Param(nameof(LongAppliedPrice), AppliedPrice.Close)
		.SetDisplay("Long Applied Price", "Price source for the long indicator", "Indicator");
		_longDigit = Param(nameof(LongDigit), 2)
		.SetDisplay("Long Rounding Digits", "Number of digits used to round the indicator", "Indicator");
		_longSignalBar = Param(nameof(LongSignalBar), 1)
		.SetDisplay("Long Signal Bar", "Bar shift used to evaluate long signals", "Indicator");
		_longStopLossPoints = Param(nameof(LongStopLossPoints), 1000)
		.SetDisplay("Long Stop Loss (pts)", "Stop loss distance in price steps for long trades", "Risk");
		_longTakeProfitPoints = Param(nameof(LongTakeProfitPoints), 2000)
		.SetDisplay("Long Take Profit (pts)", "Take profit distance in price steps for long trades", "Risk");
		_enableLongOpen = Param(nameof(EnableLongOpen), true)
		.SetDisplay("Enable Long Entries", "Allow opening new long positions", "Trading");
		_enableLongClose = Param(nameof(EnableLongClose), true)
		.SetDisplay("Enable Long Exits", "Allow closing long positions on signals", "Trading");

		_shortJmaLength = Param(nameof(ShortJmaLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("Short JMA Length", "Period of the Jurik moving average for shorts", "Indicator");
		_shortJmaPhase = Param(nameof(ShortJmaPhase), -100)
		.SetDisplay("Short JMA Phase", "Phase adjustment for the Jurik moving average", "Indicator");
		_shortAppliedPrice = Param(nameof(ShortAppliedPrice), AppliedPrice.Close)
		.SetDisplay("Short Applied Price", "Price source for the short indicator", "Indicator");
		_shortDigit = Param(nameof(ShortDigit), 2)
		.SetDisplay("Short Rounding Digits", "Number of digits used to round the indicator", "Indicator");
		_shortSignalBar = Param(nameof(ShortSignalBar), 1)
		.SetDisplay("Short Signal Bar", "Bar shift used to evaluate short signals", "Indicator");
		_shortStopLossPoints = Param(nameof(ShortStopLossPoints), 1000)
		.SetDisplay("Short Stop Loss (pts)", "Stop loss distance in price steps for short trades", "Risk");
		_shortTakeProfitPoints = Param(nameof(ShortTakeProfitPoints), 2000)
		.SetDisplay("Short Take Profit (pts)", "Take profit distance in price steps for short trades", "Risk");
		_enableShortOpen = Param(nameof(EnableShortOpen), true)
		.SetDisplay("Enable Short Entries", "Allow opening new short positions", "Trading");
		_enableShortClose = Param(nameof(EnableShortClose), true)
		.SetDisplay("Enable Short Exits", "Allow closing short positions on signals", "Trading");
	}

	/// <summary>
	/// Timeframe used for the long-side indicator.
	/// </summary>
	public DataType LongCandleType
	{
		get => _longCandleType.Value;
		set => _longCandleType.Value = value;
	}

	/// <summary>
	/// Timeframe used for the short-side indicator.
	/// </summary>
	public DataType ShortCandleType
	{
		get => _shortCandleType.Value;
		set => _shortCandleType.Value = value;
	}

	/// <summary>
	/// Jurik moving average length for the long indicator.
	/// </summary>
	public int LongJmaLength
	{
		get => _longJmaLength.Value;
		set => _longJmaLength.Value = value;
	}

	/// <summary>
	/// Jurik moving average phase for the long indicator.
	/// </summary>
	public int LongJmaPhase
	{
		get => _longJmaPhase.Value;
		set => _longJmaPhase.Value = value;
	}

	/// <summary>
	/// Applied price for the long indicator.
	/// </summary>
	public AppliedPrice LongAppliedPrice
	{
		get => _longAppliedPrice.Value;
		set => _longAppliedPrice.Value = value;
	}

	/// <summary>
	/// Number of digits used to round the long indicator output.
	/// </summary>
	public int LongDigit
	{
		get => _longDigit.Value;
		set => _longDigit.Value = value;
	}

	/// <summary>
	/// Bar shift used when reading long signals.
	/// </summary>
	public int LongSignalBar
	{
		get => _longSignalBar.Value;
		set => _longSignalBar.Value = value;
	}

	/// <summary>
	/// Stop loss distance for long trades measured in price steps.
	/// </summary>
	public int LongStopLossPoints
	{
		get => _longStopLossPoints.Value;
		set => _longStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance for long trades measured in price steps.
	/// </summary>
	public int LongTakeProfitPoints
	{
		get => _longTakeProfitPoints.Value;
		set => _longTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enable or disable new long entries.
	/// </summary>
	public bool EnableLongOpen
	{
		get => _enableLongOpen.Value;
		set => _enableLongOpen.Value = value;
	}

	/// <summary>
	/// Enable or disable long exits generated by the indicator.
	/// </summary>
	public bool EnableLongClose
	{
		get => _enableLongClose.Value;
		set => _enableLongClose.Value = value;
	}

	/// <summary>
	/// Jurik moving average length for the short indicator.
	/// </summary>
	public int ShortJmaLength
	{
		get => _shortJmaLength.Value;
		set => _shortJmaLength.Value = value;
	}

	/// <summary>
	/// Jurik moving average phase for the short indicator.
	/// </summary>
	public int ShortJmaPhase
	{
		get => _shortJmaPhase.Value;
		set => _shortJmaPhase.Value = value;
	}

	/// <summary>
	/// Applied price for the short indicator.
	/// </summary>
	public AppliedPrice ShortAppliedPrice
	{
		get => _shortAppliedPrice.Value;
		set => _shortAppliedPrice.Value = value;
	}

	/// <summary>
	/// Number of digits used to round the short indicator output.
	/// </summary>
	public int ShortDigit
	{
		get => _shortDigit.Value;
		set => _shortDigit.Value = value;
	}

	/// <summary>
	/// Bar shift used when reading short signals.
	/// </summary>
	public int ShortSignalBar
	{
		get => _shortSignalBar.Value;
		set => _shortSignalBar.Value = value;
	}

	/// <summary>
	/// Stop loss distance for short trades measured in price steps.
	/// </summary>
	public int ShortStopLossPoints
	{
		get => _shortStopLossPoints.Value;
		set => _shortStopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance for short trades measured in price steps.
	/// </summary>
	public int ShortTakeProfitPoints
	{
		get => _shortTakeProfitPoints.Value;
		set => _shortTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enable or disable new short entries.
	/// </summary>
	public bool EnableShortOpen
	{
		get => _enableShortOpen.Value;
		set => _enableShortOpen.Value = value;
	}

	/// <summary>
	/// Enable or disable short exits generated by the indicator.
	/// </summary>
	public bool EnableShortClose
	{
		get => _enableShortClose.Value;
		set => _enableShortClose.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var longIndicator = new ColorJfatlDigitIndicator
		{
			Length = LongJmaLength,
			Phase = LongJmaPhase,
			AppliedPrice = LongAppliedPrice,
			Digit = LongDigit,
			SignalBar = LongSignalBar
		};

		var shortIndicator = new ColorJfatlDigitIndicator
		{
			Length = ShortJmaLength,
			Phase = ShortJmaPhase,
			AppliedPrice = ShortAppliedPrice,
			Digit = ShortDigit,
			SignalBar = ShortSignalBar
		};

		var longSubscription = SubscribeCandles(LongCandleType);
		longSubscription
		.BindEx(longIndicator, ProcessLongSignal)
		.Start();

		var shortSubscription = SubscribeCandles(ShortCandleType);
		shortSubscription
		.BindEx(shortIndicator, ProcessShortSignal)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, longSubscription);
			DrawIndicator(area, longIndicator);
			DrawIndicator(area, shortIndicator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessLongSignal(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (indicatorValue is not ColorJfatlDigitValue value || !value.IsReady)
		return;

		if (CheckLongRisk(candle))
		return;

		var currentColor = value.CurrentColor!.Value;
		var previousColor = value.PreviousColor!.Value;

		if (EnableLongClose && currentColor == 0 && Position > 0)
		{
			ClosePosition();
			ClearLongRisk();
			return;
		}

		if (EnableLongOpen && currentColor == 2 && previousColor < 2 && Position <= 0)
		{
			OpenLong(candle.ClosePrice);
		}
	}

	private void ProcessShortSignal(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (indicatorValue is not ColorJfatlDigitValue value || !value.IsReady)
		return;

		if (CheckShortRisk(candle))
		return;

		var currentColor = value.CurrentColor!.Value;
		var previousColor = value.PreviousColor!.Value;

		if (EnableShortClose && currentColor == 2 && Position < 0)
		{
			ClosePosition();
			ClearShortRisk();
			return;
		}

		if (EnableShortOpen && currentColor == 0 && previousColor > 0 && Position >= 0)
		{
			OpenShort(candle.ClosePrice);
		}
	}

	private void OpenLong(decimal entryPrice)
	{
		var volume = Volume;
		if (Position < 0)
		volume += Math.Abs(Position);

		if (volume <= 0)
		return;

		BuyMarket(volume);
		SetupLongRisk(entryPrice);
		ClearShortRisk();
	}

	private void OpenShort(decimal entryPrice)
	{
		var volume = Volume;
		if (Position > 0)
		volume += Math.Abs(Position);

		if (volume <= 0)
		return;

		SellMarket(volume);
		SetupShortRisk(entryPrice);
		ClearLongRisk();
	}

	private void SetupLongRisk(decimal entryPrice)
	{
		var step = Security?.PriceStep ?? 1m;
		_longStopPrice = LongStopLossPoints > 0 ? entryPrice - LongStopLossPoints * step : null;
		_longTakePrice = LongTakeProfitPoints > 0 ? entryPrice + LongTakeProfitPoints * step : null;
	}

	private void SetupShortRisk(decimal entryPrice)
	{
		var step = Security?.PriceStep ?? 1m;
		_shortStopPrice = ShortStopLossPoints > 0 ? entryPrice + ShortStopLossPoints * step : null;
		_shortTakePrice = ShortTakeProfitPoints > 0 ? entryPrice - ShortTakeProfitPoints * step : null;
	}

	private bool CheckLongRisk(ICandleMessage candle)
	{
		if (Position <= 0)
		{
			ClearLongRisk();
			return false;
		}

		if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
		{
			ClosePosition();
			ClearLongRisk();
			return true;
		}

		if (_longTakePrice is decimal take && candle.HighPrice >= take)
		{
			ClosePosition();
			ClearLongRisk();
			return true;
		}

		return false;
	}

	private bool CheckShortRisk(ICandleMessage candle)
	{
		if (Position >= 0)
		{
			ClearShortRisk();
			return false;
		}

		if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
		{
			ClosePosition();
			ClearShortRisk();
			return true;
		}

		if (_shortTakePrice is decimal take && candle.LowPrice <= take)
		{
			ClosePosition();
			ClearShortRisk();
			return true;
		}

		return false;
	}

	private void ClearLongRisk()
	{
		_longStopPrice = null;
		_longTakePrice = null;
	}

	private void ClearShortRisk()
	{
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	/// <summary>
	/// Applied price options supported by the Color JFATL Digit indicator.
	/// </summary>
	public enum AppliedPrice
	{
		/// <summary>
		/// Close price of the candle.
		/// </summary>
		Close = 1,

		/// <summary>
		/// Open price of the candle.
		/// </summary>
		Open,

		/// <summary>
		/// High price of the candle.
		/// </summary>
		High,

		/// <summary>
		/// Low price of the candle.
		/// </summary>
		Low,

		/// <summary>
		/// Median price (high + low) / 2.
		/// </summary>
		Median,

		/// <summary>
		/// Typical price (close + high + low) / 3.
		/// </summary>
		Typical,

		/// <summary>
		/// Weighted price (2 * close + high + low) / 4.
		/// </summary>
		Weighted,

		/// <summary>
		/// Average of open and close.
		/// </summary>
		Average,

		/// <summary>
		/// Quarter price (open + close + high + low) / 4.
		/// </summary>
		Quarter,

		/// <summary>
		/// Trend-following price (high for bullish candles, low for bearish candles).
		/// </summary>
		TrendFollow0,

		/// <summary>
		/// Trend-following price using half candle body.
		/// </summary>
		TrendFollow1,

		/// <summary>
		/// Demark price formulation.
		/// </summary>
		Demark
	}

	private sealed class ColorJfatlDigitIndicator : Indicator<ICandleMessage>
	{
		private const int FatlPeriod = 39;
		private static readonly decimal[] FatlWeights =
		{
			0.4360409450m, 0.3658689069m, 0.2460452079m, 0.1104506886m,
			-0.0054034585m, -0.0760367731m, -0.0933058722m, -0.0670110374m,
			-0.0190795053m, 0.0259609206m, 0.0502044896m, 0.0477818607m,
			0.0249252327m, -0.0047706151m, -0.0272432537m, -0.0338917071m,
			-0.0244141482m, -0.0055774838m, 0.0128149838m, 0.0226522218m,
			0.0208778257m, 0.0100299086m, -0.0036771622m, -0.0136744850m,
			-0.0160483392m, -0.0108597376m, -0.0016060704m, 0.0069480557m,
			0.0110573605m, 0.0095711419m, 0.0040444064m, -0.0023824623m,
			-0.0067093714m, -0.0072003400m, -0.0047717710m, 0.0005541115m,
			0.0007860160m, 0.0130129076m, 0.0040364019m
		};

		private readonly List<decimal> _priceBuffer = new();
		private readonly List<IndicatorEntry> _history = new();
		private JurikMovingAverage? _jma;
		private decimal? _previousRaw;

		public int Length { get; set; } = 5;
		public int Phase { get; set; } = -100;
		public AppliedPrice AppliedPrice { get; set; } = AppliedPrice.Close;
		public int Digit { get; set; } = 2;
		public int SignalBar { get; set; } = 1;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();
			if (candle == null || candle.State != CandleStates.Finished)
			{
				IsFormed = false;
				return new ColorJfatlDigitValue(this, input, null, null, null, null);
			}

			var length = Math.Max(1, Length);
			if (_jma == null)
			{
				_jma = new JurikMovingAverage { Length = length };
			}
			else if (_jma.Length != length)
			{
				_jma.Length = length;
				_jma.Reset();
				_priceBuffer.Clear();
				_history.Clear();
				_previousRaw = null;
			}

			var price = GetPrice(candle);
			_priceBuffer.Add(price);
			if (_priceBuffer.Count > FatlWeights.Length)
			_priceBuffer.RemoveAt(0);

			if (_priceBuffer.Count < FatlPeriod)
			{
				IsFormed = false;
				return new ColorJfatlDigitValue(this, input, null, null, null, null);
			}

			decimal fatl = 0m;
			for (var i = 0; i < FatlWeights.Length; i++)
			{
				var priceIndex = _priceBuffer.Count - 1 - i;
				fatl += FatlWeights[i] * _priceBuffer[priceIndex];
			}

			var jmaValue = _jma.Process(new DecimalIndicatorValue(_jma, fatl, candle.CloseTime));
			var baseValue = jmaValue.ToDecimal();
			var adjusted = ApplyPhase(baseValue);
			var rounded = Round(adjusted);
			var color = CalculateColor(rounded);

			_history.Add(new IndicatorEntry(candle.CloseTime, rounded, color));

			var requiredHistory = Math.Max(5, Math.Max(0, SignalBar) + 3);
			if (_history.Count > requiredHistory)
			_history.RemoveRange(0, _history.Count - requiredHistory);

			var signalBar = Math.Max(0, SignalBar);
			if (_history.Count <= signalBar)
			{
				IsFormed = false;
				return new ColorJfatlDigitValue(this, input, null, null, null, null);
			}

			var index = _history.Count - 1 - signalBar;
			var entry = _history[index];
			var prevColor = index > 0 ? _history[index - 1].Color : (int?)null;

			if (prevColor == null)
			{
				IsFormed = false;
				return new ColorJfatlDigitValue(this, input, null, null, null, null);
			}

			IsFormed = true;
			return new ColorJfatlDigitValue(this, input, entry.Value, entry.Color, prevColor.Value, entry.Time);
		}

		private decimal GetPrice(ICandleMessage candle)
		{
			var open = candle.OpenPrice;
			var close = candle.ClosePrice;
			var high = candle.HighPrice;
			var low = candle.LowPrice;

			switch (AppliedPrice)
			{
				case AppliedPrice.Close:
				return close;
				case AppliedPrice.Open:
				return open;
				case AppliedPrice.High:
				return high;
				case AppliedPrice.Low:
				return low;
				case AppliedPrice.Median:
				return (high + low) / 2m;
				case AppliedPrice.Typical:
				return (close + high + low) / 3m;
				case AppliedPrice.Weighted:
				return (2m * close + high + low) / 4m;
				case AppliedPrice.Average:
				return (open + close) / 2m;
				case AppliedPrice.Quarter:
				return (open + close + high + low) / 4m;
				case AppliedPrice.TrendFollow0:
				return close > open ? high : close < open ? low : close;
				case AppliedPrice.TrendFollow1:
				return close > open ? (high + close) / 2m : close < open ? (low + close) / 2m : close;
				case AppliedPrice.Demark:
				var res = high + low + close;
				if (close < open)
				res = (res + low) / 2m;
				else if (close > open)
				res = (res + high) / 2m;
				else
				res = (res + close) / 2m;
				return ((res - low) + (res - high)) / 2m;
				default:
				return close;
			}
		}

		private decimal ApplyPhase(decimal baseValue)
		{
			var phase = Phase;
			if (phase > 100)
			phase = 100;
			else if (phase < -100)
			phase = -100;

			var adjusted = baseValue;
			if (_previousRaw is decimal prev)
			{
				var diff = baseValue - prev;
				adjusted = baseValue + diff * (phase / 100m);
			}

			_previousRaw = baseValue;
			return adjusted;
		}

		private decimal Round(decimal value)
		{
			if (Digit < 0)
			return value;

			return Math.Round(value, Digit, MidpointRounding.AwayFromZero);
		}

		private int CalculateColor(decimal currentValue)
		{
			if (_history.Count == 0)
			return 1;

			var previous = _history[^1];
			var diff = currentValue - previous.Value;
			if (diff > 0m)
			return 2;
			if (diff < 0m)
			return 0;
			return previous.Color;
		}

		public override void Reset()
		{
			base.Reset();
			_priceBuffer.Clear();
			_history.Clear();
			_previousRaw = null;
			_jma?.Reset();
			IsFormed = false;
		}
	}

	private sealed record IndicatorEntry(DateTimeOffset Time, decimal Value, int Color);

	private sealed class ColorJfatlDigitValue : ComplexIndicatorValue
	{
		public ColorJfatlDigitValue(IIndicator indicator, IIndicatorValue input, decimal? value, int? currentColor, int? previousColor, DateTimeOffset? signalTime)
		: base(indicator, input,
		(nameof(Value), value),
		(nameof(CurrentColor), currentColor),
		(nameof(PreviousColor), previousColor),
		(nameof(SignalTime), signalTime))
		{
		}

		public decimal? Value => (decimal?)GetValue(nameof(Value));
		public int? CurrentColor => (int?)GetValue(nameof(CurrentColor));
		public int? PreviousColor => (int?)GetValue(nameof(PreviousColor));
		public DateTimeOffset? SignalTime => (DateTimeOffset?)GetValue(nameof(SignalTime));
		public bool IsReady => Value.HasValue && CurrentColor.HasValue && PreviousColor.HasValue;
	}
}
