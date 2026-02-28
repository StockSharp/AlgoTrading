namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Close Profit End Of Week strategy: Momentum + EMA trend following.
/// Buys when momentum positive and close above EMA, sells when momentum negative and close below EMA.
/// </summary>
public class CloseProfitEndOfWeekStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _momPeriod;
	private readonly StrategyParam<int> _emaPeriod;

	private decimal _prevMom;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MomPeriod { get => _momPeriod.Value; set => _momPeriod.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	public CloseProfitEndOfWeekStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_momPeriod = Param(nameof(MomPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Momentum period", "Indicators");
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA filter period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var mom = new Momentum { Length = MomPeriod };
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(mom, ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal momValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			if (_prevMom <= 0 && momValue > 0 && candle.ClosePrice > emaValue && Position <= 0)
				BuyMarket();
			else if (_prevMom >= 0 && momValue < 0 && candle.ClosePrice < emaValue && Position >= 0)
				SellMarket();
		}

		_prevMom = momValue;
		_hasPrev = true;
	}
}
