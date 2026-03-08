using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on moving averages and RelativeStrengthIndex crossover.
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
	private readonly StrategyParam<decimal> _minRsiSpread;
	private readonly StrategyParam<decimal> _minMaSpreadPercent;
	private readonly StrategyParam<int> _cooldownBars;

	private int _previousTrend;
	private int _cooldownRemaining;

	/// <summary>
	/// Fast RelativeStrengthIndex period.
	/// </summary>
	public int FastRsiPeriod { get => _fastRsiPeriod.Value; set => _fastRsiPeriod.Value = value; }

	/// <summary>
	/// Slow RelativeStrengthIndex period.
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
	/// Minimum spread between fast and slow RelativeStrengthIndex values.
	/// </summary>
	public decimal MinRsiSpread { get => _minRsiSpread.Value; set => _minRsiSpread.Value = value; }

	/// <summary>
	/// Minimum normalized EMA spread.
	/// </summary>
	public decimal MinMaSpreadPercent { get => _minMaSpreadPercent.Value; set => _minMaSpreadPercent.Value = value; }

	/// <summary>
	/// Number of completed candles to wait after a position change.
	/// </summary>
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MaRsiTriggerStrategy"/> class.
	/// </summary>
	public MaRsiTriggerStrategy()
	{
		_fastRsiPeriod = Param(nameof(FastRsiPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast RelativeStrengthIndex Period", "Period of the fast RelativeStrengthIndex", "RelativeStrengthIndex")
			.SetOptimize(2, 10, 1);

		_slowRsiPeriod = Param(nameof(SlowRsiPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Slow RelativeStrengthIndex Period", "Period of the slow RelativeStrengthIndex", "RelativeStrengthIndex")
			.SetOptimize(10, 30, 1);

		_fastMaPeriod = Param(nameof(FastMaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Period", "Period of the fast EMA", "MA")
			.SetOptimize(3, 15, 1);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Period", "Period of the slow EMA", "MA")
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

		_minRsiSpread = Param(nameof(MinRsiSpread), 6m)
			.SetDisplay("Minimum RelativeStrengthIndex Spread", "Minimum spread between fast and slow RelativeStrengthIndex values", "Filters");

		_minMaSpreadPercent = Param(nameof(MinMaSpreadPercent), 0.0025m)
			.SetDisplay("Minimum EMA Spread %", "Minimum normalized spread between fast and slow EMA values", "Filters");

		_cooldownBars = Param(nameof(CooldownBars), 6)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading");
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
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastRsi = new RelativeStrengthIndex { Length = FastRsiPeriod };
		var slowRsi = new RelativeStrengthIndex { Length = SlowRsiPeriod };
		var fastMa = new ExponentialMovingAverage { Length = FastMaPeriod };
		var slowMa = new ExponentialMovingAverage { Length = SlowMaPeriod };

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
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var normalizedMaSpread = slowMaValue != 0m ? Math.Abs(fastMaValue - slowMaValue) / slowMaValue : 0m;
		var rsiSpread = Math.Abs(fastRsiValue - slowRsiValue);
		var trend = 0;

		if (fastMaValue > slowMaValue && normalizedMaSpread >= MinMaSpreadPercent)
			trend++;
		else if (fastMaValue < slowMaValue && normalizedMaSpread >= MinMaSpreadPercent)
			trend--;

		if (fastRsiValue > slowRsiValue && rsiSpread >= MinRsiSpread)
			trend++;
		else if (fastRsiValue < slowRsiValue && rsiSpread >= MinRsiSpread)
			trend--;

		if (_cooldownRemaining == 0)
		{
			if (_previousTrend < 0 && trend > 0)
			{
				if (AllowShortExit && Position < 0)
					BuyMarket();

				if (AllowBuyEntry && Position <= 0)
				{
					BuyMarket();
					_cooldownRemaining = CooldownBars;
				}
			}
			else if (_previousTrend > 0 && trend < 0)
			{
				if (AllowLongExit && Position > 0)
					SellMarket();

				if (AllowSellEntry && Position >= 0)
				{
					SellMarket();
					_cooldownRemaining = CooldownBars;
				}
			}
		}

		if (trend != 0)
			_previousTrend = trend;
	}
}
