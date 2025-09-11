using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CANX MA crossover strategy.
/// Enters long when fast EMA crosses above slow EMA.
/// Enters short when fast EMA crosses below slow EMA (if long-only disabled).
/// </summary>
public class CanxMaCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _ratio;
	private readonly StrategyParam<bool> _longOnly;
	private readonly StrategyParam<int> _startYear;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Slow EMA multiplier.
	/// </summary>
	public int Ratio
	{
		get => _ratio.Value;
		set => _ratio.Value = value;
	}

	/// <summary>
	/// Enable long only trading.
	/// </summary>
	public bool LongOnly
	{
		get => _longOnly.Value;
		set => _longOnly.Value = value;
	}

	/// <summary>
	/// Year from which trading starts.
	/// </summary>
	public int StartYear
	{
		get => _startYear.Value;
		set => _startYear.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public CanxMaCrossoverStrategy()
	{
		_length = Param(nameof(Length), 17)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Length", "Period of the fast EMA", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 5);

		_ratio = Param(nameof(Ratio), 6)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Slow EMA is fast EMA length times this value", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_longOnly = Param(nameof(LongOnly), false)
			.SetDisplay("Long Only", "Trade only long positions", "Parameters");

		_startYear = Param(nameof(StartYear), 2024)
			.SetDisplay("Start Year", "Ignore data before this year", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(2015, 2025, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "General");
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
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastEma = new ExponentialMovingAverage
		{
			Length = Length,
			CandlePrice = CandlePrice.Median,
		};

		var slowEma = new ExponentialMovingAverage
		{
			Length = Length * Ratio,
			CandlePrice = CandlePrice.Median,
		};

		var subscription = SubscribeCandles(CandleType);

		var startDate = new DateTimeOffset(StartYear, 1, 1, 0, 0, 0, time.Offset);

		var prevFast = 0m;
		var prevSlow = 0m;
		var wasFastBelowSlow = false;
		var isInitialized = false;

		subscription
			.Bind(fastEma, slowEma, (candle, fast, slow) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (candle.OpenTime < startDate)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				if (!isInitialized)
				{
					if (!fastEma.IsFormed || !slowEma.IsFormed)
						return;

					prevFast = fast;
					prevSlow = slow;
					wasFastBelowSlow = fast < slow;
					isInitialized = true;
					return;
				}

				var isFastBelowSlow = fast < slow;

				if (wasFastBelowSlow && !isFastBelowSlow)
				{
					if (Position <= 0)
						BuyMarket(Volume + Math.Abs(Position));
				}
				else if (!wasFastBelowSlow && isFastBelowSlow)
				{
					if (LongOnly)
					{
						if (Position > 0)
							SellMarket(Position);
					}
					else
					{
						if (Position >= 0)
							SellMarket(Volume + Math.Abs(Position));
					}
				}

				wasFastBelowSlow = isFastBelowSlow;
				prevFast = fast;
				prevSlow = slow;
			})
			.Start();

		StartProtection();
	}
}
