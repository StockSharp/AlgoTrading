using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Donchian Stochastic strategy.
/// The channel spans the highest high and lowest low of the previous DonchianPeriod candles. A close above it with %K above StochOverbought
/// confirms momentum and goes long, a close below it with %K below StochOversold goes short, reversing an opposite position;
/// %K is the stochastic over StochPeriod candles smoothed over StochK candles.
/// The breakout fails, closing the position, when price closes back beyond the broken level, and a percent stop limits the loss.
/// </summary>
public class DonchianStochasticStrategy : Strategy
{
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<int> _stochK;
	private readonly StrategyParam<decimal> _stochOverbought;
	private readonly StrategyParam<decimal> _stochOversold;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevUpper;
	private decimal? _prevLower;
	private decimal _breakoutLevel;

	/// <summary>
	/// Previous candles the channel spans.
	/// </summary>
	public int DonchianPeriod
	{
		get => _donchianPeriod.Value;
		set => _donchianPeriod.Value = value;
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
	/// %K level that confirms an upside breakout.
	/// </summary>
	public decimal StochOverbought
	{
		get => _stochOverbought.Value;
		set => _stochOverbought.Value = value;
	}

	/// <summary>
	/// %K level that confirms a downside breakout.
	/// </summary>
	public decimal StochOversold
	{
		get => _stochOversold.Value;
		set => _stochOversold.Value = value;
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
	public DonchianStochasticStrategy()
	{
		_donchianPeriod = Param(nameof(DonchianPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Donchian Period", "Previous candles the channel spans", "Indicators");

		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Lookback period of the raw stochastic", "Stochastic");

		_stochK = Param(nameof(StochK), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "Smoothing period of %K", "Stochastic");

		_stochOverbought = Param(nameof(StochOverbought), 80m)
			.SetDisplay("Stochastic Overbought", "%K level that confirms an upside breakout", "Stochastic");

		_stochOversold = Param(nameof(StochOversold), 20m)
			.SetDisplay("Stochastic Oversold", "%K level that confirms a downside breakout", "Stochastic");

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
		_prevUpper = null;
		_prevLower = null;
		_breakoutLevel = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevUpper = null;
		_prevLower = null;
		_breakoutLevel = default;

		var donchian = new DonchianChannels { Length = DonchianPeriod };
		// The D line of the core oscillator is the smoothed %K.
		var stochastic = new StochasticOscillator
		{
			K = { Length = StochPeriod },
			D = { Length = StochK },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(donchian, stochastic, ProcessCandle)
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
			DrawIndicator(area, donchian);
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

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The channel is measured on the candles before this one.
		var upper = _prevUpper;
		var lower = _prevLower;

		if (donchianValue.IsFormed && donchianValue is IDonchianChannelsValue { UpperBand: decimal currentUpper, LowerBand: decimal currentLower })
		{
			_prevUpper = currentUpper;
			_prevLower = currentLower;
		}

		if (!stochasticValue.IsFormed || upper is not decimal channelHigh || lower is not decimal channelLow)
			return;

		if (stochasticValue is not IStochasticOscillatorValue { D: decimal k })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (close > channelHigh && k > StochOverbought && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_breakoutLevel = channelHigh;
		}
		else if (close < channelLow && k < StochOversold && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_breakoutLevel = channelLow;
		}
		else if (Position > 0 && close < _breakoutLevel)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && close > _breakoutLevel)
		{
			BuyMarket(-Position);
		}
	}
}
