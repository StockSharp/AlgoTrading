using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class LiquidityBreakoutStrategy : Strategy
{
	public enum StopLossModes { SuperTrend, FixedPercentage, None }
	public enum TradeDirections { Both, LongOnly, ShortOnly }

	private readonly StrategyParam<int> _pivotLength;
	private readonly StrategyParam<TradeDirections> _direction;
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

	public int PivotLength { get => _pivotLength.Value; set => _pivotLength.Value = value; }
	public TradeDirections Direction { get => _direction.Value; set => _direction.Value = value; }
	public StopLossModes StopLoss { get => _stopLossMode.Value; set => _stopLossMode.Value = value; }
	public decimal FixedPercentage { get => _fixedPercentage.Value; set => _fixedPercentage.Value = value; }
	public int SuperTrendPeriod { get => _superTrendPeriod.Value; set => _superTrendPeriod.Value = value; }
	public decimal SuperTrendMultiplier { get => _superTrendMultiplier.Value; set => _superTrendMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LiquidityBreakoutStrategy()
	{
		_pivotLength = Param(nameof(PivotLength), 12).SetGreaterThanZero().SetDisplay("Lookback", "Bars for range", "General");
		_direction = Param(nameof(Direction), TradeDirections.Both).SetDisplay("Direction", "Trade direction", "General");
		_stopLossMode = Param(nameof(StopLoss), StopLossModes.SuperTrend).SetDisplay("SL Type", "Stop loss mode", "Risk");
		_fixedPercentage = Param(nameof(FixedPercentage), 0.1m).SetDisplay("Fixed %", "SL percentage", "Risk");
		_superTrendPeriod = Param(nameof(SuperTrendPeriod), 10).SetDisplay("ST Length", "ATR period", "SuperTrend");
		_superTrendMultiplier = Param(nameof(SuperTrendMultiplier), 3m).SetDisplay("ST Mult", "ATR multiplier", "SuperTrend");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_initialized = false;
		_prevHigh = _prevLow = default;

		_highest = new Highest { Length = PivotLength };
		_lowest = new Lowest { Length = PivotLength };
		_superTrend = new SuperTrend { Length = SuperTrendPeriod, Multiplier = SuperTrendMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_highest, _lowest, _superTrend, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _superTrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highValue, decimal lowValue, decimal stValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		if (!_initialized)
		{
			_prevHigh = highValue;
			_prevLow = lowValue;
			_initialized = true;
			return;
		}

		var allowLong = Direction != TradeDirections.ShortOnly;
		var allowShort = Direction != TradeDirections.LongOnly;

		var longEntry = allowLong && candle.ClosePrice > _prevHigh;
		var shortEntry = allowShort && candle.ClosePrice < _prevLow;

		var exitLong = shortEntry;
		var exitShort = longEntry;

		if (StopLoss == StopLossModes.SuperTrend && _superTrend.IsFormed && stValue > 0)
		{
			if (Position > 0 && candle.ClosePrice < stValue)
				exitLong = true;
			else if (Position < 0 && candle.ClosePrice > stValue)
				exitShort = true;
		}

		if (StopLoss == StopLossModes.FixedPercentage && _prevHigh > 0)
		{
			if (Position > 0)
			{
				var stopPrice = _prevHigh * (1m - FixedPercentage / 100m);
				if (candle.ClosePrice < stopPrice) exitLong = true;
			}
			else if (Position < 0)
			{
				var stopPrice = _prevLow * (1m + FixedPercentage / 100m);
				if (candle.ClosePrice > stopPrice) exitShort = true;
			}
		}

		if (longEntry && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortEntry && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && exitLong)
			SellMarket(Math.Abs(Position));
		else if (Position < 0 && exitShort)
			BuyMarket(Math.Abs(Position));

		_prevHigh = highValue;
		_prevLow = lowValue;
	}
}
