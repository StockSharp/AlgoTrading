namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Fast In Fast Out scalping strategy: quick entry/exit using EMA and RSI.
/// </summary>
public class FxfFastInFastOutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;

	private decimal _prevRsi;
	private bool _hasPrev;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public FxfFastInFastOutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_emaPeriod = Param(nameof(EmaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevRsi = rsi;
			_hasPrev = true;
			return;
		}

		var close = candle.ClosePrice;

		if (_prevRsi <= 50 && rsi > 50 && close > ema && Position <= 0)
			BuyMarket();
		else if (_prevRsi >= 50 && rsi < 50 && close < ema && Position >= 0)
			SellMarket();

		_prevRsi = rsi;
	}
}
