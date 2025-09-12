using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining Bollinger Bands breakout with Heikin Ashi confirmation.
/// </summary>
public class SunilBbBlastHeikinAshiStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TradeDirection> _direction;
	private readonly StrategyParam<TimeSpan> _sessionBegin;
	private readonly StrategyParam<TimeSpan> _sessionEnd;

	private decimal? _haOpenPrev;
	private decimal? _haClosePrev;
	private decimal? _prevOpen;
	private decimal? _prevClose;
	private decimal _stop;
	private decimal _target;

	/// <summary>
	/// Allowed trade direction.
	/// </summary>
	public enum TradeDirection
	{
		LongOnly,
		ShortOnly,
		Both
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }

	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BollingerMultiplier { get => _bollingerMultiplier.Value; set => _bollingerMultiplier.Value = value; }

	/// <summary>
	/// Risk to reward ratio.
	/// </summary>
	public decimal RiskRewardRatio { get => _riskReward.Value; set => _riskReward.Value = value; }

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Trade direction filter.
	/// </summary>
	public TradeDirection Direction { get => _direction.Value; set => _direction.Value = value; }

	/// <summary>
	/// Trading window start time.
	/// </summary>
	public TimeSpan SessionBegin { get => _sessionBegin.Value; set => _sessionBegin.Value = value; }

	/// <summary>
	/// Trading window end time.
	/// </summary>
	public TimeSpan SessionEnd { get => _sessionEnd.Value; set => _sessionEnd.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="SunilBbBlastHeikinAshiStrategy"/>.
	/// </summary>
	public SunilBbBlastHeikinAshiStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 19)
			.SetRange(10, 50)
			.SetDisplay("Bollinger Period", "Length for Bollinger Bands", "Indicators")
			.SetCanOptimize(true);

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2m)
			.SetRange(1m, 3m)
			.SetDisplay("Bollinger Multiplier", "Deviation multiplier for Bollinger Bands", "Indicators")
			.SetCanOptimize(true);

		_riskReward = Param(nameof(RiskRewardRatio), 1m)
			.SetRange(0.5m, 3m)
			.SetDisplay("Risk/Reward", "Risk to reward ratio", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_direction = Param(nameof(Direction), TradeDirection.Both)
			.SetDisplay("Trade Direction", "Allowed trade direction", "General");

		_sessionBegin = Param(nameof(SessionBegin), new TimeSpan(9, 20, 0))
			.SetDisplay("Session Begin", "Trading window start", "General");

		_sessionEnd = Param(nameof(SessionEnd), new TimeSpan(15, 0, 0))
			.SetDisplay("Session End", "Trading window end", "General");
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
		_haOpenPrev = null;
		_haClosePrev = null;
		_prevOpen = null;
		_prevClose = null;
		_stop = 0m;
		_target = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerMultiplier
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime.LocalDateTime.TimeOfDay;
		if (time < SessionBegin || time > SessionEnd)
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(-Position);

			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.UpBand is not decimal upperBand || bb.LowBand is not decimal lowerBand)
			return;

		var prevHaOpen = _haOpenPrev;
		var prevHaClose = _haClosePrev;
		var prevOpen = _prevOpen;
		var prevClose = _prevClose;

		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		var haOpen = (prevHaOpen is null || prevHaClose is null)
			? (candle.OpenPrice + candle.ClosePrice) / 2m
			: (prevHaOpen.Value + prevHaClose.Value) / 2m;

		_haOpenPrev = haOpen;
		_haClosePrev = haClose;
		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;

		if (prevHaOpen is null || prevHaClose is null || prevOpen is null || prevClose is null)
			return;

		var allowLong = Direction != TradeDirection.ShortOnly;
		var allowShort = Direction != TradeDirection.LongOnly;

		if (Position <= 0 && allowLong && prevHaClose > prevHaOpen && prevClose > prevOpen && candle.ClosePrice > upperBand)
		{
			_stop = lowerBand;
			_target = candle.ClosePrice + (candle.ClosePrice - _stop) * RiskRewardRatio;
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (Position >= 0 && allowShort && prevHaClose < prevHaOpen && prevClose < prevOpen && candle.ClosePrice < lowerBand)
		{
			_stop = upperBand;
			_target = candle.ClosePrice - (_stop - candle.ClosePrice) * RiskRewardRatio;
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}

		if (Position > 0 && (candle.ClosePrice <= _stop || candle.ClosePrice >= _target))
		{
			SellMarket(Position);
		}
		else if (Position < 0 && (candle.ClosePrice >= _stop || candle.ClosePrice <= _target))
		{
			BuyMarket(-Position);
		}
	}
}
