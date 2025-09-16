namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// ZigZag pivot strategy based on the original ZigZagEvgeTrofi expert advisor.
/// Reacts to the most recent zigzag swing and enters within a limited number of bars.
/// </summary>
public class ZigZagEvgeTrofiStrategy : Strategy
{
	private enum PivotType
	{
		None,
		High,
		Low
	}

	private readonly StrategyParam<int> _depth;
	private readonly StrategyParam<decimal> _deviation;
	private readonly StrategyParam<int> _backstep;
	private readonly StrategyParam<int> _urgency;
	private readonly StrategyParam<bool> _signalReverse;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _volume;

	private Highest _highest;
	private Lowest _lowest;
	private PivotType _pivotType;
	private decimal _pivotPrice;
	private int _barsSincePivot;
	private decimal _priceStep;

	/// <summary>
	/// ZigZag depth parameter controlling the swing detection window.
	/// </summary>
	public int Depth
	{
		get => _depth.Value;
		set => _depth.Value = value;
	}

	/// <summary>
	/// Minimum deviation in price steps required to confirm a new pivot.
	/// </summary>
	public decimal Deviation
	{
		get => _deviation.Value;
		set => _deviation.Value = value;
	}

	/// <summary>
	/// Minimum number of bars between opposite pivot updates.
	/// </summary>
	public int Backstep
	{
		get => _backstep.Value;
		set => _backstep.Value = value;
	}

	/// <summary>
	/// Maximum number of bars after a pivot when entries are allowed.
	/// </summary>
	public int Urgency
	{
		get => _urgency.Value;
		set => _urgency.Value = value;
	}

	/// <summary>
	/// Reverses the direction of the generated signals.
	/// </summary>
	public bool SignalReverse
	{
		get => _signalReverse.Value;
		set => _signalReverse.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Trading volume submitted on every entry.
	/// </summary>
	public decimal VolumePerTrade
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ZigZagEvgeTrofiStrategy"/> class.
	/// </summary>
	public ZigZagEvgeTrofiStrategy()
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
			.SetGreaterThanZero()
			.SetDisplay("Backstep", "Bars to lock a pivot before switching", "ZigZag")
			.SetCanOptimize(true)
			.SetOptimize(1, 15, 1);

		_urgency = Param(nameof(Urgency), 2)
			.SetGreaterThanOrEqual(0)
			.SetDisplay("Urgency", "Maximum bars to use the latest signal", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0, 5, 1);

		_signalReverse = Param(nameof(SignalReverse), false)
			.SetDisplay("Signal Reverse", "Flip long and short entries", "Trading");

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
		_pivotType = PivotType.None;
		_pivotPrice = 0m;
		_barsSincePivot = int.MaxValue;
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
		// Skip unfinished candles to ensure decisions are made on closed bars only.
		if (candle.State != CandleStates.Finished)
			return;

		// Wait until both indicators are fully formed before reacting.
		if (_highest == null || _lowest == null || !_highest.IsFormed || !_lowest.IsFormed)
			return;

		// Increment the bar counter that measures freshness of the latest pivot.
		if (_pivotType != PivotType.None && _barsSincePivot < int.MaxValue)
			_barsSincePivot++;

		var deviationPrice = Math.Max(GetDeviationInPrice(), _priceStep);
		var canSwitch = _pivotType == PivotType.None || _barsSincePivot >= Backstep;

		// Detect a fresh swing high if price pushes above the tracked maximum.
		if (candle.HighPrice >= highestValue && highestValue > 0m)
		{
			var difference = candle.HighPrice - _pivotPrice;
			if ((_pivotType != PivotType.High && canSwitch) || (_pivotType == PivotType.High && difference >= deviationPrice))
				SetPivot(PivotType.High, candle.HighPrice);
		}
		// Detect a fresh swing low when price dips under the tracked minimum.
		else if (candle.LowPrice <= lowestValue && lowestValue > 0m)
		{
			var difference = _pivotPrice - candle.LowPrice;
			if ((_pivotType != PivotType.Low && canSwitch) || (_pivotType == PivotType.Low && difference >= deviationPrice))
				SetPivot(PivotType.Low, candle.LowPrice);
		}

		if (_pivotType == PivotType.None)
			return;

		// Ensure trading conditions are satisfied (connection, data, permissions).
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var isBuySignal = _pivotType == PivotType.High ? !SignalReverse : SignalReverse;

		// Close opposite exposure before entering in the new direction.
		if (isBuySignal)
		{
			if (Position < 0)
			{
				var closeVolume = Math.Abs(Position);
				if (closeVolume > 0m)
					BuyMarket(closeVolume);
			}
		}
		else
		{
			if (Position > 0)
			{
				var closeVolume = Math.Abs(Position);
				if (closeVolume > 0m)
					SellMarket(closeVolume);
			}
		}

		// Enter the market while the pivot is still considered fresh.
		if (_barsSincePivot > Urgency)
			return;

		var volume = VolumePerTrade;
		if (volume <= 0m)
			return;

		if (isBuySignal)
			BuyMarket(volume);
		else
			SellMarket(volume);
	}

	// Update the stored pivot information when a new swing is confirmed.
	private void SetPivot(PivotType type, decimal price)
	{
		_pivotType = type;
		_pivotPrice = price;
		_barsSincePivot = 0;
	}

	// Convert the deviation input expressed in points to a price value.
	private decimal GetDeviationInPrice()
	{
		return Deviation * _priceStep;
	}

	// Determine the effective price step for translating point-based parameters.
	private decimal GetEffectivePriceStep()
	{
		if (Security.PriceStep.HasValue && Security.PriceStep.Value > 0m)
			return Security.PriceStep.Value;

		if (Security.MinPriceStep > 0m)
			return Security.MinPriceStep;

		return 1m;
	}
}
