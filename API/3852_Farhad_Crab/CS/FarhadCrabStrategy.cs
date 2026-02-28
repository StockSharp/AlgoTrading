using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Farhad Crab strategy - EMA and SMA crossover with trend filter.
/// Buys when EMA crosses above SMA.
/// Sells when EMA crosses below SMA.
/// </summary>
public class FarhadCrabStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEma;
	private decimal _prevSma;
	private bool _hasPrev;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FarhadCrabStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 10)
			.SetDisplay("EMA Period", "EMA lookback", "Indicators");

		_smaPeriod = Param(nameof(SmaPeriod), 20)
			.SetDisplay("SMA Period", "SMA lookback", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var sma = new SimpleMovingAverage { Length = SmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema, decimal sma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevEma = ema;
			_prevSma = sma;
			_hasPrev = true;
			return;
		}

		if (_prevEma <= _prevSma && ema > sma && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (_prevEma >= _prevSma && ema < sma && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevEma = ema;
		_prevSma = sma;
	}
}
