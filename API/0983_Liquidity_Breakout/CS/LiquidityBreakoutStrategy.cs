using System;
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
	private readonly StrategyParam<decimal> _breakoutBufferPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private SuperTrend _superTrend;
	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _initialized;
	private int _barsFromTrade;

	public int PivotLength { get => _pivotLength.Value; set => _pivotLength.Value = value; }
	public TradeDirections Direction { get => _direction.Value; set => _direction.Value = value; }
	public StopLossModes StopLoss { get => _stopLossMode.Value; set => _stopLossMode.Value = value; }
	public decimal FixedPercentage { get => _fixedPercentage.Value; set => _fixedPercentage.Value = value; }
	public int SuperTrendPeriod { get => _superTrendPeriod.Value; set => _superTrendPeriod.Value = value; }
	public decimal SuperTrendMultiplier { get => _superTrendMultiplier.Value; set => _superTrendMultiplier.Value = value; }
	public decimal BreakoutBufferPercent { get => _breakoutBufferPercent.Value; set => _breakoutBufferPercent.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LiquidityBreakoutStrategy()
	{
		_pivotLength = Param(nameof(PivotLength), 14).SetGreaterThanZero().SetDisplay("Lookback", "Bars for range", "General");
		_direction = Param(nameof(Direction), TradeDirections.Both).SetDisplay("Direction", "Trade direction", "General");
		_stopLossMode = Param(nameof(StopLoss), StopLossModes.SuperTrend).SetDisplay("SL Type", "Stop loss mode", "Risk");
		_fixedPercentage = Param(nameof(FixedPercentage), 0.1m).SetDisplay("Fixed %", "SL percentage", "Risk");
		_superTrendPeriod = Param(nameof(SuperTrendPeriod), 10).SetDisplay("ST Length", "ATR period", "SuperTrend");
		_superTrendMultiplier = Param(nameof(SuperTrendMultiplier), 3m).SetDisplay("ST Mult", "ATR multiplier", "SuperTrend");
		_breakoutBufferPercent = Param(nameof(BreakoutBufferPercent), 0.05m).SetGreaterThanZero().SetDisplay("Breakout Buffer %", "Breakout confirmation buffer", "General");
		_cooldownBars = Param(nameof(CooldownBars), 8).SetGreaterThanZero().SetDisplay("Cooldown Bars", "Bars between trade actions", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_highest = null;
		_lowest = null;
		_superTrend = null;
		_prevHigh = 0m;
		_prevLow = 0m;
		_initialized = false;
		_barsFromTrade = int.MaxValue;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_initialized = false;
		_prevHigh = _prevLow = default;
		_barsFromTrade = int.MaxValue;

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

		_barsFromTrade++;

		var allowLong = Direction != TradeDirections.ShortOnly;
		var allowShort = Direction != TradeDirections.LongOnly;
		var canTradeNow = _barsFromTrade >= CooldownBars;

		var upperBreakout = _prevHigh * (1m + BreakoutBufferPercent / 100m);
		var lowerBreakout = _prevLow * (1m - BreakoutBufferPercent / 100m);

		var longEntry = allowLong && candle.ClosePrice > upperBreakout;
		var shortEntry = allowShort && candle.ClosePrice < lowerBreakout;

		var exitLong = false;
		var exitShort = false;

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

		if (canTradeNow)
		{
			if (Position > 0 && shortEntry)
				exitLong = true;
			else if (Position < 0 && longEntry)
				exitShort = true;
		}

		if (canTradeNow)
		{
			if (Position == 0)
			{
				if (longEntry)
				{
					BuyMarket(Volume);
					_barsFromTrade = 0;
				}
				else if (shortEntry)
				{
					SellMarket(Volume);
					_barsFromTrade = 0;
				}
			}
			else if (Position > 0 && exitLong)
			{
				SellMarket(Math.Abs(Position));
				_barsFromTrade = 0;
			}
			else if (Position < 0 && exitShort)
			{
				BuyMarket(Math.Abs(Position));
				_barsFromTrade = 0;
			}
		}

		_prevHigh = highValue;
		_prevLow = lowValue;
	}
}
