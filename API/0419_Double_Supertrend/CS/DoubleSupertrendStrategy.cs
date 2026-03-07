using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double Supertrend Strategy.
/// Uses two SuperTrend indicators with different parameters.
/// Enters long when both SuperTrends are bullish.
/// Enters short when both SuperTrends are bearish.
/// </summary>
public class DoubleSupertrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _atrPeriod1;
	private readonly StrategyParam<decimal> _factor1;
	private readonly StrategyParam<int> _atrPeriod2;
	private readonly StrategyParam<decimal> _factor2;
	private readonly StrategyParam<int> _cooldownBars;

	private SuperTrend _st1;
	private SuperTrend _st2;
	private bool _prevUpTrend1;
	private bool _prevUpTrend2;
	private bool _hasPrev;
	private int _cooldownRemaining;

	public DoubleSupertrendStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_atrPeriod1 = Param(nameof(ATRPeriod1), 10)
			.SetGreaterThanZero()
			.SetDisplay("ST1 Period", "First SuperTrend ATR period", "SuperTrend 1");

		_factor1 = Param(nameof(Factor1), 2.0m)
			.SetDisplay("ST1 Factor", "First SuperTrend multiplier", "SuperTrend 1");

		_atrPeriod2 = Param(nameof(ATRPeriod2), 20)
			.SetGreaterThanZero()
			.SetDisplay("ST2 Period", "Second SuperTrend ATR period", "SuperTrend 2");

		_factor2 = Param(nameof(Factor2), 4.0m)
			.SetDisplay("ST2 Factor", "Second SuperTrend multiplier", "SuperTrend 2");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int ATRPeriod1
	{
		get => _atrPeriod1.Value;
		set => _atrPeriod1.Value = value;
	}

	public decimal Factor1
	{
		get => _factor1.Value;
		set => _factor1.Value = value;
	}

	public int ATRPeriod2
	{
		get => _atrPeriod2.Value;
		set => _atrPeriod2.Value = value;
	}

	public decimal Factor2
	{
		get => _factor2.Value;
		set => _factor2.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_st1 = null;
		_st2 = null;
		_prevUpTrend1 = false;
		_prevUpTrend2 = false;
		_hasPrev = false;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_st1 = new SuperTrend { Length = ATRPeriod1, Multiplier = Factor1 };
		_st2 = new SuperTrend { Length = ATRPeriod2, Multiplier = Factor2 };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(_st1, _st2, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _st1);
			DrawIndicator(area, _st2);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue st1Value, IIndicatorValue st2Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_st1.IsFormed || !_st2.IsFormed)
			return;

		if (st1Value.IsEmpty || st2Value.IsEmpty)
			return;

		var stv1 = (SuperTrendIndicatorValue)st1Value;
		var stv2 = (SuperTrendIndicatorValue)st2Value;

		var upTrend1 = stv1.IsUpTrend;
		var upTrend2 = stv2.IsUpTrend;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevUpTrend1 = upTrend1;
			_prevUpTrend2 = upTrend2;
			_hasPrev = true;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevUpTrend1 = upTrend1;
			_prevUpTrend2 = upTrend2;
			_hasPrev = true;
			return;
		}

		if (!_hasPrev)
		{
			_prevUpTrend1 = upTrend1;
			_prevUpTrend2 = upTrend2;
			_hasPrev = true;
			return;
		}

		// Both bullish
		var bothBullish = upTrend1 && upTrend2;
		// Both bearish
		var bothBearish = !upTrend1 && !upTrend2;

		// Trend changed to both bullish
		var bullishSignal = bothBullish && (!_prevUpTrend1 || !_prevUpTrend2);
		// Trend changed to both bearish
		var bearishSignal = bothBearish && (_prevUpTrend1 || _prevUpTrend2);

		// Buy when both SuperTrends turn bullish
		if (bullishSignal && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell when both SuperTrends turn bearish
		else if (bearishSignal && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long if either SuperTrend turns bearish
		else if (Position > 0 && !bothBullish && (_prevUpTrend1 && _prevUpTrend2))
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short if either SuperTrend turns bullish
		else if (Position < 0 && !bothBearish && (!_prevUpTrend1 && !_prevUpTrend2))
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevUpTrend1 = upTrend1;
		_prevUpTrend2 = upTrend2;
	}
}
