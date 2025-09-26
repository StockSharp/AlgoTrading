// EFridayStrategy.cs
// -----------------------------------------------------------------------------
// Port of the MetaTrader "e-Friday" expert advisor.
// Trades on Fridays based on the direction of the previous daily candle.
// Automatically exits by the configured hour and supports optional trailing.
// -----------------------------------------------------------------------------
// Date: 2 Aug 2025
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Friday breakout strategy that trades in the opposite direction of the previous daily candle.
/// </summary>
public class EFridayStrategy : Strategy
{
	#region Params
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _hourOpen;
	private readonly StrategyParam<bool> _useClosePositions;
	private readonly StrategyParam<int> _hourClose;
	private readonly StrategyParam<bool> _useTrailing;
	private readonly StrategyParam<bool> _profitTrailing;
	private readonly StrategyParam<int> _trailingStopPoints;
	private readonly StrategyParam<int> _trailingStepPoints;
	private readonly StrategyParam<DataType> _intradayCandleType;
	private readonly StrategyParam<DataType> _dailyCandleType;


	/// <summary>
	/// Stop-loss distance in price steps. Set to zero to disable.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price steps. Set to zero to disable.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Hour (platform time) when the position should be opened.
	/// </summary>
	public int HourOpen
	{
		get => _hourOpen.Value;
		set => _hourOpen.Value = value;
	}

	/// <summary>
	/// Enable automatic closing of the position after the configured hour.
	/// </summary>
	public bool UseClosePositions
	{
		get => _useClosePositions.Value;
		set => _useClosePositions.Value = value;
	}

	/// <summary>
	/// Hour (platform time) after which any open position is closed.
	/// </summary>
	public int HourClose
	{
		get => _hourClose.Value;
		set => _hourClose.Value = value;
	}

	/// <summary>
	/// Enable trailing stop management.
	/// </summary>
	public bool UseTrailing
	{
		get => _useTrailing.Value;
		set => _useTrailing.Value = value;
	}

	/// <summary>
	/// Only activate trailing once the profit exceeds the trailing distance.
	/// </summary>
	public bool ProfitTrailing
	{
		get => _profitTrailing.Value;
		set => _profitTrailing.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public int TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Additional price steps required before tightening the trailing stop.
	/// </summary>
	public int TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for intraday timing.
	/// </summary>
	public DataType IntradayCandleType
	{
		get => _intradayCandleType.Value;
		set => _intradayCandleType.Value = value;
	}

	/// <summary>
	/// Candle type used for daily sentiment detection.
	/// </summary>
	public DataType DailyCandleType
	{
		get => _dailyCandleType.Value;
		set => _dailyCandleType.Value = value;
	}
	#endregion

	private DateTime _lastIntradayDate;
	private bool _hasTradedToday;
	private decimal? _previousDailyOpen;
	private decimal? _previousDailyClose;
	private DateTime _previousDailyDate;
	private decimal? _entryPrice;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="EFridayStrategy"/> class.
	/// </summary>
	public EFridayStrategy()
	{

		_stopLossPoints = Param(nameof(StopLossPoints), 75)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Stop loss distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0)
		.SetNotNegative()
		.SetDisplay("Take Profit", "Take profit distance in points", "Risk");

		_hourOpen = Param(nameof(HourOpen), 7)
		.SetRange(0, 23)
		.SetDisplay("Hour Open", "Hour to open the position", "Timing");

		_useClosePositions = Param(nameof(UseClosePositions), true)
		.SetDisplay("Close Positions", "Close positions after the close hour", "Timing");

		_hourClose = Param(nameof(HourClose), 19)
		.SetRange(0, 23)
		.SetDisplay("Hour Close", "Hour to close the position", "Timing");

		_useTrailing = Param(nameof(UseTrailing), true)
		.SetDisplay("Use Trailing", "Enable trailing stop management", "Risk");

		_profitTrailing = Param(nameof(ProfitTrailing), true)
		.SetDisplay("Profit Trailing", "Require profit before trailing", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 60)
		.SetNotNegative()
		.SetDisplay("Trailing Stop", "Trailing stop distance in points", "Risk");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 5)
		.SetNotNegative()
		.SetDisplay("Trailing Step", "Extra points before moving trailing stop", "Risk");

