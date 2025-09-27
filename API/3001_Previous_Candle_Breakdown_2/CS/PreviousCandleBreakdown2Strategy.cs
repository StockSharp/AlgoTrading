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
/// Breakout strategy that trades when price exceeds the previous candle range with optional MA and time filters.
/// </summary>
public class PreviousCandleBreakdown2Strategy : Strategy
{
	private readonly StrategyParam<int> _indentPips;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _fastShift;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _slowShift;
	private readonly StrategyParam<MaMethods> _maMethod;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _profitClose;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;
	private readonly StrategyParam<DataType> _candleType;

	private RollingWindow<decimal> _fastWindow;
	private RollingWindow<decimal> _slowWindow;
	private IIndicator _fastIndicator;
	private IIndicator _slowIndicator;

	private ICandleMessage _previousCandle;
	private DateTimeOffset? _lastBuyReference;
	private DateTimeOffset? _lastSellReference;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _trailPrice;
	private bool _trailingActive;

	/// <summary>
	/// Offset in pips added above/below the previous candle extremes.
	/// </summary>
	public int IndentPips
	{
		get => _indentPips.Value;
		set => _indentPips.Value = value;
	}

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Shift for the fast moving average.
	/// </summary>
	public int FastShift
	{
		get => _fastShift.Value;
		set => _fastShift.Value = value;
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Shift for the slow moving average.
	/// </summary>
	public int SlowShift
	{
		get => _slowShift.Value;
		set => _slowShift.Value = value;
	}

	/// <summary>
	/// Moving average calculation method.
	/// </summary>
	public MaMethods MaMethods
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Stop-loss size in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit size in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing stop step in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Profit threshold that closes all positions.
	/// </summary>
	public decimal ProfitClose
	{
		get => _profitClose.Value;
		set => _profitClose.Value = value;
	}

	/// <summary>
	/// Maximum absolute position size per direction.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Fixed order volume used when risk is disabled.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Risk percentage used for dynamic sizing when stop-loss is defined.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Trading window start time (exchange time).
	/// </summary>
	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Trading window end time (exchange time).
	/// </summary>
	public TimeSpan EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	/// <summary>
	/// Candle type used for signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	private bool HasMaFilter => FastPeriod > 0 && SlowPeriod > 0;

	/// <summary>
	/// Initializes a new instance of the <see cref="PreviousCandleBreakdown2Strategy"/> class.
	/// </summary>
	public PreviousCandleBreakdown2Strategy()
	{
		_indentPips = Param(nameof(IndentPips), 10)
		.SetNotNegative()
		.SetDisplay("Indent (pips)", "Price offset beyond the previous candle", "Entry");

		_fastPeriod = Param(nameof(FastPeriod), 10)
		.SetNotNegative()
		.SetDisplay("Fast MA", "Fast moving average period", "Filters");

		_fastShift = Param(nameof(FastShift), 3)
		.SetNotNegative()
		.SetDisplay("Fast Shift", "Shift for fast MA", "Filters");

		_slowPeriod = Param(nameof(SlowPeriod), 30)
		.SetNotNegative()
		.SetDisplay("Slow MA", "Slow moving average period", "Filters");

		_slowShift = Param(nameof(SlowShift), 0)
		.SetNotNegative()
		.SetDisplay("Slow Shift", "Shift for slow MA", "Filters");

		_maMethod = Param(nameof(MaMethods), MaMethods.Simple)
		.SetDisplay("MA Method", "Moving average calculation method", "Filters");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Stop-loss size in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 150m)
		.SetNotNegative()
		.SetDisplay("Take Profit", "Take-profit size in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 15m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop", "Trailing distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetNotNegative()
		.SetDisplay("Trailing Step", "Minimum move before trailing adjusts", "Risk");

		_profitClose = Param(nameof(ProfitClose), 100m)
		.SetNotNegative()
		.SetDisplay("Profit Close", "Close all positions when profit reached", "Risk");

		_maxPositions = Param(nameof(MaxPositions), 10)
		.SetGreaterThanZero()
		.SetDisplay("Max Position", "Maximum absolute position per direction", "General");

		_orderVolume = Param(nameof(OrderVolume), 0m)
		.SetNotNegative()
		.SetDisplay("Order Volume", "Fixed order volume", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 5m)
		.SetNotNegative()
		.SetDisplay("Risk %", "Risk percent when using stop-loss", "Risk");

		_startTime = Param(nameof(StartTime), new TimeSpan(9, 9, 0))
		.SetDisplay("Start Time", "Start of trading window", "Time Filter");

		_endTime = Param(nameof(EndTime), new TimeSpan(19, 19, 0))
		.SetDisplay("End Time", "End of trading window", "Time Filter");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Candle timeframe used for signals", "General");
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

		_fastWindow = null;
		_slowWindow = null;
		_fastIndicator = null;
		_slowIndicator = null;
		_previousCandle = null;
		_lastBuyReference = null;
		_lastSellReference = null;
		_stopPrice = 0m;
		_takePrice = 0m;
		_trailPrice = 0m;
		_trailingActive = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		if (HasMaFilter)
		{
			_fastIndicator = CreateMovingAverage(FastPeriod);
			_slowIndicator = CreateMovingAverage(SlowPeriod);

			_fastWindow = new RollingWindow<decimal>(Math.Max(1, FastShift + 1));
			_slowWindow = new RollingWindow<decimal>(Math.Max(1, SlowShift + 1));

			subscription
			.Bind(_fastIndicator, _slowIndicator, ProcessCandleWithMa)
			.Start();

			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _fastIndicator);
				DrawIndicator(area, _slowIndicator);
				DrawOwnTrades(area);
			}
		}
		else
		{
			subscription
			.Bind(ProcessCandleWithoutMa)
			.Start();

			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawOwnTrades(area);
			}
		}
	}

	private void ProcessCandleWithMa(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_fastWindow!.Add(fastValue);
		_slowWindow!.Add(slowValue);

		if (_fastIndicator?.IsFormed == false || _slowIndicator?.IsFormed == false)
		return;

		var fastShifted = GetShiftedValue(_fastWindow, FastShift);
		var slowShifted = GetShiftedValue(_slowWindow, SlowShift);

		if (fastShifted is null || slowShifted is null)
		return;

		ProcessCandleCore(candle, fastShifted, slowShifted);
	}

	private void ProcessCandleWithoutMa(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		ProcessCandleCore(candle, null, null);
	}

	private void ProcessCandleCore(ICandleMessage candle, decimal? fastValue, decimal? slowValue)
	{
		if (ProfitClose > 0m && PnL >= ProfitClose && Position != 0)
		{
			ExitAllPositions();
		}

		if (_previousCandle != null)
		{
			var timeOk = IsWithinTradingWindow(candle.CloseTime);
			var pipSize = GetPipSize();

			if (pipSize > 0m && timeOk)
			{
				var breakoutHigh = _previousCandle.HighPrice + IndentPips * pipSize;
				var breakoutLow = _previousCandle.LowPrice - IndentPips * pipSize;

				var longAllowed = fastValue is null || slowValue is null || fastValue > slowValue;
				var shortAllowed = fastValue is null || slowValue is null || fastValue < slowValue;

				if (longAllowed && Math.Max(0m, Position) < MaxPositions && _lastBuyReference != _previousCandle.OpenTime)
				{
					if (candle.HighPrice >= breakoutHigh && EnterLong(candle, pipSize))
					{
						_lastBuyReference = _previousCandle.OpenTime;
					}
				}

				if (shortAllowed && Math.Max(0m, -Position) < MaxPositions && _lastSellReference != _previousCandle.OpenTime)
				{
					if (candle.LowPrice <= breakoutLow && EnterShort(candle, pipSize))
					{
						_lastSellReference = _previousCandle.OpenTime;
					}
				}
			}

			ManageOpenPositions(candle, pipSize);
		}

		_previousCandle = (ICandleMessage)candle.Clone();
	}

	private void ManageOpenPositions(ICandleMessage candle, decimal pipSize)
	{
		if (Position > 0)
		{
			var entryPrice = Position.AveragePrice ?? candle.ClosePrice;

			if (_stopPrice == 0m && StopLossPips > 0m)
			_stopPrice = entryPrice - StopLossPips * pipSize;

			if (_takePrice == 0m && TakeProfitPips > 0m)
			_takePrice = entryPrice + TakeProfitPips * pipSize;

			ApplyTrailingForLong(candle, entryPrice, pipSize);

			if (_stopPrice > 0m && candle.LowPrice <= _stopPrice)
			{
				SellMarket(Position);
				ResetStops();
				return;
			}

			if (_takePrice > 0m && candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
				ResetStops();
			}
		}
		else if (Position < 0)
		{
			var entryPrice = Position.AveragePrice ?? candle.ClosePrice;

			if (_stopPrice == 0m && StopLossPips > 0m)
			_stopPrice = entryPrice + StopLossPips * pipSize;

			if (_takePrice == 0m && TakeProfitPips > 0m)
			_takePrice = entryPrice - TakeProfitPips * pipSize;

			ApplyTrailingForShort(candle, entryPrice, pipSize);

			if (_stopPrice > 0m && candle.HighPrice >= _stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				ResetStops();
				return;
			}

			if (_takePrice > 0m && candle.LowPrice <= _takePrice)
			{
				BuyMarket(Math.Abs(Position));
				ResetStops();
			}
		}
		else
		{
			ResetStops();
		}
	}

	private bool EnterLong(ICandleMessage candle, decimal pipSize)
	{
		var volume = GetOrderVolume(true);
		if (volume <= 0m)
		return false;

		BuyMarket(volume);

		var entryPrice = candle.ClosePrice;
		_stopPrice = StopLossPips > 0m ? entryPrice - StopLossPips * pipSize : 0m;
		_takePrice = TakeProfitPips > 0m ? entryPrice + TakeProfitPips * pipSize : 0m;
		_trailPrice = entryPrice;
		_trailingActive = false;

		return true;
	}

	private bool EnterShort(ICandleMessage candle, decimal pipSize)
	{
		var volume = GetOrderVolume(false);
		if (volume <= 0m)
		return false;

		SellMarket(volume);

		var entryPrice = candle.ClosePrice;
		_stopPrice = StopLossPips > 0m ? entryPrice + StopLossPips * pipSize : 0m;
		_takePrice = TakeProfitPips > 0m ? entryPrice - TakeProfitPips * pipSize : 0m;
		_trailPrice = entryPrice;
		_trailingActive = false;

		return true;
	}

	private void ApplyTrailingForLong(ICandleMessage candle, decimal entryPrice, decimal pipSize)
	{
		if (TrailingStopPips <= 0m)
		return;

		var distance = TrailingStopPips * pipSize;
		var step = TrailingStepPips * pipSize;

		if (!_trailingActive)
		{
			if (candle.HighPrice - entryPrice >= distance)
			{
				_trailingActive = true;
				_trailPrice = candle.HighPrice;
				var newStop = candle.HighPrice - distance;
				if (newStop > _stopPrice)
				_stopPrice = newStop;
			}
		}
		else
		{
			var newHigh = Math.Max(_trailPrice, candle.HighPrice);
			if (newHigh - _trailPrice >= step)
			{
				_trailPrice = newHigh;
				var newStop = newHigh - distance;
				if (newStop > _stopPrice)
				_stopPrice = newStop;
			}
		}
	}

	private void ApplyTrailingForShort(ICandleMessage candle, decimal entryPrice, decimal pipSize)
	{
		if (TrailingStopPips <= 0m)
		return;

		var distance = TrailingStopPips * pipSize;
		var step = TrailingStepPips * pipSize;

		if (!_trailingActive)
		{
			if (entryPrice - candle.LowPrice >= distance)
			{
				_trailingActive = true;
				_trailPrice = candle.LowPrice;
				var newStop = candle.LowPrice + distance;
				if (_stopPrice == 0m || newStop < _stopPrice)
				_stopPrice = newStop;
			}
		}
		else
		{
			var newLow = Math.Min(_trailPrice, candle.LowPrice);
			if (_trailPrice - newLow >= step)
			{
				_trailPrice = newLow;
				var newStop = newLow + distance;
				if (_stopPrice == 0m || newStop < _stopPrice)
				_stopPrice = newStop;
			}
		}
	}

	private void ExitAllPositions()
	{
		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		ResetStops();
	}

	private void ResetStops()
	{
		_stopPrice = 0m;
		_takePrice = 0m;
		_trailPrice = 0m;
		_trailingActive = false;
	}

	private decimal? GetShiftedValue(RollingWindow<decimal> window, int shift)
	{
		if (shift < 0 || window.Count <= shift)
		return null;

		return window[shift];
	}

	private IIndicator CreateMovingAverage(int period)
	{
		return MaMethods switch
		{
			MaMethods.Simple => new SimpleMovingAverage { Length = period },
			MaMethods.Exponential => new ExponentialMovingAverage { Length = period },
			MaMethods.Smoothed => new SmoothedMovingAverage { Length = period },
			MaMethods.Weighted => new WeightedMovingAverage { Length = period },
			_ => new SimpleMovingAverage { Length = period }
		};
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var start = StartTime;
		var end = EndTime;
		var current = time.TimeOfDay;

		return start <= end
		? current >= start && current <= end
		: current >= start || current <= end;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step == 0m)
		return 0m;

		var decimals = Security?.Decimals ?? 0;
		return (decimals == 3 || decimals == 5) ? step * 10m : step;
	}

	private decimal GetOrderVolume(bool isLong)
	{
		var current = isLong ? Math.Max(0m, Position) : Math.Max(0m, -Position);
		var remaining = MaxPositions - current;
		if (remaining <= 0m)
		return 0m;

		if (OrderVolume > 0m)
		return Math.Min(OrderVolume, remaining);

		if (RiskPercent > 0m && StopLossPips > 0m)
		{
			var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
			var pipSize = GetPipSize();
			var stop = StopLossPips * pipSize;

			if (portfolioValue > 0m && pipSize > 0m && stop > 0m)
			{
				var riskAmount = portfolioValue * (RiskPercent / 100m);
				var volume = riskAmount / stop;
				return Math.Min(volume, remaining);
			}
		}

		return Math.Min(Volume > 0m ? Volume : 1m, remaining);
	}

	/// <summary>
	/// Moving average method enumeration matching the original MQL parameters.
	/// </summary>
	public enum MaMethods
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted
	}
}

