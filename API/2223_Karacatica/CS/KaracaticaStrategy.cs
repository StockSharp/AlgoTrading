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
/// Karacatica strategy uses ADX to determine trend direction and compares
/// current close price with the close price from a specified period ago.
/// It goes long when an uptrend is detected and price is rising, and
/// goes short when a downtrend is detected and price is falling.
/// </summary>
public class KaracaticaStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;

	private AverageDirectionalIndex _adx;
	private readonly Queue<decimal> _closeQueue = new();
	private int _lastSignal;

	/// <summary>
	/// Indicator period used for ADX and price comparison.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public KaracaticaStrategy()
	{
		_period = Param(nameof(Period), 30)
			.SetGreaterThanZero()
			.SetDisplay("Period", "ADX period and lookback for close comparison", "Indicators")
			.SetOptimize(20, 50, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		_adx = null;
		_closeQueue.Clear();
		_lastSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_adx = new AverageDirectionalIndex { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closeQueue.Enqueue(candle.ClosePrice);
		if (_closeQueue.Count > Period + 1)
			_closeQueue.Dequeue();

		if (_closeQueue.Count <= Period)
			return;

		if (!adxValue.IsFormed)
			return;

		var pastClose = _closeQueue.Peek();

		var adxTyped = adxValue as IAverageDirectionalIndexValue;
		if (adxTyped?.Dx is not IDirectionalIndexValue dxVal)
			return;

		var plusDi = dxVal.Plus;
		var minusDi = dxVal.Minus;

		if (plusDi is null || minusDi is null)
			return;

		var buySignal = candle.ClosePrice > pastClose && plusDi > minusDi && _lastSignal != 1;
		var sellSignal = candle.ClosePrice < pastClose && minusDi > plusDi && _lastSignal != -1;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (buySignal && Position <= 0)
		{
			BuyMarket();
			_lastSignal = 1;
		}
		else if (sellSignal && Position >= 0)
		{
			SellMarket();
			_lastSignal = -1;
		}
	}
}
