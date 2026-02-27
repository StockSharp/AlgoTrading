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
/// Strategy based on William Blau's triple smoothed stochastic oscillator.
/// </summary>
public class BlauTsStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<BlauSignalModes> _mode;
	private readonly StrategyParam<AppliedPriceTypes> _appliedPrice;
	private readonly StrategyParam<BlauSmoothingTypes> _smoothing;
	private readonly StrategyParam<int> _baseLength;
	private readonly StrategyParam<int> _smoothLength1;
	private readonly StrategyParam<int> _smoothLength2;
	private readonly StrategyParam<int> _smoothLength3;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<bool> _enableLongEntry;
	private readonly StrategyParam<bool> _enableShortEntry;
	private readonly StrategyParam<bool> _enableLongExit;
	private readonly StrategyParam<bool> _enableShortExit;

	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private IIndicator _stochSmooth1 = null!;
	private IIndicator _stochSmooth2 = null!;
	private IIndicator _stochSmooth3 = null!;
	private IIndicator _rangeSmooth1 = null!;
	private IIndicator _rangeSmooth2 = null!;
	private IIndicator _rangeSmooth3 = null!;
	private IIndicator _signalSmooth = null!;
	private readonly List<decimal> _histHistory = new();
	private readonly List<decimal> _signalHistory = new();

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Entry and exit signal mode.
	/// </summary>
	public BlauSignalModes Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Applied price used for the stochastic calculation.
	/// </summary>
	public AppliedPriceTypes AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Smoothing algorithm used for stochastic and signal averaging.
	/// </summary>
	public BlauSmoothingTypes Smoothing
	{
		get => _smoothing.Value;
		set => _smoothing.Value = value;
	}

	/// <summary>
	/// Lookback for the highest and lowest price range.
	/// </summary>
	public int BaseLength
	{
		get => _baseLength.Value;
		set => _baseLength.Value = value;
	}

	/// <summary>
	/// First smoothing length for the stochastic numerator and denominator.
	/// </summary>
	public int SmoothLength1
	{
		get => _smoothLength1.Value;
		set => _smoothLength1.Value = value;
	}

	/// <summary>
	/// Second smoothing length for the stochastic numerator and denominator.
	/// </summary>
	public int SmoothLength2
	{
		get => _smoothLength2.Value;
		set => _smoothLength2.Value = value;
	}

	/// <summary>
	/// Third smoothing length for the stochastic numerator and denominator.
	/// </summary>
	public int SmoothLength3
	{
		get => _smoothLength3.Value;
		set => _smoothLength3.Value = value;
	}

	/// <summary>
	/// Smoothing length for the signal line.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Bar shift used to evaluate trading signals.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Stop-loss size expressed in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit size expressed in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enable opening long positions.
	/// </summary>
	public bool EnableLongEntry
	{
		get => _enableLongEntry.Value;
		set => _enableLongEntry.Value = value;
	}

	/// <summary>
	/// Enable opening short positions.
	/// </summary>
	public bool EnableShortEntry
	{
		get => _enableShortEntry.Value;
		set => _enableShortEntry.Value = value;
	}

	/// <summary>
	/// Enable closing long positions on indicator signals.
	/// </summary>
	public bool EnableLongExit
	{
		get => _enableLongExit.Value;
		set => _enableLongExit.Value = value;
	}

	/// <summary>
	/// Enable closing short positions on indicator signals.
	/// </summary>
	public bool EnableShortExit
	{
		get => _enableShortExit.Value;
		set => _enableShortExit.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public BlauTsStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Time frame for signal calculations", "General");

		_mode = Param(nameof(Mode), BlauSignalModes.Twist)
		.SetDisplay("Signal Mode", "Signal detection algorithm", "Signals");

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPriceTypes.Close)
		.SetDisplay("Applied Price", "Price source for the oscillator", "Indicator");

		_smoothing = Param(nameof(Smoothing), BlauSmoothingTypes.Exponential)
		.SetDisplay("Smoothing Type", "Moving average used for smoothing", "Indicator");

		_baseLength = Param(nameof(BaseLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("Range Length", "Number of bars for high/low range", "Indicator")
		
		.SetOptimize(3, 20, 1);

		_smoothLength1 = Param(nameof(SmoothLength1), 10)
		.SetGreaterThanZero()
		.SetDisplay("Smoothing #1", "First smoothing length", "Indicator")
		
		.SetOptimize(5, 40, 5);

		_smoothLength2 = Param(nameof(SmoothLength2), 5)
		.SetGreaterThanZero()
		.SetDisplay("Smoothing #2", "Second smoothing length", "Indicator")
		
		.SetOptimize(2, 20, 1);

		_smoothLength3 = Param(nameof(SmoothLength3), 3)
		.SetGreaterThanZero()
		.SetDisplay("Smoothing #3", "Third smoothing length", "Indicator")
		
		.SetOptimize(2, 15, 1);

		_signalLength = Param(nameof(SignalLength), 3)
		.SetGreaterThanZero()
		.SetDisplay("Signal Length", "Length of the signal line", "Indicator")
		
		.SetOptimize(2, 15, 1);

		_signalBar = Param(nameof(SignalBar), 1)
		.SetNotNegative()
		.SetDisplay("Signal Bar", "Shift used for signal evaluation", "Signals")
		;

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetNotNegative()
		.SetDisplay("Stop Loss (points)", "Stop size in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetNotNegative()
		.SetDisplay("Take Profit (points)", "Target size in price steps", "Risk");

		_enableLongEntry = Param(nameof(EnableLongEntry), true)
		.SetDisplay("Enable Long Entries", "Allow opening long trades", "Trading");

		_enableShortEntry = Param(nameof(EnableShortEntry), true)
		.SetDisplay("Enable Short Entries", "Allow opening short trades", "Trading");

		_enableLongExit = Param(nameof(EnableLongExit), true)
		.SetDisplay("Close Long Positions", "Allow indicator-based long exits", "Trading");

		_enableShortExit = Param(nameof(EnableShortExit), true)
		.SetDisplay("Close Short Positions", "Allow indicator-based short exits", "Trading");
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
		_histHistory.Clear();
		_signalHistory.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highest = new Highest { Length = BaseLength };
		_lowest = new Lowest { Length = BaseLength };
		_stochSmooth1 = CreateMovingAverage(Smoothing, SmoothLength1);
		_stochSmooth2 = CreateMovingAverage(Smoothing, SmoothLength2);
		_stochSmooth3 = CreateMovingAverage(Smoothing, SmoothLength3);
		_rangeSmooth1 = CreateMovingAverage(Smoothing, SmoothLength1);
		_rangeSmooth2 = CreateMovingAverage(Smoothing, SmoothLength2);
		_rangeSmooth3 = CreateMovingAverage(Smoothing, SmoothLength3);
		_signalSmooth = CreateMovingAverage(Smoothing, SignalLength);

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private decimal _entryPrice;

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var highResult = _highest.Process(candle);
		var lowResult = _lowest.Process(candle);

		if (highResult.IsEmpty || lowResult.IsEmpty || !_highest.IsFormed || !_lowest.IsFormed)
		return;

		// Manage SL/TP
		if (Position != 0)
		{
			var step = Security?.PriceStep ?? 1m;
			if (Position > 0)
			{
				if (StopLossPoints > 0 && candle.LowPrice <= _entryPrice - StopLossPoints * step)
				{ SellMarket(Position); return; }
				if (TakeProfitPoints > 0 && candle.HighPrice >= _entryPrice + TakeProfitPoints * step)
				{ SellMarket(Position); return; }
			}
			else
			{
				var vol = Math.Abs(Position);
				if (StopLossPoints > 0 && candle.HighPrice >= _entryPrice + StopLossPoints * step)
				{ BuyMarket(vol); return; }
				if (TakeProfitPoints > 0 && candle.LowPrice <= _entryPrice - TakeProfitPoints * step)
				{ BuyMarket(vol); return; }
			}
		}

		var t = candle.OpenTime;
		var high = highResult.ToDecimal();
		var low = lowResult.ToDecimal();
		var price = GetAppliedPrice(candle, AppliedPrice);
		var stochRaw = price - low;
		var rangeRaw = high - low;

		var stoch1 = _stochSmooth1.Process(new DecimalIndicatorValue(_stochSmooth1, stochRaw, t) { IsFinal = true });
		if (stoch1.IsEmpty)
		return;
		var stoch2 = _stochSmooth2.Process(new DecimalIndicatorValue(_stochSmooth2, stoch1.ToDecimal(), t) { IsFinal = true });
		if (stoch2.IsEmpty)
		return;
		var stoch3 = _stochSmooth3.Process(new DecimalIndicatorValue(_stochSmooth3, stoch2.ToDecimal(), t) { IsFinal = true });
		if (stoch3.IsEmpty)
		return;

		var range1 = _rangeSmooth1.Process(new DecimalIndicatorValue(_rangeSmooth1, rangeRaw, t) { IsFinal = true });
		if (range1.IsEmpty)
		return;
		var range2 = _rangeSmooth2.Process(new DecimalIndicatorValue(_rangeSmooth2, range1.ToDecimal(), t) { IsFinal = true });
		if (range2.IsEmpty)
		return;
		var range3 = _rangeSmooth3.Process(new DecimalIndicatorValue(_rangeSmooth3, range2.ToDecimal(), t) { IsFinal = true });
		if (range3.IsEmpty)
		return;

		var denom = range3.ToDecimal();
		if (denom == 0m)
		return;

		var hist = 200m * stoch3.ToDecimal() / denom - 100m;
		var signalValue = _signalSmooth.Process(new DecimalIndicatorValue(_signalSmooth, hist, t) { IsFinal = true });
		if (signalValue.IsEmpty)
		return;
		var signal = signalValue.ToDecimal();

		UpdateHistory(_histHistory, hist);
		UpdateHistory(_signalHistory, signal);

		var required = Mode == BlauSignalModes.Twist ? SignalBar + 3 : SignalBar + 2;
		if (_histHistory.Count < required)
		return;
		if (Mode == BlauSignalModes.CloudTwist && _signalHistory.Count < SignalBar + 2)
		return;

		var histCurrent = _histHistory[SignalBar];
		var histPrev = _histHistory[SignalBar + 1];
		var histPrev2 = Mode == BlauSignalModes.Twist ? _histHistory[SignalBar + 2] : 0m;

		var openLong = false;
		var openShort = false;
		var closeLong = false;
		var closeShort = false;

		switch (Mode)
		{
		case BlauSignalModes.Breakdown:
			{
				if (histPrev > 0m)
				{
					if (EnableLongEntry && histCurrent <= 0m)
					openLong = true;
					if (EnableShortExit)
					closeShort = true;
				}

				if (histPrev < 0m)
				{
					if (EnableShortEntry && histCurrent >= 0m)
					openShort = true;
					if (EnableLongExit)
					closeLong = true;
				}
				break;
			}
		case BlauSignalModes.Twist:
			{
				if (_histHistory.Count < SignalBar + 3)
				return;

				if (histPrev < histPrev2)
				{
					if (EnableLongEntry && histCurrent > histPrev)
					openLong = true;
					if (EnableShortExit)
					closeShort = true;
				}

				if (histPrev > histPrev2)
				{
					if (EnableShortEntry && histCurrent < histPrev)
					openShort = true;
					if (EnableLongExit)
					closeLong = true;
				}
				break;
			}
		case BlauSignalModes.CloudTwist:
			{
				if (_signalHistory.Count < SignalBar + 2)
				return;

				var upPrev = histPrev;
				var upCurrent = histCurrent;
				var sigPrev = _signalHistory[SignalBar + 1];
				var sigCurrent = _signalHistory[SignalBar];

				if (upPrev > sigPrev)
				{
					if (EnableLongEntry && upCurrent <= sigCurrent)
					openLong = true;
					if (EnableShortExit)
					closeShort = true;
				}

				if (upPrev < sigPrev)
				{
					if (EnableShortEntry && upCurrent >= sigCurrent)
					openShort = true;
					if (EnableLongExit)
					closeLong = true;
				}
				break;
			}
		}

		if (closeLong && Position > 0)
		SellMarket(Position);

		if (closeShort && Position < 0)
		BuyMarket(-Position);

		var volume = Volume + Math.Abs(Position);

		if (openLong && Position <= 0)
		{
			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
		}
		else if (openShort && Position >= 0)
		{
			SellMarket(volume);
			_entryPrice = candle.ClosePrice;
		}
	}

	private void UpdateHistory(List<decimal> buffer, decimal value)
	{
		buffer.Insert(0, value);
		var capacity = Math.Max(SignalBar + 3, 4);
		if (buffer.Count > capacity)
		buffer.RemoveAt(buffer.Count - 1);
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceTypes type)
	{
		return type switch
		{
			AppliedPriceTypes.Close => candle.ClosePrice,
			AppliedPriceTypes.Open => candle.OpenPrice,
			AppliedPriceTypes.High => candle.HighPrice,
			AppliedPriceTypes.Low => candle.LowPrice,
			AppliedPriceTypes.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceTypes.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
			AppliedPriceTypes.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPriceTypes.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPriceTypes.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPriceTypes.TrendFollow0 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice : candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
			AppliedPriceTypes.TrendFollow1 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m : candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
			AppliedPriceTypes.Demark =>
			GetDemarkPrice(candle),
			_ => candle.ClosePrice,
		};
	}

	private static decimal GetDemarkPrice(ICandleMessage candle)
	{
		var res = candle.HighPrice + candle.LowPrice + candle.ClosePrice;

		if (candle.ClosePrice < candle.OpenPrice)
		res = (res + candle.LowPrice) / 2m;
		else if (candle.ClosePrice > candle.OpenPrice)
		res = (res + candle.HighPrice) / 2m;
		else
		res = (res + candle.ClosePrice) / 2m;

		return ((res - candle.LowPrice) + (res - candle.HighPrice)) / 2m;
	}

	private static IIndicator CreateMovingAverage(BlauSmoothingTypes type, int length)
	{
		return type switch
		{
			BlauSmoothingTypes.Simple => new SimpleMovingAverage { Length = length },
			BlauSmoothingTypes.Exponential => new ExponentialMovingAverage { Length = length },
			BlauSmoothingTypes.Smoothed => new SmoothedMovingAverage { Length = length },
			BlauSmoothingTypes.Weighted => new WeightedMovingAverage { Length = length },
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
		};
	}

	/// <summary>
	/// Signal modes replicated from the original MQL expert advisor.
	/// </summary>
	public enum BlauSignalModes
	{
		/// <summary>Histogram crosses the zero line.</summary>
		Breakdown,
		/// <summary>Histogram direction change.</summary>
		Twist,
		/// <summary>Signal cloud color change (histogram vs. signal line crossover).</summary>
		CloudTwist,
	}

	/// <summary>
	/// Price sources supported by the strategy.
	/// </summary>
	public enum AppliedPriceTypes
	{
		/// <summary>Close price.</summary>
		Close = 1,
		/// <summary>Open price.</summary>
		Open,
		/// <summary>High price.</summary>
		High,
		/// <summary>Low price.</summary>
		Low,
		/// <summary>Median price (high+low)/2.</summary>
		Median,
		/// <summary>Typical price (high+low+close)/3.</summary>
		Typical,
		/// <summary>Weighted close price (2*close+high+low)/4.</summary>
		Weighted,
		/// <summary>Simple price (open+close)/2.</summary>
		Simple,
		/// <summary>Quarter price (open+close+high+low)/4.</summary>
		Quarter,
		/// <summary>Trend-following price variant #1.</summary>
		TrendFollow0,
		/// <summary>Trend-following price variant #2.</summary>
		TrendFollow1,
		/// <summary>Tom DeMark price calculation.</summary>
		Demark,
	}

	/// <summary>
	/// Moving average families supported by the smoothed stochastic.
	/// </summary>
	public enum BlauSmoothingTypes
	{
		/// <summary>Simple moving average.</summary>
		Simple,
		/// <summary>Exponential moving average.</summary>
		Exponential,
		/// <summary>Smoothed moving average (RMA/SMMA).</summary>
		Smoothed,
		/// <summary>Weighted moving average.</summary>
		Weighted,
	}
}