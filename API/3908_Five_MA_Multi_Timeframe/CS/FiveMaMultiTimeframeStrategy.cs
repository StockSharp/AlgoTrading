using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe trend strategy based on five simple moving averages and the accelerator oscillator.
/// Aggregates momentum across three timeframes and produces graded entry signals.
/// </summary>
public class FiveMaMultiTimeframeStrategy : Strategy
{
	private const decimal Weight = 12.5m;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherTimeframe1;
	private readonly StrategyParam<DataType> _higherTimeframe2;
	private readonly StrategyParam<int> _firstPeriod;
	private readonly StrategyParam<int> _secondPeriod;
	private readonly StrategyParam<int> _thirdPeriod;
	private readonly StrategyParam<int> _fourthPeriod;
	private readonly StrategyParam<int> _fifthPeriod;
	private readonly StrategyParam<int> _openLevel;
	private readonly StrategyParam<int> _closeLevel;

	private readonly TimeframeState _primaryState = new(true);
	private readonly TimeframeState _secondaryState = new(false);
	private readonly TimeframeState _tertiaryState = new(true);

	private SimpleMovingAverage[] _primarySma = Array.Empty<SimpleMovingAverage>();
	private SimpleMovingAverage[] _secondarySma = Array.Empty<SimpleMovingAverage>();
	private SimpleMovingAverage[] _tertiarySma = Array.Empty<SimpleMovingAverage>();
	private AcceleratorOscillator? _primaryAccelerator;
	private AcceleratorOscillator? _tertiaryAccelerator;

	private int _currentSignal;

	/// <summary>
	/// Initializes a new instance of <see cref="FiveMaMultiTimeframeStrategy"/>.
	/// </summary>
	public FiveMaMultiTimeframeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Primary TF", "Primary candle timeframe", "General");

