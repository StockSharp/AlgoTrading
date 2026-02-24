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
/// Strategy based on bull/bear power comparison using EMA smoothing.
/// </summary>
public class BnBStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;

	private decimal _prevBull;
	private decimal _prevBear;
	private bool _initialized;

	// Manual EMA for bull/bear
	private decimal _bullEma;
	private decimal _bearEma;
	private decimal _k;
	private int _count;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Length { get => _length.Value; set => _length.Value = value; }

	public BnBStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for calculations", "General");

		_length = Param(nameof(Length), 14)
			.SetDisplay("EMA Length", "Length of smoothing for bulls and bears", "Parameters");
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

		_k = 2m / (Length + 1m);
		_count = 0;

		// Use a dummy SMA for Bind pattern
		var sma = new SimpleMovingAverage { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

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

		// Calculate bull/bear power
		var bullPower = candle.HighPrice - smaValue;
		var bearPower = candle.LowPrice - smaValue;

		// Manual EMA smoothing
		_count++;
		if (_count == 1)
		{
			_bullEma = bullPower;
			_bearEma = bearPower;
		}
		else
		{
			_bullEma = bullPower * _k + _bullEma * (1m - _k);
			_bearEma = bearPower * _k + _bearEma * (1m - _k);
		}

		if (_count < Length)
			return;

		if (!_initialized)
		{
			_prevBull = _bullEma;
			_prevBear = _bearEma;
			_initialized = true;
			return;
		}

		// Net power = bull + bear (bear is negative)
		var netPower = _bullEma + _bearEma;
		var prevNet = _prevBull + _prevBear;

		var crossUp = prevNet <= 0 && netPower > 0;
		var crossDown = prevNet >= 0 && netPower < 0;

		if (crossUp && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (crossDown && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevBull = _bullEma;
		_prevBear = _bearEma;
	}
}
