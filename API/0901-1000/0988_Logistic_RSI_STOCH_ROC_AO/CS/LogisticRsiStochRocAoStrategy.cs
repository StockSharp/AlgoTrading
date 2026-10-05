using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Logistic RSI, Stochastic, ROC, AO strategy.
/// The selected indicator is centred around zero and scaled by its highest absolute value over Length bars, then passed through
/// the logistic map x * (1 - |x|). The standard deviation of the mapped values over Length bars, signed by the latest mapped value,
/// goes long when it crosses above zero and short when it crosses below zero, reversing any open position.
/// </summary>
public class LogisticRsiStochRocAoStrategy : Strategy
{
	/// <summary>
	/// Indicator fed into the logistic map.
	/// </summary>
	public enum LogisticIndicators
	{
		/// <summary>
		/// Close momentum over LenLd bars.
		/// </summary>
		LogisticDominance,

		/// <summary>
		/// Rate of change over LenRoc bars.
		/// </summary>
		Roc,

		/// <summary>
		/// RSI over LenRsi bars, centred at 50.
		/// </summary>
		Rsi,

		/// <summary>
		/// Stochastic %K over LenSto bars, centred at 50.
		/// </summary>
		Stochastic,

		/// <summary>
		/// Awesome oscillator.
		/// </summary>
		AwesomeOscillator,
	}

	private readonly StrategyParam<LogisticIndicators> _indicator;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _lenLd;
	private readonly StrategyParam<int> _lenRoc;
	private readonly StrategyParam<int> _lenRsi;
	private readonly StrategyParam<int> _lenSto;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _scale;
	private StandardDeviation _deviation;
	private decimal? _prevSigned;

	/// <summary>
	/// Indicator fed into the logistic map.
	/// </summary>
	public LogisticIndicators Indicator
	{
		get => _indicator.Value;
		set => _indicator.Value = value;
	}

	/// <summary>
	/// Scaling and standard deviation length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Momentum length of the logistic dominance source.
	/// </summary>
	public int LenLd
	{
		get => _lenLd.Value;
		set => _lenLd.Value = value;
	}

	/// <summary>
	/// ROC length.
	/// </summary>
	public int LenRoc
	{
		get => _lenRoc.Value;
		set => _lenRoc.Value = value;
	}

	/// <summary>
	/// RSI length.
	/// </summary>
	public int LenRsi
	{
		get => _lenRsi.Value;
		set => _lenRsi.Value = value;
	}

	/// <summary>
	/// Stochastic length.
	/// </summary>
	public int LenSto
	{
		get => _lenSto.Value;
		set => _lenSto.Value = value;
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
	public LogisticRsiStochRocAoStrategy()
	{
		_indicator = Param(nameof(Indicator), LogisticIndicators.LogisticDominance)
			.SetDisplay("Indicator", "Indicator fed into the logistic map", "General");

		_length = Param(nameof(Length), 13)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Scaling and standard deviation length", "Indicators");

		_lenLd = Param(nameof(LenLd), 5)
			.SetGreaterThanZero()
			.SetDisplay("LD Length", "Momentum length of the logistic dominance source", "Indicators");

		_lenRoc = Param(nameof(LenRoc), 9)
			.SetGreaterThanZero()
			.SetDisplay("ROC Length", "ROC length", "Indicators");

		_lenRsi = Param(nameof(LenRsi), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI length", "Indicators");

		_lenSto = Param(nameof(LenSto), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Length", "Stochastic length", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_prevSigned = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevSigned = null;

		var momentum = new Momentum { Length = LenLd };
		var roc = new RateOfChange { Length = LenRoc };
		var rsi = new RelativeStrengthIndex { Length = LenRsi };
		var stochastic = new StochasticOscillator();
		stochastic.K.Length = LenSto;
		stochastic.D.Length = 3;
		var ao = new AwesomeOscillator();
		_scale = new Highest { Length = Length };
		_deviation = new StandardDeviation { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(momentum, roc, rsi, stochastic, ao, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue momentumValue, IIndicatorValue rocValue, IIndicatorValue rsiValue, IIndicatorValue stochasticValue, IIndicatorValue aoValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		decimal? source = Indicator switch
		{
			LogisticIndicators.LogisticDominance => momentumValue.IsFormed ? momentumValue.GetValue<decimal>() : null,
			LogisticIndicators.Roc => rocValue.IsFormed ? rocValue.GetValue<decimal>() : null,
			LogisticIndicators.Rsi => rsiValue.IsFormed ? rsiValue.GetValue<decimal>() - 50m : null,
			LogisticIndicators.Stochastic => stochasticValue.IsFormed && stochasticValue is IStochasticOscillatorValue { K: decimal k } ? k - 50m : null,
			LogisticIndicators.AwesomeOscillator => aoValue.IsFormed ? aoValue.GetValue<decimal>() : null,
			_ => null,
		};

		if (source is not decimal value)
			return;

		var scaleValue = _scale.Process(new DecimalIndicatorValue(_scale, Math.Abs(value), candle.OpenTime) { IsFinal = true });
		if (!scaleValue.IsFormed)
			return;

		var scale = scaleValue.GetValue<decimal>();
		var x = scale > 0 ? value / scale : 0m;
		var mapped = x * (1m - Math.Abs(x));

		var deviationValue = _deviation.Process(new DecimalIndicatorValue(_deviation, mapped, candle.OpenTime) { IsFinal = true });
		if (!deviationValue.IsFormed)
			return;

		var signed = deviationValue.GetValue<decimal>() * Math.Sign(mapped);
		var prev = _prevSigned;
		_prevSigned = signed;

		if (prev is not decimal prevSigned)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (prevSigned <= 0 && signed > 0 && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (prevSigned >= 0 && signed < 0 && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
