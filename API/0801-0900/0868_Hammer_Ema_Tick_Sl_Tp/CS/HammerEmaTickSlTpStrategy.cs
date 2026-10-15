using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hammer + EMA strategy with tick-based stop loss and take profit.
/// A hammer (lower wick at least twice the body, upper wick no longer than the body) closing above the EMA buys; an inverted
/// hammer (upper wick at least twice the body, lower wick no longer than the body) closing below the EMA sells. An opposite
/// signal reverses the position. Stop loss and take profit are set in price steps.
/// </summary>
public class HammerEmaTickSlTpStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// EMA trend filter length.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Stop loss in price steps.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Take profit in price steps.
	/// </summary>
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
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
	public HammerEmaTickSlTpStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA trend filter length", "Indicators");

		_stopLossTicks = Param(nameof(StopLossTicks), 1)
			.SetNotNegative()
			.SetDisplay("Stop Loss Ticks", "Stop loss in price steps", "Risk");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 10)
			.SetNotNegative()
			.SetDisplay("Take Profit Ticks", "Take profit in price steps", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var step = Security?.PriceStep ?? 1m;

		StartProtection(
			TakeProfitTicks > 0 ? new Unit(TakeProfitTicks * step, UnitTypes.Absolute) : new Unit(),
			StopLossTicks > 0 ? new Unit(StopLossTicks * step, UnitTypes.Absolute) : new Unit(),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		if (body <= 0)
			return;

		var upperWick = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
		var lowerWick = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;

		var isHammer = lowerWick >= 2 * body && upperWick <= body;
		var isInvertedHammer = upperWick >= 2 * body && lowerWick <= body;

		if (isHammer && candle.ClosePrice > emaValue && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (isInvertedHammer && candle.ClosePrice < emaValue && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
