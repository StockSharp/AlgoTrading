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
/// Fades confirmed stochastic extremes and exits on the return to neutral.
/// The native oscillator's D is the smoothed %K; a second SMA forms %D.
/// </summary>
public class StochasticOverboughtOversoldStrategy : Strategy
{
	private const decimal ComparisonTolerance = 0.00000001m;
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private readonly Queue<decimal> _dWindow = new();
	private decimal? _previousK;
	private Order _pendingOrder;

	public int StochPeriod { get => _stochPeriod.Value; set => _stochPeriod.Value = value; }
	public int KPeriod { get => _kPeriod.Value; set => _kPeriod.Value = value; }
	public int DPeriod { get => _dPeriod.Value; set => _dPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	public StochasticOverboughtOversoldStrategy()
	{
		_stochPeriod = Param(nameof(StochPeriod), 14).SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Raw high/low lookback", "Indicators");
		_kPeriod = Param(nameof(KPeriod), 3).SetGreaterThanZero()
			.SetDisplay("K Period", "SMA length of raw %K", "Indicators");
		_dPeriod = Param(nameof(DPeriod), 3).SetGreaterThanZero()
			.SetDisplay("D Period", "SMA length of smoothed %K", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Stochastic timeframe", "General");
		_stopLossPercent = Param(nameof(StopLossPercent), 2m).SetNotNegative()
			.SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection");
		OrderRegistering += order => _pendingOrder = order;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ClearState();
	}

	private void ClearState()
	{
		_dWindow.Clear();
		_previousK = null;
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ClearState();
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}
		var stochastic = new StochasticOscillator
		{
			K = { Length = StochPeriod },
			D = { Length = KPeriod },
		};
		var candles = SubscribeCandles(CandleType);
		candles.BindEx(stochastic, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candles);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native actual-fill protection evaluates executable quotes between signal candles.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue output)
	{
		if (candle.State != CandleStates.Finished || !output.Indicator.IsFormed ||
			output is not StochasticOscillatorValue value || value.D is not decimal k)
			return;

		_dWindow.Enqueue(k);
		if (_dWindow.Count > DPeriod)
			_dWindow.Dequeue();
		if (_dWindow.Count < DPeriod)
			return;

		var d = _dWindow.Average();
		var previous = _previousK;
		_previousK = k;
		if (previous is not decimal prior || !IsFormedAndOnlineAndAllowTrading() ||
			_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;

		// The native rolling SMA can leave a sub-ulp remainder at exact levels (e.g. 50).
		if (Position == 0m && prior < 20m - ComparisonTolerance &&
			k > prior + ComparisonTolerance && k > d + ComparisonTolerance)
			BuyMarket(Volume);
		else if (Position == 0m && prior > 80m + ComparisonTolerance &&
			k < prior - ComparisonTolerance && k < d - ComparisonTolerance)
			SellMarket(Volume);
		// %K crossing 50 either way closes, so a position whose entry bar already passed 50 exits on the cross back.
		else if (Position > 0m && (prior < 50m - ComparisonTolerance) != (k < 50m - ComparisonTolerance))
			SellMarket(Position);
		else if (Position < 0m && (prior > 50m + ComparisonTolerance) != (k > 50m + ComparisonTolerance))
			BuyMarket(Math.Abs(Position));
	}
}
