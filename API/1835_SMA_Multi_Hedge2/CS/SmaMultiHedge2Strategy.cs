using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on SMA trend direction (three consecutive rising/falling SMA values).
/// Buys on uptrend, sells on downtrend.
/// Simplified from multi-currency hedging to single instrument.
/// </summary>
public class SmaMultiHedge2Strategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevSma1;
	private decimal _prevSma2;

	/// <summary>SMA period for trend detection.</summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	/// <summary>Candle type.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public SmaMultiHedge2Strategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 20)
			.SetDisplay("SMA Period", "Period for trend SMA", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles for analysis", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = SmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevSma1 == 0 || _prevSma2 == 0)
		{
			_prevSma2 = _prevSma1;
			_prevSma1 = smaValue;
			return;
		}

		// Determine trend using three SMA values
		var trend = 0;
		if (_prevSma2 < _prevSma1 && _prevSma1 < smaValue)
			trend = 1;
		else if (_prevSma2 > _prevSma1 && _prevSma1 > smaValue)
			trend = -1;

		_prevSma2 = _prevSma1;
		_prevSma1 = smaValue;

		if (trend == 1 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (trend == -1 && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
	}
}
