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
/// Volatility breakout strategy with momentum filter.
/// Breaks out above/below rolling high/low with EMA trend and RSI momentum filter.
/// Uses StdDev-based stop and risk/reward target.
/// </summary>
public class VolatilityMomentumBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiLong;
	private readonly StrategyParam<decimal> _rsiShort;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<decimal> _stopMult;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private decimal _entryPrice;
	private decimal _stopDist;
	private int _cooldownRemaining;

	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal RsiLong { get => _rsiLong.Value; set => _rsiLong.Value = value; }
	public decimal RsiShort { get => _rsiShort.Value; set => _rsiShort.Value = value; }
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }
	public decimal StopMult { get => _stopMult.Value; set => _stopMult.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VolatilityMomentumBreakoutStrategy()
	{
		_lookback = Param(nameof(Lookback), 40)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Breakout lookback", "General");

		_emaLength = Param(nameof(EmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA trend filter", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "General");

		_rsiLong = Param(nameof(RsiLong), 55m)
			.SetDisplay("RSI Long", "RSI above for longs", "General");

		_rsiShort = Param(nameof(RsiShort), 45m)
			.SetDisplay("RSI Short", "RSI below for shorts", "General");

		_riskReward = Param(nameof(RiskReward), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk/Reward", "Target ratio", "Risk");

		_stopMult = Param(nameof(StopMult), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Mult", "StdDev multiplier for stop", "Risk");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 12)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait after a trade", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_highs.Clear();
		_lows.Clear();
		_entryPrice = 0;
		_stopDist = 0;
		_cooldownRemaining = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var stdDev = new StandardDeviation { Length = 14 };

		_highs.Clear();
		_lows.Clear();
		_entryPrice = 0;
		_stopDist = 0;
		_cooldownRemaining = 0;

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

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		while (_highs.Count > Lookback + 1)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (_highs.Count <= Lookback)
			return;

		// Previous highest/lowest (exclude current bar)
		decimal prevHigh = decimal.MinValue;
		decimal prevLow = decimal.MaxValue;
		for (int i = 0; i < _highs.Count - 1; i++)
		{
			if (_highs[i] > prevHigh) prevHigh = _highs[i];
			if (_lows[i] < prevLow) prevLow = _lows[i];
		}

		// TP/SL management
		if (Position > 0 && _entryPrice > 0 && _stopDist > 0)
		{
			var sl = _entryPrice - _stopDist;
			var tp = _entryPrice + _stopDist * RiskReward;
			if (candle.LowPrice <= sl || candle.HighPrice >= tp)
			{
				SellMarket();
				_entryPrice = 0;
				_stopDist = 0;
				_cooldownRemaining = SignalCooldownBars;
			}
		}
		else if (Position < 0 && _entryPrice > 0 && _stopDist > 0)
		{
			var sl = _entryPrice + _stopDist;
			var tp = _entryPrice - _stopDist * RiskReward;
			if (candle.HighPrice >= sl || candle.LowPrice <= tp)
			{
				BuyMarket();
				_entryPrice = 0;
				_stopDist = 0;
				_cooldownRemaining = SignalCooldownBars;
			}
		}

		// Entry signals
		if (_cooldownRemaining == 0 && Position <= 0 && candle.ClosePrice > prevHigh && candle.ClosePrice > emaVal && rsiVal > RsiLong && stdVal > 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_stopDist = StopMult * stdVal;
		}
		else if (_cooldownRemaining == 0 && Position >= 0 && candle.ClosePrice < prevLow && candle.ClosePrice < emaVal && rsiVal < RsiShort && stdVal > 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_stopDist = StopMult * stdVal;
		}
	}
}
