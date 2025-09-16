using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy placing limit orders around each candle open and protecting position with stop loss and trailing stop.
/// </summary>
public class LimitsBotStrategy : Strategy
{
	private readonly StrategyParam<bool> _buyAllow;
	private readonly StrategyParam<bool> _sellAllow;
	private readonly StrategyParam<decimal> _stopOrderDistance;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailingStart;
	private readonly StrategyParam<decimal> _trailingDistance;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;
	private readonly StrategyParam<DataType> _candleType;

	private Order _buyOrder;
	private Order _sellOrder;
	private decimal? _entryPrice;
	private decimal? _longStop, _longTake, _shortStop, _shortTake;
	private decimal _lastPosition;

	/// <summary>
	/// Allow long entries.
	/// </summary>
	public bool BuyAllow { get => _buyAllow.Value; set => _buyAllow.Value = value; }

	/// <summary>
	/// Allow short entries.
	/// </summary>
	public bool SellAllow { get => _sellAllow.Value; set => _sellAllow.Value = value; }

	/// <summary>
	/// Distance from open price for pending orders (in ticks).
	/// </summary>
	public decimal StopOrderDistance { get => _stopOrderDistance.Value; set => _stopOrderDistance.Value = value; }

	/// <summary>
	/// Take profit distance in ticks.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Stop loss distance in ticks.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Profit in ticks needed to activate trailing stop.
	/// </summary>
	public decimal TrailingStart { get => _trailingStart.Value; set => _trailingStart.Value = value; }

	/// <summary>
	/// Trailing stop distance in ticks.
	/// </summary>
	public decimal TrailingDistance { get => _trailingDistance.Value; set => _trailingDistance.Value = value; }

	/// <summary>
	/// Minimal move in ticks to shift trailing stop.
	/// </summary>
	public decimal TrailingStep { get => _trailingStep.Value; set => _trailingStep.Value = value; }

	/// <summary>
	/// Session start time.
	/// </summary>
	public TimeSpan StartTime { get => _startTime.Value; set => _startTime.Value = value; }

	/// <summary>
	/// Session end time.
	/// </summary>
	public TimeSpan EndTime { get => _endTime.Value; set => _endTime.Value = value; }

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public LimitsBotStrategy()
	{
		_buyAllow = Param(nameof(BuyAllow), true)
			.SetDisplay("Buy Allow", "Enable long orders", "Trading");
		_sellAllow = Param(nameof(SellAllow), true)
			.SetDisplay("Sell Allow", "Enable short orders", "Trading");
		_stopOrderDistance = Param(nameof(StopOrderDistance), 5m)
			.SetDisplay("Stop Order Distance", "Distance from open price", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 35m)
			.SetDisplay("Take Profit", "Take profit in ticks", "Risk");
		_stopLoss = Param(nameof(StopLoss), 8m)
			.SetDisplay("Stop Loss", "Stop loss in ticks", "Risk");
		_trailingStart = Param(nameof(TrailingStart), 40m)
			.SetDisplay("Trailing Start", "Profit to activate trailing", "Risk");
		_trailingDistance = Param(nameof(TrailingDistance), 30m)
			.SetDisplay("Trailing Distance", "Trailing stop distance", "Risk");
		_trailingStep = Param(nameof(TrailingStep), 1m)
			.SetDisplay("Trailing Step", "Minimal move to shift trailing", "Risk");
		_startTime = Param(nameof(StartTime), TimeSpan.Zero)
			.SetDisplay("Start Time", "Trading session start", "Session");
		_endTime = Param(nameof(EndTime), new TimeSpan(23, 59, 0))
			.SetDisplay("End Time", "Trading session end", "Session");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		var priceStep = Security.PriceStep ?? 1m;
		var t = candle.OpenTime.TimeOfDay;
		var inSession = StartTime <= EndTime ? t >= StartTime && t <= EndTime : t >= StartTime || t <= EndTime;

		if (Position > 0 && _lastPosition <= 0)
		{
			_entryPrice = _buyOrder?.Price ?? candle.OpenPrice;
			_longStop = _entryPrice - StopLoss * priceStep;
			_longTake = _entryPrice + TakeProfit * priceStep;
			if (_sellOrder != null)
			{
				CancelOrder(_sellOrder);
				_sellOrder = null;
			}
		}
		else if (Position < 0 && _lastPosition >= 0)
		{
			_entryPrice = _sellOrder?.Price ?? candle.OpenPrice;
			_shortStop = _entryPrice + StopLoss * priceStep;
			_shortTake = _entryPrice - TakeProfit * priceStep;
			if (_buyOrder != null)
			{
				CancelOrder(_buyOrder);
				_buyOrder = null;
			}
		}

		if (Position > 0 && _entryPrice is decimal entryLong)
		{
			if (TrailingDistance > 0m && TrailingStart > 0m && candle.ClosePrice - entryLong >= TrailingStart * priceStep)
			{
				var newStop = candle.ClosePrice - TrailingDistance * priceStep;
				if (_longStop == null || newStop >= _longStop.Value + TrailingStep * priceStep)
					_longStop = newStop;
			}

			if ((_longStop.HasValue && candle.LowPrice <= _longStop) || (_longTake.HasValue && candle.HighPrice >= _longTake))
			{
				SellMarket(Position);
				_entryPrice = _longStop = _longTake = null;
			}
		}
		else if (Position < 0 && _entryPrice is decimal entryShort)
		{
			if (TrailingDistance > 0m && TrailingStart > 0m && entryShort - candle.ClosePrice >= TrailingStart * priceStep)
			{
				var newStop = candle.ClosePrice + TrailingDistance * priceStep;
				if (_shortStop == null || newStop <= _shortStop.Value - TrailingStep * priceStep)
					_shortStop = newStop;
			}

			if ((_shortStop.HasValue && candle.HighPrice >= _shortStop) || (_shortTake.HasValue && candle.LowPrice <= _shortTake))
			{
				BuyMarket(-Position);
				_entryPrice = _shortStop = _shortTake = null;
			}
		}
		else if (Position == 0)
		{
			_entryPrice = null;
			_longStop = _longTake = _shortStop = _shortTake = null;

			if (_buyOrder != null)
			{
				CancelOrder(_buyOrder);
				_buyOrder = null;
			}
			if (_sellOrder != null)
			{
				CancelOrder(_sellOrder);
				_sellOrder = null;
			}

			if (inSession)
			{
				if (BuyAllow)
					_buyOrder = BuyLimit(candle.OpenPrice - StopOrderDistance * priceStep, Volume);
				if (SellAllow)
					_sellOrder = SellLimit(candle.OpenPrice + StopOrderDistance * priceStep, Volume);
			}
		}

		_lastPosition = Position;
	}
}

