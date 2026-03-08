using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Liquidex breakout strategy using Keltner Channels.
/// </summary>
public class LiquidexStrategy : Strategy
{
	private readonly StrategyParam<int> _kcPeriod;
	private readonly StrategyParam<bool> _useKcFilter;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _moveToBe;
	private readonly StrategyParam<decimal> _moveToBeOffset;
	private readonly StrategyParam<decimal> _trailingDistance;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _breakoutPercent;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private int _cooldownRemaining;

	/// <summary>
	/// Keltner Channels period.
	/// </summary>
	public int KcPeriod
	{
		get => _kcPeriod.Value;
		set => _kcPeriod.Value = value;
	}

	/// <summary>
	/// Use Keltner Channel breakout filter.
	/// </summary>
	public bool UseKcFilter
	{
		get => _useKcFilter.Value;
		set => _useKcFilter.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Profit to move stop to break-even.
	/// </summary>
	public decimal MoveToBe
	{
		get => _moveToBe.Value;
		set => _moveToBe.Value = value;
	}

	/// <summary>
	/// Offset from entry when moving stop to break-even.
	/// </summary>
	public decimal MoveToBeOffset
	{
		get => _moveToBeOffset.Value;
		set => _moveToBeOffset.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price units.
	/// </summary>
	public decimal TrailingDistance
	{
		get => _trailingDistance.Value;
		set => _trailingDistance.Value = value;
	}

	/// <summary>
	/// Candle type to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Minimum breakout percentage beyond the Keltner boundary.
	/// </summary>
	public decimal BreakoutPercent
	{
		get => _breakoutPercent.Value;
		set => _breakoutPercent.Value = value;
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
	/// Initialize Liquidex strategy.
	/// </summary>
	public LiquidexStrategy()
	{
		_kcPeriod = Param(nameof(KcPeriod), 10)
			.SetDisplay("KC Period", "Keltner Channels period", "Parameters");
		_useKcFilter = Param(nameof(UseKcFilter), true)
			.SetDisplay("Use KC Filter", "Enable Keltner Channels breakout filter", "Parameters");
		_stopLoss = Param(nameof(StopLoss), 60m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 120m)
			.SetDisplay("Take Profit", "Take profit in price units, 0 disables", "Risk");
		_moveToBe = Param(nameof(MoveToBe), 30m)
			.SetDisplay("Move To BE", "Profit to move stop to break-even, 0 disables", "Risk");
		_moveToBeOffset = Param(nameof(MoveToBeOffset), 4m)
			.SetDisplay("BE Offset", "Offset when moving stop to break-even", "Risk");
		_trailingDistance = Param(nameof(TrailingDistance), 15m)
			.SetDisplay("Trailing", "Trailing stop distance, 0 disables", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle", "Candle type", "General");
		_breakoutPercent = Param(nameof(BreakoutPercent), 0.0025m)
			.SetDisplay("Breakout %", "Minimum breakout beyond Keltner boundary", "Filters");
		_cooldownBars = Param(nameof(CooldownBars), 6)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading");
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
		_stopPrice = 0m;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var keltner = new KeltnerChannels { Length = KcPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(keltner, ProcessCandle).Start();

		StartProtection(null, null);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, keltner);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue keltnerValue)
	{
		if (candle.State != CandleStates.Finished || !keltnerValue.IsFinal)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var kc = (KeltnerChannelsValue)keltnerValue;
		if (kc.Upper is not decimal upper || kc.Lower is not decimal lower || kc.Middle is not decimal middle)
			return;

		var price = candle.ClosePrice;
		var longBreakout = price > upper && price >= upper * (1m + BreakoutPercent);
		var shortBreakout = price < lower && price <= lower * (1m - BreakoutPercent);

		if (Position == 0 && _cooldownRemaining == 0)
		{
			if (!UseKcFilter || longBreakout)
			{
				BuyMarket();
				_entryPrice = price;
				_stopPrice = price - StopLoss;
				_cooldownRemaining = CooldownBars;
			}
			else if (!UseKcFilter || shortBreakout)
			{
				SellMarket();
				_entryPrice = price;
				_stopPrice = price + StopLoss;
				_cooldownRemaining = CooldownBars;
			}
		}
		else if (Position > 0)
		{
			if (TakeProfit > 0m && price >= _entryPrice + TakeProfit)
			{
				SellMarket();
				_cooldownRemaining = CooldownBars;
			}
			else if (price <= _stopPrice)
			{
				SellMarket();
				_cooldownRemaining = CooldownBars;
			}
			else
			{
				if (MoveToBe > 0m && price - _entryPrice >= MoveToBe)
					_stopPrice = Math.Max(_stopPrice, _entryPrice + MoveToBeOffset);
				if (TrailingDistance > 0m)
					_stopPrice = Math.Max(_stopPrice, price - TrailingDistance);
			}
		}
		else if (Position < 0)
		{
			if (TakeProfit > 0m && price <= _entryPrice - TakeProfit)
			{
				BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
			else if (price >= _stopPrice)
			{
				BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
			else
			{
				if (MoveToBe > 0m && _entryPrice - price >= MoveToBe)
					_stopPrice = Math.Min(_stopPrice, _entryPrice - MoveToBeOffset);
				if (TrailingDistance > 0m)
					_stopPrice = Math.Min(_stopPrice, price + TrailingDistance);
			}
		}
	}
}
