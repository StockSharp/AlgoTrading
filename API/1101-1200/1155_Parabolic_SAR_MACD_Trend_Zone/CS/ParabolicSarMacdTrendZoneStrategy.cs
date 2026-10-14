using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR with MACD confirmation.
/// A close crossing above the Parabolic SAR while the MACD line is above its signal line goes long, a close crossing below the SAR
/// while MACD is below its signal goes short, reversing an opposite position. A position is closed on the opposite price/SAR
/// crossover or on the opposite MACD/signal crossover.
/// </summary>
public class ParabolicSarMacdTrendZoneStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarStart;
	private readonly StrategyParam<decimal> _sarIncrement;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private decimal? _prevSar;
	private decimal? _prevMacd;
	private decimal? _prevSignal;

	/// <summary>
	/// Initial SAR acceleration factor.
	/// </summary>
	public decimal SarStart
	{
		get => _sarStart.Value;
		set => _sarStart.Value = value;
	}

	/// <summary>
	/// SAR acceleration factor increment.
	/// </summary>
	public decimal SarIncrement
	{
		get => _sarIncrement.Value;
		set => _sarIncrement.Value = value;
	}

	/// <summary>
	/// Maximum SAR acceleration factor.
	/// </summary>
	public decimal SarMax
	{
		get => _sarMax.Value;
		set => _sarMax.Value = value;
	}

	/// <summary>
	/// Fast EMA period of MACD.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow EMA period of MACD.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// Signal line period of MACD.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
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
	public ParabolicSarMacdTrendZoneStrategy()
	{
		_sarStart = Param(nameof(SarStart), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Start", "Initial SAR acceleration factor", "Indicators");

		_sarIncrement = Param(nameof(SarIncrement), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Increment", "SAR acceleration factor increment", "Indicators");

		_sarMax = Param(nameof(SarMax), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max", "Maximum SAR acceleration factor", "Indicators");

		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period of MACD", "Indicators");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period of MACD", "Indicators");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal line period of MACD", "Indicators");

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
		_prevClose = null;
		_prevSar = null;
		_prevMacd = null;
		_prevSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var sar = new ParabolicSar
		{
			Acceleration = SarStart,
			AccelerationStep = SarIncrement,
			AccelerationMax = SarMax
		};

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sar, macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sar);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, macd);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue sarValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!sarValue.IsFormed || !macdValue.IsFormed)
			return;

		if (macdValue is not IMovingAverageConvergenceDivergenceSignalValue { Macd: decimal macdLine, Signal: decimal signalLine })
			return;

		var sar = sarValue.ToDecimal();
		var close = candle.ClosePrice;

		var prevClose = _prevClose;
		var prevSar = _prevSar;
		var prevMacd = _prevMacd;
		var prevSignal = _prevSignal;

		_prevClose = close;
		_prevSar = sar;
		_prevMacd = macdLine;
		_prevSignal = signalLine;

		if (prevClose is not decimal pc || prevSar is not decimal ps || prevMacd is not decimal pm || prevSignal is not decimal pSig)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossUp = pc <= ps && close > sar;
		var crossDown = pc >= ps && close < sar;
		var macdCrossUp = pm <= pSig && macdLine > signalLine;
		var macdCrossDown = pm >= pSig && macdLine < signalLine;

		if (crossUp && macdLine > signalLine && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && macdLine < signalLine && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && (crossDown || macdCrossDown))
			SellMarket(Position);
		else if (Position < 0 && (crossUp || macdCrossUp))
			BuyMarket(-Position);
	}
}
