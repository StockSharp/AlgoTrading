using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on moving averages and RSI crossover.
/// Goes long when both fast EMA and RSI exceed their slow counterparts, and short in the opposite case.
/// </summary>
public class MaRsiTriggerStrategy : Strategy
{
	private readonly StrategyParam<int> _fastRsiPeriod;
	private readonly StrategyParam<int> _slowRsiPeriod;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<bool> _allowBuyEntry;
	private readonly StrategyParam<bool> _allowSellEntry;
	private readonly StrategyParam<bool> _allowLongExit;
	private readonly StrategyParam<bool> _allowShortExit;
	private readonly StrategyParam<DataType> _candleType;

	private int _previousTrend;

	/// <summary>
	/// Fast RSI period.
	/// </summary>
	public int FastRsiPeriod { get => _fastRsiPeriod.Value; set => _fastRsiPeriod.Value = value; }

	/// <summary>
	/// Slow RSI period.
	/// </summary>
	public int SlowRsiPeriod { get => _slowRsiPeriod.Value; set => _slowRsiPeriod.Value = value; }

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastMaPeriod { get => _fastMaPeriod.Value; set => _fastMaPeriod.Value = value; }

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowMaPeriod { get => _slowMaPeriod.Value; set => _slowMaPeriod.Value = value; }

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool AllowBuyEntry { get => _allowBuyEntry.Value; set => _allowBuyEntry.Value = value; }

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool AllowSellEntry { get => _allowSellEntry.Value; set => _allowSellEntry.Value = value; }

	/// <summary>
	/// Allow closing long positions when trend becomes bearish.
	/// </summary>
	public bool AllowLongExit { get => _allowLongExit.Value; set => _allowLongExit.Value = value; }

	/// <summary>
	/// Allow closing short positions when trend becomes bullish.
	/// </summary>
	public bool AllowShortExit { get => _allowShortExit.Value; set => _allowShortExit.Value = value; }

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public MaRsiTriggerStrategy()
	{
		_fastRsiPeriod = Param(nameof(FastRsiPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast RSI Period", "Period of the fast RSI", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);

		_slowRsiPeriod = Param(nameof(SlowRsiPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Slow RSI Period", "Period of the slow RSI", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 1);

		_fastMaPeriod = Param(nameof(FastMaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Period", "Period of the fast EMA", "MA")
			.SetCanOptimize(true)
			.SetOptimize(3, 15, 1);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Period", "Period of the slow EMA", "MA")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_allowBuyEntry = Param(nameof(AllowBuyEntry), true)
			.SetDisplay("Allow Buy Entry", "Enable entering long positions", "General");

		_allowSellEntry = Param(nameof(AllowSellEntry), true)
			.SetDisplay("Allow Sell Entry", "Enable entering short positions", "General");

		_allowLongExit = Param(nameof(AllowLongExit), true)
			.SetDisplay("Allow Long Exit", "Enable exiting long positions", "General");

		_allowShortExit = Param(nameof(AllowShortExit), true)
			.SetDisplay("Allow Short Exit", "Enable exiting short positions", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_previousTrend = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastRsi = new RSI { Length = FastRsiPeriod };
		var slowRsi = new RSI { Length = SlowRsiPeriod };
		var fastMa = new EMA { Length = FastMaPeriod };
		var slowMa = new EMA { Length = SlowMaPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(fastRsi, slowRsi, fastMa, slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawIndicator(area, fastRsi);
			DrawIndicator(area, slowRsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastRsiValue, decimal slowRsiValue, decimal fastMaValue, decimal slowMaValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure strategy is ready for trading
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var trend = 0;

		if (fastMaValue > slowMaValue)
			trend++;
		else if (fastMaValue < slowMaValue)
			trend--;

		if (fastRsiValue > slowRsiValue)
			trend++;
		else if (fastRsiValue < slowRsiValue)
			trend--;

		if (_previousTrend < 0 && trend > 0)
		{
			// Trend turned bullish
			if (AllowShortExit && Position < 0)
				BuyMarket(Math.Abs(Position));

			if (AllowBuyEntry && Position <= 0)
				BuyMarket(Volume);
		}
		else if (_previousTrend > 0 && trend < 0)
		{
			// Trend turned bearish
			if (AllowLongExit && Position > 0)
				SellMarket(Position);

			if (AllowSellEntry && Position >= 0)
				SellMarket(Volume);
		}

		_previousTrend = trend;
	}
}
