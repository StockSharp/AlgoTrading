using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fast and slow Relative Vigor Index crossover strategy.
/// Opens a long position when the main RVI line crosses above the signal line within the trading window,
/// and opens a short position on the opposite crossover.
/// Includes optional stop-loss, take-profit, and trailing stop distances defined in pips.
/// </summary>
public class FastSlowRviCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _rviPeriod;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _stopTime;

	private RelativeVigorIndex _rvi = null!;
	private SimpleMovingAverage _signal = null!;

	private decimal? _previousRvi;
	private decimal? _previousSignal;
	private DateTimeOffset? _lastSignalBar;
	private decimal _pipSize;

	/// <summary>
	/// RVI calculation period.
	/// </summary>
	public int RviPeriod
	{
		get => _rviPeriod.Value;
		set => _rviPeriod.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
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
	/// Trailing step distance in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Trading session start time.
	/// </summary>
	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Trading session stop time.
	/// </summary>
	public TimeSpan StopTime
	{
		get => _stopTime.Value;
		set => _stopTime.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="FastSlowRviCrossoverStrategy"/>.
	/// </summary>
	public FastSlowRviCrossoverStrategy()
	{
		_rviPeriod = Param(nameof(RviPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RVI Period", "Period for the Relative Vigor Index", "Indicators")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 10m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 10m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk")
			.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 2m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Step (pips)", "Trailing step distance in pips", "Risk")
			.SetCanOptimize(true);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for analysis", "General");

		_startTime = Param(nameof(StartTime), new TimeSpan(0, 0, 0))
			.SetDisplay("Start Time", "Trading session start", "General");

		_stopTime = Param(nameof(StopTime), new TimeSpan(23, 59, 0))
			.SetDisplay("Stop Time", "Trading session end", "General");
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

		_rvi = default!;
		_signal = default!;
		_previousRvi = default;
		_previousSignal = default;
		_lastSignalBar = default;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_rvi = new RelativeVigorIndex
		{
			Length = RviPeriod
		};

		_signal = new SimpleMovingAverage
		{
			Length = 4
		};

		_pipSize = GetPipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rvi, _signal, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: TakeProfitPips > 0m ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute) : null,
			stopLoss: StopLossPips > 0m ? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute) : null,
			trailingStop: TrailingStopPips > 0m ? new Unit(TrailingStopPips * _pipSize, UnitTypes.Absolute) : null,
			trailingStep: TrailingStopPips > 0m && TrailingStepPips > 0m ? new Unit(TrailingStepPips * _pipSize, UnitTypes.Absolute) : null,
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rvi);
			DrawIndicator(area, _signal);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rviValue, decimal signalValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rvi.IsFormed || !_signal.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!IsWithinTradingWindow(candle.CloseTime))
			return;

		var longSignal = _previousRvi <= _previousSignal && rviValue > signalValue;
		var shortSignal = _previousRvi >= _previousSignal && rviValue < signalValue;

		if (longSignal && Position <= 0 && IsNewSignalBar(candle))
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
			_lastSignalBar = candle.OpenTime;
		}
		else if (shortSignal && Position >= 0 && IsNewSignalBar(candle))
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			SellMarket(volume);
			_lastSignalBar = candle.OpenTime;
		}

		_previousRvi = rviValue;
		_previousSignal = signalValue;
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var start = StartTime;
		var stop = StopTime;

		if (start == stop)
			return true;

		var current = time.TimeOfDay;

		if (start < stop)
			return current >= start && current < stop;

		return current >= start || current < stop;
	}

	private bool IsNewSignalBar(ICandleMessage candle)
	{
		if (_lastSignalBar == null)
			return true;

		return candle.OpenTime > _lastSignalBar;
	}

	private decimal GetPipSize()
	{
		var step = Security.PriceStep ?? 0.0001m;
		var decimals = Security.Decimals;

		return decimals is 3 or 5 ? step * 10m : step;
	}
}
