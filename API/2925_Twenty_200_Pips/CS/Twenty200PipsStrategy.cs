using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily breakout strategy derived from the "20/200 pips" MQL5 expert.
/// Compares open prices from different bar offsets and trades the breakout.
/// </summary>
public class Twenty200PipsStrategy : Strategy
{
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _firstOffset;
	private readonly StrategyParam<int> _secondOffset;
	private readonly StrategyParam<int> _deltaPoints;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _opens = new();
	private decimal _pointValue;

	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public int FirstOffset
	{
		get => _firstOffset.Value;
		set => _firstOffset.Value = value;
	}

	public int SecondOffset
	{
		get => _secondOffset.Value;
		set => _secondOffset.Value = value;
	}

	public int DeltaPoints
	{
		get => _deltaPoints.Value;
		set => _deltaPoints.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public Twenty200PipsStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 200)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (points)", "Take profit distance in points", "Risk")
			.SetOptimize(50, 500, 50);

		_stopLoss = Param(nameof(StopLoss), 2000)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (points)", "Stop loss distance in points", "Risk")
			.SetOptimize(200, 4000, 100);

		_firstOffset = Param(nameof(FirstOffset), 7)
			.SetGreaterThanZero()
			.SetDisplay("First Offset", "Older bar index", "Signal")
			.SetOptimize(1, 12, 1);

		_secondOffset = Param(nameof(SecondOffset), 2)
			.SetGreaterThanZero()
			.SetDisplay("Second Offset", "Newer bar index", "Signal")
			.SetOptimize(1, 6, 1);

		_deltaPoints = Param(nameof(DeltaPoints), 1)
			.SetGreaterThanZero()
			.SetDisplay("Delta (points)", "Minimum difference between opens", "Signal")
			.SetOptimize(10, 200, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_opens.Clear();
		_pointValue = 0m;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_pointValue = Security?.PriceStep ?? 1m;
		if (_pointValue <= 0m)
			_pointValue = 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: new Unit(TakeProfit * _pointValue, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss * _pointValue, UnitTypes.Absolute),
			useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_opens.Add(candle.OpenPrice);

		var maxOffset = Math.Max(FirstOffset, SecondOffset);
		if (_opens.Count <= maxOffset)
			return;

		// Keep buffer limited
		if (_opens.Count > maxOffset + 100)
			_opens.RemoveRange(0, _opens.Count - maxOffset - 50);

		if (Position != 0)
			return;

		var openFirst = _opens[_opens.Count - 1 - FirstOffset];
		var openSecond = _opens[_opens.Count - 1 - SecondOffset];
		var threshold = DeltaPoints * _pointValue;

		if (openFirst > openSecond + threshold)
		{
			SellMarket();
		}
		else if (openFirst + threshold < openSecond)
		{
			BuyMarket();
		}
	}
}
