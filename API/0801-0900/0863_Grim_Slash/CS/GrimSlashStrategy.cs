using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grim Slash strategy.
/// Buys when the candle's low touches or dips below the previous close and exits when the high reaches the previous high.
/// A percent take profit and stop loss manage the risk.
/// </summary>
public class GrimSlashStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private decimal? _prevHigh;

	/// <summary>
	/// Take profit percent from the entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop loss percent from the entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	public GrimSlashStrategy()
	{
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 15m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit percent from the entry price", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 5m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percent from the entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevClose = null;
		_prevHigh = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;
		_prevHigh = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			TakeProfitPercent > 0 ? new Unit(TakeProfitPercent, UnitTypes.Percent) : new Unit(),
			StopLossPercent > 0 ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit(),
			useMarketOrders: true);

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

		var prevClose = _prevClose;
		var prevHigh = _prevHigh;

		_prevClose = candle.ClosePrice;
		_prevHigh = candle.HighPrice;

		if (prevClose is not decimal lastClose || prevHigh is not decimal lastHigh)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if (candle.HighPrice >= lastHigh)
				SellMarket(Position);
		}
		else if (Position == 0 && candle.LowPrice <= lastClose)
		{
			BuyMarket(Volume);
		}
	}
}
