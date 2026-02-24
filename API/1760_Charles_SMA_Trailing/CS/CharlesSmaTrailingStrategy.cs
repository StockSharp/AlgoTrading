using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SMA crossover strategy with optional trailing stop management.
/// </summary>
public class CharlesSmaTrailingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailStart;
	private readonly StrategyParam<decimal> _trailingAmount;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private bool _trailActive;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal TrailStart { get => _trailStart.Value; set => _trailStart.Value = value; }
	public decimal TrailingAmount { get => _trailingAmount.Value; set => _trailingAmount.Value = value; }

	public CharlesSmaTrailingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");

		_fastPeriod = Param(nameof(FastPeriod), 18)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Length of fast SMA", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 60)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Length of slow SMA", "Indicators");

		_stopLoss = Param(nameof(StopLoss), 500m)
			.SetDisplay("Stop Loss", "Fixed stop loss", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 1000m)
			.SetDisplay("Take Profit", "Fixed take profit", "Risk");

		_trailStart = Param(nameof(TrailStart), 500m)
			.SetDisplay("Trail Start", "Profit to activate trailing", "Risk");

		_trailingAmount = Param(nameof(TrailingAmount), 300m)
			.SetDisplay("Trailing Amount", "Trailing stop distance", "Risk");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastSma = new SMA { Length = FastPeriod };
		var slowSma = new SMA { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastSma, slowSma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Manage existing positions first
		if (Position > 0)
		{
			if (StopLoss > 0 && candle.ClosePrice <= _stopPrice)
			{
				SellMarket();
				return;
			}

			if (TakeProfit > 0 && candle.ClosePrice - _entryPrice >= TakeProfit)
			{
				SellMarket();
				return;
			}

			if (TrailStart > 0 && TrailingAmount > 0)
			{
				var move = candle.ClosePrice - _entryPrice;
				if (!_trailActive && move >= TrailStart)
					_trailActive = true;

				if (_trailActive)
				{
					var newStop = candle.ClosePrice - TrailingAmount;
					if (newStop > _stopPrice)
						_stopPrice = newStop;

					if (candle.ClosePrice <= _stopPrice)
					{
						SellMarket();
						return;
					}
				}
			}
		}
		else if (Position < 0)
		{
			if (StopLoss > 0 && candle.ClosePrice >= _stopPrice)
			{
				BuyMarket();
				return;
			}

			if (TakeProfit > 0 && _entryPrice - candle.ClosePrice >= TakeProfit)
			{
				BuyMarket();
				return;
			}

			if (TrailStart > 0 && TrailingAmount > 0)
			{
				var move = _entryPrice - candle.ClosePrice;
				if (!_trailActive && move >= TrailStart)
					_trailActive = true;

				if (_trailActive)
				{
					var newStop = candle.ClosePrice + TrailingAmount;
					if (newStop < _stopPrice)
						_stopPrice = newStop;

					if (candle.ClosePrice >= _stopPrice)
					{
						BuyMarket();
						return;
					}
				}
			}
		}

		// Entry signals
		if (fast > slow && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_stopPrice = StopLoss > 0 ? _entryPrice - StopLoss : 0m;
			_trailActive = false;
		}
		else if (fast < slow && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_stopPrice = StopLoss > 0 ? _entryPrice + StopLoss : 0m;
			_trailActive = false;
		}
	}
}
