using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ehlers SwamiCharts RSI based strategy generating signals from averaged RSI colors.
/// </summary>
public class EhlersSwamiChartsRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _longColor;
	private readonly StrategyParam<int> _shortColor;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex[] _rsis;

	/// <summary>
	/// Long color threshold.
	/// </summary>
	public int LongColor
	{
		get => _longColor.Value;
		set => _longColor.Value = value;
	}

	/// <summary>
	/// Short color threshold.
	/// </summary>
	public int ShortColor
	{
		get => _shortColor.Value;
		set => _shortColor.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="EhlersSwamiChartsRsiStrategy"/>.
	/// </summary>
	public EhlersSwamiChartsRsiStrategy()
	{
		_longColor = Param(nameof(LongColor), 50)
			.SetDisplay("LongColor", "Long color threshold", "General");

		_shortColor = Param(nameof(ShortColor), 50)
			.SetDisplay("ShortColor", "Short color threshold", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
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

		var list = new List<RelativeStrengthIndex>();
		for (var i = 2; i <= 48; i++)
		{
			list.Add(new RelativeStrengthIndex { Length = i });
		}
		_rsis = list.ToArray();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_rsis, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		int color1Tot = 0;
		int color2Tot = 0;
		int count = 0;

		foreach (var val in values)
		{
			if (!val.IsFinal)
				continue;
			var rsi = val.ToDecimal() / 100m;
			int c1;
			int c2;
			if (rsi >= 0.5m)
			{
				c1 = (int)Math.Ceiling(255m * (2m - 2m * rsi));
				c2 = 255;
			}
			else
			{
				c1 = 255;
				c2 = (int)Math.Ceiling(255m * 2m * rsi);
			}
			color1Tot += c1;
			color2Tot += c2;
			count++;
		}

		if (count == 0)
			return;

		var color1Avg = (int)Math.Ceiling(color1Tot / (decimal)count);
		var color2Avg = (int)Math.Ceiling(color2Tot / (decimal)count);

		var longSignal = color1Avg == 255 && color2Avg > LongColor;
		var shortSignal = color1Avg > ShortColor && color2Avg == 255;

		if (longSignal && Position <= 0)
		{
			BuyMarket(Position < 0 ? Math.Abs(Position) + 1 : 1);
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket(Position > 0 ? Position + 1 : 1);
		}
	}
}
