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

	private bool _isHistogramInitialized;
	private decimal _prevHistogram;

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
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Slow SMA period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 1);

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "MACD fast EMA period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 24)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow EMA period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(18, 40, 1);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "MACD signal EMA period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 15, 1);

		_histogramThreshold = Param(nameof(HistogramThreshold), 0m)
			.SetDisplay("Histogram Threshold", "Required MACD histogram magnitude", "Logic");

		_trailingPeriod = Param(nameof(TrailingPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Trailing SMA", "Trailing SMA period", "Logic")
			.SetCanOptimize(true)
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

		_isHistogramInitialized = false;
		_prevHistogram = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdFastPeriod,
			LongPeriod = MacdSlowPeriod,
			SignalPeriod = MacdSignalPeriod
		};

		var fastMa = new SimpleMovingAverage { Length = FastMaPeriod };
		var slowMa = new SimpleMovingAverage { Length = SlowMaPeriod };
		var trailingMa = new SimpleMovingAverage { Length = TrailingPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(macd, fastMa, slowMa, trailingMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);

			var macdArea = CreateChartArea();
			DrawIndicator(macdArea, macd);

			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdLine, decimal signalLine, decimal histogram, decimal fastMa, decimal slowMa, decimal trailingMa)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isHistogramInitialized)
		{
			_prevHistogram = histogram;
			_isHistogramInitialized = true;
			return;
		}

		var threshold = HistogramThreshold;
		var positiveBarrier = threshold <= 0m ? 0m : threshold;
		var negativeBarrier = threshold <= 0m ? 0m : -threshold;

		var bullishHistogram = threshold <= 0m ? histogram > 0m : histogram > threshold;
		var bearishHistogram = threshold <= 0m ? histogram < 0m : histogram < -threshold;

		var histogramCrossUp = _prevHistogram <= positiveBarrier && histogram > positiveBarrier;
		var histogramCrossDown = _prevHistogram >= negativeBarrier && histogram < negativeBarrier;

		var bullishTrend = fastMa > slowMa;
		var bearishTrend = fastMa < slowMa;

		var price = candle.ClosePrice;

		if (Position > 0 && (!bullishHistogram || !bullishTrend || price <= trailingMa))
		{
			SellMarket(Position);
		}
		else if (Position < 0 && (!bearishHistogram || !bearishTrend || price >= trailingMa))
		{
			BuyMarket(-Position);
		}

		if (Position <= 0 && histogramCrossUp && bullishTrend && price > trailingMa && bullishHistogram)
		{
			if (Position < 0)
				BuyMarket(-Position);

			BuyMarket();
		}
		else if (Position >= 0 && histogramCrossDown && bearishTrend && price < trailingMa && bearishHistogram)
		{
			if (Position > 0)
				SellMarket(Position);

			SellMarket();
		}

		_prevHistogram = histogram;
	}
}

