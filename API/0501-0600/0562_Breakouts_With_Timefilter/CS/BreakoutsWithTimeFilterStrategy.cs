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
/// Breakout strategy with ATR-based stop and take profit.
/// </summary>
public class BreakoutsWithTimeFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _riskReward;

	private AverageTrueRange _atr;
	private decimal _stopLevel;
	private decimal _targetLevel;
	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();

	public int Length { get => _length.Value; set => _length.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }

	public BreakoutsWithTimeFilterStrategy()
	{
		_length = Param(nameof(Length), 20)
			.SetDisplay("Length", "Lookback period for breakout levels", "General")
			.SetOptimize(5, 50, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetDisplay("ATR Multiplier", "Multiplier for ATR stop", "Risk Management");

		_riskReward = Param(nameof(RiskReward), 2m)
			.SetDisplay("Risk Reward", "Risk to reward ratio", "Risk Management");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_stopLevel = 0m;
		_targetLevel = 0m;
		_highs.Clear();
		_lows.Clear();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_atr = new AverageTrueRange { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Track highs/lows history manually
		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		if (_highs.Count > Length + 1)
			_highs.RemoveAt(0);
		if (_lows.Count > Length + 1)
			_lows.RemoveAt(0);

		if (_highs.Count <= Length)
			return;

		// Breakout levels from previous N candles (excluding current)
		var prevHighest = decimal.MinValue;
		var prevLowest = decimal.MaxValue;
		for (var i = 0; i < _highs.Count - 1; i++)
		{
			if (_highs[i] > prevHighest) prevHighest = _highs[i];
			if (_lows[i] < prevLowest) prevLowest = _lows[i];
		}

		if (Position == 0)
		{
			if (candle.ClosePrice > prevHighest)
			{
				_stopLevel = candle.ClosePrice - atrValue * AtrMultiplier;
				var stopDistance = candle.ClosePrice - _stopLevel;
				_targetLevel = candle.ClosePrice + RiskReward * stopDistance;
				BuyMarket();
			}
			else if (candle.ClosePrice < prevLowest)
			{
				_stopLevel = candle.ClosePrice + atrValue * AtrMultiplier;
				var stopDistance = _stopLevel - candle.ClosePrice;
				_targetLevel = candle.ClosePrice - RiskReward * stopDistance;
				SellMarket();
			}
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= _stopLevel || candle.HighPrice >= _targetLevel)
				SellMarket();
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopLevel || candle.LowPrice <= _targetLevel)
				BuyMarket();
		}
	}
}
