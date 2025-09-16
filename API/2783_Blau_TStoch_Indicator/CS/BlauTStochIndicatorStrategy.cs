using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that ports the Blau Triple Stochastic Index expert advisor.
/// Supports zero breakdown and trend twist entry modes with optional position permissions.
/// </summary>
public class BlauTStochIndicatorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<SmoothingMethod> _smoothingMethod;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<int> _firstSmoothing;
	private readonly StrategyParam<int> _secondSmoothing;
	private readonly StrategyParam<int> _thirdSmoothing;
	private readonly StrategyParam<int> _phase;
	private readonly StrategyParam<AppliedPriceType> _priceType;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<BlauEntryMode> _mode;
	private readonly StrategyParam<bool> _allowLongEntries;
	private readonly StrategyParam<bool> _allowShortEntries;
	private readonly StrategyParam<bool> _allowLongExits;
	private readonly StrategyParam<bool> _allowShortExits;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;

	private BlauTripleStochastic? _indicator;
	private readonly List<decimal> _indicatorValues = new();

	/// <summary>
	/// Available entry algorithms.
	/// </summary>
	public enum BlauEntryMode
	{
		/// <summary>
		/// Trade zero line breakdown of the Blau Triple Stochastic Index.
		/// </summary>
		Breakdown,

		/// <summary>
		/// Trade twists of the indicator slope.
		/// </summary>
		Twist
	}

	/// <summary>
	/// Supported smoothing techniques.
	/// </summary>
	public enum SmoothingMethod
	{
		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Ema,

		/// <summary>
		/// Simple moving average.
		/// </summary>
		Sma,

		/// <summary>
		/// Smoothed (RMA) moving average.
		/// </summary>
		Smma,

		/// <summary>
		/// Weighted moving average.
		/// </summary>
		Lwma
	}

	/// <summary>
	/// Applied price options.
	/// </summary>
	public enum AppliedPriceType
	{
		/// <summary>
		/// Closing price.
		/// </summary>
		Close,

		/// <summary>
		/// Opening price.
		/// </summary>
		Open,

		/// <summary>
		/// Highest price.
		/// </summary>
		High,

		/// <summary>
		/// Lowest price.
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
		/// Simple price (open + close) / 2.
		/// </summary>
		Simple,

		/// <summary>
		/// Quarted price (open + close + high + low) / 4.
		/// </summary>
		Quarted,

		/// <summary>
		/// Trend-following price variant #0.
		/// </summary>
		TrendFollow0,

		/// <summary>
		/// Trend-following price variant #1.
		/// </summary>
		TrendFollow1,

		/// <summary>
		/// DeMark price.
		/// </summary>
		Demark
	}

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Smoothing algorithm applied to stochastic and range series.
	/// </summary>
	public SmoothingMethod Smoothing
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	/// <summary>
	/// Momentum lookback for the Blau Triple Stochastic Index.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// Length of the first smoothing stage.
	/// </summary>
	public int FirstSmoothing
	{
		get => _firstSmoothing.Value;
		set => _firstSmoothing.Value = value;
	}

	/// <summary>
	/// Length of the second smoothing stage.
	/// </summary>
	public int SecondSmoothing
	{
		get => _secondSmoothing.Value;
		set => _secondSmoothing.Value = value;
	}

	/// <summary>
	/// Length of the third smoothing stage.
	/// </summary>
	public int ThirdSmoothing
	{
		get => _thirdSmoothing.Value;
		set => _thirdSmoothing.Value = value;
	}

	/// <summary>
	/// Phase parameter retained for compatibility.
	/// </summary>
	public int Phase
	{
		get => _phase.Value;
		set => _phase.Value = value;
	}

	/// <summary>
	/// Applied price selection.
	/// </summary>
	public AppliedPriceType PriceType
	{
		get => _priceType.Value;
		set => _priceType.Value = value;
	}

	/// <summary>
	/// Bar shift used to evaluate the indicator.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Entry algorithm.
	/// </summary>
	public BlauEntryMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Allow long entries.
	/// </summary>
	public bool AllowLongEntries
	{
		get => _allowLongEntries.Value;
		set => _allowLongEntries.Value = value;
	}

	/// <summary>
	/// Allow short entries.
	/// </summary>
	public bool AllowShortEntries
	{
		get => _allowShortEntries.Value;
		set => _allowShortEntries.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool AllowLongExits
	{
		get => _allowLongExits.Value;
		set => _allowLongExits.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool AllowShortExits
	{
		get => _allowShortExits.Value;
		set => _allowShortExits.Value = value;
	}

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public BlauTStochIndicatorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for indicator calculation", "General");

		_smoothingMethod = Param(nameof(Smoothing), SmoothingMethod.Ema)
		.SetDisplay("Smoothing", "Moving average type for smoothing", "Indicator");

		_momentumLength = Param(nameof(MomentumLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Length", "Lookback for highest and lowest prices", "Indicator");

		_firstSmoothing = Param(nameof(FirstSmoothing), 5)
		.SetGreaterThanZero()
		.SetDisplay("First Smoothing", "Length of the first smoothing stage", "Indicator");

		_secondSmoothing = Param(nameof(SecondSmoothing), 8)
		.SetGreaterThanZero()
		.SetDisplay("Second Smoothing", "Length of the second smoothing stage", "Indicator");

		_thirdSmoothing = Param(nameof(ThirdSmoothing), 3)
		.SetGreaterThanZero()
		.SetDisplay("Third Smoothing", "Length of the third smoothing stage", "Indicator");

		_phase = Param(nameof(Phase), 15)
		.SetDisplay("Phase", "Compatibility phase parameter", "Indicator");

		_priceType = Param(nameof(PriceType), AppliedPriceType.Close)
		.SetDisplay("Applied Price", "Price source for momentum calculation", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetGreaterThanZero()
		.SetDisplay("Signal Bar", "Closed bar index used for signals", "Signals");

		_mode = Param(nameof(Mode), BlauEntryMode.Twist)
		.SetDisplay("Mode", "Entry algorithm", "Signals");

		_allowLongEntries = Param(nameof(AllowLongEntries), true)
		.SetDisplay("Allow Long Entries", "Enable opening long positions", "Permissions");

		_allowShortEntries = Param(nameof(AllowShortEntries), true)
		.SetDisplay("Allow Short Entries", "Enable opening short positions", "Permissions");

		_allowLongExits = Param(nameof(AllowLongExits), true)
		.SetDisplay("Allow Long Exits", "Enable closing long positions", "Permissions");

		_allowShortExits = Param(nameof(AllowShortExits), true)
		.SetDisplay("Allow Short Exits", "Enable closing short positions", "Permissions");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetNotNegative()
		.SetDisplay("Take Profit Points", "Take profit distance in price steps", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetNotNegative()
		.SetDisplay("Stop Loss Points", "Stop loss distance in price steps", "Risk");
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

		_indicatorValues.Clear();
		_indicator = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_indicator = new BlauTripleStochastic
		{
			Method = Smoothing,
			Period = MomentumLength,
			Smooth1 = FirstSmoothing,
			Smooth2 = SecondSmoothing,
			Smooth3 = ThirdSmoothing,
			PriceType = PriceType
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_indicator, ProcessIndicator).Start();

		StartProtection(
		takeProfit: TakeProfitPoints > 0 ? new Unit(TakeProfitPoints, UnitTypes.Step) : null,
		stopLoss: StopLossPoints > 0 ? new Unit(StopLossPoints, UnitTypes.Step) : null);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _indicator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessIndicator(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_indicator is null || !_indicator.IsFormed)
		{
			var rawValue = indicatorValue.ToDecimal();
			_indicatorValues.Add(rawValue);
			TrimHistory();
			return;
		}

		var currentValue = indicatorValue.ToDecimal();
		_indicatorValues.Add(currentValue);
		TrimHistory();

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var maxOffset = Mode == BlauEntryMode.Twist ? 2 : 1;
		if (_indicatorValues.Count < SignalBar + maxOffset)
		return;

		var value0 = GetShiftValue(0);
		var value1 = GetShiftValue(1);
		var value2 = maxOffset >= 2 ? GetShiftValue(2) : 0m;

		var openLong = false;
		var openShort = false;
		var closeLong = false;
		var closeShort = false;

		switch (Mode)
		{
			case BlauEntryMode.Breakdown:
			{
				if (value1 > 0m)
				{
					if (AllowLongEntries && value0 <= 0m)
					openLong = true;

					if (AllowShortExits)
					closeShort = true;
				}

				if (value1 < 0m)
				{
					if (AllowShortEntries && value0 >= 0m)
					openShort = true;

					if (AllowLongExits)
					closeLong = true;
				}
				break;
			}

			case BlauEntryMode.Twist:
			{
				if (value1 < value2)
				{
					if (AllowLongEntries && value0 >= value1)
					openLong = true;

					if (AllowShortExits)
					closeShort = true;
				}

				if (value1 > value2)
				{
					if (AllowShortEntries && value0 <= value1)
					openShort = true;

					if (AllowLongExits)
					closeLong = true;
				}
				break;
			}
		}

		if (closeLong && Position > 0m)
		SellMarket(Position);

		if (closeShort && Position < 0m)
		BuyMarket(-Position);

		if (openLong && Position <= 0m)
		{
			var volume = Volume + (Position < 0m ? -Position : 0m);
			if (volume > 0m)
			BuyMarket(volume);
		}

		if (openShort && Position >= 0m)
		{
			var volume = Volume + (Position > 0m ? Position : 0m);
			if (volume > 0m)
			SellMarket(volume);
		}
	}

	private void TrimHistory()
	{
		var maxLength = Math.Max(SignalBar + 5, 10);
		while (_indicatorValues.Count > maxLength)
		_indicatorValues.RemoveAt(0);
	}

	private decimal GetShiftValue(int offset)
	{
		var index = _indicatorValues.Count - SignalBar - offset;
		if (index < 0 || index >= _indicatorValues.Count)
		throw new InvalidOperationException("Insufficient indicator history for the requested shift.");

		return _indicatorValues[index];
	}

	private class BlauTripleStochastic : Indicator<ICandleMessage>
	{
		public SmoothingMethod Method { get; set; } = SmoothingMethod.Ema;
		public int Period { get; set; } = 20;
		public int Smooth1 { get; set; } = 5;
		public int Smooth2 { get; set; } = 8;
		public int Smooth3 { get; set; } = 3;
		public AppliedPriceType PriceType { get; set; } = AppliedPriceType.Close;

		private Highest? _highest;
		private Lowest? _lowest;
		private IIndicator? _stoch1;
		private IIndicator? _stoch2;
		private IIndicator? _stoch3;
		private IIndicator? _range1;
		private IIndicator? _range2;
		private IIndicator? _range3;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			EnsureInitialized();

			var candle = input.GetValue<ICandleMessage>();
			var price = GetPrice(candle);

			var highestValue = _highest!.Process(candle.HighPrice).ToDecimal();
			var lowestValue = _lowest!.Process(candle.LowPrice).ToDecimal();

			if (!_highest.IsFormed || !_lowest.IsFormed)
			{
				IsFormed = false;
				return new DecimalIndicatorValue(this, 0m, input.Time);
			}

			var stoch = price - lowestValue;
			var range = highestValue - lowestValue;

			var stage1Stoch = ProcessStage(_stoch1!, stoch);
			var stage1Range = ProcessStage(_range1!, range);

			if (!_stoch1!.IsFormed || !_range1!.IsFormed)
			{
				IsFormed = false;
				return new DecimalIndicatorValue(this, 0m, input.Time);
			}

			var stage2Stoch = ProcessStage(_stoch2!, stage1Stoch);
			var stage2Range = ProcessStage(_range2!, stage1Range);

			if (!_stoch2!.IsFormed || !_range2!.IsFormed)
			{
				IsFormed = false;
				return new DecimalIndicatorValue(this, 0m, input.Time);
			}

			var stage3Stoch = ProcessStage(_stoch3!, stage2Stoch);
			var stage3Range = ProcessStage(_range3!, stage2Range);

			if (!_stoch3!.IsFormed || !_range3!.IsFormed || stage3Range == 0m)
			{
				IsFormed = false;
				return new DecimalIndicatorValue(this, 0m, input.Time);
			}

			IsFormed = true;
			var value = 100m * stage3Stoch / stage3Range - 50m;
			return new DecimalIndicatorValue(this, value, input.Time);
		}

		public override void Reset()
		{
			base.Reset();

			_highest = null;
			_lowest = null;
			_stoch1 = _stoch2 = _stoch3 = null;
			_range1 = _range2 = _range3 = null;
			IsFormed = false;
		}

		private void EnsureInitialized()
		{
			if (_highest != null)
			return;

			_highest = new Highest { Length = Math.Max(1, Period) };
			_lowest = new Lowest { Length = Math.Max(1, Period) };

			_stoch1 = CreateSmoother(Smooth1);
			_stoch2 = CreateSmoother(Smooth2);
			_stoch3 = CreateSmoother(Smooth3);
			_range1 = CreateSmoother(Smooth1);
			_range2 = CreateSmoother(Smooth2);
			_range3 = CreateSmoother(Smooth3);
		}

		private decimal ProcessStage(IIndicator indicator, decimal value)
		{
			return indicator.Process(value).ToDecimal();
		}

		private IIndicator CreateSmoother(int length)
		{
			var len = Math.Max(1, length);

			return Method switch
			{
				SmoothingMethod.Sma => new SimpleMovingAverage { Length = len },
				SmoothingMethod.Smma => new SmoothedMovingAverage { Length = len },
				SmoothingMethod.Lwma => new WeightedMovingAverage { Length = len },
				_ => new ExponentialMovingAverage { Length = len },
			};
		}

		private decimal GetPrice(ICandleMessage candle)
		{
			var open = candle.OpenPrice;
			var high = candle.HighPrice;
			var low = candle.LowPrice;
			var close = candle.ClosePrice;

			return PriceType switch
			{
				AppliedPriceType.Open => open,
				AppliedPriceType.High => high,
				AppliedPriceType.Low => low,
				AppliedPriceType.Median => (high + low) / 2m,
				AppliedPriceType.Typical => (close + high + low) / 3m,
				AppliedPriceType.Weighted => (2m * close + high + low) / 4m,
				AppliedPriceType.Simple => (open + close) / 2m,
				AppliedPriceType.Quarted => (open + close + high + low) / 4m,
				AppliedPriceType.TrendFollow0 => close > open ? high : close < open ? low : close,
				AppliedPriceType.TrendFollow1 => close > open ? (high + close) / 2m : close < open ? (low + close) / 2m : close,
				AppliedPriceType.Demark => CalculateDemark(open, high, low, close),
				_ => close,
			};
		}

		private static decimal CalculateDemark(decimal open, decimal high, decimal low, decimal close)
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
