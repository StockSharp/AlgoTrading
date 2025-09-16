using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trailing manager strategy converted from the MetaTrader "e-Smart_Tralling" expert.
/// </summary>
public class ESmartTrallingStrategy : Strategy
{
	private const decimal DefaultPartialStep = 0.1m;

	private readonly StrategyParam<bool> _useCloseOneThird;
	private readonly StrategyParam<decimal> _levelProfit1;
	private readonly StrategyParam<decimal> _levelProfit2;
	private readonly StrategyParam<decimal> _levelProfit3;
	private readonly StrategyParam<decimal> _levelMoving1;
	private readonly StrategyParam<decimal> _levelMoving2;
	private readonly StrategyParam<decimal> _levelMoving3;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _priceStep;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private bool _closedOneThird;
	private int _positionSign;

	/// <summary>
	/// Initializes a new instance of the <see cref="ESmartTrallingStrategy"/> class.
	/// </summary>
	public ESmartTrallingStrategy()
	{
		_useCloseOneThird = Param(nameof(UseCloseOneThird), true)
		.SetDisplay("Close One Third", "Close one third of the position after the first profit level", "Risk")
		.SetCanOptimize(true);

		_levelProfit1 = Param(nameof(LevelProfit1), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Profit Level 1 (points)", "Profit in points required before activating the first trailing level", "Trailing")
		.SetCanOptimize(true);

		_levelProfit2 = Param(nameof(LevelProfit2), 35m)
		.SetGreaterThanZero()
		.SetDisplay("Profit Level 2 (points)", "Profit in points required before activating the second trailing level", "Trailing")
		.SetCanOptimize(true);

		_levelProfit3 = Param(nameof(LevelProfit3), 55m)
		.SetGreaterThanZero()
		.SetDisplay("Profit Level 3 (points)", "Profit in points required before activating the third trailing level", "Trailing")
		.SetCanOptimize(true);

		_levelMoving1 = Param(nameof(LevelMoving1), 1m)
		.SetDisplay("Stop Offset 1 (points)", "Stop-loss distance applied after reaching the first profit level", "Trailing")
		.SetCanOptimize(true);

		_levelMoving2 = Param(nameof(LevelMoving2), 10m)
		.SetDisplay("Stop Offset 2 (points)", "Stop-loss distance applied after reaching the second profit level", "Trailing")
		.SetCanOptimize(true);

		_levelMoving3 = Param(nameof(LevelMoving3), 30m)
		.SetDisplay("Stop Offset 3 (points)", "Stop-loss distance applied after reaching the third profit level", "Trailing")
		.SetCanOptimize(true);

		_trailingStop = Param(nameof(TrailingStop), 30m)
		.SetDisplay("Trailing Stop (points)", "Distance in points kept between price and the trailing stop", "Trailing")
		.SetCanOptimize(true);

		_trailingStep = Param(nameof(TrailingStep), 5m)
		.SetDisplay("Trailing Step (points)", "Additional favorable movement required before moving the trailing stop again", "Trailing")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Candle type providing price updates for the trailing logic", "General");
	}

	/// <summary>
	/// Whether to close one third of the position when the first profit level is reached.
	/// </summary>
	public bool UseCloseOneThird
	{
		get => _useCloseOneThird.Value;
		set => _useCloseOneThird.Value = value;
	}

	/// <summary>
	/// Profit in points required before activating the first trailing level.
	/// </summary>
	public decimal LevelProfit1
	{
		get => _levelProfit1.Value;
		set => _levelProfit1.Value = value;
	}

	/// <summary>
	/// Profit in points required before activating the second trailing level.
	/// </summary>
	public decimal LevelProfit2
	{
		get => _levelProfit2.Value;
		set => _levelProfit2.Value = value;
	}

	/// <summary>
	/// Profit in points required before activating the third trailing level.
	/// </summary>
	public decimal LevelProfit3
	{
		get => _levelProfit3.Value;
		set => _levelProfit3.Value = value;
	}

	/// <summary>
	/// Stop-loss distance applied after reaching the first profit level.
	/// </summary>
	public decimal LevelMoving1
	{
		get => _levelMoving1.Value;
		set => _levelMoving1.Value = value;
	}

	/// <summary>
	/// Stop-loss distance applied after reaching the second profit level.
	/// </summary>
	public decimal LevelMoving2
	{
		get => _levelMoving2.Value;
		set => _levelMoving2.Value = value;
	}

	/// <summary>
	/// Stop-loss distance applied after reaching the third profit level.
	/// </summary>
	public decimal LevelMoving3
	{
		get => _levelMoving3.Value;
		set => _levelMoving3.Value = value;
	}

	/// <summary>
	/// Distance in points kept between price and the trailing stop.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Additional favorable movement required before moving the trailing stop again.
	/// </summary>
	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	/// <summary>
	/// Candle type providing price updates for the trailing logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_priceStep = 0m;
		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? Security?.Step ?? 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		var sign = Math.Sign(Position);
		if (sign == 0)
		{
			ResetPositionState();
			return;
		}

		if (sign != _positionSign)
		{
			// Reset trailing information when the direction changes.
			_stopPrice = null;
			_closedOneThird = false;
		}

		if (PositionPrice != 0m)
		{
			// Track the average entry price reported by the portfolio.
			_entryPrice = PositionPrice;
		}

		_positionSign = sign;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_priceStep <= 0m || Position == 0m || _entryPrice is null)
			return;

