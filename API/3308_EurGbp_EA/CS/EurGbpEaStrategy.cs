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
/// Cross-currency MACD strategy trading the main instrument based on EUR/USD and GBP/USD momentum comparison.
/// </summary>
public class EurGbpEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Security> _eurUsdSecurity;
	private readonly StrategyParam<Security> _gbpUsdSecurity;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;

	private MovingAverageConvergenceDivergence _eurMacd;
	private MovingAverageConvergenceDivergence _gbpMacd;

	private decimal _eurHistogram;
	private decimal _eurSignal;
	private decimal _gbpHistogram;
	private decimal _gbpSignal;
	private decimal _lastTradingClose;

	private DateTimeOffset? _lastTradingBar;
	private DateTimeOffset? _lastBuyBar;
	private DateTimeOffset? _lastSellBar;

	/// <summary>
	/// Initializes a new instance of <see cref="EurGbpEaStrategy"/>.
	/// </summary>
	public EurGbpEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for analysis", "General");

		_eurUsdSecurity = Param<Security>(nameof(EurUsdSecurity))
			.SetDisplay("EURUSD Security", "EUR/USD reference symbol", "General");

		_gbpUsdSecurity = Param<Security>(nameof(GbpUsdSecurity))
			.SetDisplay("GBPUSD Security", "GBP/USD reference symbol", "General");


		_stopLoss = Param(nameof(StopLoss), 75)
			.SetDisplay("Stop Loss", "Stop loss distance in points", "Protection")
			.SetGreaterOrEqualToZero()
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 46)
			.SetDisplay("Take Profit", "Take profit distance in points", "Protection")
			.SetGreaterOrEqualToZero()
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// EUR/USD security providing the first MACD stream.
	/// </summary>
	public Security EurUsdSecurity
	{
		get => _eurUsdSecurity.Value;
		set => _eurUsdSecurity.Value = value;
	}

	/// <summary>
	/// GBP/USD security providing the second MACD stream.
	/// </summary>
	public Security GbpUsdSecurity
	{
		get => _gbpUsdSecurity.Value;
		set => _gbpUsdSecurity.Value = value;
	}


	/// <summary>
	/// Stop loss distance measured in price steps.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance measured in price steps.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);

		if (EurUsdSecurity != null)
			yield return (EurUsdSecurity, CandleType);

		if (GbpUsdSecurity != null)
			yield return (GbpUsdSecurity, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_eurMacd = null;
		_gbpMacd = null;
		_eurHistogram = 0m;
		_gbpHistogram = 0m;
		_eurSignal = 0m;
		_gbpSignal = 0m;
		_lastTradingClose = 0m;
		_lastTradingBar = null;
		_lastBuyBar = null;
		_lastSellBar = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		if (Security == null)
			throw new InvalidOperationException("Trading security must be specified.");

		if (EurUsdSecurity == null || GbpUsdSecurity == null)
			throw new InvalidOperationException("Reference securities must be specified.");

		base.OnStarted(time);

		StartProtection();

		_eurMacd = CreateMacd();
		_gbpMacd = CreateMacd();

		var tradingSubscription = SubscribeCandles(CandleType);
		tradingSubscription.Bind(OnTradingCandle).Start();

		var eurSubscription = SubscribeCandles(CandleType, true, EurUsdSecurity);
		eurSubscription.Bind(_eurMacd, OnEurCandle).Start();

		var gbpSubscription = SubscribeCandles(CandleType, true, GbpUsdSecurity);
		gbpSubscription.Bind(_gbpMacd, OnGbpCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, tradingSubscription);
			DrawOwnTrades(area);
		}
	}

	private static MovingAverageConvergenceDivergence CreateMacd()
	{
		return new MovingAverageConvergenceDivergence
		{
			ShortPeriod = 12,
			LongPeriod = 26,
			SignalPeriod = 9
		};
	}

	private void OnTradingCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastTradingClose = candle.ClosePrice;
		_lastTradingBar = candle.OpenTime;
	}

	private void OnEurCandle(ICandleMessage candle, decimal macd, decimal signal, decimal histogram)
	{
		if (candle.State != CandleStates.Finished || !_eurMacd.IsFormed)
			return;

		_eurSignal = signal;
		_eurHistogram = histogram;

		TryTrade(candle.OpenTime);
	}

	private void OnGbpCandle(ICandleMessage candle, decimal macd, decimal signal, decimal histogram)
	{
		if (candle.State != CandleStates.Finished || !_gbpMacd.IsFormed)
			return;

		_gbpSignal = signal;
		_gbpHistogram = histogram;

		TryTrade(candle.OpenTime);
	}

	private void TryTrade(DateTimeOffset barTime)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_eurMacd == null || _gbpMacd == null)
			return;

		if (!_eurMacd.IsFormed || !_gbpMacd.IsFormed)
			return;

		if (_lastTradingBar == null || _lastTradingClose <= 0m)
			return;

		if (barTime < _lastTradingBar.Value)
			return;

		var volume = Volume;
		if (volume <= 0m)
			return;

		var buyCondition = _eurHistogram < _gbpHistogram && _eurSignal > _gbpSignal;
		if (buyCondition && Position <= 0m)
		{
			if (_lastBuyBar == barTime)
				return;

			var resultingPosition = Position + volume;
			BuyMarket(volume);
			ApplyProtection(_lastTradingClose, resultingPosition);
			_lastBuyBar = barTime;
			return;
		}

		var sellCondition = _gbpHistogram < _eurHistogram && _gbpSignal > _eurSignal;
		if (sellCondition && Position >= 0m)
		{
			if (_lastSellBar == barTime)
				return;

			var resultingPosition = Position - volume;
			SellMarket(volume);
			ApplyProtection(_lastTradingClose, resultingPosition);
			_lastSellBar = barTime;
		}
	}

	private void ApplyProtection(decimal referencePrice, decimal resultingPosition)
	{
		if (Security?.PriceStep is not decimal step || step <= 0m)
			return;

		if (TakeProfit > 0)
		{
			var distance = TakeProfit * step;
			SetTakeProfit(distance, referencePrice, resultingPosition);
		}

		if (StopLoss > 0)
		{
			var distance = StopLoss * step;
			SetStopLoss(distance, referencePrice, resultingPosition);
		}
	}
}

