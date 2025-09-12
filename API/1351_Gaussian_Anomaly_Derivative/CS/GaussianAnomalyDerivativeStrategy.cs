using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades on price anomaly and its derivative.
/// </summary>
public class GaussianAnomalyDerivativeStrategy : Strategy
{
	private readonly StrategyParam<bool> _useSma;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _thresholdCoeff;
	private readonly StrategyParam<int> _derivativeMaPeriod;
	private readonly StrategyParam<decimal> _derivativeThresholdCoeff;
	private readonly StrategyParam<bool> _tradeOnDerivative;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<int> _startBarCount;
	private readonly StrategyParam<int> _thresholdPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator _trendMa;
	private EMA _derivativeMa;
	private SMA _thresholdMa;
	private SMA _derivativeThresholdMa;

	private decimal? _prevCloseTrend;
	private int _barsProcessed;

	/// <summary>
	/// Use SMA instead of EMA for base signal.
	/// </summary>
	public bool UseSma
	{
		get => _useSma.Value;
		set => _useSma.Value = value;
	}

	/// <summary>
	/// Base signal MA period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Coefficient for base threshold.
	/// </summary>
	public decimal ThresholdCoeff
	{
		get => _thresholdCoeff.Value;
		set => _thresholdCoeff.Value = value;
	}

	/// <summary>
	/// Derivative smoothing period.
	/// </summary>
	public int DerivativeMaPeriod
	{
		get => _derivativeMaPeriod.Value;
		set => _derivativeMaPeriod.Value = value;
	}

	/// <summary>
	/// Coefficient for derivative threshold.
	/// </summary>
	public decimal DerivativeThresholdCoeff
	{
		get => _derivativeThresholdCoeff.Value;
		set => _derivativeThresholdCoeff.Value = value;
	}

	/// <summary>
	/// Use derivative signal for trading.
	/// </summary>
	public bool TradeOnDerivative
	{
		get => _tradeOnDerivative.Value;
		set => _tradeOnDerivative.Value = value;
	}

	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Allow long trades.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Bars to skip before trading starts.
	/// </summary>
	public int StartBarCount
	{
		get => _startBarCount.Value;
		set => _startBarCount.Value = value;
	}

	/// <summary>
	/// Period for threshold calculation.
	/// </summary>
	public int ThresholdPeriod
	{
		get => _thresholdPeriod.Value;
		set => _thresholdPeriod.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public GaussianAnomalyDerivativeStrategy()
	{
		_useSma = Param(nameof(UseSma), true)
		.SetDisplay("Use SMA", "Use SMA instead of EMA for base signal", "Signal");

		_maPeriod = Param(nameof(MaPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Base signal MA period", "Signal")
		.SetCanOptimize(true)
		.SetOptimize(1, 10, 1);

		_thresholdCoeff = Param(nameof(ThresholdCoeff), 1.0m)
		.SetGreaterThanZero()
		.SetDisplay("Threshold Coeff", "Coefficient for base signal threshold", "Signal")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 2.0m, 0.5m);

		_derivativeMaPeriod = Param(nameof(DerivativeMaPeriod), 2)
		.SetGreaterThanZero()
		.SetDisplay("Derivative MA Period", "Derivative smoothing period", "Derivative")
		.SetCanOptimize(true)
		.SetOptimize(1, 10, 1);

		_derivativeThresholdCoeff = Param(nameof(DerivativeThresholdCoeff), 1.0m)
		.SetGreaterThanZero()
		.SetDisplay("Derivative Threshold Coeff", "Coefficient for derivative threshold", "Derivative")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 2.0m, 0.5m);

		_tradeOnDerivative = Param(nameof(TradeOnDerivative), true)
		.SetDisplay("Trade on Derivative", "Use derivative signal for trading", "Trading");

		_enableShort = Param(nameof(EnableShort), true)
		.SetDisplay("Short", "Allow short trades", "Trading");

		_enableLong = Param(nameof(EnableLong), true)
		.SetDisplay("Long", "Allow long trades", "Trading");

		_startBarCount = Param(nameof(StartBarCount), 600)
		.SetGreaterThanZero()
		.SetDisplay("Start Bar Count", "Bars to skip before trading starts", "Trading");

		_thresholdPeriod = Param(nameof(ThresholdPeriod), 100)
		.SetGreaterThanZero()
		.SetDisplay("Threshold Period", "Period for threshold calculation", "Signal")
		.SetCanOptimize(true)
		.SetOptimize(50, 300, 50);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevCloseTrend = null;
		_barsProcessed = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_trendMa = UseSma ? new SMA { Length = MaPeriod } : new EMA { Length = MaPeriod } as IIndicator;
		_derivativeMa = new EMA { Length = DerivativeMaPeriod };
		_thresholdMa = new SMA { Length = ThresholdPeriod };
		_derivativeThresholdMa = new SMA { Length = ThresholdPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _trendMa);
			DrawIndicator(area, _derivativeMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var value = 1m - (candle.HighPrice + candle.LowPrice) / (2m * candle.ClosePrice);
		var trendValue = _trendMa.Process(value);
		if (!trendValue.IsFinal)
		return;

		var closeTrend = trendValue.GetValue<decimal>();

		var thValue = _thresholdMa.Process(Math.Max(closeTrend, 0m));
		var th = thValue.IsFinal ? thValue.GetValue<decimal>() * ThresholdCoeff : 0m;

		decimal dv = 0m;
		decimal th2 = 0m;

		if (_prevCloseTrend.HasValue)
		{
			var dvValue = _derivativeMa.Process(closeTrend - _prevCloseTrend.Value);
			if (dvValue.IsFinal)
			{
				dv = dvValue.GetValue<decimal>();
				var th2Value = _derivativeThresholdMa.Process(Math.Max(dv, 0m));
				if (th2Value.IsFinal)
				th2 = th2Value.GetValue<decimal>() * DerivativeThresholdCoeff;
			}
		}

		_prevCloseTrend = closeTrend;

		_barsProcessed++;
		if (_barsProcessed < StartBarCount)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (TradeOnDerivative)
		{
			if (dv > th2 && EnableLong && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
			else if (dv < -th2 && EnableShort && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		}
		else
		{
			if (closeTrend > th && EnableLong && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
			else if (closeTrend < -th && EnableShort && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
