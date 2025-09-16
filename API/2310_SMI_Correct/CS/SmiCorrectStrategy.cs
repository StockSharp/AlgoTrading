namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on Stochastic Momentum Index crossings.
/// Buys when the SMI falls below its signal line and sells when it rises above.
/// </summary>
public class SmiCorrectStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smiLength;
	private readonly StrategyParam<int> _signalLength;

	private StochasticOscillator _stochastic;
	private SimpleMovingAverage _signal;
	private decimal? _prevSmi;
	private decimal? _prevSignal;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period for SMI calculation.
	/// </summary>
	public int SmiLength
	{
		get => _smiLength.Value;
		set => _smiLength.Value = value;
	}

	/// <summary>
	/// Smoothing period for the signal line.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SmiCorrectStrategy"/> class.
	/// </summary>
	public SmiCorrectStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_smiLength = Param(nameof(SmiLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("SMI Length", "Period for SMI calculation", "SMI");

		_signalLength = Param(nameof(SignalLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Smoothing period", "SMI");
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
		_prevSmi = null;
		_prevSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_stochastic = new StochasticOscillator
		{
			K = { Length = SmiLength },
			D = { Length = 1 }
		};

		_signal = new SimpleMovingAverage { Length = SignalLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawIndicator(area, _signal);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal k)
			return;

		var signalValue = _signal.Process(new DecimalIndicatorValue(_signal, k, candle.ServerTime));
		if (!signalValue.IsFormed)
		{
			_prevSmi = k;
			_prevSignal = signalValue.ToDecimal();
			return;
		}

		var signal = signalValue.ToDecimal();
		if (_prevSmi is null || _prevSignal is null)
		{
			_prevSmi = k;
			_prevSignal = signal;
			return;
		}

		var crossUp = _prevSmi < _prevSignal && k >= signal;
		var crossDown = _prevSmi > _prevSignal && k <= signal;

		if (crossDown && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossUp && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevSmi = k;
		_prevSignal = signal;
	}
}
