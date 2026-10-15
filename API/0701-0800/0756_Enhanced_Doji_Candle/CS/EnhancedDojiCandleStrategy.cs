using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Enhanced doji candle strategy.
/// A doji has a body of at most 30% of its range. When flat, a doji goes long if it is bullish with a lower wick of at most 1% of
/// its range or the previous candle was bullish, and short if it is bearish with an upper wick of at most 1% or the previous
/// candle was bearish. While a position is open any new doji closes it. The stop lies StopLossPips price steps from the entry and
/// the take profit RiskRewardRatio times that distance. The SMA is drawn for reference.
/// </summary>
public class EnhancedDojiCandleStrategy : Strategy
{
	private const decimal _dojiBodyPercent = 30m;
	private const decimal _wickPercent = 1m;

	private readonly StrategyParam<decimal> _riskRewardRatio;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _prevCandle;

	/// <summary>
	/// Take profit distance in multiples of the stop distance.
	/// </summary>
	public decimal RiskRewardRatio
	{
		get => _riskRewardRatio.Value;
		set => _riskRewardRatio.Value = value;
	}

	/// <summary>
	/// Stop loss in price steps.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// SMA period.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
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
	public EnhancedDojiCandleStrategy()
	{
		_riskRewardRatio = Param(nameof(RiskRewardRatio), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Reward", "Take profit in multiples of the stop distance", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 5)
			.SetNotNegative()
			.SetDisplay("Stop Loss Pips", "Stop loss in price steps", "Risk");

		_smaPeriod = Param(nameof(SmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "SMA period", "Indicators");

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
		_prevCandle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevCandle = null;

		var sma = new SimpleMovingAverage { Length = SmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var stopDistance = StopLossPips * (Security?.PriceStep ?? 1m);
		StartProtection(
			stopDistance > 0m ? new Unit(stopDistance * RiskRewardRatio, UnitTypes.Absolute) : new Unit(),
			stopDistance > 0m ? new Unit(stopDistance, UnitTypes.Absolute) : new Unit(),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prev = _prevCandle;
		_prevCandle = candle;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var range = candle.HighPrice - candle.LowPrice;
		if (range <= 0m)
			return;

		var open = candle.OpenPrice;
		var close = candle.ClosePrice;
		var isDoji = Math.Abs(close - open) / range * 100m <= _dojiBodyPercent;

		if (!isDoji)
			return;

		if (Position > 0)
		{
			SellMarket(Position);
			return;
		}

		if (Position < 0)
		{
			BuyMarket(-Position);
			return;
		}

		var lowerWick = (Math.Min(open, close) - candle.LowPrice) / range * 100m;
		var upperWick = (candle.HighPrice - Math.Max(open, close)) / range * 100m;
		var prevBullish = prev != null && prev.ClosePrice > prev.OpenPrice;
		var prevBearish = prev != null && prev.ClosePrice < prev.OpenPrice;

		if ((close > open && lowerWick <= _wickPercent) || prevBullish)
			BuyMarket(Volume);
		else if ((close < open && upperWick <= _wickPercent) || prevBearish)
			SellMarket(Volume);
	}
}
