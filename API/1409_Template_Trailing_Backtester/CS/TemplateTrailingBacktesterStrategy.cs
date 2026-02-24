using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Template strategy demonstrating trailing stop and take profit management.
/// Uses EMA crossover for entries.
/// </summary>
public class TemplateTrailingBacktesterStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailDistance;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;
	private decimal _trailStop;
	private decimal _highSinceEntry;
	private decimal _lowSinceEntry;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLossPercent { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TrailDistancePercent { get => _trailDistance.Value; set => _trailDistance.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TemplateTrailingBacktesterStrategy()
	{
		_fastLength = Param(nameof(FastLength), 20);
		_slowLength = Param(nameof(SlowLength), 50);
		_takeProfit = Param(nameof(TakeProfitPercent), 2m);
		_stopLoss = Param(nameof(StopLossPercent), 1m);
		_trailDistance = Param(nameof(TrailDistancePercent), 0.5m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
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
		_prevFast = 0m;
		_prevSlow = 0m;
		_entryPrice = 0m;
		_trailStop = 0m;
		_highSinceEntry = 0m;
		_lowSinceEntry = decimal.MaxValue;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new ExponentialMovingAverage { Length = FastLength };
		var slow = new ExponentialMovingAverage { Length = SlowLength };

		var sub = SubscribeCandles(CandleType);
		sub.Bind(fast, slow, Process).Start();
	}

	private void Process(ICandleMessage candle, decimal fastVal, decimal slowVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Manage trailing and exits first
		if (Position > 0 && _entryPrice > 0)
		{
			_highSinceEntry = Math.Max(_highSinceEntry, candle.HighPrice);
			var newTrail = _highSinceEntry * (1 - TrailDistancePercent / 100m);
			_trailStop = Math.Max(_trailStop, newTrail);

			var tp = _entryPrice * (1 + TakeProfitPercent / 100m);
			var sl = _entryPrice * (1 - StopLossPercent / 100m);
			var effectiveStop = Math.Max(sl, _trailStop);

			if (candle.ClosePrice <= effectiveStop || candle.ClosePrice >= tp)
			{
				SellMarket();
				_entryPrice = 0m;
				_prevFast = fastVal;
				_prevSlow = slowVal;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			_lowSinceEntry = Math.Min(_lowSinceEntry, candle.LowPrice);
			var newTrail = _lowSinceEntry * (1 + TrailDistancePercent / 100m);
			if (_trailStop == 0 || newTrail < _trailStop)
				_trailStop = newTrail;

			var tp = _entryPrice * (1 - TakeProfitPercent / 100m);
			var sl = _entryPrice * (1 + StopLossPercent / 100m);
			var effectiveStop = _trailStop > 0 ? Math.Min(sl, _trailStop) : sl;

			if (candle.ClosePrice >= effectiveStop || candle.ClosePrice <= tp)
			{
				BuyMarket();
				_entryPrice = 0m;
				_prevFast = fastVal;
				_prevSlow = slowVal;
				return;
			}
		}

		// Entries only when flat
		if (Position == 0 && _prevFast != 0 && _prevSlow != 0)
		{
			var longCond = _prevFast <= _prevSlow && fastVal > slowVal;
			var shortCond = _prevFast >= _prevSlow && fastVal < slowVal;

			if (longCond)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_highSinceEntry = candle.HighPrice;
				_trailStop = 0m;
			}
			else if (shortCond)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_lowSinceEntry = candle.LowPrice;
				_trailStop = 0m;
			}
		}

		_prevFast = fastVal;
		_prevSlow = slowVal;
	}
}