		// Apply the trailing logic based on the current direction.
		if (Position > 0m)
		{
			ManageLongPosition(candle);
		}
		else if (Position < 0m)
		{
			ManageShortPosition(candle);
		}
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		var entry = _entryPrice!.Value;
		var maxPrice = Math.Max(candle.HighPrice, candle.ClosePrice);
		var minPrice = Math.Min(candle.LowPrice, candle.ClosePrice);

		if (_stopPrice is decimal stop && minPrice <= stop)
		{
			// Close the long position once the trailing stop is touched.
			SellMarket(Position);
			return;
		}

		var profitPoints = (maxPrice - entry) / _priceStep;
		var stopOffsetPoints = _stopPrice is decimal currentStop
		? (currentStop - entry) / _priceStep
		: decimal.MinValue;

		if (profitPoints > LevelProfit1 && profitPoints <= LevelProfit2 && stopOffsetPoints < LevelMoving1)
		{
			UpdateLongStop(entry + LevelMoving1 * _priceStep);

			if (UseCloseOneThird && !_closedOneThird)
				CloseOneThirdLong();
		}

		if (profitPoints > LevelProfit2 && profitPoints <= LevelProfit3 && stopOffsetPoints < LevelMoving2)
		{
			UpdateLongStop(entry + LevelMoving2 * _priceStep);
		}

		if (profitPoints > LevelProfit3 && stopOffsetPoints < LevelMoving3)
		{
			UpdateLongStop(entry + LevelMoving3 * _priceStep);
		}

		var trailingThreshold = LevelMoving3 + TrailingStop + TrailingStep;
		if (TrailingStop > 0m && profitPoints > trailingThreshold)
		{
			ApplyLongTrailing(maxPrice);
		}
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		var entry = _entryPrice!.Value;
		var minPrice = Math.Min(candle.LowPrice, candle.ClosePrice);
		var maxPrice = Math.Max(candle.HighPrice, candle.ClosePrice);

		if (_stopPrice is decimal stop && maxPrice >= stop)
		{
			// Close the short position once the trailing stop is touched.
			BuyMarket(-Position);
			return;
		}

		var profitPoints = (entry - minPrice) / _priceStep;
		var stopOffsetPoints = _stopPrice is decimal currentStop
		? (entry - currentStop) / _priceStep
		: decimal.MinValue;

