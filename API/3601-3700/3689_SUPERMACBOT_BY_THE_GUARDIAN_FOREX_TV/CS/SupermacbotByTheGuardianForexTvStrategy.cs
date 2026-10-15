using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SUPERMACBOT strategy converted from the MQL expert by The Guardian Forex TV.
/// </summary>
public class SupermacbotByTheGuardianForexTvStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _histogramThreshold;
	private readonly StrategyParam<int> _trailingPeriod;

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast simple moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow simple moving average period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA period for MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Minimal absolute histogram value required for new entries.
	/// </summary>
	public decimal HistogramThreshold
	{
		get => _histogramThreshold.Value;
		set => _histogramThreshold.Value = value;
	}

	/// <summary>
	/// Trailing simple moving average period.
	/// </summary>
	public int TrailingPeriod
	{
		get => _trailingPeriod.Value;
		set => _trailingPeriod.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SupermacbotByTheGuardianForexTvStrategy"/>.
	/// </summary>
	public SupermacbotByTheGuardianForexTvStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
			
			.SetOptimize(5, 30, 1);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Slow SMA period", "Indicators")
			
			.SetOptimize(10, 60, 1);

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "MACD fast EMA period", "Indicators")
			
			.SetOptimize(5, 20, 1);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 24)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow EMA period", "Indicators")
			
			.SetOptimize(18, 40, 1);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "MACD signal EMA period", "Indicators")
			
			.SetOptimize(3, 15, 1);

		_histogramThreshold = Param(nameof(HistogramThreshold), 0m)
			.SetDisplay("Histogram Threshold", "Required MACD histogram magnitude", "Logic");

		_trailingPeriod = Param(nameof(TrailingPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Trailing SMA", "Trailing SMA period", "Logic")
			
			.SetOptimize(6, 30, 1);
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
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastMa = new SimpleMovingAverage { Length = FastMaPeriod };
		var slowMa = new SimpleMovingAverage { Length = SlowMaPeriod };
		var trailingMa = new SimpleMovingAverage { Length = TrailingPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, trailingMa, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastMa, decimal slowMa, decimal trailingMa)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position != 0)
			return;

		var bullishTrend = fastMa > slowMa;
		var bearishTrend = fastMa < slowMa;
		var price = candle.ClosePrice;

		if (bullishTrend && price > trailingMa)
			BuyMarket();
		else if (bearishTrend && price < trailingMa)
			SellMarket();
	}
}

