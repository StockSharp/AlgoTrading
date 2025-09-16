using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that demonstrates a basic trailing stop.
/// Opens a long position and lets the stop follow the price.
/// </summary>
public class SimpleTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _trailPoints;
	private readonly StrategyParam<DataType> _candleType;

	private bool _positionOpened;

	/// <summary>
	/// Distance in price points for trailing stop.
	/// </summary>
	public decimal TrailPoints
	{
		get => _trailPoints.Value;
		set => _trailPoints.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="SimpleTrailingStopStrategy"/>.
	/// </summary>
	public SimpleTrailingStopStrategy()
	{
		_trailPoints = Param(nameof(TrailPoints), 25m)
			.SetDisplay("Trail Points", "Distance for trailing stop", "Protection")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_positionOpened = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(
			takeProfit: null,
			stopLoss: new Unit(TrailPoints, UnitTypes.Absolute),
			isStopTrailing: true,
			useMarketOrders: true);

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenCandlesFinished(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_positionOpened)
		{
			BuyMarket(Volume);
			_positionOpened = true;
		}
	}
}

