namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on True Strength Index crossovers of the signal line.
/// </summary>
public class ErgodicTicksVolumeIndicatorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<int> _signalLength;

	private TrueStrengthIndex _tsi;
	private ExponentialMovingAverage _signal;
	private decimal _prevTsi;
	private decimal _prevSignal;

	/// <summary>
	/// Candle type used for indicator calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast smoothing length of TSI.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	/// <summary>
	/// Slow smoothing length of TSI.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}

	/// <summary>
	/// Length of EMA used as signal line.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ErgodicTicksVolumeIndicatorStrategy"/> class.
	/// </summary>
	public ErgodicTicksVolumeIndicatorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");

		_shortLength = Param(nameof(ShortLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("Short Length", "Fast smoothing length", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);

		_longLength = Param(nameof(LongLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("Long Length", "Slow smoothing length", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);

		_signalLength = Param(nameof(SignalLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("Signal Length", "EMA length for signal line", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(2, 15, 1);
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
		_prevTsi = default;
		_prevSignal = default;
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

		var signalValue = _signal.Process(new DecimalIndicatorValue(_signal, tsiValue, candle.ServerTime));
		if (!signalValue.IsFormed)
				return;

		var signal = signalValue.ToDecimal();

		if (_prevTsi <= _prevSignal && tsiValue > signal && Position <= 0)
		{
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (_prevTsi >= _prevSignal && tsiValue < signal && Position >= 0)
		{
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevTsi = tsiValue;
		_prevSignal = signal;
	}
}
