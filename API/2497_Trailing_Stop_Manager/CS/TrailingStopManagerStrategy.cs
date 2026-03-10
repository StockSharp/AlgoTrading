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
/// Trailing stop manager that mirrors the pip-based trailing logic from the original MQL expert.
/// </summary>
public class TrailingStopManagerStrategy : Strategy
{
	/// <summary>
	/// Direction of the optional market order placed when the strategy starts.
	/// </summary>
	public enum InitialDirections
	{
		None,
		Long,
		Short
	}

	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<InitialDirections> _startDirection;

	private decimal _entryPrice;
	private decimal _trailingStopPrice;
	private bool _trailingActive;
	private InitialDirections _currentDirection = InitialDirections.None;
	private decimal _priceStep = 1m;

	/// <summary>
	/// Trailing stop activation distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum pip distance that must be covered before the trailing stop is adjusted again.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Optional market order direction executed on start to quickly demonstrate trailing behaviour.
	/// </summary>
	public InitialDirections StartDirection
	{
		get => _startDirection.Value;
		set => _startDirection.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TrailingStopManagerStrategy"/> class.
	/// </summary>
	public TrailingStopManagerStrategy()
	{
		_trailingStopPips = Param(nameof(TrailingStopPips), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop (pips)", "Distance to activate trailing", "Risk Management")
			;

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Minimal move before adjusting stop", "Risk Management")
			;

		_startDirection = Param(nameof(StartDirection), InitialDirections.None)
			.SetDisplay("Initial Direction", "Optional market order on start", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "Data");
	}

	private readonly StrategyParam<DataType> _candleType;

	/// <summary>Candle type.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	private readonly List<decimal> _closes = new();
	private const int FastLen = 10;
	private const int SlowLen = 30;
	private decimal? _prevFast;
	private decimal? _prevSlow;

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Retrieve price step to convert pip values into price offsets.
		_priceStep = Security?.PriceStep ?? 1m;
		if (_priceStep <= 0m)
			_priceStep = 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryPrice = 0m;
		_trailingStopPrice = 0m;
		_trailingActive = false;
		_currentDirection = InitialDirections.None;
		_priceStep = 1m;
		_closes.Clear();
		_prevFast = null;
		_prevSlow = null;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Trade == null)
			return;

		var tradePrice = trade.Trade.Price;

		// Reset trailing state whenever a new position is opened.
		if (Position > 0 && trade.Order.Side == Sides.Buy)
		{
			_entryPrice = tradePrice;
			_trailingActive = false;
			_trailingStopPrice = 0m;
			_currentDirection = InitialDirections.Long;
		}
		else if (Position < 0 && trade.Order.Side == Sides.Sell)
		{
			_entryPrice = tradePrice;
			_trailingActive = false;
			_trailingStopPrice = 0m;
			_currentDirection = InitialDirections.Short;
		}
		else if (Position == 0)
		{
			ResetTrailing();
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0)
			ResetTrailing();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closes.Add(candle.ClosePrice);
		if (_closes.Count > SlowLen + 10) _closes.RemoveAt(0);

		if (_closes.Count < SlowLen)
			return;

		var fast = _closes.Skip(_closes.Count - FastLen).Take(FastLen).Average();
		var slow = _closes.Skip(_closes.Count - SlowLen).Take(SlowLen).Average();

		var prevFast = _prevFast;
		var prevSlow = _prevSlow;
		_prevFast = fast;
		_prevSlow = slow;

		// Entry logic: use SMA crossover when flat
		if (Position == 0)
		{
			if (prevFast is decimal lastFast && prevSlow is decimal lastSlow && lastFast <= lastSlow && fast > slow)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_trailingActive = false;
				_trailingStopPrice = 0m;
				_currentDirection = InitialDirections.Long;
			}
			else if (prevFast is decimal lastFast2 && prevSlow is decimal lastSlow2 && lastFast2 >= lastSlow2 && fast < slow)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_trailingActive = false;
				_trailingStopPrice = 0m;
				_currentDirection = InitialDirections.Short;
			}
			return;
		}

		var price = candle.ClosePrice;

		if (Position > 0 && _currentDirection == InitialDirections.Long)
		{
			UpdateLongTrailing(price);
		}
		else if (Position < 0 && _currentDirection == InitialDirections.Short)
		{
			UpdateShortTrailing(price);
		}
	}

	private void UpdateLongTrailing(decimal price)
	{
		var stopDistance = TrailingStopPips * _priceStep;
		if (stopDistance <= 0m)
			return;

		var stepDistance = TrailingStepPips * _priceStep;

		// Activate the trailing stop once price has moved far enough above the entry.
		if (!_trailingActive)
		{
			if (price - _entryPrice >= stopDistance)
			{
				_trailingActive = true;
				_trailingStopPrice = _entryPrice;
			}
		}
		else
		{
			var desiredStop = price - stopDistance;

			// Only move the stop forward when the configured step distance is met.
			if (stepDistance <= 0m)
			{
				if (desiredStop > _trailingStopPrice)
					_trailingStopPrice = desiredStop;
			}
			else if (desiredStop - _trailingStopPrice >= stepDistance)
			{
				_trailingStopPrice = desiredStop;
			}
		}

		// Exit once price drops to the trailing stop level.
		if (_trailingActive && price <= _trailingStopPrice)
			ExitLong();
	}

	private void UpdateShortTrailing(decimal price)
	{
		var stopDistance = TrailingStopPips * _priceStep;
		if (stopDistance <= 0m)
			return;

		var stepDistance = TrailingStepPips * _priceStep;

		// Activate the trailing stop once price has moved far enough below the entry.
		if (!_trailingActive)
		{
			if (_entryPrice - price >= stopDistance)
			{
				_trailingActive = true;
				_trailingStopPrice = _entryPrice;
			}
		}
		else
		{
			var desiredStop = price + stopDistance;

			// Only move the stop forward when the configured step distance is met.
			if (stepDistance <= 0m)
			{
				if (desiredStop < _trailingStopPrice || _trailingStopPrice == 0m)
					_trailingStopPrice = desiredStop;
			}
			else if (_trailingStopPrice - desiredStop >= stepDistance)
			{
				_trailingStopPrice = desiredStop;
			}
		}

		// Exit once price rises to the trailing stop level.
		if (_trailingActive && price >= _trailingStopPrice)
			ExitShort();
	}

	private void ExitLong()
	{
		if (Position <= 0)
			return;

		SellMarket();
	}

	private void ExitShort()
	{
		if (Position >= 0)
			return;

		BuyMarket();
	}

	private void ResetTrailing()
	{
		_entryPrice = 0m;
		_trailingStopPrice = 0m;
		_trailingActive = false;
		_currentDirection = InitialDirections.None;
	}
}
