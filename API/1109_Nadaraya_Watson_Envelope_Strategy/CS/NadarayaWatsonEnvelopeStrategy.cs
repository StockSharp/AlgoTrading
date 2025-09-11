using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Nadaraya-Watson envelope strategy.
/// Enters long when price crosses above the lower envelope
/// and exits or reverses when price crosses envelopes.
/// </summary>
public class NadarayaWatsonEnvelopeStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackWindow;
	private readonly StrategyParam<decimal> _relativeWeighting;
	private readonly StrategyParam<int> _startRegressionBar;
	private readonly StrategyParam<string> _strategyType;
	private readonly StrategyParam<DataType> _candleType;

	private readonly NadarayaWatson _highEnvelope = new();
	private readonly NadarayaWatson _lowEnvelope = new();

	private decimal _prevEnvelopeHigh;
	private decimal _prevEnvelopeLow;
	private decimal _prevClose;
	private bool _hasPrev;

	/// <summary>
	/// Lookback window for regression.
	/// </summary>
	public int LookbackWindow
	{
		get => _lookbackWindow.Value;
		set => _lookbackWindow.Value = value;
	}

	/// <summary>
	/// Relative weighting (alpha).
	/// </summary>
	public decimal RelativeWeighting
	{
		get => _relativeWeighting.Value;
		set => _relativeWeighting.Value = value;
	}

	/// <summary>
	/// Start regression at bar.
	/// </summary>
	public int StartRegressionBar
	{
		get => _startRegressionBar.Value;
		set => _startRegressionBar.Value = value;
	}

	/// <summary>
	/// Strategy type (Long Only or Long/Short).
	/// </summary>
	public string StrategyType
	{
		get => _strategyType.Value;
		set => _strategyType.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="NadarayaWatsonEnvelopeStrategy"/>.
	/// </summary>
	public NadarayaWatsonEnvelopeStrategy()
	{
		_lookbackWindow = Param(nameof(LookbackWindow), 8)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Window", "Kernel lookback", "Parameters");

		_relativeWeighting = Param(nameof(RelativeWeighting), 8m)
			.SetGreaterThanZero()
			.SetDisplay("Relative Weighting", "Alpha parameter", "Parameters");

		_startRegressionBar = Param(nameof(StartRegressionBar), 25)
			.SetGreaterThanZero()
			.SetDisplay("Start Regression Bar", "Kernel center", "Parameters");

		_strategyType = Param(nameof(StrategyType), "Long Only")
			.SetDisplay("Strategy Type", "Long only or long/short", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "Parameters");
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
		_highEnvelope.Reset();
		_lowEnvelope.Reset();
		_prevEnvelopeHigh = 0m;
		_prevEnvelopeLow = 0m;
		_prevClose = 0m;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		_highEnvelope.Length = LookbackWindow;
		_highEnvelope.Alpha = RelativeWeighting;
		_highEnvelope.StartBar = StartRegressionBar;

		_lowEnvelope.Length = LookbackWindow;
		_lowEnvelope.Alpha = RelativeWeighting;
		_lowEnvelope.StartBar = StartRegressionBar;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var highVal = _highEnvelope.Process(new DecimalIndicatorValue(_highEnvelope, candle.HighPrice, candle.OpenTime));
		var lowVal = _lowEnvelope.Process(new DecimalIndicatorValue(_lowEnvelope, candle.LowPrice, candle.OpenTime));

		if (!_highEnvelope.IsFormed || !_lowEnvelope.IsFormed)
		{
			_prevEnvelopeHigh = highVal.GetValue<decimal>();
			_prevEnvelopeLow = lowVal.GetValue<decimal>();
			_prevClose = candle.ClosePrice;
			_hasPrev = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading() || !_hasPrev)
			return;

		var envelopeHigh = highVal.GetValue<decimal>();
		var envelopeLow = lowVal.GetValue<decimal>();
		var close = candle.ClosePrice;

		var longCondition = _prevClose <= _prevEnvelopeLow && close > envelopeLow;
		var exitLongCondition = _prevEnvelopeHigh <= _prevClose && envelopeHigh > close;
		var shortCondition = StrategyType == "Long/Short" && _prevClose >= _prevEnvelopeHigh && close < envelopeHigh;
		var exitShortCondition = StrategyType == "Long/Short" && _prevEnvelopeLow >= _prevClose && envelopeLow < close;

		if (StrategyType == "Long Only")
		{
			if (longCondition && Position <= 0)
				BuyMarket();
			else if (exitLongCondition && Position > 0)
				SellMarket(Position);
		}
		else
		{
			if (longCondition && Position <= 0)
				BuyMarket();
			if (shortCondition && Position >= 0)
				SellMarket();
			if (exitLongCondition && Position > 0)
				SellMarket(Position);
			if (exitShortCondition && Position < 0)
				BuyMarket(-Position);
		}

		_prevEnvelopeHigh = envelopeHigh;
		_prevEnvelopeLow = envelopeLow;
		_prevClose = close;
	}

	private class NadarayaWatson : Indicator<decimal>
	{
		public int Length { get; set; } = 8;
		public decimal Alpha { get; set; } = 8m;
		public int StartBar { get; set; } = 25;

		private readonly List<decimal> _values = new();

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var price = input.GetValue<decimal>();
			var logPrice = (decimal)Math.Log((double)price);

			_values.Insert(0, logPrice);
			if (_values.Count > Length + 1)
				_values.RemoveAt(_values.Count - 1);

			if (_values.Count <= Length)
			{
				IsFormed = false;
				return new DecimalIndicatorValue(this, price, input.Time);
			}

			decimal sumWeights = 0m;
			decimal sumXWeights = 0m;
			for (var i = 0; i <= Length; i++)
			{
				var dist = StartBar - i;
				var weight = (decimal)Math.Pow((double)(1m + dist * dist / (2m * Alpha * Length * Length)), (double)(-Alpha));
				sumWeights += weight;
				sumXWeights += weight * _values[i];
			}

			IsFormed = true;
			var envelope = (decimal)Math.Exp((double)(sumXWeights / sumWeights));
			return new DecimalIndicatorValue(this, envelope, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_values.Clear();
		}
	}
}
