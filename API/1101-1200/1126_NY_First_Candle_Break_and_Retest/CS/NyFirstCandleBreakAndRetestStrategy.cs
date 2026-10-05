using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// NY first candle break and retest strategy.
/// The candle opening at NyStartHour:NyStartMinute (UTC) defines the session range. During the next SessionLength hours a close
/// beyond its high or low by at least MinBreakSize ATR marks a breakout; a later candle that pulls back to within RetestThreshold
/// ATR of the broken level and closes beyond it enters in the breakout direction, optionally only on the matching side of the EMA.
/// The stop sits AtrMultiplier ATR from entry and the target RewardRiskRatio times that distance.
/// </summary>
public class NyFirstCandleBreakAndRetestStrategy : Strategy
{
	private readonly StrategyParam<int> _nyStartHour;
	private readonly StrategyParam<int> _nyStartMinute;
	private readonly StrategyParam<int> _sessionLength;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _rewardRiskRatio;
	private readonly StrategyParam<decimal> _minBreakSize;
	private readonly StrategyParam<decimal> _retestThreshold;
	private readonly StrategyParam<bool> _useEmaFilter;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentDay;
	private decimal? _rangeHigh;
	private decimal? _rangeLow;
	private bool _brokeUp;
	private bool _brokeDown;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Session start hour (UTC).
	/// </summary>
	public int NyStartHour
	{
		get => _nyStartHour.Value;
		set => _nyStartHour.Value = value;
	}

	/// <summary>
	/// Session start minute.
	/// </summary>
	public int NyStartMinute
	{
		get => _nyStartMinute.Value;
		set => _nyStartMinute.Value = value;
	}

	/// <summary>
	/// Session length in hours.
	/// </summary>
	public int SessionLength
	{
		get => _sessionLength.Value;
		set => _sessionLength.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Stop distance in ATR.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Target distance as a multiple of the stop distance.
	/// </summary>
	public decimal RewardRiskRatio
	{
		get => _rewardRiskRatio.Value;
		set => _rewardRiskRatio.Value = value;
	}

	/// <summary>
	/// Minimum breakout beyond the first candle in ATR.
	/// </summary>
	public decimal MinBreakSize
	{
		get => _minBreakSize.Value;
		set => _minBreakSize.Value = value;
	}

	/// <summary>
	/// Maximum retest distance from the broken level in ATR.
	/// </summary>
	public decimal RetestThreshold
	{
		get => _retestThreshold.Value;
		set => _retestThreshold.Value = value;
	}

	/// <summary>
	/// Require the close on the trade side of the EMA.
	/// </summary>
	public bool UseEmaFilter
	{
		get => _useEmaFilter.Value;
		set => _useEmaFilter.Value = value;
	}

	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
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
	public NyFirstCandleBreakAndRetestStrategy()
	{
		_nyStartHour = Param(nameof(NyStartHour), 9)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Session start hour (UTC)", "Session");

		_nyStartMinute = Param(nameof(NyStartMinute), 30)
			.SetRange(0, 59)
			.SetDisplay("Start Minute", "Session start minute", "Session");

		_sessionLength = Param(nameof(SessionLength), 4)
			.SetGreaterThanZero()
			.SetDisplay("Session Length", "Session length in hours", "Session");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Stop distance in ATR", "Risk");

		_rewardRiskRatio = Param(nameof(RewardRiskRatio), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Reward/Risk", "Target distance as a multiple of the stop distance", "Risk");

		_minBreakSize = Param(nameof(MinBreakSize), 0.15m)
			.SetNotNegative()
			.SetDisplay("Min Break Size", "Minimum breakout beyond the first candle in ATR", "Entry");

		_retestThreshold = Param(nameof(RetestThreshold), 0.25m)
			.SetNotNegative()
			.SetDisplay("Retest Threshold", "Maximum retest distance from the broken level in ATR", "Entry");

		_useEmaFilter = Param(nameof(UseEmaFilter), true)
			.SetDisplay("Use EMA Filter", "Require the close on the trade side of the EMA", "Entry");

		_emaLength = Param(nameof(EmaLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Entry");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		ResetState();
	}

	private void ResetState()
	{
		_currentDay = default;
		_rangeHigh = null;
		_rangeLow = null;
		_brokeUp = false;
		_brokeDown = false;
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var atr = new AverageTrueRange { Length = AtrPeriod };
		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var openTime = candle.OpenTime;
		var day = openTime.Date;

		if (day != _currentDay)
		{
			_currentDay = day;
			_rangeHigh = null;
			_rangeLow = null;
			_brokeUp = false;
			_brokeDown = false;
		}

		var sessionStart = day + new TimeSpan(NyStartHour, NyStartMinute, 0);
		var sessionEnd = sessionStart + TimeSpan.FromHours(SessionLength);

		if (openTime == sessionStart)
		{
			_rangeHigh = candle.HighPrice;
			_rangeLow = candle.LowPrice;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (Position > 0 && _stopPrice is decimal longStop && _takePrice is decimal longTake)
		{
			if (candle.LowPrice <= longStop || candle.HighPrice >= longTake)
			{
				SellMarket(Position);
				_stopPrice = null;
				_takePrice = null;
				return;
			}
		}
		else if (Position < 0 && _stopPrice is decimal shortStop && _takePrice is decimal shortTake)
		{
			if (candle.HighPrice >= shortStop || candle.LowPrice <= shortTake)
			{
				BuyMarket(-Position);
				_stopPrice = null;
				_takePrice = null;
				return;
			}
		}

		if (openTime <= sessionStart || openTime >= sessionEnd || _rangeHigh is not decimal high || _rangeLow is not decimal low)
			return;

		var stopDistance = atr * AtrMultiplier;

		if (_brokeUp && Position <= 0 && candle.LowPrice <= high + RetestThreshold * atr && close > high && (!UseEmaFilter || close > ema))
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - stopDistance;
			_takePrice = close + stopDistance * RewardRiskRatio;
			_brokeUp = false;
			return;
		}

		if (_brokeDown && Position >= 0 && candle.HighPrice >= low - RetestThreshold * atr && close < low && (!UseEmaFilter || close < ema))
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + stopDistance;
			_takePrice = close - stopDistance * RewardRiskRatio;
			_brokeDown = false;
			return;
		}

		// A breakout candle only arms the setup; the retest has to come on a later candle.
		if (close > high + MinBreakSize * atr)
			_brokeUp = true;
		else if (close < low - MinBreakSize * atr)
			_brokeDown = true;
	}
}
