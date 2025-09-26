using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double Channel strategy converted from MetaTrader 4.
/// Replicates the double-channel indicator based breakout logic with optional risk management helpers.
/// </summary>
public class DoubleChannelEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<int> _indicatorShift;
	private readonly StrategyParam<bool> _openEverySignal;
	private readonly StrategyParam<bool> _closeInSignal;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenPoints;
	private readonly StrategyParam<decimal> _breakEvenAfterPoints;
	private readonly StrategyParam<bool> _autoLotSize;
	private readonly StrategyParam<decimal> _riskFactor;
	private readonly StrategyParam<decimal> _manualLotSize;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _timeStartTrade;
	private readonly StrategyParam<int> _timeEndTrade;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<decimal> _maxSpreadPoints;

	private readonly Queue<DoubleChannelValue> _signalDelay = new();

	private DoubleChannelIndicator _indicator;
	private decimal? _bestBidPrice;
	private decimal? _bestAskPrice;
	private decimal _entryPrice;
	private decimal _highestSinceEntry;
	private decimal _lowestSinceEntry;
	private bool _trailingActive;
	private decimal _trailingStopPrice;
	private bool _breakEvenActive;
	private decimal _breakEvenPrice;

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public DoubleChannelEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for calculations", "General");

		_channelPeriod = Param(nameof(ChannelPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Channel Period", "Number of candles for the double channel window", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 2);

		_indicatorShift = Param(nameof(IndicatorShift), 0)
			.SetDisplay("Indicator Shift", "Delay signals by the specified number of closed candles", "Indicator")
			.SetOptimize(0, 3, 1);

		_openEverySignal = Param(nameof(OpenEverySignal), true)
			.SetDisplay("Open Every Signal", "Allow stacking multiple positions on consecutive signals", "Trading");

		_closeInSignal = Param(nameof(CloseInSignal), false)
			.SetDisplay("Close On Opposite Signal", "Close the current position when an opposite arrow appears", "Trading");

		_useTakeProfit = Param(nameof(UseTakeProfit), false)
			.SetDisplay("Use Take Profit", "Enable take profit distance", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Absolute price distance for the target", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5m, 50m, 5m);

		_useStopLoss = Param(nameof(UseStopLoss), false)
			.SetDisplay("Use Stop Loss", "Enable protective stop distance", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Absolute price distance for the protective stop", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5m, 50m, 5m);

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
			.SetDisplay("Use Trailing Stop", "Enable dynamic trailing stop", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Distance used when trailing the position", "Risk")
			.SetOptimize(3m, 20m, 1m);

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Step", "Minimum improvement required before updating the trail", "Risk")
			.SetOptimize(0.5m, 5m, 0.5m);

		_useBreakEven = Param(nameof(UseBreakEven), false)
			.SetDisplay("Use Break Even", "Move the stop once the trade is in profit", "Risk");

		_breakEvenPoints = Param(nameof(BreakEvenPoints), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Break Even Offset", "Distance to keep after activating break even", "Risk")
			.SetOptimize(2m, 15m, 1m);

		_breakEvenAfterPoints = Param(nameof(BreakEvenAfterPoints), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Break Even Trigger", "Extra profit required before moving the stop", "Risk")
			.SetOptimize(1m, 10m, 1m);

		_autoLotSize = Param(nameof(AutoLotSize), true)
			.SetDisplay("Auto Lot Size", "Scale volume by the risk factor", "Money Management");

		_riskFactor = Param(nameof(RiskFactor), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Factor", "Multiplier applied when auto-sizing volume", "Money Management")
			.SetOptimize(0.5m, 3m, 0.5m);

		_manualLotSize = Param(nameof(ManualLotSize), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Manual Lot Size", "Base volume used when auto sizing is disabled", "Money Management")
			.SetOptimize(0.01m, 0.1m, 0.01m);

		_useTimeFilter = Param(nameof(UseTimeFilter), false)
			.SetDisplay("Use Time Filter", "Restrict trading to specific hours", "Schedule");

		_timeStartTrade = Param(nameof(TimeStartTrade), 0)
			.SetDisplay("Start Hour", "Hour of day to start trading (0-23)", "Schedule");

		_timeEndTrade = Param(nameof(TimeEndTrade), 0)
			.SetDisplay("End Hour", "Hour of day to stop trading (0-23)", "Schedule");

		_maxOrders = Param(nameof(MaxOrders), 0)
			.SetDisplay("Max Orders", "Maximum stacked positions per direction (0 disables)", "Trading");

		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 0m)
			.SetDisplay("Max Spread", "Maximum allowed spread before blocking entries", "Trading")
			.SetOptimize(0m, 5m, 0.5m);
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period of the double channel indicator.
	/// </summary>
	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
	}

	/// <summary>
	/// Number of closed candles to delay signals.
	/// </summary>
	public int IndicatorShift
	{
		get => _indicatorShift.Value;
		set => _indicatorShift.Value = value;
	}

	/// <summary>
	/// Allow stacking multiple positions.
	/// </summary>
	public bool OpenEverySignal
	{
		get => _openEverySignal.Value;
		set => _openEverySignal.Value = value;
	}

	/// <summary>
	/// Close current position on the opposite arrow.
	/// </summary>
	public bool CloseInSignal
	{
		get => _closeInSignal.Value;
		set => _closeInSignal.Value = value;
	}

	/// <summary>
	/// Enable take profit.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Take profit distance in absolute price units.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enable stop loss.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Stop loss distance in absolute price units.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Enable trailing stop logic.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Distance applied to the trailing stop.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Minimum improvement required before moving the trailing stop.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Enable break-even logic.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Offset applied once the position moves to break-even.
	/// </summary>
	public decimal BreakEvenPoints
	{
		get => _breakEvenPoints.Value;
		set => _breakEvenPoints.Value = value;
	}

	/// <summary>
	/// Additional profit required before break-even activates.
	/// </summary>
	public decimal BreakEvenAfterPoints
	{
		get => _breakEvenAfterPoints.Value;
		set => _breakEvenAfterPoints.Value = value;
	}

	/// <summary>
	/// Enable auto lot calculation.
	/// </summary>
	public bool AutoLotSize
	{
		get => _autoLotSize.Value;
		set => _autoLotSize.Value = value;
	}

	/// <summary>
	/// Risk multiplier used for auto lot sizing.
	/// </summary>
	public decimal RiskFactor
	{
		get => _riskFactor.Value;
		set => _riskFactor.Value = value;
	}

	/// <summary>
	/// Manual lot size when auto sizing is disabled.
	/// </summary>
	public decimal ManualLotSize
	{
		get => _manualLotSize.Value;
		set => _manualLotSize.Value = value;
	}

	/// <summary>
	/// Enable schedule filter.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Start hour for trading.
	/// </summary>
	public int TimeStartTrade
	{
		get => _timeStartTrade.Value;
		set => _timeStartTrade.Value = value;
	}

	/// <summary>
	/// End hour for trading.
	/// </summary>
	public int TimeEndTrade
	{
		get => _timeEndTrade.Value;
		set => _timeEndTrade.Value = value;
	}

	/// <summary>
	/// Maximum stacked positions per direction.
	/// </summary>
	public int MaxOrders
	{
		get => _maxOrders.Value;
		set => _maxOrders.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread.
	/// </summary>
	public decimal MaxSpreadPoints
	{
		get => _maxSpreadPoints.Value;
		set => _maxSpreadPoints.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_signalDelay.Clear();
		_indicator?.Reset();
		_bestBidPrice = null;
		_bestAskPrice = null;
		_entryPrice = 0m;
		_highestSinceEntry = 0m;
		_lowestSinceEntry = 0m;
		_trailingActive = false;
		_trailingStopPrice = 0m;
		_breakEvenActive = false;
		_breakEvenPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_indicator = new DoubleChannelIndicator
		{
			Length = ChannelPeriod
		};

		SubscribeLevel1()
			.Bind(OnLevel1)
			.Start();

		SubscribeCandles(CandleType)
			.BindEx(_indicator, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_indicator == null)
			return;

		if (indicatorValue is not DoubleChannelValue channelValue || !channelValue.IsFormed)
			return;

		if (_indicator.Length != ChannelPeriod)
			_indicator.Length = ChannelPeriod;

		if (IndicatorShift > 0)
		{
			_signalDelay.Enqueue(channelValue);
			if (_signalDelay.Count <= IndicatorShift)
				return;

			channelValue = _signalDelay.Dequeue();
		}
		else
		{
			_signalDelay.Clear();
		}

		if (Position != 0m)
		{
			if (ManageOpenPosition(candle))
				return;
		}
		else
		{
			ResetPositionState();
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!IsWithinTradingHours(candle.OpenTime))
			return;

		if (!IsSpreadAcceptable())
			return;

		if (channelValue.BuySignal)
		{
			if (CloseInSignal && Position < 0m)
			{
				if (ExitShort())
				{
					return;
				}
			}

			if (!OpenEverySignal && Position > 0m)
				return;

			var baseVolume = CalculateOrderVolume();
			if (!CanOpen(baseVolume))
				return;

			EnterLong(candle.ClosePrice, baseVolume);
			return;
		}

		if (channelValue.SellSignal)
		{
			if (CloseInSignal && Position > 0m)
			{
				if (ExitLong())
				{
					return;
				}
			}

			if (!OpenEverySignal && Position < 0m)
				return;

			var baseVolume = CalculateOrderVolume();
			if (!CanOpen(baseVolume))
				return;

			EnterShort(candle.ClosePrice, baseVolume);
		}
	}

	private bool ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			_highestSinceEntry = Math.Max(_highestSinceEntry, candle.HighPrice);
			_lowestSinceEntry = _lowestSinceEntry == 0m ? candle.LowPrice : Math.Min(_lowestSinceEntry, candle.LowPrice);

			if (UseStopLoss && StopLossPoints > 0m)
			{
				var stopPrice = _entryPrice - StopLossPoints;
				if (candle.LowPrice <= stopPrice)
					return ExitLong();
			}

			if (UseTakeProfit && TakeProfitPoints > 0m)
			{
				var targetPrice = _entryPrice + TakeProfitPoints;
				if (candle.HighPrice >= targetPrice)
					return ExitLong();
			}

			if (UseBreakEven && BreakEvenPoints > 0m && !_breakEvenActive)
			{
				var triggerPrice = _entryPrice + BreakEvenPoints + BreakEvenAfterPoints;
				if (candle.HighPrice >= triggerPrice)
				{
					_breakEvenActive = true;
					_breakEvenPrice = _entryPrice + BreakEvenPoints;
				}
			}

			if (_breakEvenActive && candle.LowPrice <= _breakEvenPrice)
				return ExitLong();

			if (UseTrailingStop && TrailingStopPoints > 0m && !_breakEvenActive)
			{
				var desiredStop = candle.ClosePrice - TrailingStopPoints;
				if (!_trailingActive || desiredStop > _trailingStopPrice + TrailingStepPoints)
				{
					_trailingStopPrice = desiredStop;
					_trailingActive = true;
				}

				if (_trailingActive && candle.LowPrice <= _trailingStopPrice)
					return ExitLong();
			}
		}
		else if (Position < 0m)
		{
			_highestSinceEntry = _highestSinceEntry == 0m ? candle.HighPrice : Math.Max(_highestSinceEntry, candle.HighPrice);
			_lowestSinceEntry = Math.Min(_lowestSinceEntry == 0m ? candle.LowPrice : _lowestSinceEntry, candle.LowPrice);

			if (UseStopLoss && StopLossPoints > 0m)
			{
				var stopPrice = _entryPrice + StopLossPoints;
				if (candle.HighPrice >= stopPrice)
					return ExitShort();
			}

			if (UseTakeProfit && TakeProfitPoints > 0m)
			{
				var targetPrice = _entryPrice - TakeProfitPoints;
				if (candle.LowPrice <= targetPrice)
					return ExitShort();
			}

			if (UseBreakEven && BreakEvenPoints > 0m && !_breakEvenActive)
			{
				var triggerPrice = _entryPrice - BreakEvenPoints - BreakEvenAfterPoints;
				if (candle.LowPrice <= triggerPrice)
				{
					_breakEvenActive = true;
					_breakEvenPrice = _entryPrice - BreakEvenPoints;
				}
			}

			if (_breakEvenActive && candle.HighPrice >= _breakEvenPrice)
				return ExitShort();

			if (UseTrailingStop && TrailingStopPoints > 0m && !_breakEvenActive)
			{
				var desiredStop = candle.ClosePrice + TrailingStopPoints;
				if (!_trailingActive || desiredStop < _trailingStopPrice - TrailingStepPoints)
				{
					_trailingStopPrice = desiredStop;
					_trailingActive = true;
				}

				if (_trailingActive && candle.HighPrice >= _trailingStopPrice)
					return ExitShort();
			}
		}
		else
		{
			ResetPositionState();
		}

		return false;
	}

	private void EnterLong(decimal price, decimal baseVolume)
	{
		var orderVolume = PrepareOrderVolume(baseVolume, Position < 0m ? Math.Abs(Position) : 0m);
		if (orderVolume <= 0m)
			return;

		BuyMarket(orderVolume);
		InitializePositionState(price);
	}

	private void EnterShort(decimal price, decimal baseVolume)
	{
		var orderVolume = PrepareOrderVolume(baseVolume, Position > 0m ? Position : 0m);
		if (orderVolume <= 0m)
			return;

		SellMarket(orderVolume);
		InitializePositionState(price);
	}

	private decimal PrepareOrderVolume(decimal baseVolume, decimal hedgeVolume)
	{
		if (baseVolume <= 0m)
			return 0m;

		var total = baseVolume + Math.Abs(hedgeVolume);
		return total;
	}

	private bool ExitLong()
	{
		var volume = Position;
		if (volume <= 0m)
			return false;

		SellMarket(volume);
		ResetPositionState();
		return true;
	}

	private bool ExitShort()
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return false;

		BuyMarket(volume);
		ResetPositionState();
		return true;
	}

	private void InitializePositionState(decimal price)
	{
		_entryPrice = price;
		_highestSinceEntry = price;
		_lowestSinceEntry = price;
		_trailingActive = false;
		_trailingStopPrice = 0m;
		_breakEvenActive = false;
		_breakEvenPrice = 0m;
	}

	private void ResetPositionState()
	{
		_entryPrice = 0m;
		_highestSinceEntry = 0m;
		_lowestSinceEntry = 0m;
		_trailingActive = false;
		_trailingStopPrice = 0m;
		_breakEvenActive = false;
		_breakEvenPrice = 0m;
	}

	private decimal CalculateOrderVolume()
	{
		var volume = ManualLotSize;
		if (AutoLotSize)
		{
			var factor = Math.Max(RiskFactor, 0.1m);
			volume *= factor;
		}

		return Math.Max(volume, 0m);
	}

	private bool CanOpen(decimal baseVolume)
	{
		if (baseVolume <= 0m)
			return false;

		if (MaxOrders <= 0)
			return true;

		var maxVolume = MaxOrders * baseVolume;
		return Math.Abs(Position) < maxVolume;
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		if (!UseTimeFilter)
			return true;

		var startHour = Math.Clamp(TimeStartTrade, 0, 23);
		var endHour = Math.Clamp(TimeEndTrade, 0, 23);
		var hour = time.Hour;

		if (startHour == endHour)
			return true;

		if (startHour < endHour)
			return hour >= startHour && hour < endHour;

		return hour >= startHour || hour < endHour;
	}

	private bool IsSpreadAcceptable()
	{
		if (MaxSpreadPoints <= 0m)
			return true;

		if (!_bestBidPrice.HasValue || !_bestAskPrice.HasValue)
			return false;

		var spread = _bestAskPrice.Value - _bestBidPrice.Value;
		if (spread <= MaxSpreadPoints)
			return true;

		LogInfo($"Spread {spread:0.#####} exceeds limit {MaxSpreadPoints:0.#####}.");
		return false;
	}

	private void OnLevel1(Level1ChangeMessage message)
	{
		_bestBidPrice = message.TryGetDecimal(Level1Fields.BestBidPrice) ?? _bestBidPrice;
		_bestAskPrice = message.TryGetDecimal(Level1Fields.BestAskPrice) ?? _bestAskPrice;
	}
}

/// <summary>
/// Indicator replicating the double channel buffers and arrow logic.
/// </summary>
public sealed class DoubleChannelIndicator : BaseIndicator<DoubleChannelValue>
{
	private readonly Queue<ICandleMessage> _window = new();
	private decimal _sumClose;
	private decimal _sumUpperPartA;
	private decimal _sumUpperPartB;
	private decimal _sumLowerPartC;
	private decimal _sumLowerPartD;
	private ChannelSnapshot? _previous;
	private ChannelSnapshot? _previous2;

	/// <summary>
	/// Lookback window length.
	/// </summary>
	public int Length { get; set; } = 14;

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_window.Clear();
		_sumClose = 0m;
		_sumUpperPartA = 0m;
		_sumUpperPartB = 0m;
		_sumLowerPartC = 0m;
		_sumLowerPartD = 0m;
		_previous = null;
		_previous2 = null;
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
			return new DoubleChannelValue(this, input, 0m, 0m, 0m, false, false, false);

		if (Length <= 0)
			return new DoubleChannelValue(this, input, 0m, 0m, 0m, false, false, false);

		_window.Enqueue(candle);
		_sumClose += candle.ClosePrice;
		_sumUpperPartA += 2m * candle.HighPrice - candle.ClosePrice;
		_sumUpperPartB += candle.HighPrice + candle.ClosePrice - candle.LowPrice;
		_sumLowerPartC += 2m * candle.LowPrice - candle.OpenPrice;
		_sumLowerPartD += candle.LowPrice + candle.OpenPrice - candle.HighPrice;

		if (_window.Count > Length)
		{
			var removed = _window.Dequeue();
			_sumClose -= removed.ClosePrice;
			_sumUpperPartA -= 2m * removed.HighPrice - removed.ClosePrice;
			_sumUpperPartB -= removed.HighPrice + removed.ClosePrice - removed.LowPrice;
			_sumLowerPartC -= 2m * removed.LowPrice - removed.OpenPrice;
			_sumLowerPartD -= removed.LowPrice + removed.OpenPrice - removed.HighPrice;
		}

		if (_window.Count < Length)
			return new DoubleChannelValue(this, input, 0m, 0m, 0m, false, false, false);

		var middle = _sumClose / Length;
		var avgUpperA = _sumUpperPartA / Length;
		var avgUpperB = _sumUpperPartB / Length;
		var avgLowerC = _sumLowerPartC / Length;
		var avgLowerD = _sumLowerPartD / Length;

		var upper = middle + (avgUpperA - avgUpperB);
		var lower = middle + (avgLowerC - avgLowerD);

		var snapshot = new ChannelSnapshot(
			candle.OpenPrice,
			candle.ClosePrice,
			candle.HighPrice,
			candle.LowPrice,
			upper,
			lower,
			middle);

		var previous = _previous;
		var previous2 = _previous2;

		_previous2 = previous;
		_previous = snapshot;

		var buySignal = false;
		var sellSignal = false;

		if (previous.HasValue && previous2.HasValue)
		{
			var p1 = previous.Value;
			var p2 = previous2.Value;

			if (p1.Lower > p1.Upper &&
				p2.Lower < p2.Upper &&
				p1.Upper > p1.Middle &&
				p1.Upper > p2.Upper &&
				p1.Lower > p2.Lower &&
				(p1.Lower - p1.Upper) > (p1.Upper - p1.Middle) &&
				p1.Close > p1.Lower &&
				p1.Open < p1.Close)
			{
				buySignal = true;
			}

			if (p1.Lower < p1.Upper &&
				p2.Lower > p2.Upper &&
				p1.Upper < p1.Middle &&
				p1.Upper < p2.Upper &&
				p1.Lower < p2.Lower &&
				(p1.Upper - p1.Lower) > (p1.Middle - p1.Upper) &&
				p1.Close < p1.Lower &&
				p1.Open > p1.Close)
			{
				sellSignal = true;
			}
		}

		return new DoubleChannelValue(this, input, upper, lower, middle, buySignal, sellSignal, true);
	}

	private readonly struct ChannelSnapshot
	{
		public ChannelSnapshot(decimal open, decimal close, decimal high, decimal low, decimal upper, decimal lower, decimal middle)
		{
			Open = open;
			Close = close;
			High = high;
			Low = low;
			Upper = upper;
			Lower = lower;
			Middle = middle;
		}

		public decimal Open { get; }
		public decimal Close { get; }
		public decimal High { get; }
		public decimal Low { get; }
		public decimal Upper { get; }
		public decimal Lower { get; }
		public decimal Middle { get; }
	}
}

/// <summary>
/// Indicator value returned by <see cref="DoubleChannelIndicator"/>.
/// </summary>
public sealed class DoubleChannelValue : ComplexIndicatorValue
{
	/// <summary>
	/// Initializes a new instance of the value container.
	/// </summary>
	public DoubleChannelValue(IIndicator indicator, IIndicatorValue input, decimal upper, decimal lower, decimal middle, bool buySignal, bool sellSignal, bool isFormed)
		: base(indicator, input,
			(nameof(Upper), upper),
			(nameof(Lower), lower),
			(nameof(Middle), middle))
	{
		Upper = upper;
		Lower = lower;
		Middle = middle;
		BuySignal = buySignal;
		SellSignal = sellSignal;
		IsFormed = isFormed;
	}

	/// <summary>
	/// Upper channel line.
	/// </summary>
	public decimal Upper { get; }

	/// <summary>
	/// Lower channel line.
	/// </summary>
	public decimal Lower { get; }

	/// <summary>
	/// Central moving average.
	/// </summary>
	public decimal Middle { get; }

	/// <summary>
	/// True when the indicator emits a buy arrow.
	/// </summary>
	public bool BuySignal { get; }

	/// <summary>
	/// True when the indicator emits a sell arrow.
	/// </summary>
	public bool SellSignal { get; }

	/// <summary>
	/// True when the indicator has enough history to produce signals.
	/// </summary>
	public bool IsFormed { get; }
}
