using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// HSI1 breakout of first 30-minute range on a 15-minute chart.
/// </summary>
public class Hsi1First30mCandleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TradeDirection> _tradeDirection;

	private decimal? _firstHigh;
	private decimal? _firstLow;
	private bool _rangeLocked;
	private bool _tradedToday;
	private int _currentYmd;
	private decimal _stopPrice;
	private decimal _takePrice;
	private readonly TimeZoneInfo _tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Hong_Kong");

	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public TradeDirection Direction
	{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
	}

	public Hsi1First30mCandleStrategy()
	{
		_riskReward = Param(nameof(RiskReward), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Reward", "Reward to risk ratio", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_tradeDirection = Param(nameof(Direction), TradeDirection.Both)
			.SetDisplay("Trade Direction", "Allowed direction", "Parameters");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_firstHigh = null;
		_firstLow = null;
		_rangeLocked = false;
		_tradedToday = false;
		_currentYmd = 0;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sub = SubscribeCandles(CandleType);
		sub.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var local = TimeZoneInfo.ConvertTime(candle.OpenTime, _tz);
		var ymd = local.Year * 10000 + local.Month * 100 + local.Day;

		if (_currentYmd != ymd)
		{
			_currentYmd = ymd;
			_firstHigh = null;
			_firstLow = null;
			_rangeLocked = false;
			_tradedToday = false;
		}

		var tod = local.TimeOfDay;
		var inSession =
			(tod >= new TimeSpan(9, 15, 0) && tod <= new TimeSpan(12, 0, 0)) ||
			(tod >= new TimeSpan(13, 0, 0) && tod <= new TimeSpan(16, 0, 0));

		var start30 = new TimeSpan(9, 15, 0);
		var end30 = new TimeSpan(9, 45, 0);

		if (tod >= start30 && tod < end30)
		{
			_firstHigh = _firstHigh is null ? candle.HighPrice : Math.Max(_firstHigh.Value, candle.HighPrice);
			_firstLow = _firstLow is null ? candle.LowPrice : Math.Min(_firstLow.Value, candle.LowPrice);
		}

		if (!_rangeLocked && tod >= end30 && _firstHigh.HasValue && _firstLow.HasValue)
			_rangeLocked = true;

		var canTrade = inSession && _rangeLocked && !_tradedToday;

		if (Position == 0 && canTrade && _firstHigh.HasValue && _firstLow.HasValue)
		{
			var range = _firstHigh.Value - _firstLow.Value;

			if (candle.HighPrice >= _firstHigh && Direction != TradeDirection.SellOnly)
			{
				BuyMarket(Volume);
				_stopPrice = _firstLow.Value;
				_takePrice = _firstHigh.Value + range * RiskReward;
				_tradedToday = true;
			}
			else if (candle.LowPrice <= _firstLow && Direction != TradeDirection.BuyOnly)
			{
				SellMarket(Volume);
				_stopPrice = _firstHigh.Value;
				_takePrice = _firstLow.Value - range * RiskReward;
				_tradedToday = true;
			}
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
				BuyMarket(-Position);
		}
	}
}
