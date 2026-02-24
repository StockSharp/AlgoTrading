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
/// Time range statistics strategy.
/// Collects statistics over rolling windows and trades based on percent change.
/// </summary>
public class TimeRangeStatisticsStrategy : Strategy
{
	private readonly StrategyParam<int> _windowSize;
	private readonly StrategyParam<decimal> _changeThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closes = new();

	public int WindowSize { get => _windowSize.Value; set => _windowSize.Value = value; }
	public decimal ChangeThreshold { get => _changeThreshold.Value; set => _changeThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TimeRangeStatisticsStrategy()
	{
		_windowSize = Param(nameof(WindowSize), 100)
			.SetGreaterThanZero();
		_changeThreshold = Param(nameof(ChangeThreshold), 0.5m)
			.SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_closes.Clear();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 10 };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closes.Add(candle.ClosePrice);
		if (_closes.Count > WindowSize)
			_closes.RemoveAt(0);

		if (_closes.Count < WindowSize)
			return;

		var startPrice = _closes[0];
		var endPrice = _closes[_closes.Count - 1];
		var percentChange = (endPrice - startPrice) / startPrice * 100m;

		// Trade based on percent change over the window
		if (percentChange > ChangeThreshold && Position <= 0)
			BuyMarket();
		else if (percentChange < -ChangeThreshold && Position >= 0)
			SellMarket();
	}
}
