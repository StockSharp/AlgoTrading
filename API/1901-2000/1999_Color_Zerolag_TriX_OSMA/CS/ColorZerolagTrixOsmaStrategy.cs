using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on a zero-lag TRIX OSMA oscillator.
/// Uses TRIX direction changes for trend reversal signals.
/// </summary>
public class ColorZerolagTrixOsmaStrategy : Strategy
{
	private readonly StrategyParam<int> _trixPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevOsma;
	private decimal _prevPrevOsma;
	private int _count;

	public int TrixPeriod { get => _trixPeriod.Value; set => _trixPeriod.Value = value; }
	public int SignalPeriod { get => _signalPeriod.Value; set => _signalPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorZerolagTrixOsmaStrategy()
	{
		_trixPeriod = Param(nameof(TrixPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("TRIX Period", "TRIX calculation period", "Indicator");

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Period", "Signal line EMA period", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevOsma = 0;
		_prevPrevOsma = 0;
		_count = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var trix = new Trix { Length = TrixPeriod };
		var signal = new ExponentialMovingAverage { Length = SignalPeriod };

		SubscribeCandles(CandleType)
			.Bind(trix, signal, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal trixValue, decimal signalValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var osma = trixValue - signalValue;
		_count++;

		if (_count < 3)
		{
			_prevPrevOsma = _prevOsma;
			_prevOsma = osma;
			return;
		}

		// Buy when OSMA turns up
		var turnUp = _prevOsma < _prevPrevOsma && osma > _prevOsma;
		// Sell when OSMA turns down
		var turnDown = _prevOsma > _prevPrevOsma && osma < _prevOsma;

		if (turnUp && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (turnDown && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevPrevOsma = _prevOsma;
		_prevOsma = osma;
	}
}
