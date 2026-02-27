using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD sample strategy with EMA trend filter.
/// Buys on MACD crossover up when above EMA, sells on crossover down when below EMA.
/// </summary>
public class MacdSampleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maTrendPeriod;

	private decimal _prevMacd;
	private decimal _prevSignal;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MaTrendPeriod { get => _maTrendPeriod.Value; set => _maTrendPeriod.Value = value; }

	public MacdSampleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_maTrendPeriod = Param(nameof(MaTrendPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA trend filter period", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevMacd = 0;
		_prevSignal = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var macdSignal = new MovingAverageConvergenceDivergenceSignal();
		var ema = new ExponentialMovingAverage { Length = MaTrendPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessEma);
		subscription
			.BindEx(macdSignal, ProcessMacd)
			.Start();
	}

	private decimal _emaValue;
	private bool _hasEma;

	private void ProcessEma(ICandleMessage candle, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;
		_emaValue = emaVal;
		_hasEma = true;
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasEma)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			return;

		if (!_hasPrev)
		{
			_prevMacd = macd;
			_prevSignal = signal;
			_hasPrev = true;
			return;
		}

		var close = candle.ClosePrice;

		// Buy: MACD crosses above signal, price above EMA
		if (_prevMacd <= _prevSignal && macd > signal && close > _emaValue)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		// Sell: MACD crosses below signal, price below EMA
		else if (_prevMacd >= _prevSignal && macd < signal && close < _emaValue)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}

		_prevMacd = macd;
		_prevSignal = signal;
	}
}
