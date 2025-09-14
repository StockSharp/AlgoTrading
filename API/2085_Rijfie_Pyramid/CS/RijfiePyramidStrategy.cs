using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

	/// <summary>
	/// Pyramid style strategy that buys additional lots as price drops by a fixed percentage.
	/// </summary>
	public class RijfiePyramidStrategy : Strategy
	{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<decimal> _maxPrice;
	private readonly StrategyParam<decimal> _lowPrice;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _stepLevel;
	private readonly StrategyParam<bool> _closeAll;
	private readonly StrategyParam<int> _closeHour;
	private readonly StrategyParam<int> _closeMinute;

	private decimal _nextBuyPrice;
	private decimal _prevStoch;

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
	get => _candleType.Value;
	set => _candleType.Value = value;
	}

	/// <summary>
	/// Oversold threshold for the Stochastic oscillator.
	/// </summary>
	public decimal LowLevel
	{
	get => _lowLevel.Value;
	set => _lowLevel.Value = value;
	}

	/// <summary>
	/// Maximum price for opening the first position.
	/// </summary>
	public decimal MaxPrice
	{
	get => _maxPrice.Value;
	set => _maxPrice.Value = value;
	}

	/// <summary>
	/// Minimum allowed price.
	/// </summary>
	public decimal LowPrice
	{
	get => _lowPrice.Value;
	set => _lowPrice.Value = value;
	}

	/// <summary>
	/// EMA period used as a trend filter.
	/// </summary>
	public int MaPeriod
	{
	get => _maPeriod.Value;
	set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Percentage drop required before adding a new position.
	/// </summary>
	public decimal StepLevel
	{
	get => _stepLevel.Value;
	set => _stepLevel.Value = value;
	}

	/// <summary>
	/// Close positions at specified time.
	/// </summary>
	public bool CloseAll
	{
	get => _closeAll.Value;
	set => _closeAll.Value = value;
	}

	/// <summary>
	/// Hour for closing positions when <see cref="CloseAll"/> is enabled.
	/// </summary>
	public int CloseHour
	{
	get => _closeHour.Value;
	set => _closeHour.Value = value;
	}

	/// <summary>
	/// Minute for closing positions when <see cref="CloseAll"/> is enabled.
	/// </summary>
	public int CloseMinute
	{
	get => _closeMinute.Value;
	set => _closeMinute.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="RijfiePyramidStrategy"/>.
	/// </summary>
	public RijfiePyramidStrategy()
	{
	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
	.SetDisplay("Candle Type", "Type of candles to use", "General");

	_lowLevel = Param(nameof(LowLevel), 10m)
	.SetDisplay("Stochastic Low", "Oversold threshold", "Parameters");

	_maxPrice = Param(nameof(MaxPrice), 9.5m)
	.SetDisplay("Max Price", "Upper price limit", "Parameters");

	_lowPrice = Param(nameof(LowPrice), 7.5m)
	.SetDisplay("Low Price", "Lower price limit", "Parameters");

	_maPeriod = Param(nameof(MaPeriod), 5)
	.SetDisplay("EMA Period", "EMA length", "Parameters")
	.SetCanOptimize(true)
	.SetGreaterThanZero();

	_stepLevel = Param(nameof(StepLevel), 10m)
	.SetDisplay("Step Level", "Percent drop for next buy", "Parameters");

	_closeAll = Param(nameof(CloseAll), false)
	.SetDisplay("Close All", "Close positions at set time", "Parameters");

	_closeHour = Param(nameof(CloseHour), 20)
	.SetDisplay("Close Hour", "Hour to close positions", "Parameters");

	_closeMinute = Param(nameof(CloseMinute), 55)
	.SetDisplay("Close Minute", "Minute to close positions", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	var stochastic = new StochasticOscillator
	{
	Length = 5,
	K = { Length = 3 },
	D = { Length = 3 }
	};

	var ema = new ExponentialMovingAverage
	{
	Length = MaPeriod
	};

	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(stochastic, ema, ProcessCandle)
	.Start();

	StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal k, decimal d, decimal ema)
	{
	if (candle.State != CandleStates.Finished)
	return;

	var price = candle.ClosePrice;

	// Initial buy when Stochastic crosses above the low level
	if (_prevStoch < LowLevel && k > LowLevel && price < MaxPrice && price > LowPrice)
	{
	BuyMarket();
	_nextBuyPrice = price - price / 100m * StepLevel;
	}
	// Additional buys when price drops below the threshold but stays above EMA and low price
	else if (Position > 0 && price < _nextBuyPrice && price > LowPrice && price > ema)
	{
	BuyMarket();
	_nextBuyPrice = price - price / 100m * StepLevel;
	}

	// Optional time-based exit
	if (CloseAll && candle.CloseTime.Hour == CloseHour && candle.CloseTime.Minute >= CloseMinute && Position > 0)
	{
	SellMarket(Position);
	}

	_prevStoch = k;
	}
	}
