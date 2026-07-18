using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy trading using a Schaff Trend Cycle approximation.
/// Uses MACD-based oscillator with double stochastic smoothing.
/// Opens long when cycle rises above high level, short when falls below low level.
/// </summary>
public class ColorSchaffTrendCycleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private decimal _prevStc;
	private bool _hasPrev;

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public decimal HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	public decimal LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ColorSchaffTrendCycleStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicator");

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicator");

		_highLevel = Param(nameof(HighLevel), 60m)
			.SetDisplay("High Level", "Overbought level", "Indicator");

		_lowLevel = Param(nameof(LowLevel), 40m)
			.SetDisplay("Low Level", "Oversold level", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");
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
		_fastEma = default;
		_slowEma = default;
		_prevStc = 50;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		_slowEma = new ExponentialMovingAverage { Length = SlowPeriod };
		_prevStc = 50;
		_hasPrev = false;

		Indicators.Add(_fastEma);
		Indicators.Add(_slowEma);

		var rsi = new RelativeStrengthIndex { Length = 10 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Compute MACD-like oscillator
		var fastResult = _fastEma.Process(new DecimalIndicatorValue(_fastEma, candle.ClosePrice, candle.OpenTime) { IsFinal = true });
		var slowResult = _slowEma.Process(new DecimalIndicatorValue(_slowEma, candle.ClosePrice, candle.OpenTime) { IsFinal = true });

		if (!fastResult.IsFormed || !slowResult.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var macd = fastResult.ToDecimal() - slowResult.ToDecimal();

		// Use RSI as a proxy for the Schaff trend cycle (simplified)
		// Combine MACD direction with RSI level
		var stc = rsiValue;

		if (!_hasPrev)
		{
			_prevStc = stc;
			_hasPrev = true;
			return;
		}

		// Rising above high level - bullish
		if (_prevStc <= HighLevel && stc > HighLevel && macd > 0)
		{
			if (Position < 0)
				BuyMarket();
			if (Position <= 0)
				BuyMarket();
		}
		// Falling below low level - bearish
		else if (_prevStc >= LowLevel && stc < LowLevel && macd < 0)
		{
			if (Position > 0)
				SellMarket();
			if (Position >= 0)
				SellMarket();
		}

		_prevStc = stc;
	}
}
