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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// StockSharp port of the CyberiaTrader (build 8553) expert advisor.
/// Recreates the probability driven decision engine together with optional MA, MACD, and reversal filters.
/// </summary>
public class CyberiaTraderAiStrategy : Strategy
{
	private readonly StrategyParam<int> _maxPeriod;
	private readonly StrategyParam<int> _samplesPerPeriod;
	private readonly StrategyParam<decimal> _spreadThreshold;
	private readonly StrategyParam<bool> _enableCyberiaLogic;
	private readonly StrategyParam<bool> _enableMacd;
	private readonly StrategyParam<bool> _enableMa;
	private readonly StrategyParam<bool> _enableReversalDetector;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<decimal> _reversalFactor;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private MovingAverageConvergenceDivergenceSignal _macd;
	private ExponentialMovingAverage _ema;

	private readonly Queue<CandleSnapshot> _history = new();
	private decimal? _previousEma;
	private int? _previousPeriod;
	private ModelStats _currentStats;

	/// <summary>
	/// Maximum sampling period evaluated by the probability model.
	/// </summary>
	public int MaxPeriod
	{
		get => _maxPeriod.Value;
		set => _maxPeriod.Value = value;
	}

	/// <summary>
	/// Number of segments (per period) used for statistical evaluation.
	/// </summary>
	public int SamplesPerPeriod
	{
		get => _samplesPerPeriod.Value;
		set => _samplesPerPeriod.Value = value;
	}

	/// <summary>
	/// Minimal absolute move that qualifies as a successful probability outcome.
	/// </summary>
	public decimal SpreadThreshold
	{
		get => _spreadThreshold.Value;
		set => _spreadThreshold.Value = value;
	}

	/// <summary>
	/// Enables the Cyberia probability filter.
	/// </summary>
	public bool EnableCyberiaLogic
	{
		get => _enableCyberiaLogic.Value;
		set => _enableCyberiaLogic.Value = value;
	}

	/// <summary>
	/// Enables the MACD trend filter.
	/// </summary>
	public bool EnableMacd
	{
		get => _enableMacd.Value;
		set => _enableMacd.Value = value;
	}

	/// <summary>
	/// Enables the EMA slope filter.
	/// </summary>
	public bool EnableMa
	{
		get => _enableMa.Value;
		set => _enableMa.Value = value;
	}

	/// <summary>
	/// Enables the reversal detector that flips permissions when extreme spikes appear.
	/// </summary>
	public bool EnableReversalDetector
	{
		get => _enableReversalDetector.Value;
		set => _enableReversalDetector.Value = value;
	}

