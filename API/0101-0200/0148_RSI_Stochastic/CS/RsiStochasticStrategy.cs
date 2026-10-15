using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI Stochastic strategy.
/// RSI below RsiOversold together with %K below StochOversold goes long, RSI above RsiOverbought together with %K above StochOverbought
/// goes short, reversing an opposite position; %K is the stochastic over StochPeriod candles smoothed over StochK candles.
/// A long closes once RSI rises above 50 and a short once it falls below 50, and a percent stop limits the loss.
/// </summary>
public class RsiStochasticStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<int> _stochK;
	private readonly StrategyParam<decimal> _stochOversold;
	private readonly StrategyParam<decimal> _stochOverbought;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Period of RSI.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI level for longs.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// RSI level for shorts.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// Lookback period of the raw stochastic.
	/// </summary>
	public int StochPeriod
	{
		get => _stochPeriod.Value;
		set => _stochPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period of %K.
	/// </summary>
	public int StochK
	{
		get => _stochK.Value;
		set => _stochK.Value = value;
	}

	/// <summary>
	/// %K level for longs.
	/// </summary>
	public decimal StochOversold
	{
		get => _stochOversold.Value;
		set => _stochOversold.Value = value;
	}

	/// <summary>
	/// %K level for shorts.
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
	public RsiStochasticStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period of RSI", "RSI");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI level for longs", "RSI");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "RSI level for shorts", "RSI");

		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Lookback period of the raw stochastic", "Stochastic");

		_stochK = Param(nameof(StochK), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "Smoothing period of %K", "Stochastic");

		_stochOversold = Param(nameof(StochOversold), 20m)
			.SetDisplay("Stochastic Oversold", "%K level for longs", "Stochastic");

		_stochOverbought = Param(nameof(StochOverbought), 80m)
			.SetDisplay("Stochastic Overbought", "%K level for shorts", "Stochastic");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		// The D line of the core oscillator is the smoothed %K.
		var stochastic = new StochasticOscillator
		{
			K = { Length = StochPeriod },
			D = { Length = StochK },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, stochastic, ProcessCandle)
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
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, rsi);
				DrawIndicator(oscillators, stochastic);
			}
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!rsiValue.IsFormed || !stochasticValue.IsFormed)
			return;

		if (stochasticValue is not IStochasticOscillatorValue { D: decimal k })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rsi = rsiValue.GetValue<decimal>();

		if (rsi < RsiOversold && k < StochOversold && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (rsi > RsiOverbought && k > StochOverbought && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && rsi > 50m)
			SellMarket(Position);
		else if (Position < 0 && rsi < 50m)
			BuyMarket(-Position);
	}
}
