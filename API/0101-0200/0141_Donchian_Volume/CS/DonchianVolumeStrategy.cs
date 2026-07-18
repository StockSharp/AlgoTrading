using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that uses Donchian Channels for breakout detection.
/// Enters when price breaks above/below the channel.
/// </summary>
public class DonchianVolumeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<int> _cooldownBars;

	private int _cooldown;
	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Donchian Channels period.
	/// </summary>
	public int DonchianPeriod
	{
		get => _donchianPeriod.Value;
		set => _donchianPeriod.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Strategy constructor.
	/// </summary>
	public DonchianVolumeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_donchianPeriod = Param(nameof(DonchianPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("Donchian Period", "Period of the Donchian Channel", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 100)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General")
			.SetRange(5, 500);
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
		_cooldown = 0;
		_highs.Clear();
		_lows.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Use a simple SMA as a binding indicator (we use it for middle line reference)
		var sma = new SimpleMovingAverage { Length = DonchianPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		var maxBuf = DonchianPeriod * 2;
		if (_highs.Count > maxBuf)
		{
			_highs.RemoveRange(0, _highs.Count - maxBuf);
			_lows.RemoveRange(0, _lows.Count - maxBuf);
		}

		if (_highs.Count < DonchianPeriod)
			return;

		// Calculate Donchian Channel
		var start = _highs.Count - DonchianPeriod;
		var highestHigh = decimal.MinValue;
		var lowestLow = decimal.MaxValue;
		for (var i = start; i < _highs.Count; i++)
		{
			if (_highs[i] > highestHigh) highestHigh = _highs[i];
			if (_lows[i] < lowestLow) lowestLow = _lows[i];
		}

		var middleLine = (highestHigh + lowestLow) / 2;
		var close = candle.ClosePrice;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Long entry: price breaks above channel
		if (close >= highestHigh && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Short entry: price breaks below channel
		else if (close <= lowestLow && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit long: price crosses below middle line
		if (Position > 0 && close < middleLine)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit short: price crosses above middle line
		else if (Position < 0 && close > middleLine)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
