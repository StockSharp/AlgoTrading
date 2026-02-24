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
/// Simplified trend following strategy using average price change and moving average.
/// Buys when recent change is positive and price above MA, sells when negative and below MA.
/// </summary>
public class TrendFollowingKnnStrategy : Strategy
{
	private readonly StrategyParam<int> _windowSize;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _changes = new();
	private decimal _prevClose;

	public int WindowSize { get => _windowSize.Value; set => _windowSize.Value = value; }
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrendFollowingKnnStrategy()
	{
		_windowSize = Param(nameof(WindowSize), 20)
			.SetGreaterThanZero()
			.SetDisplay("Window Size", "Bars for average change", "General");

		_maLength = Param(nameof(MaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average length", "General");

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
		_changes.Clear();
		_prevClose = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ma = new SimpleMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevClose == 0)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var change = candle.ClosePrice - _prevClose;
		_prevClose = candle.ClosePrice;

		_changes.Add(change);
		if (_changes.Count > WindowSize)
			_changes.RemoveAt(0);

		if (_changes.Count < WindowSize)
			return;

		var avgChange = 0m;
		for (var i = 0; i < _changes.Count; i++)
			avgChange += _changes[i];
		avgChange /= _changes.Count;

		if (avgChange > 0 && candle.ClosePrice > maValue && Position <= 0)
			BuyMarket();
		else if (avgChange < 0 && candle.ClosePrice < maValue && Position >= 0)
			SellMarket();
	}
}
