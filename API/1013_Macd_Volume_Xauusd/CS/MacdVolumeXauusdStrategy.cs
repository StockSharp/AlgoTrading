using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MacdVolumeXauusdStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _shortVolumeEma;
	private ExponentialMovingAverage _longVolumeEma;
	private MovingAverageConvergenceDivergence _macd;

	private decimal _prevMacd;
	private bool _prevMacdSet;
	private int _barsFromSignal;

	public int ShortLength { get => _shortLength.Value; set => _shortLength.Value = value; }
	public int LongLength { get => _longLength.Value; set => _longLength.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MacdVolumeXauusdStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 5);
		_longLength = Param(nameof(LongLength), 10);
		_cooldownBars = Param(nameof(CooldownBars), 2);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_shortVolumeEma = null;
		_longVolumeEma = null;
		_macd = null;
		_prevMacd = 0m;
		_prevMacdSet = false;
		_barsFromSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_shortVolumeEma = new ExponentialMovingAverage { Length = ShortLength };
		_longVolumeEma = new ExponentialMovingAverage { Length = LongLength };
		_macd = new MovingAverageConvergenceDivergence();

		_prevMacd = 0;
		_prevMacdSet = false;
		_barsFromSignal = 0;

		var dummyEma1 = new ExponentialMovingAverage { Length = 10 };
		var dummyEma2 = new ExponentialMovingAverage { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(dummyEma1, dummyEma2, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal d1, decimal d2)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var t = candle.ServerTime;

		_shortVolumeEma.Process(new DecimalIndicatorValue(_shortVolumeEma, candle.TotalVolume, t));
		_longVolumeEma.Process(new DecimalIndicatorValue(_longVolumeEma, candle.TotalVolume, t));

		var macdResult = _macd.Process(new CandleIndicatorValue(_macd, candle));
		if (!_macd.IsFormed)
			return;

		var macd = macdResult.IsEmpty ? 0m : macdResult.GetValue<decimal>();

		if (!_prevMacdSet)
		{
			_prevMacd = macd;
			_prevMacdSet = true;
			return;
		}

		// MACD cross above zero = buy
		var longSignal = _prevMacd <= 0 && macd > 0;
		// MACD cross below zero = sell
		var shortSignal = _prevMacd >= 0 && macd < 0;
		if (_barsFromSignal < 10000) _barsFromSignal++;
		var canSignal = _barsFromSignal >= CooldownBars;

		if (canSignal && longSignal && Position <= 0)
		{
			BuyMarket();
			_barsFromSignal = 0;
		}
		else if (canSignal && shortSignal && Position >= 0)
		{
			SellMarket();
			_barsFromSignal = 0;
		}

		_prevMacd = macd;
	}
}
