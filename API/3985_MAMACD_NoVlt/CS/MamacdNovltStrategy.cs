using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using StockSharp.Algo;

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
	private readonly StrategyParam<DataType> _candleType;

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
	/// Stop-loss distance in absolute price units.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in absolute price units.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
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

		_stopLossPoints = Param(nameof(StopLossPoints), 500)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop-loss distance", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 500)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take-profit distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[] { (Security, CandleType) };
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
	protected override void OnStarted2(DateTime time)
	{
		var fastCloseEma = new EMA { Length = FastEmaPeriod };
		var firstLowWma = new WeightedMovingAverage { Length = FirstLowWmaPeriod };
		var secondLowWma = new WeightedMovingAverage { Length = SecondLowWmaPeriod };

		var macd = new MovingAverageConvergenceDivergence();
		macd.ShortMa.Length = FastSignalEmaPeriod;
		macd.LongMa.Length = SlowEmaPeriod;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastCloseEma, firstLowWma, secondLowWma, macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastCloseEma);
			DrawOwnTrades(area);
		}

		var takeProfitUnit = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints, UnitTypes.Absolute) : null;
		var stopLossUnit = StopLossPoints > 0 ? new Unit(StopLossPoints, UnitTypes.Absolute) : null;
		StartProtection(takeProfitUnit, stopLossUnit);

		base.OnStarted2(time);
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema, decimal firstLwma, decimal secondLwma, decimal macdLine)
	{
		if (candle.State != CandleStates.Finished)
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

		// Enter long when EMA crosses above both LWMAs after being below, with bullish MACD
		if (ema > firstLwma && ema > secondLwma && _isLongSetupPrepared && macdBullish && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_isLongSetupPrepared = false;
		}

		// Enter short when EMA crosses below both LWMAs after being above, with bearish MACD
		if (ema < firstLwma && ema < secondLwma && _isShortSetupPrepared && macdBearish && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Position);
			SellMarket(Volume);
			_isShortSetupPrepared = false;
		}

		_previousMacd = macdLine;
	}
}
