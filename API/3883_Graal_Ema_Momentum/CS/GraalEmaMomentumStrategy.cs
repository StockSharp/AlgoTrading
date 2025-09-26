using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class GraalEmaMomentumStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumFilter;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastCloseEma;
	private ExponentialMovingAverage _slowOpenEma;
	private Momentum? _momentum;

	private decimal? _previousFast;
	private decimal? _previousSlow;
	private decimal? _previousMomentum;

	private decimal? _entryPrice;
	private decimal? _takeProfitTarget;

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	public decimal MomentumFilter
	{
		get => _momentumFilter.Value;
		set => _momentumFilter.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public GraalEmaMomentumStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Period", "Length of the fast EMA calculated on close prices", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Period", "Length of the slow EMA calculated on open prices", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 80, 1);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Length of the momentum oscillator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 2);

		_momentumFilter = Param(nameof(MomentumFilter), 0.1m)
			.SetRange(0m, 5m)
			.SetDisplay("Momentum Filter", "Minimum momentum deviation from neutral level", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.5m, 0.05m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 200m)
			.SetRange(0m, 5000m)
			.SetDisplay("Take Profit (points)", "Distance to the take-profit level expressed in price points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(50m, 500m, 50m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used by the strategy", "General");

		Volume = 1;
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousFast = null;
		_previousSlow = null;
		_previousMomentum = null;
		_entryPrice = null;
		_takeProfitTarget = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastCloseEma = new ExponentialMovingAverage { Length = FastPeriod };
		_slowOpenEma = new ExponentialMovingAverage { Length = SlowPeriod };
		_momentum = new Momentum { Length = MomentumPeriod };

		// Subscribe to the configured candle stream and bind indicators.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastCloseEma, _momentum, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastCloseEma, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Calculate the slow EMA using candle open prices to mimic PRICE_OPEN from MT4.
		var slowValue = _slowOpenEma!.Process(candle.OpenPrice);
		if (!slowValue.IsFinal || !slowValue.TryGetValue(out var slowOpenEma))
			return;

		var currentMomentum = momentumValue - 100m;

		if (_previousFast is null || _previousSlow is null || _previousMomentum is null)
		{
			// Store the first complete indicator values before enabling trading logic.
			_previousFast = fastCloseEma;
			_previousSlow = slowOpenEma;
			_previousMomentum = currentMomentum;
			return;
		}

		CheckTakeProfit(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousFast = fastCloseEma;
			_previousSlow = slowOpenEma;
			_previousMomentum = currentMomentum;
			return;
		}

		// Determine crossover direction between fast and slow averages.
		var crossedAbove = fastCloseEma > slowOpenEma && _previousFast.Value <= _previousSlow.Value;
		var crossedBelow = fastCloseEma < slowOpenEma && _previousFast.Value >= _previousSlow.Value;

		// Evaluate momentum filter to ensure acceleration confirms the crossover.
		var momentumRising = currentMomentum > MomentumFilter && currentMomentum > _previousMomentum.Value;
		var momentumFalling = currentMomentum < -MomentumFilter && currentMomentum < _previousMomentum.Value;

		if (crossedAbove && momentumRising && Position <= 0)
		{
			EnterLong(candle);
		}
		else if (crossedBelow && momentumFalling && Position >= 0)
		{
			EnterShort(candle);
		}

		_previousFast = fastCloseEma;
		_previousSlow = slowOpenEma;
		_previousMomentum = currentMomentum;
	}

	private void EnterLong(ICandleMessage candle)
	{
		// Add the absolute position size so the order flips any existing short exposure.
		var volume = Volume + Math.Abs(Position);
		BuyMarket(volume);

		_entryPrice = candle.ClosePrice;
		SetTakeProfitTarget(isLong: true);
	}

	private void EnterShort(ICandleMessage candle)
	{
		// Add the absolute position size so the order flips any existing long exposure.
		var volume = Volume + Math.Abs(Position);
		SellMarket(volume);

		_entryPrice = candle.ClosePrice;
		SetTakeProfitTarget(isLong: false);
	}

	private void CheckTakeProfit(ICandleMessage candle)
	{
		// Recreate MT4-style point-based take profit management.
		if (_takeProfitTarget is null || _entryPrice is null || TakeProfitPoints <= 0m)
			return;

		if (Position > 0 && candle.HighPrice >= _takeProfitTarget.Value)
		{
			ClosePosition();
			_entryPrice = null;
			_takeProfitTarget = null;
			return;
		}

		if (Position < 0 && candle.LowPrice <= _takeProfitTarget.Value)
		{
			ClosePosition();
			_entryPrice = null;
			_takeProfitTarget = null;
		}
	}

	private void SetTakeProfitTarget(bool isLong)
	{
		var step = Security?.PriceStep ?? 0m;
		// Skip the target when instrument settings do not provide a valid price step.
		if (step <= 0m || TakeProfitPoints <= 0m || _entryPrice is null)
		{
			_takeProfitTarget = null;
			return;
		}

		var distance = TakeProfitPoints * step;
		_takeProfitTarget = isLong ? _entryPrice + distance : _entryPrice - distance;
	}
}
