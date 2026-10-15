using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD Long strategy.
/// An RSI reading below Oversold arms a long and a reading above Overbought arms a short. The next MACD crossover
/// confirms or cancels the setup: a bullish MACD/signal cross opens an armed long, a bearish cross opens an armed short.
/// Every opposite crossover closes the current position, or reverses it when the opposite side is armed.
/// </summary>
public class MacdLongStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<DataType> _candleType;

	private bool _longArmed;
	private bool _shortArmed;
	private decimal? _prevMacd;
	private decimal? _prevSignal;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	/// <summary>
	/// MACD fast EMA period.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// MACD slow EMA period.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD signal line period.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MacdLongStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_oversold = Param(nameof(Oversold), 30m)
			.SetDisplay("Oversold", "RSI oversold level", "RSI");

		_overbought = Param(nameof(Overbought), 70m)
			.SetDisplay("Overbought", "RSI overbought level", "RSI");

		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "MACD fast EMA period", "MACD");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow EMA period", "MACD");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "MACD signal line period", "MACD");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longArmed = false;
		_shortArmed = false;
		_prevMacd = null;
		_prevSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_longArmed = false;
		_shortArmed = false;
		_prevMacd = null;
		_prevSignal = null;

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, rsi);
				DrawIndicator(oscillators, macd);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!rsiValue.IsFormed || !macdValue.IsFormed)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			return;

		var rsi = rsiValue.ToDecimal();

		if (rsi < Oversold)
			_longArmed = true;

		if (rsi > Overbought)
			_shortArmed = true;

		var prevMacd = _prevMacd;
		var prevSignal = _prevSignal;
		_prevMacd = macd;
		_prevSignal = signal;

		if (prevMacd is not decimal lastMacd || prevSignal is not decimal lastSignal)
			return;

		var crossUp = lastMacd <= lastSignal && macd > signal;
		var crossDown = lastMacd >= lastSignal && macd < signal;

		var longSignal = crossUp && _longArmed;
		var shortSignal = crossDown && _shortArmed;

		// The first crossover after the RSI extreme decides the setup either way.
		if (crossUp)
			_longArmed = false;

		if (crossDown)
			_shortArmed = false;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (longSignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && crossDown)
			SellMarket(Position);
		else if (Position < 0 && crossUp)
			BuyMarket(-Position);
	}
}
