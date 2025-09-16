
using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Engulfing pattern strategy that reacts to bullish and bearish engulfing candles.
/// </summary>
public class BullishBearishEngulfingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _shift;
	private readonly StrategyParam<decimal> _distanceInPips;
	private readonly StrategyParam<bool> _closeOpposite;
	private readonly StrategyParam<Sides> _bullishSide;
	private readonly StrategyParam<Sides> _bearishSide;
	private readonly StrategyParam<decimal> _volume;

	private readonly Queue<CandleSnapshot> _candles = new();

	/// <summary>
	/// Initializes a new instance of <see cref="BullishBearishEngulfingStrategy"/>.
	/// </summary>
	public BullishBearishEngulfingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for analysis", "General");

		_shift = Param(nameof(Shift), 1)
			.SetGreaterThanZero()
			.SetDisplay("Shift", "Number of completed candles to skip", "Pattern")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_distanceInPips = Param(nameof(DistanceInPips), 0m)
			.SetNotNegative()
			.SetDisplay("Distance (pips)", "Additional filter expressed in pips", "Pattern")
			.SetCanOptimize(true)
			.SetOptimize(0m, 10m, 1m);

		_closeOpposite = Param(nameof(CloseOppositePositions), true)
			.SetDisplay("Close Opposite", "Close opposite position before entering", "Risk");

		_bullishSide = Param(nameof(BullishSide), Sides.Buy)
			.SetDisplay("Bullish Action", "Order side for bullish engulfing", "Pattern");

		_bearishSide = Param(nameof(BearishSide), Sides.Sell)
			.SetDisplay("Bearish Action", "Order side for bearish engulfing", "Pattern");

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Base order volume", "Trading");
	}

	/// <summary>
	/// Candle type used for pattern detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of fully completed candles to skip before pattern evaluation.
	/// </summary>
	public int Shift
	{
		get => _shift.Value;
		set => _shift.Value = value;
	}

	/// <summary>
	/// Additional price filter expressed in pips.
	/// </summary>
	public decimal DistanceInPips
	{
		get => _distanceInPips.Value;
		set => _distanceInPips.Value = value;
	}

	/// <summary>
	/// Indicates whether opposite positions should be closed before entering a new trade.
	/// </summary>
	public bool CloseOppositePositions
	{
		get => _closeOpposite.Value;
		set => _closeOpposite.Value = value;
	}

	/// <summary>
	/// Side used when a bullish engulfing pattern appears.
	/// </summary>
	public Sides BullishSide
	{
		get => _bullishSide.Value;
		set => _bullishSide.Value = value;
	}

	/// <summary>
	/// Side used when a bearish engulfing pattern appears.
	/// </summary>
	public Sides BearishSide
	{
		get => _bearishSide.Value;
		set => _bearishSide.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_candles.Clear();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var snapshot = new CandleSnapshot
		{
			Open = candle.OpenPrice,
			High = candle.HighPrice,
			Low = candle.LowPrice,
			Close = candle.ClosePrice
		};

		_candles.Enqueue(snapshot);

		var maxCount = Math.Max(Shift + 2, 3);
		while (_candles.Count > maxCount)
			_candles.Dequeue();

		if (_candles.Count < Shift + 1)
			return;

		var candles = _candles.ToArray();
		var currentIndex = candles.Length - Shift;
		if (currentIndex <= 0)
			return;

		var previousIndex = currentIndex - 1;
		if (previousIndex < 0)
			return;

		var current = candles[currentIndex];
		var previous = candles[previousIndex];
		var distance = CalculateDistanceInPrice();

		var isBullishEngulfing = current.Close > current.Open && previous.Open > previous.Close &&
			current.High > previous.High + distance &&
			current.Close > previous.Open + distance &&
			current.Open < previous.Close - distance &&
			current.Low < previous.Low - distance;

		if (isBullishEngulfing)
		{
			HandleSignal(BullishSide);
			return;
		}

		var isBearishEngulfing = current.Open > current.Close && previous.Open < previous.Close &&
			current.High > previous.High + distance &&
			current.Open > previous.Close + distance &&
			current.Close < previous.Open - distance &&
			current.Low < previous.Low - distance;

		if (isBearishEngulfing)
			HandleSignal(BearishSide);
	}

	private void HandleSignal(Sides side)
	{
		switch (side)
		{
			case Sides.Buy:
				EnterLong();
				break;
			case Sides.Sell:
				EnterShort();
				break;
		}
	}

	private void EnterLong()
	{
		if (Position > 0)
			return;

		var volume = Volume;
		if (Position < 0)
		{
			if (!CloseOppositePositions)
				return;

			volume += Math.Abs(Position);
		}

		if (volume > 0m)
			BuyMarket(volume);
	}

	private void EnterShort()
	{
		if (Position < 0)
			return;

		var volume = Volume;
		if (Position > 0)
		{
			if (!CloseOppositePositions)
				return;

			volume += Math.Abs(Position);
		}

		if (volume > 0m)
			SellMarket(volume);
	}

	private decimal CalculateDistanceInPrice()
	{
		var priceStep = Security?.PriceStep;
		if (priceStep == null)
			return 0m;

		var decimals = Security?.Decimals ?? 0;
		var multiplier = decimals is 3 or 5 ? 10m : 1m;
		return DistanceInPips * priceStep.Value * multiplier;
	}

	private struct CandleSnapshot
	{
		public decimal Open;
		public decimal High;
		public decimal Low;
		public decimal Close;
	}
}
