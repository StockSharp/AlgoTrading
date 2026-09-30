using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Lottery-style strategy that makes two independent random entry decisions per candle.
/// Each entry receives random stop-loss and take-profit distances measured in price steps.
/// </summary>
public class PinballMachineRandomDrawStrategy : Strategy
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
	private decimal _protectedPosition;
	private ProtectedPosition _protection;

	public PinballMachineRandomDrawStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Volume submitted for every matching pair.", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles that trigger random draws.", "General");

		_randomMaxValue = Param(nameof(RandomMaxValue), 99)
			.SetRange(0, 1_000_000)
			.SetDisplay("Random Maximum", "Inclusive upper bound for entry draws.", "Random");

		_minStopLossPoints = Param(nameof(MinStopLossPoints), 10)
			.SetDisplay("Minimum Stop Loss", "Minimum random stop-loss distance in price steps.", "Protection");

		_maxStopLossPoints = Param(nameof(MaxStopLossPoints), 50)
			.SetDisplay("Maximum Stop Loss", "Maximum random stop-loss distance in price steps.", "Protection");

		_minTakeProfitPoints = Param(nameof(MinTakeProfitPoints), 10)
			.SetDisplay("Minimum Take Profit", "Minimum random take-profit distance in price steps.", "Protection");

		_maxTakeProfitPoints = Param(nameof(MaxTakeProfitPoints), 100)
			.SetDisplay("Maximum Take Profit", "Maximum random take-profit distance in price steps.", "Protection");

		_randomSeed = Param(nameof(RandomSeed), 0)
			.SetDisplay("Random Seed", "Zero uses a time-based sequence; any other value is reproducible.", "Random");
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

	public int RandomMaxValue
	{
		get => _randomMaxValue.Value;
		set => _randomMaxValue.Value = value;
	}

	public int MinStopLossPoints
	{
		get => _minStopLossPoints.Value;
		set => _minStopLossPoints.Value = value;
	}

	public int MaxStopLossPoints
	{
		get => _maxStopLossPoints.Value;
		set => _maxStopLossPoints.Value = value;
	}

	public int MinTakeProfitPoints
	{
		get => _minTakeProfitPoints.Value;
		set => _minTakeProfitPoints.Value = value;
	}

	public int MaxTakeProfitPoints
	{
		get => _maxTakeProfitPoints.Value;
		set => _maxTakeProfitPoints.Value = value;
	}

	public int RandomSeed
	{
		get => _randomSeed.Value;
		set => _randomSeed.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_random = null;
		_protectedPosition = 0m;
		_protection = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_random = RandomSeed == 0 ? new Random() : new Random(RandomSeed);
		StartProtectionBook();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ProcessProtection(candle);

		var draw = DrawEvaluation(
			_random,
			RandomMaxValue,
			MinStopLossPoints,
			MaxStopLossPoints,
			MinTakeProfitPoints,
			MaxTakeProfitPoints);

		if (draw.EnterLong)
			SubmitEntry(Sides.Buy, candle, draw.StopLossPoints, draw.TakeProfitPoints);

		if (draw.EnterShort)
			SubmitEntry(Sides.Sell, candle, draw.StopLossPoints, draw.TakeProfitPoints);
	}

	private void SubmitEntry(Sides side, ICandleMessage candle, int stopLossPoints, int takeProfitPoints)
	{
		if (TradeVolume <= 0m)
			return;

		SubmitMarket(side, TradeVolume, "Pinball entry");
		_protectedPosition += side == Sides.Buy ? TradeVolume : -TradeVolume;

		if (_protectedPosition == 0m)
		{
			_protection = null;
			return;
		}

		var priceStep = Security?.PriceStep;
		if (priceStep is not decimal step || step <= 0m || (stopLossPoints <= 0 && takeProfitPoints <= 0))
		{
			_protection = null;
			return;
		}

		var protection = new ProtectedPosition(
			_protectedPosition > 0m ? Sides.Buy : Sides.Sell,
			Math.Abs(_protectedPosition),
			candle.CloseTime);
		SetStopLoss(protection, candle.ClosePrice, stopLossPoints, step);
		SetTakeProfit(protection, candle.ClosePrice, takeProfitPoints, step);
		_protection = protection;
	}

	private void ProcessProtection(ICandleMessage candle)
	{
		var protection = _protection;
		if (protection is null || candle.CloseTime <= protection.ActiveAfter)
			return;

		var stopHit = protection.StopLossPrice is decimal stop &&
			(protection.Side == Sides.Buy ? candle.LowPrice <= stop : candle.HighPrice >= stop);
		var takeHit = !stopHit && protection.TakeProfitPrice is decimal take &&
			(protection.Side == Sides.Buy ? candle.HighPrice >= take : candle.LowPrice <= take);

		if (!stopHit && !takeHit)
			return;

		var exitSide = protection.Side == Sides.Buy ? Sides.Sell : Sides.Buy;
		SubmitMarket(exitSide, protection.Volume, "Pinball protection exit");
		_protectedPosition = 0m;
		_protection = null;
	}

	private void SubmitMarket(Sides side, decimal volume, string comment)
	{
		var order = CreateOrder(side, 0m, volume);
		order.Comment = comment;
		RegisterOrder(order);
	}

	private void StartProtectionBook()
	{
		_protectedPosition = Position;
		_protection = null;
	}

	private static void SetStopLoss(ProtectedPosition protection, decimal entryPrice, int points, decimal priceStep)
	{
		if (points <= 0)
			return;

		var distance = points * priceStep;
		protection.StopLossPrice = protection.Side == Sides.Buy
			? entryPrice - distance
			: entryPrice + distance;
	}

	private static void SetTakeProfit(ProtectedPosition protection, decimal entryPrice, int points, decimal priceStep)
	{
		if (points <= 0)
			return;

		var distance = points * priceStep;
		protection.TakeProfitPrice = protection.Side == Sides.Buy
			? entryPrice + distance
			: entryPrice - distance;
	}

	internal static Evaluation DrawEvaluation(
		Random random,
		int randomMaxValue,
		int minStopLossPoints,
		int maxStopLossPoints,
		int minTakeProfitPoints,
		int maxTakeProfitPoints)
	{
		ArgumentNullException.ThrowIfNull(random);

		if (randomMaxValue < 0)
			throw new ArgumentOutOfRangeException(nameof(randomMaxValue));

		int drawEntry() => randomMaxValue == int.MaxValue
			? (int)random.NextInt64((long)int.MaxValue + 1L)
			: random.Next(randomMaxValue + 1);

		return new(
			drawEntry(),
			drawEntry(),
			drawEntry(),
			drawEntry(),
			DrawDistance(random, minStopLossPoints, maxStopLossPoints),
			DrawDistance(random, minTakeProfitPoints, maxTakeProfitPoints));
	}

	private static int DrawDistance(Random random, int minimum, int maximum)
	{
		if (minimum <= 0 || maximum <= 0 || minimum > maximum)
			return 0;

		return maximum == int.MaxValue
			? (int)random.NextInt64(minimum, (long)maximum + 1L)
			: random.Next(minimum, maximum + 1);
	}

	internal readonly record struct Evaluation(
		int LongFirst,
		int LongSecond,
		int ShortFirst,
		int ShortSecond,
		int StopLossPoints,
		int TakeProfitPoints)
	{
		public bool EnterLong => LongFirst == LongSecond;
		public bool EnterShort => ShortFirst == ShortSecond;
	}

	private sealed class ProtectedPosition(Sides side, decimal volume, DateTime activeAfter)
	{
		public Sides Side { get; } = side;
		public decimal Volume { get; } = volume;
		public DateTime ActiveAfter { get; } = activeAfter;
		public decimal? StopLossPrice { get; set; }
		public decimal? TakeProfitPrice { get; set; }
	}
}
