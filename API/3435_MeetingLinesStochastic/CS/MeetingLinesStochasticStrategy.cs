namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that replicates the MetaTrader "Expert_AML_Stoch" logic using the StockSharp high-level API.
/// </summary>
public class MeetingLinesStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stochasticLength;
	private readonly StrategyParam<int> _stochasticSmoothing;
	private readonly StrategyParam<int> _stochasticSignal;
	private readonly StrategyParam<int> _bodyAveragePeriod;
	private readonly StrategyParam<decimal> _longEntryLevel;
	private readonly StrategyParam<decimal> _shortEntryLevel;
	private readonly StrategyParam<decimal> _exitLowerLevel;
	private readonly StrategyParam<decimal> _exitUpperLevel;

	private StochasticOscillator _stochastic = null!;
	private SimpleMovingAverage _bodyAverage = null!;

	private ICandleMessage _previousCandle;
	private ICandleMessage _secondPreviousCandle;
	private decimal? _previousBodyAverage;
	private decimal? _previousSignal;
	private decimal? _secondPreviousSignal;

	/// <summary>
	/// Initializes a new instance of the <see cref="MeetingLinesStochasticStrategy"/>.
	/// </summary>
	public MeetingLinesStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle type used for analysis.", "General");

		_stochasticLength = Param(nameof(StochasticLength), 3)
			.SetDisplay("%K Length", "Lookback period for the raw %K calculation.", "Stochastic")
			.SetCanOptimize(true);

		_stochasticSmoothing = Param(nameof(StochasticSmoothing), 25)
			.SetDisplay("%K Smoothing", "Smoothing period applied to %K (MetaTrader slowing).", "Stochastic")
			.SetCanOptimize(true);

		_stochasticSignal = Param(nameof(StochasticSignal), 36)
			.SetDisplay("%D Period", "Smoothing period for the %D signal line.", "Stochastic")
			.SetCanOptimize(true);

		_bodyAveragePeriod = Param(nameof(BodyAveragePeriod), 3)
			.SetDisplay("Body Average Period", "Number of candles used to average body size.", "Pattern")
			.SetCanOptimize(true);

		_longEntryLevel = Param(nameof(LongEntryLevel), 30m)
			.SetDisplay("Bullish Confirmation", "Maximum %D level allowed for bullish entries.", "Trading Rules")
			.SetCanOptimize(true);

		_shortEntryLevel = Param(nameof(ShortEntryLevel), 70m)
			.SetDisplay("Bearish Confirmation", "Minimum %D level required for bearish entries.", "Trading Rules")
			.SetCanOptimize(true);

		_exitLowerLevel = Param(nameof(ExitLowerLevel), 20m)
			.SetDisplay("Lower Exit Level", "Threshold used for upward crosses that close shorts.", "Trading Rules")
			.SetCanOptimize(true);

		_exitUpperLevel = Param(nameof(ExitUpperLevel), 80m)
			.SetDisplay("Upper Exit Level", "Threshold used for downward crosses that close longs.", "Trading Rules")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Type of candles used for signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Lookback period for the raw %K calculation.
	/// </summary>
	public int StochasticLength
	{
		get => _stochasticLength.Value;
		set => _stochasticLength.Value = value;
	}

	/// <summary>
	/// Smoothing period applied to the %K line.
	/// </summary>
	public int StochasticSmoothing
	{
		get => _stochasticSmoothing.Value;
		set => _stochasticSmoothing.Value = value;
	}

	/// <summary>
	/// Period of the %D signal line.
	/// </summary>
	public int StochasticSignal
	{
		get => _stochasticSignal.Value;
		set => _stochasticSignal.Value = value;
	}

	/// <summary>
	/// Number of candles used to average candle body size.
	/// </summary>
	public int BodyAveragePeriod
	{
		get => _bodyAveragePeriod.Value;
		set => _bodyAveragePeriod.Value = value;
	}

	/// <summary>
	/// Maximum %D value that still allows a long entry.
	/// </summary>
	public decimal LongEntryLevel
	{
		get => _longEntryLevel.Value;
		set => _longEntryLevel.Value = value;
	}

	/// <summary>
	/// Minimum %D value that allows a short entry.
	/// </summary>
	public decimal ShortEntryLevel
	{
		get => _shortEntryLevel.Value;
		set => _shortEntryLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold used to close short positions on upward crosses.
	/// </summary>
	public decimal ExitLowerLevel
	{
		get => _exitLowerLevel.Value;
		set => _exitLowerLevel.Value = value;
	}

	/// <summary>
	/// Upper threshold used to close long positions on downward crosses.
	/// </summary>
	public decimal ExitUpperLevel
	{
		get => _exitUpperLevel.Value;
		set => _exitUpperLevel.Value = value;
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

		_previousCandle = null;
		_secondPreviousCandle = null;
		_previousBodyAverage = null;
		_previousSignal = null;
		_secondPreviousSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (StochasticLength <= 0)
			throw new InvalidOperationException("StochasticLength must be greater than zero.");

		if (StochasticSmoothing <= 0)
			throw new InvalidOperationException("StochasticSmoothing must be greater than zero.");

		if (StochasticSignal <= 0)
			throw new InvalidOperationException("StochasticSignal must be greater than zero.");

		if (BodyAveragePeriod <= 0)
			throw new InvalidOperationException("BodyAveragePeriod must be greater than zero.");

		_stochastic = new StochasticOscillator
		{
			Length = StochasticLength,
			K = { Length = StochasticSmoothing },
			D = { Length = StochasticSignal },
		};

		_bodyAverage = new SimpleMovingAverage
		{
			Length = BodyAveragePeriod,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var bodyValue = _bodyAverage.Process(new DecimalIndicatorValue(_bodyAverage, body, candle.OpenTime));
		decimal? currentBodyAverage = null;

		if (bodyValue is DecimalIndicatorValue { IsFinal: true, Value: var average })
			currentBodyAverage = average;

		if (!stochValue.IsFinal)
		{
			UpdateState(candle, null, currentBodyAverage);
			return;
		}

		var stoch = (StochasticOscillatorValue)stochValue;
		var currentSignal = stoch.D;

		if (IsFormedAndOnlineAndAllowTrading() &&
			_previousCandle is not null &&
			_secondPreviousCandle is not null &&
			_previousBodyAverage is not null &&
			_previousSignal is not null &&
			_secondPreviousSignal is not null)
		{
			var bullishPattern = IsBullishMeetingLines(_secondPreviousCandle, _previousCandle, _previousBodyAverage.Value);
			var bearishPattern = IsBearishMeetingLines(_secondPreviousCandle, _previousCandle, _previousBodyAverage.Value);

			var longEntry = bullishPattern && _previousSignal.Value < LongEntryLevel;
			var shortEntry = bearishPattern && _previousSignal.Value > ShortEntryLevel;

			var exitShort = CrossesUp(_secondPreviousSignal.Value, _previousSignal.Value, ExitLowerLevel) ||
				CrossesUp(_secondPreviousSignal.Value, _previousSignal.Value, ExitUpperLevel);

			var exitLong = CrossesDown(_secondPreviousSignal.Value, _previousSignal.Value, ExitUpperLevel) ||
				CrossesDown(_secondPreviousSignal.Value, _previousSignal.Value, ExitLowerLevel);

			var tradeVolume = Volume > 0m ? Volume : 1m;

			if (exitLong && Position > 0m)
				SellMarket(Position);

			if (exitShort && Position < 0m)
				BuyMarket(Math.Abs(Position));

			if (longEntry && Position <= 0m)
				BuyMarket(tradeVolume + Math.Abs(Position));

			if (shortEntry && Position >= 0m)
				SellMarket(tradeVolume + Position);
		}

		UpdateState(candle, currentSignal, currentBodyAverage);
	}

	private void UpdateState(ICandleMessage candle, decimal? currentSignal, decimal? currentBodyAverage)
	{
		_secondPreviousCandle = _previousCandle;
		_previousCandle = candle;

		_secondPreviousSignal = _previousSignal;
		_previousSignal = currentSignal;

		if (currentBodyAverage.HasValue)
			_previousBodyAverage = currentBodyAverage.Value;
	}

	private static bool IsBullishMeetingLines(ICandleMessage twoAgo, ICandleMessage oneAgo, decimal avgBody)
	{
		if (avgBody <= 0m)
			return false;

		var longBlack = twoAgo.OpenPrice - twoAgo.ClosePrice > avgBody;
		var longWhite = oneAgo.ClosePrice - oneAgo.OpenPrice > avgBody;
		var closesNear = Math.Abs(oneAgo.ClosePrice - twoAgo.ClosePrice) < 0.1m * avgBody;

		return longBlack && longWhite && closesNear;
	}

	private static bool IsBearishMeetingLines(ICandleMessage twoAgo, ICandleMessage oneAgo, decimal avgBody)
	{
		if (avgBody <= 0m)
			return false;

		var longWhite = twoAgo.ClosePrice - twoAgo.OpenPrice > avgBody;
		var longBlack = oneAgo.OpenPrice - oneAgo.ClosePrice > avgBody;
		var closesNear = Math.Abs(oneAgo.ClosePrice - twoAgo.ClosePrice) < 0.1m * avgBody;

		return longWhite && longBlack && closesNear;
	}

	private static bool CrossesUp(decimal previous, decimal current, decimal level)
	{
		return previous < level && current > level;
	}

	private static bool CrossesDown(decimal previous, decimal current, decimal level)
	{
		return previous > level && current < level;
	}
}
