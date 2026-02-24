using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades EMA crossover at scheduled intervals with fixed stops.
/// </summary>
public class TimeTraderIntradayStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isInitialized;
	private decimal _entryPrice;

	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TimeTraderIntradayStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance", "Protection");

		_stopLoss = Param(nameof(StopLoss), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss distance", "Protection");

		_fastPeriod = Param(nameof(FastPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast EMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow EMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new EMA { Length = FastPeriod };
		var slowEma = new EMA { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastEma, slowEma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isInitialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_isInitialized = true;
			return;
		}

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;

		_prevFast = fast;
		_prevSlow = slow;

		if (Position == 0)
		{
			if (crossUp)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
			else if (crossDown)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}
		else if (Position > 0)
		{
			var price = candle.ClosePrice;
			if (price - _entryPrice >= TakeProfit || _entryPrice - price >= StopLoss || crossDown)
				SellMarket();
		}
		else if (Position < 0)
		{
			var price = candle.ClosePrice;
			if (_entryPrice - price >= TakeProfit || price - _entryPrice >= StopLoss || crossUp)
				BuyMarket();
		}
	}
}
