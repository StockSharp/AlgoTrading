using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Golden Ratio Cubes Strategy - trades breakouts based on golden ratio extensions.
/// </summary>
public class GoldenRatioCubesStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<decimal> _phi;

	private Highest _highest;
	private Lowest _lowest;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Lookback period for highest and lowest calculations.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Golden ratio multiplier.
	/// </summary>
	public decimal Phi
	{
		get => _phi.Value;
		set => _phi.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public GoldenRatioCubesStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_lookback = Param(nameof(Lookback), 34)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Lookback period for highest and lowest", "Golden Ratio")
			.SetCanOptimize(true)
			.SetOptimize(13, 55, 5);

		_phi = Param(nameof(Phi), 1.618m)
			.SetDisplay("Phi", "Golden ratio multiplier", "Golden Ratio")
			.SetCanOptimize(true)
			.SetOptimize(1.5m, 2m, 0.1m);
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

		_highest = new Highest { Length = Lookback };
		_lowest = new Lowest { Length = Lookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_highest, _lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue highestValue, IIndicatorValue lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		var high = highestValue.ToDecimal();
		var low = lowestValue.ToDecimal();
		var range = high - low;

		var upperLevel = high + range / Phi;
		var lowerLevel = low - range / Phi;
		var price = candle.ClosePrice;

		if (price > upperLevel && Position <= 0)
			RegisterBuy();
		else if (price < lowerLevel && Position >= 0)
			RegisterSell();
	}
}

