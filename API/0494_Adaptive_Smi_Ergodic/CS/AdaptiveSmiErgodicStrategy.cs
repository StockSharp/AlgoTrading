namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Adaptive SMI Ergodic Strategy - uses True Strength Index crossovers with signal line confirmation
/// </summary>
public class AdaptiveSmiErgodicStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<decimal> _oversoldThreshold;
	private readonly StrategyParam<decimal> _overboughtThreshold;

	private TrueStrengthIndex _tsi;
	private ExponentialMovingAverage _signal;
	private decimal _previousTsi;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Long smoothing length for TSI.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}

	/// <summary>
	/// Short smoothing length for TSI.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	/// <summary>
	/// Signal line EMA length.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Oversold threshold.
	/// </summary>
	public decimal OversoldThreshold
	{
		get => _oversoldThreshold.Value;
		set => _oversoldThreshold.Value = value;
	}

	/// <summary>
	/// Overbought threshold.
	/// </summary>
	public decimal OverboughtThreshold
	{
		get => _overboughtThreshold.Value;
		set => _overboughtThreshold.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AdaptiveSmiErgodicStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_longLength = Param(nameof(LongLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Long Length", "Long smoothing length", "TSI")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_shortLength = Param(nameof(ShortLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Short Length", "Short smoothing length", "TSI")
			.SetCanOptimize(true)
			.SetOptimize(2, 15, 1);

		_signalLength = Param(nameof(SignalLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Signal EMA length", "TSI")
			.SetCanOptimize(true)
			.SetOptimize(2, 15, 1);

		_oversoldThreshold = Param(nameof(OversoldThreshold), -0.4m)
			.SetDisplay("Oversold Threshold", "Oversold level", "TSI");

		_overboughtThreshold = Param(nameof(OverboughtThreshold), 0.4m)
			.SetDisplay("Overbought Threshold", "Overbought level", "TSI");
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
		_previousTsi = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tsi = new TrueStrengthIndex
		{
			ShortLength = ShortLength,
			LongLength = LongLength
		};

		_signal = new ExponentialMovingAverage { Length = SignalLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_tsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _tsi);
			DrawIndicator(area, _signal);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal tsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_tsi.IsFormed)
		{
			_previousTsi = tsiValue;
			return;
		}

		var signalValue = _signal.Process(new DecimalIndicatorValue(_signal, tsiValue, candle.ServerTime));

		if (!signalValue.IsFormed)
		{
			_previousTsi = tsiValue;
			return;
		}

		var signal = signalValue.ToDecimal();
		var crossAboveOversold = _previousTsi <= OversoldThreshold && tsiValue > OversoldThreshold;
		var crossBelowOverbought = _previousTsi >= OverboughtThreshold && tsiValue < OverboughtThreshold;

		if (crossAboveOversold && tsiValue > signal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (crossBelowOverbought && tsiValue < signal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		_previousTsi = tsiValue;
	}
}
