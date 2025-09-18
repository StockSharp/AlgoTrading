using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Recreates the core logic of the MetaTrader "Gann Line" expert advisor 24877.
/// Combines LWMA trend confirmation, multi-timeframe momentum and a slow MACD filter.
/// Applies optional break-even and trailing stop management expressed in price steps.
/// </summary>
public class GannLineStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingActivationSteps;
	private readonly StrategyParam<decimal> _trailingDistanceSteps;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenActivationSteps;
	private readonly StrategyParam<decimal> _breakEvenOffsetSteps;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _momentumCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergence _macd = null!;

	private decimal? _momentumAbs1;
	private decimal? _momentumAbs2;
	private decimal? _momentumAbs3;
	private decimal? _macdMain;
	private decimal? _macdSignal;

	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal? _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;

	private decimal _pipSize;

	/// <summary>
	/// Initializes a new instance of the <see cref="GannLineStrategy"/> class.
	/// </summary>
	public GannLineStrategy()
	{
		_fastMaLength = Param(nameof(FastMaLength), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average", "Trend")
			.SetCanOptimize(true)
			.SetOptimize(3, 30, 1);

		_slowMaLength = Param(nameof(SlowMaLength), 85)
			.SetGreaterThanZero()
			.SetDisplay("Slow LWMA", "Length of the slow linear weighted moving average", "Trend")
			.SetCanOptimize(true)
			.SetOptimize(40, 150, 5);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Lookback used by the momentum oscillator", "Momentum")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 2);

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
			.SetNotNegative()
			.SetDisplay("Momentum Threshold", "Minimum distance from 100 required by the momentum filter", "Momentum")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1.5m, 0.1m);

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length for the MACD filter", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(8, 16, 1);

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length for the MACD filter", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 2);

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA length for the MACD filter", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit (steps)", "Take profit distance expressed in price steps", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(20m, 120m, 10m);

		_stopLossSteps = Param(nameof(StopLossSteps), 20m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (steps)", "Stop loss distance expressed in price steps", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 60m, 5m);

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
			.SetDisplay("Use Trailing", "Enable trailing stop management", "Risk Management");

		_trailingActivationSteps = Param(nameof(TrailingActivationSteps), 40m)
			.SetNotNegative()
			.SetDisplay("Trail Activation", "Minimum profit in steps required before trailing activates", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(20m, 80m, 10m);

		_trailingDistanceSteps = Param(nameof(TrailingDistanceSteps), 40m)
			.SetNotNegative()
			.SetDisplay("Trail Distance", "Distance between current extreme and trailing stop (steps)", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(20m, 80m, 10m);

		_useBreakEven = Param(nameof(UseBreakEven), true)
			.SetDisplay("Use BreakEven", "Move stop-loss to break-even once profit target is reached", "Risk Management");

		_breakEvenActivationSteps = Param(nameof(BreakEvenActivationSteps), 30m)
			.SetNotNegative()
			.SetDisplay("BreakEven Activation", "Profit in steps required before moving to break-even", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 60m, 5m);

		_breakEvenOffsetSteps = Param(nameof(BreakEvenOffsetSteps), 30m)
			.SetNotNegative()
			.SetDisplay("BreakEven Offset", "Additional profit (steps) locked after break-even", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 60m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Primary Timeframe", "Candles used for LWMA crossover logic", "General");

		_momentumCandleType = Param(nameof(MomentumCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Momentum Timeframe", "Candles forwarded into the momentum oscillator", "General");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("MACD Timeframe", "Candles forwarded into the slow MACD filter", "General");
	}

	/// <summary>
	/// Fast LWMA period.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow LWMA period.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Momentum lookback length.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum distance from 100 required by the momentum filter.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// MACD fast EMA length.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// MACD slow EMA length.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// MACD signal EMA length.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Take profit distance measured in price steps.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Stop loss distance measured in price steps.
	/// </summary>
	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Enables trailing stop management.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Profit in steps required before trailing activates.
	/// </summary>
	public decimal TrailingActivationSteps
	{
		get => _trailingActivationSteps.Value;
		set => _trailingActivationSteps.Value = value;
	}

	/// <summary>
	/// Distance between the extreme price and the trailing stop.
	/// </summary>
	public decimal TrailingDistanceSteps
	{
		get => _trailingDistanceSteps.Value;
		set => _trailingDistanceSteps.Value = value;
	}

	/// <summary>
	/// Enables break-even logic.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Profit in steps required before moving the stop to break-even.
	/// </summary>
	public decimal BreakEvenActivationSteps
	{
		get => _breakEvenActivationSteps.Value;
		set => _breakEvenActivationSteps.Value = value;
	}

	/// <summary>
	/// Additional profit locked when the stop is moved to break-even.
	/// </summary>
	public decimal BreakEvenOffsetSteps
	{
		get => _breakEvenOffsetSteps.Value;
		set => _breakEvenOffsetSteps.Value = value;
	}

	/// <summary>
	/// Primary candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Candle type used for the momentum oscillator.
	/// </summary>
	public DataType MomentumCandleType
	{
		get => _momentumCandleType.Value;
		set => _momentumCandleType.Value = value;
	}

	/// <summary>
	/// Candle type used for the slow MACD filter.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
			yield break;

		yield return (Security, CandleType);

		var momentumType = MomentumCandleType;
		if (!momentumType.Equals(CandleType))
			yield return (Security, momentumType);

		var macdType = MacdCandleType;
		if (!macdType.Equals(CandleType) && !macdType.Equals(momentumType))
			yield return (Security, macdType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_momentumAbs1 = null;
		_momentumAbs2 = null;
		_momentumAbs3 = null;
		_macdMain = null;
		_macdSignal = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_entryPrice = null;
		_highestPrice = 0m;
		_lowestPrice = 0m;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new WeightedMovingAverage { Length = FastMaLength };
		_slowMa = new WeightedMovingAverage { Length = SlowMaLength };
		_momentum = new Momentum { Length = MomentumPeriod };
		_macd = new MovingAverageConvergenceDivergence
		{
			FastLength = MacdFastLength,
			SlowLength = MacdSlowLength,
			SignalLength = MacdSignalLength
		};

		_pipSize = GetPipSize();

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription.Bind(ProcessMainCandle).Start();

		var momentumSubscription = SubscribeCandles(MomentumCandleType);
		momentumSubscription.Bind(_momentum, ProcessMomentum).Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription.BindEx(_macd, ProcessMacd).Start();

		StartProtection();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			_entryPrice = null;
			_stopPrice = null;
			_takeProfitPrice = null;
			_highestPrice = 0m;
			_lowestPrice = 0m;
			return;
		}

		if (PositionPrice is not decimal price)
			return;

		_entryPrice = price;

		var stopDistance = StepsToPrice(StopLossSteps);
		var takeDistance = StepsToPrice(TakeProfitSteps);

		if (Position > 0m)
		{
			_stopPrice = NormalizePrice(price - stopDistance);
			_takeProfitPrice = NormalizePrice(price + takeDistance);
			_highestPrice = price;
			_lowestPrice = 0m;
		}
		else
		{
			_stopPrice = NormalizePrice(price + stopDistance);
			_takeProfitPrice = NormalizePrice(price - takeDistance);
			_lowestPrice = price;
			_highestPrice = 0m;
		}
	}

	private void ProcessMainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		var fastValue = _fastMa.Process(new DecimalIndicatorValue(_fastMa, typical, candle.OpenTime)).ToDecimal();
		var slowValue = _slowMa.Process(new DecimalIndicatorValue(_slowMa, typical, candle.OpenTime)).ToDecimal();

		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
			return;

		UpdateTradeManagement(candle);

		if (CheckStops(candle))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_momentumAbs1 is not decimal mom1 ||
		_momentumAbs2 is not decimal mom2 ||
		_momentumAbs3 is not decimal mom3 ||
		_macdMain is not decimal macdMain ||
		_macdSignal is not decimal macdSignal)
		{
			return;
		}

		var buyMomentumOk = mom1 >= MomentumThreshold || mom2 >= MomentumThreshold || mom3 >= MomentumThreshold;
		var sellMomentumOk = buyMomentumOk;

		var canBuy = fastValue > slowValue && buyMomentumOk && macdMain > macdSignal;
		var canSell = fastValue < slowValue && sellMomentumOk && macdMain < macdSignal;

		if (canBuy && Position <= 0m)
		{
			if (Position < 0m)
				CloseShort(candle.ClosePrice);

			EnterLong(candle.ClosePrice);
		}
		else if (canSell && Position >= 0m)
		{
			if (Position > 0m)
				CloseLong(candle.ClosePrice);

			EnterShort(candle.ClosePrice);
		}
	}

	private void ProcessMomentum(ICandleMessage candle, decimal momentum)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var distance = Math.Abs(momentum - 100m);
		_momentumAbs3 = _momentumAbs2;
		_momentumAbs2 = _momentumAbs1;
		_momentumAbs1 = distance;
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue value)
	{
		if (!value.IsFinal)
			return;

		if (value is MovingAverageConvergenceDivergenceSignalValue macdSignalValue)
		{
			_macdMain = macdSignalValue.Macd;
			_macdSignal = macdSignalValue.Signal;
		}
		else if (value is MovingAverageConvergenceDivergenceValue macdValue)
		{
			_macdMain = macdValue.Macd;
			_macdSignal = macdValue.Signal;
		}
	}

	private void EnterLong(decimal price)
	{
		var volume = Volume;
		if (volume <= 0m)
			volume = 1m;

		BuyMarket(volume);
		_highestPrice = price;
	}

	private void EnterShort(decimal price)
	{
		var volume = Volume;
		if (volume <= 0m)
			volume = 1m;

		SellMarket(volume);
		_lowestPrice = price;
	}

	private void CloseLong(decimal price)
	{
		if (Position <= 0m)
			return;

		SellMarket(Position);
		_stopPrice = null;
		_takeProfitPrice = null;
		_highestPrice = 0m;
	}

	private void CloseShort(decimal price)
	{
		if (Position >= 0m)
			return;

		BuyMarket(-Position);
		_stopPrice = null;
		_takeProfitPrice = null;
		_lowestPrice = 0m;
	}

	private void UpdateTradeManagement(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (candle.HighPrice > _highestPrice)
				_highestPrice = candle.HighPrice;

			ApplyBreakEvenLong();
			ApplyTrailingLong();
		}
		else if (Position < 0m)
		{
			if (_lowestPrice == 0m || candle.LowPrice < _lowestPrice)
				_lowestPrice = candle.LowPrice;

			ApplyBreakEvenShort();
			ApplyTrailingShort();
		}
	}

	private bool CheckStops(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				CloseLong(stop);
				return true;
			}

			if (_takeProfitPrice is decimal take && take > 0m && candle.HighPrice >= take)
			{
				CloseLong(take);
				return true;
			}
		}
		else if (Position < 0m)
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				CloseShort(stop);
				return true;
			}

			if (_takeProfitPrice is decimal take && take > 0m && candle.LowPrice <= take)
			{
				CloseShort(take);
				return true;
			}
		}

		return false;
	}

	private void ApplyBreakEvenLong()
	{
		if (!UseBreakEven || Position <= 0m || _entryPrice is not decimal entry)
			return;

		var trigger = StepsToPrice(BreakEvenActivationSteps);
		var offset = StepsToPrice(BreakEvenOffsetSteps);
		if (trigger <= 0m)
			return;

		var profit = _highestPrice - entry;
		if (profit < trigger)
			return;

		var newStop = NormalizePrice(entry + offset);
		if (_stopPrice is not decimal current || newStop > current)
			_stopPrice = newStop;
	}

	private void ApplyBreakEvenShort()
	{
		if (!UseBreakEven || Position >= 0m || _entryPrice is not decimal entry)
			return;

		var trigger = StepsToPrice(BreakEvenActivationSteps);
		var offset = StepsToPrice(BreakEvenOffsetSteps);
		if (trigger <= 0m)
			return;

		var profit = entry - _lowestPrice;
		if (profit < trigger)
			return;

		var newStop = NormalizePrice(entry - offset);
		if (_stopPrice is not decimal current || newStop < current)
			_stopPrice = newStop;
	}

	private void ApplyTrailingLong()
	{
		if (!UseTrailingStop || Position <= 0m || _entryPrice is not decimal entry)
			return;

		var trigger = StepsToPrice(TrailingActivationSteps);
		var distance = StepsToPrice(TrailingDistanceSteps);
		if (trigger <= 0m || distance <= 0m)
			return;

		var move = _highestPrice - entry;
		if (move < trigger)
			return;

		var newStop = NormalizePrice(_highestPrice - distance);
		if (_stopPrice is not decimal current || newStop > current)
			_stopPrice = newStop;
	}

	private void ApplyTrailingShort()
	{
		if (!UseTrailingStop || Position >= 0m || _entryPrice is not decimal entry)
			return;

		var trigger = StepsToPrice(TrailingActivationSteps);
		var distance = StepsToPrice(TrailingDistanceSteps);
		if (trigger <= 0m || distance <= 0m)
			return;

		var move = entry - _lowestPrice;
		if (move < trigger)
			return;

		var newStop = NormalizePrice(_lowestPrice + distance);
		if (_stopPrice is not decimal current || newStop < current)
			_stopPrice = newStop;
	}

	private decimal StepsToPrice(decimal steps)
	{
		if (_pipSize <= 0m)
			return 0m;

		return steps * _pipSize;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		if (step < 1m)
			return step * 10m;

		return step;
	}

	private decimal NormalizePrice(decimal price)
	{
		return Security?.ShrinkPrice(price) ?? price;
	}
}
