using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Crypto SUSDT 10 min strategy.
/// A candle that opens below the EMA and closes above it buys, one that opens above the EMA and closes below it sells short,
/// reversing an opposite position. Every trade is closed by a TakeProfitPercent target or a StopLossPercent stop.
/// </summary>
public class CryptoSusdt10MinStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _orderPercent;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// EMA length.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Take profit percentage from entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Percent of equity per order (informational; the strategy trades Volume).
	/// </summary>
	public decimal OrderPercent
	{
		get => _orderPercent.Value;
		set => _orderPercent.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public CryptoSusdt10MinStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(10).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_emaLength = Param(nameof(EmaLength), 24)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA length", "Indicators");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit percentage from entry price", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_orderPercent = Param(nameof(OrderPercent), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Order %", "Percent of equity per order", "Risk");
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

		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		StartProtection(
			TakeProfitPercent > 0 ? new Unit(TakeProfitPercent, UnitTypes.Percent) : new Unit(),
			StopLossPercent > 0 ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit(),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.ClosePrice > ema && candle.OpenPrice < ema && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (candle.ClosePrice < ema && candle.OpenPrice > ema && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
