namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Three EMA Cross Strategy.
/// After the fast EMA crosses above the slow EMA, a long opens within CrossBackBars bars on a pullback whose low touches the
/// fast EMA while the close stays at or above both the fast EMA and the trend EMA. The long closes when the fast EMA drops
/// below the slow EMA, and a percent stop limits the loss.
/// </summary>
public class ThreeEmaCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _trendEmaLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _crossBackBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevSlow;
	private int? _barsSinceCross;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// Trend EMA period.
	/// </summary>
	public int TrendEmaLength
	{
		get => _trendEmaLength.Value;
		set => _trendEmaLength.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Bars after the fast/slow cross during which a pullback entry is allowed.
	/// </summary>
	public int CrossBackBars
	{
		get => _crossBackBars.Value;
		set => _crossBackBars.Value = value;
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
	public ThreeEmaCrossStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowEmaLength = Param(nameof(SlowEmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_trendEmaLength = Param(nameof(TrendEmaLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("Trend EMA", "Trend EMA period", "Indicators");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_crossBackBars = Param(nameof(CrossBackBars), 10)
			.SetGreaterThanZero()
			.SetDisplay("Cross Back Bars", "Bars after the cross during which a pullback entry is allowed", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		_prevFast = null;
		_prevSlow = null;
		_barsSinceCross = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		var trendEma = new ExponentialMovingAverage { Length = TrendEmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastEma, slowEma, trendEma, ProcessCandle)
			.Start();

		if (StopLossPercent > 0m)
		{
			StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

			// The stop has to see prices between candles, not only at their close.
			foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
			{
				var quotes = new Subscription(DataType.Level1, Security);
				quotes.MarketData.BuildField = field;
				SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
			}
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawIndicator(area, trendEma);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue trendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!fastValue.IsFormed || !slowValue.IsFormed)
			return;

		var fast = fastValue.GetValue<decimal>();
		var slow = slowValue.GetValue<decimal>();

		var prevFast = _prevFast;
		var prevSlow = _prevSlow;
		_prevFast = fast;
		_prevSlow = slow;

		if (prevFast is decimal pf && prevSlow is decimal ps && pf <= ps && fast > slow)
			_barsSinceCross = 0;
		else if (_barsSinceCross is int bars)
			_barsSinceCross = bars + 1;

		if (!trendValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var trend = trendValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (fast < slow)
				SellMarket(Position);

			return;
		}

		var recentCross = _barsSinceCross is int since && since < CrossBackBars;

		if (Position == 0 && recentCross && close >= fast && candle.LowPrice <= fast && trend <= close)
			BuyMarket(Volume);
	}
}
