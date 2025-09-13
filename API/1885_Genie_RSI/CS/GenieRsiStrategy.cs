using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI-based reversal strategy using fixed thresholds.
/// Sells when RSI rises above overbought level and buys when RSI falls below oversold level.
/// Includes optional take profit and trailing stop management.
/// </summary>
public class GenieRsiStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _trailingLevel;
	private bool _isLong;

	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price units.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public GenieRsiStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance in price units", "Risk Management")
			.SetCanOptimize(true);

		_trailingStop = Param(nameof(TrailingStop), 200m)
			.SetGreaterThanOrEqualsZero()
			.SetDisplay("Trailing Stop", "Trailing stop distance in price units", "Risk Management");

		_rsiPeriod = Param(nameof(RsiPeriod), 15)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period for RSI indicator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_trailingLevel = 0m;
		_isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RSI { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		// Entry logic when flat
		if (Position == 0)
		{
			if (rsi > 80)
			{
				SellMarket(Volume);
				_entryPrice = price;
				_trailingLevel = price + TrailingStop;
				_isLong = false;
			}
			else if (rsi < 20)
			{
				BuyMarket(Volume);
				_entryPrice = price;
				_trailingLevel = price - TrailingStop;
				_isLong = true;
			}
			return;
		}

		// Manage open position
		if (_isLong)
		{
			// Update trailing stop for long position
			if (TrailingStop > 0)
			{
				var newLevel = price - TrailingStop;
				if (newLevel > _trailingLevel)
					_trailingLevel = newLevel;
				if (price <= _trailingLevel)
				{
					SellMarket(Position);
					_entryPrice = 0m;
					return;
				}
			}

			// Take profit for long position
			if (TakeProfit > 0 && price - _entryPrice >= TakeProfit)
			{
				SellMarket(Position);
				_entryPrice = 0m;
				return;
			}

			// Exit if RSI indicates overbought
			if (rsi > 80)
			{
				SellMarket(Position);
				_entryPrice = 0m;
			}
		}
		else
		{
			// Update trailing stop for short position
			if (TrailingStop > 0)
			{
				var newLevel = price + TrailingStop;
				if (_trailingLevel == 0m || newLevel < _trailingLevel)
					_trailingLevel = newLevel;
				if (price >= _trailingLevel)
				{
					BuyMarket(Math.Abs(Position));
					_entryPrice = 0m;
					return;
				}
			}

			// Take profit for short position
			if (TakeProfit > 0 && _entryPrice - price >= TakeProfit)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = 0m;
				return;
			}

			// Exit if RSI indicates oversold
			if (rsi < 20)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = 0m;
			}
		}
	}
}
