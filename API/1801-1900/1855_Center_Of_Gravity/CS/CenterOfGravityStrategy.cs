using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Center of Gravity Strategy.
/// Uses SMA and WMA crossover.
/// Opens long when SMA crosses above WMA and short on the opposite cross.
/// </summary>
public class CenterOfGravityStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;

	private decimal _prevSma;
	private decimal _prevWma;
	private bool _initialized;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }

	public CenterOfGravityStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculation", "General");

		_period = Param(nameof(Period), 10)
			.SetDisplay("Period", "Center of Gravity averaging period", "Indicators");
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
		_prevSma = 0m;
		_prevWma = 0m;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = Period };
		var wma = new WeightedMovingAverage { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, wma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawIndicator(area, wma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal wmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_initialized)
		{
			_prevSma = smaValue;
			_prevWma = wmaValue;
			_initialized = true;
			return;
		}

		var crossUp = _prevSma <= _prevWma && smaValue > wmaValue;
		var crossDown = _prevSma >= _prevWma && smaValue < wmaValue;

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

		_prevSma = smaValue;
		_prevWma = wmaValue;
	}
}
