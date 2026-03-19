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
/// MACD with Sentiment Filter strategy.
/// Entry condition:
/// Long: MACD > Signal && Sentiment_Score > Threshold
/// Short: MACD < Signal && Sentiment_Score < -Threshold
/// Exit condition:
/// Long: MACD < Signal
/// Short: MACD > Signal
/// </summary>
public class MacdWithSentimentFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	// Sentiment score from external data source (simplified with simulation for this example)
	private decimal _sentimentScore;
	// Last MACD and Signal values stored from the previous candle
	private decimal _prevMacd;
	private decimal _prevSignal;
	private bool _hasPreviousMacd;
	private int _cooldownRemaining;

	/// <summary>
	/// MACD Fast period.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// MACD Slow period.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD Signal period.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Sentiment threshold for entry signal.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Bars to wait between position changes.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor with default parameters.
	/// </summary>
	public MacdWithSentimentFilterStrategy()
	{
		_macdFast = Param(nameof(MacdFast), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast moving average period for MACD", "MACD Settings")
		
		.SetOptimize(8, 20, 1);

		_macdSlow = Param(nameof(MacdSlow), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow moving average period for MACD", "MACD Settings")
		
		.SetOptimize(20, 34, 2);

		_macdSignal = Param(nameof(MacdSignal), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal line period for MACD", "MACD Settings")
		
		.SetOptimize(5, 13, 1);

		_threshold = Param(nameof(Threshold), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Sentiment Threshold", "Threshold for sentiment filter", "Sentiment Settings")
		
		.SetOptimize(0.2m, 0.8m, 0.1m);

		_cooldownBars = Param(nameof(CooldownBars), 24)
		.SetNotNegative()
		.SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "General");

		_stopLoss = Param(nameof(StopLoss), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (%)", "Stop Loss percentage from entry price", "Risk Management")
		
		.SetOptimize(1m, 3m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		// reset stored values

		_prevMacd = default;
		_prevSignal = default;
		_sentimentScore = default;
		_hasPreviousMacd = default;
		_cooldownRemaining = default;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Create MACD indicator

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};
		var _macdInd = macd;
		// Subscribe to candles
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(c => ProcessCandle(c, _macdInd))
		.Start();

		// Create chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}

		// Enable position protection with stop-loss
		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(StopLoss, UnitTypes.Percent)
		);
	}

	/// <summary>
	/// Process each candle and MACD values.
	/// </summary>
	private void ProcessCandle(ICandleMessage candle, MovingAverageConvergenceDivergenceSignal macdInd)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateSentimentScore(candle);

		var macdResult = macdInd.Process(candle);
		if (!macdInd.IsFormed)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdResult;
		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		if (!_hasPreviousMacd)
		{
			_prevMacd = macd;
			_prevSignal = signal;
			_hasPreviousMacd = true;
			return;
		}

		// Store previous MACD values for state tracking
		var prevMacdOverSignal = _prevMacd > _prevSignal;
		var currMacdOverSignal = macd > signal;

		// Entry conditions with sentiment filter
		if (_cooldownRemaining == 0 && prevMacdOverSignal != currMacdOverSignal && Position == 0)
		{
			if (currMacdOverSignal && _sentimentScore > Threshold)
			{
				BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
			else if (!currMacdOverSignal && _sentimentScore < -Threshold)
			{
				SellMarket();
				_cooldownRemaining = CooldownBars;
			}
		}

		_prevMacd = macd;
		_prevSignal = signal;
	}

	/// <summary>
	/// Update sentiment score based on candle data (simulation).
	/// In a real implementation, this would fetch data from an external source.
	/// </summary>
	private void UpdateSentimentScore(ICandleMessage candle)
	{
		var bodySize = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var totalSize = candle.HighPrice - candle.LowPrice;

		if (totalSize == 0)
			return;

		var bodyRatio = bodySize / totalSize;
		_sentimentScore *= 0.85m;

		// Bullish candle with strong body
		if (candle.ClosePrice > candle.OpenPrice && bodyRatio > 0.7m)
		{
			_sentimentScore = Math.Min(_sentimentScore + 0.25m, 1m);
		}
		// Bearish candle with strong body
		else if (candle.ClosePrice < candle.OpenPrice && bodyRatio > 0.7m)
		{
			_sentimentScore = Math.Max(_sentimentScore - 0.25m, -1m);
		}

		LogInfo($"Updated sentiment score: {_sentimentScore}");
	}
}
