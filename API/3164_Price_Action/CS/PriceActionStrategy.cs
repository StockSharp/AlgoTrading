using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Price action strategy converted from the MetaTrader PRICE_ACTION expert advisor.
/// Combines fractal patterns with weighted moving averages, momentum and MACD filters.
/// </summary>
public class PriceActionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _breakEvenTriggerPoints;
	private readonly StrategyParam<decimal> _breakEvenOffsetPoints;
	private readonly StrategyParam<int> _fractalLifetime;
	private readonly StrategyParam<decimal> _maxPositionUnits;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<bool> _useTakeProfit;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private readonly Queue<(decimal high, decimal low)> _fractalWindow = new();
	private decimal? _lastDownFractal;
	private decimal? _lastUpFractal;
	private int _downFractalAge;
	private int _upFractalAge;
	private bool _newDownFractal;
	private bool _newUpFractal;

	private decimal _priceStep;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal _trailingDistance;
	private decimal _breakEvenTrigger;
	private decimal _breakEvenOffset;

	private decimal? _entryPrice;
	private decimal _highestSinceEntry;
	private decimal _lowestSinceEntry;
	private decimal _lastPosition;

	/// <summary>
	/// Initializes a new instance of <see cref="PriceActionStrategy"/>.
	/// </summary>
	public PriceActionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Signal Candle", "Candle type used for analysis and trading", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
		.SetDisplay("Fast LWMA", "Length of the fast weighted moving average", "Indicators")
		.SetCanOptimize(true);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
		.SetDisplay("Slow LWMA", "Length of the slow weighted moving average", "Indicators")
		.SetCanOptimize(true);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
		.SetDisplay("Momentum Period", "Lookback for the momentum confirmation", "Indicators")
		.SetCanOptimize(true);

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
		.SetDisplay("Momentum Threshold", "Minimal |Momentum-100| deviation", "Filters")
		.SetCanOptimize(true);

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
		.SetDisplay("MACD Fast", "Short EMA length for MACD", "Indicators")
		.SetCanOptimize(true);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
		.SetDisplay("MACD Slow", "Long EMA length for MACD", "Indicators")
		.SetCanOptimize(true);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
		.SetDisplay("MACD Signal", "Signal EMA length for MACD", "Indicators")
		.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 20m)
		.SetDisplay("Stop-Loss (pts)", "Stop-loss distance in price steps", "Risk")
		.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
		.SetDisplay("Take-Profit (pts)", "Take-profit distance in price steps", "Risk")
		.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 40m)
		.SetDisplay("Trailing Stop (pts)", "Trailing stop distance in price steps", "Risk")
		.SetCanOptimize(true);

		_breakEvenTriggerPoints = Param(nameof(BreakEvenTriggerPoints), 30m)
		.SetDisplay("Break-Even Trigger", "Profit required before locking the trade", "Risk")
		.SetCanOptimize(true);

		_breakEvenOffsetPoints = Param(nameof(BreakEvenOffsetPoints), 30m)
		.SetDisplay("Break-Even Offset", "Additional profit locked once break-even triggers", "Risk")
		.SetCanOptimize(true);

		_fractalLifetime = Param(nameof(FractalLifetime), 10)
		.SetDisplay("Fractal Lifetime", "Number of candles the last fractal remains valid", "Filters")
		.SetCanOptimize(true);

		_maxPositionUnits = Param(nameof(MaxPositionUnits), 1m)
		.SetDisplay("Max Position", "Maximum absolute position size", "Risk")
		.SetCanOptimize(true);

		_enableTrailing = Param(nameof(EnableTrailing), true)
		.SetDisplay("Use Trailing", "Enable trailing stop logic", "Risk");

		_enableBreakEven = Param(nameof(EnableBreakEven), true)
		.SetDisplay("Use Break-Even", "Enable break-even stop adjustment", "Risk");

		_useStopLoss = Param(nameof(UseStopLoss), true)
		.SetDisplay("Use Stop-Loss", "Enable fixed stop-loss protection", "Risk");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
		.SetDisplay("Use Take-Profit", "Enable fixed take-profit target", "Risk");
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast weighted moving average length.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow weighted moving average length.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Momentum lookback length.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimal absolute deviation of momentum from 100.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// Fast EMA length for MACD.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length for MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA length for MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Profit distance before break-even is armed.
	/// </summary>
	public decimal BreakEvenTriggerPoints
	{
		get => _breakEvenTriggerPoints.Value;
		set => _breakEvenTriggerPoints.Value = value;
	}

	/// <summary>
	/// Additional profit locked when break-even triggers.
	/// </summary>
	public decimal BreakEvenOffsetPoints
	{
		get => _breakEvenOffsetPoints.Value;
		set => _breakEvenOffsetPoints.Value = value;
	}

	/// <summary>
	/// Number of candles a detected fractal remains valid.
	/// </summary>
	public int FractalLifetime
	{
		get => _fractalLifetime.Value;
		set => _fractalLifetime.Value = value;
	}

	/// <summary>
	/// Maximum absolute position size allowed by the strategy.
	/// </summary>
	public decimal MaxPositionUnits
	{
		get => _maxPositionUnits.Value;
		set => _maxPositionUnits.Value = value;
	}

	/// <summary>
	/// Enable trailing stop logic.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Enable break-even adjustment.
	/// </summary>
	public bool EnableBreakEven
	{
		get => _enableBreakEven.Value;
		set => _enableBreakEven.Value = value;
	}

	/// <summary>
	/// Enable fixed stop-loss protection.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Enable fixed take-profit target.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
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

		_fractalWindow.Clear();
		_lastDownFractal = null;
		_lastUpFractal = null;
		_downFractalAge = 0;
		_upFractalAge = 0;
		_newDownFractal = false;
		_newUpFractal = false;

		_priceStep = 0m;
		_stopLossDistance = 0m;
		_takeProfitDistance = 0m;
		_trailingDistance = 0m;
		_breakEvenTrigger = 0m;
		_breakEvenOffset = 0m;

		_entryPrice = null;
		_highestSinceEntry = 0m;
		_lowestSinceEntry = 0m;
		_lastPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new WeightedMovingAverage { Length = FastMaPeriod };
		_slowMa = new WeightedMovingAverage { Length = SlowMaPeriod };
		_momentum = new Momentum { Length = MomentumPeriod };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			ShortPeriod = MacdFastPeriod,
			LongPeriod = MacdSlowPeriod,
			SignalPeriod = MacdSignalPeriod
		};

		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
		{
			_priceStep = 1m;
		}

		_stopLossDistance = StopLossPoints * _priceStep;
		_takeProfitDistance = TakeProfitPoints * _priceStep;
		_trailingDistance = TrailingStopPoints * _priceStep;
		_breakEvenTrigger = BreakEvenTriggerPoints * _priceStep;
		_breakEvenOffset = BreakEvenOffsetPoints * _priceStep;

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_fastMa, _slowMa, _momentum, _macd, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var currentPosition = Position;

		if (_lastPosition == 0m && currentPosition != 0m)
		{
			// A brand-new position was opened.
			_entryPrice = trade.Trade.Price;
			_highestSinceEntry = _entryPrice ?? 0m;
			_lowestSinceEntry = _entryPrice ?? 0m;
		}
		else if (currentPosition == 0m)
		{
			// The position was fully closed.
			_entryPrice = null;
			_highestSinceEntry = 0m;
			_lowestSinceEntry = 0m;
		}
		else if (Math.Sign((double)_lastPosition) != Math.Sign((double)currentPosition) && currentPosition != 0m)
		{
			// A reversal happened, treat the current fill as new entry.
			_entryPrice = trade.Trade.Price;
			_highestSinceEntry = _entryPrice ?? 0m;
			_lowestSinceEntry = _entryPrice ?? 0m;
		}
		else if (currentPosition > 0m && trade.Order.Side == Sides.Buy)
		{
			// Added to an existing long position, update the average entry price.
			var tradeVolume = trade.Trade.Volume ?? trade.Order.Volume ?? 0m;
			if (tradeVolume > 0m && _entryPrice is decimal currentEntry && _lastPosition > 0m)
			{
				var totalVolume = currentPosition;
				var previousVolume = _lastPosition;
				_entryPrice = ((currentEntry * previousVolume) + (trade.Trade.Price * tradeVolume)) / totalVolume;
			}
			else if (_entryPrice is null)
			{
				_entryPrice = trade.Trade.Price;
			}

			_highestSinceEntry = Math.Max(_highestSinceEntry, trade.Trade.Price);
			_lowestSinceEntry = Math.Min(_lowestSinceEntry, trade.Trade.Price);
		}
		else if (currentPosition < 0m && trade.Order.Side == Sides.Sell)
		{
			// Added to an existing short position, update the average entry price.
			var tradeVolume = trade.Trade.Volume ?? trade.Order.Volume ?? 0m;
			if (tradeVolume > 0m && _entryPrice is decimal currentEntry && _lastPosition < 0m)
			{
				var totalVolume = Math.Abs(currentPosition);
				var previousVolume = Math.Abs(_lastPosition);
				_entryPrice = ((currentEntry * previousVolume) + (trade.Trade.Price * tradeVolume)) / totalVolume;
			}
			else if (_entryPrice is null)
			{
				_entryPrice = trade.Trade.Price;
			}

			_highestSinceEntry = Math.Max(_highestSinceEntry, trade.Trade.Price);
			_lowestSinceEntry = Math.Min(_lowestSinceEntry, trade.Trade.Price);
		}

		_lastPosition = currentPosition;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue momentumValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateFractals(candle);
		ManagePosition(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!fastValue.IsFinal || !slowValue.IsFinal || !momentumValue.IsFinal || !macdValue.IsFinal)
		return;

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();
		var momentumRaw = momentumValue.ToDecimal();

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macdData)
		return;

		if (macdData.Macd is not decimal macd || macdData.Signal is not decimal macdSignal)
		return;

		var momentumDistance = Math.Abs(momentumRaw - 100m);
		var hasMomentum = momentumDistance >= MomentumThreshold;
		var bullishTrend = fast > slow;
		var bearishTrend = fast < slow;
		var macdBullish = macd > macdSignal;
		var macdBearish = macd < macdSignal;

		var canBuy = _newDownFractal && hasMomentum && bullishTrend && macdBullish;
		var canSell = _newUpFractal && hasMomentum && bearishTrend && macdBearish;

		if (canBuy)
		{
			ExecuteSignal(Sides.Buy);
		}
		else if (canSell)
		{
			ExecuteSignal(Sides.Sell);
		}

		ExpireFractals();
	}

	private void ManagePosition(ICandleMessage candle)
	{
		var position = Position;
		if (position == 0m || _entryPrice is null)
		return;

		var entry = _entryPrice.Value;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		_highestSinceEntry = Math.Max(_highestSinceEntry, high);
		_lowestSinceEntry = Math.Min(_lowestSinceEntry, low);

		if (position > 0m)
		{
			if (UseStopLoss && _stopLossDistance > 0m && low <= entry - _stopLossDistance)
			{
				SellMarket(position);
				return;
			}

			if (UseTakeProfit && _takeProfitDistance > 0m && high >= entry + _takeProfitDistance)
			{
				SellMarket(position);
				return;
			}

			if (EnableTrailing && _trailingDistance > 0m && _highestSinceEntry - entry >= _trailingDistance)
			{
				var trailingStop = _highestSinceEntry - _trailingDistance;
				if (low <= trailingStop)
				{
					SellMarket(position);
					return;
				}
			}

			if (EnableBreakEven && _breakEvenTrigger > 0m && _highestSinceEntry - entry >= _breakEvenTrigger)
			{
				var breakEvenLevel = entry + _breakEvenOffset;
				if (low <= breakEvenLevel)
				{
					SellMarket(position);
					return;
				}
			}
		}
		else if (position < 0m)
		{
			var absPosition = Math.Abs(position);

			if (UseStopLoss && _stopLossDistance > 0m && high >= entry + _stopLossDistance)
			{
				BuyMarket(absPosition);
				return;
			}

			if (UseTakeProfit && _takeProfitDistance > 0m && low <= entry - _takeProfitDistance)
			{
				BuyMarket(absPosition);
				return;
			}

			if (EnableTrailing && _trailingDistance > 0m && entry - _lowestSinceEntry >= _trailingDistance)
			{
				var trailingStop = _lowestSinceEntry + _trailingDistance;
				if (high >= trailingStop)
				{
					BuyMarket(absPosition);
					return;
				}
			}

			if (EnableBreakEven && _breakEvenTrigger > 0m && entry - _lowestSinceEntry >= _breakEvenTrigger)
			{
				var breakEvenLevel = entry - _breakEvenOffset;
				if (high >= breakEvenLevel)
				{
					BuyMarket(absPosition);
					return;
				}
			}
		}

}

	private void ExpireFractals()
	{
		if (FractalLifetime <= 0)
			return;

		if (_lastDownFractal.HasValue && _downFractalAge > FractalLifetime)
			_lastDownFractal = null;

		if (_lastUpFractal.HasValue && _upFractalAge > FractalLifetime)
			_lastUpFractal = null;
	}

	private void ExecuteSignal(Sides direction)
	{
		var baseVolume = Volume > 0m ? Volume : 1m;
		if (MaxPositionUnits > 0m && baseVolume > MaxPositionUnits)
		{
			baseVolume = MaxPositionUnits;
		}

		baseVolume = NormalizeVolume(baseVolume);
		if (baseVolume <= 0m)
		return;

		var currentPosition = Position;

		if (direction == Sides.Buy)
		{
			if (MaxPositionUnits > 0m && currentPosition >= MaxPositionUnits)
			return;

			var volumeToBuy = baseVolume;
			if (currentPosition < 0m)
			{
				volumeToBuy += Math.Abs(currentPosition);
			}

			if (MaxPositionUnits > 0m)
			{
				var allowed = MaxPositionUnits - Math.Max(0m, currentPosition);
				if (allowed <= 0m)
				return;

				volumeToBuy = Math.Min(volumeToBuy, allowed + Math.Abs(Math.Min(0m, currentPosition)));
			}

			volumeToBuy = NormalizeVolume(volumeToBuy);
			if (volumeToBuy > 0m)
			{
				BuyMarket(volumeToBuy);
			}
		}
		else
		{
			if (MaxPositionUnits > 0m && Math.Abs(currentPosition) >= MaxPositionUnits && currentPosition <= 0m)
			return;

			var volumeToSell = baseVolume;
			if (currentPosition > 0m)
			{
				volumeToSell += currentPosition;
			}

			if (MaxPositionUnits > 0m)
			{
				var allowed = MaxPositionUnits - Math.Max(0m, -currentPosition);
				if (allowed <= 0m)
				return;

				volumeToSell = Math.Min(volumeToSell, allowed + Math.Max(0m, currentPosition));
			}

			volumeToSell = NormalizeVolume(volumeToSell);
			if (volumeToSell > 0m)
			{
				SellMarket(volumeToSell);
			}
		}
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var security = Security;
		if (security == null)
		return volume;

		if (security.VolumeStep is decimal step && step > 0m)
		{
			volume = Math.Round(volume / step) * step;
		}

		if (security.MinVolume is decimal min && min > 0m && volume < min)
		{
			volume = min;
		}

		if (security.MaxVolume is decimal max && max > 0m && volume > max)
		{
			volume = max;
		}

		return volume;
	}

	private void UpdateFractals(ICandleMessage candle)
	{
		_newDownFractal = false;
		_newUpFractal = false;

		_fractalWindow.Enqueue((candle.HighPrice, candle.LowPrice));
		while (_fractalWindow.Count > 5)
		{
			_fractalWindow.Dequeue();
		}

		_downFractalAge++;
		_upFractalAge++;

		if (_fractalWindow.Count < 5)
		return;

		var values = _fractalWindow.ToArray();
		var h0 = values[0].high;
		var h1 = values[1].high;
		var h2 = values[2].high;
		var h3 = values[3].high;
		var h4 = values[4].high;

		var l0 = values[0].low;
		var l1 = values[1].low;
		var l2 = values[2].low;
		var l3 = values[3].low;
		var l4 = values[4].low;

		var isUpFractal = h2 > h0 && h2 > h1 && h2 > h3 && h2 > h4;
		var isDownFractal = l2 < l0 && l2 < l1 && l2 < l3 && l2 < l4;

		if (isDownFractal)
		{
			_lastDownFractal = l2;
			_downFractalAge = 0;
			_newDownFractal = true;
		}

		if (isUpFractal)
		{
			_lastUpFractal = h2;
			_upFractalAge = 0;
			_newUpFractal = true;
		}
	}
}
