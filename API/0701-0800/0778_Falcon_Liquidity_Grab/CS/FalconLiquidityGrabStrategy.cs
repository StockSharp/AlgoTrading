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
/// Looks for wick sweeps of preceding swing extremes with an SMA trend filter.
/// Protective stop and target distances are measured in the security's minimum price step.
/// </summary>
public class FalconLiquidityGrabStrategy : Strategy
{
	private readonly StrategyParam<int> _swingPeriod;
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitMultiplier;
	private readonly StrategyParam<bool> _useSession;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<DataType> _candleType;
	private readonly Queue<decimal> _previousHighs = new();
	private readonly Queue<decimal> _previousLows = new();
	private Order _pendingOrder;

	public int SwingPeriod { get => _swingPeriod.Value; set => _swingPeriod.Value = value; }
	public int TrendLength { get => _trendLength.Value; set => _trendLength.Value = value; }
	public int StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	public decimal TakeProfitMultiplier { get => _takeProfitMultiplier.Value; set => _takeProfitMultiplier.Value = value; }
	public bool UseSession { get => _useSession.Value; set => _useSession.Value = value; }
	public TimeSpan SessionStart { get => _sessionStart.Value; set => _sessionStart.Value = value; }
	public TimeSpan SessionEnd { get => _sessionEnd.Value; set => _sessionEnd.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public FalconLiquidityGrabStrategy()
	{
		_swingPeriod = Param(nameof(SwingPeriod), 20).SetGreaterThanZero()
			.SetDisplay("Swing Period", "Lookback of previous highs and lows; excludes the signal candle.", "Signal");
		_trendLength = Param(nameof(TrendLength), 50).SetGreaterThanZero()
			.SetDisplay("Trend Length", "SMA trend-filter period.", "Signal");
		_stopLossPoints = Param(nameof(StopLossPoints), 50).SetGreaterThanZero()
			.SetDisplay("Stop Loss Points", "Number of minimum price steps from the entry.", "Protection");
		_takeProfitMultiplier = Param(nameof(TakeProfitMultiplier), 2m).SetGreaterThanZero()
			.SetDisplay("Take Profit Multiplier", "Target distance divided by the stop distance.", "Protection");
		_useSession = Param(nameof(UseSession), true)
			.SetDisplay("Use Session", "Limit entries to the UTC trading window.", "Session");
		_sessionStart = Param(nameof(SessionStart), TimeSpan.FromHours(7))
			.SetDisplay("Session Start", "UTC inclusive session start.", "Session");
		_sessionEnd = Param(nameof(SessionEnd), TimeSpan.FromHours(21))
			.SetDisplay("Session End", "UTC exclusive session end.", "Session");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe.", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousHighs.Clear();
		_previousLows.Clear();
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			throw new InvalidOperationException("The instrument must supply a positive price step for tick-based protection.");

		var stopDistance = StopLossPoints * priceStep;
		StartProtection(
			new Unit(stopDistance * TakeProfitMultiplier, UnitTypes.Absolute),
			new Unit(stopDistance, UnitTypes.Absolute),
			useMarketOrders: true,
			isLocalStop: true);

		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		OrderRegistering += order => _pendingOrder = order;
		var trend = new SimpleMovingAverage { Length = TrendLength };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(trend, (candle, value) => ProcessCandle(candle, value, trend)).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, trend);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection reacts to both quote sides before the next finished signal candle.
	}

	private void ProcessCandle(ICandleMessage candle, decimal trendValue, SimpleMovingAverage trend)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var pendingActive = _pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed);
		if (trend.IsFormed && _previousHighs.Count == SwingPeriod && _previousLows.Count == SwingPeriod
			&& Position == 0m && !pendingActive)
		{
			var utcTime = candle.OpenTime.ToUniversalTime().TimeOfDay;
			var inSession = !UseSession || (SessionStart <= SessionEnd
				? utcTime >= SessionStart && utcTime < SessionEnd
				: utcTime >= SessionStart || utcTime < SessionEnd);
			if (inSession)
			{
				if (candle.LowPrice < _previousLows.Min() && candle.ClosePrice > trendValue)
					BuyMarket(Volume);
				else if (candle.HighPrice > _previousHighs.Max() && candle.ClosePrice < trendValue)
					SellMarket(Volume);
			}
		}
		_previousHighs.Enqueue(candle.HighPrice);
		_previousLows.Enqueue(candle.LowPrice);
		while (_previousHighs.Count > SwingPeriod) _previousHighs.Dequeue();
		while (_previousLows.Count > SwingPeriod) _previousLows.Dequeue();
	}
}
