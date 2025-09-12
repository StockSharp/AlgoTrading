
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Template strategy demonstrating trailing stop and take profit management.
/// Uses EMA crossover for entries.
/// </summary>
public class TemplateTrailingStrategyBacktesterStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailStart;
	private readonly StrategyParam<decimal> _trailDistance;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _fast = new();
	private EMA _slow = new();
	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal? _stop;
	private decimal? _tp;
	private decimal? _trail;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLossPercent { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TrailStartPercent { get => _trailStart.Value; set => _trailStart.Value = value; }
	public decimal TrailDistancePercent { get => _trailDistance.Value; set => _trailDistance.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TemplateTrailingStrategyBacktesterStrategy()
	{
		_fastLength = Param(nameof(FastLength), 20);
		_slowLength = Param(nameof(SlowLength), 50);
		_takeProfit = Param(nameof(TakeProfitPercent), 2m);
		_stopLoss = Param(nameof(StopLossPercent), 1m);
		_trailStart = Param(nameof(TrailStartPercent), 1m);
		_trailDistance = Param(nameof(TrailDistancePercent), 0.5m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0m;
		_prevSlow = 0m;
		_stop = null;
		_tp = null;
		_trail = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		_fast.Length = FastLength;
		_slow.Length = SlowLength;
		var sub = SubscribeCandles(CandleType);
		sub.Bind(Process).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, _fast);
			DrawIndicator(area, _slow);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fastVal = _fast.Process(candle.ClosePrice);
		var slowVal = _slow.Process(candle.ClosePrice);

		var longCond = _prevFast <= _prevSlow && fastVal > slowVal;
		var shortCond = _prevFast >= _prevSlow && fastVal < slowVal;

		if (longCond && Position <= 0)
		{
			BuyMarket();
			var entry = candle.ClosePrice;
			_tp = entry * (1 + TakeProfitPercent / 100m);
			_stop = entry * (1 - StopLossPercent / 100m);
			_trail = null;
		}
		else if (shortCond && Position >= 0)
		{
			SellMarket();
			var entry = candle.ClosePrice;
			_tp = entry * (1 - TakeProfitPercent / 100m);
			_stop = entry * (1 + StopLossPercent / 100m);
			_trail = null;
		}

		if (Position > 0)
		{
			var trigger = PositionAvgPrice * (1 + TrailStartPercent / 100m);
			if (candle.ClosePrice >= trigger)
			{
				var newStop = candle.ClosePrice * (1 - TrailDistancePercent / 100m);
				if (!_trail.HasValue || newStop > _trail)
					_trail = newStop;
			}
			if ((_trail.HasValue && candle.LowPrice <= _trail) || (_stop.HasValue && candle.LowPrice <= _stop))
				SellMarket(Math.Abs(Position));
			else if (_tp.HasValue && candle.HighPrice >= _tp)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			var trigger = PositionAvgPrice * (1 - TrailStartPercent / 100m);
			if (candle.ClosePrice <= trigger)
			{
				var newStop = candle.ClosePrice * (1 + TrailDistancePercent / 100m);
				if (!_trail.HasValue || newStop < _trail)
					_trail = newStop;
			}
			if ((_trail.HasValue && candle.HighPrice >= _trail) || (_stop.HasValue && candle.HighPrice >= _stop))
				BuyMarket(Math.Abs(Position));
			else if (_tp.HasValue && candle.LowPrice <= _tp)
				BuyMarket(Math.Abs(Position));
		}

		_prevFast = fastVal;
		_prevSlow = slowVal;
	}
}
