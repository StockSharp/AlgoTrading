using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VWAP Reversion strategy that trades on deviations from Volume Weighted Average Price.
/// Opens positions when price deviates from VWAP and exits when price returns.
/// </summary>
public class VwapReversionStrategy : Strategy
{
	private readonly StrategyParam<decimal> _deviationPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private int _cooldown;

	/// <summary>
	/// Deviation percentage from VWAP required for entry.
	/// </summary>
	public decimal DeviationPercent
	{
		get => _deviationPercent.Value;
		set => _deviationPercent.Value = value;
	}

	/// <summary>
	/// Type of candles used for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initialize the VWAP Reversion strategy.
	/// </summary>
	public VwapReversionStrategy()
	{
		_deviationPercent = Param(nameof(DeviationPercent), 0.5m)
			.SetDisplay("Deviation %", "Deviation from VWAP for entry", "Entry")
			.SetOptimize(0.2m, 2.0m, 0.2m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 500)
			.SetRange(1, 1000)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "General");
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
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_cooldown = 0;

		var vwap = new VolumeWeightedMovingAverage();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(vwap, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, vwap);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal vwapValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (vwapValue <= 0)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var deviationRatio = (candle.ClosePrice - vwapValue) / vwapValue;
		var threshold = DeviationPercent / 100m;

		if (Position == 0)
		{
			if (deviationRatio < -threshold)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (deviationRatio > threshold)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0)
		{
			if (candle.ClosePrice >= vwapValue)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice <= vwapValue)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
		}
	}
}
