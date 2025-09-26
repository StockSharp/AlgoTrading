using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Randomized "Pinball Machine" strategy translated from MetaTrader.
/// Opens market trades whenever two independent random draws return the same value.
/// </summary>
public class PinballMachineStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _randomMaxValue;
	private readonly StrategyParam<int> _minStopLossPoints;
	private readonly StrategyParam<int> _maxStopLossPoints;
	private readonly StrategyParam<int> _minTakeProfitPoints;
	private readonly StrategyParam<int> _maxTakeProfitPoints;
	private readonly StrategyParam<int> _randomSeed;

	private Random _random;

	/// <summary>
	/// Trade size used for every random entry.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type that triggers each pseudo-random evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Upper bound (inclusive) for the random draws.
	/// </summary>
	public int RandomMaxValue
	{
		get => _randomMaxValue.Value;
		set => _randomMaxValue.Value = value;
	}

	/// <summary>
	/// Lower bound for stop-loss distance in price steps.
	/// </summary>
	public int MinStopLossPoints
	{
		get => _minStopLossPoints.Value;
		set => _minStopLossPoints.Value = value;
	}

	/// <summary>
	/// Upper bound for stop-loss distance in price steps.
	/// </summary>
	public int MaxStopLossPoints
	{
		get => _maxStopLossPoints.Value;
		set => _maxStopLossPoints.Value = value;
	}

	/// <summary>
	/// Lower bound for take-profit distance in price steps.
	/// </summary>
	public int MinTakeProfitPoints
	{
		get => _minTakeProfitPoints.Value;
		set => _minTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Upper bound for take-profit distance in price steps.
	/// </summary>
	public int MaxTakeProfitPoints
	{
		get => _maxTakeProfitPoints.Value;
		set => _maxTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Seed for the pseudo-random number generator.
	/// </summary>
	public int RandomSeed
	{
		get => _randomSeed.Value;
		set => _randomSeed.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PinballMachineStrategy"/> class.
	/// </summary>
	public PinballMachineStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order size for each random entry", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to trigger random draws", "Data");

		_randomMaxValue = Param(nameof(RandomMaxValue), 100)
			.SetGreaterOrEqual(1)
			.SetDisplay("Random Max Value", "Upper bound (inclusive) for random draws", "Randomization");

		_minStopLossPoints = Param(nameof(MinStopLossPoints), 100)
			.SetNotNegative()
			.SetDisplay("Min Stop Loss (points)", "Minimum distance for stop-loss in price steps", "Risk");

		_maxStopLossPoints = Param(nameof(MaxStopLossPoints), 1000)
			.SetNotNegative()
			.SetDisplay("Max Stop Loss (points)", "Maximum distance for stop-loss in price steps", "Risk");

		_minTakeProfitPoints = Param(nameof(MinTakeProfitPoints), 100)
			.SetNotNegative()
			.SetDisplay("Min Take Profit (points)", "Minimum distance for take-profit in price steps", "Risk");

		_maxTakeProfitPoints = Param(nameof(MaxTakeProfitPoints), 1000)
			.SetNotNegative()
			.SetDisplay("Max Take Profit (points)", "Maximum distance for take-profit in price steps", "Risk");

		_randomSeed = Param(nameof(RandomSeed), 0)
			.SetDisplay("Random Seed", "Seed for the pseudo-random generator (0 = time-based)", "Randomization");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_random = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize the pseudo-random generator either from a fixed seed or from the current time.
		_random = RandomSeed == 0
			? new Random(Environment.TickCount ^ GetHashCode())
			: new Random(RandomSeed);

		StartProtection();

		// Subscribe to candle data to trigger the random decision logic on every finished bar.
		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var random = _random;
		if (random == null)
			return;

		// Draw four random integers in the inclusive range [0, RandomMaxValue].
		var maxValue = RandomMaxValue;
		if (maxValue < 0)
			maxValue = 0;

		var first = random.Next(maxValue + 1);
		var second = random.Next(maxValue + 1);
		var third = random.Next(maxValue + 1);
		var fourth = random.Next(maxValue + 1);

		// Generate the shared stop-loss and take-profit distances for this evaluation cycle.
		var stopLossPoints = NextPoints(random, MinStopLossPoints, MaxStopLossPoints);
		var takeProfitPoints = NextPoints(random, MinTakeProfitPoints, MaxTakeProfitPoints);

		if (first == second)
			TryEnterLong(candle.ClosePrice, stopLossPoints, takeProfitPoints);

		if (third == fourth)
			TryEnterShort(candle.ClosePrice, stopLossPoints, takeProfitPoints);
	}

	private void TryEnterLong(decimal referencePrice, int stopLossPoints, int takeProfitPoints)
	{
		var volume = TradeVolume;
		if (volume <= 0m)
			return;

		// Remember the current net position before submitting the market order.
		var currentPosition = Position;
		BuyMarket(volume);
		var resultingPosition = currentPosition + volume;

		// Attach take-profit and stop-loss in terms of price points if they are enabled.
		if (takeProfitPoints > 0)
			SetTakeProfit(takeProfitPoints, referencePrice, resultingPosition);

		if (stopLossPoints > 0)
			SetStopLoss(stopLossPoints, referencePrice, resultingPosition);
	}

	private void TryEnterShort(decimal referencePrice, int stopLossPoints, int takeProfitPoints)
	{
		var volume = TradeVolume;
		if (volume <= 0m)
			return;

		var currentPosition = Position;
		SellMarket(volume);
		var resultingPosition = currentPosition - volume;

		if (takeProfitPoints > 0)
			SetTakeProfit(takeProfitPoints, referencePrice, resultingPosition);

		if (stopLossPoints > 0)
			SetStopLoss(stopLossPoints, referencePrice, resultingPosition);
	}

	private static int NextPoints(Random random, int minPoints, int maxPoints)
	{
		if (maxPoints < minPoints)
			(minPoints, maxPoints) = (maxPoints, minPoints);

		if (maxPoints <= 0)
			return 0;

		if (minPoints < 0)
			minPoints = 0;

		return random.Next(minPoints, maxPoints + 1);
	}
}
