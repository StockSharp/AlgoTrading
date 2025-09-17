using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy with break-even and trailing stop management.
/// </summary>
public class MovingAveragesStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _breakEvenLevelPoints;
	private readonly StrategyParam<int> _breakEvenProfitPoints;
	private readonly StrategyParam<int> _trailingStartPoints;
	private readonly StrategyParam<int> _trailingDistancePoints;
	private readonly StrategyParam<bool> _closeOnOppositeSignal;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortTakeProfitPrice;
	private bool _longBreakEvenActivated;
	private bool _shortBreakEvenActivated;
	private bool? _wasFastAboveSlow;

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
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
	/// Trading volume expressed in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in price points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Profit distance that activates the break-even stop.
	/// </summary>
	public int BreakEvenLevelPoints
	{
		get => _breakEvenLevelPoints.Value;
		set => _breakEvenLevelPoints.Value = value;
	}

	/// <summary>
	/// Extra profit added when moving the stop to break-even.
	/// </summary>
	public int BreakEvenProfitPoints
	{
		get => _breakEvenProfitPoints.Value;
		set => _breakEvenProfitPoints.Value = value;
	}

	/// <summary>
	/// Profit distance required before enabling the trailing stop.
	/// </summary>
	public int TrailingStartPoints
	{
		get => _trailingStartPoints.Value;
		set => _trailingStartPoints.Value = value;
	}

	/// <summary>
	/// Distance maintained by the trailing stop once activated.
	/// </summary>
	public int TrailingDistancePoints
	{
		get => _trailingDistancePoints.Value;
		set => _trailingDistancePoints.Value = value;
	}

	/// <summary>
	/// Indicates whether to close the current position on opposite crossover.
	/// </summary>
	public bool CloseOnOppositeSignal
	{
		get => _closeOnOppositeSignal.Value;
		set => _closeOnOppositeSignal.Value = value;
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
	/// Initializes strategy parameters.
	/// </summary>
	public MovingAveragesStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Period", "Period for the fast moving average", "MA Settings")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 28)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Period", "Period for the slow moving average", "MA Settings")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 5);

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Volume used for market orders", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_stopLossPoints = Param(nameof(StopLossPoints), 500)
			.SetDisplay("Stop Loss (points)", "Stop loss distance expressed in points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(100, 1000, 50);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 1000)
			.SetDisplay("Take Profit (points)", "Take profit distance expressed in points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(200, 2000, 100);

		_breakEvenLevelPoints = Param(nameof(BreakEvenLevelPoints), 300)
			.SetDisplay("Break-even Trigger", "Profit required before moving the stop to break-even", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(100, 800, 50);

		_breakEvenProfitPoints = Param(nameof(BreakEvenProfitPoints), 10)
			.SetDisplay("Break-even Offset", "Additional points kept as profit after break-even", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0, 50, 5);

		_trailingStartPoints = Param(nameof(TrailingStartPoints), 500)
			.SetDisplay("Trailing Start", "Profit distance before activating the trailing stop", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(200, 1000, 50);

		_trailingDistancePoints = Param(nameof(TrailingDistancePoints), 100)
			.SetDisplay("Trailing Distance", "Distance maintained by the trailing stop", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(50, 300, 25);

		_closeOnOppositeSignal = Param(nameof(CloseOnOppositeSignal), true)
			.SetDisplay("Close On Opposite", "Close position when an opposite crossover appears", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for calculations", "General");
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
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetState();

		var fastMa = new SMA { Length = FastPeriod };
		var slowMa = new SMA { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		ManageActivePositions(candle);

		var isFastAboveSlow = fastValue >= slowValue;
		var buySignal = isFastAboveSlow && _wasFastAboveSlow.HasValue && !_wasFastAboveSlow.Value;
		var sellSignal = !isFastAboveSlow && _wasFastAboveSlow.HasValue && _wasFastAboveSlow.Value;

		if (CloseOnOppositeSignal)
		{
			if (buySignal && Position < 0m)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				LogInfo("Closed short position on opposite signal.");
			}
			else if (sellSignal && Position > 0m)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
				LogInfo("Closed long position on opposite signal.");
			}
		}

		if (buySignal && Position == 0m)
		{
			var volume = TradeVolume;
			if (volume > 0m)
			{
				BuyMarket(volume);
				_longEntryPrice = candle.ClosePrice;
				InitializeLongTargets();
				LogInfo($"Opened long position at {candle.ClosePrice:0.#####}.");
			}
		}
		else if (sellSignal && Position == 0m)
		{
			var volume = TradeVolume;
			if (volume > 0m)
			{
				SellMarket(volume);
				_shortEntryPrice = candle.ClosePrice;
				InitializeShortTargets();
				LogInfo($"Opened short position at {candle.ClosePrice:0.#####}.");
			}
		}

		_wasFastAboveSlow = isFastAboveSlow;
	}

	private void ManageActivePositions(ICandleMessage candle)
	{
		var step = GetPriceStep();

		if (Position > 0m && _longEntryPrice.HasValue)
		{
			if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Long stop-loss triggered at {_longStopPrice.Value:0.#####}.");
				ResetLongState();
				return;
			}

			if (_longTakeProfitPrice.HasValue && candle.HighPrice >= _longTakeProfitPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Long take-profit triggered at {_longTakeProfitPrice.Value:0.#####}.");
				ResetLongState();
				return;
			}

			if (BreakEvenLevelPoints > 0 && !_longBreakEvenActivated)
			{
				var trigger = _longEntryPrice.Value + BreakEvenLevelPoints * step;
				if (candle.HighPrice >= trigger)
				{
					var breakEvenPrice = _longEntryPrice.Value + BreakEvenProfitPoints * step;
					_longStopPrice = _longStopPrice.HasValue
						? Math.Max(_longStopPrice.Value, breakEvenPrice)
						: breakEvenPrice;
					_longBreakEvenActivated = true;
					LogInfo($"Break-even activated for long position at {breakEvenPrice:0.#####}.");
				}
			}

			if (TrailingStartPoints > 0 && TrailingDistancePoints > 0)
			{
				var trailingTrigger = _longEntryPrice.Value + TrailingStartPoints * step;
				if (candle.HighPrice >= trailingTrigger)
				{
					var newStop = candle.HighPrice - TrailingDistancePoints * step;
					if (!_longStopPrice.HasValue || newStop > _longStopPrice.Value)
					{
						_longStopPrice = newStop;
						LogInfo($"Trailing stop for long position moved to {newStop:0.#####}.");
					}
				}
			}
		}
		else if (Position < 0m && _shortEntryPrice.HasValue)
		{
			if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short stop-loss triggered at {_shortStopPrice.Value:0.#####}.");
				ResetShortState();
				return;
			}

			if (_shortTakeProfitPrice.HasValue && candle.LowPrice <= _shortTakeProfitPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short take-profit triggered at {_shortTakeProfitPrice.Value:0.#####}.");
				ResetShortState();
				return;
			}

			if (BreakEvenLevelPoints > 0 && !_shortBreakEvenActivated)
			{
				var trigger = _shortEntryPrice.Value - BreakEvenLevelPoints * step;
				if (candle.LowPrice <= trigger)
				{
					var breakEvenPrice = _shortEntryPrice.Value - BreakEvenProfitPoints * step;
					_shortStopPrice = _shortStopPrice.HasValue
						? Math.Min(_shortStopPrice.Value, breakEvenPrice)
						: breakEvenPrice;
					_shortBreakEvenActivated = true;
					LogInfo($"Break-even activated for short position at {breakEvenPrice:0.#####}.");
				}
			}

			if (TrailingStartPoints > 0 && TrailingDistancePoints > 0)
			{
				var trailingTrigger = _shortEntryPrice.Value - TrailingStartPoints * step;
				if (candle.LowPrice <= trailingTrigger)
				{
					var newStop = candle.LowPrice + TrailingDistancePoints * step;
					if (!_shortStopPrice.HasValue || newStop < _shortStopPrice.Value)
					{
						_shortStopPrice = newStop;
						LogInfo($"Trailing stop for short position moved to {newStop:0.#####}.");
					}
				}
			}
		}
	}

	private void InitializeLongTargets()
	{
		var step = GetPriceStep();
		_longStopPrice = StopLossPoints > 0 ? _longEntryPrice - StopLossPoints * step : null;
		_longTakeProfitPrice = TakeProfitPoints > 0 ? _longEntryPrice + TakeProfitPoints * step : null;
		_longBreakEvenActivated = false;
	}

	private void InitializeShortTargets()
	{
		var step = GetPriceStep();
		_shortStopPrice = StopLossPoints > 0 ? _shortEntryPrice + StopLossPoints * step : null;
		_shortTakeProfitPrice = TakeProfitPoints > 0 ? _shortEntryPrice - TakeProfitPoints * step : null;
		_shortBreakEvenActivated = false;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 1m;
	}

	private void ResetState()
	{
		ResetLongState();
		ResetShortState();
		_wasFastAboveSlow = null;
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakeProfitPrice = null;
		_longBreakEvenActivated = false;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
		_shortBreakEvenActivated = false;
	}
}
