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
/// Pull-back momentum strategy with multi-timeframe confirmations and dynamic risk management.
/// </summary>
public class PullBackStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _bounceSlowLength;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<int> _trailingStopTicks;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<int> _breakEvenTriggerTicks;
	private readonly StrategyParam<int> _breakEvenOffsetTicks;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;

	private WMA _baseFast = null!;
	private WMA _baseSlow = null!;
	private WMA _higherFast = null!;
	private WMA _higherSlow = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private readonly Queue<decimal> _momentumAbsHistory = new();

	private decimal? _entryPrice;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private decimal? _macdMain;
	private decimal? _macdSignal;
	private bool _bounceLong;
	private bool _bounceShort;

	/// <summary>
	/// Fast WMA length on the trading timeframe.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow WMA length on the trading timeframe.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Slow WMA length used on the confirmation timeframe.
	/// </summary>
	public int BounceSlowLength
	{
		get => _bounceSlowLength.Value;
		set => _bounceSlowLength.Value = value;
	}

	/// <summary>
	/// Momentum lookback on the higher timeframe.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// Minimal absolute momentum distance from 100 required for long trades.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Minimal absolute momentum distance from 100 required for short trades.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in ticks from the entry price.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Take-profit distance in ticks from the entry price.
	/// </summary>
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Enable trailing stop management.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in ticks.
	/// </summary>
	public int TrailingStopTicks
	{
		get => _trailingStopTicks.Value;
		set => _trailingStopTicks.Value = value;
	}

	/// <summary>
	/// Enable break-even adjustment.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Profit trigger in ticks to activate the break-even logic.
	/// </summary>
	public int BreakEvenTriggerTicks
	{
		get => _breakEvenTriggerTicks.Value;
		set => _breakEvenTriggerTicks.Value = value;
	}

	/// <summary>
	/// Offset in ticks added to the break-even stop.
	/// </summary>
	public int BreakEvenOffsetTicks
	{
		get => _breakEvenOffsetTicks.Value;
		set => _breakEvenOffsetTicks.Value = value;
	}

	/// <summary>
	/// Fast period for the MACD confirmation.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow period for the MACD confirmation.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal period for the MACD confirmation.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Trading timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Confirmation timeframe used for momentum and bounce detection.
	/// </summary>
	public DataType HigherCandleType
	{
		get => _higherCandleType.Value;
		set => _higherCandleType.Value = value;
	}

	/// <summary>
	/// Timeframe used to evaluate MACD direction.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="PullBackStrategy"/>.
	/// </summary>
	public PullBackStrategy()
	{
		_fastMaLength = Param(nameof(FastMaLength), 6)
		.SetGreaterThanZero()
		.SetDisplay("Fast WMA Length", "Length of the fast weighted MA on the trading timeframe", "Trend")
		.SetCanOptimize(true);

		_slowMaLength = Param(nameof(SlowMaLength), 85)
		.SetGreaterThanZero()
		.SetDisplay("Slow WMA Length", "Length of the slow weighted MA on the trading timeframe", "Trend")
		.SetCanOptimize(true);

		_bounceSlowLength = Param(nameof(BounceSlowLength), 200)
		.SetGreaterThanZero()
		.SetDisplay("Bounce Slow Length", "Slow weighted MA length on the confirmation timeframe", "Trend")
		.SetCanOptimize(true);

		_momentumLength = Param(nameof(MomentumLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Length", "Lookback for the momentum confirmation", "Filters")
		.SetCanOptimize(true);

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Buy Threshold", "Minimal |Momentum-100| for long entries", "Filters")
		.SetCanOptimize(true);

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Sell Threshold", "Minimal |Momentum-100| for short entries", "Filters")
		.SetCanOptimize(true);

		_stopLossTicks = Param(nameof(StopLossTicks), 200)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (ticks)", "Stop-loss distance expressed in ticks", "Risk")
		.SetCanOptimize(true);

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 500)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (ticks)", "Take-profit distance expressed in ticks", "Risk")
		.SetCanOptimize(true);

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
		.SetDisplay("Use Trailing Stop", "Enable trailing stop logic", "Risk");

		_trailingStopTicks = Param(nameof(TrailingStopTicks), 400)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Stop (ticks)", "Trailing stop distance in ticks", "Risk")
		.SetCanOptimize(true);

		_useBreakEven = Param(nameof(UseBreakEven), true)
		.SetDisplay("Use Break Even", "Enable break-even stop adjustment", "Risk");

		_breakEvenTriggerTicks = Param(nameof(BreakEvenTriggerTicks), 300)
		.SetGreaterThanZero()
		.SetDisplay("Break-Even Trigger", "Profit in ticks required to move the stop", "Risk")
		.SetCanOptimize(true);

		_breakEvenOffsetTicks = Param(nameof(BreakEvenOffsetTicks), 300)
		.SetGreaterThanZero()
		.SetDisplay("Break-Even Offset", "Offset in ticks added to the break-even stop", "Risk")
		.SetCanOptimize(true);

		_macdFastLength = Param(nameof(MacdFastLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA length for MACD", "Filters")
		.SetCanOptimize(true);

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA length for MACD", "Filters")
		.SetCanOptimize(true);

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA length for MACD", "Filters")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Trading Timeframe", "Primary timeframe for trade execution", "General");

		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Confirmation Timeframe", "Higher timeframe used for bounce and momentum", "General");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
		.SetDisplay("MACD Timeframe", "Timeframe used to evaluate MACD trend", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var result = new List<(Security, DataType)> { (Security, CandleType) };

		if (!HigherCandleType.Equals(CandleType))
		{
			result.Add((Security, HigherCandleType));
		}

		if (!MacdCandleType.Equals(CandleType) && !MacdCandleType.Equals(HigherCandleType))
		{
			result.Add((Security, MacdCandleType));
		}

		return result;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryPrice = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_macdMain = null;
		_macdSignal = null;
		_bounceLong = false;
		_bounceShort = false;
		_momentumAbsHistory.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		_baseFast = new WMA { Length = FastMaLength };
		_baseSlow = new WMA { Length = SlowMaLength };

		var baseSubscription = SubscribeCandles(CandleType);
		baseSubscription
		.Bind(_baseFast, _baseSlow, ProcessBaseCandle)
		.Start();

		_higherFast = new WMA { Length = FastMaLength };
		_higherSlow = new WMA { Length = BounceSlowLength };
		_momentum = new Momentum { Length = MomentumLength };

		var higherSubscription = SubscribeCandles(HigherCandleType);
		higherSubscription
		.Bind(_higherFast, _higherSlow, _momentum, ProcessHigherCandle)
		.Start();

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			ShortPeriod = MacdFastLength,
			LongPeriod = MacdSlowLength,
			SignalPeriod = MacdSignalLength
		};

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription
		.BindEx(_macd, ProcessMacd)
		.Start();

		StartProtection();

		base.OnStarted(time);
	}

	private void ProcessBaseCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_baseFast.IsFormed || !_baseSlow.IsFormed)
		return;

		var priceStep = Security.PriceStep ?? 0m;
		if (priceStep <= 0m)
		priceStep = 1m;

		if (Position != 0m)
		{
			ManagePosition(candle, priceStep);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var momentumLong = HasMomentumAbove(MomentumBuyThreshold);
		var momentumShort = HasMomentumAbove(MomentumSellThreshold);

		if (_bounceLong && fastValue > slowValue && momentumLong && IsMacdBullish())
		{
			_entryPrice = candle.ClosePrice;
			_stopLossPrice = StopLossTicks > 0 ? _entryPrice - StopLossTicks * priceStep : null;
			_takeProfitPrice = TakeProfitTicks > 0 ? _entryPrice + TakeProfitTicks * priceStep : null;
			BuyMarket();
			return;
		}

		if (_bounceShort && fastValue < slowValue && momentumShort && IsMacdBearish())
		{
			_entryPrice = candle.ClosePrice;
			_stopLossPrice = StopLossTicks > 0 ? _entryPrice + StopLossTicks * priceStep : null;
			_takeProfitPrice = TakeProfitTicks > 0 ? _entryPrice - TakeProfitTicks * priceStep : null;
			SellMarket();
		}
	}

	private void ProcessHigherCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_higherFast.IsFormed || !_higherSlow.IsFormed || !_momentum.IsFormed)
		return;

		_bounceLong = fastValue > slowValue && candle.LowPrice <= fastValue && candle.OpenPrice > fastValue;
		_bounceShort = fastValue < slowValue && candle.HighPrice >= fastValue && candle.OpenPrice < fastValue;

		var momentumAbs = Math.Abs(momentumValue - 100m);
		_momentumAbsHistory.Enqueue(momentumAbs);

		while (_momentumAbsHistory.Count > 3)
		_momentumAbsHistory.Dequeue();
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_macd.IsFormed || !indicatorValue.IsFinal)
		return;

		var macdValue = (MovingAverageConvergenceDivergenceSignalValue)indicatorValue;

		if (macdValue.Signal is not decimal signal || macdValue.Macd is not decimal macd)
		return;

		_macdSignal = signal;
		_macdMain = macd;
	}

	private void ManagePosition(ICandleMessage candle, decimal priceStep)
	{
		if (_entryPrice is null)
		_entryPrice = candle.ClosePrice;

		if (UseBreakEven && BreakEvenTriggerTicks > 0 && _entryPrice.HasValue)
		{
			var trigger = _entryPrice.Value + (Position > 0m ? BreakEvenTriggerTicks * priceStep : -BreakEvenTriggerTicks * priceStep);

			if ((Position > 0m && candle.HighPrice >= trigger) || (Position < 0m && candle.LowPrice <= trigger))
			{
				var newStop = _entryPrice.Value + (Position > 0m ? BreakEvenOffsetTicks * priceStep : -BreakEvenOffsetTicks * priceStep);

				if (_stopLossPrice is null || (Position > 0m && _stopLossPrice.Value < newStop) || (Position < 0m && _stopLossPrice.Value > newStop))
				_stopLossPrice = newStop;
			}
		}

		if (UseTrailingStop && TrailingStopTicks > 0)
		{
			var trail = Position > 0m ? candle.ClosePrice - TrailingStopTicks * priceStep : candle.ClosePrice + TrailingStopTicks * priceStep;

			if (_stopLossPrice is null || (Position > 0m && _stopLossPrice.Value < trail) || (Position < 0m && _stopLossPrice.Value > trail))
			_stopLossPrice = trail;
		}

		if (_stopLossPrice is not null)
		{
			if ((Position > 0m && candle.LowPrice <= _stopLossPrice.Value) || (Position < 0m && candle.HighPrice >= _stopLossPrice.Value))
			{
				if (Position > 0m)
				SellMarket(Math.Abs(Position));
				else
				BuyMarket(Math.Abs(Position));

				ResetPositionState();
				return;
			}
		}

		if (_takeProfitPrice is not null)
		{
			if ((Position > 0m && candle.HighPrice >= _takeProfitPrice.Value) || (Position < 0m && candle.LowPrice <= _takeProfitPrice.Value))
			{
				if (Position > 0m)
				SellMarket(Math.Abs(Position));
				else
				BuyMarket(Math.Abs(Position));

				ResetPositionState();
			}
		}
	}

	private bool HasMomentumAbove(decimal threshold)
	{
		return _momentumAbsHistory.Count >= 3 && _momentumAbsHistory.Any(value => value >= threshold);
	}

	private bool IsMacdBullish()
	{
		return _macd.IsFormed && _macdMain is decimal macd && _macdSignal is decimal signal && macd > signal;
	}

	private bool IsMacdBearish()
	{
		return _macd.IsFormed && _macdMain is decimal macd && _macdSignal is decimal signal && macd < signal;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}
}

