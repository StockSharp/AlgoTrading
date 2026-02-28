using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AG MACD Dual strategy - MACD histogram crossover with EMA trend filter.
/// Buys when MACD histogram crosses above zero while price is above EMA.
/// Sells when MACD histogram crosses below zero while price is below EMA.
/// </summary>
public class AgMacdDualStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMacd;
	private decimal _prevSignal;
	private bool _hasPrev;
	private decimal _currentEma;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AgMacdDualStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetDisplay("EMA Period", "EMA trend filter", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var macd = new MovingAverageConvergenceDivergenceSignal();
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessMacd)
			.Bind(ema, ProcessEma)
			.Start();
	}

	private void ProcessEma(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_currentEma = ema;
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!value.IsFinal || value.IsEmpty)
			return;

		var macdVal = value as MovingAverageConvergenceDivergenceSignalValue;
		if (macdVal == null)
			return;

		var macdLine = macdVal.Macd;
		var signalLine = macdVal.Signal;

		if (macdLine == null || signalLine == null)
			return;

		var histogram = macdLine.Value - signalLine.Value;

		if (!_hasPrev)
		{
			_prevMacd = macdLine.Value;
			_prevSignal = signalLine.Value;
			_hasPrev = true;
			return;
		}

		var prevHist = _prevMacd - _prevSignal;
		var close = candle.ClosePrice;

		if (prevHist <= 0 && histogram > 0 && close > _currentEma && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (prevHist >= 0 && histogram < 0 && close < _currentEma && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevMacd = macdLine.Value;
		_prevSignal = signalLine.Value;
	}
}
