using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Spectral RVI crossover strategy.
/// Applies smoothing to RVI and its signal line and trades on their crossovers.
/// </summary>
public class SpectralRviStrategy : Strategy
{
	private readonly StrategyParam<int> _rviLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeVigorIndex _rvi;
	private SimpleMovingAverage _signal;
	private SimpleMovingAverage _smoothRvi;
	private SimpleMovingAverage _smoothSignal;

	private decimal? _prevRvi;
	private decimal? _prevSignal;

	/// <summary>
	/// Length for RVI calculation.
	/// </summary>
	public int RviLength
	{
		get => _rviLength.Value;
		set => _rviLength.Value = value;
	}

	/// <summary>
	/// Length for the RVI signal line.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Length of smoothing applied to RVI and its signal.
	/// </summary>
	public int SmoothLength
	{
		get => _smoothLength.Value;
		set => _smoothLength.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SpectralRviStrategy"/>.
	/// </summary>
	public SpectralRviStrategy()
	{
		_rviLength = Param(nameof(RviLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RVI Length", "Length for RVI", "General")
			.SetCanOptimize(true);

		_signalLength = Param(nameof(SignalLength), 4)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Length for signal", "General")
			.SetCanOptimize(true);

		_smoothLength = Param(nameof(SmoothLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Smooth Length", "Smoothing length", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_rvi = default;
		_signal = default;
		_smoothRvi = default;
		_smoothSignal = default;
		_prevRvi = default;
		_prevSignal = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rvi = new RelativeVigorIndex { Length = RviLength };
		_signal = new SimpleMovingAverage { Length = SignalLength };
		_smoothRvi = new SimpleMovingAverage { Length = SmoothLength };
		_smoothSignal = new SimpleMovingAverage { Length = SmoothLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _smoothRvi);
			DrawIndicator(area, _smoothSignal);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rviValue = _rvi.Process(candle);
		var signalValue = _signal.Process(rviValue);
		var smoothRviValue = _smoothRvi.Process(rviValue);
		var smoothSignalValue = _smoothSignal.Process(signalValue);

		if (!smoothRviValue.IsFinal || !smoothSignalValue.IsFinal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rvi = smoothRviValue.ToDecimal();
		var signal = smoothSignalValue.ToDecimal();

		var longCondition = _prevRvi <= _prevSignal && rvi > signal;
		var shortCondition = _prevRvi >= _prevSignal && rvi < signal;

		if (longCondition && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
		}
		else if (shortCondition && Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			SellMarket(volume);
		}

		_prevRvi = rvi;
		_prevSignal = signal;
	}
}
