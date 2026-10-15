using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Balance of Power strategy.
/// Long only: buys when Balance of Power crosses above Threshold and closes the long when it crosses below -Threshold.
/// </summary>
public class BalanceOfPowerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevBop;

	/// <summary>
	/// Balance of Power level whose upward cross opens a long.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
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
	public BalanceOfPowerStrategy()
	{
		_threshold = Param(nameof(Threshold), 0.8m)
			.SetNotNegative()
			.SetDisplay("Threshold", "Balance of Power level whose upward cross opens a long", "Indicators");

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
		_prevBop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevBop = null;

		var bop = new BalanceOfPower();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bop, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, bop);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bopValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// A candle without range has no Balance of Power value.
		if (!bopValue.IsFormed || bopValue.IsEmpty)
			return;

		var bop = bopValue.GetValue<decimal>();
		var prev = _prevBop;
		_prevBop = bop;

		if (prev is not decimal prevBop)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0 && prevBop <= Threshold && bop > Threshold)
			BuyMarket(Volume);
		else if (Position > 0 && prevBop >= -Threshold && bop < -Threshold)
			SellMarket(Position);
	}
}
