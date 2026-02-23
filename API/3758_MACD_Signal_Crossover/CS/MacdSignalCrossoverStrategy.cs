using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MacdSignalCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private bool _prevMacdAboveSignal;
	private bool _hasPrev;

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public MacdSignalCrossoverStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 23);
		_slowPeriod = Param(nameof(SlowPeriod), 40);
		_signalPeriod = Param(nameof(SignalPeriod), 8);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastPeriod },
				LongMa = { Length = SlowPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal)
			return;

		var macdTyped = macdValue as MovingAverageConvergenceDivergenceSignalValue;
		if (macdTyped == null)
			return;

		var macdLine = macdTyped.Macd;
		var signalLine = macdTyped.Signal;

		if (macdLine == null || signalLine == null)
			return;

		var isMacdAboveSignal = macdLine.Value > signalLine.Value;

		if (!_hasPrev)
		{
			_hasPrev = true;
			_prevMacdAboveSignal = isMacdAboveSignal;
			return;
		}

		var crossedAbove = isMacdAboveSignal && !_prevMacdAboveSignal;
		var crossedBelow = !isMacdAboveSignal && _prevMacdAboveSignal;

		if (crossedAbove)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		else if (crossedBelow)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}

		_prevMacdAboveSignal = isMacdAboveSignal;
	}
}
