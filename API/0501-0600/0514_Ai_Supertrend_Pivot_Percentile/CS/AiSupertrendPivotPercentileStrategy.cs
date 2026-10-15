using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AI Supertrend x Pivot Percentile strategy.
/// Goes long when the close is above both Supertrends, ADX is above AdxThreshold and Williams %R is above -50, and short when
/// the close is below both Supertrends, ADX is above AdxThreshold and Williams %R is below -50. The opposite signal reverses
/// the position, and percent take-profit and stop-loss protect it.
/// </summary>
public class AiSupertrendPivotPercentileStrategy : Strategy
{
	private readonly StrategyParam<int> _length1;
	private readonly StrategyParam<decimal> _factor1;
	private readonly StrategyParam<int> _length2;
	private readonly StrategyParam<decimal> _factor2;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _pivotLength;
	private readonly StrategyParam<decimal> _tpPercent;
	private readonly StrategyParam<decimal> _slPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// ATR period of the first Supertrend.
	/// </summary>
	public int Length1
	{
		get => _length1.Value;
		set => _length1.Value = value;
	}

	/// <summary>
	/// Multiplier of the first Supertrend.
	/// </summary>
	public decimal Factor1
	{
		get => _factor1.Value;
		set => _factor1.Value = value;
	}

	/// <summary>
	/// ATR period of the second Supertrend.
	/// </summary>
	public int Length2
	{
		get => _length2.Value;
		set => _length2.Value = value;
	}

	/// <summary>
	/// Multiplier of the second Supertrend.
	/// </summary>
	public decimal Factor2
	{
		get => _factor2.Value;
		set => _factor2.Value = value;
	}

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxLength
	{
		get => _adxLength.Value;
		set => _adxLength.Value = value;
	}

	/// <summary>
	/// Minimum ADX for entries.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int PivotLength
	{
		get => _pivotLength.Value;
		set => _pivotLength.Value = value;
	}

	/// <summary>
	/// Take-profit percentage.
	/// </summary>
	public decimal TpPercent
	{
		get => _tpPercent.Value;
		set => _tpPercent.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal SlPercent
	{
		get => _slPercent.Value;
		set => _slPercent.Value = value;
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
	public AiSupertrendPivotPercentileStrategy()
	{
		_length1 = Param(nameof(Length1), 10)
			.SetGreaterThanZero()
			.SetDisplay("ST1 Length", "ATR period of the first Supertrend", "Supertrend");

		_factor1 = Param(nameof(Factor1), 3m)
			.SetGreaterThanZero()
			.SetDisplay("ST1 Factor", "Multiplier of the first Supertrend", "Supertrend");

		_length2 = Param(nameof(Length2), 20)
			.SetGreaterThanZero()
			.SetDisplay("ST2 Length", "ATR period of the second Supertrend", "Supertrend");

		_factor2 = Param(nameof(Factor2), 4m)
			.SetGreaterThanZero()
			.SetDisplay("ST2 Factor", "Multiplier of the second Supertrend", "Supertrend");

		_adxLength = Param(nameof(AdxLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Length", "ADX period", "Filter");

		_adxThreshold = Param(nameof(AdxThreshold), 20m)
			.SetDisplay("ADX Threshold", "Minimum ADX for entries", "Filter");

		_pivotLength = Param(nameof(PivotLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Pivot Length", "Williams %R period", "Filter");

		_tpPercent = Param(nameof(TpPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take-profit percentage", "Risk");

		_slPercent = Param(nameof(SlPercent), 1m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
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

		StartProtection(new Unit(TpPercent, UnitTypes.Percent), new Unit(SlPercent, UnitTypes.Percent), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, st1);
			DrawIndicator(area, st2);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, adx);
				DrawIndicator(oscillators, wpr);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue st1Value, IIndicatorValue st2Value, IIndicatorValue adxValue, IIndicatorValue wprValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!st1Value.IsFormed || !st2Value.IsFormed || !adxValue.IsFormed || !wprValue.IsFormed)
			return;

		if (adxValue is not IAverageDirectionalIndexValue { MovingAverage: decimal adx })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var st1 = st1Value.ToDecimal();
		var st2 = st2Value.ToDecimal();
		var wpr = wprValue.ToDecimal();
		var strongTrend = adx > AdxThreshold;

		var longSignal = close > st1 && close > st2 && strongTrend && wpr > -50m;
		var shortSignal = close < st1 && close < st2 && strongTrend && wpr < -50m;

		if (longSignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
