using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double Supertrend strategy.
/// A position opens in the allowed Direction ("Long", "Short" or "Both") when the close moves above (below) both
/// Supertrend lines. A close back through the first line exits. With TPType "Supertrend" the second line also works as
/// a trailing exit; with TPType "Percent" a TPPercent take-profit is used instead. SLPercent is a percent stop-loss.
/// </summary>
public class DoubleSupertrendStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod1;
	private readonly StrategyParam<decimal> _factor1;
	private readonly StrategyParam<int> _atrPeriod2;
	private readonly StrategyParam<decimal> _factor2;
	private readonly StrategyParam<string> _direction;
	private readonly StrategyParam<string> _tpType;
	private readonly StrategyParam<decimal> _tpPercent;
	private readonly StrategyParam<decimal> _slPercent;
	private readonly StrategyParam<DataType> _candleType;

	private bool? _prevAboveBoth;
	private bool? _prevBelowBoth;

	/// <summary>
	/// ATR period of the first Supertrend.
	/// </summary>
	public int ATRPeriod1
	{
		get => _atrPeriod1.Value;
		set => _atrPeriod1.Value = value;
	}

	/// <summary>
	/// Multiplier of the first Supertrend.
	/// </summary>
	public decimal Factor1
	{
		get => _factor1.Value;
		set => _factor1.Value = value;
	}

	/// <summary>
	/// ATR period of the second Supertrend.
	/// </summary>
	public int ATRPeriod2
	{
		get => _atrPeriod2.Value;
		set => _atrPeriod2.Value = value;
	}

	/// <summary>
	/// Multiplier of the second Supertrend.
	/// </summary>
	public decimal Factor2
	{
		get => _factor2.Value;
		set => _factor2.Value = value;
	}

	/// <summary>
	/// Allowed trade direction: "Long", "Short" or "Both".
	/// </summary>
	public string Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
	}

	/// <summary>
	/// Take-profit type: "Supertrend" or "Percent".
	/// </summary>
	public string TPType
	{
		get => _tpType.Value;
		set => _tpType.Value = value;
	}

	/// <summary>
	/// Take-profit percentage used when TPType is "Percent".
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
	public DoubleSupertrendStrategy()
	{
		_atrPeriod1 = Param(nameof(ATRPeriod1), 10)
			.SetGreaterThanZero()
			.SetDisplay("ST1 Period", "ATR period of the first Supertrend", "Supertrend 1");

		_factor1 = Param(nameof(Factor1), 3.0m)
			.SetGreaterThanZero()
			.SetDisplay("ST1 Factor", "Multiplier of the first Supertrend", "Supertrend 1");

		_atrPeriod2 = Param(nameof(ATRPeriod2), 20)
			.SetGreaterThanZero()
			.SetDisplay("ST2 Period", "ATR period of the second Supertrend", "Supertrend 2");

		_factor2 = Param(nameof(Factor2), 5.0m)
			.SetGreaterThanZero()
			.SetDisplay("ST2 Factor", "Multiplier of the second Supertrend", "Supertrend 2");

		_direction = Param(nameof(Direction), "Long")
			.SetDisplay("Direction", "Allowed trade direction: Long, Short or Both", "Trading");

		_tpType = Param(nameof(TPType), "Supertrend")
			.SetDisplay("TP Type", "Take-profit type: Supertrend or Percent", "Risk");

		_tpPercent = Param(nameof(TPPercent), 1.5m)
			.SetNotNegative()
			.SetDisplay("TP %", "Take-profit percentage for the Percent type", "Risk");

		_slPercent = Param(nameof(SLPercent), 10.0m)
			.SetNotNegative()
			.SetDisplay("SL %", "Stop-loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevAboveBoth = null;
		_prevBelowBoth = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevAboveBoth = null;
		_prevBelowBoth = null;

		var st1 = new SuperTrend { Length = ATRPeriod1, Multiplier = Factor1 };
		var st2 = new SuperTrend { Length = ATRPeriod2, Multiplier = Factor2 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(st1, st2, ProcessCandle)
			.Start();

		var takeProfit = TPType.EqualsIgnoreCase("Percent") && TPPercent > 0
			? new Unit(TPPercent, UnitTypes.Percent)
			: new Unit();
		var stopLoss = SLPercent > 0 ? new Unit(SLPercent, UnitTypes.Percent) : new Unit();
		StartProtection(takeProfit, stopLoss, useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, st1);
			DrawIndicator(area, st2);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue st1Value, IIndicatorValue st2Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!st1Value.IsFormed || !st2Value.IsFormed)
			return;

		var line1 = st1Value.ToDecimal();
		var line2 = st2Value.ToDecimal();
		var close = candle.ClosePrice;

		var aboveBoth = close > line1 && close > line2;
		var belowBoth = close < line1 && close < line2;

		var prevAbove = _prevAboveBoth;
		var prevBelow = _prevBelowBoth;
		_prevAboveBoth = aboveBoth;
		_prevBelowBoth = belowBoth;

		if (prevAbove is not bool wasAbove || prevBelow is not bool wasBelow)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var allowLong = !Direction.EqualsIgnoreCase("Short");
		var allowShort = !Direction.EqualsIgnoreCase("Long");
		var trailOnSecond = TPType.EqualsIgnoreCase("Supertrend");

		var longSignal = allowLong && aboveBoth && !wasAbove;
		var shortSignal = allowShort && belowBoth && !wasBelow;

		if (longSignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && (close < line1 || (trailOnSecond && close < line2)))
			SellMarket(Position);
		else if (Position < 0 && (close > line1 || (trailOnSecond && close > line2)))
			BuyMarket(-Position);
	}
}
