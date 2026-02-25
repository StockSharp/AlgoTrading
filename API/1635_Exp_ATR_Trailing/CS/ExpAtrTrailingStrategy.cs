using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ATR trailing stop strategy. Enters on EMA crossover, exits when ATR trailing stop is hit.
/// </summary>
public class ExpAtrTrailingStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrFactor;
	private readonly StrategyParam<int> _fastEma;
	private readonly StrategyParam<int> _slowEma;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _longTrail;
	private decimal _shortTrail;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrFactor { get => _atrFactor.Value; set => _atrFactor.Value = value; }
	public int FastEma { get => _fastEma.Value; set => _fastEma.Value = value; }
	public int SlowEma { get => _slowEma.Value; set => _slowEma.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ExpAtrTrailingStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Indicators");

		_atrFactor = Param(nameof(AtrFactor), 2m)
			.SetDisplay("ATR Factor", "ATR multiplier for trailing stop", "Indicators");

		_fastEma = Param(nameof(FastEma), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA for entry", "Indicators");

		_slowEma = Param(nameof(SlowEma), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA for entry", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_longTrail = 0;
		_shortTrail = 0;
		_prevFast = 0;
		_prevSlow = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new ExponentialMovingAverage { Length = FastEma };
		var slow = new ExponentialMovingAverage { Length = SlowEma };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Trail management
		if (Position > 0)
		{
			var stop = candle.ClosePrice - atrVal * AtrFactor;
			if (stop > _longTrail)
				_longTrail = stop;

			if (candle.LowPrice <= _longTrail)
			{
				SellMarket();
				_longTrail = 0;
			}
		}
		else if (Position < 0)
		{
			var stop = candle.ClosePrice + atrVal * AtrFactor;
			if (stop < _shortTrail || _shortTrail == 0)
				_shortTrail = stop;

			if (candle.HighPrice >= _shortTrail)
			{
				BuyMarket();
				_shortTrail = 0;
			}
		}

		// Entry signals
		if (_hasPrev && atrVal > 0)
		{
			var crossUp = _prevFast <= _prevSlow && fast > slow;
			var crossDown = _prevFast >= _prevSlow && fast < slow;

			if (crossUp && Position <= 0)
			{
				BuyMarket();
				_longTrail = candle.ClosePrice - atrVal * AtrFactor;
				_shortTrail = 0;
			}
			else if (crossDown && Position >= 0)
			{
				SellMarket();
				_shortTrail = candle.ClosePrice + atrVal * AtrFactor;
				_longTrail = 0;
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
		_hasPrev = true;
	}
}
