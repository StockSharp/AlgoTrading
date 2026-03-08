using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

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
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _entryPrice;
	private decimal _trailingLevel;
	private bool _isLong;
	private decimal? _prevRsi;
	private int _cooldownRemaining;

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
	/// Number of completed candles to wait after a position change.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public GenieRsiStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance in price units", "Risk Management")
			;

		_trailingStop = Param(nameof(TrailingStop), 200m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop", "Trailing stop distance in price units", "Risk Management");

		_rsiPeriod = Param(nameof(RsiPeriod), 15)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period for RSI indicator", "Indicators")
			
			.SetOptimize(5, 30, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_cooldownBars = Param(nameof(CooldownBars), 4)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Risk Management");
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
		_prevRsi = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

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

		StartProtection(null, null);
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var price = candle.ClosePrice;
		var crossedDown = _prevRsi is decimal prevRsi1 && prevRsi1 >= 20m && rsi < 20m;
		var crossedUp = _prevRsi is decimal prevRsi2 && prevRsi2 <= 80m && rsi > 80m;
		_prevRsi = rsi;

		// Entry logic when flat
		if (Position == 0 && _cooldownRemaining == 0)
		{
			if (crossedUp)
			{
				SellMarket();
				_entryPrice = price;
				_trailingLevel = price + TrailingStop;
				_isLong = false;
				_cooldownRemaining = CooldownBars;
			}
			else if (crossedDown)
			{
				BuyMarket();
				_entryPrice = price;
				_trailingLevel = price - TrailingStop;
				_isLong = true;
				_cooldownRemaining = CooldownBars;
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
					SellMarket();
					_entryPrice = 0m;
					_cooldownRemaining = CooldownBars;
					return;
				}
			}

			// Take profit for long position
			if (TakeProfit > 0 && price - _entryPrice >= TakeProfit)
			{
				SellMarket();
				_entryPrice = 0m;
				_cooldownRemaining = CooldownBars;
				return;
			}

			// Exit if RSI indicates overbought
			if (crossedUp)
			{
				SellMarket();
				_entryPrice = 0m;
				_cooldownRemaining = CooldownBars;
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
					BuyMarket();
					_entryPrice = 0m;
					_cooldownRemaining = CooldownBars;
					return;
				}
			}

			// Take profit for short position
			if (TakeProfit > 0 && _entryPrice - price >= TakeProfit)
			{
				BuyMarket();
				_entryPrice = 0m;
				_cooldownRemaining = CooldownBars;
				return;
			}

			// Exit if RSI indicates oversold
			if (crossedDown)
			{
				BuyMarket();
				_entryPrice = 0m;
				_cooldownRemaining = CooldownBars;
			}
		}
	}
}
