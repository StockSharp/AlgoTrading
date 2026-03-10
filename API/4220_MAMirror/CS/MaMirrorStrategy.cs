namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class MaMirrorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _movingPeriod;

	private SimpleMovingAverage _sma;
	private decimal? _prevDiff;

	public MaMirrorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle type", "Timeframe processed by the strategy.", "General");

		_movingPeriod = Param(nameof(MovingPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Moving period", "Length of the SMA.", "Indicator");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MovingPeriod
	{
		get => _movingPeriod.Value;
		set => _movingPeriod.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevDiff = null;
		_sma = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevDiff = null;

		_sma = new SimpleMovingAverage { Length = MovingPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_sma.IsFormed)
			return;

		// Mirror: compare close vs SMA (of close). When close crosses above SMA = buy, below = sell.
		var diff = candle.ClosePrice - smaValue;

		if (_prevDiff is not null)
		{
			if (_prevDiff.Value <= 0 && diff > 0 && Position <= 0)
			{
				BuyMarket();
			}
			else if (_prevDiff.Value >= 0 && diff < 0 && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevDiff = diff;
	}
}
