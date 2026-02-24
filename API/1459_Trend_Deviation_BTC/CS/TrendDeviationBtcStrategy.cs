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
/// Trend deviation strategy: EMA crossover with Bollinger Bands and Momentum confirmation.
/// </summary>
public class TrendDeviationBtcStrategy : Strategy
{
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFastEma;
	private decimal _prevSlowEma;
	private decimal _prevMomentum;

	public int BbLength { get => _bbLength.Value; set => _bbLength.Value = value; }
	public decimal BbMultiplier { get => _bbMultiplier.Value; set => _bbMultiplier.Value = value; }
	public int MomentumLength { get => _momentumLength.Value; set => _momentumLength.Value = value; }
	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrendDeviationBtcStrategy()
	{
		_bbLength = Param(nameof(BbLength), 13)
			.SetDisplay("BB Length", "Bollinger period", "Parameters");

		_bbMultiplier = Param(nameof(BbMultiplier), 2.3m)
			.SetDisplay("BB Multiplier", "Bollinger width", "Parameters");

		_momentumLength = Param(nameof(MomentumLength), 10)
			.SetDisplay("Momentum Length", "Momentum period", "Parameters");

		_fastEmaLength = Param(nameof(FastEmaLength), 15)
			.SetDisplay("Fast EMA", "Fast EMA length", "Parameters");

		_slowEmaLength = Param(nameof(SlowEmaLength), 50)
			.SetDisplay("Slow EMA", "Slow EMA length", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFastEma = 0;
		_prevSlowEma = 0;
		_prevMomentum = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = BbLength };
		var stdDev = new StandardDeviation { Length = BbLength };
		var momentum = new Momentum { Length = MomentumLength };
		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, stdDev, momentum, fastEma, slowEma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal, decimal stdVal, decimal momVal, decimal fastEmaVal, decimal slowEmaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (stdVal <= 0 || _prevFastEma == 0)
		{
			_prevFastEma = fastEmaVal;
			_prevSlowEma = slowEmaVal;
			_prevMomentum = momVal;
			return;
		}

		var upper = smaVal + BbMultiplier * stdVal;
		var lower = smaVal - BbMultiplier * stdVal;
		var source = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;

		// EMA crossover (replacing DMI cross)
		var emaCrossUp = _prevFastEma <= _prevSlowEma && fastEmaVal > slowEmaVal;
		var emaCrossDown = _prevFastEma >= _prevSlowEma && fastEmaVal < slowEmaVal;

		// BB confirmation
		var bbLong = source < upper;
		var bbShort = source > lower;

		// Momentum confirmation
		var momLong = momVal > 0 && momVal > _prevMomentum;
		var momShort = momVal < 0 && momVal < _prevMomentum;

		if (emaCrossUp && bbLong && momLong && Position <= 0)
			BuyMarket();
		else if (emaCrossDown && bbShort && momShort && Position >= 0)
			SellMarket();

		_prevFastEma = fastEmaVal;
		_prevSlowEma = slowEmaVal;
		_prevMomentum = momVal;
	}
}
