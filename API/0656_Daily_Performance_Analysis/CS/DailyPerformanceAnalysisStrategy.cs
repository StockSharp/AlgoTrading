using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class DailyPerformanceAnalysisStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<DataType> _candleType;
	private int _prevDir;
	private decimal _entryPrice;
	private DateTimeOffset _entryTime;
	private bool _isLong;
	private readonly DayStats[] _week = new DayStats[7];
	private readonly DayStats[] _month = new DayStats[31];

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal Factor { get => _factor.Value; set => _factor.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public DailyPerformanceAnalysisStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 10).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period for SuperTrend", "Parameters");
		_factor = Param(nameof(Factor), 3m).SetGreaterThanZero().SetDisplay("Factor", "SuperTrend ATR multiplier", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type", "Type of candles to use", "General");
		for (var i = 0; i < _week.Length; i++) _week[i] = new DayStats();
		for (var i = 0; i < _month.Length; i++) _month[i] = new DayStats();
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevDir = 0;
		_entryPrice = 0;
		_entryTime = default;
		_isLong = false;
		foreach (var s in _week) s.Reset();
		foreach (var s in _month) s.Reset();
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		var st = new SuperTrend { Length = AtrPeriod, Multiplier = Factor };
		var sub = SubscribeCandles(CandleType);
		sub.BindEx(st, Process).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, st);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading())
			return;
		var st = (SuperTrendIndicatorValue)value;
		var dir = st.IsUpTrend ? 1 : -1;
		if (_prevDir != 0 && dir != _prevDir)
		{
			var price = candle.ClosePrice;
			if (_entryPrice != 0 && Position != 0)
			{
				var p = _isLong ? (price - _entryPrice) : (_entryPrice - price);
				UpdateStats(p * Math.Abs(Position), _entryTime);
			}
			if (dir - _prevDir < 0)
			{
				if (Position <= 0)
				{
					_entryPrice = price;
					_entryTime = candle.OpenTime;
					_isLong = true;
					BuyMarket(Volume + Math.Abs(Position));
				}
			}
			else if (Position >= 0)
			{
				_entryPrice = price;
				_entryTime = candle.OpenTime;
				_isLong = false;
				SellMarket(Volume + Math.Abs(Position));
			}
		}
		_prevDir = dir;
	}

	private void UpdateStats(decimal profit, DateTimeOffset time)
	{
		_week[(int)time.DayOfWeek].Update(profit);
		var d = time.Day;
		if (d >= 1 && d <= 31) _month[d - 1].Update(profit);
	}

	private class DayStats
	{
		public int Wins;
		public int Losses;
		public decimal GrossProfit;
		public decimal GrossLoss;
		public void Update(decimal p)
		{
			if (p > 0) { Wins++; GrossProfit += p; } else { Losses++; GrossLoss += Math.Abs(p); }
		}
		public void Reset()
		{
			Wins = 0;
			Losses = 0;
			GrossProfit = 0;
			GrossLoss = 0;
		}
		public decimal NetProfit => GrossProfit - GrossLoss;
		public decimal ProfitFactor => GrossLoss > 0 ? GrossProfit / GrossLoss : 0m;
		public decimal WinRate => (Wins + Losses) > 0 ? (decimal)Wins / (Wins + Losses) * 100m : 0m;
	}
}
