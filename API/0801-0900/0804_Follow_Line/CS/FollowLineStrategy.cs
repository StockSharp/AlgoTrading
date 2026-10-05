using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Follow Line strategy.
/// A close above the upper Bollinger Band moves the follow line up to the candle low (minus ATR with UseAtrFilter) but never down;
/// a close below the lower band moves it down to the candle high (plus ATR) but never up; otherwise it stays. The trend turns up when
/// the line rises and down when it falls. A turn up goes long and a turn down goes short, reversing an opposite position. With
/// UseHtfConfirmation the same follow line on HtfCandleType must point the same way, and a position is closed when that trend turns
/// against it. With UseTimeFilter entries happen only inside Session ("HHmm-HHmm", UTC).
/// </summary>
public class FollowLineStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _bbDeviation;
	private readonly StrategyParam<bool> _useAtrFilter;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<string> _session;
	private readonly StrategyParam<bool> _useHtfConfirmation;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _htfCandleType;

	private decimal? _line;
	private int _trend;
	private int _prevTrend;
	private decimal? _htfLine;
	private int _htfTrend;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BbPeriod
	{
		get => _bbPeriod.Value;
		set => _bbPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation.
	/// </summary>
	public decimal BbDeviation
	{
		get => _bbDeviation.Value;
		set => _bbDeviation.Value = value;
	}

	/// <summary>
	/// Offset the follow line by ATR.
	/// </summary>
	public bool UseAtrFilter
	{
		get => _useAtrFilter.Value;
		set => _useAtrFilter.Value = value;
	}

	/// <summary>
	/// Enter only inside the session.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Trading session as "HHmm-HHmm" in UTC.
	/// </summary>
	public string Session
	{
		get => _session.Value;
		set => _session.Value = value;
	}

	/// <summary>
	/// Require the higher timeframe follow line to agree.
	/// </summary>
	public bool UseHtfConfirmation
	{
		get => _useHtfConfirmation.Value;
		set => _useHtfConfirmation.Value = value;
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
	/// Higher timeframe candle type.
	/// </summary>
	public DataType HtfCandleType
	{
		get => _htfCandleType.Value;
		set => _htfCandleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public FollowLineStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Indicators");

		_bbPeriod = Param(nameof(BbPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");

		_bbDeviation = Param(nameof(BbDeviation), 1m)
			.SetGreaterThanZero()
			.SetDisplay("BB Deviation", "Bollinger Bands deviation", "Indicators");

		_useAtrFilter = Param(nameof(UseAtrFilter), true)
			.SetDisplay("Use ATR Filter", "Offset the follow line by ATR", "Indicators");

		_useTimeFilter = Param(nameof(UseTimeFilter), false)
			.SetDisplay("Use Time Filter", "Enter only inside the session", "Time");

		_session = Param(nameof(Session), "0000-2400")
			.SetDisplay("Session", "Trading session as HHmm-HHmm in UTC", "Time");

		_useHtfConfirmation = Param(nameof(UseHtfConfirmation), false)
			.SetDisplay("HTF Confirmation", "Require the higher timeframe follow line to agree", "Higher Timeframe");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_htfCandleType = Param(nameof(HtfCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("HTF Candle Type", "Higher timeframe candles", "Higher Timeframe");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (UseHtfConfirmation)
			return [(Security, CandleType), (Security, HtfCandleType)];

		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		_line = null;
		_trend = 0;
		_prevTrend = 0;
		_htfLine = null;
		_htfTrend = 0;
	}

	private static decimal? NextLine(ICandleMessage candle, decimal? prevLine, decimal upper, decimal lower, decimal atr, bool useAtr)
	{
		if (candle.ClosePrice > upper)
		{
			var candidate = useAtr ? candle.LowPrice - atr : candle.LowPrice;
			return prevLine is decimal p && candidate < p ? p : candidate;
		}

		if (candle.ClosePrice < lower)
		{
			var candidate = useAtr ? candle.HighPrice + atr : candle.HighPrice;
			return prevLine is decimal p && candidate > p ? p : candidate;
		}

		return prevLine;
	}

	private static int NextTrend(int trend, decimal? prevLine, decimal? line)
	{
		if (line is decimal current && prevLine is decimal previous)
		{
			if (current > previous)
				return 1;

			if (current < previous)
				return -1;
		}

		return trend;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var bollinger = new BollingerBands { Length = BbPeriod, Width = BbDeviation };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, atr, ProcessCandle)
			.Start();

		if (UseHtfConfirmation)
		{
			var htfBollinger = new BollingerBands { Length = BbPeriod, Width = BbDeviation };
			var htfAtr = new AverageTrueRange { Length = AtrPeriod };

			SubscribeCandles(HtfCandleType)
				.BindEx(htfBollinger, htfAtr, ProcessHtfCandle)
				.Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessHtfCandle(ICandleMessage candle, IIndicatorValue bbValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bbValue.IsFormed || !atrValue.IsFormed || bbValue is not IBollingerBandsValue { UpBand: decimal upper, LowBand: decimal lower })
			return;

		var line = NextLine(candle, _htfLine, upper, lower, atrValue.ToDecimal(), UseAtrFilter);
		_htfTrend = NextTrend(_htfTrend, _htfLine, line);
		_htfLine = line;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bbValue.IsFormed || !atrValue.IsFormed || bbValue is not IBollingerBandsValue { UpBand: decimal upper, LowBand: decimal lower })
			return;

		var line = NextLine(candle, _line, upper, lower, atrValue.ToDecimal(), UseAtrFilter);
		_prevTrend = _trend;
		_trend = NextTrend(_trend, _line, line);
		_line = line;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var htfTrend = _htfTrend;

		if (UseHtfConfirmation)
		{
			if (Position > 0 && htfTrend < 0)
			{
				SellMarket(Position);
				return;
			}

			if (Position < 0 && htfTrend > 0)
			{
				BuyMarket(-Position);
				return;
			}
		}

		var turnUp = _prevTrend == -1 && _trend == 1;
		var turnDown = _prevTrend == 1 && _trend == -1;

		if (!turnUp && !turnDown)
			return;

		var canEnter = !UseTimeFilter || InSession(candle.OpenTime);

		if (turnUp)
		{
			if (canEnter && (!UseHtfConfirmation || htfTrend > 0) && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (Position < 0)
				BuyMarket(-Position);
		}
		else
		{
			if (canEnter && (!UseHtfConfirmation || htfTrend < 0) && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			else if (Position > 0)
				SellMarket(Position);
		}
	}

	private bool InSession(DateTime time)
	{
		var parts = (Session ?? string.Empty).Split('-');
		if (parts.Length != 2 || !int.TryParse(parts[0], out var start) || !int.TryParse(parts[1], out var end))
			return true;

		var startMinutes = start / 100 * 60 + start % 100;
		var endMinutes = end / 100 * 60 + end % 100;
		var minutes = time.Hour * 60 + time.Minute;

		return startMinutes <= endMinutes
			? minutes >= startMinutes && minutes < endMinutes
			: minutes >= startMinutes || minutes < endMinutes;
	}
}
