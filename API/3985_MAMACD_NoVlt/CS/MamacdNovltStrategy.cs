using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that replicates the MAMACD_novlt MetaTrader expert advisor.
/// It prepares long or short setups when a fast EMA is below or above two LWMA values
/// built from candle lows, and then confirms entries with the MACD main line momentum.
/// </summary>
public class MamacdNovltStrategy : Strategy
{
	private readonly StrategyParam<int> _firstLowWmaPeriod;
	private readonly StrategyParam<int> _secondLowWmaPeriod;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _fastSignalEmaPeriod;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private WeightedMovingAverage _firstLowWma = null!;
	private WeightedMovingAverage _secondLowWma = null!;
	private ExponentialMovingAverage _fastCloseEma = null!;
	private MovingAverageConvergenceDivergence _macd = null!;

	private bool _isLongSetupPrepared;
	private bool _isShortSetupPrepared;
	private decimal? _previousMacd;

	/// <summary>
	/// Period of the first LWMA calculated on low prices.
	/// </summary>
	public int FirstLowWmaPeriod
	{
		get => _firstLowWmaPeriod.Value;
		set => _firstLowWmaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the second LWMA calculated on low prices.
	/// </summary>
	public int SecondLowWmaPeriod
	{
		get => _secondLowWmaPeriod.Value;
		set => _secondLowWmaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the fast EMA calculated on close prices.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow period of the MACD indicator.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Fast period of the MACD indicator.
	/// </summary>
	public int FastSignalEmaPeriod
	{
		get => _fastSignalEmaPeriod.Value;
		set => _fastSignalEmaPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Order volume used for market entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
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
	/// Initializes strategy parameters that mirror the original MetaTrader inputs.
	/// </summary>
	public MamacdNovltStrategy()
	{
		_firstLowWmaPeriod = Param(nameof(FirstLowWmaPeriod), 85)
		.SetGreaterThanZero()
		.SetDisplay("First LWMA Period", "First LWMA period on lows", "Indicators");

		_secondLowWmaPeriod = Param(nameof(SecondLowWmaPeriod), 75)
		.SetGreaterThanZero()
		.SetDisplay("Second LWMA Period", "Second LWMA period on lows", "Indicators");

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA Period", "Fast EMA period on closes", "Indicators");

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow Period", "Slow EMA period for MACD", "Indicators");

		_fastSignalEmaPeriod = Param(nameof(FastSignalEmaPeriod), 15)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast Period", "Fast EMA period for MACD", "Indicators");

		_stopLossPoints = Param(nameof(StopLossPoints), 15)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Stop Loss (steps)", "Stop-loss distance in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 15)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Take Profit (steps)", "Take-profit distance in price steps", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Default order volume", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for calculations", "General");
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

	_isLongSetupPrepared = false;
	_isShortSetupPrepared = false;
	_previousMacd = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	Volume = TradeVolume;

	_firstLowWma = new WeightedMovingAverage
	{
	Length = FirstLowWmaPeriod,
	CandlePrice = CandlePrice.Low
	};

	_secondLowWma = new WeightedMovingAverage
	{
	Length = SecondLowWmaPeriod,
	CandlePrice = CandlePrice.Low
	};

	_fastCloseEma = new ExponentialMovingAverage
	{
	Length = FastEmaPeriod,
	CandlePrice = CandlePrice.Close
	};

	_macd = new MovingAverageConvergenceDivergence
	{
	Fast = FastSignalEmaPeriod,
	Slow = SlowEmaPeriod
	};

	var subscription = SubscribeCandles(CandleType);
	subscription
	.BindEx(_fastCloseEma, _firstLowWma, _secondLowWma, _macd, ProcessCandle)
	.Start();

	var mainArea = CreateChartArea();
	if (mainArea != null)
	{
	DrawCandles(mainArea, subscription);
	DrawIndicator(mainArea, _fastCloseEma);
	DrawIndicator(mainArea, _firstLowWma);
	DrawIndicator(mainArea, _secondLowWma);
	DrawOwnTrades(mainArea);
	}

	var macdArea = CreateChartArea();
	if (macdArea != null)
	{
	DrawIndicator(macdArea, _macd);
	}

	var takeProfitUnit = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints, UnitTypes.PriceStep) : null;
	var stopLossUnit = StopLossPoints > 0 ? new Unit(StopLossPoints, UnitTypes.PriceStep) : null;

	StartProtection(takeProfitUnit, stopLossUnit);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue firstLwmaValue, IIndicatorValue secondLwmaValue, IIndicatorValue macdValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (!emaValue.IsFinal || !firstLwmaValue.IsFinal || !secondLwmaValue.IsFinal || !macdValue.IsFinal)
	return;

	var ema = emaValue.ToDecimal();
	var firstLwma = firstLwmaValue.ToDecimal();
	var secondLwma = secondLwmaValue.ToDecimal();

	var macdData = (MovingAverageConvergenceDivergenceValue)macdValue;
	if (macdData.Macd is not decimal macdLine)
	return;

	// Track when the fast EMA moves below both LWMA values to arm the long setup.
	if (ema < firstLwma && ema < secondLwma)
	{
	_isLongSetupPrepared = true;
	}

	// Track when the fast EMA moves above both LWMA values to arm the short setup.
	if (ema > firstLwma && ema > secondLwma)
	{
	_isShortSetupPrepared = true;
	}

	var hasPreviousMacd = _previousMacd.HasValue;
	var macdPrev = _previousMacd ?? macdLine;

	var macdBullish = macdLine > 0m || (hasPreviousMacd && macdLine > macdPrev);
	var macdBearish = macdLine < 0m || (hasPreviousMacd && macdLine < macdPrev);

	if (!IsFormedAndOnlineAndAllowTrading())
	{
	_previousMacd = macdLine;
	return;
	}

	// Enter long position once the EMA breaks above both LWMA values after being below and MACD momentum turns positive.
	if (ema > firstLwma && ema > secondLwma && _isLongSetupPrepared && macdBullish && Position <= 0)
	{
	BuyMarket(Volume + Math.Abs(Position));
	_isLongSetupPrepared = false;
	}

	// Enter short position once the EMA breaks below both LWMA values after being above and MACD momentum turns negative.
	if (ema < firstLwma && ema < secondLwma && _isShortSetupPrepared && macdBearish && Position >= 0)
	{
	SellMarket(Volume + Math.Abs(Position));
	_isShortSetupPrepared = false;
	}

	_previousMacd = macdLine;
	}
}