		if (profitPoints > LevelProfit1 && profitPoints <= LevelProfit2 && stopOffsetPoints < LevelMoving1)
		{
			UpdateShortStop(entry - LevelMoving1 * _priceStep);

			if (UseCloseOneThird && !_closedOneThird)
				CloseOneThirdShort();
		}

		if (profitPoints > LevelProfit2 && profitPoints <= LevelProfit3 && stopOffsetPoints < LevelMoving2)
		{
			UpdateShortStop(entry - LevelMoving2 * _priceStep);
		}

		if (profitPoints > LevelProfit3 && stopOffsetPoints < LevelMoving3)
		{
			UpdateShortStop(entry - LevelMoving3 * _priceStep);
		}

		var trailingThreshold = LevelMoving3 + TrailingStop + TrailingStep;
		if (TrailingStop > 0m && profitPoints > trailingThreshold)
		{
			ApplyShortTrailing(minPrice);
		}
	}

	private void ApplyLongTrailing(decimal referencePrice)
	{
		var trailingDistance = TrailingStop * _priceStep;
		if (trailingDistance <= 0m)
			return;

		var candidateStop = referencePrice - trailingDistance;
		var minIncrement = Math.Max(0m, (TrailingStep - 1m) * _priceStep);

		if (_stopPrice is decimal currentStop)
		{
			// Move the stop only forward and after a sufficient additional movement.
			if (candidateStop <= currentStop || candidateStop - currentStop <= minIncrement)
				return;
		}

		UpdateLongStop(candidateStop);
	}

	private void ApplyShortTrailing(decimal referencePrice)
	{
		var trailingDistance = TrailingStop * _priceStep;
		if (trailingDistance <= 0m)
			return;

		var candidateStop = referencePrice + trailingDistance;
		var minIncrement = Math.Max(0m, (TrailingStep - 1m) * _priceStep);

		if (_stopPrice is decimal currentStop)
		{
			// Move the stop only closer to price after a sufficient additional movement.
			if (candidateStop >= currentStop || currentStop - candidateStop <= minIncrement)
				return;
		}

		UpdateShortStop(candidateStop);
	}

	private void UpdateLongStop(decimal newStop)
	{
		if (_stopPrice is decimal current && newStop <= current)
			return;

		// Store the price that would be used by a protective stop order.
		_stopPrice = newStop;
	}

	private void UpdateShortStop(decimal newStop)
	{
		if (_stopPrice is decimal current && newStop >= current)
			return;

		// Store the price that would be used by a protective stop order.
		_stopPrice = newStop;
	}

	private void CloseOneThirdLong()
	{
		var volume = CalculateOneThirdVolume();
		if (volume <= 0m)
			return;

		var orderVolume = Math.Min(volume, Position);
		if (orderVolume <= 0m)
			return;

		// Close part of the long position to lock partial profits.
		SellMarket(orderVolume);
		_closedOneThird = true;
	}

	private void CloseOneThirdShort()
	{
		var volume = CalculateOneThirdVolume();
		if (volume <= 0m)
			return;

		var orderVolume = Math.Min(volume, -Position);
		if (orderVolume <= 0m)
			return;

		// Close part of the short position to lock partial profits.
		BuyMarket(orderVolume);
		_closedOneThird = true;
	}

	private decimal CalculateOneThirdVolume()
	{
		var absPosition = Math.Abs(Position);
		if (absPosition <= 0m)
			return 0m;

		var step = Security?.VolumeStep ?? DefaultPartialStep;
		if (step <= 0m)
			step = DefaultPartialStep;

		// MetaTrader version rounds the volume up to the nearest step (default 0.1 lots).
		var ratio = absPosition / 3m;
		var stepsCount = Math.Ceiling(ratio / step);
		var volume = stepsCount * step;

		return Math.Min(volume, absPosition);
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_closedOneThird = false;
		_positionSign = 0;
	}
}
