using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Stochastic strategy.
/// A candle that touches the lower band while stochastic %K is below StochOversold goes long; a candle that touches the upper band
/// while %K is above StochOverbought goes short. An opposite signal reverses the position, and a percent stop limits the loss.
/// </summary>
public class BollingerStochasticStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<decimal> _stochOversold;
	private readonly StrategyParam<decimal> _stochOverbought;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Period of the Bollinger Bands.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier of the bands.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Lookback period of stochastic %K.
	/// </summary>
	public int StochPeriod
	{
		get => _stochPeriod.Value;
		set => _stochPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period of stochastic %D.
	/// </summary>
	public int StochDPeriod
	{
		get => _stochDPeriod.Value;
		set => _stochDPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic level for longs.
	/// </summary>
	public decimal StochOversold
	{
		get => _stochOversold.Value;
		set => _stochOversold.Value = value;
	}

	/// <summary>
	/// Stochastic level for shorts.
	/// </summary>
	public decimal StochOverbought
	{
		get => _stochOverbought.Value;
		set => _stochOverbought.Value = value;
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
	public BollingerStochasticStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Period of the Bollinger Bands", "Bollinger");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BB Deviation", "Standard deviation multiplier of the bands", "Bollinger");

		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Lookback period of stochastic %K", "Stochastic");

		_stochDPeriod = Param(nameof(StochDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "Smoothing period of stochastic %D", "Stochastic");

		_stochOversold = Param(nameof(StochOversold), 20m)
			.SetDisplay("Oversold Level", "Stochastic level for longs", "Stochastic");

		_stochOverbought = Param(nameof(StochOverbought), 80m)
			.SetDisplay("Overbought Level", "Stochastic level for shorts", "Stochastic");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bollinger = new BollingerBands { Length = BollingerPeriod, Width = BollingerDeviation };
		var stochastic = new StochasticOscillator
		{
			K = { Length = StochPeriod },
			D = { Length = StochDPeriod },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, stochastic, ProcessCandle)
			.Start();

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

		// The stop has to see prices between candles, not only at their close.
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
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, stochastic);
			}
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bollingerValue.IsFormed || !stochasticValue.IsFormed)
			return;

		var bands = (BollingerBandsValue)bollingerValue;
		var stoch = (IStochasticOscillatorValue)stochasticValue;

		if (bands.UpBand is not decimal upper || bands.LowBand is not decimal lower || stoch.K is not decimal k)
			return;

		var signal = 0;

		if (candle.LowPrice <= lower && k < StochOversold)
			signal = 1;
		else if (candle.HighPrice >= upper && k > StochOverbought)
			signal = -1;

		if (signal == 0 || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (signal > 0 && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (signal < 0 && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
