using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EUR/USD Multi-Layer Statistical Regression Strategy.
/// </summary>
public class EurUsdMultiLayerStatisticalRegressionStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _mediumLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<decimal> _minRSquared;
	private readonly StrategyParam<decimal> _slopeThreshold;
	private readonly StrategyParam<decimal> _weightShort;
	private readonly StrategyParam<decimal> _weightMedium;
	private readonly StrategyParam<decimal> _weightLong;
	private readonly StrategyParam<decimal> _positionSizePct;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _maxDailyLossPct;

	private readonly Queue<decimal> _shortPrices = [];
	private readonly Queue<decimal> _mediumPrices = [];
	private readonly Queue<decimal> _longPrices = [];

	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	public int MediumLength
	{
		get => _mediumLength.Value;
		set => _mediumLength.Value = value;
	}

	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}

	public decimal MinRSquared
	{
		get => _minRSquared.Value;
		set => _minRSquared.Value = value;
	}

	public decimal SlopeThreshold
	{
		get => _slopeThreshold.Value;
		set => _slopeThreshold.Value = value;
	}

	public decimal WeightShort
	{
		get => _weightShort.Value;
		set => _weightShort.Value = value;
	}

	public decimal WeightMedium
	{
		get => _weightMedium.Value;
		set => _weightMedium.Value = value;
	}

	public decimal WeightLong
	{
		get => _weightLong.Value;
		set => _weightLong.Value = value;
	}

	public decimal PositionSizePct
	{
		get => _positionSizePct.Value;
		set => _positionSizePct.Value = value;
	}

	public decimal MaxDailyLossPct
	{
		get => _maxDailyLossPct.Value;
		set => _maxDailyLossPct.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public EurUsdMultiLayerStatisticalRegressionStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Short Length", "Short regression length", "Indicator");

		_mediumLength = Param(nameof(MediumLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Medium Length", "Medium regression length", "Indicator");

		_longLength = Param(nameof(LongLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("Long Length", "Long regression length", "Indicator");

		_minRSquared = Param(nameof(MinRSquared), 0.45m)
			.SetDisplay("Min R²", "Minimal R² threshold", "Signal");

		_slopeThreshold = Param(nameof(SlopeThreshold), 0.00005m)
			.SetDisplay("Slope Threshold", "Minimal slope value", "Signal");

		_weightShort = Param(nameof(WeightShort), 0.4m)
			.SetDisplay("Weight Short", "Weight for short layer", "Signal");

		_weightMedium = Param(nameof(WeightMedium), 0.35m)
			.SetDisplay("Weight Medium", "Weight for medium layer", "Signal");

		_weightLong = Param(nameof(WeightLong), 0.25m)
			.SetDisplay("Weight Long", "Weight for long layer", "Signal");

		_positionSizePct = Param(nameof(PositionSizePct), 50m)
			.SetDisplay("Position Size %", "Portfolio percent for position", "Risk");

		_maxDailyLossPct = Param(nameof(MaxDailyLossPct), 12m)
			.SetDisplay("Max Daily Loss %", "Daily loss percentage limit", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_shortPrices.Clear();
		_mediumPrices.Clear();
		_longPrices.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(new(), new Unit(MaxDailyLossPct, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		UpdateQueue(_shortPrices, candle.ClosePrice, ShortLength);
		UpdateQueue(_mediumPrices, candle.ClosePrice, MediumLength);
		UpdateQueue(_longPrices, candle.ClosePrice, LongLength);

		var hasShort = TryRegression(_shortPrices, ShortLength, out var slopeShort, out var r2Short);
		var hasMedium = TryRegression(_mediumPrices, MediumLength, out var slopeMedium, out var r2Medium);
		var hasLong = TryRegression(_longPrices, LongLength, out var slopeLong, out var r2Long);

		decimal ensemble = 0;
		int valid = 0;

		if (hasShort && r2Short >= MinRSquared && Math.Abs(slopeShort) >= SlopeThreshold)
		{
			ensemble += WeightShort * slopeShort;
			valid++;
		}

		if (hasMedium && r2Medium >= MinRSquared && Math.Abs(slopeMedium) >= SlopeThreshold)
		{
			ensemble += WeightMedium * slopeMedium;
			valid++;
		}

		if (hasLong && r2Long >= MinRSquared && Math.Abs(slopeLong) >= SlopeThreshold)
		{
			ensemble += WeightLong * slopeLong;
			valid++;
		}

		if (valid == 0)
			return;

		var reliability = (decimal)valid / 3m;

		var volume = (Portfolio.CurrentValue * PositionSizePct / 100m) / candle.ClosePrice;

		if (ensemble > 0 && Position <= 0 && reliability > 0.5m)
			BuyMarket(volume);
		else if (ensemble < 0 && Position >= 0 && reliability > 0.5m)
			SellMarket(volume);
	}

	private static void UpdateQueue(Queue<decimal> queue, decimal value, int length)
	{
		queue.Enqueue(value);
		while (queue.Count > length)
			queue.Dequeue();
	}

	private static bool TryRegression(Queue<decimal> data, int length, out decimal slope, out decimal rSquared)
	{
		slope = 0;
		rSquared = 0;

		if (data.Count < length)
			return false;

		decimal sumY = 0;
		decimal sumXY = 0;
		decimal sumY2 = 0;
		var x = 1;
		foreach (var y in data)
		{
			sumY += y;
			sumXY += x * y;
			sumY2 += y * y;
			x++;
		}

		var n = length;
		var sumX = n * (n + 1m) / 2m;
		var sumX2 = n * (n + 1m) * (2m * n + 1m) / 6m;
		var denominator = n * sumX2 - sumX * sumX;
		if (denominator == 0)
			return false;

		slope = (n * sumXY - sumX * sumY) / denominator;

		var corrDenominator = (double)((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY));
		if (corrDenominator <= 0)
			return false;

		var correlation = (n * sumXY - sumX * sumY) / (decimal)Math.Sqrt(corrDenominator);
		rSquared = correlation * correlation;
		return true;
	}
}