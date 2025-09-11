using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Ichimoku cloud and ZLEMA composite.
/// It trades when bullish components outnumber bearish ones.
/// </summary>
public class DynamicDotsDashboardStrategy : Strategy
{
	private readonly StrategyParam<int> _conversionPeriods;
	private readonly StrategyParam<int> _basePeriods;
	private readonly StrategyParam<int> _laggingSpan2Periods;
	private readonly StrategyParam<int> _ma1Length;
	private readonly StrategyParam<int> _ma2Length;
	private readonly StrategyParam<bool> _useCloud;
	private readonly StrategyParam<DataType> _candleType;

	private Ichimoku _ichimoku;
	private ZeroLagExponentialMovingAverage _zlema1;
	private ZeroLagExponentialMovingAverage _zlema2;

	/// <summary>
	/// Ichimoku conversion line period.
	/// </summary>
	public int ConversionPeriods
	{
		get => _conversionPeriods.Value;
		set => _conversionPeriods.Value = value;
	}

	/// <summary>
	/// Ichimoku base line period.
	/// </summary>
	public int BasePeriods
	{
		get => _basePeriods.Value;
		set => _basePeriods.Value = value;
	}

	/// <summary>
	/// Ichimoku lagging span B period.
	/// </summary>
	public int LaggingSpan2Periods
	{
		get => _laggingSpan2Periods.Value;
		set => _laggingSpan2Periods.Value = value;
	}

	/// <summary>
	/// Length of first ZLEMA.
	/// </summary>
	public int Ma1Length
	{
		get => _ma1Length.Value;
		set => _ma1Length.Value = value;
	}

	/// <summary>
	/// Length of second ZLEMA.
	/// </summary>
	public int Ma2Length
	{
		get => _ma2Length.Value;
		set => _ma2Length.Value = value;
	}

	/// <summary>
	/// Use Ichimoku cloud signals.
	/// </summary>
	public bool UseCloud
	{
		get => _useCloud.Value;
		set => _useCloud.Value = value;
	}

	/// <summary>
	/// Candle type used by strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy.
	/// </summary>
	public DynamicDotsDashboardStrategy()
	{
		_conversionPeriods = Param(nameof(ConversionPeriods), 9)
			.SetDisplay("Conversion Periods", "Ichimoku conversion line length", "Ichimoku");

		_basePeriods = Param(nameof(BasePeriods), 26)
			.SetDisplay("Base Periods", "Ichimoku base line length", "Ichimoku");

		_laggingSpan2Periods = Param(nameof(LaggingSpan2Periods), 52)
			.SetDisplay("Lagging Span B Periods", "Ichimoku span B length", "Ichimoku");

		_ma1Length = Param(nameof(Ma1Length), 99)
			.SetDisplay("MA1 Length", "First ZLEMA length", "Moving Averages");

		_ma2Length = Param(nameof(Ma2Length), 200)
			.SetDisplay("MA2 Length", "Second ZLEMA length", "Moving Averages");

		_useCloud = Param(nameof(UseCloud), true)
			.SetDisplay("Use Cloud", "Include Ichimoku cloud signals", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_ichimoku = null;
		_zlema1 = null;
		_zlema2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ichimoku = new Ichimoku
		{
			Tenkan = { Length = ConversionPeriods },
			Kijun = { Length = BasePeriods },
			SenkouB = { Length = LaggingSpan2Periods }
		};

		_zlema1 = new ZeroLagExponentialMovingAverage { Length = Ma1Length };
		_zlema2 = new ZeroLagExponentialMovingAverage { Length = Ma2Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_ichimoku, _zlema1, _zlema2, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _zlema1);
			DrawIndicator(area, _zlema2);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue, IIndicatorValue zlema1Value, IIndicatorValue zlema2Value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var ichimokuTyped = (IchimokuValue)ichimokuValue;

		if (ichimokuTyped.Tenkan is not decimal tenkan ||
		ichimokuTyped.Kijun is not decimal kijun ||
		ichimokuTyped.SenkouA is not decimal senkouA ||
		ichimokuTyped.SenkouB is not decimal senkouB)
		return;

		var z1 = zlema1Value.ToDecimal();
		var z2 = zlema2Value.ToDecimal();

		var aboveCount = 0;

		if (UseCloud)
		{
		if (candle.ClosePrice > tenkan)
		aboveCount++;
		if (candle.ClosePrice > kijun)
		aboveCount++;
		if (candle.ClosePrice >= senkouA)
		aboveCount++;
		if (candle.OpenPrice >= senkouB)
		aboveCount++;
		}

		if (candle.ClosePrice > z1)
		aboveCount++;
		if (candle.ClosePrice > z2)
		aboveCount++;

		var total = UseCloud ? 6 : 2;
		var belowCount = total - aboveCount;

		if (aboveCount > belowCount && Position <= 0)
		{
		BuyMarket(Volume + Math.Abs(Position));
		}
		else if (belowCount > aboveCount && Position >= 0)
		{
		SellMarket(Volume + Math.Abs(Position));
		}
	}
}
