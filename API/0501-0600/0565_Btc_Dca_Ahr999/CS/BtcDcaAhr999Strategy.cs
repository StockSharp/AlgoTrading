using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BTC DCA strategy that uses dual EMA crossover to time
/// accumulation entries and profit-taking exits.
/// </summary>
public class BtcDcaAhr999Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;

	private decimal _prevFastEma;
	private decimal _prevSlowEma;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastEmaPeriod { get => _fastEmaPeriod.Value; set => _fastEmaPeriod.Value = value; }
	public int SlowEmaPeriod { get => _slowEmaPeriod.Value; set => _slowEmaPeriod.Value = value; }

	public BtcDcaAhr999Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 120)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 450)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");
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
		_prevFastEma = 0m;
		_prevSlowEma = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastEmaPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastEmaValue, decimal slowEmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFastEma == 0m || _prevSlowEma == 0m)
		{
			_prevFastEma = fastEmaValue;
			_prevSlowEma = slowEmaValue;
			return;
		}

		// Buy on golden cross
		if (_prevFastEma <= _prevSlowEma && fastEmaValue > slowEmaValue && Position <= 0)
		{
			BuyMarket();
		}
		// Sell on death cross
		else if (_prevFastEma >= _prevSlowEma && fastEmaValue < slowEmaValue && Position >= 0)
		{
			SellMarket();
		}

		_prevFastEma = fastEmaValue;
		_prevSlowEma = slowEmaValue;
	}
}
