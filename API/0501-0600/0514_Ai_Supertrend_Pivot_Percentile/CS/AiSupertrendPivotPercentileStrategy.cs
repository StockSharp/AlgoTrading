using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AI Supertrend x Pivot Percentile Strategy - combines two Supertrend indicators
/// with ADX and Williams %R filters.
/// </summary>
public class AiSupertrendPivotPercentileStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length1;
	private readonly StrategyParam<decimal> _factor1;
	private readonly StrategyParam<int> _length2;
	private readonly StrategyParam<decimal> _factor2;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _pivotLength;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _entryPrice;
	private int _cooldownRemaining;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Length1 { get => _length1.Value; set => _length1.Value = value; }
	public decimal Factor1 { get => _factor1.Value; set => _factor1.Value = value; }
	public int Length2 { get => _length2.Value; set => _length2.Value = value; }
	public decimal Factor2 { get => _factor2.Value; set => _factor2.Value = value; }
	public int AdxLength { get => _adxLength.Value; set => _adxLength.Value = value; }
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	public int PivotLength { get => _pivotLength.Value; set => _pivotLength.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public AiSupertrendPivotPercentileStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_length1 = Param(nameof(Length1), 10)
			.SetGreaterThanZero()
			.SetDisplay("ST1 Length", "First Supertrend ATR length", "Supertrend");

		_factor1 = Param(nameof(Factor1), 3m)
			.SetGreaterThanZero()
			.SetDisplay("ST1 Factor", "First Supertrend multiplier", "Supertrend");

		_length2 = Param(nameof(Length2), 20)
			.SetGreaterThanZero()
			.SetDisplay("ST2 Length", "Second Supertrend ATR length", "Supertrend");

		_factor2 = Param(nameof(Factor2), 4m)
			.SetGreaterThanZero()
			.SetDisplay("ST2 Factor", "Second Supertrend multiplier", "Supertrend");

		_adxLength = Param(nameof(AdxLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Length", "ADX calculation period", "Filter");

		_adxThreshold = Param(nameof(AdxThreshold), 15m)
			.SetDisplay("ADX Threshold", "Minimum ADX for trading", "Filter");

		_pivotLength = Param(nameof(PivotLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Pivot Length", "Length for Williams %R", "Filter");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var st1 = new SuperTrend { Length = Length1, Multiplier = Factor1 };
		var st2 = new SuperTrend { Length = Length2, Multiplier = Factor2 };
		var adx = new AverageDirectionalIndex { Length = AdxLength };
		var wpr = new WilliamsR { Length = PivotLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(st1, st2, adx, wpr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, st1);
			DrawIndicator(area, st2);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle,
		IIndicatorValue st1Value,
		IIndicatorValue st2Value,
		IIndicatorValue adxValue,
		IIndicatorValue wprValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var st1 = (SuperTrendIndicatorValue)st1Value;
		var st2 = (SuperTrendIndicatorValue)st2Value;
		var adxTyped = (IAverageDirectionalIndexValue)adxValue;
		var wpr = wprValue.ToDecimal();

		if (adxTyped.MovingAverage is not decimal adxMa)
			return;

		var st1Val = st1Value.ToDecimal();
		var st2Val = st2Value.ToDecimal();

		var isBull = candle.ClosePrice > st1Val && candle.ClosePrice > st2Val;
		var isBear = candle.ClosePrice < st1Val && candle.ClosePrice < st2Val;
		var strongTrend = adxMa > AdxThreshold;
		var pivotBull = wpr > -50m;
		var pivotBear = wpr < -50m;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		if (isBull && strongTrend && pivotBull && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_cooldownRemaining = CooldownBars;
		}
		else if (isBear && strongTrend && pivotBear && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_cooldownRemaining = CooldownBars;
		}
		// Exit conditions
		else if (Position > 0 && (!isBull || !pivotBull))
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && (!isBear || !pivotBear))
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
	}
}
