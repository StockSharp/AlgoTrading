using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades breakouts of the visible chart range defined by a number of recent bars.
/// </summary>
public class VisibleChartStrategy : Strategy
{
	private readonly StrategyParam<int> _visibleBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _highVal;
	private decimal _lowVal;
	private int _cooldown;

	public int VisibleBars { get => _visibleBars.Value; set => _visibleBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VisibleChartStrategy()
	{
		_visibleBars = Param(nameof(VisibleBars), 40)
			.SetDisplay("Visible Bars", "Number of bars considered visible", "General")
			.SetOptimize(20, 200, 20);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to analyze", "General");
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
		_highVal = 0;
		_lowVal = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = VisibleBars };
		var lowest = new Lowest { Length = VisibleBars };

		_highVal = 0;
		_lowVal = 0;
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal high, decimal low)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			_highVal = high;
			_lowVal = low;
			return;
		}

		if (_highVal == 0)
		{
			_highVal = high;
			_lowVal = low;
			return;
		}

		var breakoutUp = candle.ClosePrice >= _highVal && Position <= 0;
		var breakoutDown = candle.ClosePrice <= _lowVal && Position >= 0;

		if (breakoutUp)
		{
			BuyMarket();
			_cooldown = 30;
		}
		else if (breakoutDown)
		{
			SellMarket();
			_cooldown = 30;
		}

		_highVal = high;
		_lowVal = low;
	}
}