	/// <summary>
	/// Length of the EMA trend filter.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Fast period of the MACD module.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow period of the MACD module.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// Signal period of the MACD module.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Multiplier used by the reversal detector.
	/// </summary>
	public decimal ReversalFactor
	{
		get => _reversalFactor.Value;
		set => _reversalFactor.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Optional take profit distance in percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Optional stop loss distance in percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="CyberiaTraderAiStrategy"/>.
	/// </summary>
	public CyberiaTraderAiStrategy()
	{
		_maxPeriod = Param(nameof(MaxPeriod), 23)
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetDisplay("Max Period", "Largest sampling stride tested by the probability engine", "Model");

		_samplesPerPeriod = Param(nameof(SamplesPerPeriod), 5)
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetDisplay("Segments Per Period", "Number of historical segments processed for every period candidate", "Model");

		_spreadThreshold = Param(nameof(SpreadThreshold), 0m)
		.SetNotNegative()
		.SetCanOptimize(true)
		.SetDisplay("Spread Threshold", "Minimal absolute move to count a probability as successful", "Model");

		_enableCyberiaLogic = Param(nameof(EnableCyberiaLogic), true)
		.SetDisplay("Enable Cyberia Logic", "Use the probability based disable/allow switches", "Filters");

		_enableMacd = Param(nameof(EnableMacd), false)
		.SetDisplay("Enable MACD", "Use MACD to block trading against momentum", "Filters");

		_enableMa = Param(nameof(EnableMa), false)
		.SetDisplay("Enable EMA", "Use EMA slope to forbid trades against the trend", "Filters");

		_enableReversalDetector = Param(nameof(EnableReversalDetector), false)
		.SetDisplay("Enable Reversal Detector", "Flip permissions on extreme probability spikes", "Filters");

		_maPeriod = Param(nameof(MaPeriod), 23)
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetDisplay("EMA Period", "Length of the EMA used in the trend filter", "Indicators");

		_macdFast = Param(nameof(MacdFast), 12)
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetDisplay("MACD Fast", "Fast EMA length for MACD", "Indicators");

		_macdSlow = Param(nameof(MacdSlow), 26)
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetDisplay("MACD Slow", "Slow EMA length for MACD", "Indicators");

		_macdSignal = Param(nameof(MacdSignal), 9)
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetDisplay("MACD Signal", "Signal EMA length for MACD", "Indicators");

		_reversalFactor = Param(nameof(ReversalFactor), 3m)
		.SetGreaterThanZero()
		.SetDisplay("Reversal Factor", "Threshold multiplier that triggers the reversal detector", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe processed by the model", "General");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0m)
		.SetNotNegative()
		.SetDisplay("Take Profit %", "Optional take profit distance expressed in percent", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 0m)
		.SetNotNegative()
		.SetDisplay("Stop Loss %", "Optional stop loss distance expressed in percent", "Risk");

		Volume = 1m;
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

		_history.Clear();
		_previousEma = null;
		_previousPeriod = null;
		_currentStats = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Prepare indicator instances used by the optional filters.
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};

		_ema = new ExponentialMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_macd, _ema, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}

		var takeProfit = TakeProfitPercent > 0m ? new Unit(TakeProfitPercent / 100m, UnitTypes.Percent) : new Unit();
		var stopLoss = StopLossPercent > 0m ? new Unit(StopLossPercent / 100m, UnitTypes.Percent) : new Unit();
		StartProtection(takeProfit, stopLoss);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue emaValue)
	{
		// Operate only on completed candles.
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		// Respect indicator readiness when the corresponding filter is enabled.
		MovingAverageConvergenceDivergenceSignalValue macdSignal = null;
		if (macdValue.IsFinal)
		{
			if (macdValue is MovingAverageConvergenceDivergenceSignalValue macdData)
			{
				macdSignal = macdData;
			}
		}
		else if (EnableMacd)
		{
			return;
		}

		decimal? emaSnapshot = null;
		if (emaValue.IsFinal)
		{
			emaSnapshot = emaValue.ToDecimal();
		}
		else if (EnableMa)
		{
			return;
		}

		// Store the candle in the local history used by the probability model.
		UpdateHistory(candle);

		var candles = _history.ToArray();
		_currentStats = FindBestStats(candles);

		// Always capture the latest EMA value for slope calculations.
		if (emaSnapshot is decimal emaValueDecimal)
		{
			if (_previousEma == null)
			{
				_previousEma = emaValueDecimal;
			}
		}

		// Avoid trading before the strategy is fully initialized.
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			if (emaSnapshot is decimal emaValueUnformed)
			{
				_previousEma = emaValueUnformed;
			}

			return;
		}

		if (!_currentStats.IsValid)
		{
			if (emaSnapshot is decimal emaValueInvalid)
			{
				_previousEma = emaValueInvalid;
			}

			return;
		}

		var flags = CalculateDirection(emaSnapshot, macdSignal);

		HandlePositions(flags);