		_intradayCandleType = Param(nameof(IntradayCandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Intraday Candles", "Candle type for intraday timing", "Data");

		_dailyCandleType = Param(nameof(DailyCandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Daily Candles", "Candle type for previous day analysis", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, IntradayCandleType), (Security, DailyCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastIntradayDate = default;
		_hasTradedToday = false;
		_previousDailyOpen = null;
		_previousDailyClose = null;
		_previousDailyDate = default;
		_entryPrice = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var intradaySubscription = SubscribeCandles(IntradayCandleType);
		intradaySubscription
		.Bind(ProcessIntradayCandle)
		.Start();

		var dailySubscription = SubscribeCandles(DailyCandleType);
		dailySubscription
		.Bind(ProcessDailyCandle)
		.Start();
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_previousDailyOpen = candle.OpenPrice;
		_previousDailyClose = candle.ClosePrice;
		_previousDailyDate = candle.OpenTime.Date;
	}

	private void ProcessIntradayCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var currentDate = candle.OpenTime.Date;
		if (_lastIntradayDate != currentDate)
		{
			_lastIntradayDate = currentDate;
			_hasTradedToday = false;
		}

		if (candle.OpenTime.DayOfWeek != DayOfWeek.Friday)
		{
			UpdateTrailingAndExits(candle);
			return;
		}

		if (!_hasTradedToday && candle.OpenTime.Hour == HourOpen && candle.OpenTime.Minute == 0)
		TryOpenPosition();

		if (UseClosePositions && candle.OpenTime.Hour >= HourClose && Position != 0)
		{
			ClosePosition();
			ResetProtection();
		}

		UpdateTrailingAndExits(candle);
	}

	private void TryOpenPosition()
	{
		if (Position != 0)
		return;

		if (_previousDailyOpen is null || _previousDailyClose is null)
		return;

		if (_previousDailyDate >= _lastIntradayDate)
		{
			var previousDate = _lastIntradayDate.AddDays(-1);
			if (_previousDailyDate > previousDate)
			return;
		}

		if (_previousDailyOpen > _previousDailyClose)
		{
			BuyMarket(Volume);
			_hasTradedToday = true;
		}
		else if (_previousDailyOpen < _previousDailyClose)
		{
			SellMarket(Volume);
			_hasTradedToday = true;
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order == null)
		return;

		var direction = trade.Order.Side;
		if (Position > 0 && direction == Sides.Buy)
		{
			_entryPrice = trade.Trade.Price;
			_stopLossPrice = StopLossPoints > 0 ? trade.Trade.Price - ConvertPoints(StopLossPoints) : null;
			_takeProfitPrice = TakeProfitPoints > 0 ? trade.Trade.Price + ConvertPoints(TakeProfitPoints) : null;
		}
		else if (Position < 0 && direction == Sides.Sell)
		{
			_entryPrice = trade.Trade.Price;
			_stopLossPrice = StopLossPoints > 0 ? trade.Trade.Price + ConvertPoints(StopLossPoints) : null;
			_takeProfitPrice = TakeProfitPoints > 0 ? trade.Trade.Price - ConvertPoints(TakeProfitPoints) : null;
		}
		else if (Position == 0)
		{
			ResetProtection();
		}
	}

	private void UpdateTrailingAndExits(ICandleMessage candle)
	{
		if (Position == 0)
		return;

		if (_stopLossPrice is decimal stopLoss)
		{
			if (Position > 0 && candle.LowPrice <= stopLoss)
			{
				ClosePosition();
				ResetProtection();
				return;
			}

			if (Position < 0 && candle.HighPrice >= stopLoss)
			{
				ClosePosition();
				ResetProtection();
				return;
			}
		}

		if (_takeProfitPrice is decimal takeProfit)
		{
			if (Position > 0 && candle.HighPrice >= takeProfit)
			{
				ClosePosition();
				ResetProtection();
				return;
			}

			if (Position < 0 && candle.LowPrice <= takeProfit)
			{
				ClosePosition();
				ResetProtection();
				return;
			}
		}

		if (!UseTrailing || _entryPrice is null || TrailingStopPoints <= 0)
		return;

		var trailingDistance = ConvertPoints(TrailingStopPoints);
		var trailingStep = ConvertPoints(TrailingStepPoints);

		if (Position > 0)
		{
			var currentPrice = candle.ClosePrice;
			var profit = currentPrice - _entryPrice.Value;
			if (!ProfitTrailing || profit >= trailingDistance)
			{
				var candidate = currentPrice - trailingDistance;
				if (_stopLossPrice is not decimal existing || candidate - existing >= trailingStep)
				_stopLossPrice = candidate;
			}
		}
		else if (Position < 0)
		{
			var currentPrice = candle.ClosePrice;
			var profit = _entryPrice.Value - currentPrice;
			if (!ProfitTrailing || profit >= trailingDistance)
			{
				var candidate = currentPrice + trailingDistance;
				if (_stopLossPrice is not decimal existing || existing - candidate >= trailingStep)
				_stopLossPrice = candidate;
			}
		}
	}

	private decimal ConvertPoints(int points)
	{
		if (points <= 0)
		return 0m;

		var step = Security?.PriceStep ?? 0m;
		if (step > 0m)
		return points * step;

		var decimals = Security?.Decimals ?? 0;
		return (decimal)(points / Math.Pow(10, decimals));
	}

	private void ResetProtection()
	{
		_entryPrice = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}
}
