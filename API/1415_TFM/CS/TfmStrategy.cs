using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Timeframe multiplier breakout strategy.
/// </summary>
public class TfmStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _candleTime;
	private readonly StrategyParam<int> _multiplier;
	private readonly StrategyParam<bool> _allowShort;

	private decimal _highLevel;
	private decimal _lowLevel;

	/// <summary>
	/// Base candle timeframe.
	/// </summary>
	public TimeSpan CandleTime { get => _candleTime.Value; set => _candleTime.Value = value; }

	/// <summary>
	/// Higher timeframe multiplier.
	/// </summary>
	public int Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }

	/// <summary>
	/// Enable short trades.
	/// </summary>
	public bool AllowShort { get => _allowShort.Value; set => _allowShort.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="TfmStrategy"/> class.
	/// </summary>
	public TfmStrategy()
	{
		_candleTime = Param(nameof(CandleTime), TimeSpan.FromMinutes(1))
			.SetDisplay("Candle Time", "Base candle timeframe", "General");

		_multiplier = Param(nameof(Multiplier), 2)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Higher timeframe multiplier", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(2, 5, 1);

		_allowShort = Param(nameof(AllowShort), false)
			.SetDisplay("Allow Short", "Enable short trades", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleTime.TimeFrame());
		yield return (Security, TimeSpan.FromTicks(CandleTime.Ticks * Multiplier).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_highLevel = 0m;
		_lowLevel = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var baseSub = SubscribeCandles(CandleTime.TimeFrame());
		baseSub.Bind(ProcessBase).Start();

		var highSub = SubscribeCandles(TimeSpan.FromTicks(CandleTime.Ticks * Multiplier).TimeFrame());
		highSub.Bind(ProcessHigh).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, baseSub);
			DrawOwnTrades(area);
		}
	}

	private void ProcessHigh(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_highLevel = candle.HighPrice;
		_lowLevel = candle.LowPrice;
	}

	private void ProcessBase(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.ClosePrice > _highLevel && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (candle.ClosePrice < _lowLevel)
		{
			if (AllowShort && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			else if (!AllowShort && Position > 0)
				SellMarket(Position);
		}
	}
}
