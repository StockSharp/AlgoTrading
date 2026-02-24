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
/// SIDUS strategy based on moving average crossovers.
/// Buys when fast LWMA crosses above slow LWMA or when slow LWMA crosses above slow EMA.
/// Sells on opposite crossovers.
/// </summary>
public class SidusStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEma;
	private readonly StrategyParam<int> _slowEma;
	private readonly StrategyParam<int> _fastLwma;
	private readonly StrategyParam<int> _slowLwma;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastEmaIndicator;
	private ExponentialMovingAverage _slowEmaIndicator;
	private WeightedMovingAverage _fastLwmaIndicator;
	private WeightedMovingAverage _slowLwmaIndicator;

	private decimal _prevFastLwma;
	private decimal _prevSlowLwma;
	private decimal _prevSlowEma;
	private bool _isInitialized;

	public int FastEma { get => _fastEma.Value; set => _fastEma.Value = value; }
	public int SlowEma { get => _slowEma.Value; set => _slowEma.Value = value; }
	public int FastLwma { get => _fastLwma.Value; set => _fastLwma.Value = value; }
	public int SlowLwma { get => _slowLwma.Value; set => _slowLwma.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SidusStrategy()
	{
		_fastEma = Param(nameof(FastEma), 18)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Length of the fast EMA", "Sidus")
			.SetOptimize(10, 30, 2);

		_slowEma = Param(nameof(SlowEma), 28)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Length of the slow EMA", "Sidus")
			.SetOptimize(20, 50, 2);

		_fastLwma = Param(nameof(FastLwma), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast LWMA", "Length of the fast LWMA", "Sidus")
			.SetOptimize(3, 10, 1);

		_slowLwma = Param(nameof(SlowLwma), 8)
			.SetGreaterThanZero()
			.SetDisplay("Slow LWMA", "Length of the slow LWMA", "Sidus")
			.SetOptimize(5, 15, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFastLwma = 0;
		_prevSlowLwma = 0;
		_prevSlowEma = 0;
		_isInitialized = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastEmaIndicator = new ExponentialMovingAverage { Length = FastEma };
		_slowEmaIndicator = new ExponentialMovingAverage { Length = SlowEma };
		_fastLwmaIndicator = new WeightedMovingAverage { Length = FastLwma };
		_slowLwmaIndicator = new WeightedMovingAverage { Length = SlowLwma };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEmaIndicator, _slowEmaIndicator, _fastLwmaIndicator, _slowLwmaIndicator, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEmaIndicator);
			DrawIndicator(area, _slowEmaIndicator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastEmaValue, decimal slowEmaValue, decimal fastLwmaValue, decimal slowLwmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isInitialized)
		{
			_prevFastLwma = fastLwmaValue;
			_prevSlowLwma = slowLwmaValue;
			_prevSlowEma = slowEmaValue;
			_isInitialized = true;
			return;
		}

		var buySignal =
			(fastLwmaValue > slowLwmaValue && _prevFastLwma <= _prevSlowLwma) ||
			(slowLwmaValue > slowEmaValue && _prevSlowLwma <= _prevSlowEma);

		var sellSignal =
			(fastLwmaValue < slowLwmaValue && _prevFastLwma >= _prevSlowLwma) ||
			(slowLwmaValue < slowEmaValue && _prevSlowLwma >= _prevSlowEma);

		if (IsFormedAndOnlineAndAllowTrading())
		{
			if (buySignal && Position <= 0)
				BuyMarket();
			else if (sellSignal && Position >= 0)
				SellMarket();
		}

		_prevFastLwma = fastLwmaValue;
		_prevSlowLwma = slowLwmaValue;
		_prevSlowEma = slowEmaValue;
	}
}
