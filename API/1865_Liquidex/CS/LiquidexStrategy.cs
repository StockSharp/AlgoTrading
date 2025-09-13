using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
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

	private decimal _entryPrice;
	private decimal _stopPrice;

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
	/// Initialize Liquidex strategy.
	/// </summary>
	public LiquidexStrategy()
	{
		_kcPeriod = Param(nameof(KcPeriod), 10)
			.SetDisplay("KC Period", "Keltner Channels period", "Parameters");
		_useKcFilter = Param(nameof(UseKcFilter), true)
			.SetDisplay("Use KC Filter", "Enable Keltner Channels breakout filter", "Parameters");
		_stopLoss = Param(nameof(StopLoss), 30m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 0m)
			.SetDisplay("Take Profit", "Take profit in price units, 0 disables", "Risk");
		_moveToBe = Param(nameof(MoveToBe), 15m)
			.SetDisplay("Move To BE", "Profit to move stop to break-even, 0 disables", "Risk");
		_moveToBeOffset = Param(nameof(MoveToBeOffset), 2m)
			.SetDisplay("BE Offset", "Offset when moving stop to break-even", "Risk");
		_trailingDistance = Param(nameof(TrailingDistance), 5m)
			.SetDisplay("Trailing", "Trailing stop distance, 0 disables", "Risk");
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(15)))
			.SetDisplay("Candle", "Candle type", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var keltner = new KeltnerChannels { Length = KcPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(keltner, ProcessCandle).Start();

		StartProtection();

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
		if (candle.State != CandleStates.Finished)
			return;

		var kc = (KeltnerChannelsValue)keltnerValue;
		if (kc.UpBand is not decimal upper || kc.LowBand is not decimal lower || kc.MovingAverage is not decimal middle)
			return;

		var price = candle.ClosePrice;

		if (Position == 0)
		{
			if (!UseKcFilter || price > upper)
			{
				BuyMarket();
				_entryPrice = price;
				_stopPrice = price - StopLoss;
			}
			else if (!UseKcFilter || price < lower)
			{
				SellMarket();
				_entryPrice = price;
				_stopPrice = price + StopLoss;
			}
		}
		else if (Position > 0)
		{
			if (TakeProfit > 0m && price >= _entryPrice + TakeProfit)
			{
				SellMarket(Position);
			}
			else if (price <= _stopPrice)
			{
				SellMarket(Position);
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
				BuyMarket(-Position);
			}
			else if (price >= _stopPrice)
			{
				BuyMarket(-Position);
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
