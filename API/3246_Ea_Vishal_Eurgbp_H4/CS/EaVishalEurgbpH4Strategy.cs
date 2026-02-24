using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EA Vishal strategy: Stochastic K/D crossover entry with SMA envelope exit.
/// Buys on stochastic bullish cross, sells on bearish cross, exits at envelope bands.
/// </summary>
public class EaVishalEurgbpH4Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _envelopePeriod;
	private readonly StrategyParam<decimal> _envelopeDeviation;

	private decimal? _prevK;
	private decimal? _prevD;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	public int EnvelopePeriod
	{
		get => _envelopePeriod.Value;
		set => _envelopePeriod.Value = value;
	}

	public decimal EnvelopeDeviation
	{
		get => _envelopeDeviation.Value;
		set => _envelopeDeviation.Value = value;
	}

	public EaVishalEurgbpH4Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic K", "K period", "Indicators");

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic D", "D period", "Indicators");

		_envelopePeriod = Param(nameof(EnvelopePeriod), 32)
			.SetGreaterThanZero()
			.SetDisplay("Envelope Period", "SMA period for envelope", "Indicators");

		_envelopeDeviation = Param(nameof(EnvelopeDeviation), 0.3m)
			.SetDisplay("Envelope Dev %", "Deviation percent for envelope bands", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevK = null;
		_prevD = null;

		var stoch = new StochasticOscillator
		{
			K = { Length = StochasticKPeriod },
			D = { Length = StochasticDPeriod }
		};

		var sma = new SimpleMovingAverage { Length = EnvelopePeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stoch, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue, IIndicatorValue smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var sv = (StochasticOscillatorValue)stochValue;
		if (sv.K is not decimal currentK || sv.D is not decimal currentD)
			return;

		var sma = smaValue.ToDecimal();
		if (sma <= 0)
			return;

		var deviation = EnvelopeDeviation / 100m;
		var upperBand = sma * (1m + deviation);
		var lowerBand = sma * (1m - deviation);
		var close = candle.ClosePrice;

		// Exit long at upper envelope
		if (Position > 0 && close >= upperBand)
		{
			SellMarket();
		}
		// Exit short at lower envelope
		else if (Position < 0 && close <= lowerBand)
		{
			BuyMarket();
		}

		// Stochastic K/D crossover entries
		if (_prevK.HasValue && _prevD.HasValue)
		{
			// Bullish cross: K crosses above D
			if (_prevK.Value <= _prevD.Value && currentK > currentD && Position <= 0)
			{
				BuyMarket();
			}
			// Bearish cross: K crosses below D
			else if (_prevK.Value >= _prevD.Value && currentK < currentD && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevK = currentK;
		_prevD = currentD;
	}
}
