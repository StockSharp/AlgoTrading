using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy using displaced simple moving averages.
/// </summary>
public class BrandyV12Strategy : Strategy
{
	private readonly StrategyParam<int> _longPeriod;
	private readonly StrategyParam<int> _longShift;
	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<int> _shortShift;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _longSma;
	private SimpleMovingAverage _shortSma;
	private readonly List<decimal> _longHistory = new();
	private readonly List<decimal> _shortHistory = new();
	private decimal? _entryPrice;
	private decimal? _stopPrice;

	/// <summary>
	/// Initializes a new instance of <see cref="BrandyV12Strategy"/>.
	/// </summary>
	public BrandyV12Strategy()
	{
		_longPeriod = Param(nameof(LongPeriod), 70)
			.SetGreaterThanZero()
			.SetDisplay("Long SMA Period", "Period for the longer moving average.", "Indicators")
			.SetCanOptimize(true);

		_longShift = Param(nameof(LongShift), 5)
			.SetNotNegative()
			.SetDisplay("Long SMA Shift", "Backward shift applied to the longer SMA.", "Indicators")
			.SetCanOptimize(true);

		_shortPeriod = Param(nameof(ShortPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Short SMA Period", "Period for the shorter moving average.", "Indicators")
			.SetCanOptimize(true);

		_shortShift = Param(nameof(ShortShift), 5)
			.SetNotNegative()
			.SetDisplay("Short SMA Shift", "Backward shift applied to the shorter SMA.", "Indicators")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (points)", "Initial stop-loss distance expressed in price steps.", "Risk")
			.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 150m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop (points)", "Trailing stop distance in price steps. Activates when >= 100.", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle series processed by the strategy.", "General");
	}

	/// <summary>
	/// Period for the longer simple moving average.
	/// </summary>
	public int LongPeriod
	{
		get => _longPeriod.Value;
		set => _longPeriod.Value = value;
	}

	/// <summary>
	/// Backward shift used when evaluating the longer SMA.
	/// </summary>
	public int LongShift
	{
		get => _longShift.Value;
		set => _longShift.Value = value;
	}

	/// <summary>
	/// Period for the shorter simple moving average.
	/// </summary>
	public int ShortPeriod
	{
		get => _shortPeriod.Value;
		set => _shortPeriod.Value = value;
	}

	/// <summary>
	/// Backward shift used when evaluating the shorter SMA.
	/// </summary>
	public int ShortShift
	{
		get => _shortShift.Value;
		set => _shortShift.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in points (price steps).
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points (price steps).
	/// Trailing activates only when the configured value is at least 100.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_longSma = null;
		_shortSma = null;
		_longHistory.Clear();
		_shortHistory.Clear();
		_entryPrice = null;
		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_longSma = new SimpleMovingAverage { Length = LongPeriod };
		_shortSma = new SimpleMovingAverage { Length = ShortPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_longSma, _shortSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _longSma);
			DrawIndicator(area, _shortSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal longValue, decimal shortValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_longSma?.IsFormed != true || _shortSma?.IsFormed != true)
			return;

		var longCapacity = Math.Max(LongShift, 1) + 2;
		var shortCapacity = Math.Max(ShortShift, 1) + 2;
		UpdateHistory(_longHistory, longValue, longCapacity);
		UpdateHistory(_shortHistory, shortValue, shortCapacity);

		if (!TryGetShiftedValue(_longHistory, 1, out var longPrev) ||
			!TryGetShiftedValue(_longHistory, LongShift, out var longShifted) ||
			!TryGetShiftedValue(_shortHistory, 1, out var shortPrev) ||
			!TryGetShiftedValue(_shortHistory, ShortShift, out var shortShifted))
		{
			return;
		}

		if (ManageExistingPosition(candle, longPrev, longShifted))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0)
		{
			var bullish = longPrev > longShifted && shortPrev > shortShifted;
			var bearish = longPrev < longShifted && shortPrev < shortShifted;

			if (bullish)
			{
				EnterLong(candle);
			}
			else if (bearish)
			{
				EnterShort(candle);
			}
		}
	}

	private bool ManageExistingPosition(ICandleMessage candle, decimal longPrev, decimal longShifted)
	{
		if (Position > 0)
		{
			if (longPrev < longShifted)
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}

			if (UpdateLongStops(candle))
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}
		}
		else if (Position < 0)
		{
			if (longPrev > longShifted)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return true;
			}

			if (UpdateShortStops(candle))
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return true;
			}
		}

		return false;
	}

	private void EnterLong(ICandleMessage candle)
	{
		var volume = Volume;
		if (volume <= 0m)
			return;

		BuyMarket(volume);

		var step = GetPoint();
		var price = candle.ClosePrice;
		_entryPrice = price;

		_stopPrice = StopLossPoints > 0m ? price - StopLossPoints * step : null;
	}

	private void EnterShort(ICandleMessage candle)
	{
		var volume = Volume;
		if (volume <= 0m)
			return;

		SellMarket(volume);

		var step = GetPoint();
		var price = candle.ClosePrice;
		_entryPrice = price;

		_stopPrice = StopLossPoints > 0m ? price + StopLossPoints * step : null;
	}

	private bool UpdateLongStops(ICandleMessage candle)
	{
		if (_entryPrice is not decimal entry)
			return false;

		var step = GetPoint();
		if (step <= 0m)
			return false;

		if (_stopPrice is null && StopLossPoints > 0m)
		{
			_stopPrice = entry - StopLossPoints * step;
		}

		if (TrailingStopPoints >= 100m)
		{
			var trailingDistance = TrailingStopPoints * step;
			if (trailingDistance > 0m)
			{
				var currentPrice = candle.ClosePrice;
				if (currentPrice - entry > trailingDistance)
				{
					var newStop = currentPrice - trailingDistance;
					if (_stopPrice is not decimal existing || currentPrice - existing > trailingDistance)
					{
						_stopPrice = newStop;
					}
				}
			}
		}

		if (_stopPrice is not decimal stop)
			return false;

		return candle.LowPrice <= stop;
	}

	private bool UpdateShortStops(ICandleMessage candle)
	{
		if (_entryPrice is not decimal entry)
			return false;

		var step = GetPoint();
		if (step <= 0m)
			return false;

		if (_stopPrice is null && StopLossPoints > 0m)
		{
			_stopPrice = entry + StopLossPoints * step;
		}

		if (TrailingStopPoints >= 100m)
		{
			var trailingDistance = TrailingStopPoints * step;
			if (trailingDistance > 0m)
			{
				var currentPrice = candle.ClosePrice;
				if (entry - currentPrice > trailingDistance)
				{
					var newStop = currentPrice + trailingDistance;
					if (_stopPrice is not decimal existing || existing - currentPrice > trailingDistance)
					{
						_stopPrice = newStop;
					}
				}
			}
		}

		if (_stopPrice is not decimal stop)
			return false;

		return candle.HighPrice >= stop;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
	}

	private static void UpdateHistory(List<decimal> history, decimal value, int capacity)
	{
		history.Add(value);
		if (history.Count > capacity)
		{
			history.RemoveAt(0);
		}
	}

	private static bool TryGetShiftedValue(List<decimal> history, int shift, out decimal value)
	{
		value = 0m;

		if (shift < 0)
			return false;

		var index = history.Count - 1 - shift;
		if (index < 0 || index >= history.Count)
			return false;

		value = history[index];
		return true;
	}

	private decimal GetPoint()
	{
		var step = Security?.PriceStep;
		if (step is decimal priceStep && priceStep > 0m)
			return priceStep;

		return 0.0001m;
	}
}
