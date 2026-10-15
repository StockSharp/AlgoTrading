using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VWAP Stochastic strategy.
/// The market trades around the clock, so the session VWAP restarts with each UTC day and weighs each candle's typical price by its volume.
/// A close below VWAP with %K below OversoldLevel goes long and a close above VWAP with %K above OverboughtLevel goes short, reversing
/// an opposite position; %K is the stochastic over StochPeriod candles smoothed over StochKPeriod candles. A long closes above VWAP
/// and a short below it, and a percent stop limits the loss.
/// </summary>
public class VwapStochasticStrategy : Strategy
{
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _day;
	private decimal _cumulativePriceVolume;
	private decimal _cumulativeVolume;

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
	public int StochKPeriod
	{
		get => _stochKPeriod.Value;
		set => _stochKPeriod.Value = value;
	}

	/// <summary>
	/// %K level for shorts.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// %K level for longs.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
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
	public VwapStochasticStrategy()
	{
		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Lookback period of the raw stochastic", "Stochastic");

		_stochKPeriod = Param(nameof(StochKPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "Smoothing period of %K", "Stochastic");

		_overboughtLevel = Param(nameof(OverboughtLevel), 80m)
			.SetDisplay("Overbought Level", "%K level for shorts", "Stochastic");

		_oversoldLevel = Param(nameof(OversoldLevel), 20m)
			.SetDisplay("Oversold Level", "%K level for longs", "Stochastic");

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
	protected override void OnReseted()
	{
		base.OnReseted();
		_day = null;
		_cumulativePriceVolume = 0;
		_cumulativeVolume = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_day = null;
		_cumulativePriceVolume = 0;
		_cumulativeVolume = 0;

		// The D line of the core oscillator is the smoothed %K.
		var stochastic = new StochasticOscillator
		{
			K = { Length = StochPeriod },
			D = { Length = StochKPeriod },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, ProcessCandle)
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
				DrawIndicator(oscillators, stochastic);
			}
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.Date;

		if (_day != day)
		{
			_day = day;
			_cumulativePriceVolume = 0;
			_cumulativeVolume = 0;
		}

		var typicalPrice = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3;
		_cumulativePriceVolume += typicalPrice * candle.TotalVolume;
		_cumulativeVolume += candle.TotalVolume;

		if (!stochasticValue.IsFormed || _cumulativeVolume <= 0)
			return;

		if (stochasticValue is not IStochasticOscillatorValue { D: decimal k })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var vwap = _cumulativePriceVolume / _cumulativeVolume;
		var close = candle.ClosePrice;

		if (close < vwap && k < OversoldLevel && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close > vwap && k > OverboughtLevel && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && close > vwap)
			SellMarket(Position);
		else if (Position < 0 && close < vwap)
			BuyMarket(-Position);
	}
}
