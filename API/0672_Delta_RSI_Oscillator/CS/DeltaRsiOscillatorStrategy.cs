namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public enum SignalCondition
{
	ZeroCrossing,
	SignalLineCrossing,
	DirectionChange,
}

/// <summary>
/// Delta-RSI Oscillator Strategy
/// </summary>
public class DeltaRsiOscillatorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<SignalCondition> _buyCondition;
	private readonly StrategyParam<SignalCondition> _sellCondition;
	private readonly StrategyParam<SignalCondition> _exitCondition;
	private readonly StrategyParam<bool> _useLong;
	private readonly StrategyParam<bool> _useShort;

	private RelativeStrengthIndex _rsi = null!;
	private ExponentialMovingAverage _signal = null!;

	private decimal _prevRsi;
	private decimal _prevDelta;
	private decimal _prevPrevDelta;
	private decimal _prevSignal;

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	public SignalCondition BuyCondition
	{
		get => _buyCondition.Value;
		set => _buyCondition.Value = value;
	}

	public SignalCondition SellCondition
	{
		get => _sellCondition.Value;
		set => _sellCondition.Value = value;
	}

	public SignalCondition ExitCondition
	{
		get => _exitCondition.Value;
		set => _exitCondition.Value = value;
	}

	public bool UseLong
	{
		get => _useLong.Value;
		set => _useLong.Value = value;
	}

	public bool UseShort
	{
		get => _useShort.Value;
		set => _useShort.Value = value;
	}

	public DeltaRsiOscillatorStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General");
		_rsiLength = Param(nameof(RsiLength), 21).SetRange(1, 100).SetDisplay("RSI Length", "RSI period", "Model Parameters").SetCanOptimize(true);
		_signalLength = Param(nameof(SignalLength), 9).SetRange(1, 100).SetDisplay("Signal Length", "EMA period", "Model Parameters").SetCanOptimize(true);
		_buyCondition = Param(nameof(BuyCondition), SignalCondition.ZeroCrossing).SetDisplay("Buy Condition", "Entry condition for longs", "Conditions");
		_sellCondition = Param(nameof(SellCondition), SignalCondition.ZeroCrossing).SetDisplay("Sell Condition", "Entry condition for shorts", "Conditions");
		_exitCondition = Param(nameof(ExitCondition), SignalCondition.ZeroCrossing).SetDisplay("Exit Condition", "Exit condition", "Conditions");
		_useLong = Param(nameof(UseLong), true).SetDisplay("Enable Long", "Allow long trades", "General");
		_useShort = Param(nameof(UseShort), true).SetDisplay("Enable Short", "Allow short trades", "General");
	}

	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_prevRsi = 0;
		_prevDelta = 0;
		_prevPrevDelta = 0;
		_prevSignal = 0;
		base.OnReseted();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_signal = new ExponentialMovingAverage { Length = SignalLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _signal);
			DrawOwnTrades(area);
		}

		StartProtection();

		base.OnStarted(time);
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed)
			return;

		var delta = rsiValue - _prevRsi;
		_prevRsi = rsiValue;

		var signalValue = _signal.Process(delta, candle.CloseTime, true).ToDecimal();

		if (!_signal.IsFormed)
		{
			_prevSignal = signalValue;
			_prevPrevDelta = _prevDelta;
			_prevDelta = delta;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevSignal = signalValue;
			_prevPrevDelta = _prevDelta;
			_prevDelta = delta;
			return;
		}

		var crossUp = _prevDelta <= 0 && delta > 0;
		var crossDown = _prevDelta >= 0 && delta < 0;

		var crossSignalUp = _prevDelta <= _prevSignal && delta > signalValue;
		var crossSignalDown = _prevDelta >= _prevSignal && delta < signalValue;

		var dirChangeUp = delta > _prevDelta && _prevDelta < _prevPrevDelta && _prevDelta < 0;
		var dirChangeDown = delta < _prevDelta && _prevDelta > _prevPrevDelta && _prevDelta > 0;

		var goLong = BuyCondition switch
		{
			SignalCondition.DirectionChange => dirChangeUp,
			SignalCondition.SignalLineCrossing => crossSignalUp,
			_ => crossUp,
		};

		var goShort = SellCondition switch
		{
			SignalCondition.DirectionChange => dirChangeDown,
			SignalCondition.SignalLineCrossing => crossSignalDown,
			_ => crossDown,
		};

		var exitLong = ExitCondition switch
		{
			SignalCondition.DirectionChange => dirChangeDown,
			SignalCondition.SignalLineCrossing => crossSignalDown,
			_ => crossDown,
		};

		var exitShort = ExitCondition switch
		{
			SignalCondition.DirectionChange => dirChangeUp,
			SignalCondition.SignalLineCrossing => crossSignalUp,
			_ => crossUp,
		};

		if (goLong && UseLong && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (goShort && UseShort && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && exitLong)
			ClosePosition();
		else if (Position < 0 && exitShort)
			ClosePosition();

		_prevSignal = signalValue;
		_prevPrevDelta = _prevDelta;
		_prevDelta = delta;
	}
}
