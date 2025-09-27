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
/// MACD strategy that replicates the original MetaTrader "MACD Sample" expert advisor behaviour.
/// </summary>
public class MacdSampleStrategy : Strategy
{

	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _trendMaPeriod;
	private readonly StrategyParam<decimal> _macdOpenLevel;
	private readonly StrategyParam<decimal> _macdCloseLevel;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _minimumHistoryCandles;

	private decimal _pointSize;
	private decimal? _prevMacd;
	private decimal? _prevSignal;
	private decimal? _trendMaCurrent;
	private decimal? _trendMaPrevious;
	private int _finishedCandles;
	private DateTimeOffset? _lastProcessedTime;

	/// <summary>
	/// Fast EMA period for the MACD indicator.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for the MACD indicator.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period for the MACD indicator.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Period of the trend EMA used as direction filter.
	/// </summary>
	public int TrendMaPeriod
	{
		get => _trendMaPeriod.Value;
		set => _trendMaPeriod.Value = value;
	}

	/// <summary>
	/// Threshold for MACD entries expressed in points (price steps).
	/// </summary>
	public decimal MacdOpenLevel
	{
		get => _macdOpenLevel.Value;
		set => _macdOpenLevel.Value = value;
	}

	/// <summary>
	/// Threshold for MACD exits expressed in points (price steps).
	/// </summary>
	public decimal MacdCloseLevel
	{
		get => _macdCloseLevel.Value;
		set => _macdCloseLevel.Value = value;
	}

	/// <summary>
	/// Take profit distance measured in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance measured in price points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
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
	/// Number of finished candles required before the strategy begins trading.
	/// </summary>
	public int MinimumHistoryCandles
	{
		get => _minimumHistoryCandles.Value;
		set => _minimumHistoryCandles.Value = value;
	}

	/// <summary>
	/// Initialize default parameters for the MACD Sample strategy.
	/// </summary>
	public MacdSampleStrategy()
	{
		Volume = 1;

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 12)
		.SetDisplay("Fast EMA", "Fast EMA period for MACD", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(6, 18, 2);

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 26)
		.SetDisplay("Slow EMA", "Slow EMA period for MACD", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 32, 2);

		_signalPeriod = Param(nameof(SignalPeriod), 9)
		.SetDisplay("Signal EMA", "Signal EMA period for MACD", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 13, 2);

		_trendMaPeriod = Param(nameof(TrendMaPeriod), 26)
		.SetDisplay("Trend EMA", "EMA period used for directional filter", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 40, 2);

		_macdOpenLevel = Param(nameof(MacdOpenLevel), 3m)
		.SetDisplay("MACD Open", "Entry threshold in MACD points", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 1m);

		_macdCloseLevel = Param(nameof(MacdCloseLevel), 2m)
		.SetDisplay("MACD Close", "Exit threshold in MACD points", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(1m, 4m, 1m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
		.SetDisplay("Take Profit", "Take profit distance in price points", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(20m, 100m, 10m);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 30m)
		.SetDisplay("Trailing Stop", "Trailing stop distance in price points", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10m, 60m, 10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Candle type used for analysis", "General");
		_minimumHistoryCandles = Param(nameof(MinimumHistoryCandles), 100)
			.SetDisplay("Warm-up candles", "Number of finished candles required before trading starts", "General")
			.SetGreaterThanZero();
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

		_prevMacd = null;
		_prevSignal = null;
		_trendMaCurrent = null;
		_trendMaPrevious = null;
		_finishedCandles = 0;
		_lastProcessedTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointSize = Security?.PriceStep ?? 1m;

		// Configure indicators exactly as in the original expert advisor.
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastEmaPeriod },
				LongMa = { Length = SlowEmaPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};

		var trendMa = new ExponentialMovingAverage { Length = TrendMaPeriod };

		// Subscribe to candles and bind indicators for automatic updates.
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(macd, ProcessMacdValues);
		subscription.Bind(trendMa, ProcessTrendMaValue);
		subscription.Start();

		// Visualize price, indicators and trades when a chart is available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawIndicator(area, trendMa);
			DrawOwnTrades(area);
		}

		var takeProfitDistance = TakeProfitPoints * _pointSize;
		var trailingDistance = TrailingStopPoints * _pointSize;

		if (takeProfitDistance > 0m || trailingDistance > 0m)
		{
			StartProtection(
			takeProfitDistance > 0m ? new Unit(takeProfitDistance, UnitTypes.Price) : null,
			trailingDistance > 0m ? new Unit(trailingDistance, UnitTypes.Price) : null,
			isStopTrailing: trailingDistance > 0m);
		}
	}

