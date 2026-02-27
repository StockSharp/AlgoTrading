namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Trading Panel Control strategy: Williams %R crossover.
/// Buys when Williams %R crosses above -80, sells when crosses below -20.
/// </summary>
public class TradingPanelControlStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;

	private decimal _prevWr;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }

	public TradingPanelControlStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_period = Param(nameof(Period), 14)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Williams %R period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var wr = new WilliamsR { Length = Period };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(wr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal wrValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			if (_prevWr < -80 && wrValue >= -80 && Position <= 0)
				BuyMarket();
			else if (_prevWr > -20 && wrValue <= -20 && Position >= 0)
				SellMarket();
		}

		_prevWr = wrValue;
		_hasPrev = true;
	}
}
