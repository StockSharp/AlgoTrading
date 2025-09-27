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
/// Momentum and MACD based multi-timeframe strategy converted from the Trading Criteria expert advisor.
/// </summary>
public class TradingCriteriaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<DataType> _monthlyCandleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPoints;
	private readonly StrategyParam<decimal> _breakEvenOffsetPoints;

	private LinearWeightedMovingAverage _fastMa = null!;
	private LinearWeightedMovingAverage _slowMa = null!;
	private Momentum _trendMomentum = null!;
	private MovingAverageConvergenceDivergenceSignal _trendMacd = null!;
	private MovingAverageConvergenceDivergenceSignal _monthlyMacd = null!;

	private decimal? _momentumRecent;
	private decimal? _momentumPrevious;
	private decimal? _momentumOld;

	private decimal? _trendMacdValue;
	private decimal? _trendMacdSignal;
	private decimal? _trendMacdPrev;
	private decimal? _trendSignalPrev;

	private decimal? _monthlyMacdValue;
	private decimal? _monthlyMacdSignal;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortTakeProfitPrice;
	private decimal _longHighestPrice;
	private decimal _shortLowestPrice;
	private bool _longBreakEvenActive;
	private bool _shortBreakEvenActive;

	private decimal _pipSize;
	private decimal _signedPosition;

	/// <summary>
	/// Initializes a new instance of the <see cref="TradingCriteriaStrategy"/> class.
	/// </summary>
	public TradingCriteriaStrategy()
	{
		Volume = 1m;

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Entry TF", "Primary timeframe used for entries", "General");

		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Trend TF", "Timeframe used for trend momentum and MACD", "General");

		_monthlyCandleType = Param(nameof(MonthlyCandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("Monthly TF", "Slow timeframe used for long term MACD filter", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 1)
			.SetGreaterThanZero()
			.SetDisplay("Fast LWMA", "Fast linear weighted moving average length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Slow LWMA", "Slow linear weighted moving average length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 1);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Momentum lookback on trend timeframe", "Indicators");

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Bull Momentum", "Minimum momentum deviation for long trades", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 2m, 0.1m);

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Bear Momentum", "Minimum momentum deviation for short trades", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 2m, 0.1m);

		_maxPositions = Param(nameof(MaxPositions), 1)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum number of base lots that can remain open", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Protective stop distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance in points", "Risk");

		_enableTrailing = Param(nameof(EnableTrailing), true)
			.SetDisplay("Enable Trailing", "Enable trailing stop logic", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 40m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Trailing distance in points", "Risk");

		_enableBreakEven = Param(nameof(EnableBreakEven), true)
			.SetDisplay("Enable Break Even", "Move stop to break even after trigger", "Risk");

		_breakEvenTriggerPoints = Param(nameof(BreakEvenTriggerPoints), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Break Even Trigger", "Price distance that activates break even", "Risk");

		_breakEvenOffsetPoints = Param(nameof(BreakEvenOffsetPoints), 5m)
			.SetDisplay("Break Even Offset", "Offset applied when stop moves to break even", "Risk");
	}

	/// <summary>
	/// Primary candle type used for entries.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Trend timeframe used for momentum and MACD filters.
	/// </summary>
	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	/// <summary>
	/// Slow timeframe for monthly MACD confirmation.
	/// </summary>
	public DataType MonthlyCandleType
	{
		get => _monthlyCandleType.Value;
		set => _monthlyCandleType.Value = value;
	}

	/// <summary>
	/// Fast linear weighted moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow linear weighted moving average period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Momentum calculation period on trend timeframe.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum deviation from 100 required to allow long trades.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Minimum deviation from 100 required to allow short trades.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Maximum number of base lots that strategy can hold.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enables or disables the trailing stop logic.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Enables or disables break even protection.
	/// </summary>
	public bool EnableBreakEven
	{
		get => _enableBreakEven.Value;
		set => _enableBreakEven.Value = value;
	}

	/// <summary>
	/// Price distance required before moving the stop to break even.
	/// </summary>
	public decimal BreakEvenTriggerPoints
	{
		get => _breakEvenTriggerPoints.Value;
		set => _breakEvenTriggerPoints.Value = value;
	}

	/// <summary>
	/// Offset added when the stop moves to break even.
	/// </summary>
	public decimal BreakEvenOffsetPoints
	{
		get => _breakEvenOffsetPoints.Value;
		set => _breakEvenOffsetPoints.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_momentumRecent = null;
		_momentumPrevious = null;
		_momentumOld = null;
		_trendMacdValue = null;
		_trendMacdSignal = null;
		_trendMacdPrev = null;
		_trendSignalPrev = null;
		_monthlyMacdValue = null;
		_monthlyMacdSignal = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakeProfitPrice = null;
		_shortTakeProfitPrice = null;
		_longHighestPrice = 0m;
		_shortLowestPrice = 0m;
		_longBreakEvenActive = false;
		_shortBreakEvenActive = false;
		_pipSize = 0m;
		_signedPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var step = Security?.PriceStep ?? 0m;
		if (step == 0m)
			step = 1m;
		else if (step == 0.00001m || step == 0.001m)
			step *= 10m;

		_pipSize = step;
		// Normalize price step to align with the original pip handling.

		_fastMa = new LinearWeightedMovingAverage { Length = FastMaPeriod };
		_slowMa = new LinearWeightedMovingAverage { Length = SlowMaPeriod };
		_trendMomentum = new Momentum { Length = MomentumPeriod };
		_trendMacd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 }
			},
			SignalMa = { Length = 9 }
		};

		_monthlyMacd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 }
			},
			SignalMa = { Length = 9 }
		};

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription.Bind(_fastMa, _slowMa, ProcessMainCandle).Start();
		// Subscribe to the base timeframe and stream moving average values into the handler.

		var trendMomentumSubscription = SubscribeCandles(TrendCandleType);
		trendMomentumSubscription.Bind(_trendMomentum, ProcessTrendMomentum).Start();
		// Track momentum deviations on the trend timeframe.

		var trendMacdSubscription = SubscribeCandles(TrendCandleType);
		trendMacdSubscription.BindEx(_trendMacd, ProcessTrendMacd).Start();
		// Evaluate MACD relationship between main and signal lines on the same timeframe.

		var monthlyMacdSubscription = SubscribeCandles(MonthlyCandleType);
		monthlyMacdSubscription.BindEx(_monthlyMacd, ProcessMonthlyMacd).Start();
		// Use a slower timeframe MACD to confirm the large scale direction.

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawOwnTrades(area);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
		}

		StartProtection();
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal fastMa, decimal slowMa)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (ManageExistingPosition(candle))
			return;
		// Risk management closed the position on this candle, skip new signals.

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_momentumRecent is null || _momentumPrevious is null || _momentumOld is null)
			return;

		if (_trendMacdValue is null || _trendMacdSignal is null)
			return;

		if (_monthlyMacdValue is null || _monthlyMacdSignal is null)
			return;

		var momentumBullish = _momentumRecent.Value > MomentumBuyThreshold
			|| _momentumPrevious.Value > MomentumBuyThreshold
			|| _momentumOld.Value > MomentumBuyThreshold;

		var momentumBearish = _momentumRecent.Value > MomentumSellThreshold
			|| _momentumPrevious.Value > MomentumSellThreshold
			|| _momentumOld.Value > MomentumSellThreshold;

		var macdBullish = _trendMacdValue.Value > _trendMacdSignal.Value
			&& (_trendMacdPrev is null || _trendSignalPrev is null || _trendMacdPrev.Value <= _trendSignalPrev.Value);
		// Require MACD main line to stay above the signal line and avoid repeat triggers.

		var macdBearish = _trendMacdValue.Value < _trendMacdSignal.Value
			&& (_trendMacdPrev is null || _trendSignalPrev is null || _trendMacdPrev.Value >= _trendSignalPrev.Value);
		// Mirror condition for the bearish side with confirmation from the previous bar.

		var monthlyBullish = _monthlyMacdValue.Value > _monthlyMacdSignal.Value;
		var monthlyBearish = _monthlyMacdValue.Value < _monthlyMacdSignal.Value;

		var longTrend = fastMa > slowMa;
		var shortTrend = fastMa < slowMa;

		var maxExposure = MaxPositions * Volume;

		if (longTrend && momentumBullish && macdBullish && monthlyBullish && Position < maxExposure)
		{
			var volume = GetVolumeToOpenLong();
			if (volume > 0m)
				BuyMarket(volume);
		}
		else if (shortTrend && momentumBearish && macdBearish && monthlyBearish && Math.Abs(Position) < maxExposure)
		{
			var volume = GetVolumeToOpenShort();
			if (volume > 0m)
				SellMarket(volume);
		}
	}

	private void ProcessTrendMomentum(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_trendMomentum.IsFormed)
			return;

		var deviation = Math.Abs(momentumValue - 100m);

		_momentumOld = _momentumPrevious;
		_momentumPrevious = _momentumRecent;
		_momentumRecent = deviation;
	}

	private void ProcessTrendMacd(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (typed.Macd is not decimal macdLine || typed.Signal is not decimal signalLine)
			return;

		_trendMacdPrev = _trendMacdValue;
		_trendSignalPrev = _trendMacdSignal;
		_trendMacdValue = macdLine;
		_trendMacdSignal = signalLine;
	}

	private void ProcessMonthlyMacd(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (typed.Macd is not decimal macdLine || typed.Signal is not decimal signalLine)
			return;

		_monthlyMacdValue = macdLine;
		_monthlyMacdSignal = signalLine;
	}

	private bool ManageExistingPosition(ICandleMessage candle)
	{
		if (_signedPosition > 0m)
		{
			var volume = _signedPosition;

			if (_longTakeProfitPrice is decimal takeProfit && candle.HighPrice >= takeProfit)
			{
				SellMarket(volume);
				return true;
			}
			// Long position reached take profit, close the trade.

			if (_longStopPrice is decimal stopPrice && candle.LowPrice <= stopPrice)
			{
				SellMarket(volume);
				return true;
			}
			// Protective stop hit for the long position, exit immediately.

			if (_longEntryPrice is decimal entry)
			{
				if (EnableBreakEven && !_longBreakEvenActive && BreakEvenTriggerPoints > 0m)
				{
					var triggerPrice = entry + BreakEvenTriggerPoints * _pipSize;
					if (candle.ClosePrice >= triggerPrice)
					{
						_longBreakEvenActive = true;
						var offset = BreakEvenOffsetPoints > 0m ? BreakEvenOffsetPoints * _pipSize : 0m;
						_longStopPrice = entry + offset;
					}
				}
				// Activate break even once price moves far enough in favor.

				if (EnableTrailing && TrailingStopPoints > 0m)
				{
					if (candle.HighPrice > _longHighestPrice)
						_longHighestPrice = candle.HighPrice;

					var progress = _longHighestPrice - entry;
					var requiredMove = TrailingStopPoints * _pipSize;
					if (progress >= requiredMove)
					{
						var newStop = _longHighestPrice - requiredMove;
						if (!_longStopPrice.HasValue || newStop > _longStopPrice.Value)
							_longStopPrice = newStop;
					}
				}
				// Trail the stop using the highest price reached by the trend.
			}
		}
		else if (_signedPosition < 0m)
		{
			var volume = Math.Abs(_signedPosition);

			if (_shortTakeProfitPrice is decimal takeProfit && candle.LowPrice <= takeProfit)
			{
				BuyMarket(volume);
				return true;
			}
			// Short position reached take profit, cover the position.

			if (_shortStopPrice is decimal stopPrice && candle.HighPrice >= stopPrice)
			{
				BuyMarket(volume);
				return true;
			}
			// Protective stop for the short side was hit, cover the trade.

			if (_shortEntryPrice is decimal entry)
			{
				if (EnableBreakEven && !_shortBreakEvenActive && BreakEvenTriggerPoints > 0m)
				{
					var triggerPrice = entry - BreakEvenTriggerPoints * _pipSize;
					if (candle.ClosePrice <= triggerPrice)
					{
						_shortBreakEvenActive = true;
						var offset = BreakEvenOffsetPoints > 0m ? BreakEvenOffsetPoints * _pipSize : 0m;
						_shortStopPrice = entry - offset;
					}
				}
				// Activate break even for shorts once price moves enough in our favor.

				if (EnableTrailing && TrailingStopPoints > 0m)
				{
					if (_shortLowestPrice == 0m || candle.LowPrice < _shortLowestPrice)
						_shortLowestPrice = candle.LowPrice;

					var progress = entry - _shortLowestPrice;
					var requiredMove = TrailingStopPoints * _pipSize;
					if (progress >= requiredMove)
					{
						var newStop = _shortLowestPrice + requiredMove;
						if (!_shortStopPrice.HasValue || newStop < _shortStopPrice.Value)
							_shortStopPrice = newStop;
					}
				}
				// Trail the stop for shorts using the lowest price achieved.
			}
		}

		return false;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var order = trade.Order;
		if (order == null || order.Security != Security)
			return;

		var signedDelta = trade.Volume * (order.Side == Sides.Buy ? 1m : -1m);
		// Track the signed position manually to reproduce the MQL exposure handling.
		var previousPosition = _signedPosition;
		_signedPosition += signedDelta;

		var tradePrice = trade.Trade?.Price ?? order.Price;

		if (previousPosition <= 0m && _signedPosition > 0m)
		{
			_longEntryPrice = tradePrice;
			_longTakeProfitPrice = TakeProfitPoints > 0m ? tradePrice + TakeProfitPoints * _pipSize : null;
			// Initialize protective targets for the new long position.
			_longStopPrice = StopLossPoints > 0m ? tradePrice - StopLossPoints * _pipSize : null;
			_longHighestPrice = tradePrice;
			_longBreakEvenActive = false;
		}
		else if (previousPosition >= 0m && _signedPosition < 0m)
		{
			_shortEntryPrice = tradePrice;
			_shortTakeProfitPrice = TakeProfitPoints > 0m ? tradePrice - TakeProfitPoints * _pipSize : null;
			// Initialize protective targets for the new short position.
			_shortStopPrice = StopLossPoints > 0m ? tradePrice + StopLossPoints * _pipSize : null;
			_shortLowestPrice = tradePrice;
			_shortBreakEvenActive = false;
		}

		if (_signedPosition <= 0m)
		{
			_longEntryPrice = null;
			_longTakeProfitPrice = null;
			_longStopPrice = null;
			_longHighestPrice = 0m;
			_longBreakEvenActive = false;
		}

		if (_signedPosition >= 0m)
		{
			_shortEntryPrice = null;
			_shortTakeProfitPrice = null;
			_shortStopPrice = null;
			_shortLowestPrice = 0m;
			_shortBreakEvenActive = false;
		}
	}

	private decimal GetVolumeToOpenLong()
	{
		var current = Position;
		if (current < 0m)
			return Volume + Math.Abs(current);

		return Volume;
	}

	private decimal GetVolumeToOpenShort()
	{
		var current = Position;
		if (current > 0m)
			return Volume + current;

		return Volume;
	}
}

