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
/// Up Gap with Delay strategy.
/// Goes long after a gap up if sufficient bars have passed since previous trade.
/// Exits after holding for specified number of bars.
/// </summary>
public class UpGapWithDelayStrategy : Strategy
{
	private readonly StrategyParam<decimal> _gapThreshold;
	private readonly StrategyParam<int> _delayPeriods;
	private readonly StrategyParam<int> _holdingPeriods;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private long _entryIndex;
	private long _currentIndex;

	public decimal GapThreshold { get => _gapThreshold.Value; set => _gapThreshold.Value = value; }
	public int DelayPeriods { get => _delayPeriods.Value; set => _delayPeriods.Value = value; }
	public int HoldingPeriods { get => _holdingPeriods.Value; set => _holdingPeriods.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public UpGapWithDelayStrategy()
	{
		_gapThreshold = Param(nameof(GapThreshold), 0.1m)
			.SetDisplay("Gap Threshold (%)", "Minimum gap size", "General");

		_delayPeriods = Param(nameof(DelayPeriods), 0)
			.SetDisplay("Delay Periods", "Bars to wait", "General");

		_holdingPeriods = Param(nameof(HoldingPeriods), 7)
			.SetGreaterThanZero()
			.SetDisplay("Holding Periods", "Bars to hold", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0;
		_entryIndex = -100;
		_currentIndex = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Need a dummy indicator for Bind to work in backtest
		var sma = new SimpleMovingAverage { Length = 2 };

		_prevClose = 0;
		_entryIndex = -100;
		_currentIndex = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_currentIndex++;

		if (_prevClose > 0)
		{
			var gapSize = (candle.OpenPrice - _prevClose) / _prevClose * 100m;
			var upGap = gapSize >= GapThreshold;
			var canEnter = upGap && (_currentIndex > _entryIndex + DelayPeriods);

			if (canEnter && Position <= 0)
			{
				BuyMarket();
				_entryIndex = _currentIndex;
			}

			if (Position > 0 && _currentIndex >= _entryIndex + HoldingPeriods)
			{
				SellMarket();
			}
		}

		_prevClose = candle.ClosePrice;
	}
}
