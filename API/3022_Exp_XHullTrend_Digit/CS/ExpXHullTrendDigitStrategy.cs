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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy driven by the XHullTrend Digit indicator converted from MQL5.
/// </summary>
public class ExpXHullTrendDigitStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<bool> _enableBuyEntry;
	private readonly StrategyParam<bool> _enableSellEntry;
	private readonly StrategyParam<bool> _enableBuyExit;
	private readonly StrategyParam<bool> _enableSellExit;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _baseLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<CandlePrice> _priceSource;
	private readonly StrategyParam<SmoothMethods> _smoothMethod;
	private readonly StrategyParam<int> _phase;
	private readonly StrategyParam<int> _roundDigits;
	private readonly StrategyParam<int> _signalBar;

	private XHullTrendDigitIndicator _indicator = null!;
	private readonly List<(decimal fast, decimal slow)> _history = new();

	/// <summary>
	/// Order volume used for market entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Enables long entries.
	/// </summary>
	public bool EnableBuyEntry
	{
		get => _enableBuyEntry.Value;
		set => _enableBuyEntry.Value = value;
	}

	/// <summary>
	/// Enables short entries.
	/// </summary>
	public bool EnableSellEntry
	{
		get => _enableSellEntry.Value;
		set => _enableSellEntry.Value = value;
	}

	/// <summary>
	/// Enables long position exits.
	/// </summary>
	public bool EnableBuyExit
	{
		get => _enableBuyExit.Value;
		set => _enableBuyExit.Value = value;
	}

	/// <summary>
	/// Enables short position exits.
	/// </summary>
	public bool EnableSellExit
	{
		get => _enableSellExit.Value;
		set => _enableSellExit.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base smoothing length for the indicator.
	/// </summary>
	public int BaseLength
	{
		get => _baseLength.Value;
		set => _baseLength.Value = value;
	}

	/// <summary>
	/// Hull smoothing length applied on the intermediate series.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Price source taken from each candle.
	/// </summary>
	public CandlePrice PriceSource
	{
		get => _priceSource.Value;
		set => _priceSource.Value = value;
	}

	/// <summary>
	/// Smoothing method used by the internal moving averages.
	/// </summary>
	public SmoothMethods SmoothMethods
	{
		get => _smoothMethod.Value;
		set => _smoothMethod.Value = value;
	}

	/// <summary>
	/// Phase parameter kept for compatibility with the original script.
	/// </summary>
	public int Phase
	{
		get => _phase.Value;
		set => _phase.Value = value;
	}

	/// <summary>
	/// Number of rounding digits applied to the indicator output.
	/// </summary>
	public int RoundingDigits
	{
		get => _roundDigits.Value;
		set => _roundDigits.Value = value;
	}

	/// <summary>
	/// Bar offset used for signal evaluation.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpXHullTrendDigitStrategy"/> class.
	/// </summary>
	public ExpXHullTrendDigitStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume used for entries", "Trading");

		_stopLoss = Param(nameof(StopLoss), 1000)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss distance in price steps", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 2000)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take profit distance in price steps", "Risk");

		_enableBuyEntry = Param(nameof(EnableBuyEntry), true)
			.SetDisplay("Enable Long Entry", "Allow opening long positions", "Signals");

		_enableSellEntry = Param(nameof(EnableSellEntry), true)
			.SetDisplay("Enable Short Entry", "Allow opening short positions", "Signals");

		_enableBuyExit = Param(nameof(EnableBuyExit), true)
			.SetDisplay("Enable Long Exit", "Allow closing long positions", "Signals");

		_enableSellExit = Param(nameof(EnableSellExit), true)
			.SetDisplay("Enable Short Exit", "Allow closing short positions", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Indicator Timeframe", "Timeframe used for signals", "General");

		_baseLength = Param(nameof(BaseLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Base Length", "Base smoothing length", "Indicator");

		_signalLength = Param(nameof(SignalLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Hull Length", "Length of the Hull smoothing", "Indicator");

		_priceSource = Param(nameof(PriceSource), CandlePrice.Close)
			.SetDisplay("Price Source", "Candle price used in calculations", "Indicator");

		_smoothMethod = Param(nameof(SmoothMethods), SmoothMethods.Weighted)
			.SetDisplay("Smoothing Method", "Moving average used inside the indicator", "Indicator");

		_phase = Param(nameof(Phase), 15)
			.SetDisplay("Phase", "Compatibility phase parameter", "Indicator");

		_roundDigits = Param(nameof(RoundingDigits), 2)
			.SetNotNegative()
			.SetDisplay("Rounding Digits", "Digits used to round indicator outputs", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Signal Bar", "Shift used for crossover detection", "Signals");
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
		_history.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_indicator = new XHullTrendDigitIndicator
		{
			BaseLength = BaseLength,
			SignalLength = SignalLength,
			PriceType = PriceSource,
			Method = SmoothMethods,
			Phase = Phase,
			RoundingDigits = RoundingDigits,
			PriceStep = Security?.PriceStep ?? 0.0001m
		};

		Volume = OrderVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_indicator, ProcessCandle)
			.Start();

		if (StopLoss > 0 || TakeProfit > 0)
		{
			StartProtection(
				stopLoss: StopLoss > 0 ? new Unit(StopLoss, UnitTypes.Step) : null,
				takeProfit: TakeProfit > 0 ? new Unit(TakeProfit, UnitTypes.Step) : null);
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _indicator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (indicatorValue is not XHullTrendDigitValue value || !value.IsFormed)
			return;

		// Store rounded indicator values for crossover evaluation.
		_history.Add((value.Fast, value.Slow));

		var required = Math.Max(2, SignalBar + 2);
		if (_history.Count > required)
			_history.RemoveAt(0);

		if (_history.Count < required)
			return;

		var index = _history.Count - 1 - SignalBar;
		if (index <= 0)
			return;

		var current = _history[index];
		var previous = _history[index - 1];

		var trendUp = previous.fast > previous.slow;
		var trendDown = previous.fast < previous.slow;

		var buyOpenSignal = EnableBuyEntry && trendUp && current.fast <= current.slow;
		var sellOpenSignal = EnableSellEntry && trendDown && current.fast >= current.slow;
		var buyCloseSignal = EnableBuyExit && trendDown;
		var sellCloseSignal = EnableSellExit && trendUp;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Close existing positions when the opposite trend appears.
		if (buyCloseSignal && Position > 0)
			SellMarket(Position);

		if (sellCloseSignal && Position < 0)
			BuyMarket(Math.Abs(Position));

		// Open or flip positions according to crossover signals.
		if (buyOpenSignal)
		{
			if (Position < 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (Position == 0)
				BuyMarket();
		}

		if (sellOpenSignal)
		{
			if (Position > 0)
				SellMarket(Position + Volume);
			else if (Position == 0)
				SellMarket();
		}
	}

	public enum SmoothMethods
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Simple,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Exponential,

		/// <summary>
		/// Smoothed moving average (RMA).
		/// </summary>
		Smoothed,

		/// <summary>
		/// Weighted moving average.
		/// </summary>
		Weighted
	}

	/// <summary>
	/// Indicator reproducing the logic of the XHullTrend Digit MQL5 indicator.
	/// </summary>
	public class XHullTrendDigitIndicator : BaseIndicator<decimal>
	{
		private IIndicator _shortMa;
		private IIndicator _longMa;
		private IIndicator _hullSmoother;
		private IIndicator _signalSmoother;

		public int BaseLength { get; set; } = 20;
		public int SignalLength { get; set; } = 5;
		public CandlePrice PriceType { get; set; } = CandlePrice.Close;
		public SmoothMethods Method { get; set; } = SmoothMethods.Weighted;
		public int Phase { get; set; }
		public int RoundingDigits { get; set; } = 2;
		public decimal PriceStep { get; set; } = 0.0001m;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
				return new XHullTrendDigitValue(this, input, 0m, 0m, false);

			EnsureIndicators();

			var price = SelectPrice(candle);
			var time = input.Time;

			var shortValue = _shortMa!.Process(new DecimalIndicatorValue(_shortMa, price, time)).ToDecimal();
			var longValue = _longMa!.Process(new DecimalIndicatorValue(_longMa, price, time)).ToDecimal();

			if (!_shortMa.IsFormed || !_longMa.IsFormed)
			{
				IsFormed = false;
				return new XHullTrendDigitValue(this, input, shortValue, longValue, false);
			}

			var hullRaw = 2m * shortValue - longValue;
			var hullValue = _hullSmoother!.Process(new DecimalIndicatorValue(_hullSmoother, hullRaw, time)).ToDecimal();
			var signalValue = _signalSmoother!.Process(new DecimalIndicatorValue(_signalSmoother, hullValue, time)).ToDecimal();

			IsFormed = _signalSmoother.IsFormed;

			var fast = RoundValue(hullValue);
			var slow = RoundValue(signalValue);

			if (fast == slow && hullValue != signalValue)
			{
				var step = GetRoundedStep();
				if (step > 0m)
				{
					if (hullValue > signalValue)
						fast += step;
					else
						slow += step;
				}
			}

			return new XHullTrendDigitValue(this, input, fast, slow, IsFormed);
		}

		public override void Reset()
		{
			base.Reset();
			_shortMa?.Reset();
			_longMa?.Reset();
			_hullSmoother?.Reset();
			_signalSmoother?.Reset();
		}

		private void EnsureIndicators()
		{
			if (_shortMa != null && _longMa != null && _hullSmoother != null && _signalSmoother != null)
				return;

			var halfLength = Math.Max(1, BaseLength / 2);
			var sqrtLength = Math.Max(1, (int)Math.Max(1, Math.Sqrt(BaseLength)));

			_shortMa = CreateSmoother(halfLength);
			_longMa = CreateSmoother(Math.Max(1, BaseLength));
			_hullSmoother = CreateSmoother(Math.Max(1, SignalLength));
			_signalSmoother = CreateSmoother(sqrtLength);
		}

		private IIndicator CreateSmoother(int length)
		{
			return Method switch
			{
				SmoothMethods.Simple => new SimpleMovingAverage { Length = length },
				SmoothMethods.Exponential => new ExponentialMovingAverage { Length = length },
				SmoothMethods.Smoothed => new SmoothedMovingAverage { Length = length },
				_ => new WeightedMovingAverage { Length = length },
			};
		}

		private decimal SelectPrice(ICandleMessage candle)
		{
			return PriceType switch
			{
				CandlePrice.Open => candle.OpenPrice,
				CandlePrice.High => candle.HighPrice,
				CandlePrice.Low => candle.LowPrice,
				CandlePrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
				CandlePrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
				CandlePrice.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
				CandlePrice.Average => (candle.OpenPrice + candle.ClosePrice) / 2m,
				_ => candle.ClosePrice,
			};
		}

		private decimal RoundValue(decimal value)
		{
			var step = GetRoundedStep();
			if (step <= 0m)
				return value;

			return Math.Round(value / step, MidpointRounding.AwayFromZero) * step;
		}

		private decimal GetRoundedStep()
		{
			var baseStep = PriceStep > 0m ? PriceStep : 0.0001m;
			var multiplier = (decimal)Math.Pow(10, RoundingDigits);
			return baseStep * (multiplier <= 0m ? 1m : multiplier);
		}
	}

	/// <summary>
	/// Indicator value carrying both fast and slow rounded lines.
	/// </summary>
	public class XHullTrendDigitValue : ComplexIndicatorValue
	{
		public XHullTrendDigitValue(IIndicator indicator, IIndicatorValue input, decimal fast, decimal slow, bool isFormed)
			: base(indicator, input, (nameof(Fast), fast), (nameof(Slow), slow))
		{
			IsFormed = isFormed;
		}

		/// <summary>
		/// Rounded fast line of the indicator.
		/// </summary>
		public decimal Fast => (decimal)GetValue(nameof(Fast));

		/// <summary>
		/// Rounded slow line of the indicator.
		/// </summary>
		public decimal Slow => (decimal)GetValue(nameof(Slow));

		/// <summary>
		/// Shows whether all internal smoothers are formed.
		/// </summary>
		public bool IsFormed { get; }
	}
}
