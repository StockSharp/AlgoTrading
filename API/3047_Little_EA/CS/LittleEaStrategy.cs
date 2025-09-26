namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class LittleEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _ohlcBarIndex;
	private readonly StrategyParam<int> _maxPositionsPerSide;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MovingAverageType> _maType;
	private readonly StrategyParam<AppliedPriceType> _appliedPrice;
	private readonly StrategyParam<decimal> _tradeVolume;

	private LengthIndicator<decimal> _movingAverage;
	private readonly List<decimal> _maHistory = new(); // Stores MA values to support the shift parameter
	private readonly List<ICandleMessage> _candleHistory = new(); // Keeps finished candles for OHLC indexing

	public LittleEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Primary timeframe used for signals.", "General");

		_ohlcBarIndex = Param(nameof(OhlcBarIndex), 1)
			.SetNotNegative()
			.SetDisplay("OHLC bar index", "Historical bar index used for MA crossover detection.", "Signals");

		_maxPositionsPerSide = Param(nameof(MaxPositionsPerSide), 15)
			.SetNotNegative()
			.SetDisplay("Max positions", "Maximum cumulative positions per direction.", "Risk");

		_maPeriod = Param(nameof(MaPeriod), 64)
			.SetGreaterThanZero()
			.SetDisplay("MA period", "Moving average length.", "Indicator");

		_maShift = Param(nameof(MaShift), 0)
			.SetNotNegative()
			.SetDisplay("MA shift", "Number of bars to shift the moving average.", "Indicator");

		_maType = Param(nameof(MaType), MovingAverageType.Smoothed)
			.SetDisplay("MA type", "Moving average calculation mode.", "Indicator");

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPriceType.Close)
			.SetDisplay("Applied price", "Price source fed into the moving average.", "Indicator");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade volume", "Order volume used for each entry.", "Risk");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int OhlcBarIndex
	{
		get => _ohlcBarIndex.Value;
		set => _ohlcBarIndex.Value = value;
	}

	public int MaxPositionsPerSide
	{
		get => _maxPositionsPerSide.Value;
		set => _maxPositionsPerSide.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	public MovingAverageType MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	public AppliedPriceType AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume; // Align default volume with the configured trade size

		_movingAverage = CreateMovingAverage(MaType, MaPeriod);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (_movingAverage != null)
				DrawIndicator(area, _movingAverage);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_movingAverage == null)
			return;

		var price = GetPrice(candle, AppliedPrice);
		var maValue = _movingAverage.Process(price, candle.OpenTime, true).ToDecimal();

		_candleHistory.Add(candle);
		TrimHistory(_candleHistory, GetHistoryLimit());

		if (!_movingAverage.IsFormed)
			return;

		_maHistory.Add(maValue);
		TrimHistory(_maHistory, GetHistoryLimit());

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_maHistory.Count <= MaShift)
			return;

		if (_candleHistory.Count <= OhlcBarIndex)
			return;

		var maIndex = _maHistory.Count - 1 - MaShift;
		var referenceMa = _maHistory[maIndex];

		var candleIndex = _candleHistory.Count - 1 - OhlcBarIndex;
		var referenceCandle = _candleHistory[candleIndex];

		// Detect a close above or below the shifted moving average
		var crossUp = referenceCandle.OpenPrice < referenceMa && referenceCandle.ClosePrice > referenceMa;
		var crossDown = referenceCandle.OpenPrice > referenceMa && referenceCandle.ClosePrice < referenceMa;

		if (TradeVolume <= 0m)
			return;

		var longExposure = Math.Max(Position, 0m); // Net long quantity in lots
		var shortExposure = Math.Max(-Position, 0m); // Net short quantity in lots
		var exposureLimit = TradeVolume * MaxPositionsPerSide;

		if (crossDown && longExposure >= exposureLimit && longExposure > 0m)
		{
			// Reverse signal when long positions already reached the maximum limit
			SellMarket(longExposure);
			return;
		}

		if (crossUp && shortExposure >= exposureLimit && shortExposure > 0m)
		{
			// Reverse signal when short positions already reached the maximum limit
			BuyMarket(shortExposure);
			return;
		}

		if (crossUp && longExposure < exposureLimit)
		{
			// Add a new long tranche while staying below the configured cap
			BuyMarket(TradeVolume);
		}
		else if (crossDown && shortExposure < exposureLimit)
		{
			// Add a new short tranche while staying below the configured cap
			SellMarket(TradeVolume);
		}
	}

	private int GetHistoryLimit()
	{
		var shift = Math.Max(MaShift, 0);
		var bars = Math.Max(OhlcBarIndex, 0);
		return Math.Max(shift, bars) + 5; // Keep a small buffer to avoid frequent reallocations
	}

	private static void TrimHistory<T>(IList<T> list, int limit)
	{
		if (list.Count <= limit)
			return;

		var removeCount = list.Count - limit;
		for (var i = 0; i < removeCount; i++)
		{
			list.RemoveAt(0);
		}
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageType type, int length)
	{
		return type switch
		{
			MovingAverageType.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageType.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageType.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageType.Weighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}

	private static decimal GetPrice(ICandleMessage candle, AppliedPriceType priceType)
	{
		return priceType switch
		{
			AppliedPriceType.Open => candle.OpenPrice,
			AppliedPriceType.High => candle.HighPrice,
			AppliedPriceType.Low => candle.LowPrice,
			AppliedPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceType.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceType.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}
}

public enum MovingAverageType
{
	Simple,
	Exponential,
	Smoothed,
	Weighted
}

public enum AppliedPriceType
{
	Close,
	Open,
	High,
	Low,
	Median,
	Typical,
	Weighted
}
