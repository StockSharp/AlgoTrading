using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ICT NY Kill Zone Auto Trading strategy.
/// Trades during the New York kill zone when a fair value gap and order block appear.
/// Uses fixed take profit and stop loss.
/// </summary>
public class IctNyKillZoneAutoTradingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _prev1;
	private ICandleMessage _prev2;

	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
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
	/// Initialize strategy parameters.
	/// </summary>
	public IctNyKillZoneAutoTradingStrategy()
	{
		_stopLoss = Param(nameof(StopLoss), 30m)
			.SetDisplay("Stop Loss", "Stop loss in ticks", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 10m);

		_takeProfit = Param(nameof(TakeProfit), 60m)
			.SetDisplay("Take Profit", "Take profit in ticks", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(20m, 200m, 10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prev1 = null;
		_prev2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute));

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

		if (_prev1 != null && _prev2 != null)
		{
			var isKillZone = IsKillZone(candle.OpenTime);
			var isFvg = _prev2.HighPrice < candle.LowPrice && _prev1.HighPrice < candle.LowPrice;
			var bullishOb = _prev2.ClosePrice < _prev2.OpenPrice && _prev1.ClosePrice > _prev1.OpenPrice && candle.ClosePrice > candle.OpenPrice;
			var bearishOb = _prev2.ClosePrice > _prev2.OpenPrice && _prev1.ClosePrice < _prev1.OpenPrice && candle.ClosePrice < candle.OpenPrice;

			if (isKillZone && isFvg && bullishOb && Position <= 0)
				BuyMarket(Volume);
			else if (isKillZone && isFvg && bearishOb && Position >= 0)
				SellMarket(Volume);
		}

		_prev2 = _prev1;
		_prev1 = candle;
	}

	private static bool IsKillZone(DateTimeOffset time)
	{
		var start = new DateTimeOffset(time.Year, time.Month, time.Day, 9, 30, 0, time.Offset);
		var end = new DateTimeOffset(time.Year, time.Month, time.Day, 16, 0, 0, time.Offset);
		return time >= start && time <= end;
	}
}

