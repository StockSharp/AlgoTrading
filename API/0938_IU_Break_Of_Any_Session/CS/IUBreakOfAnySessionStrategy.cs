using System;
using System.Collections.Generic;

using Ecng.Common;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout from custom session high or low.
/// Enters once per day when price breaks session range.
/// </summary>
public class IUBreakOfAnySessionStrategy : Strategy
{
	private readonly StrategyParam<int> _sessionBars;
	private readonly StrategyParam<decimal> _profitFactor;
	private readonly StrategyParam<int> _maxEntries;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _sessionHigh;
	private decimal _sessionLow;
	private int _barCount;
	private int _entriesExecuted;
	private int _cooldown;
	private decimal _stopPrice;
	private decimal _targetPrice;

	public int SessionBars { get => _sessionBars.Value; set => _sessionBars.Value = value; }
	public decimal ProfitFactor { get => _profitFactor.Value; set => _profitFactor.Value = value; }
	public int MaxEntries { get => _maxEntries.Value; set => _maxEntries.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public IUBreakOfAnySessionStrategy()
	{
		_sessionBars = Param(nameof(SessionBars), 48)
			.SetGreaterThanZero()
			.SetDisplay("Session Bars", "Number of bars to form session range", "Session")
			.SetOptimize(24, 96, 24);

		_profitFactor = Param(nameof(ProfitFactor), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Factor", "Risk to reward ratio", "Risk")
			.SetOptimize(1m, 4m, 1m);

		_maxEntries = Param(nameof(MaxEntries), 45)
			.SetDisplay("Max Entries", "Maximum number of entries per test", "Trading");

		_cooldownBars = Param(nameof(CooldownBars), 50)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Minimum bars between entries", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_sessionHigh = 0m;
		_sessionLow = 0m;
		_barCount = 0;
		_entriesExecuted = 0;
		_cooldown = 0;
		_stopPrice = 0m;
		_targetPrice = 0m;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var dummyEma1 = new StockSharp.Algo.Indicators.ExponentialMovingAverage { Length = 10 };
		var dummyEma2 = new StockSharp.Algo.Indicators.ExponentialMovingAverage { Length = 20 };
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(dummyEma1, dummyEma2, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal d1, decimal d2)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barCount++;
		_cooldown++;

		if (_barCount <= SessionBars)
		{
			if (_sessionLow == 0m)
			{
				_sessionHigh = candle.HighPrice;
				_sessionLow = candle.LowPrice;
			}
			else
			{
				_sessionHigh = Math.Max(_sessionHigh, candle.HighPrice);
				_sessionLow = Math.Min(_sessionLow, candle.LowPrice);
			}
			return;
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _targetPrice)
			{
				SellMarket();
				_sessionHigh = candle.HighPrice;
				_sessionLow = candle.LowPrice;
				_barCount = 1;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _targetPrice)
			{
				BuyMarket();
				_sessionHigh = candle.HighPrice;
				_sessionLow = candle.LowPrice;
				_barCount = 1;
			}
		}
		else if (_entriesExecuted < MaxEntries && _cooldown >= CooldownBars)
		{
			if (candle.ClosePrice > _sessionHigh)
			{
				_stopPrice = _sessionLow;
				var risk = candle.ClosePrice - _stopPrice;
				_targetPrice = candle.ClosePrice + risk * ProfitFactor;
				BuyMarket();
				_entriesExecuted++;
				_cooldown = 0;
			}
			else if (candle.ClosePrice < _sessionLow)
			{
				_stopPrice = _sessionHigh;
				var risk = _stopPrice - candle.ClosePrice;
				_targetPrice = candle.ClosePrice - risk * ProfitFactor;
				SellMarket();
				_entriesExecuted++;
				_cooldown = 0;
			}
			else
			{
				_sessionHigh = Math.Max(_sessionHigh, candle.HighPrice);
				_sessionLow = Math.Min(_sessionLow, candle.LowPrice);
			}
		}
		else
		{
			_sessionHigh = Math.Max(_sessionHigh, candle.HighPrice);
			_sessionLow = Math.Min(_sessionLow, candle.LowPrice);
		}
	}
}
