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
/// X MAN momentum breakout strategy converted from MetaTrader expert 27492.
/// Combines multi-timeframe momentum with weighted moving averages and a MACD filter.
/// </summary>
public class XManStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<decimal> _distancePoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;

	private decimal? _fastMaValue;
	private decimal? _slowMaValue;

	private decimal? _momentumDiff1;
	private decimal? _momentumDiff2;
	private decimal? _momentumDiff3;

	private decimal? _macdValue;
	private decimal? _macdSignalValue;

	private DateTimeOffset? _lastSignalTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="XManStrategy"/> class.
	/// </summary>
	public XManStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average", "Trend")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
			.SetGreaterThanZero()
			.SetDisplay("Slow LWMA", "Length of the slow linear weighted moving average", "Trend")
			.SetCanOptimize(true)
			.SetOptimize(40, 150, 5);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Lookback length of the higher timeframe momentum", "Momentum")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 2);

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
			.SetNotNegative()
			.SetDisplay("Momentum Buy", "Minimum distance from 100 to allow long trades", "Momentum")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1.0m, 0.1m);

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
			.SetNotNegative()
			.SetDisplay("Momentum Sell", "Minimum distance from 100 to allow short trades", "Momentum")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1.0m, 0.1m);

		_distancePoints = Param(nameof(DistancePoints), 25m)
			.SetNotNegative()
			.SetDisplay("Distance", "Additional points required between LWMA lines", "Trend")
			.SetCanOptimize(true)
			.SetOptimize(5m, 60m, 5m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take profit distance in points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(20m, 120m, 10m);

		_stopLossPoints = Param(nameof(StopLossPoints), 20m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss distance in points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 80m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Primary Candle", "Primary timeframe used for LWMA trend detection", "General");

		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Momentum Candle", "Higher timeframe used for the momentum filter", "General");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("MACD Candle", "Timeframe used for the MACD filter", "General");
	}

	/// <summary>
	/// Fast LWMA period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow LWMA period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
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
	/// Buy-side momentum threshold.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Sell-side momentum threshold.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Minimal distance in points between the weighted averages.
	/// </summary>
	public decimal DistancePoints
	{
		get => _distancePoints.Value;
		set => _distancePoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for the main LWMA calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Candle type used for the momentum filter.
	/// </summary>
	public DataType HigherCandleType
	{
		get => _higherCandleType.Value;
		set => _higherCandleType.Value = value;
	}

	/// <summary>
	/// Candle type used for the MACD confirmation.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);

		if (!HigherCandleType.Equals(CandleType))
			yield return (Security, HigherCandleType);

		if (!MacdCandleType.Equals(CandleType) && !MacdCandleType.Equals(HigherCandleType))
			yield return (Security, MacdCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastMaValue = null;
		_slowMaValue = null;

		_momentumDiff1 = null;
		_momentumDiff2 = null;
		_momentumDiff3 = null;

		_macdValue = null;
		_macdSignalValue = null;

		_lastSignalTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create the indicators used by the trading rules.
		var fastMa = new LinearWeightedMovingAverage { Length = FastMaPeriod, CandlePrice = CandlePrice.Typical };
		var slowMa = new LinearWeightedMovingAverage { Length = SlowMaPeriod, CandlePrice = CandlePrice.Typical };
		var momentum = new Momentum { Length = MomentumPeriod };
		var macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = 12,
			LongPeriod = 26,
			SignalPeriod = 9
		};

		// Connect indicator pipelines to their respective candle subscriptions.
		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription.Bind(fastMa, slowMa, ProcessTrend).Start();

		var higherSubscription = SubscribeCandles(HigherCandleType);
		higherSubscription.Bind(momentum, ProcessMomentum).Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription.BindEx(macd, ProcessMacd).Start();

		var takeProfitUnit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints, UnitTypes.Point) : null;
		var stopLossUnit = StopLossPoints > 0m ? new Unit(StopLossPoints, UnitTypes.Point) : null;

		if (takeProfitUnit != null || stopLossUnit != null)
		{
			StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit, useMarketOrders: true);
		}

		// Optionally plot the signals together with candles and indicators.
		var chartArea = CreateChartArea();
		if (chartArea != null)
		{
			DrawCandles(chartArea, mainSubscription);
			DrawIndicator(chartArea, fastMa);
			DrawIndicator(chartArea, slowMa);
			DrawOwnTrades(chartArea);
		}
	}

	private void ProcessTrend(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_fastMaValue = fastValue;
		_slowMaValue = slowValue;

		TryGenerateSignal(candle);
	}

	private void ProcessMomentum(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var difference = Math.Abs(momentumValue - 100m);

		// Store the last three momentum deviations from the neutral 100 level.
		_momentumDiff3 = _momentumDiff2;
		_momentumDiff2 = _momentumDiff1;
		_momentumDiff1 = difference;
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished || !macdValue.IsFinal)
			return;

		if (macdValue is not MovingAverageConvergenceDivergenceValue macdData)
			return;

		if (macdData.Macd is not decimal macdLine || macdData.Signal is not decimal signalLine)
			return;

		_macdValue = macdLine;
		_macdSignalValue = signalLine;
	}

	private void TryGenerateSignal(ICandleMessage candle)
	{
		// Prevent duplicate orders on the same bar.
		if (candle.CloseTime == _lastSignalTime)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_fastMaValue is not decimal fast || _slowMaValue is not decimal slow)
			return;

		// Ensure momentum and MACD filters are ready.
		if (_momentumDiff1 is not decimal _)
			return;

		if (_macdValue is not decimal macd || _macdSignalValue is not decimal macdSignal)
			return;

		var distance = DistancePoints > 0m ? DistancePoints * GetPointValue() : 0m;
		var fastAbove = fast > slow + distance;
		var slowAbove = slow > fast + distance;

		var buyMomentum = HasMomentumAboveThreshold(MomentumBuyThreshold);
		var sellMomentum = HasMomentumAboveThreshold(MomentumSellThreshold);

		var macdBullish = macd > macdSignal;
		var macdBearish = macd < macdSignal;

		// Execute long and short entries when all filters align.
		if (fastAbove && buyMomentum && macdBullish && Position <= 0)
		{
			BuyMarket();
			_lastSignalTime = candle.CloseTime;
		}
		else if (slowAbove && sellMomentum && macdBearish && Position >= 0)
		{
			SellMarket();
			_lastSignalTime = candle.CloseTime;
		}
	}

	private bool HasMomentumAboveThreshold(decimal threshold)
	{
		if (_momentumDiff1 is decimal current && current >= threshold)
			return true;

		if (_momentumDiff2 is decimal previous && previous >= threshold)
			return true;

		if (_momentumDiff3 is decimal older && older >= threshold)
			return true;

		return false;
	}

	private decimal GetPointValue()
	{
		var step = Security?.PriceStep;
		return step > 0 ? step.Value : 1m;
	}
}

