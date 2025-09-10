using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// One-Two-Three Reversal strategy - enters on bullish 1-2-3 pattern and exits after holding period or when price closes above moving average.
/// </summary>
public class OneTwoThreeReversalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _daysToHold;
	private readonly StrategyParam<int> _maLength;

	private SimpleMovingAverage _sma;

	private decimal _low1;
	private decimal _low2;
	private decimal _low3;
	private decimal _low4;
	private decimal _high1;
	private decimal _high2;
	private decimal _high3;
	private int _historyCount;
	private int _barsSinceEntry = int.MaxValue;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of bars to hold position.
	/// </summary>
	public int DaysToHold
	{
		get => _daysToHold.Value;
		set => _daysToHold.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public OneTwoThreeReversalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_daysToHold = Param(nameof(DaysToHold), 7)
			.SetGreaterThanZero()
			.SetDisplay("Days To Hold", "Number of bars to hold position", "General")
			.SetCanOptimize(true)
			.SetOptimize(3, 14, 1);

		_maLength = Param(nameof(MaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average length", "Moving Average")
			.SetCanOptimize(true)
			.SetOptimize(50, 300, 50);
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

		StartProtection();

		_sma = new SimpleMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position > 0)
			_barsSinceEntry++;

		if (_historyCount >= 4)
		{
			var condition1 = candle.LowPrice < _low1;
			var condition2 = _low1 < _low3;
			var condition3 = _low2 < _low4;
			var condition4 = _high2 < _high3;

			var exitCondition = Position > 0 && (_barsSinceEntry >= DaysToHold || (_sma.IsFormed && candle.ClosePrice >= maValue));

			if (exitCondition)
			{
				RegisterSell();
				_barsSinceEntry = int.MaxValue;
			}
			else if (condition1 && condition2 && condition3 && condition4 && Position <= 0)
			{
				RegisterBuy();
				_barsSinceEntry = 0;
			}
		}

		_low4 = _low3;
		_low3 = _low2;
		_low2 = _low1;
		_low1 = candle.LowPrice;

		_high3 = _high2;
		_high2 = _high1;
		_high1 = candle.HighPrice;

		if (_historyCount < 4)
			_historyCount++;
	}
}
