using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Translates the MetaTrader "4H swing" expert advisor to the StockSharp high level API.
/// </summary>
public class FourHourSwingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _signalCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _mediumEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSmoothPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;
	private readonly StrategyParam<bool> _useMacdExit;

	private ExponentialMovingAverage _fastEma = null!;
	private ExponentialMovingAverage _mediumEma = null!;
	private ExponentialMovingAverage _slowEma = null!;
	private StochasticOscillator _stochastic = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private decimal? _fastEmaValue;
	private decimal? _mediumEmaValue;
	private decimal? _slowEmaValue;
	private decimal? _stochasticMain;
	private decimal? _stochasticSignal;
	private decimal? _momentumDistance1;
	private decimal? _momentumDistance2;
	private decimal? _momentumDistance3;
	private decimal? _macdMain;
	private decimal? _macdSignal;
	private ICandleMessage? _previousBaseCandle;

	private decimal _tickSize;
	private decimal _pipSize;

	private Sides? _activeSide;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal? _highestSinceEntry;
	private decimal? _lowestSinceEntry;
	private bool _breakEvenActivated;

	/// <summary>
	/// Initializes a new instance of <see cref="FourHourSwingStrategy"/>.
	/// </summary>
	public FourHourSwingStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
			.SetDisplay("Trade Volume", "Default market order volume", "Trading")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Primary Candle", "Time frame used for the main signal", "General");

		_signalCandleType = Param(nameof(SignalCandleType), TimeSpan.FromDays(7).TimeFrame())
			.SetDisplay("Signal Candle", "Higher time frame for stochastic and momentum", "General");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("MACD Candle", "Macro time frame used for the MACD filter", "General");

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 4)
			.SetDisplay("Fast EMA", "Length of the fast EMA computed on typical price", "Indicators")
			.SetGreaterThanZero();

		_mediumEmaPeriod = Param(nameof(MediumEmaPeriod), 14)
			.SetDisplay("Medium EMA", "Length of the medium EMA computed on typical price", "Indicators")
			.SetGreaterThanZero();

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 50)
			.SetDisplay("Slow EMA", "Length of the slow EMA computed on typical price", "Indicators")
			.SetGreaterThanZero();

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 13)
			.SetDisplay("Stochastic %K", "Main period of the stochastic oscillator", "Indicators")
			.SetGreaterThanZero();

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 5)
			.SetDisplay("Stochastic %D", "Signal period of the stochastic oscillator", "Indicators")
			.SetGreaterThanZero();

		_stochasticSmoothPeriod = Param(nameof(StochasticSmoothPeriod), 5)
			.SetDisplay("Stochastic Smoothing", "Slowing factor applied to %K", "Indicators")
			.SetGreaterThanZero();

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetDisplay("Momentum Period", "Lookback period for the momentum ratio", "Indicators")
			.SetGreaterThanZero();

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
			.SetDisplay("Momentum Threshold", "Minimum distance from 100 required for momentum", "Trading Rules")
			.SetGreaterOrEqualZero();

		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk")
			.SetGreaterOrEqualZero();

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetDisplay("Take Profit (pips)", "Protective take profit distance in pips", "Risk")
			.SetGreaterOrEqualZero();

		_trailingStopPips = Param(nameof(TrailingStopPips), 40m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk")
			.SetGreaterOrEqualZero();

		_useBreakEven = Param(nameof(UseBreakEven), true)
			.SetDisplay("Use Break Even", "Move the stop to break even once profit target is reached", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 30m)
			.SetDisplay("Break Even Trigger", "Profit in pips required to lock the trade", "Risk")
			.SetGreaterOrEqualZero();

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 30m)
			.SetDisplay("Break Even Offset", "Additional pips applied when locking", "Risk")
			.SetGreaterOrEqualZero();

		_useMacdExit = Param(nameof(UseMacdExit), false)
			.SetDisplay("MACD Exit", "Close positions on an opposite MACD signal", "Trading Rules");
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DataType SignalCandleType
	{
		get => _signalCandleType.Value;
		set => _signalCandleType.Value = value;
	}

	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	public int MediumEmaPeriod
	{
		get => _mediumEmaPeriod.Value;
		set => _mediumEmaPeriod.Value = value;
	}

	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	public int StochasticSmoothPeriod
	{
		get => _stochasticSmoothPeriod.Value;
		set => _stochasticSmoothPeriod.Value = value;
	}

	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	public bool UseMacdExit
	{
		get => _useMacdExit.Value;
		set => _useMacdExit.Value = value;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_tickSize = Security?.PriceStep ?? 0m;
		if (_tickSize <= 0m)
			_tickSize = 0.0001m;

		_pipSize = _tickSize;
		if (_tickSize == 0.00001m || _tickSize == 0.001m)
			_pipSize = _tickSize * 10m;

		_fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
		_mediumEma = new ExponentialMovingAverage { Length = MediumEmaPeriod };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };
		_stochastic = new StochasticOscillator
		{
			Length = StochasticKPeriod,
			K = { Length = StochasticSmoothPeriod },
			D = { Length = StochasticDPeriod }
		};
		_momentum = new Momentum { Length = MomentumPeriod };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			ShortPeriod = 12,
			LongPeriod = 26,
			SignalPeriod = 9
		};

		var baseSubscription = SubscribeCandles(CandleType);
		baseSubscription.Bind(ProcessBaseCandle).Start();

		var signalSubscription = SubscribeCandles(SignalCandleType);
		signalSubscription.BindEx(_stochastic, ProcessSignalCandle).Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription.BindEx(_macd, ProcessMacd).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, baseSubscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _mediumEma);
			DrawIndicator(area, _slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessBaseCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var typicalPrice = GetTypicalPrice(candle);
		var fastResult = _fastEma.Process(new DecimalIndicatorValue(_fastEma, typicalPrice, candle.OpenTime));
		var mediumResult = _mediumEma.Process(new DecimalIndicatorValue(_mediumEma, typicalPrice, candle.OpenTime));
		var slowResult = _slowEma.Process(new DecimalIndicatorValue(_slowEma, typicalPrice, candle.OpenTime));

		if (!fastResult.IsFinal || fastResult is not DecimalIndicatorValue fastValue)
			return;

		if (!mediumResult.IsFinal || mediumResult is not DecimalIndicatorValue mediumValue)
			return;

		if (!slowResult.IsFinal || slowResult is not DecimalIndicatorValue slowValue)
			return;

		var previousFast = _fastEmaValue;
		var previousMedium = _mediumEmaValue;
		var previousSlow = _slowEmaValue;
		var previousCandle = _previousBaseCandle;

		_fastEmaValue = fastValue.Value;
		_mediumEmaValue = mediumValue.Value;
		_slowEmaValue = slowValue.Value;
		_previousBaseCandle = candle;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (UseMacdExit)
			ApplyMacdExit();

		if (UpdatePositionState(candle))
			return;

		if (previousFast is null || previousMedium is null || previousSlow is null || previousCandle is null)
			return;

		TryEnter(previousCandle, previousFast.Value, previousMedium.Value, previousSlow.Value);

		UpdatePositionState(candle);
	}

	private void ProcessSignalCandle(ICandleMessage candle, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var momentumValue = _momentum.Process(new DecimalIndicatorValue(_momentum, candle.ClosePrice, candle.OpenTime));
		if (momentumValue.IsFinal)
		{
			var diff = momentumValue.ToDecimal();
			var previousPrice = candle.ClosePrice - diff;
			if (previousPrice != 0m)
			{
				var ratio = candle.ClosePrice / previousPrice * 100m;
				var distance = Math.Abs(100m - ratio);
				_momentumDistance3 = _momentumDistance2;
				_momentumDistance2 = _momentumDistance1;
				_momentumDistance1 = distance;
			}
		}

		if (!stochasticValue.IsFinal)
			return;

		var stoch = (StochasticOscillatorValue)stochasticValue;
		if (stoch.K is decimal main)
			_stochasticMain = main;
		if (stoch.D is decimal signal)
			_stochasticSignal = signal;
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!value.IsFinal || value is not MovingAverageConvergenceDivergenceSignalValue macdValue)
			return;

		if (macdValue.Macd is decimal macd)
			_macdMain = macd;

		if (macdValue.Signal is decimal signal)
			_macdSignal = signal;
	}

	private void TryEnter(ICandleMessage previousCandle, decimal fast, decimal medium, decimal slow)
	{
		if (!HasMomentumData() || _stochasticMain is not decimal main || _stochasticSignal is not decimal signal || !HasMacdData())
			return;

		var volume = Volume + Math.Abs(Position);

		var longCondition = fast > medium && medium > slow && main > signal && MomentumDistanceAboveThreshold() && IsMacdBullish();
		if (longCondition && Position <= 0)
		{
			BuyMarket(volume);
			return;
		}

		var shortCondition = fast < medium && medium < slow && main < signal && MomentumDistanceAboveThreshold() && IsMacdBearish();
		if (shortCondition && Position >= 0)
			SellMarket(volume);
	}

	private bool UpdatePositionState(ICandleMessage candle)
	{
		if (Position > 0)
		{
			InitializeLongStateIfNeeded(candle);

			_highestSinceEntry = _highestSinceEntry.HasValue ? Math.Max(_highestSinceEntry.Value, candle.HighPrice) : candle.HighPrice;
			_lowestSinceEntry = _lowestSinceEntry.HasValue ? Math.Min(_lowestSinceEntry.Value, candle.LowPrice) : candle.LowPrice;

			if (UseBreakEven)
				ApplyBreakEvenForLong(candle);

			if (TrailingStopPips > 0m)
				ApplyTrailingForLong();

			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}
		}
		else if (Position < 0)
		{
			InitializeShortStateIfNeeded(candle);

			_highestSinceEntry = _highestSinceEntry.HasValue ? Math.Max(_highestSinceEntry.Value, candle.HighPrice) : candle.HighPrice;
			_lowestSinceEntry = _lowestSinceEntry.HasValue ? Math.Min(_lowestSinceEntry.Value, candle.LowPrice) : candle.LowPrice;

			if (UseBreakEven)
				ApplyBreakEvenForShort(candle);

			if (TrailingStopPips > 0m)
				ApplyTrailingForShort();

			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(-Position);
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(-Position);
				ResetPositionState();
				return true;
			}
		}
		else
		{
			ResetPositionState();
		}

		return false;
	}

	private void ApplyBreakEvenForLong(ICandleMessage candle)
	{
		if (_entryPrice is not decimal entry)
			return;

		var trigger = entry + GetPipDistance(BreakEvenTriggerPips);
		if (!_breakEvenActivated && candle.HighPrice >= trigger)
		{
			_stopPrice = entry + GetPipDistance(BreakEvenOffsetPips);
			_breakEvenActivated = true;
		}
	}

	private void ApplyBreakEvenForShort(ICandleMessage candle)
	{
		if (_entryPrice is not decimal entry)
			return;

		var trigger = entry - GetPipDistance(BreakEvenTriggerPips);
		if (!_breakEvenActivated && candle.LowPrice <= trigger)
		{
			_stopPrice = entry - GetPipDistance(BreakEvenOffsetPips);
			_breakEvenActivated = true;
		}
	}

	private void ApplyTrailingForLong()
	{
		if (_highestSinceEntry is not decimal high)
			return;

		var candidate = high - GetPipDistance(TrailingStopPips);
		if (_stopPrice is not decimal current || candidate > current)
			_stopPrice = candidate;
	}

	private void ApplyTrailingForShort()
	{
		if (_lowestSinceEntry is not decimal low)
			return;

		var candidate = low + GetPipDistance(TrailingStopPips);
		if (_stopPrice is not decimal current || candidate < current)
			_stopPrice = candidate;
	}

	private void InitializeLongStateIfNeeded(ICandleMessage candle)
	{
		if (_activeSide == Sides.Buy && _entryPrice.HasValue)
			return;

		_activeSide = Sides.Buy;
		_entryPrice = PositionAvgPrice;
		_stopPrice = StopLossPips > 0m ? _entryPrice - GetPipDistance(StopLossPips) : null;
		_takeProfitPrice = TakeProfitPips > 0m ? _entryPrice + GetPipDistance(TakeProfitPips) : null;
		_highestSinceEntry = candle.HighPrice;
		_lowestSinceEntry = candle.LowPrice;
		_breakEvenActivated = false;
	}

	private void InitializeShortStateIfNeeded(ICandleMessage candle)
	{
		if (_activeSide == Sides.Sell && _entryPrice.HasValue)
			return;

		_activeSide = Sides.Sell;
		_entryPrice = PositionAvgPrice;
		_stopPrice = StopLossPips > 0m ? _entryPrice + GetPipDistance(StopLossPips) : null;
		_takeProfitPrice = TakeProfitPips > 0m ? _entryPrice - GetPipDistance(TakeProfitPips) : null;
		_highestSinceEntry = candle.HighPrice;
		_lowestSinceEntry = candle.LowPrice;
		_breakEvenActivated = false;
	}

	private void ResetPositionState()
	{
		_activeSide = null;
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_highestSinceEntry = null;
		_lowestSinceEntry = null;
		_breakEvenActivated = false;
	}

	private bool HasMomentumData()
	{
		return _momentumDistance1.HasValue && _momentumDistance2.HasValue && _momentumDistance3.HasValue;
	}

	private bool MomentumDistanceAboveThreshold()
	{
		var threshold = MomentumThreshold;
		return (_momentumDistance1 is decimal m1 && m1 >= threshold)
			|| (_momentumDistance2 is decimal m2 && m2 >= threshold)
			|| (_momentumDistance3 is decimal m3 && m3 >= threshold);
	}

	private bool HasMacdData()
	{
		return _macdMain.HasValue && _macdSignal.HasValue;
	}

	private bool IsMacdBullish()
	{
		return _macdMain is decimal main && _macdSignal is decimal signal && main > signal;
	}

	private bool IsMacdBearish()
	{
		return _macdMain is decimal main && _macdSignal is decimal signal && main < signal;
	}

	private static decimal GetTypicalPrice(ICandleMessage candle)
	{
		return (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
	}

	private decimal GetPipDistance(decimal pips)
	{
		return pips * _pipSize;
	}
}
