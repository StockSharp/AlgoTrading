using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Time-based strategy that opens a single directional position at the configured time
/// and closes it at another time or when optional stop/target levels are hit.
/// </summary>
public class TimeEaStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _openTime;
	private readonly StrategyParam<TimeSpan> _closeTime;
	private readonly StrategyParam<TimeEaPositionType> _openedType;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _minSpreadMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _lastEntryDate;
	private DateTime? _lastCloseDate;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;

	/// <summary>
	/// Time of day to open the position.
	/// </summary>
	public TimeSpan OpenTime
	{
		get => _openTime.Value;
		set => _openTime.Value = value;
	}

	/// <summary>
	/// Time of day to close the position.
	/// </summary>
	public TimeSpan CloseTime
	{
		get => _closeTime.Value;
		set => _closeTime.Value = value;
	}

	/// <summary>
	/// Direction of the position opened at the scheduled time.
	/// </summary>
	public TimeEaPositionType OpenedType
	{
		get => _openedType.Value;
		set => _openedType.Value = value;
	}

	/// <summary>
	/// Market order volume for opening trades.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Minimal distance multiplier applied to stops and targets.
	/// </summary>
	public int MinSpreadMultiplier
	{
		get => _minSpreadMultiplier.Value;
		set => _minSpreadMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type used to evaluate time windows.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="TimeEaStrategy"/>.
	/// </summary>
	public TimeEaStrategy()
	{
		_openTime = Param(nameof(OpenTime), new TimeSpan(1, 0, 0))
			.SetDisplay("Open Time", "Time to enter the market", "Scheduling");

		_closeTime = Param(nameof(CloseTime), TimeSpan.Zero)
			.SetDisplay("Close Time", "Time to exit the market", "Scheduling");

		_openedType = Param(nameof(OpenedType), TimeEaPositionType.Buy)
			.SetDisplay("Position Type", "Direction to maintain", "Trading");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Quantity for market orders", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 0)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (points)", "Distance in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (points)", "Distance in price steps", "Risk");

		_minSpreadMultiplier = Param(nameof(MinSpreadMultiplier), 2)
			.SetGreaterOrEqualZero()
			.SetDisplay("Minimum Distance Multiplier", "Minimal offset applied to stops", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for scheduling", "General");
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

		_lastEntryDate = null;
		_lastCloseDate = null;
		ResetRiskLevels();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Use finished candles to evaluate the time windows.
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var candleDate = candle.CloseTime.Date;

		if (ContainsTime(candle, OpenTime) && _lastEntryDate != candleDate)
		{
			_lastEntryDate = candleDate;
			HandleOpen(candle);
		}

		if (ContainsTime(candle, CloseTime) && _lastCloseDate != candleDate)
		{
			_lastCloseDate = candleDate;

			if (Position != 0)
			{
				ClosePosition();
				ResetRiskLevels();
			}
			return;
		}

		ManageRisk(candle);
	}

	private void HandleOpen(ICandleMessage candle)
	{
		// Close opposite exposure before opening a new position.
		if (OpenedType == TimeEaPositionType.Buy)
		{
			if (Position < 0)
			{
				ClosePosition();
				ResetRiskLevels();
			}

			if (Position == 0 && OrderVolume > 0)
			{
				BuyMarket(OrderVolume);
				SetRiskLevels(candle.ClosePrice, true);
			}
		}
		else
		{
			if (Position > 0)
			{
				ClosePosition();
				ResetRiskLevels();
			}

			if (Position == 0 && OrderVolume > 0)
			{
				SellMarket(OrderVolume);
				SetRiskLevels(candle.ClosePrice, false);
			}
		}
	}

	private void ManageRisk(ICandleMessage candle)
	{
		// Monitor active position for stop loss and take profit.
		if (Position > 0)
		{
			if (_stopPrice > 0m && candle.LowPrice <= _stopPrice)
			{
				ClosePosition();
				ResetRiskLevels();
				return;
			}

			if (_takeProfitPrice > 0m && candle.HighPrice >= _takeProfitPrice)
			{
				ClosePosition();
				ResetRiskLevels();
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice > 0m && candle.HighPrice >= _stopPrice)
			{
				ClosePosition();
				ResetRiskLevels();
				return;
			}

			if (_takeProfitPrice > 0m && candle.LowPrice <= _takeProfitPrice)
			{
				ClosePosition();
				ResetRiskLevels();
			}
		}
	}

	private void SetRiskLevels(decimal closePrice, bool isLong)
	{
		_entryPrice = closePrice;

		var step = Security?.PriceStep ?? 1m;
		var minDistance = Math.Max(MinSpreadMultiplier, 0) * step;
		var stopDistance = StopLossPoints > 0 ? Math.Max(StopLossPoints * step, minDistance) : 0m;
		var takeDistance = TakeProfitPoints > 0 ? Math.Max(TakeProfitPoints * step, minDistance) : 0m;

		// Calculate price levels in the same direction logic as the original Expert Advisor.
		if (isLong)
		{
			_stopPrice = stopDistance > 0m ? closePrice - stopDistance : 0m;
			_takeProfitPrice = takeDistance > 0m ? closePrice + takeDistance : 0m;
		}
		else
		{
			_stopPrice = stopDistance > 0m ? closePrice + stopDistance : 0m;
			_takeProfitPrice = takeDistance > 0m ? closePrice - takeDistance : 0m;
		}
	}

	private void ResetRiskLevels()
	{
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
	}

	private static bool ContainsTime(ICandleMessage candle, TimeSpan target)
	{
		var openTime = candle.OpenTime;
		var closeTime = candle.CloseTime;

		var openSpan = openTime.TimeOfDay;
		var closeSpan = closeTime.TimeOfDay;

		var crossesMidnight = closeTime.Date > openTime.Date || closeSpan < openSpan;

		if (!crossesMidnight)
			return target >= openSpan && target <= closeSpan;

		var startMinutes = openSpan.TotalMinutes;
		var endMinutes = closeSpan.TotalMinutes + TimeSpan.FromDays(1).TotalMinutes;
		var targetMinutes = target.TotalMinutes;

		if (targetMinutes < startMinutes)
			targetMinutes += TimeSpan.FromDays(1).TotalMinutes;

		return targetMinutes >= startMinutes && targetMinutes <= endMinutes;
	}

	/// <summary>
	/// Supported position directions.
	/// </summary>
	public enum TimeEaPositionType
	{
		/// <summary>
		/// Open a long position.
		/// </summary>
		Buy,

		/// <summary>
		/// Open a short position.
		/// </summary>
		Sell
	}
}
