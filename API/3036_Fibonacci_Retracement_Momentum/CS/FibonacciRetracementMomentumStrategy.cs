using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fibonacci retracement strategy with momentum and MACD filters.
/// </summary>
public class FibonacciRetracementMomentumStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _fibonacciCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private readonly Queue<decimal> _momentumDiffs = new();
	private readonly List<ICandleMessage> _recentCandles = new();

	private decimal? _fibHigh;
	private decimal? _fibLow;
	private decimal? _macdMain;
	private decimal? _macdSignal;
	private decimal? _entryPrice;

	/// <summary>
	/// Length of the fast weighted moving average on the primary timeframe.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Length of the slow weighted moving average on the primary timeframe.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Lookback for the momentum indicator on the higher timeframe.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// Minimum deviation from 100 required from the momentum indicator.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Fast exponential length used inside the MACD filter.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow exponential length used inside the MACD filter.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal length used inside the MACD filter.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Primary candle type for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used for Fibonacci anchors and momentum.
	/// </summary>
	public DataType FibonacciCandleType
	{
		get => _fibonacciCandleType.Value;
		set => _fibonacciCandleType.Value = value;
	}

	/// <summary>
	/// Timeframe used for the MACD trend filter.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy and declares tunable parameters.
	/// </summary>
	public FibonacciRetracementMomentumStrategy()
	{
		_fastMaLength = Param(nameof(FastMaLength), 6)
			.SetDisplay("Fast MA Length", "Length of the fast WMA on the base timeframe", "Indicators")
			.SetGreaterThanZero();

		_slowMaLength = Param(nameof(SlowMaLength), 85)
			.SetDisplay("Slow MA Length", "Length of the slow WMA on the base timeframe", "Indicators")
			.SetGreaterThanZero();

		_momentumLength = Param(nameof(MomentumLength), 14)
			.SetDisplay("Momentum Length", "Lookback period for momentum on higher timeframe", "Indicators")
			.SetGreaterThanZero();

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
			.SetDisplay("Momentum Threshold", "Minimum |Momentum-100| value", "Filters")
			.SetGreaterOrEqualZero();

		_stopLossSteps = Param(nameof(StopLossSteps), 20m)
			.SetDisplay("Stop Loss (steps)", "Stop-loss distance in price steps", "Risk")
			.SetGreaterOrEqualZero();

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 50m)
			.SetDisplay("Take Profit (steps)", "Take-profit distance in price steps", "Risk")
			.SetGreaterOrEqualZero();

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetDisplay("MACD Fast", "Fast EMA length for MACD filter", "Indicators")
			.SetGreaterThanZero();

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetDisplay("MACD Slow", "Slow EMA length for MACD filter", "Indicators")
			.SetGreaterThanZero();

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetDisplay("MACD Signal", "Signal EMA length for MACD filter", "Indicators")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Primary Candles", "Timeframe used for trade execution", "General");

		_fibonacciCandleType = Param(nameof(FibonacciCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Fibonacci Candles", "Higher timeframe for Fibonacci anchors", "General");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("MACD Candles", "Timeframe for MACD trend filter", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_momentumDiffs.Clear();
		_recentCandles.Clear();
		_fibHigh = null;
		_fibLow = null;
		_macdMain = null;
		_macdSignal = null;
		_entryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Instantiate indicators according to current parameter values.
		_fastMa = new WeightedMovingAverage { Length = FastMaLength };
		_slowMa = new WeightedMovingAverage { Length = SlowMaLength };
		_momentum = new Momentum { Length = MomentumLength };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength },
			},
			SignalMa = { Length = MacdSignalLength }
		};

		// Subscribe to primary timeframe candles and bind moving averages.
		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
			.Bind(_fastMa, _slowMa, ProcessMainCandle)
			.Start();

		// Higher timeframe provides Fibonacci anchors and the momentum filter.
		var fibSubscription = SubscribeCandles(FibonacciCandleType);
		fibSubscription
			.Bind(_momentum, ProcessFibonacciCandle)
			.Start();

		// Monthly MACD filter monitors the dominant trend direction.
		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription
			.BindEx(_macd, ProcessMacdCandle)
			.Start();
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal fastMaValue, decimal slowMaValue)
	{
		// Only work with completed bars to replicate the MQL implementation.
		if (candle.State != CandleStates.Finished)
			return;

		_recentCandles.Add(candle);
		if (_recentCandles.Count > 5)
			_recentCandles.RemoveAt(0);

		// Ensure trading is allowed and all core indicators are ready.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || _fibHigh is null || _fibLow is null || _macdMain is null || _macdSignal is null)
			return;

		if (_momentumDiffs.Count == 0)
			return;

	// Manage protective exits before checking for new entries.
		if (ManageRisk(candle))
			return;

		if (_recentCandles.Count < 4)
			return;

		var previous = _recentCandles[^1];
		var prior1 = _recentCandles[^2];
		var prior2 = _recentCandles[^3];
		var prior3 = _recentCandles[^4];

		var range = _fibHigh.Value - _fibLow.Value;
		if (range <= 0m)
			return;

		var fibLevels = new[]
		{
			_fibHigh.Value,
			_fibHigh.Value - range * 0.236m,
			_fibHigh.Value - range * 0.382m,
			_fibHigh.Value - range * 0.5m,
			_fibHigh.Value - range * 0.618m,
			_fibHigh.Value - range * 0.764m,
			_fibLow.Value
		};

		var hasMomentumImpulse = HasMomentumImpulse();
		var macdBullish = _macdMain > _macdSignal;
		var macdBearish = _macdMain < _macdSignal;
		var fibSupportTouched = CheckFibonacciTouch(fibLevels, previous.LowPrice, prior1.ClosePrice, prior2.ClosePrice, prior3.ClosePrice, true);
		var fibResistanceTouched = CheckFibonacciTouch(fibLevels, previous.HighPrice, prior1.ClosePrice, prior2.ClosePrice, prior3.ClosePrice, false);
		var bullishStructure = prior1.LowPrice < previous.HighPrice;
		var bearishStructure = previous.LowPrice < prior1.HighPrice;

		if (fibSupportTouched && bullishStructure && hasMomentumImpulse && macdBullish && fastMaValue > slowMaValue)
		{
			// Close shorts before entering a long position.
			if (Position < 0)
			{
				ClosePosition();
				_entryPrice = null;
				return;
			}

			if (Position == 0)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
		}
		else if (fibResistanceTouched && bearishStructure && hasMomentumImpulse && macdBearish && fastMaValue < slowMaValue)
		{
			// Close longs before entering a short position.
			if (Position > 0)
			{
				ClosePosition();
				_entryPrice = null;
				return;
			}

			if (Position == 0)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}
	}

	private void ProcessFibonacciCandle(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Store latest swing high/low for Fibonacci retracement levels.
		_fibHigh = candle.HighPrice;
		_fibLow = candle.LowPrice;

		if (!_momentum.IsFormed)
			return;

		var deviation = Math.Abs(100m - momentumValue);
		if (_momentumDiffs.Count == 3)
			_momentumDiffs.Dequeue();
		_momentumDiffs.Enqueue(deviation);
	}

	private void ProcessMacdCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_macd.IsFormed)
			return;

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue typed)
			return;

		if (typed.Macd is not decimal macd || typed.Signal is not decimal signal)
			return;

		_macdMain = macd;
		_macdSignal = signal;
	}

	private bool HasMomentumImpulse()
	{
		foreach (var diff in _momentumDiffs)
		{
			if (diff > MomentumThreshold)
				return true;
		}

		return false;
	}

	private bool CheckFibonacciTouch(decimal[] levels, decimal extreme, decimal close1, decimal close2, decimal close3, bool isLong)
	{
		foreach (var level in levels)
		{
			var closeCondition = isLong
				? close1 > level || close2 > level || close3 > level
				: close1 < level || close2 < level || close3 < level;

			var touchCondition = isLong ? extreme <= level : extreme >= level;

			if (closeCondition && touchCondition)
				return true;
		}

		return false;
	}

	private bool ManageRisk(ICandleMessage candle)
	{
		if (Position == 0)
		{
			_entryPrice = null;
			return false;
		}

		if (_entryPrice is null)
			return false;

		var priceStep = Security?.PriceStep ?? 1m;
		var stopDistance = StopLossSteps > 0m ? StopLossSteps * priceStep : (decimal?)null;
		var takeDistance = TakeProfitSteps > 0m ? TakeProfitSteps * priceStep : (decimal?)null;

		if (Position > 0)
		{
			if (takeDistance is decimal tp && candle.HighPrice >= _entryPrice.Value + tp)
			{
				ClosePosition();
				_entryPrice = null;
				return true;
			}

			if (stopDistance is decimal sl && candle.LowPrice <= _entryPrice.Value - sl)
			{
				ClosePosition();
				_entryPrice = null;
				return true;
			}
		}
		else if (Position < 0)
		{
			if (takeDistance is decimal tp && candle.LowPrice <= _entryPrice.Value - tp)
			{
				ClosePosition();
				_entryPrice = null;
				return true;
			}

			if (stopDistance is decimal sl && candle.HighPrice >= _entryPrice.Value + sl)
			{
				ClosePosition();
				_entryPrice = null;
				return true;
			}
		}

		return false;
	}
}
