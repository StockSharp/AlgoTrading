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
/// Ichimoku trend strategy combined with LWMA, momentum and MACD filters.
/// Recreates the core logic of the MetaTrader "Ichimoku" expert advisor 23469.
/// Buys when Tenkan-Kijun alignment, weighted MA trend, momentum and MACD agree.
/// Sells when the same conditions flip to bearish alignment.
/// </summary>
public class IchimokuMomentumMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouBPeriod;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _fastMaPrev;
	private decimal? _fastMaCurrent;
	private decimal? _slowMaPrev;
	private decimal? _slowMaCurrent;

	private decimal? _momentumPrev1;
	private decimal? _momentumPrev2;
	private decimal? _momentumPrev3;
	private decimal? _momentumCurrent;

	private decimal? _macdPrev;
	private decimal? _macdCurrent;
	private decimal? _macdSignalPrev;
	private decimal? _macdSignalCurrent;

	private decimal? _tenkanPrev;
	private decimal? _tenkanCurrent;
	private decimal? _kijunPrev;
	private decimal? _kijunCurrent;

	private DateTimeOffset? _maUpdateTime;
	private DateTimeOffset? _momentumUpdateTime;
	private DateTimeOffset? _macdUpdateTime;
	private DateTimeOffset? _ichimokuUpdateTime;
	private DateTimeOffset? _lastProcessedTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="IchimokuMomentumMacdStrategy"/> class.
	/// </summary>
	public IchimokuMomentumMacdStrategy()
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
			.SetOptimize(40, 120, 5);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Lookback used by the momentum oscillator", "Momentum")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 2);

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
			.SetNotNegative()
			.SetDisplay("Momentum Threshold", "Minimum distance from 100 for bullish/bearish momentum", "Momentum")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1.0m, 0.1m);

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period for MACD filter", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(8, 16, 1);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period for MACD filter", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 2);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA period for MACD filter", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Conversion line length of Ichimoku", "Ichimoku")
			.SetCanOptimize(true)
			.SetOptimize(7, 13, 1);

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Base line length of Ichimoku", "Ichimoku")
			.SetCanOptimize(true)
			.SetOptimize(20, 30, 1);

		_senkouBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("Senkou Span B", "Span B length of Ichimoku", "Ichimoku")
			.SetCanOptimize(true)
			.SetOptimize(40, 60, 2);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take profit distance measured in points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(20m, 100m, 10m);

		_stopLossPoints = Param(nameof(StopLossPoints), 20m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss distance measured in points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 60m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for calculations", "General");
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
	/// Momentum lookback length.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum momentum deviation from 100 required by the filter.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// MACD fast EMA period.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// MACD slow EMA period.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// MACD signal EMA period.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Ichimoku Tenkan length.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Ichimoku Kijun length.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Ichimoku Senkou Span B length.
	/// </summary>
	public int SenkouSpanBPeriod
	{
		get => _senkouBPeriod.Value;
		set => _senkouBPeriod.Value = value;
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
	/// Primary candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_fastMaPrev = null;
		_fastMaCurrent = null;
		_slowMaPrev = null;
		_slowMaCurrent = null;

		_momentumPrev1 = null;
		_momentumPrev2 = null;
		_momentumPrev3 = null;
		_momentumCurrent = null;

		_macdPrev = null;
		_macdCurrent = null;
		_macdSignalPrev = null;
		_macdSignalCurrent = null;

		_tenkanPrev = null;
		_tenkanCurrent = null;
		_kijunPrev = null;
		_kijunCurrent = null;

		_maUpdateTime = null;
		_momentumUpdateTime = null;
		_macdUpdateTime = null;
		_ichimokuUpdateTime = null;
		_lastProcessedTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastMa = new WeightedMovingAverage { Length = FastMaPeriod };
		var slowMa = new WeightedMovingAverage { Length = SlowMaPeriod };
		var momentum = new Momentum { Length = MomentumPeriod };
		var macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdFastPeriod,
			LongPeriod = MacdSlowPeriod,
			SignalPeriod = MacdSignalPeriod
		};
		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanBPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, ProcessMovingAverages)
			.Bind(momentum, ProcessMomentum)
			.BindEx(macd, ProcessMacd)
			.BindEx(ichimoku, ProcessIchimoku)
			.Start();

		var takeProfitUnit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints, UnitTypes.Point) : null;
		var stopLossUnit = StopLossPoints > 0m ? new Unit(StopLossPoints, UnitTypes.Point) : null;

		if (takeProfitUnit != null || stopLossUnit != null)
		{
			StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit, useMarketOrders: true);
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawIndicator(area, ichimoku);
			DrawOwnTrades(area);
		}
	}

	private void ProcessMovingAverages(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_fastMaPrev = _fastMaCurrent;
		_fastMaCurrent = fastValue;
		_slowMaPrev = _slowMaCurrent;
		_slowMaCurrent = slowValue;
		_maUpdateTime = candle.CloseTime;

		TryEvaluateSignal(candle);
	}

	private void ProcessMomentum(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var normalized = Math.Abs(momentumValue - 100m);

		_momentumPrev3 = _momentumPrev2;
		_momentumPrev2 = _momentumPrev1;
		_momentumPrev1 = _momentumCurrent;
		_momentumCurrent = normalized;
		_momentumUpdateTime = candle.CloseTime;

		TryEvaluateSignal(candle);
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal || macdValue is not MovingAverageConvergenceDivergenceValue typed)
			return;

		if (typed.Macd is not decimal macdMain || typed.Signal is not decimal macdSignal)
			return;

		_macdPrev = _macdCurrent;
		_macdCurrent = macdMain;
		_macdSignalPrev = _macdSignalCurrent;
		_macdSignalCurrent = macdSignal;
		_macdUpdateTime = candle.CloseTime;

		TryEvaluateSignal(candle);
	}

	private void ProcessIchimoku(ICandleMessage candle, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!ichimokuValue.IsFinal || ichimokuValue is not IchimokuValue typed)
			return;

		if (typed.Tenkan is not decimal tenkan || typed.Kijun is not decimal kijun)
			return;

		_tenkanPrev = _tenkanCurrent;
		_tenkanCurrent = tenkan;
		_kijunPrev = _kijunCurrent;
		_kijunCurrent = kijun;
		_ichimokuUpdateTime = candle.CloseTime;

		TryEvaluateSignal(candle);
	}

	private void TryEvaluateSignal(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var time = candle.CloseTime;

		if (_lastProcessedTime == time)
			return;

		if (_maUpdateTime != time || _momentumUpdateTime != time || _macdUpdateTime != time || _ichimokuUpdateTime != time)
			return;

		if (_fastMaPrev is null || _slowMaPrev is null || _momentumPrev1 is null || _momentumPrev2 is null || _momentumPrev3 is null ||
			_macdPrev is null || _macdSignalPrev is null || _tenkanPrev is null || _kijunPrev is null)
		{
			return;
		}

		var fastPrev = _fastMaPrev.Value;
		var slowPrev = _slowMaPrev.Value;
		var mom1 = _momentumPrev1.Value;
		var mom2 = _momentumPrev2.Value;
		var mom3 = _momentumPrev3.Value;
		var macdPrev = _macdPrev.Value;
		var macdSignalPrev = _macdSignalPrev.Value;
		var tenkanPrev = _tenkanPrev.Value;
		var kijunPrev = _kijunPrev.Value;

		var hasSufficientMomentum = mom1 >= MomentumThreshold || mom2 >= MomentumThreshold || mom3 >= MomentumThreshold;
		var macdBullish = (macdPrev > 0m && macdPrev > macdSignalPrev) || (macdPrev < 0m && macdPrev > macdSignalPrev);
		var macdBearish = (macdPrev < 0m && macdPrev < macdSignalPrev) || (macdPrev > 0m && macdPrev < macdSignalPrev);

		var longSignal = tenkanPrev > kijunPrev && fastPrev > slowPrev && hasSufficientMomentum && macdBullish;
		var shortSignal = tenkanPrev < kijunPrev && fastPrev < slowPrev && hasSufficientMomentum && macdBearish;

		if (longSignal && Position <= 0)
		{
			var volume = Volume + Math.Max(0m, -Position);
			if (volume > 0m)
			{
				BuyMarket(volume);
				LogInfo($"Long entry at {candle.ClosePrice} detected on {time}. Tenkan={tenkanPrev:F5}, Kijun={kijunPrev:F5}, " +
					$"FastLWMA={fastPrev:F5}, SlowLWMA={slowPrev:F5}, Momentum={mom1:F5}/{mom2:F5}/{mom3:F5}, MACD={macdPrev:F5}, Signal={macdSignalPrev:F5}");
			}
		}
		else if (shortSignal && Position >= 0)
		{
			var volume = Volume + Math.Max(0m, Position);
			if (volume > 0m)
			{
				SellMarket(volume);
				LogInfo($"Short entry at {candle.ClosePrice} detected on {time}. Tenkan={tenkanPrev:F5}, Kijun={kijunPrev:F5}, " +
					$"FastLWMA={fastPrev:F5}, SlowLWMA={slowPrev:F5}, Momentum={mom1:F5}/{mom2:F5}/{mom3:F5}, MACD={macdPrev:F5}, Signal={macdSignalPrev:F5}");
			}
		}

		if (Position > 0 && shortSignal)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && longSignal)
		{
			BuyMarket(Math.Abs(Position));
		}

		_lastProcessedTime = time;
	}
}

