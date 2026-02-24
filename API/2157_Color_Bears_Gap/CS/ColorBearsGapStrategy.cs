using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Color Bears Gap indicator.
/// Computes smoothed bears power gap and trades on zero-line crossovers.
/// </summary>
public class ColorBearsGapStrategy : Strategy
{
	private readonly StrategyParam<int> _length1;
	private readonly StrategyParam<int> _length2;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _smaClose;
	private SimpleMovingAverage _smaOpen;
	private SimpleMovingAverage _smaBullsC;
	private SimpleMovingAverage _smaBullsO;
	private decimal _prevXBullsC;
	private bool _isFirst = true;
	private decimal _prevValue;

	public int Length1 { get => _length1.Value; set => _length1.Value = value; }
	public int Length2 { get => _length2.Value; set => _length2.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorBearsGapStrategy()
	{
		_length1 = Param(nameof(Length1), 12)
			.SetDisplay("Length 1", "First smoothing length", "Parameters");

		_length2 = Param(nameof(Length2), 5)
			.SetDisplay("Length 2", "Second smoothing length", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candle subscription", "Parameters");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_smaClose = new SimpleMovingAverage { Length = Length1 };
		_smaOpen = new SimpleMovingAverage { Length = Length1 };
		_smaBullsC = new SimpleMovingAverage { Length = Length2 };
		_smaBullsO = new SimpleMovingAverage { Length = Length2 };
		_isFirst = true;
		_prevValue = 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var candleTime = candle.OpenTime;

		var smoothClose = _smaClose.Process(candle.ClosePrice, candleTime, true).GetValue<decimal>();
		var smoothOpen = _smaOpen.Process(candle.OpenPrice, candleTime, true).GetValue<decimal>();

		var bullsC = candle.HighPrice - smoothClose;
		var bullsO = candle.HighPrice - smoothOpen;

		var xbullsC = _smaBullsC.Process(bullsC, candleTime, true).GetValue<decimal>();
		var xbullsO = _smaBullsO.Process(bullsO, candleTime, true).GetValue<decimal>();

		if (_isFirst)
		{
			_prevXBullsC = xbullsC;
			_isFirst = false;
			return;
		}

		var diff = xbullsO - _prevXBullsC;
		_prevXBullsC = xbullsC;

		// Signal detection: zero-line crossover
		var prevSignal = _prevValue > 0m ? 1 : _prevValue < 0m ? -1 : 0;
		var signal = diff > 0m ? 1 : diff < 0m ? -1 : 0;

		if (prevSignal <= 0 && signal > 0)
		{
			if (Position <= 0)
				BuyMarket();
		}
		else if (prevSignal >= 0 && signal < 0)
		{
			if (Position >= 0)
				SellMarket();
		}

		_prevValue = diff;
	}
}
