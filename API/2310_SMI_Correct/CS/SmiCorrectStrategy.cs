using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Stochastic Momentum Index crossings.
/// Uses Stochastic %K with a signal line (SMA of %K).
/// Buys when K crosses above signal, sells when K crosses below.
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

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int SmiLength
	{
		get => _smiLength.Value;
		set => _smiLength.Value = value;
	}

	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	public SmiCorrectStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_stochastic = null;
		_signal = null;
		_prevSmi = null;
		_prevSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevSmi = null;
		_prevSignal = null;

		_stochastic = new StochasticOscillator
		{
			K = { Length = SmiLength },
			D = { Length = 1 }
		};

		_signal = new SimpleMovingAverage { Length = SignalLength };

		Indicators.Add(_stochastic);
		Indicators.Add(_signal);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandleNew)
			.Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandleNew(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stochResult = _stochastic.Process(candle);
		if (!_stochastic.IsFormed)
			return;

		var stochTyped = (StochasticOscillatorValue)stochResult;
		if (stochTyped.K is not decimal k)
			return;

		var signalResult = _signal.Process(new DecimalIndicatorValue(_signal, k, candle.OpenTime) { IsFinal = true });
		if (!_signal.IsFormed)
		{
			_prevSmi = k;
			return;
		}

		var signal = signalResult.ToDecimal();

		if (_prevSmi is null || _prevSignal is null)
		{
			_prevSmi = k;
			_prevSignal = signal;
			return;
		}

		var crossUp = _prevSmi < _prevSignal && k >= signal;
		var crossDown = _prevSmi > _prevSignal && k <= signal;

		if (crossUp && Position == 0)
			BuyMarket();
		else if (crossDown && Position == 0)
			SellMarket();

		_prevSmi = k;
		_prevSignal = signal;
	}
}
