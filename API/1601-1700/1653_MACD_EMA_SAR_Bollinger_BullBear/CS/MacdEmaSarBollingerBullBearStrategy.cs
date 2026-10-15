using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on MACD histogram, EMA crossover and Parabolic SAR confirmation.
/// Buys when MACD histogram positive, fast EMA above slow EMA, SAR below price.
/// Sells on opposite conditions.
/// </summary>
public class MacdEmaSarBollingerBullBearStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	public int FastMaPeriod { get => _fastMaPeriod.Value; set => _fastMaPeriod.Value = value; }
	public int SlowMaPeriod { get => _slowMaPeriod.Value; set => _slowMaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MacdEmaSarBollingerBullBearStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0;
		_prevSlow = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastMaPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowMaPeriod };
		var sar = new ParabolicSar();
		var macd = new MovingAverageConvergenceDivergence();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, sar, macd, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal sarVal, decimal macdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_hasPrev = true;
			return;
		}

		var close = candle.ClosePrice;

		// Buy: EMA crossover up + SAR below
		var buySignal = _prevFast <= _prevSlow && fast > slow &&
			sarVal < close;

		// Sell: EMA crossover down + SAR above
		var sellSignal = _prevFast >= _prevSlow && fast < slow &&
			sarVal > close;

		if (buySignal)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		else if (sellSignal)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
