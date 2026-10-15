using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volatility Contraction Pattern using prior rolling High/Low range breakouts.
/// Exits on an adverse price/SMA crossing or actual-fill percent protection.
/// </summary>
public class VcpStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _contractionBars;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousHigh;
	private decimal _previousLow;
	private int _contractionCount;
	private decimal? _previousClose;
	private decimal _previousMean;
	private Order _pendingOrder;

	public int MAPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public int LookbackPeriod { get => _lookbackPeriod.Value; set => _lookbackPeriod.Value = value; }
	public int ContractionBars { get => _contractionBars.Value; set => _contractionBars.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VcpStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20).SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
			.SetOptimize(10, 50, 10);
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20).SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Rolling High/Low range length", "Indicators");
		_contractionBars = Param(nameof(ContractionBars), 3).SetGreaterThanZero()
			.SetDisplay("Contractions", "Strict range reductions without an intervening expansion; equal widths are neutral.", "Entry");
		_stopLossPercent = Param(nameof(StopLossPercent), 2m).SetNotNegative()
			.SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		OrderRegistering += order => _pendingOrder = order;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	private void ResetPattern()
	{
		_previousHigh = null;
		_previousLow = default;
		_contractionCount = 0;
		_previousClose = null;
		_previousMean = default;
		_pendingOrder = null;
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetPattern();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ResetPattern();
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}
		var highest = new Highest { Length = LookbackPeriod };
		var lowest = new Lowest { Length = LookbackPeriod };
		var sma = new SimpleMovingAverage { Length = MAPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(highest, lowest, sma, ProcessCandle, false).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection runs before this callback, including between finished candles.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue highestValue, IIndicatorValue lowestValue, IIndicatorValue smaValue)
	{
		if (candle.State != CandleStates.Finished || !highestValue.Indicator.IsFormed || !lowestValue.Indicator.IsFormed)
			return;

		// The setup and breakout boundaries belong to the PRIOR completed channel.
		var upper = _previousHigh;
		var lower = _previousLow;
		var contracted = _contractionCount >= ContractionBars;
		var high = highestValue.GetValue<decimal>();
		var low = lowestValue.GetValue<decimal>();
		if (upper is decimal previousHigh)
		{
			var change = high - low - (previousHigh - lower);
			if (change < 0m) _contractionCount++;
			else if (change > 0m) _contractionCount = 0;
			// Equal width preserves, but does not add to, the contraction sequence.
		}
		_previousHigh = high;
		_previousLow = low;

		if (!smaValue.Indicator.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;
		var close = candle.ClosePrice;
		var mean = smaValue.GetValue<decimal>();
		var upwardCross = _previousClose is decimal up && up <= _previousMean && close > mean;
		var downwardCross = _previousClose is decimal down && down >= _previousMean && close < mean;
		var seeded = _previousClose.HasValue;
		_previousClose = close;
		_previousMean = mean;
		if (!seeded || upper is not decimal previousUpper
			|| _pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (Position > 0m && downwardCross)
			SellMarket(Position);
		else if (Position < 0m && upwardCross)
			BuyMarket(Math.Abs(Position));
		else if (Position == 0m && contracted)
		{
			if (close > previousUpper)
			{
				_contractionCount = 0;
				BuyMarket(Volume);
			}
			else if (close < lower)
			{
				_contractionCount = 0;
				SellMarket(Volume);
			}
		}
	}
}
