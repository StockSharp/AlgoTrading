using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ichimoku oscillator strategy converted from the MQL Exp_ICHI_OSC expert.
/// Generates entries based on color transitions of the smoothed oscillator derived from Ichimoku lines.
/// </summary>
public class IchiOscillatorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _ichimokuBasePeriod;
	private readonly StrategyParam<SmoothingMethod> _smoothingMethod;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<int> _smoothingPhase;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _buyEntriesEnabled;
	private readonly StrategyParam<bool> _sellEntriesEnabled;
	private readonly StrategyParam<bool> _buyExitsEnabled;
	private readonly StrategyParam<bool> _sellExitsEnabled;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _orderVolume;

	private Ichimoku _ichimoku = null!;
	private LengthIndicator<decimal> _smoother = null!;
	private readonly List<int> _colorHistory = new();
	private decimal? _previousSmoothed;
	private TimeSpan _timeShift;

	/// <summary>
	/// Initializes a new instance of the <see cref="IchiOscillatorStrategy"/> class.
	/// </summary>
	public IchiOscillatorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for Ichimoku calculations", "General");

		_ichimokuBasePeriod = Param(nameof(IchimokuBasePeriod), 22)
			.SetGreaterThanZero()
			.SetDisplay("Ichimoku Base", "Base value to derive Tenkan, Kijun and Senkou spans", "Ichimoku")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 2);

		_smoothingMethod = Param(nameof(Smoothing), SmoothingMethod.Jurik)
			.SetDisplay("Smoothing Method", "Moving average applied to the oscillator", "Oscillator");

		_smoothingLength = Param(nameof(SmoothingLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Length", "Length for oscillator smoothing", "Oscillator")
			.SetCanOptimize(true)
			.SetOptimize(3, 25, 1);

		_smoothingPhase = Param(nameof(SmoothingPhase), 15)
			.SetDisplay("Smoothing Phase", "Additional phase parameter for selected smoothing", "Oscillator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("Signal Bar", "Bar shift used for signal confirmation", "Logic");

		_buyEntriesEnabled = Param(nameof(BuyEntriesEnabled), true)
			.SetDisplay("Enable Buy Entries", "Allow opening long positions", "Logic");

		_sellEntriesEnabled = Param(nameof(SellEntriesEnabled), true)
			.SetDisplay("Enable Sell Entries", "Allow opening short positions", "Logic");

		_buyExitsEnabled = Param(nameof(BuyExitsEnabled), true)
			.SetDisplay("Enable Buy Exits", "Allow closing long positions", "Logic");

		_sellExitsEnabled = Param(nameof(SellExitsEnabled), true)
			.SetDisplay("Enable Sell Exits", "Allow closing short positions", "Logic");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (points)", "Protective stop distance in price steps", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(200, 2000, 200);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (points)", "Protective take-profit distance in price steps", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(200, 4000, 200);

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Base order volume for market orders", "General");
	}

	/// <summary>
	/// Candle data type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base Ichimoku period that controls Tenkan, Kijun and Senkou lengths.
	/// </summary>
	public int IchimokuBasePeriod
	{
		get => _ichimokuBasePeriod.Value;
		set => _ichimokuBasePeriod.Value = value;
	}

	/// <summary>
	/// Smoothing method applied to the oscillator.
	/// </summary>
	public SmoothingMethod Smoothing
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	/// <summary>
	/// Oscillator smoothing length.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	/// <summary>
	/// Phase parameter for smoothing algorithms that support it.
	/// </summary>
	public int SmoothingPhase
	{
		get => _smoothingPhase.Value;
		set => _smoothingPhase.Value = value;
	}

	/// <summary>
	/// Bar offset used to confirm oscillator color transitions.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Enable opening of long positions.
	/// </summary>
	public bool BuyEntriesEnabled
	{
		get => _buyEntriesEnabled.Value;
		set => _buyEntriesEnabled.Value = value;
	}

	/// <summary>
	/// Enable opening of short positions.
	/// </summary>
	public bool SellEntriesEnabled
	{
		get => _sellEntriesEnabled.Value;
		set => _sellEntriesEnabled.Value = value;
	}

	/// <summary>
	/// Enable closing of existing long positions.
	/// </summary>
	public bool BuyExitsEnabled
	{
		get => _buyExitsEnabled.Value;
		set => _buyExitsEnabled.Value = value;
	}

	/// <summary>
	/// Enable closing of existing short positions.
	/// </summary>
	public bool SellExitsEnabled
	{
		get => _sellExitsEnabled.Value;
		set => _sellExitsEnabled.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Base volume used for market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set
		{
			_orderVolume.Value = value;
			Volume = value;
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_colorHistory.Clear();
		_previousSmoothed = null;
		_ichimoku?.Reset();
		_smoother?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		var tenkanLength = Math.Max(1, (int)(IchimokuBasePeriod * 0.5m));
		var kijunLength = Math.Max(1, (int)(IchimokuBasePeriod * 1.5m));
		var senkouBLength = Math.Max(1, (int)(IchimokuBasePeriod * 3m));

		_ichimoku = new Ichimoku
		{
			Tenkan = { Length = tenkanLength },
			Kijun = { Length = kijunLength },
			SenkouB = { Length = senkouBLength }
		};

		_smoother = CreateSmoother(Smoothing, SmoothingLength, SmoothingPhase);

		_timeShift = CandleType.Arg is TimeSpan span && span > TimeSpan.Zero ? span : TimeSpan.Zero;

		_colorHistory.Clear();
		_previousSmoothed = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_ichimoku, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _smoother);
			DrawOwnTrades(area);
		}

		StartProtection(
			stopLoss: StopLossPoints > 0 ? new Unit(StopLossPoints, UnitTypes.Step) : null,
			takeProfit: TakeProfitPoints > 0 ? new Unit(TakeProfitPoints, UnitTypes.Step) : null);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ichimokuTyped = (IchimokuValue)ichimokuValue;

		if (ichimokuTyped.Tenkan is not decimal tenkan ||
			ichimokuTyped.Kijun is not decimal kijun ||
			ichimokuTyped.SenkouA is not decimal senkouA)
		{
			return;
		}

		var step = Security?.Step ?? 1m;
		if (step == 0m)
			step = 1m;

		var markt = candle.ClosePrice - senkouA;
		var trend = tenkan - kijun;
		var rawOscillator = (markt - trend) / step;

		var smoothValue = _smoother.Process(new DecimalIndicatorValue(_smoother, rawOscillator, candle.OpenTime));
		if (!smoothValue.IsFinal || smoothValue is not DecimalIndicatorValue smoothResult)
			return;

		var smoothed = smoothResult.Value;
		UpdateColorHistory(smoothed);

		if (_colorHistory.Count <= SignalBar + 1)
			return;

		var currentIndex = _colorHistory.Count - 1 - SignalBar;
		var previousIndex = currentIndex - 1;
		if (previousIndex < 0)
			return;

		var currentColor = _colorHistory[currentIndex];
		var previousColor = _colorHistory[previousIndex];

		var buyOpen = false;
		var sellOpen = false;
		var buyClose = false;
		var sellClose = false;

		if (previousColor == 0 || previousColor == 3)
		{
			sellClose = SellExitsEnabled;

			if (BuyEntriesEnabled && (currentColor == 2 || currentColor == 1 || currentColor == 4))
				buyOpen = true;
		}

		if (previousColor == 4 || previousColor == 1)
		{
			buyClose = BuyExitsEnabled;

			if (SellEntriesEnabled && (currentColor == 0 || currentColor == 1 || currentColor == 3))
				sellOpen = true;
		}

		var signalTime = candle.CloseTime + _timeShift;

		if (buyClose && Position > 0)
		{
			SellMarket(Position);
			LogInfo($"[{signalTime}] Closing long at {candle.ClosePrice} due to oscillator color change {previousColor}->{currentColor}.");
		}

		if (sellClose && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			LogInfo($"[{signalTime}] Closing short at {candle.ClosePrice} due to oscillator color change {previousColor}->{currentColor}.");
		}

		if (buyOpen && Position <= 0)
		{
			var volume = Volume + Math.Max(0m, -Position);
			BuyMarket(volume);
			LogInfo($"[{signalTime}] Opening long at {candle.ClosePrice} with oscillator {smoothed:F5}.");
		}

		if (sellOpen && Position >= 0)
		{
			var volume = Volume + Math.Max(0m, Position);
			SellMarket(volume);
			LogInfo($"[{signalTime}] Opening short at {candle.ClosePrice} with oscillator {smoothed:F5}.");
		}
	}

	private void UpdateColorHistory(decimal smoothed)
	{
		var color = 2;

		if (_previousSmoothed.HasValue)
		{
			var prev = _previousSmoothed.Value;

			if (smoothed > 0m)
			{
				if (prev < smoothed)
					color = 0;
				else if (prev > smoothed)
					color = 1;
			}
			else if (smoothed < 0m)
			{
				if (prev < smoothed)
					color = 4;
				else if (prev > smoothed)
					color = 3;
			}
		}
		else
		{
			if (smoothed > 0m)
				color = 0;
			else if (smoothed < 0m)
				color = 3;
		}

		_colorHistory.Add(color);
		_previousSmoothed = smoothed;
	}

	private LengthIndicator<decimal> CreateSmoother(SmoothingMethod method, int length, int phase)
	{
		return method switch
		{
			SmoothingMethod.Simple => new SimpleMovingAverage { Length = length },
			SmoothingMethod.Exponential => new ExponentialMovingAverage { Length = length },
			SmoothingMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			SmoothingMethod.Weighted => new WeightedMovingAverage { Length = length },
			SmoothingMethod.Jurik => new JurikMovingAverage { Length = length },
			SmoothingMethod.Kaufman => new KaufmanAdaptiveMovingAverage { Length = length },
			_ => new JurikMovingAverage { Length = length }
		};
	}

	/// <summary>
	/// Supported smoothing algorithms for the oscillator.
	/// </summary>
	public enum SmoothingMethod
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
		/// Smoothed moving average.
		/// </summary>
		Smoothed,

		/// <summary>
		/// Weighted moving average.
		/// </summary>
		Weighted,

		/// <summary>
		/// Jurik moving average.
		/// </summary>
		Jurik,

		/// <summary>
		/// Kaufman adaptive moving average.
		/// </summary>
		Kaufman
	}
}
