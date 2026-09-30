using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Implied volatility spike strategy.
/// The implied volatility readings are the candle closes of a separate instrument on the same timeframe.
/// A spike is a rise to at least <see cref="IVSpikeThreshold"/> times the previous reading that also leaves the reading
/// above the average of the last <see cref="IVPeriod"/> readings. On a spike the strategy enters against the price move:
/// long when the close is below the moving average, short when it is above.
/// The position is closed when a reading falls below the previous one or the stop-loss is hit.
/// </summary>
public class IvSpikeStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _ivPeriod;
	private readonly StrategyParam<decimal> _ivSpikeThreshold;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Security> _ivSecurity;

	private decimal? _previousIv;
	private DateTime? _ivBarTime;
	private bool _hasIvChange;
	private bool _isIvSpike;
	private bool _isIvDecline;
	private DateTime? _mainBarTime;
	private decimal _mainPrice;
	private decimal _mainAverage;
	private DateTime? _processedPairTime;
	private Order _pendingOrder;

	/// <summary>
	/// Period of the moving average the close is compared with (default: 20).
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Number of implied volatility readings in the average a spike reading must exceed (default: 20).
	/// </summary>
	public int IVPeriod
	{
		get => _ivPeriod.Value;
		set => _ivPeriod.Value = value;
	}

	/// <summary>
	/// Multiple of the previous implied volatility reading that a rise must reach to count as a spike (default: 1.5).
	/// </summary>
	public decimal IVSpikeThreshold
	{
		get => _ivSpikeThreshold.Value;
		set => _ivSpikeThreshold.Value = value;
	}

	/// <summary>
	/// Stop-loss as a percentage from the entry price (default: 2%). Zero disables it.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Type of candles used for both the traded instrument and the implied volatility instrument.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Instrument whose candle closes are the implied volatility readings (required).
	/// </summary>
	public Security IVSecurity
	{
		get => _ivSecurity.Value;
		set => _ivSecurity.Value = value;
	}

	/// <summary>
	/// Initialize the IV Spike strategy.
	/// </summary>
	public IvSpikeStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period of the moving average the close is compared with", "Indicators")
			.SetOptimize(10, 50, 10);

		_ivPeriod = Param(nameof(IVPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("IV Period", "Implied volatility readings in the average a spike reading must exceed", "Indicators")
			.SetOptimize(10, 30, 5);

		_ivSpikeThreshold = Param(nameof(IVSpikeThreshold), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("IV Spike Threshold", "Multiple of the previous implied volatility reading a spike must reach", "Entry")
			.SetOptimize(1.2m, 2.0m, 0.1m);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss as percentage from entry price", "Risk Management")
			.SetOptimize(1m, 5m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for the traded and the implied volatility instruments", "General");

		_ivSecurity = Param<Security>(nameof(IVSecurity))
			.SetDisplay("IV Security", "Instrument whose candle closes are the implied volatility readings", "Data")
			.SetRequired();

		OrderRegistering += order => _pendingOrder = order;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (IVSecurity, CandleType), (Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ClearSignalState();
	}

	private void ClearSignalState()
	{
		_previousIv = null;
		_ivBarTime = null;
		_hasIvChange = false;
		_isIvSpike = false;
		_isIvDecline = false;
		_mainBarTime = null;
		_mainPrice = 0m;
		_mainAverage = 0m;
		_processedPairTime = null;
		_pendingOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		// Reject before any startup side effects: SubscribeCandles(null) falls back to the traded instrument.
		if (IVSecurity is null || Security is null || string.Equals(IVSecurity.Id, Security.Id, StringComparison.OrdinalIgnoreCase))
			throw new InvalidOperationException("IVSecurity must explicitly identify a different external instrument.");

		base.OnStarted2(time);
		ClearSignalState();

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

		// Bid and ask updates let the native stop react between finished candles.
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var priceAverage = new SimpleMovingAverage { Length = MAPeriod };
		var ivAverage = new SimpleMovingAverage { Length = IVPeriod };

		var mainSubscription = SubscribeCandles(CandleType);
		var ivSubscription = SubscribeCandles(CandleType, security: IVSecurity);

		mainSubscription
			.BindEx(priceAverage, ProcessMainCandle)
			.Start();

		ivSubscription
			.BindEx(ivAverage, ProcessIvCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, priceAverage);
			DrawOwnTrades(area);

			var ivArea = CreateChartArea();
			DrawCandles(ivArea, ivSubscription);
			DrawIndicator(ivArea, ivAverage);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection runs before this callback, including between signal pairs.
	}

	private void ProcessIvCandle(ICandleMessage candle, IIndicatorValue average)
	{
		if (candle.State != CandleStates.Finished || _ivBarTime is DateTime previousTime && candle.OpenTime <= previousTime)
			return;

		var reading = candle.ClosePrice;

		_ivBarTime = candle.OpenTime;
		_hasIvChange = _previousIv.HasValue;
		_isIvDecline = _previousIv is decimal previous && reading < previous;

		// The jump is measured from the previous reading; the average only confirms the level is raised.
		_isIvSpike = _previousIv is decimal prior && prior > 0m && reading > prior && reading >= IVSpikeThreshold * prior
			&& average.Indicator.IsFormed && reading > average.GetValue<decimal>();

		_previousIv = reading;

		ProcessMatchedPair();
	}

	private void ProcessMainCandle(ICandleMessage candle, IIndicatorValue average)
	{
		if (candle.State != CandleStates.Finished || !average.Indicator.IsFormed
			|| _mainBarTime is DateTime previousTime && candle.OpenTime <= previousTime)
			return;

		_mainBarTime = candle.OpenTime;
		_mainPrice = candle.ClosePrice;
		_mainAverage = average.GetValue<decimal>();

		ProcessMatchedPair();
	}

	private void ProcessMatchedPair()
	{
		// Act once per bar time and only when both streams finished it; an unmatched bar is never reused.
		if (!_hasIvChange || _mainBarTime is not DateTime time || _ivBarTime != time || _processedPairTime == time)
			return;

		_processedPairTime = time;

		if (!IsFormedAndOnlineAndAllowTrading()
			|| _pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;

		if (Position != 0m)
		{
			if (!_isIvDecline)
				return;

			if (Position > 0m)
				SellMarket(Position);
			else
				BuyMarket(Math.Abs(Position));
		}
		else if (_isIvSpike)
		{
			// A close exactly on the average shows no price move to fade.
			if (_mainPrice < _mainAverage)
				BuyMarket(Volume);
			else if (_mainPrice > _mainAverage)
				SellMarket(Volume);
		}
	}
}
