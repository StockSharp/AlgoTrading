using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Full Candle strategy.
/// Goes long on a bullish candle closing above the EMA whose upper shadow is at most ShadowPercent of the candle range,
/// and short on a bearish candle closing below the EMA whose lower shadow is at most ShadowPercent of the range. The
/// opposite signal reverses the position; percent take-profit and stop-loss manage the trade (0 disables either).
/// </summary>
public class FullCandleStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _shadowPercent;
	private readonly StrategyParam<decimal> _tpPercent;
	private readonly StrategyParam<decimal> _slPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Maximum breakout-side shadow in percent of the candle range.
	/// </summary>
	public decimal ShadowPercent
	{
		get => _shadowPercent.Value;
		set => _shadowPercent.Value = value;
	}

	/// <summary>
	/// Take-profit percentage. 0 disables it.
	/// </summary>
	public decimal TPPercent
	{
		get => _tpPercent.Value;
		set => _tpPercent.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage. 0 disables it.
	/// </summary>
	public decimal SLPercent
	{
		get => _slPercent.Value;
		set => _slPercent.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public FullCandleStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Indicators");

		_shadowPercent = Param(nameof(ShadowPercent), 5m)
			.SetNotNegative()
			.SetDisplay("Shadow %", "Maximum breakout-side shadow in percent of the candle range", "Signals");

		_tpPercent = Param(nameof(TPPercent), 1.2m)
			.SetNotNegative()
			.SetDisplay("TP %", "Take-profit percentage, 0 disables", "Risk");

		_slPercent = Param(nameof(SLPercent), 1.8m)
			.SetNotNegative()
			.SetDisplay("SL %", "Stop-loss percentage, 0 disables", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema, ProcessCandle)
			.Start();

		StartProtection(
			TPPercent > 0 ? new Unit(TPPercent, UnitTypes.Percent) : new Unit(),
			SLPercent > 0 ? new Unit(SLPercent, UnitTypes.Percent) : new Unit(),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!emaValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ema = emaValue.ToDecimal();
		var open = candle.OpenPrice;
		var close = candle.ClosePrice;
		var range = candle.HighPrice - candle.LowPrice;

		if (range <= 0)
			return;

		var maxShadow = range * ShadowPercent / 100m;

		var longSignal = close > open && close > ema && candle.HighPrice - close <= maxShadow;
		var shortSignal = close < open && close < ema && close - candle.LowPrice <= maxShadow;

		if (longSignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
