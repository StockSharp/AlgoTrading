using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class ObvTrafficLightsStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _obv;
	private decimal _prevFast;
	private decimal _prevSlow;
	private int _count;
	private DateTimeOffset _lastSignal = DateTimeOffset.MinValue;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ObvTrafficLightsStrategy()
	{
		_fastLength = Param(nameof(FastLength), 5).SetGreaterThanZero();
		_slowLength = Param(nameof(SlowLength), 14).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0;
		_obv = 0;
		_prevFast = 0;
		_prevSlow = 0;
		_count = 0;
		_lastSignal = DateTimeOffset.MinValue;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = 0;
		_obv = 0;
		_prevFast = 0;
		_prevSlow = 0;
		_count = 0;

		// Use a dummy indicator to ensure IsFormed checks work through Bind
		var sma = new SimpleMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_count++;

		// OBV calculation
		if (_prevClose != 0)
		{
			if (candle.ClosePrice > _prevClose)
				_obv += candle.TotalVolume;
			else if (candle.ClosePrice < _prevClose)
				_obv -= candle.TotalVolume;
		}
		_prevClose = candle.ClosePrice;

		// EMA of OBV
		var fastMult = 2.0m / (FastLength + 1);
		var slowMult = 2.0m / (SlowLength + 1);

		if (_count <= 2)
		{
			_prevFast = _obv;
			_prevSlow = _obv;
			return;
		}

		var fastValue = _obv * fastMult + _prevFast * (1 - fastMult);
		var slowValue = _obv * slowMult + _prevSlow * (1 - slowMult);

		_prevFast = fastValue;
		_prevSlow = slowValue;

		if (_count < SlowLength + 5)
			return;

		var goLong = _obv > slowValue && fastValue > slowValue;
		var goShort = _obv < slowValue && fastValue < slowValue;
		var cooldown = TimeSpan.FromMinutes(600);

		if (candle.OpenTime - _lastSignal < cooldown)
			return;

		if (goLong && Position <= 0)
		{
			BuyMarket();
			_lastSignal = candle.OpenTime;
		}
		else if (goShort && Position >= 0)
		{
			SellMarket();
			_lastSignal = candle.OpenTime;
		}
	}
}
