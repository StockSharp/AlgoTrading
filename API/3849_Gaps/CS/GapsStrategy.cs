using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "Gaps" MetaTrader strategy that opens trades when a new 15-minute candle gaps away from the previous range.
/// The approach mirrors the original logic by comparing the new session open against the prior candle extremes plus the spread buffer.
/// </summary>
public class GapsStrategy : Strategy
{
	private readonly StrategyParam<int> _minGapSize;
	private readonly StrategyParam<decimal> _gapVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousHigh;
	private decimal _previousLow;
	private bool _hasPreviousCandle;
	private DateTimeOffset? _lastOrderTime;

	/// <summary>
	/// Minimum size of the gap expressed in price steps.
	/// </summary>
	public int MinGapSize
	{
		get => _minGapSize.Value;
		set => _minGapSize.Value = value;
	}

	/// <summary>
	/// Market order volume used when entering trades.
	/// </summary>
	public decimal GapVolume
	{
		get => _gapVolume.Value;
		set => _gapVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for price analysis (15-minute candles by default).
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="GapsStrategy"/> class.
	/// </summary>
	public GapsStrategy()
	{
		_minGapSize = Param(nameof(MinGapSize), 1)
			.SetRange(0, 20)
			.SetDisplay("Min Gap Size", "Minimum gap size measured in price steps", "Trading Rules")
			.SetCanOptimize(true);

		_gapVolume = Param(nameof(GapVolume), 0.1m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Gap Volume", "Order volume for gap entries", "Trading Rules")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle source for the strategy", "Data");
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

		_previousHigh = 0m;
		_previousLow = 0m;
		_hasPreviousCandle = false;
		_lastOrderTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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

		if (!_hasPreviousCandle)
		{
			// Store the very first finished candle to compare against the next one.
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			_hasPreviousCandle = true;
			return;
		}

		var openTime = candle.OpenTime;
		if (openTime == default)
			return;

		var gapBuffer = CalculateGapBuffer();
		var upperTrigger = _previousHigh + gapBuffer;
		var lowerTrigger = _previousLow - gapBuffer;

		var openPrice = candle.OpenPrice;
		var hasOrderThisCandle = _lastOrderTime == openTime;

		// Check for bullish gap below the previous low.
		if (!hasOrderThisCandle && openPrice < lowerTrigger)
		{
			EnterLong(openTime);
			hasOrderThisCandle = true;
		}

		// Check for bearish gap above the previous high.
		if (!hasOrderThisCandle && openPrice > upperTrigger)
		{
			EnterShort(openTime);
		}

		// Preserve the current candle as the new reference for the next iteration.
		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
	}

	private void EnterLong(DateTimeOffset openTime)
	{
		var volume = Math.Max(GapVolume, 0m);
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		_lastOrderTime = openTime;
	}

	private void EnterShort(DateTimeOffset openTime)
	{
		var volume = Math.Max(GapVolume, 0m);
		if (volume <= 0m)
			return;

		SellMarket(volume);
		_lastOrderTime = openTime;
	}

	private decimal CalculateGapBuffer()
	{
		var pointValue = ResolvePointValue();
		var minGapSteps = Math.Max(MinGapSize, 0);
		var spread = GetSpreadPrice();

		if (pointValue > 0m)
		{
			var spreadSteps = spread > 0m ? spread / pointValue : 0m;
			var totalSteps = (decimal)minGapSteps + spreadSteps;
			return totalSteps * pointValue;
		}

		return minGapSteps + Math.Max(spread, 0m);
	}

	private decimal ResolvePointValue()
	{
		var step = Security.MinPriceStep ?? 0m;
		if (step > 0m)
			return step;

		step = Security.PriceStep ?? 0m;
		if (step > 0m)
			return step;

		return 0m;
	}

	private decimal GetSpreadPrice()
	{
		var bestAsk = Security.BestAsk?.Price;
		var bestBid = Security.BestBid?.Price;

		if (bestAsk is decimal ask && bestBid is decimal bid && ask >= bid && ask > 0m && bid > 0m)
			return ask - bid;

		var step = ResolvePointValue();
		if (step > 0m)
			return step;

		return 0m;
	}
}