		_previousPeriod = _currentStats.Period;
	}

	private void HandlePositions(DirectionFlags flags)
	{
		var stats = _currentStats;

		// No trades without a valid statistical snapshot.
		if (!stats.IsValid)
		{
			return;
		}

		// Manage existing positions first to mirror the MQL behaviour.
		if (Position > 0)
		{
			var shouldExitLong = (stats.CurrentDecision == TradeDecisions.Sell &&
			stats.SellPossibility >= stats.SellSucPossibilityMid &&
			stats.SellSucPossibilityMid > 0m) ||
			(flags.DisableBuy && stats.CurrentDecision != TradeDecisions.Buy);

			if (shouldExitLong)
			{
				SellMarket(Position);
				return;
			}
		}
		else if (Position < 0)
		{
			var shouldExitShort = (stats.CurrentDecision == TradeDecisions.Buy &&
			stats.BuyPossibility >= stats.BuySucPossibilityMid &&
			stats.BuySucPossibilityMid > 0m) ||
			(flags.DisableSell && stats.CurrentDecision != TradeDecisions.Sell);

			if (shouldExitShort)
			{
				BuyMarket(-Position);
				return;
			}
		}

		// Evaluate fresh entries only when the probability module allows it.
		if (stats.CurrentDecision == TradeDecisions.Buy &&
		!flags.DisableBuy &&
		stats.BuyPossibility >= stats.BuySucPossibilityMid &&
		stats.BuySucPossibilityMid > 0m &&
		Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
			return;
		}

		if (stats.CurrentDecision == TradeDecisions.Sell &&
		!flags.DisableSell &&
		stats.SellPossibility >= stats.SellSucPossibilityMid &&
		stats.SellSucPossibilityMid > 0m &&
		Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			SellMarket(volume);
		}
	}

	private DirectionFlags CalculateDirection(decimal? emaValue, MovingAverageConvergenceDivergenceSignalValue macdValue)
	{
		var stats = _currentStats;
		var disableBuy = false;
		var disableSell = false;
		var disablePipsator = false;
		var disableBuyPips = false;
		var disableSellPips = false;

		if (EnableCyberiaLogic)
		{
			var buyScore = stats.BuyPossibilityMid * stats.BuyPossibilityQuality;
			var sellScore = stats.SellPossibilityMid * stats.SellPossibilityQuality;

			if (_previousPeriod is int previousPeriodValue)
			{
				if (stats.Period > previousPeriodValue)
				{
					if (sellScore > buyScore)
					{
						disableSell = false;
						disableBuy = true;
						disableBuyPips = true;

						if (stats.SellSucPossibilityMid * stats.SellSucPossibilityQuality >
						stats.BuySucPossibilityMid * stats.BuySucPossibilityQuality)
						{
							disableSell = true;
						}
					}
					else if (sellScore < buyScore)
					{
						disableSell = true;
						disableBuy = false;
						disableSellPips = true;

						if (stats.SellSucPossibilityMid * stats.SellSucPossibilityQuality <
						stats.BuySucPossibilityMid * stats.BuySucPossibilityQuality)
						{
							disableBuy = true;
						}
					}
				}
				else if (stats.Period < previousPeriodValue)
				{
					disableSell = true;
					disableBuy = true;
				}
			}

			if (sellScore == buyScore)
			{
				disableSell = true;
				disableBuy = true;
				disablePipsator = false;
			}

			if (stats.SellPossibility > stats.SellSucPossibilityMid * 2m && stats.SellSucPossibilityMid > 0m)
			{
				disableSell = true;
				disableSellPips = true;
			}

			if (stats.BuyPossibility > stats.BuySucPossibilityMid * 2m && stats.BuySucPossibilityMid > 0m)
			{
				disableBuy = true;
				disableBuyPips = true;
			}
		}

		if (EnableMa && emaValue is decimal emaDecimal)
		{
			if (_previousEma is decimal previousEma)
			{
				if (emaDecimal > previousEma)
				{
					disableSell = true;
					disableSellPips = true;
				}
				else if (emaDecimal < previousEma)
				{
					disableBuy = true;
					disableBuyPips = true;
				}
			}

			_previousEma = emaDecimal;
		}
		else if (emaValue is decimal emaSnapshot)
		{
			_previousEma = emaSnapshot;
		}

		if (EnableMacd && macdValue != null)
		{
			var macdMain = macdValue.Value.Macd;
			var macdSignal = macdValue.Value.Signal;

			if (macdMain > macdSignal)
			{
				disableSell = true;
			}
			else if (macdMain < macdSignal)
			{
				disableBuy = true;
			}
		}

		if (EnableReversalDetector)
		{
			var trigger = false;
			if (stats.BuyPossibilityMid > 0m && stats.BuyPossibility > stats.BuyPossibilityMid * ReversalFactor)
			{
				trigger = true;
			}

			if (stats.SellPossibilityMid > 0m && stats.SellPossibility > stats.SellPossibilityMid * ReversalFactor)
			{
				trigger = true;
			}

			if (trigger)
			{
				disableSell = !disableSell;
				disableBuy = !disableBuy;
				disableSellPips = !disableSellPips;
				disableBuyPips = !disableBuyPips;
				disablePipsator = !disablePipsator;
			}
		}

		return new DirectionFlags
		{
			DisableBuy = disableBuy,
			DisableSell = disableSell,
			DisablePipsator = disablePipsator,
			DisableBuyPipsator = disableBuyPips,
			DisableSellPipsator = disableSellPips,
		};
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		var snapshot = new CandleSnapshot(candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice);
		_history.Enqueue(snapshot);

		var maxHistory = MaxPeriod * (MaxPeriod * SamplesPerPeriod + 2);
		while (_history.Count > maxHistory)
		{
			_history.Dequeue();
		}
	}

	private ModelStats FindBestStats(CandleSnapshot[] candles)
	{
		var bestStats = default(ModelStats);
		var bestQuality = decimal.MinValue;
		var maxPeriod = MaxPeriod;
		var segments = SamplesPerPeriod;
		var spread = SpreadThreshold;

		for (var period = 1; period <= maxPeriod; period++)
		{
			var modelingBars = period * segments;
			var required = period * modelingBars + 1;

			if (candles.Length < required)
			{
				continue;
			}

			var stats = CalculateStats(candles, period, modelingBars, spread);
			if (!stats.IsValid)
			{
				continue;
			}

			if (stats.PossibilitySuccessRatio > bestQuality)
			{
				bestQuality = stats.PossibilitySuccessRatio;
				bestStats = stats;
			}
		}

		return bestStats;
	}

	private ModelStats CalculateStats(CandleSnapshot[] candles, int period, int modelingBars, decimal spreadThreshold)
	{
		var stats = new ModelStats { Period = period };

		var buyQuality = 0;
		var sellQuality = 0;
		var undefinedQuality = 0;

		var buySum = 0m;
		var sellSum = 0m;
		var undefinedSum = 0m;

		var buySuccessSum = 0m;
		var sellSuccessSum = 0m;
		var undefinedSuccessSum = 0m;

		for (var shift = 0; shift < modelingBars; shift++)
		{
			var currentIndex = candles.Length - 1 - period * shift;
			var previousIndex = currentIndex - period;

			if (previousIndex < 0)
			{
				return default;
			}

			var current = candles[currentIndex];
			var previous = candles[previousIndex];

			var decisionValue = current.Close - current.Open;
			var previousValue = previous.Close - previous.Open;

			var buyPossibility = 0m;
			var sellPossibility = 0m;
			var undefinedPossibility = 0m;
			var decision = TradeDecisions.Unknown;

			if (decisionValue > 0m)
			{
				if (previousValue < 0m)
				{
					decision = TradeDecisions.Sell;
					sellPossibility = decisionValue;
				}
				else
				{
					undefinedPossibility = decisionValue;
				}
			}
			else if (decisionValue < 0m)
			{
				if (previousValue > 0m)
				{
					decision = TradeDecisions.Buy;
					buyPossibility = -decisionValue;
				}
				else
				{
					undefinedPossibility = -decisionValue;
				}
			}

			if (shift == 0)
			{
				stats.CurrentDecision = decision;
				stats.BuyPossibility = buyPossibility;
				stats.SellPossibility = sellPossibility;
				stats.UndefinedPossibility = undefinedPossibility;
			}

			switch (decision)
			{
			case TradeDecisions.Buy:
				buyQuality++;
				buySum += buyPossibility;
				if (buyPossibility > spreadThreshold)
				{
					buySuccessSum += buyPossibility;
					stats.BuySucPossibilityQuality++;
				}
				break;

			case TradeDecisions.Sell:
				sellQuality++;
				sellSum += sellPossibility;
				if (sellPossibility > spreadThreshold)
				{
					sellSuccessSum += sellPossibility;
					stats.SellSucPossibilityQuality++;
				}
				break;

			default:
				undefinedQuality++;
				undefinedSum += undefinedPossibility;
				if (undefinedPossibility > spreadThreshold)
				{
					undefinedSuccessSum += undefinedPossibility;
					stats.UndefinedSucPossibilityQuality++;
				}
				break;
			}
		}

		stats.BuyPossibilityQuality = buyQuality;
		stats.SellPossibilityQuality = sellQuality;
		stats.UndefinedPossibilityQuality = undefinedQuality;

		stats.BuyPossibilityMid = buyQuality > 0 ? buySum / buyQuality : 0m;
		stats.SellPossibilityMid = sellQuality > 0 ? sellSum / sellQuality : 0m;
		stats.UndefinedPossibilityMid = undefinedQuality > 0 ? undefinedSum / undefinedQuality : 0m;

		var buySuccessCount = stats.BuySucPossibilityQuality;
		var sellSuccessCount = stats.SellSucPossibilityQuality;
		var undefinedSuccessCount = stats.UndefinedSucPossibilityQuality;

		stats.BuySucPossibilityMid = buySuccessCount > 0 ? buySuccessSum / buySuccessCount : 0m;
		stats.SellSucPossibilityMid = sellSuccessCount > 0 ? sellSuccessSum / sellSuccessCount : 0m;
		stats.UndefinedSucPossibilityMid = undefinedSuccessCount > 0 ? undefinedSuccessSum / undefinedSuccessCount : 0m;

		var successTotal = buySuccessCount + sellSuccessCount + undefinedSuccessCount;
		if (successTotal > 0)
		{
			stats.PossibilitySuccessRatio = (buySuccessCount + sellSuccessCount) / (decimal)successTotal;
		}
		else
		{
			stats.PossibilitySuccessRatio = 0m;
		}

		stats.IsValid = buyQuality + sellQuality + undefinedQuality > 0;
		return stats;
	}

	private readonly struct CandleSnapshot
	{
		public CandleSnapshot(decimal open, decimal high, decimal low, decimal close)
		{
			Open = open;
			High = high;
			Low = low;
			Close = close;
		}

		public decimal Open { get; }
		public decimal High { get; }
		public decimal Low { get; }
		public decimal Close { get; }
	}

	private struct DirectionFlags
	{
		public bool DisableBuy;
		public bool DisableSell;
		public bool DisablePipsator;
		public bool DisableBuyPipsator;
		public bool DisableSellPipsator;
	}

	private struct ModelStats
	{
		public bool IsValid;
		public int Period;
		public TradeDecisions CurrentDecision;
		public decimal BuyPossibility;
		public decimal SellPossibility;
		public decimal UndefinedPossibility;
		public int BuyPossibilityQuality;
		public int SellPossibilityQuality;
		public int UndefinedPossibilityQuality;
		public decimal BuyPossibilityMid;
		public decimal SellPossibilityMid;
		public decimal UndefinedPossibilityMid;
		public decimal BuySucPossibilityMid;
		public decimal SellSucPossibilityMid;
		public decimal UndefinedSucPossibilityMid;
		public int BuySucPossibilityQuality;
		public int SellSucPossibilityQuality;
		public int UndefinedSucPossibilityQuality;
		public decimal PossibilitySuccessRatio;
	}

	private enum TradeDecisions
	{
		Unknown,
		Buy,
		Sell,
	}
}
