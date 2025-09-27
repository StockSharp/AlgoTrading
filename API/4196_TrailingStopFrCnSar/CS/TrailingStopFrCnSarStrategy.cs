namespace StockSharp.Samples.Strategies;

using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using System.Globalization;

public class TrailingStopFrCnSarStrategy : Strategy
{

	private readonly StrategyParam<int> _mode;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _deltaPoints;
	private readonly StrategyParam<int> _stepPoints;
	private readonly StrategyParam<int> _fixedDistancePoints;
	private readonly StrategyParam<bool> _trailOnlyProfit;
	private readonly StrategyParam<bool> _trailOnlyBreakEven;
	private readonly StrategyParam<bool> _requireExistingStop;
	private readonly StrategyParam<bool> _useGeneralBreakEven;
	private readonly StrategyParam<int> _velocityPeriod;
	private readonly StrategyParam<decimal> _velocityMultiplier;
	private readonly StrategyParam<decimal> _parabolicStep;
	private readonly StrategyParam<decimal> _parabolicMax;
	private readonly StrategyParam<bool> _logSummary;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _historyDepth;

	private ParabolicSar _parabolicSar = default!;

	private readonly Queue<(decimal High, decimal Low)> _history = new();
	private readonly Queue<decimal> _velocityBuffer = new();

	private decimal _h1, _h2, _h3, _h4, _h5;
	private decimal _l1, _l2, _l3, _l4, _l5;
	private int _fractalSamples;
	private decimal? _lastUpFractal;
	private decimal? _lastDownFractal;

	private decimal? _previousVelocity;
	private decimal? _lastClose;

	private decimal? _longStop;
	private decimal? _shortStop;

	private string _lastSummary;

	public enum TrailingStopModes
	{
		Off = 0,
		Candle = 1,
		Fractal = 2,
		Velocity = 3,
		Parabolic = 4,
		FixedPoints = 5,
	}

