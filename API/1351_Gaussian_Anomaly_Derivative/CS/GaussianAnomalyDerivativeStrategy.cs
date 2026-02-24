using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades on price anomaly and its derivative using Gaussian-like smoothing.
/// </summary>
public class GaussianAnomalyDerivativeStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _thresholdCoeff;
	private readonly StrategyParam<int> _derivativeMaPeriod;
	private readonly StrategyParam<decimal> _derivativeThresholdCoeff;
	private readonly StrategyParam<int> _thresholdPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _trendMa;
	private ExponentialMovingAverage _derivativeMa;
	private SimpleMovingAverage _thresholdMa;
	private SimpleMovingAverage _derivativeThresholdMa;

	private decimal? _prevCloseTrend;
	private int _barsProcessed;

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public decimal ThresholdCoeff
	{
		get => _thresholdCoeff.Value;
		set => _thresholdCoeff.Value = value;
	}

	public int DerivativeMaPeriod
	{
		get => _derivativeMaPeriod.Value;
		set => _derivativeMaPeriod.Value = value;
	}

	public decimal DerivativeThresholdCoeff
	{
		get => _derivativeThresholdCoeff.Value;
		set => _derivativeThresholdCoeff.Value = value;
	}

	public int ThresholdPeriod
	{
		get => _thresholdPeriod.Value;
		set => _thresholdPeriod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public GaussianAnomalyDerivativeStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 3)
			.SetDisplay("MA Period", "Base signal MA period", "Signal");

		_thresholdCoeff = Param(nameof(ThresholdCoeff), 1.0m)
			.SetDisplay("Threshold Coeff", "Coefficient for base signal threshold", "Signal");

		_derivativeMaPeriod = Param(nameof(DerivativeMaPeriod), 2)
			.SetDisplay("Derivative MA Period", "Derivative smoothing period", "Derivative");

		_derivativeThresholdCoeff = Param(nameof(DerivativeThresholdCoeff), 1.0m)
			.SetDisplay("Derivative Threshold Coeff", "Coefficient for derivative threshold", "Derivative");

		_thresholdPeriod = Param(nameof(ThresholdPeriod), 100)
			.SetDisplay("Threshold Period", "Period for threshold calculation", "Signal");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevCloseTrend = null;
		_barsProcessed = 0;

		_trendMa = new SimpleMovingAverage { Length = MaPeriod };
		_derivativeMa = new ExponentialMovingAverage { Length = DerivativeMaPeriod };
		_thresholdMa = new SimpleMovingAverage { Length = ThresholdPeriod };
		_derivativeThresholdMa = new SimpleMovingAverage { Length = ThresholdPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var value = 1m - (candle.HighPrice + candle.LowPrice) / (2m * candle.ClosePrice);
		var trendResult = _trendMa.Process(value, candle.OpenTime, true);
		if (!trendResult.IsFinal || !_trendMa.IsFormed)
			return;

		var closeTrend = trendResult.GetValue<decimal>();

		var thResult = _thresholdMa.Process(Math.Max(closeTrend, 0m), candle.OpenTime, true);
		var th = _thresholdMa.IsFormed ? thResult.GetValue<decimal>() * ThresholdCoeff : 0m;

		decimal dv = 0m;
		decimal th2 = 0m;

		if (_prevCloseTrend.HasValue)
		{
			var dvResult = _derivativeMa.Process(closeTrend - _prevCloseTrend.Value, candle.OpenTime, true);
			if (_derivativeMa.IsFormed)
			{
				dv = dvResult.GetValue<decimal>();
				var th2Result = _derivativeThresholdMa.Process(Math.Max(dv, 0m), candle.OpenTime, true);
				if (_derivativeThresholdMa.IsFormed)
					th2 = th2Result.GetValue<decimal>() * DerivativeThresholdCoeff;
			}
		}

		_prevCloseTrend = closeTrend;
		_barsProcessed++;

		if (_barsProcessed < 200)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (dv > th2 && Position <= 0)
			BuyMarket();
		else if (dv < -th2 && Position >= 0)
			SellMarket();
	}
}
