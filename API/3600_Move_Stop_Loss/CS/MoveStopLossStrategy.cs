using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trailing stop manager converted from the original MoveStopLoss MQL5 expert advisor.
/// Automatically shifts protective stops alongside the price when profit grows above a configurable threshold.
/// </summary>
public class MoveStopLossStrategy : Strategy
{
	private readonly StrategyParam<bool> _autoTrail;
	private readonly StrategyParam<int> _manualDistancePoints;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _atrLookback;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr = null!;
	private Highest _atrHighest = null!;
	private decimal _pointSize;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _lastTrailingDistance;

	/// <summary>Enables ATR-based trailing distance.</summary>
	public bool AutoTrail
	{
		get => _autoTrail.Value;
		set => _autoTrail.Value = value;
	}

	/// <summary>Trailing distance expressed in raw points when manual mode is active.</summary>
	public int ManualDistancePoints
	{
		get => _manualDistancePoints.Value;
		set => _manualDistancePoints.Value = value;
	}

	/// <summary>ATR period used to estimate recent volatility.</summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>Number of ATR samples inspected while searching for the maximum range.</summary>
	public int AtrLookback
	{
		get => _atrLookback.Value;
		set => _atrLookback.Value = value;
	}

	/// <summary>Multiplier applied to the highest ATR value.</summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>Candle type used for ATR calculations and trailing logic.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>Gets the latest trailing distance applied in price units.</summary>
	public decimal? CurrentTrailingDistance => _lastTrailingDistance;

	/// <summary>
	/// Initializes a new instance of the <see cref="MoveStopLossStrategy"/> class.
	/// Sets default parameters to mirror the original expert configuration while adding optimization ranges.
	/// </summary>
	public MoveStopLossStrategy()
	{
		_autoTrail = Param(nameof(AutoTrail), true)
			.SetDisplay("Auto Trail", "Derive trailing distance from ATR extremes.", "Risk");

		_manualDistancePoints = Param(nameof(ManualDistancePoints), 300)
			.SetDisplay("Manual Distance (points)", "Trailing distance in points used when auto mode is disabled.", "Risk")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(50, 1000, 50);

		_atrPeriod = Param(nameof(AtrPeriod), 7)
			.SetDisplay("ATR Period", "Number of candles included in the ATR calculation.", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_atrLookback = Param(nameof(AtrLookback), 30)
			.SetDisplay("ATR Lookback", "ATR values considered when searching for the maximum range.", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_atrMultiplier = Param(nameof(AtrMultiplier), 0.85m)
			.SetDisplay("ATR Multiplier", "Multiplier applied to the highest ATR value to get trailing distance.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.50m, 1.50m, 0.05m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for ATR calculations and trailing management.", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			throw new InvalidOperationException("Security price step is not defined.");

		_pointSize = step;

		// Instantiate indicators responsible for ATR measurement and rolling maximum search.
		_atr = new AverageTrueRange { Length = AtrPeriod };
		_atrHighest = new Highest { Length = AtrLookback };

		_longStopPrice = null;
		_shortStopPrice = null;
		_lastTrailingDistance = null;

		// Enable platform level protection to reuse existing stop orders.
		StartProtection();

		// Subscribe to candles and bind ATR calculations to them.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_atr, ProcessCandle)
			.Start();

		// Optional visualization matching the informative label from the MQL version.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		// Reset cached stop values whenever the position flips or is closed.
		if (Position <= 0m)
			_longStopPrice = null;

		if (Position >= 0m)
			_shortStopPrice = null;

		if (Position == 0m)
			_lastTrailingDistance = null;
	}

	/// <summary>
	/// Processes completed candles and updates trailing stops accordingly.
	/// </summary>
	/// <param name="candle">Latest candle.</param>
	/// <param name="atrValue">ATR value calculated for the candle.</param>
	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Compute the distance the stop should trail behind the price.
		var trailingDistance = CalculateTrailingDistance(candle, atrValue);
		if (trailingDistance is not decimal distance || distance <= 0m)
			return;

		// Adjust the stop according to the direction of the active position.
		UpdateTrailingStop(candle, distance);
	}

	/// <summary>
	/// Calculates the trailing distance based on either ATR extremes or manual input.
	/// </summary>
	/// <param name="candle">Current candle used for timestamps.</param>
	/// <param name="atrValue">Most recent ATR value.</param>
	/// <returns>Trailing distance in price units or <c>null</c> when insufficient data is available.</returns>
	private decimal? CalculateTrailingDistance(ICandleMessage candle, decimal atrValue)
	{
		if (AutoTrail)
		{
			// Feed ATR values into the Highest indicator to reproduce the peak search from the original expert.
			var highestValue = _atrHighest.Process(atrValue, candle.OpenTime, true).ToDecimal();

			if (!_atrHighest.IsFormed)
				return null;

			var distance = highestValue * AtrMultiplier;
			return AlignDistance(distance);
		}

		if (ManualDistancePoints <= 0)
			return null;

		var manualDistance = ManualDistancePoints * _pointSize;
		return AlignDistance(manualDistance);
	}

	/// <summary>
	/// Moves the stop-loss so that it trails the market price by the requested distance.
	/// </summary>
	/// <param name="candle">Finished candle providing the latest close price.</param>
	/// <param name="trailingDistance">Desired trailing distance in price units.</param>
	private void UpdateTrailingStop(ICandleMessage candle, decimal trailingDistance)
	{
		var alignedDistance = AlignDistance(trailingDistance);
		if (alignedDistance <= 0m)
			return;

		var tolerance = _pointSize > 0m ? _pointSize / 2m : 0m;
		var closePrice = candle.ClosePrice;

		if (Position > 0m && PositionPrice is decimal entryPrice)
		{
			// Only trail once the bid moved past the entry price, mirroring the original MQL condition.
			if (closePrice <= entryPrice)
				return;

			var desiredStop = closePrice - alignedDistance;
			if (desiredStop <= 0m)
				return;

			// Skip updates that would not improve the already stored stop level.
			if (_longStopPrice is decimal existingStop && desiredStop <= existingStop + tolerance)
				return;

			SetStopLoss(alignedDistance, closePrice, Position);
			_longStopPrice = desiredStop;
			_lastTrailingDistance = alignedDistance;
		}
		else if (Position < 0m && PositionPrice is decimal entryPriceShort)
		{
			// Mirror the sell-side condition requiring the ask to move beyond the entry price.
			if (closePrice >= entryPriceShort)
				return;

			var desiredStop = closePrice + alignedDistance;

			if (_shortStopPrice is decimal existingStop && desiredStop >= existingStop - tolerance)
				return;

			SetStopLoss(alignedDistance, closePrice, Position);
			_shortStopPrice = desiredStop;
			_lastTrailingDistance = alignedDistance;
		}
	}

	/// <summary>
	/// Aligns a distance to the closest tradable step to avoid fractional prices unsupported by the exchange.
	/// </summary>
	/// <param name="distance">Desired distance.</param>
	/// <returns>Distance snapped to the instrument's price step.</returns>
	private decimal AlignDistance(decimal distance)
	{
		if (distance <= 0m)
			return 0m;

		var step = _pointSize;
		if (step <= 0m)
			return distance;

		var stepsCount = Math.Round(distance / step, MidpointRounding.AwayFromZero);
		if (stepsCount <= 0m)
			stepsCount = 1m;

		return stepsCount * step;
	}
}
