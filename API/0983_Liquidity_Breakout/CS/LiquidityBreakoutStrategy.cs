using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Liquidity breakout strategy.
/// Buys when price breaks above recent range high and sells when price breaks below range low.
/// Optional stop loss can use SuperTrend or fixed percentage.
/// </summary>
public class LiquidityBreakoutStrategy : Strategy
{
private readonly StrategyParam<int> _pivotLength;
private readonly StrategyParam<Sides?> _direction;
private readonly StrategyParam<StopLossModes> _stopLossMode;
	private readonly StrategyParam<decimal> _fixedPercentage;
	private readonly StrategyParam<int> _superTrendPeriod;
	private readonly StrategyParam<decimal> _superTrendMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private SuperTrend _superTrend;

	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _initialized;

/// <summary>Stop loss calculation mode.</summary>
public enum StopLossModes
	{
		SuperTrend,
		FixedPercentage,
		None,
	}

public int PivotLength { get => _pivotLength.Value; set => _pivotLength.Value = value; }
public Sides? Direction { get => _direction.Value; set => _direction.Value = value; }
public StopLossModes StopLoss { get => _stopLossMode.Value; set => _stopLossMode.Value = value; }
	public decimal FixedPercentage { get => _fixedPercentage.Value; set => _fixedPercentage.Value = value; }
	public int SuperTrendPeriod { get => _superTrendPeriod.Value; set => _superTrendPeriod.Value = value; }
	public decimal SuperTrendMultiplier { get => _superTrendMultiplier.Value; set => _superTrendMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LiquidityBreakoutStrategy()
	{
		_pivotLength = Param(nameof(PivotLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Contraction Lookback", "Bars for range detection", "General");
	 _direction = Param(nameof(Direction), null)
	        .SetDisplay("Trade Direction", "Allowed trade direction", "General");

		_stopLossMode = Param(nameof(StopLoss), StopLossModes.SuperTrend)
			.SetDisplay("Stop Loss Type", "Stop loss mode", "Risk");

		_fixedPercentage = Param(nameof(FixedPercentage), 0.1m)
			.SetDisplay("Fixed %", "Stop loss percentage", "Risk");

		_superTrendPeriod = Param(nameof(SuperTrendPeriod), 10)
			.SetDisplay("SuperTrend Length", "ATR period for SuperTrend", "SuperTrend");

		_superTrendMultiplier = Param(nameof(SuperTrendMultiplier), 3m)
			.SetDisplay("SuperTrend Mult", "ATR multiplier for SuperTrend", "SuperTrend");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_initialized = false;
		_prevHigh = _prevLow = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = PivotLength };
		_lowest = new Lowest { Length = PivotLength };
		_superTrend = new SuperTrend { Length = SuperTrendPeriod, Multiplier = SuperTrendMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_highest, _lowest, _superTrend, ProcessCandle)
			.Start();

		if (StopLoss == StopLossModes.FixedPercentage)
			StartProtection(stopLoss: new Unit(FixedPercentage, UnitTypes.Percent));
		else
			StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawIndicator(area, _superTrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highValue, decimal lowValue, IIndicatorValue stValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var st = (SuperTrendIndicatorValue)stValue;

		if (!_initialized)
		{
			_prevHigh = highValue;
			_prevLow = lowValue;
			_initialized = true;
			return;
		}
	 var allowLong = Direction is null || Direction == Sides.Buy;
	var allowShort = Direction is null || Direction == Sides.Sell;

		var longEntry = allowLong && candle.ClosePrice > _prevHigh;
		var shortEntry = allowShort && candle.ClosePrice < _prevLow;

		var exitLong = shortEntry;
		var exitShort = longEntry;

		if (StopLoss == StopLossModes.SuperTrend)
		{
			if (Position > 0 && candle.ClosePrice < st.Value)
				exitLong = true;
			else if (Position < 0 && candle.ClosePrice > st.Value)
				exitShort = true;
		}

		if (longEntry && Position <= 0)
			BuyMarket();

		if (shortEntry && Position >= 0)
			SellMarket();

		if (Position > 0 && exitLong)
			SellMarket();

		if (Position < 0 && exitShort)
			BuyMarket();

		_prevHigh = highValue;
		_prevLow = lowValue;
	}
}
