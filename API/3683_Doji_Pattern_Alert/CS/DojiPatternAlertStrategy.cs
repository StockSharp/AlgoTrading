using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that detects classic doji candles and logs alerts when the pattern appears.
/// It mirrors the DojiEN MetaTrader expert by focusing on narrow candle bodies near the price midpoint.
/// </summary>
public class DojiPatternAlertStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _middleTolerancePercent;
	private readonly StrategyParam<decimal> _bodyDifferencePoints;

	private decimal _priceStep;
	private DateTimeOffset _lastProcessedTime;

	/// <summary>
	/// Type of candles used for pattern detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Maximum distance from the candle midpoint as a percentage of the total range.
	/// </summary>
	public decimal MiddleTolerancePercent
	{
		get => _middleTolerancePercent.Value;
		set => _middleTolerancePercent.Value = value;
	}

	/// <summary>
	/// Maximum difference between open and close expressed in price steps.
	/// </summary>
	public decimal BodyDifferencePoints
	{
		get => _bodyDifferencePoints.Value;
		set => _bodyDifferencePoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DojiPatternAlertStrategy"/> class.
	/// </summary>
	public DojiPatternAlertStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe used to evaluate the doji pattern", "General");

		_middleTolerancePercent = Param(nameof(MiddleTolerancePercent), 10m)
			.SetDisplay("Middle Tolerance (%)", "Maximum distance from the middle of the candle as percentage of the total range", "Pattern")
			.SetGreaterThanZero();

		_bodyDifferencePoints = Param(nameof(BodyDifferencePoints), 3m)
			.SetDisplay("Body Difference (points)", "Maximum difference between open and close in price steps to classify a candle as a doji", "Pattern")
			.SetGreaterOrEqualZero();
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

		_priceStep = default;
		_lastProcessedTime = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
		{
			// Fallback to a neutral price step when the security does not expose a tick size.
			_priceStep = 1m;
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			// Display the input candles so the user can visually confirm the detected doji patterns.
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Work only with fully finished candles to replicate the MetaTrader expert behavior.
		if (candle.State != CandleStates.Finished)
			return;

		// Ignore duplicate callbacks for the same bar.
		if (_lastProcessedTime == candle.OpenTime)
			return;

		_lastProcessedTime = candle.OpenTime;

		var range = candle.HighPrice - candle.LowPrice;
		if (range <= 0m)
		{
			// Skip flat candles because the midpoint tolerance would collapse to zero.
			return;
		}

		var open = candle.OpenPrice;
		var close = candle.ClosePrice;
		var middle = (candle.HighPrice + candle.LowPrice) / 2m;

		var bodySize = Math.Abs(close - open);
		var bodyLimit = BodyDifferencePoints * _priceStep;

		var tolerance = range * (MiddleTolerancePercent / 100m);
		var openNearMiddle = Math.Abs(open - middle) <= tolerance;
		var closeNearMiddle = Math.Abs(close - middle) <= tolerance;

		var isBodySmall = bodySize <= bodyLimit;
		var isNearMiddle = openNearMiddle && closeNearMiddle;

		if (!isBodySmall || !isNearMiddle)
			return;

		// Log the detection so the trader receives the same alert as in the original EA.
		LogInfo($"Classic doji detected at {candle.CloseTime:O}. Body size: {bodySize}, range: {range}.");
	}
}
