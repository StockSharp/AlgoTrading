namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Three Signal Directional Trend Strategy.
/// Combines MACD, Stochastic, and RSI signals.
/// Enters when at least 2 of 3 indicators agree on direction.
/// </summary>
public class ThreeSignalDirectionalTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdAvgLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _cooldownBars;

	private MovingAverageConvergenceDivergenceSignal _macd;
	private StochasticOscillator _stochastic;
	private RelativeStrengthIndex _rsi;

	private decimal _prevMacdSignal;
	private bool _macdInit;
	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	public int MacdAvgLength
	{
		get => _macdAvgLength.Value;
		set => _macdAvgLength.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public ThreeSignalDirectionalTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length", "MACD");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length", "MACD");

		_macdAvgLength = Param(nameof(MacdAvgLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA length", "MACD");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_macd = null;
		_stochastic = null;
		_rsi = null;
		_prevMacdSignal = 0;
		_macdInit = false;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd = { ShortMa = { Length = MacdFastLength }, LongMa = { Length = MacdSlowLength } },
			SignalMa = { Length = MacdAvgLength }
		};

		_stochastic = new StochasticOscillator
		{
			K = { Length = 14 },
			D = { Length = 3 }
		};

		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, _stochastic, _rsi, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue macdVal, IIndicatorValue stochVal, IIndicatorValue rsiVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_macd.IsFormed || !_stochastic.IsFormed || !_rsi.IsFormed)
			return;

		if (macdVal.IsEmpty || stochVal.IsEmpty || rsiVal.IsEmpty)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdVal;
		if (macdTyped.Signal is not decimal macdSignal)
			return;

		var stochTyped = (StochasticOscillatorValue)stochVal;
		if (stochTyped.K is not decimal stochK)
			return;

		var rsi = rsiVal.ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevMacdSignal = macdSignal;
			_macdInit = true;
			return;
		}

		if (!_macdInit)
		{
			_prevMacdSignal = macdSignal;
			_macdInit = true;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevMacdSignal = macdSignal;
			return;
		}

		var longCount = 0;
		var shortCount = 0;

		// MACD signal rising/falling
		if (macdSignal > _prevMacdSignal)
			longCount++;
		else if (macdSignal < _prevMacdSignal)
			shortCount++;

		// Stochastic oversold/overbought
		if (stochK <= 20)
			longCount++;
		else if (stochK >= 80)
			shortCount++;

		// RSI direction
		if (rsi < 40)
			longCount++;
		else if (rsi > 60)
			shortCount++;

		// Trade when at least 2 signals agree
		if (longCount >= 2 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		else if (shortCount >= 2 && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}

		_prevMacdSignal = macdSignal;
	}
}
