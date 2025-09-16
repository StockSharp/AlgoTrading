using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple MACD slope-following strategy converted from MQL5 Simple_MACD.mq5.
/// The strategy evaluates the MACD main line on completed candles and builds positions accordingly.
/// </summary>
public class SimpleMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergence _macd = null!;
	private decimal? _previousMacdValue;
	private decimal? _prePreviousMacdValue;

	/// <summary>
	/// Fast EMA period used for the MACD main line.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period used for the MACD main line.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA period used by the MACD indicator.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Trading volume applied when new orders are sent.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type used to feed the MACD indicator.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize Simple MACD strategy with default parameters.
	/// </summary>
	public SimpleMacdStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetDisplay("MACD Fast Period", "Fast EMA length for MACD calculation", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(6, 18, 2);

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetDisplay("MACD Slow Period", "Slow EMA length for MACD calculation", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 2);

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetDisplay("MACD Signal Period", "Signal EMA length maintained for compatibility", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(6, 18, 1);

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Trade Volume", "Order volume used for each signal", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for MACD calculations", "General");
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

		_previousMacdValue = null;
		_prePreviousMacdValue = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Configure MACD indicator to match the source MQL strategy settings.
		_macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = FastPeriod,
			LongPeriod = SlowPeriod,
			SignalPeriod = SignalPeriod
		};

		// Subscribe to candle data and bind the MACD indicator.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, ProcessCandle)
			.Start();

		// Prepare visual elements when charts are available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		// React only to completed candles to avoid premature decisions.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure the indicator produced a valid value.
		if (!_macd.IsFormed || !macdValue.IsFinal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var macdData = (MovingAverageConvergenceDivergenceValue)macdValue;
		var macdLine = macdData.Macd;

		// Accumulate historical MACD values for slope calculations.
		if (_previousMacdValue is null)
		{
			_previousMacdValue = macdLine;
			return;
		}

		if (_prePreviousMacdValue is null)
		{
			_prePreviousMacdValue = _previousMacdValue;
			_previousMacdValue = macdLine;
			return;
		}

		var macdPrev = _previousMacdValue.Value;
		var macdPrevPrev = _prePreviousMacdValue.Value;

		var slopeUp = macdPrev > macdPrevPrev;
		var slopeDown = macdPrev < macdPrevPrev;

		if (slopeUp)
		{
			// Close shorts and open (or add to) longs when the MACD slope turns positive.
			var volumeToBuy = TradeVolume + Math.Max(0m, -Position);
			if (volumeToBuy > 0m)
			{
				BuyMarket(volumeToBuy);
				LogInfo($"Bullish slope detected. MACD(1)={macdPrev:F5}, MACD(2)={macdPrevPrev:F5}. Buying {volumeToBuy}.");
			}
		}
		else if (slopeDown)
		{
			// Close longs and open (or add to) shorts when the MACD slope turns negative.
			var volumeToSell = TradeVolume + Math.Max(0m, Position);
			if (volumeToSell > 0m)
			{
				SellMarket(volumeToSell);
				LogInfo($"Bearish slope detected. MACD(1)={macdPrev:F5}, MACD(2)={macdPrevPrev:F5}. Selling {volumeToSell}.");
			}
		}

		// Update stored values so the next candle compares the two previous MACD readings.
		_prePreviousMacdValue = _previousMacdValue;
		_previousMacdValue = macdLine;
	}
}
