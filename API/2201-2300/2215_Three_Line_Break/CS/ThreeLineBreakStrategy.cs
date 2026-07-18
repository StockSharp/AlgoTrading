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
/// Strategy based on the Three Line Break pattern.
/// Detects trend reversals when price breaks above/below recent N-bar high/low.
/// </summary>
public class ThreeLineBreakStrategy : Strategy
{
	private readonly StrategyParam<int> _linesBreak;
	private readonly StrategyParam<DataType> _candleType;

	private Lowest _lowest;
	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _trendUp = true;

	public int LinesBreak { get => _linesBreak.Value; set => _linesBreak.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ThreeLineBreakStrategy()
	{
		_linesBreak = Param(nameof(LinesBreak), 3)
			.SetGreaterThanZero()
			.SetDisplay("Lines Break", "Number of lines for trend detection", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_lowest = null;
		_prevHigh = 0;
		_prevLow = 0;
		_trendUp = true;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = LinesBreak };
		_lowest = new Lowest { Length = LinesBreak };
		Indicators.Add(_lowest);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(highest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue highValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var lowValue = _lowest.Process(highValue);

		if (!highValue.IsFormed || !lowValue.IsFormed)
			return;

		var currentHigh = highValue.GetValue<decimal>();
		var currentLow = lowValue.GetValue<decimal>();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevHigh = currentHigh;
			_prevLow = currentLow;
			return;
		}

		if (_prevHigh == 0 || _prevLow == 0)
		{
			_prevHigh = currentHigh;
			_prevLow = currentLow;
			return;
		}

		var trendUp = _trendUp;

		if (trendUp && candle.LowPrice < _prevLow)
			trendUp = false;
		else if (!trendUp && candle.HighPrice > _prevHigh)
			trendUp = true;

		if (trendUp != _trendUp)
		{
			if (trendUp && Position <= 0)
				BuyMarket();
			else if (!trendUp && Position >= 0)
				SellMarket();
		}

		_trendUp = trendUp;
		_prevHigh = currentHigh;
		_prevLow = currentLow;
	}
}
