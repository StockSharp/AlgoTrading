using System;
using System.Collections.Generic;

using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on True Strength Index crossovers of the signal line.
/// </summary>
public class ErgodicTicksVolumeIndicatorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _firstLength;
	private readonly StrategyParam<int> _secondLength;
	private readonly StrategyParam<int> _signalLength;

	private decimal _prevTsi;
	private decimal _prevSignal;
	private bool _prevReady;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FirstLength { get => _firstLength.Value; set => _firstLength.Value = value; }
	public int SecondLength { get => _secondLength.Value; set => _secondLength.Value = value; }
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }

	public ErgodicTicksVolumeIndicatorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_firstLength = Param(nameof(FirstLength), 25)
			.SetGreaterThanZero()
			.SetDisplay("First Length", "First smoothing length", "Indicator");

		_secondLength = Param(nameof(SecondLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("Second Length", "Second smoothing length", "Indicator");

		_signalLength = Param(nameof(SignalLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Signal line length", "Indicator");
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
		_prevReady = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var tsi = new TrueStrengthIndex
		{
			FirstLength = FirstLength,
			SecondLength = SecondLength,
			SignalLength = SignalLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(tsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var tsiVal = (ITrueStrengthIndexValue)value;

		if (tsiVal.Tsi is not decimal tsi || tsiVal.Signal is not decimal signal)
			return;

		if (!_prevReady)
		{
			_prevTsi = tsi;
			_prevSignal = signal;
			_prevReady = true;
			return;
		}

		// TSI crosses above signal - buy
		if (_prevTsi <= _prevSignal && tsi > signal && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// TSI crosses below signal - sell
		else if (_prevTsi >= _prevSignal && tsi < signal && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevTsi = tsi;
		_prevSignal = signal;
	}
}
