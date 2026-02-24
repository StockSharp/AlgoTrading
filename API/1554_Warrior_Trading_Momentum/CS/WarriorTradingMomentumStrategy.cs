using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Warrior Trading inspired momentum strategy.
/// Detects red-to-green reversals and volume spikes with EMA trend filter.
/// Uses StdDev-based stops with risk/reward targeting.
/// </summary>
public class WarriorTradingMomentumStrategy : Strategy
{
	private readonly StrategyParam<int> _minRedCandles;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<int> _maxDailyTrades;
	private readonly StrategyParam<int> _volAvgLength;
	private readonly StrategyParam<decimal> _volMult;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _volumes = new();
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private int _redCount;
	private DateTime _currentDay;
	private int _dailyTrades;

	public int MinRedCandles { get => _minRedCandles.Value; set => _minRedCandles.Value = value; }
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }
	public int MaxDailyTrades { get => _maxDailyTrades.Value; set => _maxDailyTrades.Value = value; }
	public int VolAvgLength { get => _volAvgLength.Value; set => _volAvgLength.Value = value; }
	public decimal VolMult { get => _volMult.Value; set => _volMult.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public WarriorTradingMomentumStrategy()
	{
		_minRedCandles = Param(nameof(MinRedCandles), 2)
			.SetGreaterThanZero()
			.SetDisplay("Min Red", "Red candles before reversal", "Momentum");

		_riskReward = Param(nameof(RiskReward), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Reward", "TP to SL ratio", "Risk");

		_maxDailyTrades = Param(nameof(MaxDailyTrades), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Daily trade limit", "Risk");

		_volAvgLength = Param(nameof(VolAvgLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Vol Avg Length", "Volume average period", "Parameters");

		_volMult = Param(nameof(VolMult), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Vol Mult", "Volume spike multiplier", "Parameters");

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
		_volumes.Clear();
		_stopPrice = 0;
		_takeProfitPrice = 0;
		_redCount = 0;
		_currentDay = default;
		_dailyTrades = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = 20 };
		var rsi = new RelativeStrengthIndex { Length = 14 };
		var stdDev = new StandardDeviation { Length = 14 };

		_volumes.Clear();
		_stopPrice = 0;
		_takeProfitPrice = 0;
		_redCount = 0;
		_currentDay = default;
		_dailyTrades = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, rsi, stdDev, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal, decimal rsiVal, decimal stdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.Date;
		if (day != _currentDay)
		{
			_currentDay = day;
			_dailyTrades = 0;
		}

		// Track volume
		_volumes.Add(candle.TotalVolume);
		while (_volumes.Count > VolAvgLength + 1)
			_volumes.RemoveAt(0);

		// TP/SL exit
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takeProfitPrice)
			{
				SellMarket();
				_stopPrice = 0;
				_takeProfitPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takeProfitPrice)
			{
				BuyMarket();
				_stopPrice = 0;
				_takeProfitPrice = 0;
			}
		}

		// Red candle tracking
		if (candle.ClosePrice < candle.OpenPrice)
			_redCount++;
		else
			_redCount = 0;

		if (stdVal <= 0 || _volumes.Count < VolAvgLength || _dailyTrades >= MaxDailyTrades)
			return;

		var volAvg = _volumes.Take(VolAvgLength).Sum() / VolAvgLength;
		var volumeSpike = candle.TotalVolume > volAvg * VolMult;
		var bullish = candle.ClosePrice > candle.OpenPrice;

		// Red-to-green reversal with volume spike
		var redToGreen = _redCount >= MinRedCandles && bullish && volumeSpike;

		// Momentum buy: price above EMA with RSI confirmation
		var momentumBuy = bullish && candle.ClosePrice > emaVal && rsiVal > 50 && volumeSpike;

		if ((redToGreen || momentumBuy) && Position <= 0)
		{
			BuyMarket();
			var stopDist = stdVal * 2m;
			_stopPrice = candle.ClosePrice - stopDist;
			_takeProfitPrice = candle.ClosePrice + stopDist * RiskReward;
			_dailyTrades++;
		}
		else if (candle.ClosePrice < emaVal && rsiVal < 50 && volumeSpike && Position >= 0)
		{
			SellMarket();
			var stopDist = stdVal * 2m;
			_stopPrice = candle.ClosePrice + stopDist;
			_takeProfitPrice = candle.ClosePrice - stopDist * RiskReward;
			_dailyTrades++;
		}
	}
}
