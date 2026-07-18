namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Adaptive Fibonacci Pullback strategy.
/// Combines multiple SuperTrend lines and an adaptive moving average channel.
/// </summary>
public class AdaptiveFibonacciPullbackStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _factor1;
	private readonly StrategyParam<decimal> _factor2;
	private readonly StrategyParam<decimal> _factor3;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<int> _amaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiBuy;
	private readonly StrategyParam<decimal> _rsiSell;
	private readonly StrategyParam<int> _cooldownBars;

	private SuperTrend _st1;
	private SuperTrend _st2;
	private SuperTrend _st3;
	private ExponentialMovingAverage _stSmooth;
	private ExponentialMovingAverage _amaMid;
	private RelativeStrengthIndex _rsi;

	private decimal _prevClose;
	private decimal _prevSmooth;
	private bool _isFirst;
	private int _cooldownRemaining;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal Factor1 { get => _factor1.Value; set => _factor1.Value = value; }
	public decimal Factor2 { get => _factor2.Value; set => _factor2.Value = value; }
	public decimal Factor3 { get => _factor3.Value; set => _factor3.Value = value; }
	public int SmoothLength { get => _smoothLength.Value; set => _smoothLength.Value = value; }
	public int AmaLength { get => _amaLength.Value; set => _amaLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal RsiBuy { get => _rsiBuy.Value; set => _rsiBuy.Value = value; }
	public decimal RsiSell { get => _rsiSell.Value; set => _rsiSell.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public AdaptiveFibonacciPullbackStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("SuperTrend ATR Length", "ATR period for SuperTrend", "SuperTrend");

		_factor1 = Param(nameof(Factor1), 0.618m)
			.SetDisplay("Factor 1", "Weak Fibonacci factor", "SuperTrend");

		_factor2 = Param(nameof(Factor2), 1.618m)
			.SetDisplay("Factor 2", "Golden Ratio factor", "SuperTrend");

		_factor3 = Param(nameof(Factor3), 2.618m)
			.SetDisplay("Factor 3", "Extended Fibonacci factor", "SuperTrend");

		_smoothLength = Param(nameof(SmoothLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Length", "EMA length for SuperTrend average", "SuperTrend");

		_amaLength = Param(nameof(AmaLength), 55)
			.SetGreaterThanZero()
			.SetDisplay("AMA Length", "Length for AMA midline", "AMA");

		_rsiLength = Param(nameof(RsiLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_rsiBuy = Param(nameof(RsiBuy), 50m)
			.SetDisplay("RSI Buy Threshold", "RSI must be above for long", "RSI");

		_rsiSell = Param(nameof(RsiSell), 50m)
			.SetDisplay("RSI Sell Threshold", "RSI must be below for short", "RSI");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
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

		_prevClose = default;
		_prevSmooth = default;
		_isFirst = true;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_st1 = new SuperTrend { Length = AtrPeriod, Multiplier = Factor1 };
		_st2 = new SuperTrend { Length = AtrPeriod, Multiplier = Factor2 };
		_st3 = new SuperTrend { Length = AtrPeriod, Multiplier = Factor3 };
		_stSmooth = new ExponentialMovingAverage { Length = SmoothLength };
		_amaMid = new ExponentialMovingAverage { Length = AmaLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(new IIndicator[] { _st1, _st2, _st3, _amaMid, _rsi }, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (values[0].IsEmpty || values[1].IsEmpty || values[2].IsEmpty ||
			values[3].IsEmpty || values[4].IsEmpty)
			return;

		var st1Val = values[0].ToDecimal();
		var st2Val = values[1].ToDecimal();
		var st3Val = values[2].ToDecimal();
		var mid = values[3].ToDecimal();
		var rsiVal = values[4].ToDecimal();

		var avg = (st1Val + st2Val + st3Val) / 3m;
		var smooth = _stSmooth.Process(new DecimalIndicatorValue(_stSmooth, avg, candle.ServerTime)).ToDecimal();

		if (_isFirst)
		{
			_prevClose = candle.ClosePrice;
			_prevSmooth = smooth;
			_isFirst = false;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevClose = candle.ClosePrice;
			_prevSmooth = smooth;
			return;
		}

		var baseLong = candle.LowPrice < avg && candle.ClosePrice > smooth && _prevClose > mid;
		var baseShort = candle.HighPrice > avg && candle.ClosePrice < smooth && _prevClose < mid;

		var longEntry = baseLong && candle.ClosePrice > mid && rsiVal > RsiBuy;
		var shortEntry = baseShort && candle.ClosePrice < mid && rsiVal < RsiSell;

		if (longEntry && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		else if (shortEntry && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}

		// Exit conditions
		var longExit = _prevClose > _prevSmooth && candle.ClosePrice <= smooth && Position > 0;
		var shortExit = _prevClose < _prevSmooth && candle.ClosePrice >= smooth && Position < 0;

		if (longExit)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		else if (shortExit)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevClose = candle.ClosePrice;
		_prevSmooth = smooth;
	}
}