		_higherTimeframe1 = Param(nameof(HigherTimeframe1), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Higher TF1", "First higher candle timeframe", "General");

		_higherTimeframe2 = Param(nameof(HigherTimeframe2), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Higher TF2", "Second higher candle timeframe", "General");

		_firstPeriod = Param(nameof(FirstPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("MA #1", "Fastest moving average period", "Indicators");

		_secondPeriod = Param(nameof(SecondPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("MA #2", "Second moving average period", "Indicators");

		_thirdPeriod = Param(nameof(ThirdPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("MA #3", "Middle moving average period", "Indicators");

		_fourthPeriod = Param(nameof(FourthPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("MA #4", "Fourth moving average period", "Indicators");

		_fifthPeriod = Param(nameof(FifthPeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("MA #5", "Slowest moving average period", "Indicators");

		_openLevel = Param(nameof(OpenLevel), 0)
			.SetDisplay("Open Level", "Minimum signal grade to open trades", "Trading");

		_closeLevel = Param(nameof(CloseLevel), 1)
			.SetDisplay("Close Level", "Signal grade required to close opposite trades", "Trading");
	}

	/// <summary>
	/// Primary candle type used for signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// First higher timeframe used for confirmation.
	/// </summary>
	public DataType HigherTimeframe1
	{
		get => _higherTimeframe1.Value;
		set => _higherTimeframe1.Value = value;
	}

	/// <summary>
	/// Second higher timeframe used for confirmation.
	/// </summary>
	public DataType HigherTimeframe2
	{
		get => _higherTimeframe2.Value;
		set => _higherTimeframe2.Value = value;
	}

	/// <summary>
	/// Fastest moving average period.
	/// </summary>
	public int FirstPeriod
	{
		get => _firstPeriod.Value;
		set => _firstPeriod.Value = value;
	}

	/// <summary>
	/// Second moving average period.
	/// </summary>
	public int SecondPeriod
	{
		get => _secondPeriod.Value;
		set => _secondPeriod.Value = value;
	}

	/// <summary>
	/// Third moving average period.
	/// </summary>
	public int ThirdPeriod
	{
		get => _thirdPeriod.Value;
		set => _thirdPeriod.Value = value;
	}

	/// <summary>
	/// Fourth moving average period.
	/// </summary>
	public int FourthPeriod
	{
		get => _fourthPeriod.Value;
		set => _fourthPeriod.Value = value;
	}

	/// <summary>
	/// Slowest moving average period.
	/// </summary>
	public int FifthPeriod
	{
		get => _fifthPeriod.Value;
		set => _fifthPeriod.Value = value;
	}

	/// <summary>
	/// Minimum signal magnitude required to open new trades.
	/// </summary>
	public int OpenLevel
	{
		get => _openLevel.Value;
		set => _openLevel.Value = value;
	}

	/// <summary>
	/// Signal magnitude that closes positions in the opposite direction.
	/// </summary>
	public int CloseLevel
	{
		get => _closeLevel.Value;
		set => _closeLevel.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, HigherTimeframe1), (Security, HigherTimeframe2)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		ResetState(_primaryState);
		ResetState(_secondaryState);
		ResetState(_tertiaryState);

		_primarySma = Array.Empty<SimpleMovingAverage>();
		_secondarySma = Array.Empty<SimpleMovingAverage>();
		_tertiarySma = Array.Empty<SimpleMovingAverage>();
		_primaryAccelerator = null;
		_tertiaryAccelerator = null;

		_currentSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Prepare indicators for every timeframe before subscribing.
		_primarySma = CreateSmaSet();
		_secondarySma = CreateSmaSet();
		_tertiarySma = CreateSmaSet();

		_primaryAccelerator = new AcceleratorOscillator();
		_tertiaryAccelerator = new AcceleratorOscillator();

		var primarySubscription = SubscribeCandles(CandleType);
		// Bind the primary timeframe with all required indicators.
		primarySubscription
			.Bind(
				_primarySma[0],
				_primarySma[1],
				_primarySma[2],
				_primarySma[3],
				_primarySma[4],
				_primaryAccelerator!,
				ProcessPrimary)
			.Start();

		var secondarySubscription = SubscribeCandles(HigherTimeframe1);
		// Bind the first higher timeframe without the oscillator filter.
		secondarySubscription
			.Bind(
				_secondarySma[0],
				_secondarySma[1],
				_secondarySma[2],
				_secondarySma[3],
				_secondarySma[4],
				ProcessSecondary)
			.Start();

		var tertiarySubscription = SubscribeCandles(HigherTimeframe2);
		// Bind the second higher timeframe and include the oscillator filter.
		tertiarySubscription
			.Bind(
				_tertiarySma[0],
				_tertiarySma[1],
				_tertiarySma[2],
				_tertiarySma[3],
				_tertiarySma[4],
				_tertiaryAccelerator!,
				ProcessTertiary)
			.Start();

		StartProtection();
		// Activate default position protection helper.
	}

	private SimpleMovingAverage[] CreateSmaSet()
	{
		return new[]
		{
			new SimpleMovingAverage { Length = FirstPeriod },
			new SimpleMovingAverage { Length = SecondPeriod },
			new SimpleMovingAverage { Length = ThirdPeriod },
			new SimpleMovingAverage { Length = FourthPeriod },
			new SimpleMovingAverage { Length = FifthPeriod }
		};
	}

	private void ProcessPrimary(ICandleMessage candle, decimal ma1, decimal ma2, decimal ma3, decimal ma4, decimal ma5, decimal accelerator)
	{
		// Primary timeframe drives entries, so it also triggers signal evaluation.
		if (candle.State != CandleStates.Finished)
		{
			// Only react once the candle is completed.
			return;
		}

		var acReady = UpdateAcceleratorState(_primaryState, accelerator);
		// Update oscillator history and slope counters for the timeframe.
		var smaReady = UpdateSmaState(_primaryState, ma1, ma2, ma3, ma4, ma5);

		if (!smaReady || !acReady)
			return;

		ComputeScores(_primaryState);
		// Recalculate the aggregated bullish and bearish scores for this timeframe.
		TryGenerateSignal();
	}

	private void ProcessSecondary(ICandleMessage candle, decimal ma1, decimal ma2, decimal ma3, decimal ma4, decimal ma5)
	{
		// Secondary timeframe provides slower trend confirmation.
		if (candle.State != CandleStates.Finished)
		{
			// Only react once the candle is completed.
			return;
		}

		var acReady = UpdateAcceleratorState(_secondaryState, null);
		var smaReady = UpdateSmaState(_secondaryState, ma1, ma2, ma3, ma4, ma5);

		if (!smaReady || !acReady)
			return;

		ComputeScores(_secondaryState);
	}

	private void ProcessTertiary(ICandleMessage candle, decimal ma1, decimal ma2, decimal ma3, decimal ma4, decimal ma5, decimal accelerator)
	{
		// Tertiary timeframe represents the slowest trend filter.
		if (candle.State != CandleStates.Finished)
		{
			// Only react once the candle is completed.
			return;
		}

		var acReady = UpdateAcceleratorState(_tertiaryState, accelerator);
		var smaReady = UpdateSmaState(_tertiaryState, ma1, ma2, ma3, ma4, ma5);

		if (!smaReady || !acReady)
			return;

		ComputeScores(_tertiaryState);
	}

	private void TryGenerateSignal()
	{
		// Combine scores from all timeframes into a single graded signal.
		if (!_primaryState.HasScores || !_secondaryState.HasScores || !_tertiaryState.HasScores)
			return;

		var signal = 0;
		// Start with a neutral signal and upgrade it according to momentum.

		var bullish = _primaryState.UpScore > 50m && _secondaryState.UpScore > 50m && _tertiaryState.UpScore > 50m;
		if (bullish)
			signal = 1;

		var bearish = _primaryState.DownScore > 50m && _secondaryState.DownScore > 50m && _tertiaryState.DownScore > 50m;
		if (bearish)
			signal = -1;

		var strongBullish = _primaryState.UpScore >= 75m && _secondaryState.UpScore >= 75m && _tertiaryState.UpScore >= 75m;
		if (strongBullish)
			signal = 2;

		var strongBearish = _primaryState.DownScore >= 75m && _secondaryState.DownScore >= 75m && _tertiaryState.DownScore >= 75m;
		if (strongBearish)
			signal = -2;

		_currentSignal = signal;

		ApplySignal();
	}

	private void ApplySignal()
	{
		// Execute trades or exits according to the current signal level.
		if (_currentSignal == 0)
		{
			// Nothing to do when signal is neutral.
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			// Close long positions when a strong bearish signal appears.
			if (_currentSignal < -CloseLevel)
			{
				ClosePosition();
				return;
			}
		}
		else if (Position < 0)
		{
			// Close short positions when a strong bullish signal appears.
			if (_currentSignal > CloseLevel)
			{
				ClosePosition();
				return;
			}
		}

		// Do not reverse within the same candle; wait for flat position.
		if (Position != 0)
			return;

		if (_currentSignal > OpenLevel)
		{
			BuyMarket();
		}
		else if (_currentSignal < -OpenLevel)
		{
			SellMarket();
		}
	}

	private bool UpdateSmaState(TimeframeState state, decimal ma1, decimal ma2, decimal ma3, decimal ma4, decimal ma5)
	{
		// Track current and previous moving average values to derive slope directions.
		if (!state.SmaInitialized)
		{
			// First call only stores baseline values without generating a signal.
			state.PreviousSma[0] = ma1;
			state.PreviousSma[1] = ma2;
			state.PreviousSma[2] = ma3;
			state.PreviousSma[3] = ma4;
			state.PreviousSma[4] = ma5;
			state.SmaInitialized = true;
			state.HasScores = false;
			return false;
		}

		// Count how many averages slope up or down compared to the previous candle.
		var up = 0;
		var down = 0;

		var prev1 = state.PreviousSma[0]!.Value;
		if (ma1 > prev1)
			up++;
		else if (ma1 < prev1)
			down++;
		state.PreviousSma[0] = ma1;

		var prev2 = state.PreviousSma[1]!.Value;
		if (ma2 > prev2)
			up++;
		else if (ma2 < prev2)
			down++;
		state.PreviousSma[1] = ma2;

		var prev3 = state.PreviousSma[2]!.Value;
		if (ma3 > prev3)
			up++;
		else if (ma3 < prev3)
			down++;
		state.PreviousSma[2] = ma3;

		var prev4 = state.PreviousSma[3]!.Value;
		if (ma4 > prev4)
			up++;
		else if (ma4 < prev4)
			down++;
		state.PreviousSma[3] = ma4;

		var prev5 = state.PreviousSma[4]!.Value;
		if (ma5 > prev5)
			up++;
		else if (ma5 < prev5)
			down++;
		state.PreviousSma[4] = ma5;

		state.UpSlopeCount = up;
		state.DownSlopeCount = down;
		return true;
	}

	private bool UpdateAcceleratorState(TimeframeState state, decimal? value)
	{
		// Maintain oscillator state and translate its pattern into discrete scores.
		if (!state.UseAccelerator)
		{
			// Some timeframes skip the oscillator filter entirely.
			state.UpAcceleratorContribution = 0m;
			state.DownAcceleratorContribution = 0m;
			state.AcceleratorInitialized = true;
			return true;
		}

		if (value is null)
			return false;

		// Shift history to keep the latest four oscillator readings.
		state.AcceleratorHistory[3] = state.AcceleratorHistory[2];
		state.AcceleratorHistory[2] = state.AcceleratorHistory[1];
		state.AcceleratorHistory[1] = state.AcceleratorHistory[0];
		state.AcceleratorHistory[0] = value;

		// Require four values before evaluating the oscillator pattern.
		if (state.AcceleratorHistory[3] is null)
		{
			state.AcceleratorInitialized = false;
			state.UpAcceleratorContribution = 0m;
			state.DownAcceleratorContribution = 0m;
			return false;
		}

		var ac0 = state.AcceleratorHistory[0]!.Value;
		var ac1 = state.AcceleratorHistory[1]!.Value;
		var ac2 = state.AcceleratorHistory[2]!.Value;
		var ac3 = state.AcceleratorHistory[3]!.Value;

		var bullish = (ac1 > ac2 && ac2 > ac3 && ac0 < 0m && ac0 > ac1) || (ac0 > ac1 && ac1 > ac2 && ac0 > 0m);
		var bearish = (ac1 < ac2 && ac2 < ac3 && ac0 > 0m && ac0 < ac1) || (ac0 < ac1 && ac1 < ac2 && ac0 < 0m);

		if (bullish)
		{
			state.UpAcceleratorContribution = 3m;
			state.DownAcceleratorContribution = 0m;
		}
		else if (bearish)
		{
			state.UpAcceleratorContribution = 0m;
			state.DownAcceleratorContribution = 3m;
		}
		else
		{
			state.UpAcceleratorContribution = 0m;
			state.DownAcceleratorContribution = 0m;
		}

		state.AcceleratorInitialized = true;
		return true;
	}

	private void ComputeScores(TimeframeState state)
	{
		// Convert discrete counts into percentage-style scores.
		var upComponents = state.UpSlopeCount + state.UpAcceleratorContribution;
		var downComponents = state.DownSlopeCount + state.DownAcceleratorContribution;

		state.UpScore = upComponents * Weight;
		state.DownScore = downComponents * Weight;
		state.HasScores = true;
	}

	private static void ResetState(TimeframeState state)
	{
		// Remove cached values so the strategy can restart cleanly.
		for (var i = 0; i < state.PreviousSma.Length; i++)
			state.PreviousSma[i] = null;

		for (var i = 0; i < state.AcceleratorHistory.Length; i++)
			state.AcceleratorHistory[i] = null;

		state.SmaInitialized = false;
		state.AcceleratorInitialized = !state.UseAccelerator;
		state.UpSlopeCount = 0;
		state.DownSlopeCount = 0;
		state.UpAcceleratorContribution = 0m;
		state.DownAcceleratorContribution = 0m;
		state.UpScore = 0m;
		state.DownScore = 0m;
		state.HasScores = false;
	}

	private sealed class TimeframeState
	{
		public TimeframeState(bool useAccelerator)
		{
			UseAccelerator = useAccelerator;
			PreviousSma = new decimal?[5];
			AcceleratorHistory = new decimal?[4];
		}

		public bool UseAccelerator { get; }
		public decimal?[] PreviousSma { get; }
		public decimal?[] AcceleratorHistory { get; }
		public bool SmaInitialized { get; set; }
		public bool AcceleratorInitialized { get; set; }
		public int UpSlopeCount { get; set; }
		public int DownSlopeCount { get; set; }
		public decimal UpAcceleratorContribution { get; set; }
		public decimal DownAcceleratorContribution { get; set; }
		public decimal UpScore { get; set; }
		public decimal DownScore { get; set; }
		public bool HasScores { get; set; }
	}
}
