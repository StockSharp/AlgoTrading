namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Follow Line strategy with optional higher timeframe confirmation.
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

	private decimal? _followLine;
	private decimal? _prevFollowLine;
	private int _trend;
	private int _prevTrend;

	private decimal? _followLineHtf;
	private decimal? _prevFollowLineHtf;
	private int _trendHtf;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BbPeriod { get => _bbPeriod.Value; set => _bbPeriod.Value = value; }

	/// <summary>
	/// Bollinger Bands deviation.
	/// </summary>
	public decimal BbDeviation { get => _bbDeviation.Value; set => _bbDeviation.Value = value; }

	/// <summary>
	/// Use ATR offset for follow line.
	/// </summary>
	public bool UseAtrFilter { get => _useAtrFilter.Value; set => _useAtrFilter.Value = value; }

	/// <summary>
	/// Enable trading session filter.
	/// </summary>
	public bool UseTimeFilter { get => _useTimeFilter.Value; set => _useTimeFilter.Value = value; }

	/// <summary>
	/// Trading session in HHmm-HHmm format.
	/// </summary>
	public string Session { get => _session.Value; set => _session.Value = value; }

	/// <summary>
	/// Enable higher timeframe confirmation.
	/// </summary>
	public bool UseHtfConfirmation { get => _useHtfConfirmation.Value; set => _useHtfConfirmation.Value = value; }

	/// <summary>
	/// Candle type for main timeframe.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Candle type for higher timeframe.
	/// </summary>
	public DataType HtfCandleType { get => _htfCandleType.Value; set => _htfCandleType.Value = value; }

	/// <summary>
	/// Initialize <see cref="FollowLineStrategy"/>.
	/// </summary>
	public FollowLineStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR calculation period", "Indicators");

		_bbPeriod = Param(nameof(BbPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");

		_bbDeviation = Param(nameof(BbDeviation), 1m)
			.SetRange(0.1m, 5m)
			.SetDisplay("BB Deviation", "Bollinger Bands deviation", "Indicators");

		_useAtrFilter = Param(nameof(UseAtrFilter), true)
			.SetDisplay("Use ATR Offset", "Apply ATR offset to follow line", "Indicators");

		_useTimeFilter = Param(nameof(UseTimeFilter), false)
			.SetDisplay("Use Session", "Enable trading session filter", "Time");

		_session = Param(nameof(Session), "0000-2400")
			.SetDisplay("Session", "Trading session", "Time");

		_useHtfConfirmation = Param(nameof(UseHtfConfirmation), false)
			.SetDisplay("Use HTF", "Require higher timeframe confirmation", "Trend");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Main timeframe", "General");

		_htfCandleType = Param(nameof(HtfCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("HTF Candle Type", "Higher timeframe", "Trend");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		if (UseHtfConfirmation)
			yield return (Security, HtfCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_followLine = _prevFollowLine = null;
		_trend = _prevTrend = 0;
		_followLineHtf = _prevFollowLineHtf = null;
		_trendHtf = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bb = new BollingerBands { Length = BbPeriod, Width = BbDeviation };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var sub = SubscribeCandles(CandleType);
		sub.Bind(bb, atr, ProcessTrade).Start();

		if (UseHtfConfirmation)
		{
			var bbHtf = new BollingerBands { Length = BbPeriod, Width = BbDeviation };
			var atrHtf = new AverageTrueRange { Length = AtrPeriod };
			SubscribeCandles(HtfCandleType)
				.Bind(bbHtf, atrHtf, ProcessHtf)
				.Start();
		}

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawOwnTrades(area);
		}
	}

	private void ProcessTrade(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (UseTimeFilter && !InSession(candle.CloseTime))
			return;

		var bbSignal = 0;
		if (candle.ClosePrice > upper)
			bbSignal = 1;
		else if (candle.ClosePrice < lower)
			bbSignal = -1;

		if (bbSignal != 0)
		{
			var temp = bbSignal == 1
				? (UseAtrFilter ? candle.LowPrice - atr : candle.LowPrice)
				: (UseAtrFilter ? candle.HighPrice + atr : candle.HighPrice);

			if (_followLine is null)
				_followLine = temp;
			else if (bbSignal == 1)
				_followLine = Math.Max(temp, _followLine.Value);
			else
				_followLine = Math.Min(temp, _followLine.Value);
		}

		_trend = 0;
		if (_followLine.HasValue && _prevFollowLine.HasValue)
		{
			if (_followLine > _prevFollowLine)
				_trend = 1;
			else if (_followLine < _prevFollowLine)
				_trend = -1;
			else
				_trend = _prevTrend;
		}
		else if (_followLine.HasValue && !_prevFollowLine.HasValue)
		{
			_trend = bbSignal;
		}
		else
		{
			_trend = 0;
		}

		var longCondition = _prevTrend <= 0 && _trend == 1;
		var shortCondition = _prevTrend >= 0 && _trend == -1;

		var htfLong = !UseHtfConfirmation || _trendHtf == 1;
		var htfShort = !UseHtfConfirmation || _trendHtf == -1;

		if (longCondition && htfLong && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortCondition && htfShort && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		if (Position > 0 && (_trend == -1 || _trendHtf == -1))
			SellMarket(Position);
		else if (Position < 0 && (_trend == 1 || _trendHtf == 1))
			BuyMarket(Math.Abs(Position));

		_prevTrend = _trend;
		_prevFollowLine = _followLine;
	}

	private void ProcessHtf(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bbSignal = 0;
		if (candle.ClosePrice > upper)
			bbSignal = 1;
		else if (candle.ClosePrice < lower)
			bbSignal = -1;

		if (bbSignal != 0)
		{
			var temp = bbSignal == 1
				? (UseAtrFilter ? candle.LowPrice - atr : candle.LowPrice)
				: (UseAtrFilter ? candle.HighPrice + atr : candle.HighPrice);

			if (_followLineHtf is null)
				_followLineHtf = temp;
			else if (bbSignal == 1)
				_followLineHtf = Math.Max(temp, _followLineHtf.Value);
			else
				_followLineHtf = Math.Min(temp, _followLineHtf.Value);
		}

		if (_followLineHtf.HasValue && _prevFollowLineHtf.HasValue)
		{
			if (_followLineHtf > _prevFollowLineHtf)
				_trendHtf = 1;
			else if (_followLineHtf < _prevFollowLineHtf)
				_trendHtf = -1;
			else
				_trendHtf = _trendHtf;
		}
		else if (_followLineHtf.HasValue && !_prevFollowLineHtf.HasValue)
		{
			_trendHtf = bbSignal;
		}
		else
		{
			_trendHtf = 0;
		}

		_prevFollowLineHtf = _followLineHtf;
	}

	private bool InSession(DateTimeOffset time)
	{
		ParseSession(Session, out var start, out var end);
		var t = time.TimeOfDay;
		return start <= end ? t >= start && t <= end : t >= start || t <= end;
	}

	private static void ParseSession(string input, out TimeSpan start, out TimeSpan end)
	{
		start = TimeSpan.Zero;
		end = TimeSpan.FromHours(24);
		if (string.IsNullOrWhiteSpace(input))
			return;

		var parts = input.Split('-', ':');
		if (parts.Length < 2)
			return;

		TimeSpan.TryParseExact(parts[0], "hhmm", null, out start);
		TimeSpan.TryParseExact(parts[1], "hhmm", null, out end);
	}
}
