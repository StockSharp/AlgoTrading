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
/// Breakout strategy that trades when price exceeds the previous candle high or low.
/// </summary>
public class PreviousCandleBreakdownStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _indentSteps;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _fastMaShift;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _slowMaShift;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<decimal> _trailingStopSteps;
	private readonly StrategyParam<decimal> _trailingStepSteps;
	private readonly StrategyParam<decimal> _profitClose;
	private readonly StrategyParam<int> _maxNetPosition;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;

	private SMA _fastMa;
	private SMA _slowMa;
	private Shift _fastShift;
	private Shift _slowShift;

	private decimal? _fastMaValue;
	private decimal? _slowMaValue;

	private decimal? _breakoutHigh;
	private decimal? _breakoutLow;
	private DateTimeOffset? _referenceCandleTime;

	private decimal? _nextBreakoutHigh;
	private decimal? _nextBreakoutLow;
	private DateTimeOffset? _nextReferenceTime;
	private bool _hasBreakoutSeed;

	private DateTimeOffset? _lastBuyReferenceTime;
	private DateTimeOffset? _lastSellReferenceTime;

	private decimal? _entryPrice;
	private decimal _highestSinceEntry;
	private decimal _lowestSinceEntry;
	private decimal? _trailingStopLevel;
	private decimal _lastPrice;
	private decimal _priceStep;

	private bool _profitTargetTriggered;
	private bool _longExitRequested;
	private bool _shortExitRequested;

	public PreviousCandleBreakdownStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Reference timeframe used to compute breakout levels", "General");

		_indentSteps = Param(nameof(IndentSteps), 10m)
			.SetDisplay("Indent Steps", "Offset above/below the previous candle in price steps", "General")
			.SetCanOptimize(true);

		_fastMaPeriod = Param(nameof(FastMaPeriod), 10)
			.SetDisplay("Fast MA Period", "Length of the fast moving average (0 disables filter)", "Filters");

		_fastMaShift = Param(nameof(FastMaShift), 3)
			.SetDisplay("Fast MA Shift", "Number of bars to shift the fast MA forward", "Filters");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 30)
			.SetDisplay("Slow MA Period", "Length of the slow moving average (0 disables filter)", "Filters");

		_slowMaShift = Param(nameof(SlowMaShift), 0)
			.SetDisplay("Slow MA Shift", "Number of bars to shift the slow MA forward", "Filters");

		_stopLossSteps = Param(nameof(StopLossSteps), 50m)
			.SetDisplay("Stop Loss Steps", "Distance to the stop loss in price steps", "Risk")
			.SetCanOptimize(true);

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 150m)
			.SetDisplay("Take Profit Steps", "Distance to the take profit in price steps", "Risk")
			.SetCanOptimize(true);

		_trailingStopSteps = Param(nameof(TrailingStopSteps), 15m)
			.SetDisplay("Trailing Stop Steps", "Trailing stop distance in price steps (0 disables)", "Risk")
			.SetCanOptimize(true);

		_trailingStepSteps = Param(nameof(TrailingStepSteps), 5m)
			.SetDisplay("Trailing Step Steps", "Minimal advance required to move the trailing stop", "Risk")
			.SetCanOptimize(true);

		_profitClose = Param(nameof(ProfitClose), 0m)
			.SetDisplay("Profit Close", "Floating profit target that closes all positions", "Risk");

		_maxNetPosition = Param(nameof(MaxNetPosition), 1)
			.SetGreaterThanZero()
			.SetDisplay("Max Net Position", "Maximum absolute net position allowed", "Trading");

		_startTime = Param(nameof(StartTime), new TimeSpan(9, 9, 0))
			.SetDisplay("Start Time", "Beginning of the trading window", "Sessions");

		_endTime = Param(nameof(EndTime), new TimeSpan(19, 19, 0))
			.SetDisplay("End Time", "End of the trading window", "Sessions");
	}

	public DataType CandleType => _candleType.Value;

	public decimal IndentSteps
	{
		get => _indentSteps.Value;
		set => _indentSteps.Value = value;
	}

	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	public int FastMaShift
	{
		get => _fastMaShift.Value;
		set => _fastMaShift.Value = value;
	}

	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	public int SlowMaShift
	{
		get => _slowMaShift.Value;
		set => _slowMaShift.Value = value;
	}

	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	public decimal TrailingStopSteps
	{
		get => _trailingStopSteps.Value;
		set => _trailingStopSteps.Value = value;
	}

	public decimal TrailingStepSteps
	{
		get => _trailingStepSteps.Value;
		set => _trailingStepSteps.Value = value;
	}

	public decimal ProfitClose
	{
		get => _profitClose.Value;
		set => _profitClose.Value = value;
	}

	public int MaxNetPosition
	{
		get => _maxNetPosition.Value;
		set => _maxNetPosition.Value = value;
	}

	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	public TimeSpan EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (Security, DataType.Ticks);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Validate trailing parameters before subscriptions are created.
		if (TrailingStopSteps > 0m && TrailingStepSteps <= 0m)
			throw new InvalidOperationException("Trailing step must be greater than zero when trailing stop is enabled.");

		// Cache the price step so offsets can be expressed in price steps.
		_priceStep = GetPriceStep();

		// Initialize moving averages and optional shift indicators.
		_fastMa = FastMaPeriod > 0 ? new SMA { Length = FastMaPeriod } : null;
		_slowMa = SlowMaPeriod > 0 ? new SMA { Length = SlowMaPeriod } : null;

		_fastShift = _fastMa != null && FastMaShift > 0 ? new Shift { Length = FastMaShift } : null;
		_slowShift = _slowMa != null && SlowMaShift > 0 ? new Shift { Length = SlowMaShift } : null;

		// Subscribe to reference timeframe candles and trade ticks.
		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription.Bind(ProcessCandle).Start();

		SubscribeTicks().Bind(ProcessTrade).Start();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		// Reset risk tracking whenever the net position changes.
		if (Position > 0m)
		{
			_entryPrice = PositionPrice;
			_highestSinceEntry = _entryPrice ?? 0m;
			_lowestSinceEntry = _entryPrice ?? 0m;
			_trailingStopLevel = null;
			_profitTargetTriggered = false;
			_longExitRequested = false;
			_shortExitRequested = false;
		}
		else if (Position < 0m)
		{
			_entryPrice = PositionPrice;
			_highestSinceEntry = _entryPrice ?? 0m;
			_lowestSinceEntry = _entryPrice ?? 0m;
			_trailingStopLevel = null;
			_profitTargetTriggered = false;
			_longExitRequested = false;
			_shortExitRequested = false;
		}
		else
		{
			_entryPrice = null;
			_trailingStopLevel = null;
			_profitTargetTriggered = false;
			_longExitRequested = false;
			_shortExitRequested = false;
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Use the candle close price as a fallback for risk checks.
		_lastPrice = candle.ClosePrice;

		UpdateMovingAverages(candle);
		UpdateBreakoutLevels(candle);
		CheckProfitClose(candle.ClosePrice);
		EvaluateRisk(candle.ClosePrice);
	}

		private void ProcessTrade(ITickTradeMessage trade)
		{
			var price = trade.Price;

		_lastPrice = price;

		CheckProfitClose(price);
		EvaluateRisk(price);

		// Entry checks are allowed only inside the configured trading window.
		if (!IsWithinTradingWindow(trade.ServerTime))
			return;

		if (_breakoutHigh is null || _breakoutLow is null || _referenceCandleTime is null)
			return;

		var allowLong = !UseMaFilter || (_fastMaValue is decimal fast && _slowMaValue is decimal slow && fast > slow);
		var allowShort = !UseMaFilter || (_fastMaValue is decimal fastValue && _slowMaValue is decimal slowValue && fastValue < slowValue);

		// Trigger entries when price breaches the breakout levels once per candle.
		if (allowLong && price >= _breakoutHigh.Value && _lastBuyReferenceTime != _referenceCandleTime)
			TryEnterLong();

		if (allowShort && price <= _breakoutLow.Value && _lastSellReferenceTime != _referenceCandleTime)
			TryEnterShort();
	}

	private void UpdateMovingAverages(ICandleMessage candle)
	{
		// Store the latest MA values and apply optional shift offsets.
		if (_fastMa != null)
		{
			var maValue = _fastMa.Process(candle);
			if (maValue.IsFinal)
			{
				var baseValue = maValue.ToDecimal();
				_fastMaValue = _fastShift != null
					? _fastShift.Process(baseValue, candle.OpenTime, true).ToDecimal()
					: baseValue;
			}
		}

		if (_slowMa != null)
		{
			var maValue = _slowMa.Process(candle);
			if (maValue.IsFinal)
			{
				var baseValue = maValue.ToDecimal();
				_slowMaValue = _slowShift != null
					? _slowShift.Process(baseValue, candle.OpenTime, true).ToDecimal()
					: baseValue;
			}
		}
	}

	private void UpdateBreakoutLevels(ICandleMessage candle)
	{
		// Keep the previous candle data as the active breakout reference.
		if (_hasBreakoutSeed)
		{
			_breakoutHigh = _nextBreakoutHigh;
			_breakoutLow = _nextBreakoutLow;
			_referenceCandleTime = _nextReferenceTime;
		}

		var indent = GetOffset(IndentSteps);
		_nextBreakoutHigh = candle.HighPrice + indent;
		_nextBreakoutLow = candle.LowPrice - indent;
		_nextReferenceTime = candle.OpenTime;
		_hasBreakoutSeed = true;
	}

	private void TryEnterLong()
	{
		_lastBuyReferenceTime = _referenceCandleTime;

		// Prevent scaling beyond the maximum net long exposure.
		if (Position > 0m && Position >= MaxNetPosition)
			return;

		_longExitRequested = false;
		_shortExitRequested = false;
		BuyMarket();
	}

	private void TryEnterShort()
	{
		_lastSellReferenceTime = _referenceCandleTime;

		// Prevent scaling beyond the maximum net short exposure.
		if (Position < 0m && -Position >= MaxNetPosition)
			return;

		_longExitRequested = false;
		_shortExitRequested = false;
		SellMarket();
	}

	private void EvaluateRisk(decimal price)
	{
		if (Position == 0m || _entryPrice is not decimal entry)
			return;

		if (Position > 0m)
		{
			// Track the most favorable excursion for trailing stops.
			_highestSinceEntry = Math.Max(_highestSinceEntry, price);

			var stopLoss = GetOffset(StopLossSteps);
			if (StopLossSteps > 0m && price <= entry - stopLoss)
			{
				if (!_longExitRequested)
				{
					_longExitRequested = true;
					SellMarket();
				}
				return;
			}

			var takeProfit = GetOffset(TakeProfitSteps);
			if (TakeProfitSteps > 0m && price >= entry + takeProfit)
			{
				if (!_longExitRequested)
				{
					_longExitRequested = true;
					SellMarket();
				}
				return;
			}

			if (TrailingStopSteps > 0m)
			{
				var trailingDistance = GetOffset(TrailingStopSteps);
				var trailingStep = GetOffset(TrailingStepSteps);
				var desiredStop = _highestSinceEntry - trailingDistance;

				if (_trailingStopLevel is null)
				{
					if (_highestSinceEntry - entry >= trailingDistance)
						_trailingStopLevel = desiredStop;
				}
				else if (desiredStop - _trailingStopLevel.Value >= trailingStep)
				{
					_trailingStopLevel = desiredStop;
				}

				if (_trailingStopLevel is decimal trailing && price <= trailing)
				{
					if (!_longExitRequested)
					{
						_longExitRequested = true;
						SellMarket();
					}
				}
			}
		}
		else if (Position < 0m)
		{
			// Track the most favorable excursion for trailing stops.
			_lowestSinceEntry = Math.Min(_lowestSinceEntry, price);

			var stopLoss = GetOffset(StopLossSteps);
			if (StopLossSteps > 0m && price >= entry + stopLoss)
			{
				if (!_shortExitRequested)
				{
					_shortExitRequested = true;
					BuyMarket();
				}
				return;
			}

			var takeProfit = GetOffset(TakeProfitSteps);
			if (TakeProfitSteps > 0m && price <= entry - takeProfit)
			{
				if (!_shortExitRequested)
				{
					_shortExitRequested = true;
					BuyMarket();
				}
				return;
			}

			if (TrailingStopSteps > 0m)
			{
				var trailingDistance = GetOffset(TrailingStopSteps);
				var trailingStep = GetOffset(TrailingStepSteps);
				var desiredStop = _lowestSinceEntry + trailingDistance;

				if (_trailingStopLevel is null)
				{
					if (entry - _lowestSinceEntry >= trailingDistance)
						_trailingStopLevel = desiredStop;
				}
				else if (_trailingStopLevel.Value - desiredStop >= trailingStep)
				{
					_trailingStopLevel = desiredStop;
				}

				if (_trailingStopLevel is decimal trailing && price >= trailing)
				{
					if (!_shortExitRequested)
					{
						_shortExitRequested = true;
						BuyMarket();
					}
				}
			}
		}
	}

	private void CheckProfitClose(decimal price)
	{
		if (ProfitClose <= 0m || Position == 0m)
			return;

		if (PositionPrice is not decimal positionPrice)
			return;

		// Use unrealized profit to decide when to close all positions.
		var floatingProfit = Position * (price - positionPrice);

		if (floatingProfit >= ProfitClose && !_profitTargetTriggered)
		{
			CloseAll("Floating profit target reached");
			_profitTargetTriggered = true;
		}
	}

	private decimal GetOffset(decimal steps) => steps * _priceStep;

	private decimal GetPriceStep()
	{
		// Fallback to a tiny step when the security does not provide one.
		var step = Security.PriceStep ?? 0.0001m;
		return step > 0m ? step : 0.0001m;
	}

	private bool UseMaFilter => FastMaPeriod > 0 && SlowMaPeriod > 0;

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		// Support trading windows that cross midnight.
		var current = time.TimeOfDay;
		return StartTime <= EndTime ? current >= StartTime && current <= EndTime : current >= StartTime || current <= EndTime;
	}
}

