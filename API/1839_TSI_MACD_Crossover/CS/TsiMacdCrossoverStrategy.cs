namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// True Strength Index MACD crossover strategy.
/// Generates buy when TSI crosses above its signal line and sell on opposite cross.
/// </summary>
public class TsiMacdCrossoverStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _signalLength;

	private TrueStrengthIndex _tsi;
	private ExponentialMovingAverage _signal;
	private decimal _prevTsi;
	private decimal _prevSignal;

	/// <summary>
	/// Candle type for processing.
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

	public TsiMacdCrossoverStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_longLength = Param(nameof(LongLength), 21)
			.SetDisplay("Long Length", "Slow period for TSI", "Indicators");
		_shortLength = Param(nameof(ShortLength), 8)
			.SetDisplay("Short Length", "Fast period for TSI", "Indicators");
		_signalLength = Param(nameof(SignalLength), 5)
			.SetDisplay("Signal Length", "EMA period for signal line", "Indicators");
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

		StartProtection();

		_tsi = new TrueStrengthIndex
		{
			LongLength = LongLength,
			ShortLength = ShortLength
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
		{
			_prevTsi = tsiValue;
			_prevSignal = signalValue.ToDecimal();
			return;
		}

		var signal = signalValue.ToDecimal();

		if (IsFormedAndOnlineAndAllowTrading())
		{
			var crossUp = _prevTsi <= _prevSignal && tsiValue > signal;
			var crossDown = _prevTsi >= _prevSignal && tsiValue < signal;

			if (crossUp && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (crossDown && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevTsi = tsiValue;
		_prevSignal = signal;
	}
}