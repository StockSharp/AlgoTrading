using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;
using Ecng.ComponentModel;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades based on VIX (Volatility Index) movements.
/// It enters positions when VIX is rising (indicating increasing fear/volatility in the market)
/// and price is moving in an expected direction relative to its moving average.
/// </summary>
public class VixTriggerStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Security> _vixSecurity;

	private decimal? _previousVix;
	private DateTime? _vixBarTime;
	private int _vixDirection;
	private bool _hasVixDirection;
	private DateTime? _mainBarTime;
	private decimal _mainPrice;
	private decimal _mainAverage;
	private DateTime? _processedPairTime;
	private Order _pendingOrder;

	/// <summary>
	/// Period for Moving Average calculation (default: 20)
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss as percentage from entry price (default: 2%)
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Type of candles used for strategy calculation
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// VIX Security (required)
	/// </summary>
	public Security VixSecurity
	{
		get => _vixSecurity.Value;
		set => _vixSecurity.Value = value;
	}

	/// <summary>
	/// Initialize the VIX Trigger strategy
	/// </summary>
	public VixTriggerStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20).SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for Moving Average calculation", "Technical Parameters")
			
			.SetOptimize(10, 50, 5);

		_stopLossPercent = Param(nameof(StopLossPercent), 2.0m).SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss as percentage from entry price", "Risk Management")
			
			.SetOptimize(1.0m, 5.0m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "Data");

		_vixSecurity = Param<Security>(nameof(VixSecurity))
			.SetDisplay("VIX Security", "VIX Security to use for signals", "Data")
			.SetRequired();
		OrderRegistering += order => _pendingOrder = order;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (VixSecurity, CandleType), (Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ClearSignalState();
	}

	private void ClearSignalState()
	{
		_previousVix = null;
		_vixBarTime = null;
		_vixDirection = 0;
		_hasVixDirection = false;
		_mainBarTime = null;
		_mainPrice = 0m;
		_mainAverage = 0m;
		_processedPairTime = null;
		_pendingOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		// Reject before any startup side effects: SubscribeCandles(null) falls back to the primary.
		if (VixSecurity is null || Security is null || string.Equals(VixSecurity.Id, Security.Id, StringComparison.OrdinalIgnoreCase))
			throw new InvalidOperationException("VixSecurity must explicitly identify a different external instrument.");
		base.OnStarted2(time);
		ClearSignalState();
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		// Create indicator
		var sma = new SimpleMovingAverage { Length = MAPeriod };

		// Create subscriptions
		var mainSubscription = SubscribeCandles(CandleType);
		var vixSubscription = SubscribeCandles(CandleType, security: VixSecurity);

		// Bind indicator to main security candles
		mainSubscription
			.BindEx(sma, ProcessMainCandle, false)
			.Start();

		// Process VIX candles separately
		vixSubscription
			.Bind(ProcessVixCandle)
			.Start();

		// Configure chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}


	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection runs before this callback, including between signal pairs.
	}

	private void ProcessVixCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || _vixBarTime is DateTime previousTime && candle.OpenTime <= previousTime)
			return;
		_vixBarTime = candle.OpenTime;
		_hasVixDirection = _previousVix.HasValue;
		_vixDirection = _previousVix is decimal previous ? Math.Sign(candle.ClosePrice - previous) : 0;
		_previousVix = candle.ClosePrice;
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
		// Keep only the newest bar from each source; never reuse stale unmatched bars.
		if (!_hasVixDirection || _mainBarTime is not DateTime time || _vixBarTime != time || _processedPairTime == time)
			return;
		_processedPairTime = time;
		if (!IsFormedAndOnlineAndAllowTrading()
			|| _pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (Position != 0m && _vixDirection < 0)
		{
			if (Position > 0m) SellMarket(Position);
			else BuyMarket(Math.Abs(Position));
		}
		else if (Position == 0m && _vixDirection > 0)
		{
			if (_mainPrice < _mainAverage) BuyMarket(Volume);
			else if (_mainPrice > _mainAverage) SellMarket(Volume);
		}
		// Equal index closes are not falling, and equal price/MA is not a short signal.
	}
}
