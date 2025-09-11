using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pivot breakout strategy with momentum filter and basic risk management.
/// </summary>
public class PowerHouseSwiftEdgeAiV210Strategy : Strategy
{
	private readonly StrategyParam<int> _pivotLength;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _minSignalDistance;
	private readonly StrategyParam<DataType> _candleType;

	private long _barIndex;
	private long _lastSignalBar = long.MinValue;
	private decimal? _lastHigh;
	private decimal? _lastLow;
	private decimal? _prevClose;
	private decimal? _entryPrice;

	/// <summary>
	/// Pivot period length.
	/// </summary>
	public int PivotLength
	{
		get => _pivotLength.Value;
		set => _pivotLength.Value = value;
	}

	/// <summary>
	/// Momentum threshold in percent.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// Take profit in price points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in price points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Minimum distance between signals in bars.
	/// </summary>
	public int MinSignalDistance
	{
		get => _minSignalDistance.Value;
		set => _minSignalDistance.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public PowerHouseSwiftEdgeAiV210Strategy()
	{
		_pivotLength = Param(nameof(PivotLength), 5)
			.SetDisplay("Pivot Length")
			.SetCanOptimize(true);

		_momentumThreshold = Param(nameof(MomentumThreshold), 1m)
			.SetDisplay("Momentum Threshold (%)")
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 10m)
			.SetDisplay("Take Profit (points)")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 10m)
			.SetDisplay("Stop Loss (points)")
			.SetCanOptimize(true);

		_minSignalDistance = Param(nameof(MinSignalDistance), 5)
			.SetDisplay("Min Signal Distance (bars)")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type");
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

		_barIndex = 0;
		_lastSignalBar = long.MinValue;
		_lastHigh = null;
		_lastLow = null;
		_prevClose = null;
		_entryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var highest = new Highest { Length = PivotLength };
		var lowest = new Lowest { Length = PivotLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highValue, decimal lowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		_lastHigh = highValue;
		_lastLow = lowValue;

		if (_prevClose is not null)
		{
			var change = (candle.ClosePrice - _prevClose.Value) / _prevClose.Value * 100m;

			if (change > MomentumThreshold && _lastHigh is decimal lh && candle.ClosePrice > lh && _barIndex - _lastSignalBar >= MinSignalDistance)
			{
				if (Position <= 0)
				{
					BuyMarket();
					_entryPrice = candle.ClosePrice;
					_lastSignalBar = _barIndex;
				}
			}
			else if (change < -MomentumThreshold && _lastLow is decimal ll && candle.ClosePrice < ll && _barIndex - _lastSignalBar >= MinSignalDistance)
			{
				if (Position >= 0)
				{
					SellMarket();
					_entryPrice = candle.ClosePrice;
					_lastSignalBar = _barIndex;
				}
			}
		}

		if (_entryPrice is decimal price)
		{
			if (Position > 0)
			{
				if (candle.HighPrice >= price + TakeProfit || candle.LowPrice <= price - StopLoss)
				{
					SellMarket();
					_entryPrice = null;
				}
			}
			else if (Position < 0)
			{
				if (candle.LowPrice <= price - TakeProfit || candle.HighPrice >= price + StopLoss)
				{
					BuyMarket();
					_entryPrice = null;
				}
			}
		}

		_prevClose = candle.ClosePrice;
	}
}
