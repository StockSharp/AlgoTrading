namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Strategy combining Aroon crossovers with Williams %R filters.
/// Buys when Aroon Up crosses above Aroon Down with oversold Williams %R.
/// Sells when the opposite crossover happens with overbought Williams %R.
/// Closes positions by Williams %R, optional stop-loss and take-profit.
/// </summary>
public class AroonWprCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _aroonPeriod;
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<int> _openWprLevel;
	private readonly StrategyParam<int> _closeWprLevel;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousAroonUp;
	private decimal? _previousAroonDown;
	private decimal? _entryPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="AroonWprCrossoverStrategy"/> class.
	/// </summary>
	public AroonWprCrossoverStrategy()
	{
		_aroonPeriod = Param(nameof(AroonPeriod), 14)
		.SetDisplay("Aroon Period", "Aroon indicator length", "Indicator");

		_wprPeriod = Param(nameof(WprPeriod), 35)
		.SetDisplay("WPR Period", "Williams %R length", "Indicator");

		_openWprLevel = Param(nameof(OpenWprLevel), 20)
		.SetDisplay("Open WPR", "Williams %R level for entries", "Signals");

		_closeWprLevel = Param(nameof(CloseWprLevel), 10)
		.SetDisplay("Close WPR", "Williams %R level for exits", "Signals");

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 0m)
		.SetDisplay("Take Profit", "Take profit in price steps", "Risk");

		_stopLossSteps = Param(nameof(StopLossSteps), 0m)
		.SetDisplay("Stop Loss", "Stop loss in price steps", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle type", "General");
	}

	/// <summary>
	/// Aroon indicator period.
	/// </summary>
	public int AroonPeriod
	{
		get => _aroonPeriod.Value;
		set => _aroonPeriod.Value = value;
	}

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int WprPeriod
	{
		get => _wprPeriod.Value;
		set => _wprPeriod.Value = value;
	}

	/// <summary>
	/// Williams %R level that confirms entries.
	/// </summary>
	public int OpenWprLevel
	{
		get => _openWprLevel.Value;
		set => _openWprLevel.Value = value;
	}

	/// <summary>
	/// Williams %R level that triggers exits.
	/// </summary>
	public int CloseWprLevel
	{
		get => _closeWprLevel.Value;
		set => _closeWprLevel.Value = value;
	}

	/// <summary>
	/// Take profit measured in price steps.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Stop loss measured in price steps.
	/// </summary>
	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Candle type for the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousAroonUp = null;
		_previousAroonDown = null;
		_entryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var aroon = new Aroon
		{
			Length = AroonPeriod
		};

		var wpr = new WilliamsPercentRange
		{
			Length = WprPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(aroon, wpr, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue aroonValue, IIndicatorValue wprValue)
	{
		// Process only finished candles to mirror the original EA behavior.
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!aroonValue.IsFinal || !wprValue.IsFinal)
		return;

		var aroon = (AroonValue)aroonValue;
		var currentUp = aroon.Up;
		var currentDown = aroon.Down;

		var wpr = wprValue.ToDecimal();

		if (_entryPrice.HasValue && Position == 0)
		{
			// Reset the cached entry when the position was closed externally.
			_entryPrice = null;
		}

		if (Position > 0)
		{
			// Exit long when Williams %R exits the oversold zone.
			if (wpr >= -CloseWprLevel)
			{
				ClosePosition();
				_entryPrice = null;
			}
			else
			{
				var entry = _entryPrice ?? candle.ClosePrice;
				ApplyStopsForLong(candle, entry);
			}
		}
		else if (Position < 0)
		{
			// Exit short when Williams %R exits the overbought zone.
			if (wpr <= -(100m - CloseWprLevel))
			{
				ClosePosition();
				_entryPrice = null;
			}
			else
			{
				var entry = _entryPrice ?? candle.ClosePrice;
				ApplyStopsForShort(candle, entry);
			}
		}
		else
		{
			// Check if enough Aroon history is available.
			if (_previousAroonUp is null || _previousAroonDown is null)
			{
				_previousAroonUp = currentUp;
				_previousAroonDown = currentDown;
				return;
			}

			var crossUp = _previousAroonUp <= _previousAroonDown && currentUp > currentDown;
			var crossDown = _previousAroonUp >= _previousAroonDown && currentUp < currentDown;

			var longThreshold = -(100m - OpenWprLevel);
			var shortThreshold = -OpenWprLevel;

			if (crossUp && wpr < longThreshold)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
			else if (crossDown && wpr > shortThreshold)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}

		_previousAroonUp = currentUp;
		_previousAroonDown = currentDown;
	}

	private void ApplyStopsForLong(ICandleMessage candle, decimal entry)
	{
		var step = Security?.PriceStep ?? 1m;
		var take = TakeProfitSteps * step;
		var stop = StopLossSteps * step;

		// Close the position when the optional take-profit is reached.
		if (TakeProfitSteps > 0m && candle.HighPrice >= entry + take)
		{
			ClosePosition();
			_entryPrice = null;
			return;
		}

		// Close the position when the optional stop-loss is hit.
		if (StopLossSteps > 0m && candle.LowPrice <= entry - stop)
		{
			ClosePosition();
			_entryPrice = null;
		}
	}

	private void ApplyStopsForShort(ICandleMessage candle, decimal entry)
	{
		var step = Security?.PriceStep ?? 1m;
		var take = TakeProfitSteps * step;
		var stop = StopLossSteps * step;

		// Close the position when the optional take-profit is reached.
		if (TakeProfitSteps > 0m && candle.LowPrice <= entry - take)
		{
			ClosePosition();
			_entryPrice = null;
			return;
		}

		// Close the position when the optional stop-loss is hit.
		if (StopLossSteps > 0m && candle.HighPrice >= entry + stop)
		{
			ClosePosition();
			_entryPrice = null;
		}
	}
}
