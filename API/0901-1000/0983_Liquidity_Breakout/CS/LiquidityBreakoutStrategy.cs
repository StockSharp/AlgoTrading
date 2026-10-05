using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Liquidity Breakout strategy.
/// The range is bounded by the latest pivot high and pivot low, each confirmed by PivotLength candles on both sides. A close crossing
/// above the range high goes long and a close crossing below the range low goes short, reversing an opposite position. Direction limits
/// the allowed side: a breakout of the disabled side only closes the position. The stop is either the SuperTrend line (exit on a close
/// beyond it) or a fixed percentage from the entry price.
/// </summary>
public class LiquidityBreakoutStrategy : Strategy
{
	/// <summary>
	/// Stop loss types.
	/// </summary>
	public enum StopLossModes
	{
		/// <summary>
		/// No stop loss.
		/// </summary>
		None,

		/// <summary>
		/// Exit when the close crosses the SuperTrend line.
		/// </summary>
		SuperTrend,

		/// <summary>
		/// Fixed percentage from the entry price.
		/// </summary>
		FixedPercentage,
	}

	/// <summary>
	/// Allowed trade directions.
	/// </summary>
	public enum TradeDirections
	{
		/// <summary>
		/// Long and short trades.
		/// </summary>
		Both,

		/// <summary>
		/// Long trades only.
		/// </summary>
		Long,

		/// <summary>
		/// Short trades only.
		/// </summary>
		Short,
	}

	private readonly StrategyParam<int> _pivotLength;
	private readonly StrategyParam<StopLossModes> _stopLoss;
	private readonly StrategyParam<decimal> _fixedPercentage;
	private readonly StrategyParam<int> _superTrendPeriod;
	private readonly StrategyParam<decimal> _superTrendMultiplier;
	private readonly StrategyParam<TradeDirections> _direction;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private decimal? _rangeHigh;
	private decimal? _rangeLow;
	private decimal? _prevClose;

	/// <summary>
	/// Candles on each side that confirm a pivot.
	/// </summary>
	public int PivotLength
	{
		get => _pivotLength.Value;
		set => _pivotLength.Value = value;
	}

	/// <summary>
	/// Stop loss type.
	/// </summary>
	public StopLossModes StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Fixed stop loss percentage from the entry price.
	/// </summary>
	public decimal FixedPercentage
	{
		get => _fixedPercentage.Value;
		set => _fixedPercentage.Value = value;
	}

	/// <summary>
	/// ATR period of SuperTrend.
	/// </summary>
	public int SuperTrendPeriod
	{
		get => _superTrendPeriod.Value;
		set => _superTrendPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier of SuperTrend.
	/// </summary>
	public decimal SuperTrendMultiplier
	{
		get => _superTrendMultiplier.Value;
		set => _superTrendMultiplier.Value = value;
	}

	/// <summary>
	/// Allowed trade directions.
	/// </summary>
	public TradeDirections Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
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
	public LiquidityBreakoutStrategy()
	{
		_pivotLength = Param(nameof(PivotLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Pivot Length", "Candles on each side that confirm a pivot", "Range");

		_stopLoss = Param(nameof(StopLoss), StopLossModes.SuperTrend)
			.SetDisplay("Stop Loss", "Stop loss type", "Risk");

		_fixedPercentage = Param(nameof(FixedPercentage), 0.1m)
			.SetNotNegative()
			.SetDisplay("Fixed Percentage", "Fixed stop loss percentage from the entry price", "Risk");

		_superTrendPeriod = Param(nameof(SuperTrendPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("SuperTrend Period", "ATR period of SuperTrend", "Risk");

		_superTrendMultiplier = Param(nameof(SuperTrendMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("SuperTrend Multiplier", "ATR multiplier of SuperTrend", "Risk");

		_direction = Param(nameof(Direction), TradeDirections.Both)
			.SetDisplay("Direction", "Allowed trade directions", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
		ResetState();
	}

	private void ResetState()
	{
		_highs.Clear();
		_lows.Clear();
		_rangeHigh = null;
		_rangeLow = null;
		_prevClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var superTrend = new SuperTrend { Length = SuperTrendPeriod, Multiplier = SuperTrendMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(superTrend, ProcessCandle)
			.Start();

		if (StopLoss == StopLossModes.FixedPercentage && FixedPercentage > 0)
			StartProtection(new Unit(), new Unit(FixedPercentage, UnitTypes.Percent), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, superTrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue superTrendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Breakouts are measured against the range known before this candle.
		var rangeHigh = _rangeHigh;
		var rangeLow = _rangeLow;
		var prevClose = _prevClose;
		_prevClose = candle.ClosePrice;

		UpdatePivots(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (StopLoss == StopLossModes.SuperTrend && superTrendValue.IsFormed)
		{
			var line = superTrendValue.GetValue<decimal>();

			if (Position > 0 && close < line)
			{
				SellMarket(Position);
				return;
			}

			if (Position < 0 && close > line)
			{
				BuyMarket(-Position);
				return;
			}
		}

		if (prevClose is not decimal lastClose)
			return;

		var longBreakout = rangeHigh is decimal high && lastClose <= high && close > high;
		var shortBreakout = rangeLow is decimal low && lastClose >= low && close < low;

		if (longBreakout)
		{
			if (Direction != TradeDirections.Short && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (Direction == TradeDirections.Short && Position < 0)
				BuyMarket(-Position);
		}
		else if (shortBreakout)
		{
			if (Direction != TradeDirections.Long && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			else if (Direction == TradeDirections.Long && Position > 0)
				SellMarket(Position);
		}
	}

	private void UpdatePivots(ICandleMessage candle)
	{
		var size = PivotLength * 2 + 1;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		if (_highs.Count > size)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (_highs.Count < size)
			return;

		// The middle candle is a pivot when no candle within PivotLength on either side exceeds it.
		var centerHigh = _highs[PivotLength];
		if (centerHigh >= _highs.Max())
			_rangeHigh = centerHigh;

		var centerLow = _lows[PivotLength];
		if (centerLow <= _lows.Min())
			_rangeLow = centerLow;
	}
}
