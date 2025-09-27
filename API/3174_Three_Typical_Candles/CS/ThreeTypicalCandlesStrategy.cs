namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// Three Typical Candles pattern strategy.
/// Buys after three rising typical prices and sells after three falling ones.
/// </summary>
public class ThreeTypicalCandlesStrategy : Strategy
{
	private readonly StrategyParam<bool> _useTimeControl;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousTypical;
	private decimal? _previousPreviousTypical;

	public bool UseTimeControl
	{
		get => _useTimeControl.Value;
		set => _useTimeControl.Value = value;
	}
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ThreeTypicalCandlesStrategy()
	{

		_useTimeControl = Param(nameof(UseTimeControl), true)
				.SetDisplay("Use Time Control", "Enable trading hour filter", "Schedule");

		_startHour = Param(nameof(StartHour), 11)
				.SetDisplay("Start Hour", "Trading window start hour (0-23)", "Schedule")
				.SetRange(0, 23);

		_endHour = Param(nameof(EndHour), 17)
				.SetDisplay("End Hour", "Trading window end hour (0-23)", "Schedule")
				.SetRange(0, 23);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
				.SetDisplay("Candle Type", "Candle type used for analysis", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_previousTypical = null;
		_previousPreviousTypical = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		if (UseTimeControl && !IsWithinTradingHours(candle.OpenTime))
		{
			CloseOpenPosition();
			return;
		}

		var typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;

		if (_previousTypical is decimal prev && _previousPreviousTypical is decimal prevPrev)
		{
			var isRisingSequence = prevPrev < prev && prev < typical;
			if (isRisingSequence)
			{
				EnterLong();
			}

			var isFallingSequence = prevPrev > prev && prev > typical;
			if (isFallingSequence)
			{
				EnterShort();
			}
		}

		_previousPreviousTypical = _previousTypical;
		_previousTypical = typical;
	}

	private void EnterLong()
	{
		if (Position > 0m)
			return;

		if (Position < 0m)
			BuyMarket(-Position);

		if (Position == 0m)
			BuyMarket(GetTradeVolume());
	}

	private void EnterShort()
	{
		if (Position < 0m)
			return;

		if (Position > 0m)
			SellMarket(Position);

		if (Position == 0m)
			SellMarket(GetTradeVolume());
	}

	private void CloseOpenPosition()
	{
		if (Position > 0m)
			SellMarket(Position);
		else if (Position < 0m)
			BuyMarket(-Position);
	}

	private decimal GetTradeVolume()
	{
		var volume = Volume;
		var security = Security;
		if (security == null)
			return volume;

		if (volume <= 0m)
			volume = security.VolumeStep ?? 1m;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var rounded = step * Math.Round(volume / step, MidpointRounding.AwayFromZero);
			if (rounded <= 0m)
				rounded = step;
			volume = rounded;
		}

		var minVolume = security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		var maxVolume = security.MaxVolume;
		if (maxVolume is { } max && max > 0m && volume > max)
			volume = max;

		return volume;
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		var hour = time.Hour;
		var start = StartHour;
		var end = EndHour;

		if (start == end)
			return false;

		if (start < end)
			return hour >= start && hour < end;

		return hour >= start || hour < end;
	}
}

