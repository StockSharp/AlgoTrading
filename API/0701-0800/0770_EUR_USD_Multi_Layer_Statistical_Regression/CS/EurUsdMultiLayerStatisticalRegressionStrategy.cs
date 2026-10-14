using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EUR/USD multi-layer statistical regression strategy.
/// Short, medium and long linear regressions each yield a slope and an R². A layer counts only when its R² reaches MinRSquared
/// and its absolute slope reaches SlopeThreshold. The weighted slope of the valid layers gives the direction and the weighted R²
/// of the valid layers relative to all weights gives the reliability; with reliability above 0.5 the strategy goes long on a
/// positive slope and short on a negative one, reversing an opposite position. Trading stops for the rest of the day once the
/// day's loss reaches MaxDailyLossPct of the account value.
/// </summary>
public class EurUsdMultiLayerStatisticalRegressionStrategy : Strategy
{
	private const decimal _minReliability = 0.5m;

	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _mediumLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<decimal> _minRSquared;
	private readonly StrategyParam<decimal> _slopeThreshold;
	private readonly StrategyParam<decimal> _weightShort;
	private readonly StrategyParam<decimal> _weightMedium;
	private readonly StrategyParam<decimal> _weightLong;
	private readonly StrategyParam<decimal> _positionSizePct;
	private readonly StrategyParam<decimal> _maxDailyLossPct;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentDay;
	private decimal _dayStartPnL;
	private decimal _dayStartValue;
	private bool _dayLossReached;

	/// <summary>
	/// Length of the short regression.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	/// <summary>
	/// Length of the medium regression.
	/// </summary>
	public int MediumLength
	{
		get => _mediumLength.Value;
		set => _mediumLength.Value = value;
	}

	/// <summary>
	/// Length of the long regression.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}

	/// <summary>
	/// Minimum R² for a layer to count.
	/// </summary>
	public decimal MinRSquared
	{
		get => _minRSquared.Value;
		set => _minRSquared.Value = value;
	}

	/// <summary>
	/// Minimum absolute slope for a layer to count.
	/// </summary>
	public decimal SlopeThreshold
	{
		get => _slopeThreshold.Value;
		set => _slopeThreshold.Value = value;
	}

	/// <summary>
	/// Weight of the short regression.
	/// </summary>
	public decimal WeightShort
	{
		get => _weightShort.Value;
		set => _weightShort.Value = value;
	}

	/// <summary>
	/// Weight of the medium regression.
	/// </summary>
	public decimal WeightMedium
	{
		get => _weightMedium.Value;
		set => _weightMedium.Value = value;
	}

	/// <summary>
	/// Weight of the long regression.
	/// </summary>
	public decimal WeightLong
	{
		get => _weightLong.Value;
		set => _weightLong.Value = value;
	}

	/// <summary>
	/// Position size in percent of equity (informational).
	/// </summary>
	public decimal PositionSizePct
	{
		get => _positionSizePct.Value;
		set => _positionSizePct.Value = value;
	}

	/// <summary>
	/// Daily loss limit in percent of the account value.
	/// </summary>
	public decimal MaxDailyLossPct
	{
		get => _maxDailyLossPct.Value;
		set => _maxDailyLossPct.Value = value;
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
	public EurUsdMultiLayerStatisticalRegressionStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Short Length", "Length of the short regression", "Regression");

		_mediumLength = Param(nameof(MediumLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Medium Length", "Length of the medium regression", "Regression");

		_longLength = Param(nameof(LongLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("Long Length", "Length of the long regression", "Regression");

		_minRSquared = Param(nameof(MinRSquared), 0.45m)
			.SetDisplay("Min R²", "Minimum R² for a layer to count", "Validation");

		_slopeThreshold = Param(nameof(SlopeThreshold), 0.00005m)
			.SetNotNegative()
			.SetDisplay("Slope Threshold", "Minimum absolute slope for a layer to count", "Validation");

		_weightShort = Param(nameof(WeightShort), 0.4m)
			.SetNotNegative()
			.SetDisplay("Short Weight", "Weight of the short regression", "Ensemble");

		_weightMedium = Param(nameof(WeightMedium), 0.35m)
			.SetNotNegative()
			.SetDisplay("Medium Weight", "Weight of the medium regression", "Ensemble");

		_weightLong = Param(nameof(WeightLong), 0.25m)
			.SetNotNegative()
			.SetDisplay("Long Weight", "Weight of the long regression", "Ensemble");

		_positionSizePct = Param(nameof(PositionSizePct), 50m)
			.SetNotNegative()
			.SetDisplay("Position Size %", "Position size in percent of equity", "Risk");

		_maxDailyLossPct = Param(nameof(MaxDailyLossPct), 12m)
			.SetNotNegative()
			.SetDisplay("Max Daily Loss %", "Daily loss limit in percent of the account value", "Risk");

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
		ResetDay();
	}

	private void ResetDay()
	{
		_currentDay = default;
		_dayStartPnL = 0m;
		_dayStartValue = 0m;
		_dayLossReached = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetDay();

		var shortReg = new LinearRegression { Length = ShortLength };
		var mediumReg = new LinearRegression { Length = MediumLength };
		var longReg = new LinearRegression { Length = LongLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(shortReg, mediumReg, longReg, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortReg);
			DrawIndicator(area, mediumReg);
			DrawIndicator(area, longReg);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue shortValue, IIndicatorValue mediumValue, IIndicatorValue longValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!shortValue.IsFormed || !mediumValue.IsFormed || !longValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (UpdateDailyLoss(candle.OpenTime.Date))
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(-Position);

			return;
		}

		var totalWeight = WeightShort + WeightMedium + WeightLong;
		if (totalWeight <= 0)
			return;

		var validWeight = 0m;
		var weightedSlope = 0m;
		var weightedR2 = 0m;

		AddLayer(shortValue, WeightShort, ref validWeight, ref weightedSlope, ref weightedR2);
		AddLayer(mediumValue, WeightMedium, ref validWeight, ref weightedSlope, ref weightedR2);
		AddLayer(longValue, WeightLong, ref validWeight, ref weightedSlope, ref weightedR2);

		if (validWeight <= 0)
			return;

		var slope = weightedSlope / validWeight;
		var reliability = weightedR2 / totalWeight;

		if (reliability <= _minReliability)
			return;

		if (slope > 0 && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (slope < 0 && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}

	private void AddLayer(IIndicatorValue value, decimal weight, ref decimal validWeight, ref decimal weightedSlope, ref decimal weightedR2)
	{
		if (value is not ILinearRegressionValue { LinearRegSlope: decimal slope, RSquared: decimal r2 })
			return;

		if (r2 < MinRSquared || Math.Abs(slope) < SlopeThreshold)
			return;

		validWeight += weight;
		weightedSlope += weight * slope;
		weightedR2 += weight * r2;
	}

	private bool UpdateDailyLoss(DateTime day)
	{
		if (_currentDay != day)
		{
			_currentDay = day;
			_dayStartPnL = PnL;
			_dayStartValue = Portfolio?.CurrentValue ?? 0m;
			_dayLossReached = false;
		}

		// Without a known account value there is nothing to take the percentage of.
		if (_dayStartValue > 0 && MaxDailyLossPct > 0 && PnL - _dayStartPnL <= -_dayStartValue * MaxDailyLossPct / 100m)
			_dayLossReached = true;

		return _dayLossReached;
	}
}
