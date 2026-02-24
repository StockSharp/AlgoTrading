using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple moving average crossover strategy with optional trailing stop.
/// </summary>
public class Up3x1Strategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _middlePeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _fastPrev;
	private decimal _middlePrev;
	private bool _isInitialized;
	private decimal _entryPrice;
	private decimal _currentStop;

	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int MiddlePeriod { get => _middlePeriod.Value; set => _middlePeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Up3x1Strategy()
	{
		_takeProfit = Param<decimal>(nameof(TakeProfit), 1500m)
			.SetDisplay("Take Profit", "Take Profit in price points", "General");
		_stopLoss = Param<decimal>(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Stop Loss in price points", "General");
		_trailingStop = Param<decimal>(nameof(TrailingStop), 800m)
			.SetDisplay("Trailing Stop", "Trailing Stop distance", "General");
		_fastPeriod = Param(nameof(FastPeriod), 24)
			.SetDisplay("Fast Period", "Fast SMA period", "General");
		_middlePeriod = Param(nameof(MiddlePeriod), 60)
			.SetDisplay("Middle Period", "Middle SMA period", "General");
		_slowPeriod = Param(nameof(SlowPeriod), 120)
			.SetDisplay("Slow Period", "Slow SMA period", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle Type", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastMa = new SMA { Length = FastPeriod };
		var middleMa = new SMA { Length = MiddlePeriod };
		var slowMa = new SMA { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, middleMa, slowMa, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal middle, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		if (!_isInitialized)
		{
			_fastPrev = fast;
			_middlePrev = middle;
			_isInitialized = true;
			return;
		}

		// Buy when fast crosses above middle (relaxed: no requirement relative to slow)
		var buySignal = _fastPrev <= _middlePrev && fast > middle;
		var sellSignal = _fastPrev >= _middlePrev && fast < middle;

		_fastPrev = fast;
		_middlePrev = middle;

		if (Position == 0)
		{
			if (buySignal)
			{
				BuyMarket();
				_entryPrice = price;
				_currentStop = price - StopLoss;
			}
			else if (sellSignal)
			{
				SellMarket();
				_entryPrice = price;
				_currentStop = price + StopLoss;
			}
			return;
		}

		if (Position > 0)
		{
			if (price - _entryPrice >= TakeProfit)
			{
				SellMarket();
				return;
			}

			if (TrailingStop > 0m)
			{
				_currentStop = Math.Max(_currentStop, price - TrailingStop);
				if (price <= _currentStop)
					SellMarket();
			}
			else if (price <= _currentStop)
			{
				SellMarket();
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice - price >= TakeProfit)
			{
				BuyMarket();
				return;
			}

			if (TrailingStop > 0m)
			{
				_currentStop = Math.Min(_currentStop, price + TrailingStop);
				if (price >= _currentStop)
					BuyMarket();
			}
			else if (price >= _currentStop)
			{
				BuyMarket();
			}
		}
	}
}
