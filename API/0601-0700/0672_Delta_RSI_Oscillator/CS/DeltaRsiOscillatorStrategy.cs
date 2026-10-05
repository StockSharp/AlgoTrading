using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Delta-RSI oscillator strategy.
/// Delta-RSI is the bar-to-bar change of RSI and its signal line is an EMA of it.
/// Entries follow BuyCondition: Delta-RSI crossing zero, crossing its signal line or changing direction;
/// a bullish event opens a long, a bearish event a short (each side enabled by UseLong and UseShort), reversing an opposite position.
/// Otherwise positions are closed by the opposite event of ExitCondition.
/// </summary>
public class DeltaRsiOscillatorStrategy : Strategy
{
	/// <summary>
	/// Delta-RSI events that can trigger entries or exits.
	/// </summary>
	public enum DeltaRsiConditions
	{
		/// <summary>
		/// Delta-RSI crosses zero.
		/// </summary>
		ZeroCrossing,

		/// <summary>
		/// Delta-RSI crosses its signal line.
		/// </summary>
		SignalLineCrossing,

		/// <summary>
		/// Delta-RSI changes direction.
		/// </summary>
		DirectionChange,
	}

	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<DeltaRsiConditions> _buyCondition;
	private readonly StrategyParam<DeltaRsiConditions> _exitCondition;
	private readonly StrategyParam<bool> _useLong;
	private readonly StrategyParam<bool> _useShort;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _signal;
	private decimal? _prevRsi;
	private decimal? _prevDelta;
	private decimal? _prevPrevDelta;
	private decimal? _prevSignal;

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// EMA length of the signal line.
	/// </summary>
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }

	/// <summary>
	/// Event that opens positions.
	/// </summary>
	public DeltaRsiConditions BuyCondition { get => _buyCondition.Value; set => _buyCondition.Value = value; }

	/// <summary>
	/// Event that closes positions.
	/// </summary>
	public DeltaRsiConditions ExitCondition { get => _exitCondition.Value; set => _exitCondition.Value = value; }

	/// <summary>
	/// Allow long trades.
	/// </summary>
	public bool UseLong { get => _useLong.Value; set => _useLong.Value = value; }

	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool UseShort { get => _useShort.Value; set => _useShort.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public DeltaRsiOscillatorStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI length", "Indicators");

		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "EMA length of the signal line", "Indicators");

		_buyCondition = Param(nameof(BuyCondition), DeltaRsiConditions.ZeroCrossing)
			.SetDisplay("Entry Condition", "Event that opens positions", "Signals");

		_exitCondition = Param(nameof(ExitCondition), DeltaRsiConditions.ZeroCrossing)
			.SetDisplay("Exit Condition", "Event that closes positions", "Signals");

		_useLong = Param(nameof(UseLong), true)
			.SetDisplay("Use Long", "Allow long trades", "Signals");

		_useShort = Param(nameof(UseShort), true)
			.SetDisplay("Use Short", "Allow short trades", "Signals");

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
		ResetState();
	}

	private void ResetState()
	{
		_prevRsi = null;
		_prevDelta = null;
		_prevPrevDelta = null;
		_prevSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		_signal = new ExponentialMovingAverage { Length = SignalLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!rsiValue.IsFormed)
			return;

		var rsi = rsiValue.GetValue<decimal>();
		var prevRsi = _prevRsi;
		_prevRsi = rsi;

		if (prevRsi is not decimal pr)
			return;

		var delta = rsi - pr;
		var signalValue = _signal.Process(delta, candle.ServerTime, true);
		var signal = signalValue.GetValue<decimal>();

		var prevDelta = _prevDelta;
		var prevPrevDelta = _prevPrevDelta;
		var prevSignal = _prevSignal;
		_prevPrevDelta = _prevDelta;
		_prevDelta = delta;
		_prevSignal = signalValue.IsFormed ? signal : null;

		if (!signalValue.IsFormed || prevDelta is not decimal pd || prevPrevDelta is not decimal ppd || prevSignal is not decimal ps)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		bool IsBullish(DeltaRsiConditions condition) => condition switch
		{
			DeltaRsiConditions.ZeroCrossing => pd <= 0m && delta > 0m,
			DeltaRsiConditions.SignalLineCrossing => pd <= ps && delta > signal,
			_ => pd <= ppd && delta > pd,
		};

		bool IsBearish(DeltaRsiConditions condition) => condition switch
		{
			DeltaRsiConditions.ZeroCrossing => pd >= 0m && delta < 0m,
			DeltaRsiConditions.SignalLineCrossing => pd >= ps && delta < signal,
			_ => pd >= ppd && delta < pd,
		};

		if (UseLong && IsBullish(BuyCondition) && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (UseShort && IsBearish(BuyCondition) && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && IsBearish(ExitCondition))
			SellMarket(Position);
		else if (Position < 0 && IsBullish(ExitCondition))
			BuyMarket(-Position);
	}
}