	private void ProcessTrendMaValue(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Keep current and previous EMA values for the directional filter.
		_trendMaPrevious = _trendMaCurrent;
		_trendMaCurrent = maValue;
	}

	private void ProcessMacdValues(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		// Avoid duplicate processing for the same candle.
		if (_lastProcessedTime != candle.OpenTime)
		{
			_lastProcessedTime = candle.OpenTime;
			_finishedCandles++;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_finishedCandles < MinimumHistoryCandles)
		return;

		var macdSignal = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macdSignal.Macd is not decimal macdCurrent ||
		macdSignal.Signal is not decimal signalCurrent)
		{
			return;
		}

		if (_prevMacd is not decimal macdPrevious ||
		_prevSignal is not decimal signalPrevious ||
		_trendMaCurrent is not decimal trendMaCurrent ||
		_trendMaPrevious is not decimal trendMaPrevious)
		{
			_prevMacd = macdCurrent;
			_prevSignal = signalCurrent;
			return;
		}

		var macdOpenThreshold = MacdOpenLevel * _pointSize;
		var macdCloseThreshold = MacdCloseLevel * _pointSize;

		// Determine trend direction using EMA slope.
		var isTrendUp = trendMaCurrent > trendMaPrevious;
		var isTrendDown = trendMaCurrent < trendMaPrevious;

		var buySignal = macdCurrent < 0m &&
		macdCurrent > signalCurrent &&
		macdPrevious < signalPrevious &&
		Math.Abs(macdCurrent) > macdOpenThreshold &&
		isTrendUp;

		var sellSignal = macdCurrent > 0m &&
		macdCurrent < signalCurrent &&
		macdPrevious > signalPrevious &&
		macdCurrent > macdOpenThreshold &&
		isTrendDown;

		var exitLongSignal = macdCurrent > 0m &&
		macdCurrent < signalCurrent &&
		macdPrevious > signalPrevious &&
		macdCurrent > macdCloseThreshold;

		var exitShortSignal = macdCurrent < 0m &&
		macdCurrent > signalCurrent &&
		macdPrevious < signalPrevious &&
		Math.Abs(macdCurrent) > macdCloseThreshold;

		if (buySignal && Position == 0m)
		{
			// MACD crossed up in negative territory and EMA confirms uptrend.
			BuyMarket(Volume);
			LogInfo($"Open long: MACD {macdCurrent:F5} above signal {signalCurrent:F5}.");
		}
		else if (sellSignal && Position == 0m)
		{
			// MACD crossed down in positive territory and EMA confirms downtrend.
			SellMarket(Volume);
			LogInfo($"Open short: MACD {macdCurrent:F5} below signal {signalCurrent:F5}.");
		}
		else if (exitLongSignal && Position > 0m)
		{
			// MACD crossed back below the signal line in positive zone - close long.
			SellMarket(Position);
			LogInfo($"Close long: MACD {macdCurrent:F5} dropped under signal {signalCurrent:F5}.");
		}
		else if (exitShortSignal && Position < 0m)
		{
			// MACD crossed back above the signal line in negative zone - close short.
			BuyMarket(Math.Abs(Position));
			LogInfo($"Close short: MACD {macdCurrent:F5} rose above signal {signalCurrent:F5}.");
		}

		_prevMacd = macdCurrent;
		_prevSignal = signalCurrent;
	}
}

