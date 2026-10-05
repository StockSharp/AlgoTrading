using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Geo strategy.
/// The candle is split at its close into the part above the low and the part below the high. When the lower part divided by the upper
/// part is within Tolerance percent of the golden ratio the close sits at the upper golden section and the strategy goes long; when the
/// upper part divided by the lower part is within tolerance it goes short. The opposite condition reverses the position.
/// </summary>
public class GeoStrategy : Strategy
{
	private const decimal _phi = 1.6180339887m;

	private readonly StrategyParam<decimal> _tolerance;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Allowed deviation from the golden ratio in percent.
	/// </summary>
	public decimal Tolerance
	{
		get => _tolerance.Value;
		set => _tolerance.Value = value;
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
	public GeoStrategy()
	{
		_tolerance = Param(nameof(Tolerance), 1m)
			.SetNotNegative()
			.SetDisplay("Tolerance", "Allowed deviation from the golden ratio in percent", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var lowerPart = candle.ClosePrice - candle.LowPrice;
		var upperPart = candle.HighPrice - candle.ClosePrice;

		if (lowerPart <= 0 || upperPart <= 0)
			return;

		var allowed = _phi * Tolerance / 100m;

		if (Math.Abs(lowerPart / upperPart - _phi) <= allowed && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (Math.Abs(upperPart / lowerPart - _phi) <= allowed && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
