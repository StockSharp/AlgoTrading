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
/// Strategy that mirrors the MetaTrader "Trailing Sl" expert by managing trailing stops for existing positions.
/// Adds SMA crossover entries for backtesting.
/// </summary>
public class TrailingStopTriggerManagerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _trailingPoints;
	private readonly StrategyParam<int> _triggerPoints;

	private decimal _lastEntryPrice;
	private decimal? _activeStopPrice;
	private bool _trailingEnabled;
	private decimal _trailingDistance;
	private decimal _triggerDistance;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public int TrailingPoints
	{
		get => _trailingPoints.Value;
		set => _trailingPoints.Value = value;
	}

	/// <summary>
	/// Profit distance that activates trailing stop.
	/// </summary>
	public int TriggerPoints
	{
		get => _triggerPoints.Value;
		set => _triggerPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public TrailingStopTriggerManagerStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_trailingPoints = Param(nameof(TrailingPoints), 1000)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Points", "Trailing stop distance", "Trailing Management");

		_triggerPoints = Param(nameof(TriggerPoints), 1500)
			.SetGreaterThanZero()
			.SetDisplay("Trigger Points", "Profit to activate trailing", "Trailing Management");
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
		_lastEntryPrice = 0m;
		_activeStopPrice = null;
		_trailingEnabled = false;
		_trailingDistance = 0m;
		_triggerDistance = 0m;
		_prevFast = 0m;
		_prevSlow = 0m;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m) step = 1m;
		_trailingDistance = step * TrailingPoints;
		_triggerDistance = step * TriggerPoints;

		var smaFast = new SimpleMovingAverage { Length = 10 };
		var smaSlow = new SimpleMovingAverage { Length = 30 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(smaFast, smaSlow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);
		_lastEntryPrice = trade.Trade.Price;
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		// Trailing stop management
		if (Position > 0 && _lastEntryPrice > 0)
		{
			var profit = price - _lastEntryPrice;
			if (!_trailingEnabled && profit >= _triggerDistance)
			{
				_trailingEnabled = true;
				_activeStopPrice = price - _trailingDistance;
			}
			else if (_trailingEnabled)
			{
				var desiredStop = price - _trailingDistance;
				if (!_activeStopPrice.HasValue || desiredStop > _activeStopPrice.Value)
					_activeStopPrice = desiredStop;
			}

			if (_trailingEnabled && _activeStopPrice.HasValue && price <= _activeStopPrice.Value)
			{
				SellMarket();
				ResetTrailingState();
				return;
			}
		}
		else if (Position < 0 && _lastEntryPrice > 0)
		{
			var profit = _lastEntryPrice - price;
			if (!_trailingEnabled && profit >= _triggerDistance)
			{
				_trailingEnabled = true;
				_activeStopPrice = price + _trailingDistance;
			}
			else if (_trailingEnabled)
			{
				var desiredStop = price + _trailingDistance;
				if (!_activeStopPrice.HasValue || desiredStop < _activeStopPrice.Value)
					_activeStopPrice = desiredStop;
			}

			if (_trailingEnabled && _activeStopPrice.HasValue && price >= _activeStopPrice.Value)
			{
				BuyMarket();
				ResetTrailingState();
				return;
			}
		}

		// SMA crossover entries
		if (_hasPrev)
		{
			var crossUp = _prevFast <= _prevSlow && fast > slow;
			var crossDown = _prevFast >= _prevSlow && fast < slow;

			if (crossUp && Position <= 0)
			{
				if (Position < 0)
					BuyMarket();
				BuyMarket();
				ResetTrailingState();
			}
			else if (crossDown && Position >= 0)
			{
				if (Position > 0)
					SellMarket();
				SellMarket();
				ResetTrailingState();
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
		_hasPrev = true;
	}

	private void ResetTrailingState()
	{
		_trailingEnabled = false;
		_activeStopPrice = null;
	}
}
