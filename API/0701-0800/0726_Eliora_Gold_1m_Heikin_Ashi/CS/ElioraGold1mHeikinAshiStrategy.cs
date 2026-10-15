using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Eliora Gold 1m Heikin Ashi strategy.
/// Heikin Ashi candles are built from the one-minute candles. A strong bullish Heikin Ashi candle (body at least
/// 60% of its range) closing above the trend SMA goes long and a strong bearish one closing below it goes short, when
/// the market is not consolidating (the recent high-low range is wider than a multiple of ATR), volatility is
/// expanding (ATR above its own average) and at least CooldownBars candles passed since the last trade.
/// Positions are exited only by an ATR trailing stop.
/// </summary>
public class ElioraGold1mHeikinAshiStrategy : Strategy
{
	private const int _trendLength = 50;
	private const int _rangeLength = 20;
	private const decimal _strongBodyRatio = 0.6m;
	private const decimal _consolidationAtrMultiplier = 2m;
	private const decimal _trailAtrMultiplier = 2m;

	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private SimpleMovingAverage _atrAverage;

	private decimal? _haOpen;
	private decimal? _haClose;
	private decimal? _trailStop;
	private int _barsSinceTrade;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Candles to wait after a trade before a new entry.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
	public ElioraGold1mHeikinAshiStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for filters and the trailing stop", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 5)
			.SetNotNegative()
			.SetDisplay("Cooldown Bars", "Candles to wait after a trade", "Trading");

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
		ResetState();
	}

	private void ResetState()
	{
		_haOpen = null;
		_haClose = null;
		_trailStop = null;
		_barsSinceTrade = int.MaxValue;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var atr = new AverageTrueRange { Length = AtrPeriod };
		var trend = new SimpleMovingAverage { Length = _trendLength };
		_highest = new Highest { Length = _rangeLength };
		_lowest = new Lowest { Length = _rangeLength };
		_atrAverage = new SimpleMovingAverage { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, trend, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, trend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr, decimal trend)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		var haOpen = _haOpen is decimal po && _haClose is decimal pc ? (po + pc) / 2m : (candle.OpenPrice + candle.ClosePrice) / 2m;
		var haHigh = Math.Max(candle.HighPrice, Math.Max(haOpen, haClose));
		var haLow = Math.Min(candle.LowPrice, Math.Min(haOpen, haClose));
		_haOpen = haOpen;
		_haClose = haClose;

		var highValue = _highest.Process(new DecimalIndicatorValue(_highest, candle.HighPrice, candle.OpenTime) { IsFinal = true });
		var lowValue = _lowest.Process(new DecimalIndicatorValue(_lowest, candle.LowPrice, candle.OpenTime) { IsFinal = true });
		var atrAvgValue = _atrAverage.Process(new DecimalIndicatorValue(_atrAverage, atr, candle.OpenTime) { IsFinal = true });

		if (_barsSinceTrade < int.MaxValue)
			_barsSinceTrade++;

		if (!highValue.IsFormed || !lowValue.IsFormed || !atrAvgValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var trailDistance = atr * _trailAtrMultiplier;

		if (Position > 0)
		{
			var candidate = candle.ClosePrice - trailDistance;
			_trailStop = _trailStop is decimal s ? Math.Max(s, candidate) : candidate;

			if (candle.LowPrice <= _trailStop)
			{
				SellMarket(Position);
				_trailStop = null;
				_barsSinceTrade = 0;
			}

			return;
		}

		if (Position < 0)
		{
			var candidate = candle.ClosePrice + trailDistance;
			_trailStop = _trailStop is decimal s ? Math.Min(s, candidate) : candidate;

			if (candle.HighPrice >= _trailStop)
			{
				BuyMarket(-Position);
				_trailStop = null;
				_barsSinceTrade = 0;
			}

			return;
		}

		if (_barsSinceTrade < CooldownBars)
			return;

		var range = highValue.GetValue<decimal>() - lowValue.GetValue<decimal>();
		var consolidating = range < atr * _consolidationAtrMultiplier;
		var expanding = atr > atrAvgValue.GetValue<decimal>();

		if (consolidating || !expanding)
			return;

		var haRange = haHigh - haLow;
		var body = Math.Abs(haClose - haOpen);
		var strong = haRange > 0 && body >= haRange * _strongBodyRatio;

		if (!strong)
			return;

		if (haClose > haOpen && haClose > trend)
		{
			BuyMarket(Volume);
			_trailStop = candle.ClosePrice - trailDistance;
			_barsSinceTrade = 0;
		}
		else if (haClose < haOpen && haClose < trend)
		{
			SellMarket(Volume);
			_trailStop = candle.ClosePrice + trailDistance;
			_barsSinceTrade = 0;
		}
	}
}
