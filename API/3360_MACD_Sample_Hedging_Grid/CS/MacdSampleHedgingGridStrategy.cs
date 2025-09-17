using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the "MACD Sample Hedging Grid" MetaTrader strategy.
/// The strategy combines MACD crossovers with higher timeframe momentum and trend confirmation.
/// </summary>
public class MacdSampleHedgingGridStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _momentumCandleType;
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _trendMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _macdOpenLevel;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _lotExponent;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<decimal> _takeProfitSteps;

	private readonly Queue<decimal> _momentumBuffer = new();

	private decimal? _prevMacd;
	private decimal? _prevSignal;
	private decimal? _prevTrendEma;
	private decimal? _trendMacd;
	private decimal? _trendSignal;
	private Sides? _currentDirection;
	private int _entriesInCurrentDirection;

	/// <summary>
	/// Main candle type used for trading signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe for momentum confirmation.
	/// </summary>
	public DataType MomentumCandleType
	{
		get => _momentumCandleType.Value;
		set => _momentumCandleType.Value = value;
	}

	/// <summary>
	/// Long-term timeframe for MACD trend filter.
	/// </summary>
	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	/// <summary>
	/// Fast EMA period inside MACD.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period inside MACD.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Signal SMA period for MACD.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// EMA period used as local trend filter.
	/// </summary>
	public int TrendMaPeriod
	{
		get => _trendMaPeriod.Value;
		set => _trendMaPeriod.Value = value;
	}

	/// <summary>
	/// Momentum indicator period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// MACD absolute threshold (expressed in price steps) required for entries.
	/// </summary>
	public decimal MacdOpenLevel
	{
		get => _macdOpenLevel.Value;
		set => _macdOpenLevel.Value = value;
	}

	/// <summary>
	/// Minimum momentum distance from 100 required for longs.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Minimum momentum distance from 100 required for shorts.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Maximum number of consecutive entries in the same direction.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Lot multiplier applied to each grid position in the same direction.
	/// </summary>
	public decimal LotExponent
	{
		get => _lotExponent.Value;
		set => _lotExponent.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price steps.
	/// </summary>
	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price steps.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Initializes default parameters.
	/// </summary>
	public MacdSampleHedgingGridStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Main Candle Type", "Primary timeframe for entries", "General");

		_momentumCandleType = Param(nameof(MomentumCandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Momentum Candle Type", "Higher timeframe used for momentum", "Filters");

		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromDays(30).TimeFrame())
		.SetDisplay("Trend Candle Type", "Long-term timeframe for trend MACD", "Filters");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 12)
		.SetDisplay("MACD Fast EMA", "Fast EMA period", "Indicators")
		.SetGreaterThanZero();

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 26)
		.SetDisplay("MACD Slow EMA", "Slow EMA period", "Indicators")
		.SetGreaterThanZero();

		_signalPeriod = Param(nameof(SignalPeriod), 9)
		.SetDisplay("MACD Signal", "Signal SMA period", "Indicators")
		.SetGreaterThanZero();

		_trendMaPeriod = Param(nameof(TrendMaPeriod), 26)
		.SetDisplay("Trend EMA", "EMA filter length", "Indicators")
		.SetGreaterThanZero();

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
		.SetDisplay("Momentum Period", "Length of momentum indicator", "Indicators")
		.SetGreaterThanZero();

		_macdOpenLevel = Param(nameof(MacdOpenLevel), 3m)
		.SetDisplay("MACD Open Level", "Absolute MACD level in price steps", "Filters")
		.SetGreaterThanZero();

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
		.SetDisplay("Momentum Buy Threshold", "Minimum momentum distance for longs", "Filters")
		.SetGreaterThanZero();

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
		.SetDisplay("Momentum Sell Threshold", "Minimum momentum distance for shorts", "Filters")
		.SetGreaterThanZero();

		_maxTrades = Param(nameof(MaxTrades), 10)
		.SetDisplay("Max Trades", "Maximum grid entries per direction", "Risk")
		.SetGreaterThanZero();

		_lotExponent = Param(nameof(LotExponent), 1.44m)
		.SetDisplay("Lot Exponent", "Multiplier for additional entries", "Risk")
		.SetGreaterThanZero();

		_stopLossSteps = Param(nameof(StopLossSteps), 20m)
		.SetDisplay("Stop Loss Steps", "Stop-loss distance in price steps", "Risk")
		.SetGreaterThanZero();

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 50m)
		.SetDisplay("Take Profit Steps", "Take-profit distance in price steps", "Risk")
		.SetGreaterThanZero();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[]
		{
			(Security, CandleType),
			(Security, MomentumCandleType),
			(Security, TrendCandleType)
		};
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_momentumBuffer.Clear();
		_prevMacd = null;
		_prevSignal = null;
		_prevTrendEma = null;
		_trendMacd = null;
		_trendSignal = null;
		_currentDirection = null;
		_entriesInCurrentDirection = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastMaPeriod },
				LongMa = { Length = SlowMaPeriod }
			},
			SignalMa = { Length = SignalPeriod }
		};

		var trendEma = new ExponentialMovingAverage { Length = TrendMaPeriod };

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
		.BindEx(macd, trendEma, ProcessMainCandle)
		.Start();

		var momentum = new Momentum { Length = MomentumPeriod };
		var momentumSubscription = SubscribeCandles(MomentumCandleType);
		momentumSubscription
		.Bind(momentum, ProcessMomentumCandle)
		.Start();

		var trendMacd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastMaPeriod },
				LongMa = { Length = SlowMaPeriod }
			},
			SignalMa = { Length = SignalPeriod }
		};

		var trendSubscription = SubscribeCandles(TrendCandleType);
		trendSubscription
		.BindEx(trendMacd, ProcessTrendCandle)
		.Start();

		if (StopLossSteps > 0m || TakeProfitSteps > 0m)
		{
			StartProtection(
			stopLoss: StopLossSteps > 0m ? new Unit(StopLossSteps, UnitTypes.Step) : null,
			takeProfit: TakeProfitSteps > 0m ? new Unit(TakeProfitSteps, UnitTypes.Step) : null);
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, macd);
			DrawIndicator(area, trendEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessMomentumCandle(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var distance = Math.Abs(100m - momentumValue);
		_momentumBuffer.Enqueue(distance);

		while (_momentumBuffer.Count > 3)
		_momentumBuffer.Dequeue();
	}

	private void ProcessTrendCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!macdValue.IsFinal)
		return;

		var trendValue = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		_trendMacd = trendValue.Macd;
		_trendSignal = trendValue.Signal;
	}

	private void ProcessMainCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue trendEmaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!macdValue.IsFinal || !trendEmaValue.IsFinal)
		return;

		if (_momentumBuffer.Count < 3 || _trendMacd is null || _trendSignal is null)
		return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macd = macdTyped.Macd;
		var signal = macdTyped.Signal;
		var trendEma = trendEmaValue.ToDecimal();

		if (_prevMacd is null || _prevSignal is null || _prevTrendEma is null)
		{
			_prevMacd = macd;
			_prevSignal = signal;
			_prevTrendEma = trendEma;
			return;
		}

		var priceStep = Security?.PriceStep ?? 0.0001m;
		var momentumValues = _momentumBuffer.ToArray();

		var buyCondition = macd < 0m && macd > signal &&
		_prevMacd < _prevSignal &&
		Math.Abs(macd) > MacdOpenLevel * priceStep &&
		trendEma > _prevTrendEma &&
		momentumValues.Any(v => v >= MomentumBuyThreshold) &&
		((_trendMacd > 0m && _trendMacd > _trendSignal) || (_trendMacd < 0m && _trendMacd > _trendSignal));

		var sellCondition = macd > 0m && macd < signal &&
		_prevMacd > _prevSignal &&
		macd > MacdOpenLevel * priceStep &&
		trendEma < _prevTrendEma &&
		momentumValues.Any(v => v >= MomentumSellThreshold) &&
		((_trendMacd > 0m && _trendMacd < _trendSignal) || (_trendMacd < 0m && _trendMacd < _trendSignal));

		if (buyCondition)
		{
			OpenInDirection(Sides.Buy);
		}
		else if (sellCondition)
		{
			OpenInDirection(Sides.Sell);
		}

		_prevMacd = macd;
		_prevSignal = signal;
		_prevTrendEma = trendEma;
	}

	private void OpenInDirection(Sides direction)
	{
		if (direction == Sides.Buy && Position < 0)
		{
			// Close short exposure before starting a new long grid.
			BuyMarket(Math.Abs(Position));
			ResetGrid();
		}
		else if (direction == Sides.Sell && Position > 0)
		{
			// Close long exposure before starting a new short grid.
			SellMarket(Math.Abs(Position));
			ResetGrid();
		}

		var volume = CalculateNextVolume(direction);

		if (volume <= 0m)
		return;

		if (direction == Sides.Buy)
		{
			BuyMarket(volume);
			LogInfo($"Buy grid entry with volume {volume}");
		}
		else
		{
			SellMarket(volume);
			LogInfo($"Sell grid entry with volume {volume}");
		}
	}

	private decimal CalculateNextVolume(Sides direction)
	{
		if (_currentDirection != direction)
		{
			ResetGrid();
			_currentDirection = direction;
		}

		if (_entriesInCurrentDirection >= MaxTrades)
		return 0m;

		var exponent = (decimal)Math.Pow((double)LotExponent, _entriesInCurrentDirection);
		var volume = Volume * exponent;
		_entriesInCurrentDirection++;

		return volume;
	}

	private void ResetGrid()
	{
		_currentDirection = null;
		_entriesInCurrentDirection = 0;
	}
}
