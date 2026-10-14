using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Liquidity Engulfment strategy.
/// Upper liquidity is the highest high of the previous UpperLookback candles and lower liquidity the lowest low of the previous
/// LowerLookback candles. Once a candle touches lower liquidity, the next bullish engulfing candle opens a long; once a candle touches
/// upper liquidity, the next bearish engulfing candle opens a short. Mode restricts the allowed side: a signal of the disabled side only
/// closes the open position. Stop loss and take profit are set in pips (price steps).
/// </summary>
public class LiquidityEngulfmentStrategy : Strategy
{
	/// <summary>
	/// Allowed trade directions.
	/// </summary>
	public enum TradeModes
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

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _upperLookback;
	private readonly StrategyParam<int> _lowerLookback;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<TradeModes> _mode;

	private decimal? _prevHighest;
	private decimal? _prevLowest;
	private ICandleMessage _prevCandle;
	private bool _upperTouched;
	private bool _lowerTouched;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Candles that define upper liquidity.
	/// </summary>
	public int UpperLookback
	{
		get => _upperLookback.Value;
		set => _upperLookback.Value = value;
	}

	/// <summary>
	/// Candles that define lower liquidity.
	/// </summary>
	public int LowerLookback
	{
		get => _lowerLookback.Value;
		set => _lowerLookback.Value = value;
	}

	/// <summary>
	/// Stop loss in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit in pips, 0 disables it.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Allowed trade directions.
	/// </summary>
	public TradeModes Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public LiquidityEngulfmentStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_upperLookback = Param(nameof(UpperLookback), 10)
			.SetGreaterThanZero()
			.SetDisplay("Upper Lookback", "Candles that define upper liquidity", "Liquidity");

		_lowerLookback = Param(nameof(LowerLookback), 10)
			.SetGreaterThanZero()
			.SetDisplay("Lower Lookback", "Candles that define lower liquidity", "Liquidity");

		_stopLossPips = Param(nameof(StopLossPips), 10m)
			.SetNotNegative()
			.SetDisplay("Stop Loss Pips", "Stop loss in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 20m)
			.SetNotNegative()
			.SetDisplay("Take Profit Pips", "Take profit in pips, 0 disables it", "Risk");

		_mode = Param(nameof(Mode), TradeModes.Both)
			.SetDisplay("Mode", "Allowed trade directions", "General");
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
		_prevHighest = null;
		_prevLowest = null;
		_prevCandle = null;
		_upperTouched = false;
		_lowerTouched = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var highest = new Highest { Length = UpperLookback };
		var lowest = new Lowest { Length = LowerLookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(highest, lowest, ProcessCandle)
			.Start();

		var step = Security?.PriceStep ?? 1m;
		StartProtection(
			TakeProfitPips > 0m ? new Unit(TakeProfitPips * step, UnitTypes.Absolute) : new Unit(),
			StopLossPips > 0m ? new Unit(StopLossPips * step, UnitTypes.Absolute) : new Unit(),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue highestValue, IIndicatorValue lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Liquidity levels come from the candles before this one.
		var upper = _prevHighest;
		var lower = _prevLowest;
		var prev = _prevCandle;

		_prevHighest = highestValue.IsFormed ? highestValue.GetValue<decimal>() : null;
		_prevLowest = lowestValue.IsFormed ? lowestValue.GetValue<decimal>() : null;
		_prevCandle = candle;

		if (upper is decimal upperLevel && candle.HighPrice >= upperLevel)
			_upperTouched = true;

		if (lower is decimal lowerLevel && candle.LowPrice <= lowerLevel)
			_lowerTouched = true;

		if (prev == null)
			return;

		var bullishEngulfing = prev.ClosePrice < prev.OpenPrice && candle.ClosePrice > candle.OpenPrice &&
			candle.OpenPrice <= prev.ClosePrice && candle.ClosePrice >= prev.OpenPrice;
		var bearishEngulfing = prev.ClosePrice > prev.OpenPrice && candle.ClosePrice < candle.OpenPrice &&
			candle.OpenPrice >= prev.ClosePrice && candle.ClosePrice <= prev.OpenPrice;

		var longSignal = _lowerTouched && bullishEngulfing;
		var shortSignal = _upperTouched && bearishEngulfing;

		if (longSignal)
			_lowerTouched = false;

		if (shortSignal)
			_upperTouched = false;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (longSignal)
		{
			if (Mode != TradeModes.Short && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (Mode == TradeModes.Short && Position < 0)
				BuyMarket(-Position);
		}
		else if (shortSignal)
		{
			if (Mode != TradeModes.Long && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			else if (Mode == TradeModes.Long && Position > 0)
				SellMarket(Position);
		}
	}
}
