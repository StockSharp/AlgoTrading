using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bitcoin breakout strategy based on 1H range and 15M close.
/// </summary>
public class Bitcoin1H15MBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _lowerCandleType;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<decimal> _stopLossBuffer;
	private readonly StrategyParam<decimal> _riskRewardRatio;

	private decimal _rangeHigh;
	private decimal _rangeLow;

	/// <summary>
	/// Lower timeframe for entry calculations.
	/// </summary>
	public DataType LowerCandleType
	{
		get => _lowerCandleType.Value;
		set => _lowerCandleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used to define breakout range.
	/// </summary>
	public DataType HigherCandleType
	{
		get => _higherCandleType.Value;
		set => _higherCandleType.Value = value;
	}

	/// <summary>
	/// Stop loss buffer in price units.
	/// </summary>
	public decimal StopLossBuffer
	{
		get => _stopLossBuffer.Value;
		set => _stopLossBuffer.Value = value;
	}

	/// <summary>
	/// Risk-reward ratio for take profit calculation.
	/// </summary>
	public decimal RiskRewardRatio
	{
		get => _riskRewardRatio.Value;
		set => _riskRewardRatio.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public Bitcoin1H15MBreakoutStrategy()
	{
		_lowerCandleType = Param(nameof(LowerCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Lower TF", "Lower timeframe for entries", "General");

		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Higher TF", "Higher timeframe for range", "General");

		_stopLossBuffer = Param(nameof(StopLossBuffer), 50m)
			.SetGreaterThanZero()
			.SetDisplay("SL Buffer", "Stop loss buffer in price units", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 100m, 10m);

		_riskRewardRatio = Param(nameof(RiskRewardRatio), 2m)
			.SetGreaterThanZero()
			.SetDisplay("RRR", "Risk-reward ratio", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var lowerSub = SubscribeCandles(LowerCandleType);
		lowerSub.Bind(OnProcessLower).Start();

		var higherSub = SubscribeCandles(HigherCandleType);
		higherSub.Bind(OnProcessHigher).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, lowerSub);
			DrawOwnTrades(area);
		}

		StartProtection(
			new Unit(StopLossBuffer * RiskRewardRatio, UnitTypes.Absolute),
			new Unit(StopLossBuffer, UnitTypes.Absolute));
	}

	private void OnProcessHigher(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_rangeHigh = candle.HighPrice;
		_rangeLow = candle.LowPrice;
	}

	private void OnProcessLower(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_rangeHigh == 0 && _rangeLow == 0)
			return;

		var close = candle.ClosePrice;

		if (close > _rangeHigh && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (close < _rangeLow && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
