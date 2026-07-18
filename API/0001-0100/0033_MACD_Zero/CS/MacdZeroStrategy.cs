using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades MACD crossings of the zero line.
/// Buys when MACD crosses above zero, sells when MACD crosses below zero.
/// </summary>
public class MacdZeroStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevMacd;
	private bool _hasPrev;
	private int _cooldown;

	/// <summary>
	/// Fast EMA period for MACD calculation.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD calculation.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period for MACD calculation.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Type of candles used for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initialize the MACD Zero strategy.
	/// </summary>
	public MacdZeroStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 8)
			.SetDisplay("Fast EMA", "Fast EMA period for MACD", "MACD")
			.SetOptimize(8, 16, 2);

		_slowPeriod = Param(nameof(SlowPeriod), 17)
			.SetDisplay("Slow EMA", "Slow EMA period for MACD", "MACD")
			.SetOptimize(15, 30, 2);

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetDisplay("Signal", "Signal line period for MACD", "MACD")
			.SetOptimize(7, 12, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 700)
			.SetRange(1, 1000)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "General");
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
		_prevMacd = default;
		_hasPrev = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevMacd = 0;
		_hasPrev = false;
		_cooldown = 0;

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastPeriod },
				LongMa = { Length = SlowPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFormed)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signal)
			return;

		if (!_hasPrev)
		{
			_prevMacd = macdLine;
			_hasPrev = true;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevMacd = macdLine;
			return;
		}

		// Zero line crossover signals
		var prevBelow = _prevMacd < 0;
		var currAbove = macdLine >= 0;
		var prevAbove = _prevMacd >= 0;
		var currBelow = macdLine < 0;

		if (Position == 0 && prevBelow && currAbove)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && prevAbove && currBelow)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && prevAbove && currBelow)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && prevBelow && currAbove)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevMacd = macdLine;
	}
}
