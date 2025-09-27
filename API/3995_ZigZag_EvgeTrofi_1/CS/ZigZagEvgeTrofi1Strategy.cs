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

using StockSharp.Algo;

/// <summary>
/// ZigZag swing strategy that reproduces the ZigZagEvgeTrofi 1 expert advisor.
/// Enters when a fresh ZigZag turning point appears within a limited number of bars.
/// </summary>
public class ZigZagEvgeTrofi1Strategy : Strategy
{
	private enum PivotTypes
	{
		None,
		High,
		Low
	}

	private readonly StrategyParam<int> _depth;
	private readonly StrategyParam<decimal> _deviation;
	private readonly StrategyParam<int> _backstep;
	private readonly StrategyParam<int> _urgency;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _volume;

	private Highest _highest;
	private Lowest _lowest;
	private PivotTypes _pivotType;
	private decimal _pivotPrice;
	private int _barsSincePivot;
	private bool _signalHandled;
	private decimal _priceStep;

	/// <summary>
	/// ZigZag depth parameter identical to the original expert advisor.
	/// </summary>
	public int Depth
	{
		get => _depth.Value;
		set => _depth.Value = value;
	}

	/// <summary>
	/// Minimum deviation expressed in points that confirms a new swing.
	/// </summary>
	public decimal Deviation
	{
		get => _deviation.Value;
		set => _deviation.Value = value;
	}

	/// <summary>
	/// Minimum bars required before switching to an opposite pivot.
	/// </summary>
	public int Backstep
	{
		get => _backstep.Value;
		set => _backstep.Value = value;
	}

	/// <summary>
	/// Number of bars after a pivot during which entries are allowed.
	/// </summary>
	public int Urgency
	{
		get => _urgency.Value;
		set => _urgency.Value = value;
	}

	/// <summary>
	/// Candle data type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Volume applied to each market order.
	/// </summary>
	public decimal VolumePerTrade
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ZigZagEvgeTrofi1Strategy"/> class.
	/// </summary>
	public ZigZagEvgeTrofi1Strategy()
	{
		_depth = Param(nameof(Depth), 17)
			.SetGreaterThanZero()
			.SetDisplay("Depth", "ZigZag depth parameter", "ZigZag")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 1);

		_deviation = Param(nameof(Deviation), 7m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation", "Minimum price movement in points", "ZigZag")
			.SetCanOptimize(true)
			.SetOptimize(1m, 20m, 1m);

		_backstep = Param(nameof(Backstep), 5)
			.SetNotNegative()
			.SetDisplay("Backstep", "Bars to wait before switching pivots", "ZigZag")
			.SetCanOptimize(true)
			.SetOptimize(0, 10, 1);

		_urgency = Param(nameof(Urgency), 2)
			.SetNotNegative()
			.SetDisplay("Urgency", "Maximum bars to trade the latest pivot", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0, 5, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");

		_volume = Param(nameof(VolumePerTrade), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume per trade", "Trading");
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

		_highest = null;
		_lowest = null;
		_pivotType = PivotTypes.None;
		_pivotPrice = 0m;
		_barsSincePivot = int.MaxValue;
		_signalHandled = true;
		_priceStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = GetEffectivePriceStep();
		_highest = new Highest { Length = Depth };
		_lowest = new Lowest { Length = Depth };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_highest, _lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		// React only on completed candles to mirror the original bar-by-bar logic.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure both indicators collected enough data before generating signals.
		if (_highest == null || _lowest == null || !_highest.IsFormed || !_lowest.IsFormed)
			return;

		// Track how many bars have passed since the latest identified pivot.
		if (_pivotType != PivotTypes.None && _barsSincePivot < int.MaxValue)
			_barsSincePivot++;

		var deviationPrice = GetDeviationPrice();
		var canSwitch = _pivotType == PivotTypes.None || _barsSincePivot >= Backstep;

		// Detect a new swing high whenever price matches the tracked maximum.
		if (candle.HighPrice >= highestValue && highestValue > 0m)
		{
			var difference = candle.HighPrice - _pivotPrice;
			if ((_pivotType != PivotTypes.High && canSwitch) || (_pivotType == PivotTypes.High && difference >= deviationPrice))
				SetPivot(PivotTypes.High, candle.HighPrice);
		}
		// Detect a new swing low whenever price touches the tracked minimum.
		else if (candle.LowPrice <= lowestValue && lowestValue > 0m)
		{
			var difference = _pivotPrice - candle.LowPrice;
			if ((_pivotType != PivotTypes.Low && canSwitch) || (_pivotType == PivotTypes.Low && difference >= deviationPrice))
				SetPivot(PivotTypes.Low, candle.LowPrice);
		}

		// Stop if no pivot is available after the checks above.
		if (_pivotType == PivotTypes.None)
			return;

		// Skip if the pivot is already considered stale by the urgency filter.
		if (_barsSincePivot > Urgency)
			return;

		// Avoid firing multiple orders for the same pivot.
		if (_signalHandled)
			return;

		// Confirm that trading permissions and connections are valid.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = VolumePerTrade;
		if (volume <= 0m)
		{
			_signalHandled = true;
			return;
		}

		var isBuySignal = _pivotType == PivotTypes.High;

		// Do nothing if a position in the same direction is already open.
		if (isBuySignal)
		{
			if (Position > 0m)
			{
				_signalHandled = true;
				return;
			}
		}
		else
		{
			if (Position < 0m)
			{
				_signalHandled = true;
				return;
			}
		}

		// Close existing exposure in the opposite direction before reversing.
		if (isBuySignal)
		{
			if (Position < 0m)
			{
				var closeVolume = Math.Abs(Position);
				if (closeVolume > 0m)
					BuyMarket(closeVolume);
			}
			BuyMarket(volume);
		}
		else
		{
			if (Position > 0m)
			{
				var closeVolume = Math.Abs(Position);
				if (closeVolume > 0m)
					SellMarket(closeVolume);
			}
			SellMarket(volume);
		}

		_signalHandled = true;
	}

	// Update the internal pivot state when a new turning point is registered.
	private void SetPivot(PivotTypes type, decimal price)
	{
		_pivotType = type;
		_pivotPrice = price;
		_barsSincePivot = 0;
		_signalHandled = false;
	}

	// Convert the deviation parameter expressed in points to an absolute price move.
	private decimal GetDeviationPrice()
	{
		var step = _priceStep > 0m ? _priceStep : 1m;
		var deviation = Deviation;
		if (deviation <= 0m)
			return step;

		var value = deviation * step;
		return value >= step ? value : step;
	}

	// Determine the effective price step for transforming point-based inputs.
	private decimal GetEffectivePriceStep()
	{
		if (Security != null)
		{
			if (Security.PriceStep.HasValue && Security.PriceStep.Value > 0m)
				return Security.PriceStep.Value;

			if (Security.MinPriceStep > 0m)
				return Security.MinPriceStep;
		}

		return 1m;
	}
}

