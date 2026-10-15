using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// N-day high/low breakout strategy.
/// Enters long when price pierces the high of the previous N candles while closing above the moving average,
/// and short when it pierces their low while closing below it.
/// Exits when the close crosses back through the moving average, on the opposite signal, or at the percent stop.
/// </summary>
public class NdayBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private SMA _ma;

	// Channel of the candles before the current one; a candle never breaks out of a range it is part of.
	private decimal? _channelHigh;
	private decimal? _channelLow;

	/// <summary>
	/// Number of candles that form the high/low range.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Period of the moving average that filters entries and triggers exits.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public NdayBreakoutStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Number of candles that form the high/low range", "Strategy Parameters")
			.SetOptimize(10, 30, 5);

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average that filters entries and triggers exits", "Strategy Parameters")
			.SetOptimize(10, 30, 5);

		_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
			.SetOptimize(1.0m, 3.0m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "Strategy Parameters");
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

		_highest = null;
		_lowest = null;
		_ma = null;
		_channelHigh = null;
		_channelLow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highest = new Highest { Length = LookbackPeriod };
		_lowest = new Lowest { Length = LookbackPeriod };
		_ma = new SMA { Length = MaPeriod };
		_channelHigh = null;
		_channelLow = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

		// The stop has to see prices between candles; an hourly candle alone would let it act once an hour.
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var maValue = _ma.Process(candle.ClosePrice, candle.OpenTime, true);

		// The breakout compares the candle with the range of the candles before it.
		var channelHigh = _channelHigh;
		var channelLow = _channelLow;

		var highValue = _highest.Process(candle.HighPrice, candle.OpenTime, true);
		var lowValue = _lowest.Process(candle.LowPrice, candle.OpenTime, true);

		if (_highest.IsFormed && _lowest.IsFormed)
		{
			_channelHigh = highValue.GetValue<decimal>();
			_channelLow = lowValue.GetValue<decimal>();
		}

		if (channelHigh is not decimal high || channelLow is not decimal low || !_ma.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ma = maValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (candle.HighPrice > high && close > ma && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (candle.LowPrice < low && close < ma && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (Position > 0 && close < ma)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && close > ma)
		{
			BuyMarket(-Position);
		}
	}
}
