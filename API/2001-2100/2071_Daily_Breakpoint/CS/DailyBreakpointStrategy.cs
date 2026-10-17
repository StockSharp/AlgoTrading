using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily-open breakout in security price steps, gated by the preceding
/// completed candle's body. Fixed protection uses actual fills and the optional
/// trailing exit is evaluated only on subsequent completed candles.
/// </summary>
public class DailyBreakpointStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _breakPoint, _lastBarMin, _lastBarMax;
	private readonly StrategyParam<decimal> _takeProfit, _stopLoss;
	private readonly StrategyParam<decimal> _trailingStart, _trailingStop, _trailingStep;

	private decimal _dailyOpen, _previousOpen, _previousClose;
	private DateTimeOffset _previousTime;
	private bool _hasPrevious;
	private decimal? _entryPrice, _bestPrice, _trailPrice;
	private decimal _trackedPosition;
	private Order _pending;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal BreakPoint { get => _breakPoint.Value; set => _breakPoint.Value = value; }
	public decimal LastBarMin { get => _lastBarMin.Value; set => _lastBarMin.Value = value; }
	public decimal LastBarMax { get => _lastBarMax.Value; set => _lastBarMax.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TrailingStart { get => _trailingStart.Value; set => _trailingStart.Value = value; }
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	public decimal TrailingStep { get => _trailingStep.Value; set => _trailingStep.Value = value; }

	public DailyBreakpointStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Signal timeframe", "General");
		_breakPoint = Param(nameof(BreakPoint), 30m).SetNotNegative()
			.SetDisplay("Break Point", "Daily-open breakout offset in price steps", "Trading");
		_lastBarMin = Param(nameof(LastBarMin), 5m).SetNotNegative()
			.SetDisplay("Last Bar Min", "Minimum previous candle body in price steps", "Trading");
		_lastBarMax = Param(nameof(LastBarMax), 100m).SetNotNegative()
			.SetDisplay("Last Bar Max", "Maximum previous candle body in price steps", "Trading");
		_takeProfit = Param(nameof(TakeProfit), 200m).SetNotNegative()
			.SetDisplay("Take Profit", "Optional target in price steps", "Risk");
		_stopLoss = Param(nameof(StopLoss), 100m).SetNotNegative()
			.SetDisplay("Stop Loss", "Optional risk in price steps", "Risk");
		_trailingStart = Param(nameof(TrailingStart), 0m).SetNotNegative()
			.SetDisplay("Trailing Start", "Favorable move activating trailing in steps", "Risk");
		_trailingStop = Param(nameof(TrailingStop), 0m).SetNotNegative()
			.SetDisplay("Trailing Stop", "Trail distance; zero disables trailing", "Risk");
		_trailingStep = Param(nameof(TrailingStep), 1m).SetGreaterThanZero()
			.SetDisplay("Trailing Step", "Minimum improvement before a trail update", "Risk");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	private void ResetState()
	{
		_dailyOpen = _previousOpen = _previousClose = 0m;
		_previousTime = default;
		_hasPrevious = false;
		_entryPrice = _bestPrice = _trailPrice = null;
		_trackedPosition = 0m;
		_pending = null;
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ResetState();
		var step = Security.PriceStep is decimal valid && valid > 0m ? valid : 1m;
		StartProtection(
			takeProfit: new Unit(TakeProfit * step, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss * step, UnitTypes.Absolute),
			useMarketOrders: true);
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	protected override void OnNewPosition(Position position)
	{
		base.OnNewPosition(position);
		ObservePosition(position);
	}

	protected override void OnPositionChanged(Position position)
	{
		base.OnPositionChanged(position);
		ObservePosition(position);
	}

	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);
		ObservePosition(position);
	}

	private void ObservePosition(Position position)
	{
		if (position.Security != Security || position.Portfolio != Portfolio) return;
		var volume = position.CurrentValue ?? 0m;
		if (volume == 0m)
		{
			_entryPrice = _bestPrice = _trailPrice = null;
			_trackedPosition = 0m;
			return;
		}
		if (position.AveragePrice is not decimal avg || avg <= 0m) return;
		if (_entryPrice == avg && _trackedPosition * volume > 0m)
		{
			_trackedPosition = volume;
			return;
		}
		_entryPrice = _bestPrice = avg;
		_trailPrice = null;
		_trackedPosition = volume;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished) return;

		// The daily open is pinned to the very first completed candle of that day.
		var isNewDay = !_hasPrevious || candle.OpenTime.Date != _previousTime.Date;
		if (isNewDay) _dailyOpen = candle.OpenPrice;

		var step = Security.PriceStep is decimal valid && valid > 0m ? valid : 1m;
		var price = candle.ClosePrice;
		if (IsFormedAndOnlineAndAllowTrading() &&
			(_pending is null || _pending.State is OrderStates.Done or OrderStates.Failed))
		{
			if (Position != 0m && _entryPrice is decimal entry && TrailingStop > 0m)
			{
				var isLong = Position > 0m;
				var best = _bestPrice ?? entry;
				best = isLong ? Math.Max(best, price) : Math.Min(best, price);
				_bestPrice = best;
				if ((isLong ? best - entry : entry - best) >= TrailingStart * step)
				{
					var candidate = isLong ? best - TrailingStop * step : best + TrailingStop * step;
					if (_trailPrice is not decimal current ||
						(isLong ? candidate >= current + TrailingStep * step
							: candidate <= current - TrailingStep * step))
						_trailPrice = candidate;
				}
				if (_trailPrice is decimal trail && (isLong ? price <= trail : price >= trail))
					_pending = isLong ? SellMarket(Math.Abs(Position)) : BuyMarket(Math.Abs(Position));
			}
			else if (Position == 0m && !isNewDay && _hasPrevious && _previousTime.Date == candle.OpenTime.Date)
			{
				var range = Math.Abs(_previousClose - _previousOpen);
				var offset = BreakPoint * step;
				var buyBreak = _dailyOpen + offset;
				var sellBreak = _dailyOpen - offset;
				var validSize = range >= LastBarMin * step && range <= LastBarMax * step;

				if (validSize && _previousClose > _previousOpen && price > buyBreak &&
					buyBreak >= _previousOpen && buyBreak <= _previousClose)
					_pending = BuyMarket(Volume);
				else if (validSize && _previousClose < _previousOpen && price < sellBreak &&
					sellBreak <= _previousOpen && sellBreak >= _previousClose)
					_pending = SellMarket(Volume);
			}
		}

		_previousOpen = candle.OpenPrice;
		_previousClose = candle.ClosePrice;
		_previousTime = candle.OpenTime;
		_hasPrevious = true;
	}
}
