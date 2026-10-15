using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Iron Bot statistical trend filter strategy.
/// The highest high and lowest low of the last AnalysisWindow candles define a range. The trend line sits at its middle, the high
/// trend level HighTrendLimit of the range below the top and the low trend level LowTrendLimit of the range below the top. A close
/// crossing above the high trend level (and so above the trend line) with a non-negative Z-score of the close goes long, a close
/// crossing below the low trend level (and the trend line) with a non-positive Z-score goes short, reversing an opposite position.
/// A stop at SlRatio and a take profit at the TP ratio picked by TakeProfitLevel, both fractions of the entry price, close trades.
/// </summary>
public class IronBotStatisticalTrendFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _zLength;
	private readonly StrategyParam<int> _analysisWindow;
	private readonly StrategyParam<decimal> _highTrendLimit;
	private readonly StrategyParam<decimal> _lowTrendLimit;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _slRatio;
	private readonly StrategyParam<decimal> _tp1Ratio;
	private readonly StrategyParam<decimal> _tp2Ratio;
	private readonly StrategyParam<decimal> _tp3Ratio;
	private readonly StrategyParam<decimal> _tp4Ratio;
	private readonly StrategyParam<int> _takeProfitLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;

	/// <summary>
	/// Length of the Z-score mean and deviation.
	/// </summary>
	public int ZLength
	{
		get => _zLength.Value;
		set => _zLength.Value = value;
	}

	/// <summary>
	/// Candles the trend range spans.
	/// </summary>
	public int AnalysisWindow
	{
		get => _analysisWindow.Value;
		set => _analysisWindow.Value = value;
	}

	/// <summary>
	/// Fibonacci fraction of the range below the top for the high trend level.
	/// </summary>
	public decimal HighTrendLimit
	{
		get => _highTrendLimit.Value;
		set => _highTrendLimit.Value = value;
	}

	/// <summary>
	/// Fibonacci fraction of the range below the top for the low trend level.
	/// </summary>
	public decimal LowTrendLimit
	{
		get => _lowTrendLimit.Value;
		set => _lowTrendLimit.Value = value;
	}

	/// <summary>
	/// EMA length shown on the chart.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Stop loss as a fraction of the entry price.
	/// </summary>
	public decimal SlRatio
	{
		get => _slRatio.Value;
		set => _slRatio.Value = value;
	}

	/// <summary>
	/// First take profit as a fraction of the entry price.
	/// </summary>
	public decimal Tp1Ratio
	{
		get => _tp1Ratio.Value;
		set => _tp1Ratio.Value = value;
	}

	/// <summary>
	/// Second take profit as a fraction of the entry price.
	/// </summary>
	public decimal Tp2Ratio
	{
		get => _tp2Ratio.Value;
		set => _tp2Ratio.Value = value;
	}

	/// <summary>
	/// Third take profit as a fraction of the entry price.
	/// </summary>
	public decimal Tp3Ratio
	{
		get => _tp3Ratio.Value;
		set => _tp3Ratio.Value = value;
	}

	/// <summary>
	/// Fourth take profit as a fraction of the entry price.
	/// </summary>
	public decimal Tp4Ratio
	{
		get => _tp4Ratio.Value;
		set => _tp4Ratio.Value = value;
	}

	/// <summary>
	/// Which take profit ratio (1-4) closes the trade.
	/// </summary>
	public int TakeProfitLevel
	{
		get => _takeProfitLevel.Value;
		set => _takeProfitLevel.Value = value;
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
	public IronBotStatisticalTrendFilterStrategy()
	{
		_zLength = Param(nameof(ZLength), 40)
			.SetGreaterThanZero()
			.SetDisplay("Z Length", "Length of the Z-score mean and deviation", "Indicators");

		_analysisWindow = Param(nameof(AnalysisWindow), 44)
			.SetGreaterThanZero()
			.SetDisplay("Analysis Window", "Candles the trend range spans", "Indicators");

		_highTrendLimit = Param(nameof(HighTrendLimit), 0.236m)
			.SetDisplay("High Trend Limit", "Fibonacci fraction below the top for the high trend level", "Indicators");

		_lowTrendLimit = Param(nameof(LowTrendLimit), 0.786m)
			.SetDisplay("Low Trend Limit", "Fibonacci fraction below the top for the low trend level", "Indicators");

		_emaLength = Param(nameof(EmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA length shown on the chart", "Indicators");

		_slRatio = Param(nameof(SlRatio), 0.008m)
			.SetNotNegative()
			.SetDisplay("SL Ratio", "Stop loss as a fraction of the entry price", "Risk");

		_tp1Ratio = Param(nameof(Tp1Ratio), 0.0075m)
			.SetNotNegative()
			.SetDisplay("TP1 Ratio", "First take profit as a fraction of the entry price", "Risk");

		_tp2Ratio = Param(nameof(Tp2Ratio), 0.011m)
			.SetNotNegative()
			.SetDisplay("TP2 Ratio", "Second take profit as a fraction of the entry price", "Risk");

		_tp3Ratio = Param(nameof(Tp3Ratio), 0.015m)
			.SetNotNegative()
			.SetDisplay("TP3 Ratio", "Third take profit as a fraction of the entry price", "Risk");

		_tp4Ratio = Param(nameof(Tp4Ratio), 0.02m)
			.SetNotNegative()
			.SetDisplay("TP4 Ratio", "Fourth take profit as a fraction of the entry price", "Risk");

		_takeProfitLevel = Param(nameof(TakeProfitLevel), 1)
			.SetRange(1, 4)
			.SetDisplay("Take Profit Level", "Which take profit ratio (1-4) closes the trade", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;

		var mean = new SimpleMovingAverage { Length = ZLength };
		var deviation = new StandardDeviation { Length = ZLength };
		var highest = new Highest { Length = AnalysisWindow };
		var lowest = new Lowest { Length = AnalysisWindow };
		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(mean, deviation, highest, lowest, ema, ProcessCandle)
			.Start();

		var takeRatio = TakeProfitLevel switch
		{
			2 => Tp2Ratio,
			3 => Tp3Ratio,
			4 => Tp4Ratio,
			_ => Tp1Ratio,
		};

		StartProtection(
			takeProfit: takeRatio > 0 ? new Unit(takeRatio * 100m, UnitTypes.Percent) : new Unit(),
			stopLoss: SlRatio > 0 ? new Unit(SlRatio * 100m, UnitTypes.Percent) : new Unit(),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal meanValue, decimal deviationValue, decimal highestValue, decimal lowestValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevClose = _prevClose;
		var close = candle.ClosePrice;
		_prevClose = close;

		if (prevClose is not decimal prev || !IsFormedAndOnlineAndAllowTrading())
			return;

		var range = highestValue - lowestValue;
		if (range <= 0)
			return;

		var zScore = deviationValue == 0m ? 0m : (close - meanValue) / deviationValue;

		var highTrendLevel = highestValue - range * HighTrendLimit;
		var trendLine = highestValue - range * 0.5m;
		var lowTrendLevel = highestValue - range * LowTrendLimit;

		var crossUp = close > trendLine && close > highTrendLevel && (prev <= trendLine || prev <= highTrendLevel);
		var crossDown = close < trendLine && close < lowTrendLevel && (prev >= trendLine || prev >= lowTrendLevel);

		if (crossUp && zScore >= 0m && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && zScore <= 0m && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
