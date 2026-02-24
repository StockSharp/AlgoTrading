using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that enters on EMA crossover and moves stop to breakeven after profit.
/// </summary>
public class StopLossToBreakEvenStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _breakEvenDist;
	private readonly StrategyParam<decimal> _initialStop;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private bool _stopMoved;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public decimal BreakEvenDist { get => _breakEvenDist.Value; set => _breakEvenDist.Value = value; }
	public decimal InitialStop { get => _initialStop.Value; set => _initialStop.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public StopLossToBreakEvenStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast EMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow EMA period", "Indicators");

		_breakEvenDist = Param(nameof(BreakEvenDist), 200m)
			.SetDisplay("Break-even Distance", "Profit before moving stop to breakeven", "Risk");

		_initialStop = Param(nameof(InitialStop), 400m)
			.SetDisplay("Initial Stop", "Initial stop loss distance", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 600m)
			.SetDisplay("Take Profit", "Take profit distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_stopPrice = 0;
		_stopMoved = false;

		var fast = new ExponentialMovingAverage { Length = FastPeriod };
		var slow = new ExponentialMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fast, slow, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		// Manage existing position
		if (Position > 0)
		{
			// Move stop to breakeven when profit exceeds threshold
			if (!_stopMoved && price - _entryPrice >= BreakEvenDist)
			{
				_stopPrice = _entryPrice;
				_stopMoved = true;
			}

			// Check stop or TP
			if (price - _entryPrice >= TakeProfit || price <= _stopPrice)
			{
				SellMarket();
				_entryPrice = 0;
				_stopPrice = 0;
				_stopMoved = false;
				return;
			}
		}
		else if (Position < 0)
		{
			if (!_stopMoved && _entryPrice - price >= BreakEvenDist)
			{
				_stopPrice = _entryPrice;
				_stopMoved = true;
			}

			if (_entryPrice - price >= TakeProfit || price >= _stopPrice)
			{
				BuyMarket();
				_entryPrice = 0;
				_stopPrice = 0;
				_stopMoved = false;
				return;
			}
		}

		// Entry on EMA crossover
		if (Position == 0)
		{
			if (fast > slow)
			{
				BuyMarket();
				_entryPrice = price;
				_stopPrice = price - InitialStop;
				_stopMoved = false;
			}
			else if (fast < slow)
			{
				SellMarket();
				_entryPrice = price;
				_stopPrice = price + InitialStop;
				_stopMoved = false;
			}
		}
	}
}
