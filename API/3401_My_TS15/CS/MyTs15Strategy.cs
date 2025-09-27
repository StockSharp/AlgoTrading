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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trailing stop manager driven by a moving average similar to the my_ts15.mq5 expert.
/// </summary>
public class MyTs15Strategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MovingAverageMethod> _maMethod;
	private readonly StrategyParam<CandlePrice> _maPrice;
	private readonly StrategyParam<int> _maBarsTrail;
	private readonly StrategyParam<decimal> _trailBehindMaPoints;
	private readonly StrategyParam<decimal> _trailBehindPricePoints;
	private readonly StrategyParam<decimal> _trailBehindNegativePoints;
	private readonly StrategyParam<decimal> _trailStepPoints;
	private readonly StrategyParam<bool> _enforceMaxStopLoss;
	private readonly StrategyParam<decimal> _maxStopLossPoints;
	private readonly StrategyParam<bool> _showIndicator;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _maHistory = new();

	private LengthIndicator<decimal> _maIndicator;
	private Order _activeStopOrder;
	private decimal? _longStop;
	private decimal? _shortStop;
	private decimal _pipSize;

	public MyTs15Strategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 50)
		.SetDisplay("MA Period", "Length of the trailing moving average.", "Moving Average")
		.SetGreaterThanZero();

		_maShift = Param(nameof(MaShift), 0)
		.SetDisplay("MA Shift", "Additional bar shift applied when requesting MA values.", "Moving Average")
		.SetNotNegative();

		_maMethod = Param(nameof(MaMethod), MovingAverageMethod.LinearWeighted)
		.SetDisplay("MA Method", "Moving average smoothing method.", "Moving Average");

		_maPrice = Param(nameof(MaPrice), CandlePrice.Weighted)
		.SetDisplay("MA Price", "Candle price used by the moving average.", "Moving Average");

		_maBarsTrail = Param(nameof(MaBarsTrail), 1)
		.SetDisplay("MA Bars Trail", "Number of completed bars between the current candle and the MA sample.", "Trailing")
		.SetNotNegative();

		_trailBehindMaPoints = Param(nameof(TrailBehindMaPoints), 5m)
		.SetDisplay("Trail Behind MA", "Distance in points kept between stop loss and MA.", "Trailing")
		.SetNotNegative();

		_trailBehindPricePoints = Param(nameof(TrailBehindPricePoints), 30m)
		.SetDisplay("Trail Behind Price", "Distance in points kept behind the price when in profit.", "Trailing")
		.SetNotNegative();

		_trailBehindNegativePoints = Param(nameof(TrailBehindNegativePoints), 60m)
		.SetDisplay("Trail Behind Negative", "Distance in points kept behind the price when in loss.", "Trailing")
		.SetNotNegative();

		_trailStepPoints = Param(nameof(TrailStepPoints), 0m)
		.SetDisplay("Trail Step", "Minimum improvement in points required to move the stop.", "Trailing")
		.SetNotNegative();

		_enforceMaxStopLoss = Param(nameof(EnforceMaxStopLoss), false)
		.SetDisplay("Enforce Max Stop", "Close or clamp positions exceeding the maximum stop distance.", "Protection");

		_maxStopLossPoints = Param(nameof(MaxStopLossPoints), 100m)
		.SetDisplay("Max Stop Loss", "Maximum allowed loss distance in points.", "Protection")
		.SetNotNegative();

		_showIndicator = Param(nameof(ShowIndicator), true)
		.SetDisplay("Show Indicator", "Draw the moving average on the chart area if available.", "Visualization");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series used for trailing logic.", "General");
	}

	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public int MaShift { get => _maShift.Value; set => _maShift.Value = value; }
	public MovingAverageMethod MaMethod { get => _maMethod.Value; set => _maMethod.Value = value; }
	public CandlePrice MaPrice { get => _maPrice.Value; set => _maPrice.Value = value; }
	public int MaBarsTrail { get => _maBarsTrail.Value; set => _maBarsTrail.Value = value; }
	public decimal TrailBehindMaPoints { get => _trailBehindMaPoints.Value; set => _trailBehindMaPoints.Value = value; }
	public decimal TrailBehindPricePoints { get => _trailBehindPricePoints.Value; set => _trailBehindPricePoints.Value = value; }
	public decimal TrailBehindNegativePoints { get => _trailBehindNegativePoints.Value; set => _trailBehindNegativePoints.Value = value; }
	public decimal TrailStepPoints { get => _trailStepPoints.Value; set => _trailStepPoints.Value = value; }
	public bool EnforceMaxStopLoss { get => _enforceMaxStopLoss.Value; set => _enforceMaxStopLoss.Value = value; }
	public decimal MaxStopLossPoints { get => _maxStopLossPoints.Value; set => _maxStopLossPoints.Value = value; }
	public bool ShowIndicator { get => _showIndicator.Value; set => _showIndicator.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_maHistory.Clear();
		_maIndicator = null;
		_activeStopOrder = null;
		_longStop = null;
		_shortStop = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		_maIndicator = CreateMovingAverage(MaMethod, MaPeriod, MaPrice);

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_maIndicator, ProcessCandle)
		.Start();

		if (ShowIndicator)
			{
			var area = CreateChartArea();
			if (area != null)
				{
				DrawCandles(area, subscription);
				DrawIndicator(area, _maIndicator);
				DrawOwnTrades(area);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!maValue.IsFinal)
			return;

		var ma = maValue.ToDecimal();
		_maHistory.Add(ma);

		var requiredShift = MaBarsTrail + MaShift;
		if (requiredShift < 0)
			requiredShift = 0;

		var maxHistory = requiredShift + 10;
		if (maxHistory < 10)
			maxHistory = 10;

		if (_maHistory.Count > maxHistory)
			_maHistory.RemoveRange(0, _maHistory.Count - maxHistory);

		var index = _maHistory.Count - 1 - requiredShift;
		if (index < 0)
			return;

		var referenceMa = _maHistory[index];
		var price = GetPrice(candle, MaPrice);

		if (Position > 0)
			{
			ManageLongPosition(price, referenceMa);
		}
		else if (Position < 0)
			{
			ManageShortPosition(price, referenceMa);
		}
		else
			{
			ResetStops();
		}
	}

	private void ManageLongPosition(decimal price, decimal ma)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		var positionPrice = PositionPrice ?? price;
		var pip = GetPipSize();
		if (pip <= 0m)
			return;

		var maOffset = TrailBehindMaPoints * pip;
		var pricePositiveOffset = TrailBehindPricePoints * pip;
		var priceNegativeOffset = TrailBehindNegativePoints * pip;
		var stepDistance = TrailStepPoints * pip;
		var maxLossDistance = MaxStopLossPoints * pip;

		if (EnforceMaxStopLoss && maxLossDistance > 0m && price <= positionPrice - maxLossDistance)
			{
			ClosePosition();
			return;
		}

		var trailCandidate = Math.Min(ma - maOffset, price - pricePositiveOffset);
		if (price <= positionPrice + pricePositiveOffset)
			{
			var negativeCandidate = price - priceNegativeOffset;
			if (trailCandidate > negativeCandidate)
				trailCandidate = negativeCandidate;
		}
		else if (trailCandidate > price - pricePositiveOffset)
			{
			trailCandidate = price - pricePositiveOffset;
		}

		if (EnforceMaxStopLoss && maxLossDistance > 0m && trailCandidate <= positionPrice - maxLossDistance)
			trailCandidate = positionPrice - maxLossDistance;

		if (trailCandidate <= 0m)
			return;

		var shouldUpdate = !_longStop.HasValue;
		if (!shouldUpdate && trailCandidate > _longStop.Value)
			{
			if (stepDistance <= 0m || trailCandidate - _longStop.Value >= stepDistance)
				shouldUpdate = true;
		}

		if (!shouldUpdate)
			return;

		if (MoveStop(true, trailCandidate, volume))
			{
			_longStop = trailCandidate;
			_shortStop = null;
		}
	}

	private void ManageShortPosition(decimal price, decimal ma)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		var positionPrice = PositionPrice ?? price;
		var pip = GetPipSize();
		if (pip <= 0m)
			return;

		var maOffset = TrailBehindMaPoints * pip;
		var pricePositiveOffset = TrailBehindPricePoints * pip;
		var priceNegativeOffset = TrailBehindNegativePoints * pip;
		var stepDistance = TrailStepPoints * pip;
		var maxLossDistance = MaxStopLossPoints * pip;

		if (EnforceMaxStopLoss && maxLossDistance > 0m && price >= positionPrice + maxLossDistance)
			{
			ClosePosition();
			return;
		}

		var trailCandidate = Math.Max(ma + maOffset, price + pricePositiveOffset);
		if (price >= positionPrice - pricePositiveOffset)
			{
			var negativeCandidate = price + priceNegativeOffset;
			if (trailCandidate < negativeCandidate)
				trailCandidate = negativeCandidate;
		}
		else if (trailCandidate < price + pricePositiveOffset)
			{
			trailCandidate = price + pricePositiveOffset;
		}

		if (EnforceMaxStopLoss && maxLossDistance > 0m && trailCandidate >= positionPrice + maxLossDistance)
			trailCandidate = positionPrice + maxLossDistance;

		if (trailCandidate <= 0m)
			return;

		var shouldUpdate = !_shortStop.HasValue;
		if (!shouldUpdate && trailCandidate < _shortStop.Value)
			{
			if (stepDistance <= 0m || _shortStop.Value - trailCandidate >= stepDistance)
				shouldUpdate = true;
		}

		if (!shouldUpdate)
			return;

		if (MoveStop(false, trailCandidate, volume))
			{
			_shortStop = trailCandidate;
			_longStop = null;
		}
	}

	private bool MoveStop(bool isLong, decimal price, decimal volume)
	{
		if (volume <= 0m)
			return false;

		if (_activeStopOrder != null && _activeStopOrder.State == OrderStates.Active)
			CancelOrder(_activeStopOrder);

		_activeStopOrder = isLong
		? SellStop(volume, price)
		: BuyStop(volume, price);

		return _activeStopOrder != null;
	}

	private void ResetStops()
	{
		_longStop = null;
		_shortStop = null;

		if (_activeStopOrder != null && _activeStopOrder.State == OrderStates.Active)
			CancelOrder(_activeStopOrder);

		_activeStopOrder = null;
	}

	private decimal GetPipSize()
	{
		if (_pipSize > 0m)
			return _pipSize;

		_pipSize = CalculatePipSize();
		return _pipSize;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0m;

		var digits = GetDecimalDigits(step);
		if (digits == 3 || digits == 5)
			return step * 10m;

		return step;
	}

	private static int GetDecimalDigits(decimal value)
	{
		value = Math.Abs(value);
		var digits = 0;

		while (value != Math.Floor(value) && digits < 10)
			{
			value *= 10m;
			digits++;
		}

		return digits;
	}

	private static decimal GetPrice(ICandleMessage candle, CandlePrice priceType)
	{
		return priceType switch
		{
			CandlePrice.Open => candle.OpenPrice,
			CandlePrice.High => candle.HighPrice,
			CandlePrice.Low => candle.LowPrice,
			CandlePrice.Close => candle.ClosePrice,
			CandlePrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			CandlePrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			CandlePrice.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethod method, int length, CandlePrice price)
	{
		LengthIndicator<decimal> indicator = method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage(),
			MovingAverageMethod.Exponential => new ExponentialMovingAverage(),
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage(),
			MovingAverageMethod.LinearWeighted => new WeightedMovingAverage(),
			_ => new SimpleMovingAverage(),
		};

		indicator.Length = length;

		switch (indicator)
			{
			case SimpleMovingAverage sma:
			sma.CandlePrice = price;
			break;
			case ExponentialMovingAverage ema:
			ema.CandlePrice = price;
			break;
			case SmoothedMovingAverage smoothed:
			smoothed.CandlePrice = price;
			break;
			case WeightedMovingAverage wma:
			wma.CandlePrice = price;
			break;
		}

		return indicator;
	}

	/// <summary>
	/// Moving average smoothing methods supported by the strategy.
	/// </summary>
	public enum MovingAverageMethod
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Simple,
		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Exponential,
		/// <summary>
		/// Smoothed moving average.
		/// </summary>
		Smoothed,
		/// <summary>
		/// Linear weighted moving average.
		/// </summary>
		LinearWeighted
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
			ResetStops();
	}
}

