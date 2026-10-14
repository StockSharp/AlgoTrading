namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Candle 2:45 Breakout Strategy.
/// The candle covering TargetHour:TargetMinute (UTC) sets the reference high and low. During the next LookForwardBars candles a
/// close above the high goes long and a close below the low goes short, reversing an opposite position. Any position is
/// closed when the observation window ends.
/// </summary>
public class Candle245BreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _targetHour;
	private readonly StrategyParam<int> _targetMinute;
	private readonly StrategyParam<int> _lookForwardBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _refHigh;
	private decimal? _refLow;
	private int _barsLeft;

	/// <summary>
	/// Hour of the reference candle (UTC).
	/// </summary>
	public int TargetHour
	{
		get => _targetHour.Value;
		set => _targetHour.Value = value;
	}

	/// <summary>
	/// Minute of the reference candle.
	/// </summary>
	public int TargetMinute
	{
		get => _targetMinute.Value;
		set => _targetMinute.Value = value;
	}

	/// <summary>
	/// Candles after the reference candle during which breakouts are traded.
	/// </summary>
	public int LookForwardBars
	{
		get => _lookForwardBars.Value;
		set => _lookForwardBars.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public Candle245BreakoutStrategy()
	{
		_targetHour = Param(nameof(TargetHour), 2)
			.SetRange(0, 23)
			.SetDisplay("Target Hour", "Hour of the reference candle (UTC)", "Session");

		_targetMinute = Param(nameof(TargetMinute), 45)
			.SetRange(0, 59)
			.SetDisplay("Target Minute", "Minute of the reference candle", "Session");

		_lookForwardBars = Param(nameof(LookForwardBars), 2)
			.SetGreaterThanZero()
			.SetDisplay("Look Forward Bars", "Candles after the reference candle during which breakouts are traded", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(45).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_refHigh = null;
		_refLow = null;
		_barsLeft = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_refHigh = null;
		_refLow = null;
		_barsLeft = 0;

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
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// With frames that do not start exactly at the target time, the candle covering it is the reference.
		var target = candle.OpenTime.Date + new TimeSpan(TargetHour, TargetMinute, 0);
		var frame = CandleType.Arg is TimeSpan tf ? tf : TimeSpan.Zero;
		var isReference = candle.OpenTime == target || (candle.OpenTime < target && target < candle.OpenTime + frame);

		if (isReference)
		{
			_refHigh = candle.HighPrice;
			_refLow = candle.LowPrice;
			_barsLeft = LookForwardBars;
			return;
		}

		if (_barsLeft <= 0 || _refHigh is not decimal high || _refLow is not decimal low)
			return;

		_barsLeft--;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_barsLeft == 0)
		{
			// The observation window ends with this candle.
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(-Position);

			return;
		}

		var close = candle.ClosePrice;

		if (close > high && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < low && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
