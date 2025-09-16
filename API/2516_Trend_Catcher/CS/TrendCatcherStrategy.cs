using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend Catcher strategy converted from MetaTrader 5 implementation.
/// Combines Parabolic SAR flips with EMA trend filters and adaptive risk management.
/// </summary>
public class TrendCatcherStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _closeOnOppositeSignal;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<bool> _tradeMonday;
	private readonly StrategyParam<bool> _tradeTuesday;
	private readonly StrategyParam<bool> _tradeWednesday;
	private readonly StrategyParam<bool> _tradeThursday;
	private readonly StrategyParam<bool> _tradeFriday;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _fastFilterPeriod;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<bool> _autoStopLoss;
	private readonly StrategyParam<bool> _autoTakeProfit;
	private readonly StrategyParam<decimal> _minStopLoss;
	private readonly StrategyParam<decimal> _maxStopLoss;
	private readonly StrategyParam<decimal> _stopLossCoefficient;
	private readonly StrategyParam<decimal> _takeProfitCoefficient;
	private readonly StrategyParam<decimal> _manualStopLoss;
	private readonly StrategyParam<decimal> _manualTakeProfit;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<bool> _useMartingale;
	private readonly StrategyParam<decimal> _martingaleMultiplier;
	private readonly StrategyParam<decimal> _breakevenTrigger;
	private readonly StrategyParam<decimal> _breakevenOffset;
	private readonly StrategyParam<decimal> _trailingTrigger;
	private readonly StrategyParam<decimal> _trailingStep;

	private ExponentialMovingAverage _slowMa = null!;
	private ExponentialMovingAverage _fastMa = null!;
	private ExponentialMovingAverage _fastFilterMa = null!;
	private ParabolicSar _parabolicSar = null!;

	private decimal _previousClose;
	private decimal? _previousSar;
	private decimal? _entryPrice;
	private decimal _stopLossPrice;
	private decimal _takeProfitPrice;
	private bool _lastTradeWasLoss;
	private DateTimeOffset? _lastExitTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="TrendCatcherStrategy"/> class.
	/// </summary>
	public TrendCatcherStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for signal calculations", "General");

		_closeOnOppositeSignal = Param(nameof(CloseOnOppositeSignal), true)
			.SetDisplay("Close On Opposite", "Exit when an opposite signal appears", "General");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Invert long and short entries", "General");

		_tradeMonday = Param(nameof(TradeMonday), true)
			.SetDisplay("Trade Monday", "Allow trading on Mondays", "Trading Days");

		_tradeTuesday = Param(nameof(TradeTuesday), true)
			.SetDisplay("Trade Tuesday", "Allow trading on Tuesdays", "Trading Days");

		_tradeWednesday = Param(nameof(TradeWednesday), true)
			.SetDisplay("Trade Wednesday", "Allow trading on Wednesdays", "Trading Days");

		_tradeThursday = Param(nameof(TradeThursday), true)
			.SetDisplay("Trade Thursday", "Allow trading on Thursdays", "Trading Days");

		_tradeFriday = Param(nameof(TradeFriday), true)
			.SetDisplay("Trade Friday", "Allow trading on Fridays", "Trading Days");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Length of the slow EMA filter", "Indicators");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Length of the fast EMA", "Indicators");

		_fastFilterPeriod = Param(nameof(FastFilterPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("Trigger EMA", "Length of the trigger EMA", "Indicators");

		_sarStep = Param(nameof(SarStep), 0.004m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Step", "Acceleration step for Parabolic SAR", "Indicators");

		_sarMax = Param(nameof(SarMax), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max", "Maximum acceleration for Parabolic SAR", "Indicators");

		_autoStopLoss = Param(nameof(AutoStopLoss), true)
			.SetDisplay("Auto Stop Loss", "Derive stop-loss from Parabolic SAR", "Risk");

		_autoTakeProfit = Param(nameof(AutoTakeProfit), true)
			.SetDisplay("Auto Take Profit", "Derive take-profit from stop-loss", "Risk");

		_minStopLoss = Param(nameof(MinStopLoss), 0.001m)
			.SetGreaterThanZero()
			.SetDisplay("Min Stop", "Minimum allowed stop distance", "Risk");

		_maxStopLoss = Param(nameof(MaxStopLoss), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("Max Stop", "Maximum allowed stop distance", "Risk");

		_stopLossCoefficient = Param(nameof(StopLossCoefficient), 1m)
			.SetGreaterThanZero()
			.SetDisplay("SL Coefficient", "Multiplier applied to SAR distance", "Risk");

		_takeProfitCoefficient = Param(nameof(TakeProfitCoefficient), 1m)
			.SetGreaterThanZero()
			.SetDisplay("TP Coefficient", "Multiplier applied to take-profit distance", "Risk");

		_manualStopLoss = Param(nameof(ManualStopLoss), 0.002m)
			.SetGreaterThanZero()
			.SetDisplay("Manual Stop", "Fixed stop distance when automation is disabled", "Risk");

		_manualTakeProfit = Param(nameof(ManualTakeProfit), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("Manual Target", "Fixed target distance when automation is disabled", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Account risk per trade", "Risk");

		_useMartingale = Param(nameof(UseMartingale), true)
			.SetDisplay("Use Martingale", "Increase risk after a losing trade", "Risk");

		_martingaleMultiplier = Param(nameof(MartingaleMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Martingale Mult", "Multiplier applied after a loss", "Risk");

		_breakevenTrigger = Param(nameof(BreakevenTrigger), 0.005m)
			.SetGreaterThanZero()
			.SetDisplay("Breakeven Trigger", "Profit needed before moving stop to entry", "Exits");

		_breakevenOffset = Param(nameof(BreakevenOffset), 0.0001m)
			.SetGreaterThanZero()
			.SetDisplay("Breakeven Offset", "Extra buffer when moving stop to breakeven", "Exits");

		_trailingTrigger = Param(nameof(TrailingTrigger), 0.005m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Trigger", "Profit needed to activate trailing stop", "Exits");

		_trailingStep = Param(nameof(TrailingStep), 0.001m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Step", "Distance maintained by the trailing stop", "Exits");
	}

	/// <summary>
	/// Selected candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Gets or sets whether to close on opposite signal.
	/// </summary>
	public bool CloseOnOppositeSignal
	{
		get => _closeOnOppositeSignal.Value;
		set => _closeOnOppositeSignal.Value = value;
	}

	/// <summary>
	/// Gets or sets whether to reverse signals.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	public bool TradeMonday
	{
		get => _tradeMonday.Value;
		set => _tradeMonday.Value = value;
	}

	public bool TradeTuesday
	{
		get => _tradeTuesday.Value;
		set => _tradeTuesday.Value = value;
	}

	public bool TradeWednesday
	{
		get => _tradeWednesday.Value;
		set => _tradeWednesday.Value = value;
	}

	public bool TradeThursday
	{
		get => _tradeThursday.Value;
		set => _tradeThursday.Value = value;
	}

	public bool TradeFriday
	{
		get => _tradeFriday.Value;
		set => _tradeFriday.Value = value;
	}

	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	public int FastFilterPeriod
	{
		get => _fastFilterPeriod.Value;
		set => _fastFilterPeriod.Value = value;
	}

	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	public decimal SarMax
	{
		get => _sarMax.Value;
		set => _sarMax.Value = value;
	}

	public bool AutoStopLoss
	{
		get => _autoStopLoss.Value;
		set => _autoStopLoss.Value = value;
	}

	public bool AutoTakeProfit
	{
		get => _autoTakeProfit.Value;
		set => _autoTakeProfit.Value = value;
	}

	public decimal MinStopLoss
	{
		get => _minStopLoss.Value;
		set => _minStopLoss.Value = value;
	}

	public decimal MaxStopLoss
	{
		get => _maxStopLoss.Value;
		set => _maxStopLoss.Value = value;
	}

	public decimal StopLossCoefficient
	{
		get => _stopLossCoefficient.Value;
		set => _stopLossCoefficient.Value = value;
	}

	public decimal TakeProfitCoefficient
	{
		get => _takeProfitCoefficient.Value;
		set => _takeProfitCoefficient.Value = value;
	}

	public decimal ManualStopLoss
	{
		get => _manualStopLoss.Value;
		set => _manualStopLoss.Value = value;
	}

	public decimal ManualTakeProfit
	{
		get => _manualTakeProfit.Value;
		set => _manualTakeProfit.Value = value;
	}

	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	public bool UseMartingale
	{
		get => _useMartingale.Value;
		set => _useMartingale.Value = value;
	}

	public decimal MartingaleMultiplier
	{
		get => _martingaleMultiplier.Value;
		set => _martingaleMultiplier.Value = value;
	}

	public decimal BreakevenTrigger
	{
		get => _breakevenTrigger.Value;
		set => _breakevenTrigger.Value = value;
	}

	public decimal BreakevenOffset
	{
		get => _breakevenOffset.Value;
		set => _breakevenOffset.Value = value;
	}

	public decimal TrailingTrigger
	{
		get => _trailingTrigger.Value;
		set => _trailingTrigger.Value = value;
	}

	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create indicators for Parabolic SAR and EMA filters.
		_slowMa = new ExponentialMovingAverage { Length = SlowMaPeriod };
		_fastMa = new ExponentialMovingAverage { Length = FastMaPeriod };
		_fastFilterMa = new ExponentialMovingAverage { Length = FastFilterPeriod };
		_parabolicSar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationStep = SarStep,
			AccelerationMax = SarMax
		};

		// Subscribe to candle flow and bind indicators to the processing method.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_slowMa, _fastMa, _fastFilterMa, _parabolicSar, ProcessCandle)
			.Start();

		// Draw indicators and trades on the chart when possible.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _fastFilterMa);
			DrawIndicator(area, _parabolicSar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal slow, decimal fast, decimal fastFilter, decimal sar)
	{
		// Skip unfinished candles.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure data and connections are ready before trading.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Manage existing position and handle trailing logic.
		var exitTriggered = ManageActivePosition(candle);
		if (exitTriggered)
		{
			_previousClose = candle.ClosePrice;
			_previousSar = sar;
			return;
		}

		// Ignore signals on disabled trading days.
		if (!IsTradingDay(candle.OpenTime.DayOfWeek))
		{
			_previousClose = candle.ClosePrice;
			_previousSar = sar;
			return;
		}

		// Detect SAR flips confirmed by EMA alignment.
		var longSignal = false;
		var shortSignal = false;

		if (_previousSar is decimal prevSar && _previousClose != 0)
		{
			longSignal = candle.ClosePrice > sar &&
				_previousClose < prevSar &&
				fast > slow &&
				candle.ClosePrice > fastFilter;

			shortSignal = candle.ClosePrice < sar &&
				_previousClose > prevSar &&
				fast < slow &&
				candle.ClosePrice < fastFilter;
		}

		if (ReverseSignals)
		{
			var temp = longSignal;
			longSignal = shortSignal;
			shortSignal = temp;
		}

		// Optionally exit when an opposite setup appears.
		if (CloseOnOppositeSignal)
		{
			if (longSignal && Position < 0)
			{
				CloseShort(candle, candle.ClosePrice);
			}
			else if (shortSignal && Position > 0)
			{
				CloseLong(candle, candle.ClosePrice);
			}
		}

		// Allow only one fresh entry per candle.
		var canOpen = Position == 0 && (!_lastExitTime.HasValue || _lastExitTime < candle.OpenTime);

		if (canOpen && longSignal)
		{
			TryOpenLong(candle, sar);
		}
		else if (canOpen && shortSignal)
		{
			TryOpenShort(candle, sar);
		}

		_previousClose = candle.ClosePrice;
		_previousSar = sar;
	}

	private bool ManageActivePosition(ICandleMessage candle)
	{
		// Handle long positions.
		if (Position > 0 && _entryPrice.HasValue)
		{
			var exitPrice = 0m;

			if (_stopLossPrice > 0 && candle.LowPrice <= _stopLossPrice)
				exitPrice = _stopLossPrice;
			else if (_takeProfitPrice > 0 && candle.HighPrice >= _takeProfitPrice)
				exitPrice = _takeProfitPrice;

			if (exitPrice > 0)
			{
				CloseLong(candle, exitPrice);
				return true;
			}

			var profit = candle.ClosePrice - _entryPrice.Value;

			if (profit >= BreakevenTrigger)
			{
				var breakeven = _entryPrice.Value + BreakevenOffset;
				if (_stopLossPrice < breakeven)
					_stopLossPrice = breakeven;
			}

			if (profit >= TrailingTrigger)
			{
				var newStop = candle.ClosePrice - TrailingStep;
				if (_stopLossPrice < newStop)
					_stopLossPrice = newStop;
			}
		}
		// Handle short positions.
		else if (Position < 0 && _entryPrice.HasValue)
		{
			var exitPrice = 0m;

			if (_stopLossPrice > 0 && candle.HighPrice >= _stopLossPrice)
				exitPrice = _stopLossPrice;
			else if (_takeProfitPrice > 0 && candle.LowPrice <= _takeProfitPrice)
				exitPrice = _takeProfitPrice;

			if (exitPrice > 0)
			{
				CloseShort(candle, exitPrice);
				return true;
			}

			var profit = _entryPrice.Value - candle.ClosePrice;

			if (profit >= BreakevenTrigger)
			{
				var breakeven = _entryPrice.Value - BreakevenOffset;
				if (_stopLossPrice == 0 || _stopLossPrice > breakeven)
					_stopLossPrice = breakeven;
			}

			if (profit >= TrailingTrigger)
			{
				var newStop = candle.ClosePrice + TrailingStep;
				if (_stopLossPrice == 0 || _stopLossPrice > newStop)
					_stopLossPrice = newStop;
			}
		}

		return false;
	}

	private void TryOpenLong(ICandleMessage candle, decimal sar)
	{
		// Calculate stops and determine volume for a potential long entry.
		if (!TryCalculateStops(candle.ClosePrice, sar, true, out var stopPrice, out var takePrice, out var stopDistance))
			return;

		var volume = CalculateOrderVolume(stopDistance);
		if (volume <= 0)
			return;

		BuyMarket(volume);
		_entryPrice = candle.ClosePrice;
		_stopLossPrice = stopPrice;
		_takeProfitPrice = takePrice;
	}

	private void TryOpenShort(ICandleMessage candle, decimal sar)
	{
		// Calculate stops and determine volume for a potential short entry.
		if (!TryCalculateStops(candle.ClosePrice, sar, false, out var stopPrice, out var takePrice, out var stopDistance))
			return;

		var volume = CalculateOrderVolume(stopDistance);
		if (volume <= 0)
			return;

		SellMarket(volume);
		_entryPrice = candle.ClosePrice;
		_stopLossPrice = stopPrice;
		_takeProfitPrice = takePrice;
	}

	private void CloseLong(ICandleMessage candle, decimal exitPrice)
	{
		// Close long position with a market order.
		var volume = Position;
		if (volume <= 0)
			return;

		SellMarket(volume);
		FinalizeTrade(exitPrice, candle.OpenTime, false);
	}

	private void CloseShort(ICandleMessage candle, decimal exitPrice)
	{
		// Close short position with a market order.
		var volume = Math.Abs(Position);
		if (volume <= 0)
			return;

		BuyMarket(volume);
		FinalizeTrade(exitPrice, candle.OpenTime, true);
	}

	private void FinalizeTrade(decimal exitPrice, DateTimeOffset time, bool wasShort)
	{
		// Store result of the latest position for future sizing decisions.
		if (_entryPrice.HasValue)
		{
			_lastTradeWasLoss = !wasShort ? exitPrice <= _entryPrice.Value : exitPrice >= _entryPrice.Value;
		}
		else
		{
			_lastTradeWasLoss = false;
		}

		_entryPrice = null;
		_stopLossPrice = 0;
		_takeProfitPrice = 0;
		_lastExitTime = time;
	}

	private decimal CalculateOrderVolume(decimal stopDistance)
	{
		// Determine order size according to risk settings.
		if (stopDistance <= 0)
			return 0;

		var volume = Volume;
		var equity = Portfolio?.CurrentValue ?? 0m;
		var riskAmount = equity * (RiskPercent / 100m);

		if (riskAmount > 0)
		{
			var size = riskAmount / stopDistance;
			if (size > 0)
				volume = size;
		}

		if (UseMartingale && _lastTradeWasLoss)
			volume *= MartingaleMultiplier;

		return volume;
	}

	private bool TryCalculateStops(decimal entryPrice, decimal sar, bool isLong, out decimal stopPrice, out decimal takePrice, out decimal stopDistance)
	{
		// Build stop-loss and take-profit levels for the next order.
		stopPrice = 0m;
		takePrice = 0m;
		stopDistance = 0m;

		decimal distance;
		if (AutoStopLoss)
		{
			if (sar == 0)
				return false;

			distance = Math.Abs(entryPrice - sar) * StopLossCoefficient;
		}
		else
		{
			distance = ManualStopLoss;
		}

		if (distance <= 0)
			return false;

		var minStop = Math.Min(MinStopLoss, MaxStopLoss);
		var maxStop = Math.Max(MinStopLoss, MaxStopLoss);
		distance = Clamp(distance, minStop, maxStop);
		stopDistance = distance;

		stopPrice = isLong ? entryPrice - distance : entryPrice + distance;

		decimal targetDistance;
		if (AutoTakeProfit)
		{
			targetDistance = distance * TakeProfitCoefficient;
		}
		else
		{
			targetDistance = ManualTakeProfit;
		}

		if (targetDistance > 0)
			takePrice = isLong ? entryPrice + targetDistance : entryPrice - targetDistance;

		return true;
	}

	private static decimal Clamp(decimal value, decimal min, decimal max)
	{
		// Helper method to clamp decimal values within a range.
		if (value < min)
			return min;
		if (value > max)
			return max;
		return value;
	}

	private bool IsTradingDay(DayOfWeek day)
	{
		// Evaluate day-of-week trading switches.
		return day switch
		{
			DayOfWeek.Monday => TradeMonday,
			DayOfWeek.Tuesday => TradeTuesday,
			DayOfWeek.Wednesday => TradeWednesday,
			DayOfWeek.Thursday => TradeThursday,
			DayOfWeek.Friday => TradeFriday,
			_ => false
		};
	}
}
