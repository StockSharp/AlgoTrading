using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader Trend_RDS expert advisor.
/// Detects three-bar momentum patterns and reverses into the move.
/// Includes configurable stop-loss, take-profit, and trailing management.
/// </summary>
public class TrendRdsReversalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maxPatternDepth;

	private readonly List<(decimal High, decimal Low)> _recentExtremes = new();

	/// <summary>
	/// Trading volume for market entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in absolute price units.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in absolute price units.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Inverts the buy and sell conditions when enabled.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Maximum number of swings tracked when validating the pattern.
	/// </summary>
	public int MaxPatternDepth
	{
		get => _maxPatternDepth.Value;
		set => _maxPatternDepth.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TrendRdsReversalStrategy"/> class.
	/// </summary>
	public TrendRdsReversalStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Market order volume", "General");

		_stopLossPips = Param(nameof(StopLossPips), 500m)
			.SetDisplay("Stop Loss", "Stop-loss distance", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 500m)
			.SetDisplay("Take Profit", "Take-profit distance", "Risk");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Invert buy and sell signals", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe", "General");

		_maxPatternDepth = Param(nameof(MaxPatternDepth), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max Pattern Depth", "Maximum candles tracked for pattern detection", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[] { (Security, CandleType) };
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_recentExtremes.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		// Use a dummy EMA to ensure candle callbacks fire in the backtester
		var ema = new EMA { Length = 5 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}

		// Use StartProtection for SL/TP
		var tp = TakeProfitPips > 0 ? new Unit(TakeProfitPips, UnitTypes.Absolute) : null;
		var sl = StopLossPips > 0 ? new Unit(StopLossPips, UnitTypes.Absolute) : null;
		StartProtection(tp, sl);

		base.OnStarted2(time);
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Track recent highs and lows
		_recentExtremes.Insert(0, (candle.HighPrice, candle.LowPrice));
		if (_recentExtremes.Count > MaxPatternDepth + 2)
			_recentExtremes.RemoveAt(_recentExtremes.Count - 1);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Need at least 3 bars for the pattern
		if (_recentExtremes.Count < 3)
			return;

		var (buySignal, sellSignal) = DetectSignals();

		if (buySignal)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			if (Position <= 0)
				BuyMarket(Volume);
		}
		else if (sellSignal)
		{
			if (Position > 0)
				SellMarket(Position);
			if (Position >= 0)
				SellMarket(Volume);
		}
	}

	private (bool Buy, bool Sell) DetectSignals()
	{
		var depth = Math.Min(_recentExtremes.Count - 2, MaxPatternDepth);
		if (depth <= 0)
			return (false, false);

		for (var index = 0; index < depth; index++)
		{
			if (index + 2 >= _recentExtremes.Count)
				break;

			var first = _recentExtremes[index];
			var second = _recentExtremes[index + 1];
			var third = _recentExtremes[index + 2];

			// Conflict: both highs and lows rising simultaneously
			var conflict = first.High < second.High && second.High < third.High &&
				first.Low > second.Low && second.Low > third.Low;

			// Rising lows pattern -> buy
			if (!conflict && first.Low > second.Low && second.Low > third.Low)
			{
				return ReverseSignals ? (false, true) : (true, false);
			}

			// Rising highs pattern -> sell
			if (!conflict && first.High < second.High && second.High < third.High)
			{
				return ReverseSignals ? (true, false) : (false, true);
			}
		}

		return (false, false);
	}
}