	public TrailingStopFrCnSarStrategy()
	{
		_mode = Param(nameof(Mode), (int)TrailingStopModes.Candle)
			.SetDisplay("Trailing mode", "Trailing stop calculation mode.", "Trailing");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Primary timeframe processed by the trailing stop manager.", "General");

		_deltaPoints = Param(nameof(DeltaPoints), 0)
			.SetNotNegative()
			.SetDisplay("Offset (points)", "Extra distance added to fractal, candle or SAR stop levels.", "Trailing");

		_stepPoints = Param(nameof(StepPoints), 0)
			.SetNotNegative()
			.SetDisplay("Step (points)", "Minimum improvement required to update an existing stop.", "Trailing");

		_fixedDistancePoints = Param(nameof(FixedDistancePoints), 50)
			.SetNotNegative()
			.SetDisplay("Fixed distance", "Trailing distance in points when fixed mode is enabled.", "Trailing");

		_trailOnlyProfit = Param(nameof(TrailOnlyProfit), true)
			.SetDisplay("Only profitable", "Move the stop only after the position becomes profitable.", "Risk");

		_trailOnlyBreakEven = Param(nameof(TrailOnlyBreakEven), false)
			.SetDisplay("Only break-even", "Stop trailing once the stop loss reaches the entry price.", "Risk");

		_requireExistingStop = Param(nameof(RequireExistingStop), false)
			.SetDisplay("Require stop", "Ignore trailing updates while no stop price exists yet.", "Risk");

		_useGeneralBreakEven = Param(nameof(UseGeneralBreakEven), false)
			.SetDisplay("Use average entry", "Base the profit filter on the average entry price of the net position.", "Risk");

		_velocityPeriod = Param(nameof(VelocityPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Velocity period", "Bars used to average close-to-close velocity.", "Trailing");

		_velocityMultiplier = Param(nameof(VelocityMultiplier), 1m)
			.SetDisplay("Velocity multiplier", "Scales the velocity adjustment applied to the trailing distance.", "Trailing");

		_parabolicStep = Param(nameof(ParabolicStep), 0.02m)
			.SetDisplay("SAR step", "Acceleration step used by the Parabolic SAR trailing mode.", "Trailing");

		_parabolicMax = Param(nameof(ParabolicMaximum), 0.2m)
			.SetDisplay("SAR maximum", "Maximum acceleration used by the Parabolic SAR trailing mode.", "Trailing");

		_logSummary = Param(nameof(LogOrderSummary), true)
			.SetDisplay("Log summary", "Write account and position diagnostics similar to the OrderBalans indicator.", "General");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade volume", "Default volume used by helper methods when flattening positions.", "Trading");
		_historyDepth = Param(nameof(HistoryDepth), 512)
			.SetGreaterThanZero()
			.SetDisplay("History Depth", "Number of candles retained for fractal calculations", "Trailing");
	}

	public TrailingStopModes Mode
	{
		get => (TrailingStopModes)_mode.Value;
		set => _mode.Value = (int)value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int DeltaPoints
	{
		get => _deltaPoints.Value;
		set => _deltaPoints.Value = value;
	}

	public int StepPoints
	{
		get => _stepPoints.Value;
		set => _stepPoints.Value = value;
	}

	public int FixedDistancePoints
	{
		get => _fixedDistancePoints.Value;
		set => _fixedDistancePoints.Value = value;
	}

	public bool TrailOnlyProfit
	{
		get => _trailOnlyProfit.Value;
		set => _trailOnlyProfit.Value = value;
	}

	public bool TrailOnlyBreakEven
	{
		get => _trailOnlyBreakEven.Value;
		set => _trailOnlyBreakEven.Value = value;
	}

	public bool RequireExistingStop
	{
		get => _requireExistingStop.Value;
		set => _requireExistingStop.Value = value;
	}

	public bool UseGeneralBreakEven
	{
		get => _useGeneralBreakEven.Value;
		set => _useGeneralBreakEven.Value = value;
	}

	public int VelocityPeriod
	{
		get => _velocityPeriod.Value;
		set => _velocityPeriod.Value = value;
	}

	public decimal VelocityMultiplier
	{
		get => _velocityMultiplier.Value;
		set => _velocityMultiplier.Value = value;
	}

	public decimal ParabolicStep
	{
		get => _parabolicStep.Value;
		set => _parabolicStep.Value = value;
	}

	public decimal ParabolicMaximum
	{
		get => _parabolicMax.Value;
		set => _parabolicMax.Value = value;
	}

	public bool LogOrderSummary
	{
		get => _logSummary.Value;
		set => _logSummary.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}
	/// <summary>
	/// Number of candles retained for fractal-based trailing analysis.
	/// </summary>
	public int HistoryDepth
	{
		get => _historyDepth.Value;
		set
		{
			if (_historyDepth.Value == value)
				return;

			_historyDepth.Value = value;
			TrimHistoryBuffer();
		}
	}


	private void TrimHistoryBuffer()
	{
		var limit = _historyDepth.Value;
		if (limit <= 0)
		{
			limit = 1;
			_historyDepth.Value = limit;
		}

		while (_history.Count > limit)
		{
			_history.Dequeue();
		}
	}
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();

		_history.Clear();
		_velocityBuffer.Clear();

		_h1 = _h2 = _h3 = _h4 = _h5 = 0m;
		_l1 = _l2 = _l3 = _l4 = _l5 = 0m;
		_fractalSamples = 0;
		_lastUpFractal = null;
		_lastDownFractal = null;

		_previousVelocity = null;
		_lastClose = null;

		_longStop = null;
		_shortStop = null;

		_lastSummary = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_parabolicSar = new ParabolicSar
		{
			AccelerationStep = ParabolicStep,
			AccelerationMax = ParabolicMaximum
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_parabolicSar, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			_longStop = null;
			_shortStop = null;
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal parabolicValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateHistory(candle);
		UpdateFractals(candle);

		var previousVelocity = _previousVelocity;
		var currentVelocity = UpdateVelocity(candle.ClosePrice);

		var mode = Mode;

		if (mode == TrailingStopModes.Parabolic && !_parabolicSar.IsFormed)
		{
			// Wait for the SAR to warm up before using its values.
			_previousVelocity = currentVelocity ?? previousVelocity;
			UpdateSummary(candle);
			return;
		}

		if (Position > 0m)
		{
			var candidate = CalculateLongCandidate(candle, mode, parabolicValue, currentVelocity, previousVelocity);
			if (candidate is decimal stop)
				TryUpdateLongStop(stop, candle);

			CheckLongExit(candle);
		}
		else
		{
			_longStop = null;
		}

		if (Position < 0m)
		{
			var candidate = CalculateShortCandidate(candle, mode, parabolicValue, currentVelocity, previousVelocity);
			if (candidate is decimal stop)
				TryUpdateShortStop(stop, candle);

			CheckShortExit(candle);
		}
		else
		{
			_shortStop = null;
		}

		_previousVelocity = currentVelocity ?? previousVelocity;

		UpdateSummary(candle);
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		_history.Enqueue((candle.HighPrice, candle.LowPrice));

		while (_history.Count > HistoryDepth)
			_history.Dequeue();
	}

	private void UpdateFractals(ICandleMessage candle)
	{
		// Maintain the rolling window required to detect five-bar fractals.
		_h1 = _h2;
		_h2 = _h3;
		_h3 = _h4;
		_h4 = _h5;
		_h5 = candle.HighPrice;

		_l1 = _l2;
		_l2 = _l3;
		_l3 = _l4;
		_l4 = _l5;
		_l5 = candle.LowPrice;

		if (_fractalSamples >= 4)
		{
			if (_h3 > _h1 && _h3 > _h2 && _h3 > _h4 && _h3 > _h5)
				_lastUpFractal = _h3;

			if (_l3 < _l1 && _l3 < _l2 && _l3 < _l4 && _l3 < _l5)
				_lastDownFractal = _l3;
		}

		if (_fractalSamples < 4)
			_fractalSamples++;
	}

	private decimal? UpdateVelocity(decimal closePrice)
	{
		var point = GetPoint();
		if (point <= 0m)
			point = 1m;

		if (_lastClose is not decimal previousClose)
		{
			_lastClose = closePrice;
			return null;
		}

		// Convert the price change into points to keep the value scale similar to MetaTrader's Velocity custom indicator.
		var diffPoints = (closePrice - previousClose) / point;
		_velocityBuffer.Enqueue(diffPoints);

		var period = Math.Max(1, VelocityPeriod);
		while (_velocityBuffer.Count > period)
			_velocityBuffer.Dequeue();

		decimal sum = 0m;
		foreach (var value in _velocityBuffer)
			sum += value;

		var average = sum / _velocityBuffer.Count;

		_lastClose = closePrice;
		return average;
	}

	private decimal? CalculateLongCandidate(ICandleMessage candle, TrailingStopModes mode, decimal sarValue, decimal? currentVelocity, decimal? previousVelocity)
	{
		var point = GetPoint();
		if (point <= 0m)
			point = 1m;

		var delta = DeltaPoints * point;

		switch (mode)
		{
			case TrailingStopModes.Off:
				return null;
			case TrailingStopModes.Candle:
				if (GetRecentLow() is not decimal candleLow)
					return null;
				var candleStop = candleLow - delta;
				return candleStop > 0m ? candleStop : null;
			case TrailingStopModes.Fractal:
				if (_lastDownFractal is not decimal fractal)
					return null;
				var fractalStop = fractal - delta;
				return fractalStop > 0m ? fractalStop : null;
			case TrailingStopModes.Velocity:
				return CalculateVelocityStop(true, candle.ClosePrice, delta, currentVelocity, previousVelocity, point);
			case TrailingStopModes.Parabolic:
				return CalculateParabolicStop(true, sarValue, delta, candle.ClosePrice);
			case TrailingStopModes.FixedPoints:
				return CalculateFixedStop(true, candle.ClosePrice, point);
			default:
				return null;
		}
	}

	private decimal? CalculateShortCandidate(ICandleMessage candle, TrailingStopModes mode, decimal sarValue, decimal? currentVelocity, decimal? previousVelocity)
	{
		var point = GetPoint();
		if (point <= 0m)
			point = 1m;

		var delta = DeltaPoints * point;

		switch (mode)
		{
			case TrailingStopModes.Off:
				return null;
			case TrailingStopModes.Candle:
				if (GetRecentHigh() is not decimal candleHigh)
					return null;
				return candleHigh + delta;
			case TrailingStopModes.Fractal:
				if (_lastUpFractal is not decimal fractal)
					return null;
				return fractal + delta;
			case TrailingStopModes.Velocity:
				return CalculateVelocityStop(false, candle.ClosePrice, delta, currentVelocity, previousVelocity, point);
			case TrailingStopModes.Parabolic:
				return CalculateParabolicStop(false, sarValue, delta, candle.ClosePrice);
			case TrailingStopModes.FixedPoints:
				return CalculateFixedStop(false, candle.ClosePrice, point);
			default:
				return null;
		}
	}

	private decimal? CalculateVelocityStop(bool isLong, decimal price, decimal delta, decimal? currentVelocity, decimal? previousVelocity, decimal point)
	{
		if (currentVelocity is not decimal current || previousVelocity is not decimal previous)
			return null;

		if (isLong)
		{
			if (current <= previous)
				return null;

			var adjustment = (current - previous) * VelocityMultiplier * point;
			var distance = delta - adjustment;
			if (distance <= point)
				distance = point;

			return price - distance;
		}

		if (previous <= current)
			return null;

		var shortAdjustment = (previous - current) * VelocityMultiplier * point;
		var shortDistance = delta + shortAdjustment;
		if (shortDistance <= point)
			shortDistance = point;

		return price + shortDistance;
	}

	private decimal? CalculateParabolicStop(bool isLong, decimal sarValue, decimal delta, decimal price)
	{
		if (isLong)
		{
			if (sarValue >= price)
				return null;

			return sarValue - delta;
		}

		if (sarValue <= price)
			return null;

		return sarValue + delta;
	}

	private decimal? CalculateFixedStop(bool isLong, decimal price, decimal point)
	{
		var distance = FixedDistancePoints * point;
		if (distance <= 0m)
			return null;

		return isLong ? price - distance : price + distance;
	}

	private decimal? GetRecentLow()
	{
		if (_history.Count < 2)
			return null;

		var data = _history.ToArray();
		for (var i = data.Length - 2; i >= 0; i--)
		{
			var low = data[i].Low;
			if (low > 0m)
				return low;
		}

		return null;
	}

	private decimal? GetRecentHigh()
	{
		if (_history.Count < 2)
			return null;

		var data = _history.ToArray();
		for (var i = data.Length - 2; i >= 0; i--)
		{
			var high = data[i].High;
			if (high > 0m)
				return high;
		}

		return null;
	}

	private void TryUpdateLongStop(decimal candidate, ICandleMessage candle)
	{
		if (candidate <= 0m || candidate >= candle.ClosePrice)
			return;

		if (RequireExistingStop && _longStop is null)
			return;

		if (TrailOnlyBreakEven && _longStop is decimal existing && PositionPrice is decimal entry && existing >= entry)
			return;

		if (TrailOnlyProfit && !UseGeneralBreakEven && PositionPrice is decimal entryPrice && candidate < entryPrice)
			return;

		if (UseGeneralBreakEven && PositionPrice is decimal averageEntry && candidate < averageEntry)
			return;

		var point = GetPoint();
		if (point <= 0m)
			point = 1m;

		var stepDistance = StepPoints * point;
		if (_longStop is decimal current && candidate <= current + stepDistance)
			return;

		_longStop = AlignPrice(candidate);
	}

	private void TryUpdateShortStop(decimal candidate, ICandleMessage candle)
	{
		if (candidate <= candle.ClosePrice)
			return;

		if (RequireExistingStop && _shortStop is null)
			return;

		if (TrailOnlyBreakEven && _shortStop is decimal existing && PositionPrice is decimal entry && existing <= entry)
			return;

		if (TrailOnlyProfit && !UseGeneralBreakEven && PositionPrice is decimal entryPrice && candidate > entryPrice)
			return;

		if (UseGeneralBreakEven && PositionPrice is decimal averageEntry && candidate > averageEntry)
			return;

		var point = GetPoint();
		if (point <= 0m)
			point = 1m;

		var stepDistance = StepPoints * point;
		if (_shortStop is decimal current && candidate >= current - stepDistance)
			return;

		_shortStop = AlignPrice(candidate);
	}

	private void CheckLongExit(ICandleMessage candle)
	{
		if (_longStop is not decimal stop)
			return;

		if (candle.LowPrice > stop)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		// Flatten the long position once price touches the trailing stop level.
		SellMarket(volume);
		_longStop = null;
	}

	private void CheckShortExit(ICandleMessage candle)
	{
		if (_shortStop is not decimal stop)
			return;

		if (candle.HighPrice < stop)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		// Cover the short exposure when the trailing stop is triggered.
		BuyMarket(volume);
		_shortStop = null;
	}

	private void UpdateSummary(ICandleMessage candle)
	{
		if (!LogOrderSummary)
			return;

		var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		var entryPrice = PositionPrice;
		var stop = Position > 0m ? _longStop : Position < 0m ? _shortStop : null;

		decimal unrealized = 0m;
		if (entryPrice is decimal average)
			unrealized = (candle.ClosePrice - average) * Position;

		var summary = string.Format(
			CultureInfo.InvariantCulture,
			"Balance={0:F2} Position={1:+0.##;-0.##;0} Entry={2} Stop={3} Price={4:F5} UnrealizedPnL={5:F2}",
			balance,
			Position,
			entryPrice?.ToString("F5", CultureInfo.InvariantCulture) ?? "-",
			stop?.ToString("F5", CultureInfo.InvariantCulture) ?? "-",
			candle.ClosePrice,
			unrealized);

		if (summary == _lastSummary)
			return;

		// Provide a textual dashboard similar to the OrderBalans indicator from MetaTrader.
		this.LogInfo(summary);
		_lastSummary = summary;
	}

	private decimal GetPoint()
	{
		var security = Security;
		if (security == null)
			return 1m;

		var step = security.PriceStep ?? 0m;
		if (step > 0m)
			return step;

		var minStep = security.MinPriceStep ?? 0m;
		if (minStep > 0m)
			return minStep;

		return 1m;
	}

	private decimal AlignPrice(decimal price)
	{
		var security = Security;
		if (security == null)
			return price;

		var step = security.PriceStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Round(price / step, MidpointRounding.AwayFromZero);
			return steps * step;
		}

		var decimals = security.Decimals ?? 0;
		if (decimals > 0)
			return Math.Round(price, decimals, MidpointRounding.AwayFromZero);

		return price;
	}
}

