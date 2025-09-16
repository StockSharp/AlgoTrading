using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// KWAN RDP trend strategy converted from MetaTrader version.
/// Combines DeMarker, Money Flow Index and Momentum indicators smoothed by a configurable moving average.
/// Opens long positions when the smoothed oscillator turns up and shorts when it turns down.
/// </summary>
public class KwanRdpStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _deMarkerPeriod;
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<SmoothingMethod> _smoothingMethod;
	private readonly StrategyParam<bool> _buyEntriesEnabled;
	private readonly StrategyParam<bool> _sellEntriesEnabled;
	private readonly StrategyParam<bool> _closeLongsOnShortSignal;
	private readonly StrategyParam<bool> _closeShortsOnLongSignal;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private MoneyFlowIndex? _mfi;
	private Momentum? _momentum;
	private LengthIndicator<decimal>? _smoother;
	private SMA? _deMaxAverage;
	private SMA? _deMinAverage;
	private decimal? _previousHigh;
	private decimal? _previousLow;
	private decimal? _previousSmoothed;
	private TrendDirection? _previousTrend;

	/// <summary>
	/// The candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// DeMarker indicator period.
	/// </summary>
	public int DeMarkerPeriod
	{
		get => _deMarkerPeriod.Value;
		set => _deMarkerPeriod.Value = value;
	}

	/// <summary>
	/// Money Flow Index length.
	/// </summary>
	public int MfiPeriod
	{
		get => _mfiPeriod.Value;
		set => _mfiPeriod.Value = value;
	}

	/// <summary>
	/// Momentum indicator length.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing length applied to the KWAN RDP oscillator.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	/// <summary>
	/// Smoothing method applied to the KWAN RDP oscillator.
	/// </summary>
	public SmoothingMethod Smoothing
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	/// <summary>
	/// Enables long entries when a bullish signal appears.
	/// </summary>
	public bool EnableLongEntries
	{
		get => _buyEntriesEnabled.Value;
		set => _buyEntriesEnabled.Value = value;
	}

	/// <summary>
	/// Enables short entries when a bearish signal appears.
	/// </summary>
	public bool EnableShortEntries
	{
		get => _sellEntriesEnabled.Value;
		set => _sellEntriesEnabled.Value = value;
	}

	/// <summary>
	/// Closes existing long positions on a bearish signal.
	/// </summary>
	public bool CloseLongsOnReverse
	{
		get => _closeLongsOnShortSignal.Value;
		set => _closeLongsOnShortSignal.Value = value;
	}

	/// <summary>
	/// Closes existing short positions on a bullish signal.
	/// </summary>
	public bool CloseShortsOnReverse
	{
		get => _closeShortsOnLongSignal.Value;
		set => _closeShortsOnLongSignal.Value = value;
	}

	/// <summary>
	/// Take profit percentage applied through position protection. Set to zero to disable.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop loss percentage applied through position protection. Set to zero to disable.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="KwanRdpStrategy"/>.
	/// </summary>
	public KwanRdpStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series", "General");

		_deMarkerPeriod = Param(nameof(DeMarkerPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("DeMarker Period", "DeMarker indicator length", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(7, 28, 1);

		_mfiPeriod = Param(nameof(MfiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("MFI Period", "Money Flow Index length", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(7, 28, 1);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Period", "Momentum indicator length", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(7, 28, 1);

		_smoothingLength = Param(nameof(SmoothingLength), 7)
		.SetGreaterThanZero()
		.SetDisplay("Smoothing Length", "Length of the smoothing average", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);

		_smoothingMethod = Param(nameof(Smoothing), SmoothingMethod.Jurik)
		.SetDisplay("Smoothing Method", "Moving average used for KWAN RDP", "Indicators");

		_buyEntriesEnabled = Param(nameof(EnableLongEntries), true)
		.SetDisplay("Enable Long Entries", "Allow opening of long positions", "Trading");

		_sellEntriesEnabled = Param(nameof(EnableShortEntries), true)
		.SetDisplay("Enable Short Entries", "Allow opening of short positions", "Trading");

		_closeLongsOnShortSignal = Param(nameof(CloseLongsOnReverse), true)
		.SetDisplay("Close Longs", "Close long positions on bearish signal", "Trading");

		_closeShortsOnLongSignal = Param(nameof(CloseShortsOnReverse), true)
		.SetDisplay("Close Shorts", "Close short positions on bullish signal", "Trading");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0m)
		.SetDisplay("Take Profit %", "Percent take profit for protection", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 0m)
		.SetDisplay("Stop Loss %", "Percent stop loss for protection", "Risk");
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

		_previousHigh = default;
		_previousLow = default;
		_previousSmoothed = default;
		_previousTrend = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_mfi = new MoneyFlowIndex { Length = MfiPeriod };
		_momentum = new Momentum { Length = MomentumPeriod };
		_smoother = CreateSmoother(Smoothing, SmoothingLength);
		_deMaxAverage = new SMA { Length = DeMarkerPeriod };
		_deMinAverage = new SMA { Length = DeMarkerPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_mfi, _momentum, ProcessCandle)
		.Start();

		if (TakeProfitPercent > 0m || StopLossPercent > 0m)
		{
			StartProtection(
			takeProfit: TakeProfitPercent > 0m ? new Unit(TakeProfitPercent, UnitTypes.Percent) : null,
			stopLoss: StopLossPercent > 0m ? new Unit(StopLossPercent, UnitTypes.Percent) : null);
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _mfi);
			DrawIndicator(area, _momentum);
			DrawIndicator(area, _smoother);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal mfiValue, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading() || _smoother == null || _mfi == null || _momentum == null || _deMaxAverage == null || _deMinAverage == null)
		return;

		if (!_mfi.IsFormed || !_momentum.IsFormed)
		return;

		var deMarker = CalculateDeMarker(candle);
		if (deMarker is not decimal deValue)
		return;

		var kwan = momentumValue == 0m
		? 100m
		: 100m * deValue * mfiValue / momentumValue;

		var smoothValue = _smoother.Process(new DecimalIndicatorValue(_smoother, kwan, candle.OpenTime));
		if (smoothValue is not DecimalIndicatorValue { IsFinal: true, Value: var smoothed })
		return;

		var previousValue = _previousSmoothed;
		_previousSmoothed = smoothed;

		if (previousValue is null)
		{
			_previousTrend = TrendDirection.Neutral;
			return;
		}

		var currentTrend = DetermineTrend(previousValue.Value, smoothed);
		GenerateSignals(currentTrend);
	}

	private decimal? CalculateDeMarker(ICandleMessage candle)
	{
		if (_deMaxAverage == null || _deMinAverage == null)
		return null;

		if (_previousHigh is null || _previousLow is null)
		{
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			return null;
		}

		var upMove = Math.Max(0m, candle.HighPrice - _previousHigh.Value);
		var downMove = Math.Max(0m, _previousLow.Value - candle.LowPrice);

		var maxValue = _deMaxAverage.Process(new DecimalIndicatorValue(_deMaxAverage, upMove, candle.OpenTime));
		var minValue = _deMinAverage.Process(new DecimalIndicatorValue(_deMinAverage, downMove, candle.OpenTime));

		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;

		if (maxValue is not DecimalIndicatorValue { IsFinal: true, Value: var maxAvg })
		return null;

		if (minValue is not DecimalIndicatorValue { IsFinal: true, Value: var minAvg })
		return null;

		var denominator = maxAvg + minAvg;
		return denominator == 0m ? 0m : maxAvg / denominator;
	}

	private void GenerateSignals(TrendDirection currentTrend)
	{
		var previousTrend = _previousTrend ?? TrendDirection.Neutral;

		if (currentTrend == TrendDirection.Rising)
		{
			if (CloseShortsOnReverse && Position < 0)
			ClosePosition();

			if (EnableLongEntries && previousTrend != TrendDirection.Rising && Position <= 0)
			BuyMarket();
		}
		else if (currentTrend == TrendDirection.Falling)
		{
			if (CloseLongsOnReverse && Position > 0)
			ClosePosition();

			if (EnableShortEntries && previousTrend != TrendDirection.Falling && Position >= 0)
			SellMarket();
		}

		_previousTrend = currentTrend;
	}

	private static LengthIndicator<decimal> CreateSmoother(SmoothingMethod method, int length)
	{
		return method switch
		{
			SmoothingMethod.Simple => new SMA { Length = length },
			SmoothingMethod.Exponential => new EMA { Length = length },
			SmoothingMethod.Smoothed => new SMMA { Length = length },
			SmoothingMethod.Weighted => new WMA { Length = length },
			_ => new JurikMovingAverage { Length = length },
		};
	}

	private static TrendDirection DetermineTrend(decimal previous, decimal current)
	{
		if (current > previous)
		return TrendDirection.Rising;

		if (current < previous)
		return TrendDirection.Falling;

		return TrendDirection.Neutral;
	}

	/// <summary>
	/// Supported smoothing methods for KWAN RDP.
	/// </summary>
	public enum SmoothingMethod
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted,
		Jurik,
	}

	private enum TrendDirection
	{
		Neutral,
		Rising,
		Falling,
	}
}
