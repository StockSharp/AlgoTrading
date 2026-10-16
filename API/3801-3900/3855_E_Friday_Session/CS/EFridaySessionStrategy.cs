namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Takes at most one contrarian position on Friday at the configured hour,
/// using the previous completed daily candle; manages session and price exits.
/// </summary>
public class EFridaySessionStrategy : Strategy
{
	private readonly StrategyParam<int> _hourOpen;
	private readonly StrategyParam<int> _hourClose;
	private readonly StrategyParam<bool> _useClosePositions;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _useTrailing;
	private readonly StrategyParam<bool> _profitTrailing;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<DataType> _intradayCandleType;
	private readonly StrategyParam<DataType> _dailyCandleType;

	private DateTime? _lastDailyDate;
	private decimal _dailyOpen;
	private decimal _dailyClose;
	private DateTime? _tradedDate;
	private decimal? _entryPrice;
	private decimal? _trailingLevel;
	private decimal _bestPrice;
	private Order _entryOrder;
	private Order _exitOrder;

	public int HourOpen { get => _hourOpen.Value; set => _hourOpen.Value = value; }
	public int HourClose { get => _hourClose.Value; set => _hourClose.Value = value; }
	public bool UseClosePositions { get => _useClosePositions.Value; set => _useClosePositions.Value = value; }
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }
	public bool UseTrailing { get => _useTrailing.Value; set => _useTrailing.Value = value; }
	public bool ProfitTrailing { get => _profitTrailing.Value; set => _profitTrailing.Value = value; }
	public decimal TrailingStopPoints { get => _trailingStopPoints.Value; set => _trailingStopPoints.Value = value; }
	public decimal TrailingStepPoints { get => _trailingStepPoints.Value; set => _trailingStepPoints.Value = value; }
	public DataType IntradayCandleType { get => _intradayCandleType.Value; set => _intradayCandleType.Value = value; }
	public DataType DailyCandleType { get => _dailyCandleType.Value; set => _dailyCandleType.Value = value; }

	public EFridaySessionStrategy()
	{
		Volume = 0.1m;
		_hourOpen = Param(nameof(HourOpen), 7).SetRange(0, 23);
		_hourClose = Param(nameof(HourClose), 19).SetRange(0, 23);
		_useClosePositions = Param(nameof(UseClosePositions), true);
		_stopLossPoints = Param(nameof(StopLossPoints), 75m).SetNotNegative();
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m).SetNotNegative();
		_useTrailing = Param(nameof(UseTrailing), true);
		_profitTrailing = Param(nameof(ProfitTrailing), true);
		_trailingStopPoints = Param(nameof(TrailingStopPoints), 60m).SetNotNegative();
		_trailingStepPoints = Param(nameof(TrailingStepPoints), 5m).SetNotNegative();
		_intradayCandleType = Param(nameof(IntradayCandleType), TimeSpan.FromMinutes(1).TimeFrame());
		_dailyCandleType = Param(nameof(DailyCandleType), TimeSpan.FromDays(1).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, IntradayCandleType), (Security, DailyCandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ClearState();
	}

	private void ClearState()
	{
		_lastDailyDate = null;
		_dailyOpen = 0m;
		_dailyClose = 0m;
		_tradedDate = null;
		_entryPrice = null;
		_trailingLevel = null;
		_bestPrice = 0m;
		_entryOrder = null;
		_exitOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ClearState();
		if ((Security?.PriceStep ?? 0m) <= 0m)
			throw new InvalidOperationException("E-Friday requires a positive PriceStep.");
		SubscribeCandles(DailyCandleType).Bind(ProcessDailyCandle).Start();
		SubscribeCandles(IntradayCandleType).Bind(ProcessIntradayCandle).Start();
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		_lastDailyDate = candle.OpenTime.Date;
		_dailyOpen = candle.OpenPrice;
		_dailyClose = candle.ClosePrice;
	}

	private static bool IsPending(Order order) => order is not null &&
		order.State is not (OrderStates.Done or OrderStates.Failed);

	private void ProcessIntradayCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position != 0m && !IsPending(_exitOrder))
		{
			if (UseClosePositions && candle.OpenTime.Hour >= HourClose)
			{
				ExitPosition("EFriday session close");
				return;
			}
			if (HandleRiskExits(candle))
				return;
		}

		var day = candle.OpenTime.Date;
		if (candle.OpenTime.DayOfWeek != DayOfWeek.Friday ||
			candle.OpenTime.Hour != HourOpen || _tradedDate == day ||
			_lastDailyDate != day.AddDays(-1) ||
			Position != 0m || IsPending(_entryOrder) || IsPending(_exitOrder))
			return;

		if (_dailyClose == _dailyOpen)
			return;

		var side = _dailyClose < _dailyOpen ? Sides.Buy : Sides.Sell;
		var volume = Volume;
		if (Security?.VolumeStep is decimal step && step > 0m)
			volume = decimal.Floor(volume / step) * step;
		if (Security?.MinVolume is decimal min && min > 0m)
			volume = Math.Max(volume, min);
		if (Security?.MaxVolume is decimal max && max > 0m)
			volume = Math.Min(volume, max);
		if (volume <= 0m)
			return;

		_entryOrder = Market(side, volume, "EFriday entry");
		_tradedDate = day;
	}

	private bool HandleRiskExits(ICandleMessage candle)
	{
		if (_entryPrice is not decimal entry)
			return false;
		var point = Security.PriceStep;
		var longPosition = Position > 0m;

		var stop = StopLossPoints * point;
		var take = TakeProfitPoints * point;
		if (stop > 0m && (longPosition ? candle.LowPrice <= entry - stop :
			candle.HighPrice >= entry + stop))
		{
			ExitPosition("EFriday stop loss");
			return true;
		}
		if (take > 0m && (longPosition ? candle.HighPrice >= entry + take :
			candle.LowPrice <= entry - take))
		{
			ExitPosition("EFriday take profit");
			return true;
		}

		// Check the previously established trailing level before using the
		// latest bar's extremes: intrabar high/low order is not available.
		if (_trailingLevel is decimal oldStop &&
			(longPosition ? candle.LowPrice <= oldStop : candle.HighPrice >= oldStop))
		{
			ExitPosition("EFriday trailing stop");
			return true;
		}

		if (!UseTrailing || TrailingStopPoints <= 0m)
			return false;

		var distance = TrailingStopPoints * point;
		var improvement = TrailingStepPoints * point;
		_bestPrice = longPosition ? Math.Max(_bestPrice, candle.HighPrice) :
			Math.Min(_bestPrice, candle.LowPrice);
		var profit = longPosition ? _bestPrice - entry : entry - _bestPrice;
		if (ProfitTrailing && profit < distance)
			return false;

		var next = longPosition ? _bestPrice - distance : _bestPrice + distance;
		if (_trailingLevel is not decimal current ||
			(longPosition ? next >= current + improvement && next > current :
				next <= current - improvement && next < current))
			_trailingLevel = next;

		return false;
	}

	private Order Market(Sides side, decimal volume, string comment)
	{
		var order = new Order
		{
			Security = Security,
			Portfolio = Portfolio,
			Type = OrderTypes.Market,
			Side = side,
			Volume = volume,
			Comment = comment,
		};
		RegisterOrder(order);
		return order;
	}

	private void ExitPosition(string comment)
	{
		if (Position == 0m || IsPending(_exitOrder))
			return;
		_exitOrder = Market(Position > 0m ? Sides.Sell : Sides.Buy, Math.Abs(Position), comment);
	}

	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);
		if (trade?.Order == _entryOrder && trade.Trade is not null)
		{
			_entryPrice = trade.Trade.Price;
			_bestPrice = _entryPrice.Value;
			_trailingLevel = null;
		}
		else if (Position == 0m)
		{
			_entryPrice = null;
			_trailingLevel = null;
			_bestPrice = 0m;
			_exitOrder = null;
		}
	}
}
