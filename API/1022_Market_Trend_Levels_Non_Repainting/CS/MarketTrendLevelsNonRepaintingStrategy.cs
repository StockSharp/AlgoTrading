using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with optional RSI filter.
/// </summary>
public class MarketTrendLevelsNonRepaintingStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<TradeDirection> _tradeDirection;
	private readonly StrategyParam<bool> _applyExitFilters;
	private readonly StrategyParam<bool> _useRsi;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiLongThreshold;
	private readonly StrategyParam<decimal> _rsiShortThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _emaFast;
	private ExponentialMovingAverage _emaSlow;
	private RelativeStrengthIndex _rsi;
	private decimal? _prevDiff;

	/// <summary>Fast EMA period.</summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

	/// <summary>Slow EMA period.</summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

	/// <summary>Allowed trading direction.</summary>
	public TradeDirection TradeDirection { get => _tradeDirection.Value; set => _tradeDirection.Value = value; }

	/// <summary>Apply exit filters on open positions.</summary>
	public bool ApplyExitFilters { get => _applyExitFilters.Value; set => _applyExitFilters.Value = value; }

	/// <summary>Enable RSI filter.</summary>
	public bool UseRsi { get => _useRsi.Value; set => _useRsi.Value = value; }

	/// <summary>RSI period.</summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>RSI long threshold.</summary>
	public decimal RsiLongThreshold { get => _rsiLongThreshold.Value; set => _rsiLongThreshold.Value = value; }

	/// <summary>RSI short threshold.</summary>
	public decimal RsiShortThreshold { get => _rsiShortThreshold.Value; set => _rsiShortThreshold.Value = value; }

	/// <summary>Candle type for strategy.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public enum TradeDirection
	{
		LongOnly,
		ShortOnly,
		Both
	}

	public MarketTrendLevelsNonRepaintingStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Trend");

		_slowLength = Param(nameof(SlowLength), 25)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Trend");

		_tradeDirection = Param(nameof(TradeDirection), TradeDirection.Both)
			.SetDisplay("Trade Direction", "Allowed direction", "Trade");

		_applyExitFilters = Param(nameof(ApplyExitFilters), false)
			.SetDisplay("Apply Exit Filters", "Close positions when filters fail", "Filters");

		_useRsi = Param(nameof(UseRsi), false)
			.SetDisplay("RSI Filter", "Enable RSI filter", "RSI");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_rsiLongThreshold = Param(nameof(RsiLongThreshold), 50m)
			.SetDisplay("RSI Long Threshold", "Threshold for long", "RSI");

		_rsiShortThreshold = Param(nameof(RsiShortThreshold), 50m)
			.SetDisplay("RSI Short Threshold", "Threshold for short", "RSI");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevDiff = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_emaFast = new ExponentialMovingAverage { Length = FastLength };
		_emaSlow = new ExponentialMovingAverage { Length = SlowLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_emaFast, _emaSlow, _rsi, Process)
			.Start();
	}

	private void Process(ICandleMessage candle, decimal fast, decimal slow, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var diff = fast - slow;
		var crossUp = _prevDiff <= 0 && diff > 0;
		var crossDown = _prevDiff >= 0 && diff < 0;
		_prevDiff = diff;

		var filterLong = !UseRsi || rsiValue > RsiLongThreshold;
		var filterShort = !UseRsi || rsiValue < RsiShortThreshold;

		var volume = Volume + Math.Abs(Position);

		if (TradeDirection == TradeDirection.Both)
		{
			if (crossUp && Position <= 0 && filterLong)
				BuyMarket(volume);
			if (crossDown && Position >= 0 && filterShort)
				SellMarket(volume);

			if (ApplyExitFilters)
			{
				if (Position > 0 && !filterLong)
					SellMarket(Position);
				else if (Position < 0 && !filterShort)
					BuyMarket(Math.Abs(Position));
			}
		}
		else if (TradeDirection == TradeDirection.LongOnly)
		{
			if (crossUp && Position <= 0 && filterLong)
				BuyMarket(volume);
			if ((crossDown || (ApplyExitFilters && !filterLong)) && Position > 0)
				SellMarket(Position);
		}
		else if (TradeDirection == TradeDirection.ShortOnly)
		{
			if (crossDown && Position >= 0 && filterShort)
				SellMarket(volume);
			if ((crossUp || (ApplyExitFilters && !filterShort)) && Position < 0)
				BuyMarket(Math.Abs(Position));
		}
	}
}
