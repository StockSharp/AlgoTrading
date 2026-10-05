using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Futures Trading Hours RSI strategy.
/// Trades only between SessionStart and SessionEnd in US Central Time. Inside the session an RSI crossing above OverSoldLevel goes
/// long and an RSI crossing below OverBoughtLevel goes short, reversing an opposite position. Any position still open at or after
/// SessionEnd is closed.
/// </summary>
public class FuturesTradingHoursRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _overSoldLevel;
	private readonly StrategyParam<decimal> _overBoughtLevel;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevRsi;

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal OverSoldLevel
	{
		get => _overSoldLevel.Value;
		set => _overSoldLevel.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal OverBoughtLevel
	{
		get => _overBoughtLevel.Value;
		set => _overBoughtLevel.Value = value;
	}

	/// <summary>
	/// Session start in US Central Time.
	/// </summary>
	public TimeSpan SessionStart
	{
		get => _sessionStart.Value;
		set => _sessionStart.Value = value;
	}

	/// <summary>
	/// Session end in US Central Time.
	/// </summary>
	public TimeSpan SessionEnd
	{
		get => _sessionEnd.Value;
		set => _sessionEnd.Value = value;
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
	public FuturesTradingHoursRsiStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI length", "Indicators");

		_overSoldLevel = Param(nameof(OverSoldLevel), 30m)
			.SetDisplay("Oversold", "RSI oversold level", "Indicators");

		_overBoughtLevel = Param(nameof(OverBoughtLevel), 70m)
			.SetDisplay("Overbought", "RSI overbought level", "Indicators");

		_sessionStart = Param(nameof(SessionStart), new TimeSpan(8, 30, 0))
			.SetDisplay("Session Start", "Session start in US Central Time", "Session");

		_sessionEnd = Param(nameof(SessionEnd), new TimeSpan(15, 0, 0))
			.SetDisplay("Session End", "Session end in US Central Time", "Session");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevRsi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevRsi = null;

		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, rsi);
		}
	}

	private static TimeZoneInfo GetCentralTimeZone()
	{
		try
		{
			return TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
		}
		catch (TimeZoneNotFoundException)
		{
			return TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevRsi = _prevRsi;
		_prevRsi = rsi;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var utc = DateTime.SpecifyKind(candle.OpenTime, DateTimeKind.Utc);
		var timeOfDay = TimeZoneInfo.ConvertTimeFromUtc(utc, GetCentralTimeZone()).TimeOfDay;

		if (timeOfDay >= SessionEnd || timeOfDay < SessionStart)
		{
			if (timeOfDay >= SessionEnd)
			{
				if (Position > 0)
					SellMarket(Position);
				else if (Position < 0)
					BuyMarket(-Position);
			}

			return;
		}

		if (prevRsi is not decimal lastRsi)
			return;

		if (lastRsi <= OverSoldLevel && rsi > OverSoldLevel && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (lastRsi >= OverBoughtLevel && rsi < OverBoughtLevel && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
