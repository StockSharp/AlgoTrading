using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Candle breakout strategy converted from the Alexav SpeedUp M1 expert advisor.
/// Enters in the direction of strong candle bodies and manages exits with optional stop-loss,
/// take-profit, and trailing stop logic.
/// </summary>
public class AlexavSpeedUpM1Strategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _minimumBodySizePips;
	private readonly StrategyParam<DataType> _candleType;

	private Sides? _currentDirection;
	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal? _trailingStopDistance;
	private decimal? _trailingStepDistance;

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step in pips.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Minimum candle body size required to open a trade, expressed in pips.
	/// </summary>
	public int MinimumBodySizePips
	{
		get => _minimumBodySizePips.Value;
		set => _minimumBodySizePips.Value = value;
	}

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AlexavSpeedUpM1Strategy"/>.
	/// </summary>
	public AlexavSpeedUpM1Strategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Position size in lots", "General");

		_stopLossPips = Param(nameof(StopLossPips), 30)
		.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(10, 100, 10);

		_takeProfitPips = Param(nameof(TakeProfitPips), 90)
		.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(30, 180, 30);

		_trailingStopPips = Param(nameof(TrailingStopPips), 10)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 5);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
		.SetDisplay("Trailing Step (pips)", "Price movement required to move the trailing stop", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 5);

		_minimumBodySizePips = Param(nameof(MinimumBodySizePips), 100)
		.SetDisplay("Minimum Body (pips)", "Minimum candle body size to trigger entries", "Signal")
		.SetCanOptimize(true)
		.SetOptimize(50, 200, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles for analysis", "General");
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
		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0 && TrailingStepPips == 0)
			throw new InvalidOperationException("Trailing step must be greater than zero when trailing stop is enabled.");

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_currentDirection != null && Position == 0)
			ResetPositionState();

		if (_currentDirection != null)
		{
			if (ManageActivePosition(candle))
				return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var pipSize = GetPipSize();
		var minimumBody = MinimumBodySizePips <= 0 ? 0m : MinimumBodySizePips * pipSize;
		var bodySize = Math.Abs(candle.ClosePrice - candle.OpenPrice);

		if (bodySize <= minimumBody)
			return;

		if (_currentDirection != null)
			return;

		var direction = candle.ClosePrice >= candle.OpenPrice ? Sides.Buy : Sides.Sell;
		OpenPosition(direction, candle.ClosePrice);
	}

	private bool ManageActivePosition(ICandleMessage candle)
	{
		if (_currentDirection == null)
			return false;

		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		if (_currentDirection == Sides.Buy)
		{
			if (_stopPrice is decimal stop && low <= stop)
			{
				ClosePosition();
				return true;
			}

			if (_takeProfitPrice is decimal take && high >= take)
			{
				ClosePosition();
				return true;
			}

			UpdateTrailingStopForLong(close);
		}
		else if (_currentDirection == Sides.Sell)
		{
			if (_stopPrice is decimal stop && high >= stop)
			{
				ClosePosition();
				return true;
			}

			if (_takeProfitPrice is decimal take && low <= take)
			{
				ClosePosition();
				return true;
			}

			UpdateTrailingStopForShort(close);
		}

		return false;
	}

	private void OpenPosition(Sides direction, decimal price)
	{
		if (OrderVolume <= 0)
			return;

		var desiredPosition = direction == Sides.Buy ? OrderVolume : -OrderVolume;
		var difference = desiredPosition - Position;

		if (difference > 0)
			BuyMarket(difference);
		else if (difference < 0)
			SellMarket(-difference);

		_currentDirection = direction;
		_entryPrice = price;

		var pipSize = GetPipSize();

		_stopPrice = StopLossPips > 0
			? direction == Sides.Buy
				? price - StopLossPips * pipSize
				: price + StopLossPips * pipSize
			: null;

		_takeProfitPrice = TakeProfitPips > 0
			? direction == Sides.Buy
				? price + TakeProfitPips * pipSize
				: price - TakeProfitPips * pipSize
			: null;

		if (TrailingStopPips > 0)
		{
			_trailingStopDistance = TrailingStopPips * pipSize;
			_trailingStepDistance = TrailingStepPips * pipSize;
		}
		else
		{
			_trailingStopDistance = null;
			_trailingStepDistance = null;
		}
	}

	private void ClosePosition()
	{
		var currentPosition = Position;

		if (currentPosition > 0)
			SellMarket(currentPosition);
		else if (currentPosition < 0)
			BuyMarket(-currentPosition);

		ResetPositionState();
	}

	private void UpdateTrailingStopForLong(decimal price)
	{
		if (_trailingStopDistance is not decimal trailing || _trailingStepDistance is not decimal step)
			return;

		if (price - _entryPrice < trailing + step)
			return;

		var candidate = price - trailing;

		if (_stopPrice is decimal stop && stop >= candidate - step)
			return;

		_stopPrice = candidate;
	}

	private void UpdateTrailingStopForShort(decimal price)
	{
		if (_trailingStopDistance is not decimal trailing || _trailingStepDistance is not decimal step)
			return;

		if (_entryPrice - price < trailing + step)
			return;

		var candidate = price + trailing;

		if (_stopPrice is decimal stop && stop <= candidate + step)
			return;

		_stopPrice = candidate;
	}

	private void ResetPositionState()
	{
		_currentDirection = null;
		_entryPrice = 0m;
		_stopPrice = null;
		_takeProfitPrice = null;
		_trailingStopDistance = null;
		_trailingStepDistance = null;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0.0001m;
		var decimals = Security?.Decimals ?? 5;

		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}
}
